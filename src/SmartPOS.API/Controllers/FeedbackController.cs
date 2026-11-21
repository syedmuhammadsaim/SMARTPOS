using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Feedback;
using SmartPOS.Application.Features.Feedback.DTOs;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [HttpPost]
    public async Task<IActionResult> RecordFeedback([FromBody] CreateFeedbackRequest request, CancellationToken ct)
    {
        var result = await _feedbackService.RecordFeedbackAsync(request, ct);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetRecentFeedbacks([FromQuery] Guid? branchId, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var list = await _feedbackService.GetRecentFeedbacksAsync(branchId, limit, ct);
        return Ok(list);
    }

    [HttpPost("complaints")]
    public async Task<IActionResult> RegisterComplaint([FromBody] CreateComplaintRequest request, CancellationToken ct)
    {
        var result = await _feedbackService.RegisterComplaintAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("complaints/resolve")]
    public async Task<IActionResult> ResolveComplaint([FromBody] ResolveComplaintRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _feedbackService.ResolveComplaintAsync(request, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("complaints")]
    public async Task<IActionResult> GetComplaints([FromQuery] string? status, [FromQuery] Guid? branchId, CancellationToken ct)
    {
        var list = await _feedbackService.GetComplaintsAsync(status, branchId, ct);
        return Ok(list);
    }
}
