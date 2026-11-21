using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Reports;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("profit-loss")]
    public async Task<IActionResult> GetProfitLoss(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? branchId,
        CancellationToken ct)
    {
        var from = fromDate ?? DateTime.UtcNow.AddMonths(-1);
        var to = toDate ?? DateTime.UtcNow;
        var result = await _reportService.GetProfitAndLossReportAsync(from, to, branchId, ct);
        return Ok(result);
    }

    [HttpGet("sales-by-category")]
    public async Task<IActionResult> GetSalesByCategory(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? branchId,
        CancellationToken ct)
    {
        var from = fromDate ?? DateTime.UtcNow.AddMonths(-1);
        var to = toDate ?? DateTime.UtcNow;
        var result = await _reportService.GetSalesByCategoryReportAsync(from, to, branchId, ct);
        return Ok(result);
    }

    [HttpGet("sales-by-payment-method")]
    public async Task<IActionResult> GetSalesByPaymentMethod(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? branchId,
        CancellationToken ct)
    {
        var from = fromDate ?? DateTime.UtcNow.AddMonths(-1);
        var to = toDate ?? DateTime.UtcNow;
        var result = await _reportService.GetSalesByPaymentMethodReportAsync(from, to, branchId, ct);
        return Ok(result);
    }

    [HttpGet("cashier-performance")]
    public async Task<IActionResult> GetCashierPerformance(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? branchId,
        CancellationToken ct)
    {
        var from = fromDate ?? DateTime.UtcNow.AddMonths(-1);
        var to = toDate ?? DateTime.UtcNow;
        var result = await _reportService.GetCashierPerformanceReportAsync(from, to, branchId, ct);
        return Ok(result);
    }

    [HttpGet("inventory-valuation")]
    public async Task<IActionResult> GetInventoryValuation([FromQuery] Guid? warehouseId, CancellationToken ct)
    {
        var result = await _reportService.GetInventoryValuationReportAsync(warehouseId, ct);
        return Ok(result);
    }
}
