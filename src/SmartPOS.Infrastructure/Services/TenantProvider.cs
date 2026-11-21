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

            // Check claims
            var claim = context.User.FindFirst("BusinessId")?.Value;
            if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var bId))
                return bId;

            // Check header
            if (context.Request.Headers.TryGetValue("X-Business-Id", out var headerVal) &&
                Guid.TryParse(headerVal.FirstOrDefault(), out var hId))
            {
                return hId;
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
