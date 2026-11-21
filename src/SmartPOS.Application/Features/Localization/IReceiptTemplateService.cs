using SmartPOS.Application.Features.Localization.DTOs;

namespace SmartPOS.Application.Features.Localization;

public interface IReceiptTemplateService
{
    Task<ReceiptTemplateDto> GetTemplateForBranchAsync(Guid? branchId, CancellationToken ct = default);
    Task<ReceiptTemplateDto> SaveTemplateAsync(UpsertReceiptTemplateRequest request, CancellationToken ct = default);
    Task<string> FormatBilingualReceiptTextAsync(BilingualReceiptPayload payload, CancellationToken ct = default);
}
