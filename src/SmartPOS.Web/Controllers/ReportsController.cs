using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Reports;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public ReportsController(
        IReportService reportService,
        SmartPOSDbContext context,
        ITenantProvider tenantProvider)
    {
        _reportService = reportService;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var from = fromDate ?? DateTime.UtcNow.AddMonths(-1);
        var to = toDate ?? DateTime.UtcNow;

        var pnl = await _reportService.GetProfitAndLossReportAsync(from, to, null, ct);
        var categories = await _reportService.GetSalesByCategoryReportAsync(from, to, null, ct);
        var paymentMethods = await _reportService.GetSalesByPaymentMethodReportAsync(from, to, null, ct);
        var cashiers = await _reportService.GetCashierPerformanceReportAsync(from, to, null, ct);
        var valuation = await _reportService.GetInventoryValuationReportAsync(null, ct);

        ViewBag.FromDate = from.ToString("yyyy-MM-dd");
        ViewBag.ToDate = to.ToString("yyyy-MM-dd");
        ViewBag.Pnl = pnl;
        ViewBag.Categories = categories;
        ViewBag.PaymentMethods = paymentMethods;
        ViewBag.Cashiers = cashiers;
        ViewBag.Valuation = valuation;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ExportSalesCsv(DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate ?? DateTime.UtcNow.AddMonths(-1);
        var to = toDate ?? DateTime.UtcNow;

        var orders = await _context.POSOrders
            .Include(o => o.Customer)
            .Include(o => o.Branch)
            .Where(o => o.BusinessDate >= from.Date && o.BusinessDate <= to.Date.AddDays(1).AddTicks(-1))
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("InvoiceNo,Date,Branch,Customer,Subtotal,Discount,Tax,Total,Status");

        foreach (var o in orders)
        {
            var custName = (o.Customer?.Name ?? "Walk-in").Replace(",", " ");
            var branchName = (o.Branch?.Name ?? "Main").Replace(",", " ");
            sb.AppendLine($"{o.InvoiceNo},{o.CreatedAtUtc:yyyy-MM-dd HH:mm},{branchName},{custName},{o.Subtotal},{o.DiscountTotal},{o.TaxTotal},{o.Total},{o.Status}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"Sales_Export_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }
}
