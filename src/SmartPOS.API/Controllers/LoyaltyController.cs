using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Loyalty;
using SmartPOS.Application.Features.Loyalty.DTOs;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService;

    public LoyaltyController(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService;
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetCustomerLoyaltyProfile(Guid customerId, CancellationToken ct)
    {
        try
        {
            var profile = await _loyaltyService.GetCustomerLoyaltyProfileAsync(customerId, ct);
            return Ok(profile);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("earn")]
    public async Task<IActionResult> EarnPoints([FromBody] EarnPointsRequestDto request, CancellationToken ct)
    {
        var pointsEarned = await _loyaltyService.EarnPointsForOrderAsync(request, ct);
        return Ok(new { pointsEarned });
    }

    [HttpPost("redeem")]
    public async Task<IActionResult> RedeemPoints([FromBody] RedeemPointsRequestDto request, CancellationToken ct)
    {
        var result = await _loyaltyService.RedeemPointsAsync(request, ct);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("tiers")]
    public async Task<IActionResult> GetMembershipTiers(CancellationToken ct)
    {
        var tiers = await _loyaltyService.GetMembershipTiersAsync(ct);
        return Ok(tiers);
    }
}
