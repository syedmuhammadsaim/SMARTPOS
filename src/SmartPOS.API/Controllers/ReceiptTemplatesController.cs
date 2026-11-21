using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Localization;
using SmartPOS.Application.Features.Localization.DTOs;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ReceiptTemplatesController : ControllerBase
{
    private readonly IReceiptTemplateService _templateService;

    public ReceiptTemplatesController(IReceiptTemplateService templateService)
    {
        _templateService = templateService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTemplate([FromQuery] Guid? branchId, CancellationToken ct)
    {
        var template = await _templateService.GetTemplateForBranchAsync(branchId, ct);
        return Ok(template);
    }

    [HttpPost]
    public async Task<IActionResult> SaveTemplate([FromBody] UpsertReceiptTemplateRequest request, CancellationToken ct)
    {
        var template = await _templateService.SaveTemplateAsync(request, ct);
        return Ok(template);
    }

    [HttpPost("preview-format")]
    public async Task<IActionResult> PreviewFormat([FromBody] BilingualReceiptPayload payload, CancellationToken ct)
    {
        var formattedText = await _templateService.FormatBilingualReceiptTextAsync(payload, ct);
        return Ok(new { receiptText = formattedText });
    }
}
