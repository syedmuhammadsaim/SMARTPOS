using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartPOS.Application.Features.Auth.DTOs;
using SmartPOS.Application.Features.Auth.Interfaces;
using SmartPOS.Domain.Entities.Identity;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Infrastructure.Identity;

namespace SmartPOS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly SmartPOSDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        SmartPOSDbContext context,
        IConfiguration configuration,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _configuration = configuration;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null || !user.IsActive)
        {
            return new LoginResponse(false, null, null, null, null, null, null, null, null, "Invalid username or account is disabled.");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            return new LoginResponse(false, null, null, null, null, null, null, null, null, "Invalid credentials.");
        }

        var roles = await _userManager.GetRolesAsync(user);

        // Fetch permissions for roles
        var roleEntities = await _roleManager.Roles.Where(r => roles.Contains(r.Name!)).ToListAsync();
        var roleIds = roleEntities.Select(r => r.Id).ToList();

        var permissionCodes = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Include(rp => rp.Permission)
            .Where(rp => rp.Permission != null)
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .ToListAsync();

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"] ?? "SmartPOS_Enterprise_Default_Super_Secret_Key_2026_!");
        var expires = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "1440"));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new("FullName", user.FullName ?? string.Empty),
        };

        if (user.BusinessId.HasValue)
        {
            claims.Add(new Claim("BusinessId", user.BusinessId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var perm in permissionCodes)
        {
            claims.Add(new Claim("Permission", perm));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _configuration["JwtSettings:Issuer"] ?? "SmartPOS.API",
            Audience = _configuration["JwtSettings:Audience"] ?? "SmartPOS.Clients",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return new LoginResponse(
            Success: true,
            Token: tokenString,
            ExpiresAtUtc: expires,
            UserId: user.Id,
            FullName: user.FullName,
            Username: user.UserName,
            BusinessId: user.BusinessId,
            Roles: roles,
            Permissions: permissionCodes,
            Message: "Login successful."
        );
    }

    public async Task<ManagerPinVerifyResponse> VerifyManagerPinAsync(ManagerPinVerifyRequest request, Guid requestingUserId)
    {
        // Approver must be a different user than the requester
        if (request.ManagerId == requestingUserId)
        {
            return new ManagerPinVerifyResponse(false, null, "Manager override approver cannot be the same user requesting the override.");
        }

        var manager = await _userManager.FindByIdAsync(request.ManagerId.ToString());
        if (manager == null || !manager.IsActive)
        {
            return new ManagerPinVerifyResponse(false, null, "Manager not found or inactive.");
        }

        // Check if locked
        if (manager.PinLockedUntilUtc.HasValue && manager.PinLockedUntilUtc.Value > DateTime.UtcNow)
        {
            var remaining = (int)(manager.PinLockedUntilUtc.Value - DateTime.UtcNow).TotalMinutes;
            return new ManagerPinVerifyResponse(false, null, $"PIN is locked due to multiple incorrect attempts. Try again in {remaining + 1} minutes.");
        }

        if (string.IsNullOrEmpty(manager.PinHash))
        {
            return new ManagerPinVerifyResponse(false, null, "Manager has not configured an override PIN.");
        }

        // Verify PIN hash
        var verification = _passwordHasher.VerifyHashedPassword(manager, manager.PinHash, request.Pin);
        if (verification == PasswordVerificationResult.Failed)
        {
            manager.FailedPinAttempts++;
            if (manager.FailedPinAttempts >= 5)
            {
                manager.PinLockedUntilUtc = DateTime.UtcNow.AddMinutes(15);
                manager.FailedPinAttempts = 0;
            }
            await _userManager.UpdateAsync(manager);

            return new ManagerPinVerifyResponse(false, null, "Invalid PIN.");
        }

        // Reset failed attempts on success
        manager.FailedPinAttempts = 0;
        manager.PinLockedUntilUtc = null;
        await _userManager.UpdateAsync(manager);

        // Record Manager Override log
        var managerOverride = new ManagerOverride
        {
            BusinessId = manager.BusinessId ?? Guid.Empty,
            Action = request.Action,
            RequestedBy = requestingUserId,
            ApprovedBy = manager.Id,
            POSOrderId = request.POSOrderId,
            Reason = request.Reason,
            OriginalValue = request.OriginalValue,
            OverriddenValue = request.OverriddenValue
        };

        _context.ManagerOverrides.Add(managerOverride);
        await _context.SaveChangesAsync();

        return new ManagerPinVerifyResponse(true, managerOverride.Id, "Override approved successfully.");
    }

    public async Task<bool> SetUserPinAsync(Guid userId, string pin)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        user.PinHash = _passwordHasher.HashPassword(user, pin);
        user.FailedPinAttempts = 0;
        user.PinLockedUntilUtc = null;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }
}
