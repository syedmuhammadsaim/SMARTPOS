namespace SmartPOS.Application.Features.Feedback.DTOs;

public record CreateFeedbackRequest(
    Guid BranchId,
    Guid? POSOrderId,
    string? CustomerName,
    string? CustomerPhone,
    int RatingStars,
    string? Comments
);

public record FeedbackDto(
    Guid Id,
    Guid BranchId,
    Guid? POSOrderId,
    string? CustomerName,
    string? CustomerPhone,
    int RatingStars,
    string? Comments,
    DateTime CreatedAtUtc
);

public record CreateComplaintRequest(
    Guid BranchId,
    Guid? CustomerId,
    string CustomerContact,
    string Category,
    string Description,
    Guid? AssignedToManagerId
);

public record ResolveComplaintRequest(
    Guid ComplaintId,
    Guid ManagerId,
    string ResolutionNotes
);

public record ComplaintDto(
    Guid Id,
    string ComplaintNo,
    Guid BranchId,
    Guid? CustomerId,
    string CustomerContact,
    string Category,
    string Description,
    string Status,
    string? ResolutionNotes,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc
);
