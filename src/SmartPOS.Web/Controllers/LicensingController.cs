using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Features.Licensing;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Web.Controllers;

public class LicensingController : Controller
{
    private readonly ILicenseService _licenseService;
    private readonly SmartPOSDbContext _context;

    public LicensingController(ILicenseService licenseService, SmartPOSDbContext context)
    {
        _licenseService = licenseService;
        _context = context;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var business = await _context.Businesses.FirstOrDefaultAsync(ct);
        var businessId = business?.Id ?? Guid.Parse("11111111-1111-1111-1111-111111111111");

        var subStatus = await _licenseService.GetSubscriptionStatusAsync(businessId, ct);
        var licenses = await _context.LicenseKeys
            .AsNoTracking()
            .Where(l => l.BusinessId == businessId)
            .OrderByDescending(l => l.ExpiresAt)
            .ToListAsync(ct);

        var devices = await _context.DeviceRegistrations
            .AsNoTracking()
            .Where(d => d.BusinessId == businessId)
            .OrderByDescending(d => d.LastSeenUtc)
            .ToListAsync(ct);

        ViewBag.Subscription = subStatus;
        ViewBag.Licenses = licenses;
        ViewBag.Devices = devices;
        ViewBag.BusinessName = business?.Name ?? "SMARTPOS Enterprise";

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> GenerateKey(string plan, int deviceLimit, int validityDays, CancellationToken ct)
    {
        var business = await _context.Businesses.FirstOrDefaultAsync(ct);
        var businessId = business?.Id ?? Guid.Parse("11111111-1111-1111-1111-111111111111");

        await _licenseService.GenerateLicenseKeyAsync(businessId, plan, deviceLimit, validityDays, ct);
        TempData["SuccessMessage"] = "New machine license key generated and registered successfully!";

        return RedirectToAction(nameof(Index));
    }
}
