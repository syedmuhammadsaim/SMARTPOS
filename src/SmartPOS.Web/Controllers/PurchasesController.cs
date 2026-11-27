using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Domain.Entities.Purchasing;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Web.Controllers;

[Authorize]
public class PurchasesController : Controller
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public PurchasesController(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index()
    {
        var purchases = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync();

        ViewBag.Suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
        ViewBag.Branches = await _context.Branches.Where(b => b.Status == "Active").ToListAsync();
        ViewBag.Products = await _context.Products.Where(p => p.Status == "Active").OrderBy(p => p.Name).ToListAsync();

        var business = await _context.Businesses.FindAsync(_tenantProvider.CurrentBusinessId);
        ViewBag.Currency = business?.CurrencyCode ?? "PKR";

        return View(purchases);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Guid supplierId,
        Guid branchId,
        Guid productId,
        decimal qty,
        decimal unitCost,
        string? notes)
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        var poNo = $"PO-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var total = qty * unitCost;

        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.BranchId == branchId && w.Status == "Active");
        if (warehouse == null)
        {
            warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Status == "Active");
            if (warehouse == null)
            {
                TempData["ErrorMessage"] = "No active warehouse found.";
                return RedirectToAction(nameof(Index));
            }
        }

        var po = new PurchaseOrder
        {
            BusinessId = businessId.Value,
            SupplierId = supplierId,
            WarehouseId = warehouse.Id,
            OrderNo = poNo,
            OrderDate = DateTime.UtcNow,
            TotalAmount = total,
            Status = "Approved",
            Notes = notes?.Trim()
        };

        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        var poItem = new PurchaseOrderItem
        {
            BusinessId = businessId.Value,
            PurchaseOrderId = po.Id,
            ProductId = productId,
            OrderedQty = qty,
            ReceivedQty = qty,
            UnitCost = unitCost,
            TotalCost = total
        };
        _context.PurchaseOrderItems.Add(poItem);

        // Update Stock on purchase confirmation
        if (warehouse != null)
        {
            var stockBalance = await _context.StockBalances
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == warehouse.Id);

            if (stockBalance == null)
            {
                stockBalance = new Domain.Entities.Inventory.StockBalance
                {
                    BusinessId = businessId.Value,
                    WarehouseId = warehouse.Id,
                    ProductId = productId,
                    QtyOnHand = qty
                };
                _context.StockBalances.Add(stockBalance);
            }
            else
            {
                stockBalance.QtyOnHand += qty;
            }

            _context.StockTransactions.Add(new Domain.Entities.Inventory.StockTransaction
            {
                BusinessId = businessId.Value,
                WarehouseId = warehouse.Id,
                ProductId = productId,
                Type = StockTransactionType.GoodsReceipt,
                Qty = qty,
                UnitCost = unitCost,
                ReferenceType = "PurchaseOrder",
                ReferenceId = po.Id,
                Note = poNo
            });
        }

        // Record Supplier Ledger Credit Entry
        _context.SupplierLedgers.Add(new SupplierLedger
        {
            BusinessId = businessId.Value,
            SupplierId = supplierId,
            Date = DateTime.UtcNow,
            Type = "Purchase",
            ReferenceId = po.Id,
            Credit = total,
            Debit = 0m,
            RunningBalance = total,
            Notes = $"Purchase Order {poNo}"
        });

        // Update supplier balance
        var supplier = await _context.Suppliers.FindAsync(supplierId);
        if (supplier != null)
        {
            supplier.CurrentBalance += total;
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Purchase Order {poNo} approved and stock received.";
        return RedirectToAction(nameof(Index));
    }
}
