using SmartPOS.Application.Features.Feedback.DTOs;

namespace SmartPOS.Application.Features.Feedback;

public interface IFeedbackService
{
    Task<FeedbackDto> RecordFeedbackAsync(CreateFeedbackRequest request, CancellationToken ct = default);
    Task<ComplaintDto> RegisterComplaintAsync(CreateComplaintRequest request, CancellationToken ct = default);
    Task<ComplaintDto> ResolveComplaintAsync(ResolveComplaintRequest request, CancellationToken ct = default);
    Task<List<FeedbackDto>> GetRecentFeedbacksAsync(Guid? branchId = null, int limit = 50, CancellationToken ct = default);
    Task<List<ComplaintDto>> GetComplaintsAsync(string? status = null, Guid? branchId = null, CancellationToken ct = default);
}
