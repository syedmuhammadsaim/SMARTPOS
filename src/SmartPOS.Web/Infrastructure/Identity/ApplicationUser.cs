using Microsoft.AspNetCore.Identity;

namespace SmartPOS.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }
    public string? PinHash { get; set; } // Hash for Manager Override PIN - never stored as plain text
    public Guid? EmployeeId { get; set; }
    public Guid? BusinessId { get; set; }
    public bool IsActive { get; set; } = true;
    public int FailedPinAttempts { get; set; }
    public DateTime? PinLockedUntilUtc { get; set; }
}

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public Guid? BusinessId { get; set; } // Null for system global roles, specific Guid for tenant custom roles
}
