using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Features.Sequences.Interfaces;
using SmartPOS.Domain.Entities.System;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class DocumentSequenceService : IDocumentSequenceService
{
    private readonly SmartPOSDbContext _context;

    public DocumentSequenceService(SmartPOSDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetNextNumberAsync(Guid businessId, Guid? branchId, Guid? counterId, string docType, string prefix)
    {
        var currentYear = DateTime.UtcNow.Year;

        var sequence = await _context.DocumentSequences
            .FirstOrDefaultAsync(s => 
                s.BusinessId == businessId && 
                s.DocType == docType && 
                s.BranchId == branchId &&
                s.FiscalYear == currentYear);

        if (sequence == null)
        {
            sequence = new DocumentSequence
            {
                BusinessId = businessId,
                BranchId = branchId,
                CounterId = counterId,
                DocType = docType,
                Prefix = prefix,
                FiscalYear = currentYear,
                LastNumber = 1
            };
            _context.DocumentSequences.Add(sequence);
        }
        else
        {
            sequence.LastNumber++;
        }

        await _context.SaveChangesAsync();

        return $"{prefix}-{currentYear}-{sequence.LastNumber:D6}";
    }
}
