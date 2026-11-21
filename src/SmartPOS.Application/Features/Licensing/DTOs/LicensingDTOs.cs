namespace SmartPOS.Application.Features.Licensing.DTOs;

public record LicenseValidationRequest(
    Guid BusinessId,
    string LicenseKey,
    string DeviceFingerprint,
    string MachineName
);

public record LicenseValidationResult(
    bool IsValid,
    string Status, // Active, GracePeriod, Expired, DeviceLimitExceeded, InvalidKey
    string Plan,
    DateTime ExpiresAt,
    int DaysRemaining,
    int DeviceLimit,
    int ActiveDevicesCount,
    string Message
);

public record ActivateLicenseRequest(
    Guid BusinessId,
    string Key,
    string DeviceFingerprint,
    string MachineName,
    Guid CounterId
);

public record SubscriptionStatusDto(
    Guid BusinessId,
    string Plan,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    int MaxBranches,
    int CurrentBranchesCount,
    int MaxCounters,
    int CurrentCountersCount,
    bool IsInGracePeriod,
    int GracePeriodDaysRemaining
);
