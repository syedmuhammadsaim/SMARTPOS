using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Domain.Entities.Inventory;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Web.Controllers;

[Authorize]
public class InventoryController : Controller
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public InventoryController(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index(Guid? warehouseId, bool lowStockOnly = false)
    {
        var warehouses = await _context.Warehouses.Include(w => w.Branch).ToListAsync();
        ViewBag.Warehouses = warehouses;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.LowStockOnly = lowStockOnly;

        var query = _context.StockBalances
            .Include(s => s.Product).ThenInclude(p => p.Category)
            .Include(s => s.Warehouse).ThenInclude(w => w.Branch)
            .AsQueryable();

        if (warehouseId.HasValue)
        {
            query = query.Where(s => s.WarehouseId == warehouseId.Value);
        }

        var list = await query.ToListAsync();

        if (lowStockOnly)
        {
            list = list.Where(s => s.Product != null && s.QtyOnHand <= s.Product.MinStock).ToList();
        }

        var business = await _context.Businesses.FindAsync(_tenantProvider.CurrentBusinessId);
        ViewBag.Currency = business?.CurrencyCode ?? "PKR";
        ViewBag.TotalValuation = list.Sum(s => s.QtyOnHand * (s.Product?.PurchasePrice ?? 0m));

        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(Guid productId, Guid warehouseId, decimal qtyAdjustment, string reason)
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        var balance = await _context.StockBalances
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId);

        var product = await _context.Products.FindAsync(productId);
        if (product == null) return NotFound();

        if (balance == null)
        {
            balance = new StockBalance
            {
                BusinessId = businessId.Value,
                ProductId = productId,
                WarehouseId = warehouseId,
                QtyOnHand = Math.Max(0, qtyAdjustment)
            };
            _context.StockBalances.Add(balance);
        }
        else
        {
            balance.QtyOnHand += qtyAdjustment;
        }

        // Record auditable transaction
        _context.StockTransactions.Add(new StockTransaction
        {
            BusinessId = businessId.Value,
            WarehouseId = warehouseId,
            ProductId = productId,
            Type = StockTransactionType.DamageAdjustment,
            Qty = qtyAdjustment,
            UnitCost = product.PurchasePrice,
            ReferenceType = "Adjustment",
            ReferenceId = balance.Id,
            Note = $"ADJ: {reason?.Trim() ?? "Manual Adjustment"}"
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Stock for '{product.Name}' adjusted by {qtyAdjustment:+#;-#;0}.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Transfers()
    {
        var transfers = await _context.StockTransfers
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Include(t => t.Items).ThenInclude(i => i.Product)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync();

        ViewBag.Warehouses = await _context.Warehouses.ToListAsync();
        ViewBag.Products = await _context.Products.Where(p => p.Status == "Active").ToListAsync();

        return View(transfers);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTransfer(Guid fromWarehouseId, Guid toWarehouseId, Guid productId, decimal qty, string? note)
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        if (fromWarehouseId == toWarehouseId)
        {
            TempData["ErrorMessage"] = "Source and destination warehouses cannot be the same.";
            return RedirectToAction(nameof(Transfers));
        }

        var sourceBalance = await _context.StockBalances
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == fromWarehouseId);

        if (sourceBalance == null || sourceBalance.QtyOnHand < qty)
        {
            TempData["ErrorMessage"] = "Insufficient stock available in source warehouse.";
            return RedirectToAction(nameof(Transfers));
        }

        var destBalance = await _context.StockBalances
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == toWarehouseId);

        if (destBalance == null)
        {
            destBalance = new StockBalance
            {
                BusinessId = businessId.Value,
                ProductId = productId,
                WarehouseId = toWarehouseId,
                QtyOnHand = 0m
            };
            _context.StockBalances.Add(destBalance);
        }

        // Deduct from source and credit to dest
        sourceBalance.QtyOnHand -= qty;
        destBalance.QtyOnHand += qty;

        var transferNo = $"TRF-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var transfer = new StockTransfer
        {
            BusinessId = businessId.Value,
            TransferNo = transferNo,
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = toWarehouseId,
            Status = "Received",
            Reason = note?.Trim()
        };
        _context.StockTransfers.Add(transfer);
        await _context.SaveChangesAsync();

        transfer.Items.Add(new StockTransferItem
        {
            BusinessId = businessId.Value,
            StockTransferId = transfer.Id,
            ProductId = productId,
            Qty = qty,
            ReceivedQty = qty
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Transfer {transferNo} successfully processed.";
        return RedirectToAction(nameof(Transfers));
    }
}
