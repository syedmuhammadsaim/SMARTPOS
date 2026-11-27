using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Domain.Entities.Sales;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Web.Controllers;

[Authorize]
public class SalesController : Controller
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public SalesController(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index(string? search, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.POSOrders
            .Include(o => o.Customer)
            .Include(o => o.Branch)
            .Include(o => o.Counter)
            .Include(o => o.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(o => o.InvoiceNo.ToLower().Contains(term) || (o.Customer != null && o.Customer.Name.ToLower().Contains(term)));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.BusinessDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.BusinessDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(100)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

        var business = await _context.Businesses.FindAsync(_tenantProvider.CurrentBusinessId);
        ViewBag.Currency = business?.CurrencyCode ?? "PKR";
        ViewBag.TotalSales = orders.Sum(o => o.Total);

        return View(orders);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var order = await _context.POSOrders
            .Include(o => o.Customer)
            .Include(o => o.Branch)
            .Include(o => o.Counter)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Payments).ThenInclude(p => p.PaymentMethod)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        var business = await _context.Businesses.FindAsync(order.BusinessId);
        ViewBag.Business = business;

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessReturn(Guid orderId, string reason)
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return Forbid();

        var order = await _context.POSOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return NotFound();

        if (order.Status == OrderStatus.Refunded || order.Status == OrderStatus.Cancelled)
        {
            TempData["ErrorMessage"] = "This order has already been returned or cancelled.";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        order.Status = OrderStatus.Refunded;
        order.UpdatedAtUtc = DateTime.UtcNow;

        // Restore stock for items
        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.BranchId == order.BranchId && w.Status == "Active");
        if (warehouse != null)
        {
            foreach (var item in order.Items)
            {
                var balance = await _context.StockBalances
                    .FirstOrDefaultAsync(s => s.ProductId == item.ProductId && s.WarehouseId == warehouse.Id);

                if (balance != null)
                {
                    balance.QtyOnHand += item.Qty;
                }

                _context.StockTransactions.Add(new Domain.Entities.Inventory.StockTransaction
                {
                    BusinessId = businessId.Value,
                    WarehouseId = warehouse.Id,
                    ProductId = item.ProductId,
                    Type = StockTransactionType.SaleReturn,
                    Qty = item.Qty,
                    UnitCost = item.PriceSnapshot,
                    ReferenceType = "SaleReturn",
                    ReferenceId = order.Id,
                    Note = $"RET-{order.InvoiceNo}"
                });
            }
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Order {order.InvoiceNo} successfully marked as Returned and stock restored.";
        return RedirectToAction(nameof(Details), new { id = orderId });
    }
}
