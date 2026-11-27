using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Web.Models;

namespace SmartPOS.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public DashboardController(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("SuperAdmin"))
        {
            return RedirectToAction("Index", "SuperAdmin");
        }

        var businessId = _tenantProvider.CurrentBusinessId;
        if (!businessId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var business = await _context.Businesses.FindAsync(businessId.Value);
        var currency = business?.CurrencyCode ?? "PKR";

        var today = DateTime.UtcNow.Date;
        var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var sevenDaysAgo = today.AddDays(-6);

        // Sales Orders
        var todayOrders = await _context.POSOrders
            .Where(o => o.BusinessDate >= today && o.Status != SmartPOS.Domain.Enums.OrderStatus.Cancelled)
            .ToListAsync();

        var monthOrders = await _context.POSOrders
            .Where(o => o.BusinessDate >= startOfMonth && o.Status != SmartPOS.Domain.Enums.OrderStatus.Cancelled)
            .ToListAsync();

        var todayRevenue = todayOrders.Sum(o => o.Total);
        var todayOrdersCount = todayOrders.Count;
        var monthRevenue = monthOrders.Sum(o => o.Total);
        var monthOrdersCount = monthOrders.Count;

        // Total products, low stock & out-of-stock
        var products = await _context.Products.ToListAsync();
        var stockBalances = await _context.StockBalances.ToListAsync();
        var stockByProduct = stockBalances
            .GroupBy(s => s.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.QtyOnHand));

        var lowStockCount = 0;
        var outOfStockCount = 0;
        decimal totalValuation = 0m;

        foreach (var p in products)
        {
            var qty = stockByProduct.TryGetValue(p.Id, out var q) ? q : 0m;
            if (qty <= 0) outOfStockCount++;
            else if (qty <= p.MinStock) lowStockCount++;

            totalValuation += qty * p.PurchasePrice;
        }

        // Expenses
        var totalExpenses = await _context.Expenses
            .Where(e => e.CreatedAtUtc >= startOfMonth && e.Status == "Approved")
            .SumAsync(e => (decimal?)e.Amount) ?? 0m;

        // Gross Margin & Net Profit calculation
        var orderItems = await _context.POSOrderItems
            .Include(i => i.Product)
            .Where(i => i.CreatedAtUtc >= startOfMonth)
            .ToListAsync();

        var totalCogs = orderItems.Sum(i => i.Qty * (i.Product?.PurchasePrice ?? 0m));
        var grossProfit = monthRevenue - totalCogs;
        var netProfit = grossProfit - totalExpenses;

        // Customers & Udhaar
        var totalCustomers = await _context.Customers.CountAsync();
        var totalCustomerUdhaar = await _context.Customers.SumAsync(c => (decimal?)c.CurrentBalance) ?? 0m;
        var totalSuppliers = await _context.Suppliers.CountAsync();

        // 7-Day Sales Trend
        var last7DaysOrders = await _context.POSOrders
            .Where(o => o.BusinessDate >= sevenDaysAgo && o.Status != SmartPOS.Domain.Enums.OrderStatus.Cancelled)
            .ToListAsync();

        var trendDays = new List<string>();
        var trendAmounts = new List<decimal>();

        for (int i = 0; i < 7; i++)
        {
            var day = sevenDaysAgo.AddDays(i);
            trendDays.Add(day.ToString("dd MMM"));
            trendAmounts.Add(last7DaysOrders.Where(o => o.BusinessDate.Date == day.Date).Sum(o => o.Total));
        }

        // Payment Method Analytics
        var payments = await _context.Payments
            .Include(p => p.PaymentMethod)
            .Where(p => p.CreatedAtUtc >= startOfMonth)
            .ToListAsync();

        var paymentDist = payments
            .GroupBy(p => p.PaymentMethod?.Name ?? "Cash")
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        if (!paymentDist.Any())
        {
            paymentDist["Cash"] = todayRevenue;
        }

        // Recent Orders
        var recentOrders = await _context.POSOrders
            .Include(o => o.Customer)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(6)
            .ToListAsync();

        // Branch Performance
        var branches = await _context.Branches.ToListAsync();
        var branchMetrics = new List<BranchMetricDto>();
        foreach (var b in branches)
        {
            var bOrders = monthOrders.Where(o => o.BranchId == b.Id).ToList();
            branchMetrics.Add(new BranchMetricDto
            {
                BranchId = b.Id,
                BranchName = b.Name,
                OrdersCount = bOrders.Count,
                SalesAmount = bOrders.Sum(o => o.Total)
            });
        }

        var vm = new CompanyDashboardViewModel
        {
            CompanyName = business?.Name ?? "Company Workspace",
            CurrencyCode = currency,
            TodayRevenue = todayRevenue,
            TodayOrders = todayOrdersCount,
            MonthRevenue = monthRevenue,
            MonthOrders = monthOrdersCount,
            TotalExpenses = totalExpenses,
            GrossProfit = grossProfit,
            NetProfit = netProfit,
            TotalProducts = products.Count,
            LowStockProducts = lowStockCount,
            OutOfStockProducts = outOfStockCount,
            TotalStockValuation = totalValuation,
            TotalCustomers = totalCustomers,
            TotalCustomerUdhaar = totalCustomerUdhaar,
            TotalSuppliers = totalSuppliers,
            SalesTrendDays = trendDays,
            SalesTrendAmounts = trendAmounts,
            PaymentMethodDistribution = paymentDist,
            RecentOrders = recentOrders,
            BranchPerformances = branchMetrics
        };

        return View(vm);
    }
}
