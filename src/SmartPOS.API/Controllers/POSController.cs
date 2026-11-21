using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Communication;
using SmartPOS.Application.Features.POS.DTOs;
using SmartPOS.Application.Features.POS.Interfaces;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class POSController : ControllerBase
{
    private readonly IPOSService _posService;
    private readonly IWhatsAppService _whatsAppService;

    public POSController(IPOSService posService, IWhatsAppService whatsAppService)
    {
        _posService = posService;
        _whatsAppService = whatsAppService;
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateSaleOrderDto dto)
    {
        var cashierId = GetCurrentUserId();
        var response = await _posService.CreateSaleOrderAsync(dto, cashierId);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPost("hold")]
    public async Task<IActionResult> HoldBill([FromBody] HoldBillDto dto)
    {
        var cashierId = GetCurrentUserId();
        var result = await _posService.HoldBillAsync(dto, cashierId);
        return Ok(result);
    }

    [HttpGet("resume/{id:guid}")]
    public async Task<IActionResult> ResumeBill(Guid id)
    {
        var result = await _posService.ResumeHeldBillAsync(id);
        if (result == null)
        {
            return NotFound("Held bill not found or already resumed.");
        }
        return Ok(result);
    }

    [HttpGet("sessions/{sessionId:guid}/x-report")]
    public async Task<IActionResult> GetXReport(Guid sessionId)
    {
        var operatorId = GetCurrentUserId();
        var report = await _posService.GenerateXReportAsync(sessionId, operatorId);
        return Ok(report);
    }

    [HttpPost("sessions/{sessionId:guid}/z-report")]
    public async Task<IActionResult> CloseSessionZReport(Guid sessionId, [FromBody] decimal actualCashCounted)
    {
        var operatorId = GetCurrentUserId();
        var report = await _posService.CloseSessionAndGenerateZReportAsync(sessionId, actualCashCounted, operatorId);
        return Ok(report);
    }

    [HttpPost("orders/{id:guid}/send-whatsapp")]
    public async Task<IActionResult> SendWhatsAppReceipt(Guid id, [FromBody] WhatsAppReceiptRequest request, CancellationToken ct)
    {
        var result = await _whatsAppService.SendDigitalReceiptAsync(request, ct);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var id))
        {
            return id;
        }
        return Guid.Empty;
    }
}
