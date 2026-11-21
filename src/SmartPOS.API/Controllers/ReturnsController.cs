using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Returns;
using SmartPOS.Application.Features.Returns.DTOs;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ReturnsController : ControllerBase
{
    private readonly IReturnService _returnService;

    public ReturnsController(IReturnService returnService)
    {
        _returnService = returnService;
    }

    [HttpGet("orders/{invoiceNo}/eligibility")]
    public async Task<IActionResult> GetOrderForReturn(string invoiceNo, CancellationToken ct)
    {
        var result = await _returnService.GetOrderForReturnAsync(invoiceNo, ct);
        if (result == null)
        {
            return NotFound(new { message = $"Order with invoice '{invoiceNo}' not found." });
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> ProcessReturn([FromBody] CreateSaleReturnRequest request, CancellationToken ct)
    {
        var result = await _returnService.ProcessSaleReturnAsync(request, ct);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
