using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.POS.DTOs;
using SmartPOS.Application.Features.POS.Interfaces;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Customers;
using SmartPOS.Domain.Entities.Organization;
using SmartPOS.Domain.Entities.Sales;
using SmartPOS.Domain.Entities.ShiftsAndSessions;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Web.Controllers;

[Authorize]
public class POSController : Controller
{
    private readonly SmartPOSDbContext _context;
    private readonly IPOSService _posService;
    private readonly ITenantProvider _tenantProvider;

    public POSController(
        SmartPOSDbContext context,
        IPOSService posService,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _posService = posService;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index()
    {
        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue) return RedirectToAction("Login", "Account");

        var business = await _context.Businesses.FindAsync(businessId.Value);
        ViewBag.Business = business;
        ViewBag.Currency = business?.CurrencyCode ?? "PKR";

        // Branches and Counters
        var branches = await _context.Branches.Include(b => b.Counters).ToListAsync();
        ViewBag.Branches = branches;

        var defaultBranch = branches.FirstOrDefault();
        var defaultCounter = defaultBranch?.Counters.FirstOrDefault();

        // Check for active Counter Session for this counter / cashier
        CounterSession? activeSession = null;
        if (defaultCounter != null)
        {
            activeSession = await _context.CounterSessions
                .FirstOrDefaultAsync(s => s.CounterId == defaultCounter.Id && s.Status == CounterSessionStatus.Open);

            // If no active session exists, automatically create or open one for seamless POS operations
            if (activeSession == null)
            {
                var userId = _tenantProvider.CurrentUserId ?? Guid.NewGuid();
                activeSession = new CounterSession
                {
                    BusinessId = businessId.Value,
                    BranchId = defaultBranch!.Id,
                    CounterId = defaultCounter.Id,
                    EmployeeId = userId,
                    ShiftId = Guid.NewGuid(),
                    OpeningCash = 1000m,
                    StartUtc = DateTime.UtcNow,
                    Status = CounterSessionStatus.Open
                };
                _context.CounterSessions.Add(activeSession);
                await _context.SaveChangesAsync();
            }
        }

        ViewBag.ActiveSession = activeSession;
        ViewBag.ActiveBranchId = defaultBranch?.Id;
        ViewBag.ActiveCounterId = defaultCounter?.Id;

        // Categories
        var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Categories = categories;

        // Payment Methods
        var paymentMethods = await _context.PaymentMethods.Where(p => p.IsActive).ToListAsync();
        ViewBag.PaymentMethods = paymentMethods;

        // Customers
        var customers = await _context.Customers.OrderBy(c => c.Name).Take(50).ToListAsync();
        ViewBag.Customers = customers;

        // Products with barcodes and stock
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Barcodes)
            .Where(p => p.Status == "Active")
            .OrderBy(p => p.Name)
            .Take(100)
            .ToListAsync();

        var stockBalances = await _context.StockBalances.ToListAsync();
        var stockMap = stockBalances.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.Sum(s => s.QtyOnHand));
        ViewBag.StockMap = stockMap;

        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> SearchProducts(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            var defaultList = await _context.Products
                .Include(p => p.Barcodes)
                .Where(p => p.Status == "Active")
                .Take(40)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    sku = p.Sku,
                    barcode = p.Barcodes.FirstOrDefault(b => b.IsPrimary) != null ? p.Barcodes.FirstOrDefault(b => b.IsPrimary)!.Barcode : (p.Barcodes.FirstOrDefault() != null ? p.Barcodes.FirstOrDefault()!.Barcode : p.Sku),
                    price = p.SalePrice,
                    categoryId = p.CategoryId
                })
                .ToListAsync();

            return Json(defaultList);
        }

        var clean = query.Trim().ToLower();

        var matches = await _context.Products
            .Include(p => p.Barcodes)
            .Where(p => p.Status == "Active" &&
                (p.Name.ToLower().Contains(clean) ||
                 p.Sku.ToLower().Contains(clean) ||
                 p.Barcodes.Any(b => b.Barcode.ToLower().Contains(clean))))
            .Take(30)
            .Select(p => new
            {
                id = p.Id,
                name = p.Name,
                sku = p.Sku,
                barcode = p.Barcodes.FirstOrDefault(b => b.IsPrimary) != null ? p.Barcodes.FirstOrDefault(b => b.IsPrimary)!.Barcode : (p.Barcodes.FirstOrDefault() != null ? p.Barcodes.FirstOrDefault()!.Barcode : p.Sku),
                price = p.SalePrice,
                categoryId = p.CategoryId
            })
            .ToListAsync();

        return Json(matches);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout([FromBody] CreateSaleOrderDto dto)
    {
        var cashierId = _tenantProvider.CurrentUserId ?? Guid.NewGuid();
        var result = await _posService.CreateSaleOrderAsync(dto, cashierId);

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Json(new
        {
            success = true,
            orderId = result.OrderId,
            invoiceNo = result.InvoiceNo,
            subtotal = result.Subtotal,
            discountTotal = result.DiscountTotal,
            taxTotal = result.TaxTotal,
            total = result.Total,
            paidAmount = result.PaidAmount,
            changeAmount = result.ChangeAmount,
            fbrInvoiceNo = result.FbrInvoiceNo,
            fbrQrCode = result.FbrQrCode,
            receiptUrl = Url.Action("Receipt", new { orderId = result.OrderId })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HoldBill([FromBody] HoldBillDto dto)
    {
        var cashierId = _tenantProvider.CurrentUserId ?? Guid.NewGuid();
        var result = await _posService.HoldBillAsync(dto, cashierId);
        return Json(new { success = true, heldBill = result });
    }

    [HttpGet]
    public async Task<IActionResult> GetHeldBills(Guid branchId)
    {
        var bills = await _context.HeldBills
            .Where(b => b.BranchId == branchId && b.Status == "Held")
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => new
            {
                id = b.Id,
                holdNo = b.HoldNo,
                note = b.Note,
                createdAt = b.CreatedAtUtc.ToString("HH:mm:ss"),
                itemCount = b.Items.Count,
                estimatedTotal = b.Items.Sum(i => i.Qty * i.UnitPrice)
            })
            .ToListAsync();

        return Json(bills);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResumeHeldBill(Guid id)
    {
        var cart = await _posService.ResumeHeldBillAsync(id);
        if (cart == null) return NotFound(new { message = "Held bill not found" });

        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);

        var responseItems = cart.Items.Select(i => new
        {
            productId = i.ProductId,
            productName = products.TryGetValue(i.ProductId, out var name) ? name : "Product",
            qty = i.Qty,
            price = i.UnitPrice,
            discount = i.DiscountAmount
        });

        return Json(new { success = true, items = responseItems, customerId = cart.CustomerId });
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(Guid orderId)
    {
        var order = await _context.POSOrders
            .Include(o => o.Branch)
            .Include(o => o.Counter)
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Payments).ThenInclude(p => p.PaymentMethod)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return NotFound();

        var business = await _context.Businesses.FindAsync(order.BusinessId);
        ViewBag.Business = business;

        return View(order);
    }
}
