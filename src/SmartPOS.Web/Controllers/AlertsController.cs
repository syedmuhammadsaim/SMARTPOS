using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Alerts;

namespace SmartPOS.Web.Controllers;

public class AlertsController : Controller
{
    private readonly ISmartAlertService _alertService;

    public AlertsController(ISmartAlertService alertService)
    {
        _alertService = alertService;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var summary = await _alertService.GetAlertSummaryAsync(null, ct);
        return View(summary);
    }
}
