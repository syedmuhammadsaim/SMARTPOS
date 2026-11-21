using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Features.Customers;
using SmartPOS.Application.Features.Inventory;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Web.Models;

namespace SmartPOS.Web.Controllers;

public class HomeController : Controller
{
    private readonly SmartPOSDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly ICustomerService _customerService;

    public HomeController(SmartPOSDbContext context, IInventoryService inventoryService, ICustomerService customerService)
    {
        _context = context;
        _inventoryService = inventoryService;
        _customerService = customerService;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;

        var todayOrders = await _context.POSOrders
            .AsNoTracking()
            .Where(o => o.BusinessDate >= today)
            .ToListAsync(ct);

        var todaySales = todayOrders.Sum(o => o.Total);
        var todayCount = todayOrders.Count;

        var lowStock = await _inventoryService.GetStockBalancesAsync(null, lowStockOnly: true, ct);
        var expiringBatches = await _inventoryService.GetExpiringBatchesAsync(60, null, ct);
        var ageingReport = await _customerService.GetCustomerAgeingReportAsync(ct);

        var totalUdhaar = await _context.Customers.SumAsync(c => (decimal?)c.CurrentBalance, ct) ?? 0m;
        var totalStockValuation = (await _inventoryService.GetStockBalancesAsync(null, false, ct))
            .Sum(s => s.TotalValuation);

        // 7-Day sales trend for Chart.js
        var sevenDaysAgo = today.AddDays(-6);
        var last7DaysOrders = await _context.POSOrders
            .AsNoTracking()
            .Where(o => o.BusinessDate >= sevenDaysAgo)
            .ToListAsync(ct);

        var chartDays = new List<string>();
        var chartSales = new List<decimal>();

        for (int i = 0; i < 7; i++)
        {
            var day = sevenDaysAgo.AddDays(i);
            chartDays.Add(day.ToString("dd MMM"));
            chartSales.Add(last7DaysOrders.Where(o => o.BusinessDate.Date == day.Date).Sum(o => o.Total));
        }

        ViewBag.ChartDays = chartDays;
        ViewBag.ChartSales = chartSales;

        var vm = new DashboardViewModel
        {
            TodaySales = todaySales,
            TodayTransactions = todayCount,
            TotalCustomerUdhaar = totalUdhaar,
            TotalStockValuation = totalStockValuation,
            LowStockAlertsCount = lowStock.Count,
            ExpiringBatchesCount = expiringBatches.Count,
            LowStockItems = lowStock.Take(10).ToList(),
            ExpiringBatches = expiringBatches.Take(10).ToList(),
            TopUdhaarCustomers = ageingReport.OrderByDescending(c => c.TotalOutstanding).Take(10).ToList()
        };

        return View(vm);
    }

    public async Task<IActionResult> Inventory(CancellationToken ct)
    {
        var stock = await _inventoryService.GetStockBalancesAsync(null, false, ct);
        return View(stock);
    }

    public async Task<IActionResult> ExpiringBatches(CancellationToken ct)
    {
        var batches = await _inventoryService.GetExpiringBatchesAsync(90, null, ct);
        return View(batches);
    }

    public async Task<IActionResult> Customers(CancellationToken ct)
    {
        var customers = await _customerService.GetAllCustomersAsync(null, ct);
        return View(customers);
    }

    public async Task<IActionResult> CustomerAgeing(CancellationToken ct)
    {
        var ageing = await _customerService.GetCustomerAgeingReportAsync(ct);
        return View(ageing);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
