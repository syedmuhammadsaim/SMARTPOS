using SmartPOS.Application.Features.Licensing.DTOs;

namespace SmartPOS.Application.Features.Licensing;

public interface ILicenseService
{
    Task<LicenseValidationResult> ValidateLicenseAsync(LicenseValidationRequest request, CancellationToken ct = default);
    Task<LicenseValidationResult> ActivateLicenseAsync(ActivateLicenseRequest request, CancellationToken ct = default);
    Task<SubscriptionStatusDto> GetSubscriptionStatusAsync(Guid businessId, CancellationToken ct = default);
    Task<string> GenerateLicenseKeyAsync(Guid businessId, string plan, int deviceLimit, int validityDays, CancellationToken ct = default);
}
