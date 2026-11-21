using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Alerts;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly ISmartAlertService _alertService;

    public AlertsController(ISmartAlertService alertService)
    {
        _alertService = alertService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] Guid? branchId, CancellationToken ct)
    {
        var summary = await _alertService.GetAlertSummaryAsync(branchId, ct);
        return Ok(summary);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock([FromQuery] Guid? branchId, CancellationToken ct)
    {
        var alerts = await _alertService.GetLowStockAlertsAsync(branchId, ct);
        return Ok(alerts);
    }

    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiring([FromQuery] int days = 30, [FromQuery] Guid? branchId = null, CancellationToken ct = default)
    {
        var alerts = await _alertService.GetExpiryAlertsAsync(days, branchId, ct);
        return Ok(alerts);
    }

    [HttpGet("dead-stock")]
    public async Task<IActionResult> GetDeadStock([FromQuery] int days = 90, [FromQuery] Guid? branchId = null, CancellationToken ct = default)
    {
        var alerts = await _alertService.GetDeadStockAlertsAsync(days, branchId, ct);
        return Ok(alerts);
    }
}
