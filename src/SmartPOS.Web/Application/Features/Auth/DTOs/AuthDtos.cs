namespace SmartPOS.Application.Features.Auth.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    bool Success,
    string? Token,
    DateTime? ExpiresAtUtc,
    Guid? UserId,
    string? FullName,
    string? Username,
    Guid? BusinessId,
    IList<string>? Roles,
    IList<string>? Permissions,
    string? Message);

public record ManagerPinVerifyRequest(
    Guid ManagerId,
    string Pin,
    string Action,
    Guid? POSOrderId,
    string Reason,
    decimal? OriginalValue = null,
    decimal? OverriddenValue = null);

public record ManagerPinVerifyResponse(
    bool IsAuthorized,
    Guid? OverrideId,
    string? Message);
