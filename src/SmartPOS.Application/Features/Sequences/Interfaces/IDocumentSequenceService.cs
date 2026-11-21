namespace SmartPOS.Application.Features.Sequences.Interfaces;

public interface IDocumentSequenceService
{
    Task<string> GetNextNumberAsync(Guid businessId, Guid? branchId, Guid? counterId, string docType, string prefix);
}
