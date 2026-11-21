using SmartPOS.Domain.Common;
using SmartPOS.Domain.Entities.Pricing;

namespace SmartPOS.Domain.Entities.Customers;

public class Customer : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Cnic { get; set; }
    public string? TaxRegistrationNo { get; set; }
    public Guid? CustomerGroupId { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsCreditBlocked { get; set; }
    public string Status { get; set; } = "Active";

    public CustomerGroup? CustomerGroup { get; set; }
    public ICollection<CustomerLedger> LedgerEntries { get; set; } = new List<CustomerLedger>();
}

public class CustomerLedger : BaseTenantEntity
{
    public Guid CustomerId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Type { get; set; } = string.Empty; // OpeningBalance, Sale, Payment, Return, Adjustment
    public Guid? ReferenceId { get; set; }
    public decimal Debit { get; set; }  // Invoice / Udhaar Sale
    public decimal Credit { get; set; } // Payment / Return
    public decimal RunningBalance { get; set; }
    public string? Notes { get; set; }

    public Customer? Customer { get; set; }
}

public class CustomerPayment : BaseTenantEntity
{
    public string ReceiptNo { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? SessionId { get; set; } // Linked to cashier counter session
    public decimal Amount { get; set; }
    public Guid PaymentMethodId { get; set; }
    public Guid ReceivedBy { get; set; }
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public Customer? Customer { get; set; }
}
