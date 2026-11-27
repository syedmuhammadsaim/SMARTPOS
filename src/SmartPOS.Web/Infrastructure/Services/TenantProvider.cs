using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SmartPOS.Application.Common.Interfaces;

namespace SmartPOS.Infrastructure.Services;

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _explicitBusinessId;
    private Guid? _explicitBranchId;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? CurrentBusinessId
    {
        get
        {
            if (_explicitBusinessId.HasValue)
                return _explicitBusinessId.Value;

            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            // 1. Primary Source of Truth: Authenticated user's verified BusinessId claim
            var claim = context.User.FindFirst("BusinessId")?.Value;
            if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var bId))
            {
                // If user is SuperAdmin and an explicit tenant switch header is provided for support/audit:
                if (context.User.IsInRole("SuperAdmin"))
                {
                    if (context.Request.Headers.TryGetValue("X-Business-Id", out var adminOverride) &&
                        Guid.TryParse(adminOverride.FirstOrDefault(), out var overrideId))
                    {
                        return overrideId;
                    }
                }
                return bId;
            }

            // 2. For SuperAdmin without a direct BusinessId claim, allow support context header
            if (context.User.IsInRole("SuperAdmin"))
            {
                if (context.Request.Headers.TryGetValue("X-Business-Id", out var adminHeader) &&
                    Guid.TryParse(adminHeader.FirstOrDefault(), out var hId))
                {
                    return hId;
                }
                return null;
            }

            // 3. For public/anonymous API endpoints (e.g. self-service device handshake with credentials)
            if (context.Request.Headers.TryGetValue("X-Business-Id", out var headerVal) &&
                Guid.TryParse(headerVal.FirstOrDefault(), out var anonId))
            {
                return anonId;
            }

            return null;
        }
    }

    public Guid? CurrentBranchId
    {
        get
        {
            if (_explicitBranchId.HasValue)
                return _explicitBranchId.Value;

            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            var claim = context.User.FindFirst("BranchId")?.Value;
            if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var bId))
                return bId;

            if (context.Request.Headers.TryGetValue("X-Branch-Id", out var headerVal) &&
                Guid.TryParse(headerVal.FirstOrDefault(), out var hId))
            {
                return hId;
            }

            return null;
        }
    }

    public Guid? CurrentUserId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            var claim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var uId))
                return uId;

            return null;
        }
    }

    public void SetTenant(Guid businessId, Guid? branchId = null)
    {
        _explicitBusinessId = businessId;
        _explicitBranchId = branchId;
    }
}
