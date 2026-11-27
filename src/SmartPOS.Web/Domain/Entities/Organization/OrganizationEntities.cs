using SmartPOS.Domain.Common;

namespace SmartPOS.Domain.Entities.Organization;

public class Business : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Contact { get; set; }
    public string? Address { get; set; }
    public string? Logo { get; set; }
    public string CurrencyCode { get; set; } = "PKR";
    public string TimeZone { get; set; } = "Pakistan Standard Time";
    public string Status { get; set; } = "Active"; // Active, Suspended, Inactive
    public string? TaxRegistrationNumber { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? SubscriptionPlanId { get; set; }
    public DateTime? SubscriptionExpiresAtUtc { get; set; }

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    public ICollection<Department> Departments { get; set; } = new List<Department>();
}

public class Branch : BaseTenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Contact { get; set; }
    public string? Hours { get; set; }
    public string Status { get; set; } = "Active";

    // Blueprint v2 additions
    public string? TaxRegNo { get; set; }
    public string? FbrPosId { get; set; }
    public Guid? DefaultPriceListId { get; set; }
    public Guid? ReceiptTemplateId { get; set; }
    public string InvoicePrefix { get; set; } = "INV";

    public Business? Business { get; set; }
    public ICollection<Counter> Counters { get; set; } = new List<Counter>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class Counter : BaseTenantEntity
{
    public Guid BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public string? PrinterName { get; set; }
    public string Status { get; set; } = "Active";

    public Branch? Branch { get; set; }
}

public class Department : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class Designation : BaseTenantEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class Employee : BaseTenantEntity
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? FatherName { get; set; }
    public string? Contact { get; set; }
    public string? Email { get; set; }
    public string? Cnic { get; set; }
    public string? Address { get; set; }
    public Guid BranchId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
    public DateTime JoiningDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Active";

    public Branch? Branch { get; set; }
    public Department? Department { get; set; }
    public Designation? Designation { get; set; }
}
