using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Promotions;
using SmartPOS.Application.Features.Promotions.DTOs;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PromotionsController : ControllerBase
{
    private readonly IPromotionGiftCardService _service;

    public PromotionsController(IPromotionGiftCardService service)
    {
        _service = service;
    }

    [HttpPost("coupons/validate")]
    public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponRequest request, CancellationToken ct)
    {
        var result = await _service.ValidateCouponAsync(request, ct);
        return Ok(result);
    }
}
