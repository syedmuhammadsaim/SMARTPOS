using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Licensing;
using SmartPOS.Application.Features.Licensing.DTOs;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LicensingController : ControllerBase
{
    private readonly ILicenseService _licenseService;

    public LicensingController(ILicenseService licenseService)
    {
        _licenseService = licenseService;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateLicense([FromBody] LicenseValidationRequest request, CancellationToken ct)
    {
        var result = await _licenseService.ValidateLicenseAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("activate")]
    public async Task<IActionResult> ActivateLicense([FromBody] ActivateLicenseRequest request, CancellationToken ct)
    {
        var result = await _licenseService.ActivateLicenseAsync(request, ct);
        if (!result.IsValid)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("subscription/{businessId:guid}")]
    public async Task<IActionResult> GetSubscriptionStatus(Guid businessId, CancellationToken ct)
    {
        var status = await _licenseService.GetSubscriptionStatusAsync(businessId, ct);
        return Ok(status);
    }

    [HttpPost("generate-key")]
    public async Task<IActionResult> GenerateKey(
        [FromQuery] Guid businessId,
        [FromQuery] string plan = "Enterprise",
        [FromQuery] int deviceLimit = 10,
        [FromQuery] int validityDays = 365,
        CancellationToken ct = default)
    {
        var key = await _licenseService.GenerateLicenseKeyAsync(businessId, plan, deviceLimit, validityDays, ct);
        return Ok(new { licenseKey = key });
    }
}
