namespace SmartPOS.Domain.Common;

public interface ITenantScoped
{
    Guid BusinessId { get; set; }
}

public abstract class BaseTenantEntity : BaseEntity, ITenantScoped
{
    public Guid BusinessId { get; set; }
}
