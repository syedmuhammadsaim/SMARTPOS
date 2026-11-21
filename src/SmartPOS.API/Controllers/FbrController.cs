using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.FBR;
using SmartPOS.Application.Features.FBR.DTOs;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/fbr")]
[Authorize]
public class FbrController : ControllerBase
{
    private readonly IFbrService _fbrService;

    public FbrController(IFbrService fbrService)
    {
        _fbrService = fbrService;
    }

    [HttpPost("invoices/{orderId:guid}/submit")]
    public async Task<ActionResult<FbrSubmissionResult>> SubmitInvoice(Guid orderId, CancellationToken ct)
    {
        var result = await _fbrService.SubmitInvoiceAsync(orderId, ct);
        return Ok(result);
    }

    [HttpPost("retry-queue")]
    public async Task<ActionResult> ProcessRetryQueue(CancellationToken ct)
    {
        var count = await _fbrService.ProcessRetryQueueAsync(ct);
        return Ok(new { processedCount = count, message = $"{count} pending FBR invoices fiscalized successfully." });
    }
}
