namespace SmartPOS.Application.Common.Interfaces;

public interface ITenantProvider
{
    Guid? CurrentBusinessId { get; }
    Guid? CurrentBranchId { get; }
    Guid? CurrentUserId { get; }
    void SetTenant(Guid businessId, Guid? branchId = null);
}
