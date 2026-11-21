using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Feedback;
using SmartPOS.Application.Features.Feedback.DTOs;
using SmartPOS.Domain.Entities.System;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class FeedbackService : IFeedbackService
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public FeedbackService(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<FeedbackDto> RecordFeedbackAsync(CreateFeedbackRequest request, CancellationToken ct = default)
    {
        var feedback = new Feedback
        {
            BusinessId = _tenantProvider.CurrentBusinessId ?? Guid.Empty,
            BranchId = request.BranchId,
            POSOrderId = request.POSOrderId,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            RatingStars = Math.Clamp(request.RatingStars, 1, 5),
            Comments = request.Comments,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync(ct);

        return new FeedbackDto(
            feedback.Id,
            feedback.BranchId,
            feedback.POSOrderId,
            feedback.CustomerName,
            feedback.CustomerPhone,
            feedback.RatingStars,
            feedback.Comments,
            feedback.CreatedAtUtc
        );
    }

    public async Task<ComplaintDto> RegisterComplaintAsync(CreateComplaintRequest request, CancellationToken ct = default)
    {
        var count = await _context.Complaints.CountAsync(ct) + 1;
        var complaintNo = $"CMP-{DateTime.UtcNow:yyyyMMdd}-{count:D4}";

        var complaint = new Complaint
        {
            BusinessId = _tenantProvider.CurrentBusinessId ?? Guid.Empty,
            ComplaintNo = complaintNo,
            BranchId = request.BranchId,
            CustomerId = request.CustomerId,
            CustomerContact = request.CustomerContact,
            Category = request.Category,
            Description = request.Description,
            AssignedToManagerId = request.AssignedToManagerId,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Complaints.Add(complaint);
        await _context.SaveChangesAsync(ct);

        return new ComplaintDto(
            complaint.Id,
            complaint.ComplaintNo,
            complaint.BranchId,
            complaint.CustomerId,
            complaint.CustomerContact,
            complaint.Category,
            complaint.Description,
            complaint.Status,
            complaint.ResolutionNotes,
            complaint.CreatedAtUtc,
            complaint.ResolvedAtUtc
        );
    }

    public async Task<ComplaintDto> ResolveComplaintAsync(ResolveComplaintRequest request, CancellationToken ct = default)
    {
        var complaint = await _context.Complaints.FindAsync(new object[] { request.ComplaintId }, ct);
        if (complaint == null)
            throw new KeyNotFoundException($"Complaint {request.ComplaintId} not found.");

        complaint.Status = "Resolved";
        complaint.ResolutionNotes = request.ResolutionNotes;
        complaint.AssignedToManagerId = request.ManagerId;
        complaint.ResolvedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return new ComplaintDto(
            complaint.Id,
            complaint.ComplaintNo,
            complaint.BranchId,
            complaint.CustomerId,
            complaint.CustomerContact,
            complaint.Category,
            complaint.Description,
            complaint.Status,
            complaint.ResolutionNotes,
            complaint.CreatedAtUtc,
            complaint.ResolvedAtUtc
        );
    }

    public async Task<List<FeedbackDto>> GetRecentFeedbacksAsync(Guid? branchId = null, int limit = 50, CancellationToken ct = default)
    {
        var query = _context.Feedbacks.AsNoTracking().AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(f => f.BranchId == branchId.Value);
        }

        var list = await query
            .OrderByDescending(f => f.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(ct);

        return list.Select(f => new FeedbackDto(
            f.Id,
            f.BranchId,
            f.POSOrderId,
            f.CustomerName,
            f.CustomerPhone,
            f.RatingStars,
            f.Comments,
            f.CreatedAtUtc
        )).ToList();
    }

    public async Task<List<ComplaintDto>> GetComplaintsAsync(string? status = null, Guid? branchId = null, CancellationToken ct = default)
    {
        var query = _context.Complaints.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        if (branchId.HasValue)
        {
            query = query.Where(c => c.BranchId == branchId.Value);
        }

        var list = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(ct);

        return list.Select(c => new ComplaintDto(
            c.Id,
            c.ComplaintNo,
            c.BranchId,
            c.CustomerId,
            c.CustomerContact,
            c.Category,
            c.Description,
            c.Status,
            c.ResolutionNotes,
            c.CreatedAtUtc,
            c.ResolvedAtUtc
        )).ToList();
    }
}
