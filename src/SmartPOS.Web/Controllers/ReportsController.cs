using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Reports;

namespace SmartPOS.Web.Controllers;

public class ReportsController : Controller
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
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
}
