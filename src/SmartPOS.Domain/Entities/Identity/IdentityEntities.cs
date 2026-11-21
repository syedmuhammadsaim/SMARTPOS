using SmartPOS.Domain.Common;

namespace SmartPOS.Domain.Entities.Identity;

public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public Permission? Permission { get; set; }
}

public class UserBranchScope : BaseTenantEntity
{
    public Guid UserId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? CounterId { get; set; }
}

public class ManagerOverride : BaseTenantEntity
{
    public string Action { get; set; } = string.Empty; // pos.void, pos.discount, pos.price.change, pos.refund
    public Guid RequestedBy { get; set; }
    public Guid ApprovedBy { get; set; }
    public Guid? POSOrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal? OriginalValue { get; set; }
    public decimal? OverriddenValue { get; set; }
}

public class AuditLog : BaseTenantEntity
{
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
}
