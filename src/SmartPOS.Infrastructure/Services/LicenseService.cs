using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Licensing;
using SmartPOS.Application.Features.Licensing.DTOs;
using SmartPOS.Domain.Entities.System;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class LicenseService : ILikeLicenseServiceInternal
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public LicenseService(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<LicenseValidationResult> ValidateLicenseAsync(LicenseValidationRequest request, CancellationToken ct = default)
    {
        var license = await _context.LicenseKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.BusinessId == request.BusinessId && l.Key == request.LicenseKey, ct);

        if (license == null)
        {
            return new LicenseValidationResult(
                IsValid: false,
                Status: "InvalidKey",
                Plan: "None",
                ExpiresAt: DateTime.MinValue,
                DaysRemaining: 0,
                DeviceLimit: 0,
                ActiveDevicesCount: 0,
                Message: "Invalid or unrecognized license key for this business."
            );
        }

        var subscription = await _context.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BusinessId == request.BusinessId, ct);

        var plan = subscription?.Plan ?? "Enterprise";
        var now = DateTime.UtcNow;
        var daysRemaining = (int)Math.Ceiling((license.ExpiresAt - now).TotalDays);

        // Check active device count
        var registeredDevices = await _context.DeviceRegistrations
            .AsNoTracking()
            .Where(d => d.BusinessId == request.BusinessId)
            .Select(d => d.DeviceId)
            .Distinct()
            .ToListAsync(ct);

        var isDeviceRegistered = registeredDevices.Contains(request.DeviceFingerprint);

        if (!isDeviceRegistered && registeredDevices.Count >= license.DeviceLimit)
        {
            return new LicenseValidationResult(
                IsValid: false,
                Status: "DeviceLimitExceeded",
                Plan: plan,
                ExpiresAt: license.ExpiresAt,
                DaysRemaining: Math.Max(0, daysRemaining),
                DeviceLimit: license.DeviceLimit,
                ActiveDevicesCount: registeredDevices.Count,
                Message: $"License device limit of {license.DeviceLimit} reached. Deauthorize an old terminal or upgrade plan."
            );
        }

        if (daysRemaining < -14)
        {
            return new LicenseValidationResult(
                IsValid: false,
                Status: "Expired",
                Plan: plan,
                ExpiresAt: license.ExpiresAt,
                DaysRemaining: 0,
                DeviceLimit: license.DeviceLimit,
                ActiveDevicesCount: registeredDevices.Count,
                Message: $"License expired on {license.ExpiresAt:yyyy-MM-dd}. 14-day grace period has elapsed. Please renew."
            );
        }

        if (daysRemaining < 0)
        {
            var graceRemaining = 14 + daysRemaining;
            return new LicenseValidationResult(
                IsValid: true,
                Status: "GracePeriod",
                Plan: plan,
                ExpiresAt: license.ExpiresAt,
                DaysRemaining: 0,
                DeviceLimit: license.DeviceLimit,
                ActiveDevicesCount: registeredDevices.Count,
                Message: $"License expired! Operating in Emergency Grace Period ({graceRemaining} days remaining). Renew immediately."
            );
        }

        return new LicenseValidationResult(
            IsValid: true,
            Status: "Active",
            Plan: plan,
            ExpiresAt: license.ExpiresAt,
            DaysRemaining: daysRemaining,
            DeviceLimit: license.DeviceLimit,
            ActiveDevicesCount: registeredDevices.Count,
            Message: "License is active and valid."
        );
    }

    public async Task<LicenseValidationResult> ActivateLicenseAsync(ActivateLicenseRequest request, CancellationToken ct = default)
    {
        var valReq = new LicenseValidationRequest(
            request.BusinessId,
            request.Key,
            request.DeviceFingerprint,
            request.MachineName
        );

        var valResult = await ValidateLicenseAsync(valReq, ct);
        if (!valResult.IsValid)
        {
            return valResult;
        }

        // Register or update device registration
        var device = await _context.DeviceRegistrations
            .FirstOrDefaultAsync(d => d.BusinessId == request.BusinessId && d.DeviceId == request.DeviceFingerprint, ct);

        if (device == null)
        {
            device = new DeviceRegistration
            {
                BusinessId = request.BusinessId,
                CounterId = request.CounterId,
                DeviceId = request.DeviceFingerprint,
                LastSeenUtc = DateTime.UtcNow
            };
            _context.DeviceRegistrations.Add(device);
        }
        else
        {
            device.CounterId = request.CounterId;
            device.LastSeenUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
        return await ValidateLicenseAsync(valReq, ct);
    }

    public async Task<SubscriptionStatusDto> GetSubscriptionStatusAsync(Guid businessId, CancellationToken ct = default)
    {
        var sub = await _context.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BusinessId == businessId, ct);

        var branchCount = await _context.Branches.CountAsync(b => b.BusinessId == businessId, ct);
        var counterCount = await _context.Counters.CountAsync(c => c.BusinessId == businessId, ct);

        var now = DateTime.UtcNow;
        var endDate = sub?.EndDate ?? now.AddYears(1);
        var inGrace = now > endDate && now <= endDate.AddDays(14);
        var graceDaysRemaining = inGrace ? (int)Math.Max(0, (endDate.AddDays(14) - now).TotalDays) : 0;

        return new SubscriptionStatusDto(
            BusinessId: businessId,
            Plan: sub?.Plan ?? "Enterprise",
            Status: inGrace ? "GracePeriod" : (now > endDate ? "Expired" : (sub?.Status ?? "Active")),
            StartDate: sub?.StartDate ?? now,
            EndDate: endDate,
            MaxBranches: sub?.MaxBranches ?? 10,
            CurrentBranchesCount: branchCount,
            MaxCounters: sub?.MaxCounters ?? 50,
            CurrentCountersCount: counterCount,
            IsInGracePeriod: inGrace,
            GracePeriodDaysRemaining: graceDaysRemaining
        );
    }

    public async Task<string> GenerateLicenseKeyAsync(Guid businessId, string plan, int deviceLimit, int validityDays, CancellationToken ct = default)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(12);
        var rawHex = Convert.ToHexString(randomBytes);
        var key = $"SP-{plan.ToUpper()}-{rawHex[..4]}-{rawHex.Substring(4, 4)}-{rawHex.Substring(8, 4)}";

        var license = new LicenseKey
        {
            BusinessId = businessId,
            Key = key,
            DeviceLimit = deviceLimit,
            ExpiresAt = DateTime.UtcNow.AddDays(validityDays)
        };

        _context.LicenseKeys.Add(license);
        await _context.SaveChangesAsync(ct);

        return key;
    }
}

public interface ILikeLicenseServiceInternal : ILicenseService { }
