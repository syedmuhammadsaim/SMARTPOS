using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Promotions;
using SmartPOS.Application.Features.Promotions.DTOs;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class GiftCardsController : ControllerBase
{
    private readonly IPromotionGiftCardService _service;

    public GiftCardsController(IPromotionGiftCardService service)
    {
        _service = service;
    }

    [HttpPost("issue")]
    public async Task<IActionResult> IssueGiftCard([FromBody] IssueGiftCardRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _service.IssueGiftCardAsync(request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{cardNo}/balance")]
    public async Task<IActionResult> GetBalance(string cardNo, CancellationToken ct)
    {
        var result = await _service.GetGiftCardBalanceAsync(cardNo, ct);
        if (result == null)
        {
            return NotFound(new { message = $"Gift Card '{cardNo}' not found." });
        }
        return Ok(result);
    }

    [HttpPost("redeem")]
    public async Task<IActionResult> Redeem([FromBody] RedeemGiftCardRequest request, CancellationToken ct)
    {
        var result = await _service.RedeemGiftCardAsync(request, ct);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
