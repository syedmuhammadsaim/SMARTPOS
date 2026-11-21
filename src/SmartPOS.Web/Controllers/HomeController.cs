using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Features.Customers;
using SmartPOS.Application.Features.Customers.DTOs;
using SmartPOS.Application.Features.Inventory;
using SmartPOS.Application.Features.Inventory.DTOs;
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
        var sevenDaysAgo = today.AddDays(-6);
        var chartDays = new List<string>();
        var chartSales = new List<decimal>();

        for (int i = 0; i < 7; i++)
        {
            chartDays.Add(sevenDaysAgo.AddDays(i).ToString("dd MMM"));
            chartSales.Add(0m);
        }

        var vm = new DashboardViewModel
        {
            TodaySales = 0m,
            TodayTransactions = 0,
            TotalCustomerUdhaar = 0m,
            TotalStockValuation = 0m,
            LowStockAlertsCount = 0,
            ExpiringBatchesCount = 0
        };

        try
        {
            var todayOrders = await _context.POSOrders
                .AsNoTracking()
                .Where(o => o.BusinessDate >= today)
                .ToListAsync(ct);

            vm.TodaySales = todayOrders.Sum(o => o.Total);
            vm.TodayTransactions = todayOrders.Count;

            var lowStock = await _inventoryService.GetStockBalancesAsync(null, lowStockOnly: true, ct);
            var expiringBatches = await _inventoryService.GetExpiringBatchesAsync(60, null, ct);
            var ageingReport = await _customerService.GetCustomerAgeingReportAsync(ct);

            vm.TotalCustomerUdhaar = await _context.Customers.SumAsync(c => (decimal?)c.CurrentBalance, ct) ?? 0m;
            vm.TotalStockValuation = (await _inventoryService.GetStockBalancesAsync(null, false, ct))
                .Sum(s => s.TotalValuation);

            var last7DaysOrders = await _context.POSOrders
                .AsNoTracking()
                .Where(o => o.BusinessDate >= sevenDaysAgo)
                .ToListAsync(ct);

            chartSales.Clear();
            for (int i = 0; i < 7; i++)
            {
                var day = sevenDaysAgo.AddDays(i);
                chartSales.Add(last7DaysOrders.Where(o => o.BusinessDate.Date == day.Date).Sum(o => o.Total));
            }

            vm.LowStockAlertsCount = lowStock.Count;
            vm.ExpiringBatchesCount = expiringBatches.Count;
            vm.LowStockItems = lowStock.Take(10).ToList();
            vm.ExpiringBatches = expiringBatches.Take(10).ToList();
            vm.TopUdhaarCustomers = ageingReport.OrderByDescending(c => c.TotalOutstanding).Take(10).ToList();
        }
        catch (Exception ex)
        {
            // Fallback for offline/unseeded DB
            Console.WriteLine($"[Dashboard Note] Live query fallback: {ex.Message}");
        }

        ViewBag.ChartDays = chartDays;
        ViewBag.ChartSales = chartSales;

        return View(vm);
    }

    public async Task<IActionResult> Inventory(CancellationToken ct)
    {
        try
        {
            var stock = await _inventoryService.GetStockBalancesAsync(null, false, ct);
            return View(stock);
        }
        catch
        {
            return View(new List<StockBalanceDto>());
        }
    }

    public async Task<IActionResult> ExpiringBatches(CancellationToken ct)
    {
        try
        {
            var batches = await _inventoryService.GetExpiringBatchesAsync(90, null, ct);
            return View(batches);
        }
        catch
        {
            return View(new List<ExpiringStockAlertDto>());
        }
    }

    public async Task<IActionResult> Customers(CancellationToken ct)
    {
        try
        {
            var customers = await _customerService.GetAllCustomersAsync(null, ct);
            return View(customers);
        }
        catch
        {
            return View(new List<CustomerDto>());
        }
    }

    public async Task<IActionResult> CustomerAgeing(CancellationToken ct)
    {
        try
        {
            var ageing = await _customerService.GetCustomerAgeingReportAsync(ct);
            return View(ageing);
        }
        catch
        {
            return View(new List<CustomerAgeingReportDto>());
        }
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
