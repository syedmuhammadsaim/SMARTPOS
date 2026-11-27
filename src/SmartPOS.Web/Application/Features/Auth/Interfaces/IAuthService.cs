using SmartPOS.Application.Features.Auth.DTOs;

namespace SmartPOS.Application.Features.Auth.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<ManagerPinVerifyResponse> VerifyManagerPinAsync(ManagerPinVerifyRequest request, Guid requestingUserId);
    Task<bool> SetUserPinAsync(Guid userId, string pin);
}
