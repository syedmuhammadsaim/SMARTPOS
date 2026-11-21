namespace SmartPOS.Application.Features.Customers.DTOs;

public class CustomerDto
{
    public Guid Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? CNIC { get; set; }
    public string? NTN { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; } // Positive = customer owes money (Udhaar)
    public int CreditDaysLimit { get; set; }
    public bool IsCreditAllowed { get; set; }
    public Guid? CustomerGroupId { get; set; }
    public string? CustomerGroupName { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? CNIC { get; set; }
    public string? NTN { get; set; }
    public decimal CreditLimit { get; set; }
    public int CreditDaysLimit { get; set; } = 30;
    public bool IsCreditAllowed { get; set; } = true;
    public Guid? CustomerGroupId { get; set; }
}

public class CustomerPaymentRequest
{
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public Guid PaymentMethodId { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
    public Guid? CounterSessionId { get; set; }
    public Guid? BranchId { get; set; }
}

public class CustomerLedgerEntryDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Debit { get; set; }   // Sales (increase debt)
    public decimal Credit { get; set; }  // Payments / Returns (decrease debt)
    public decimal Balance { get; set; } // Running balance
}

public class CustomerAgeingReportDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal Current_0_30 { get; set; }
    public decimal Overdue_31_60 { get; set; }
    public decimal Overdue_61_90 { get; set; }
    public decimal Overdue_90Plus { get; set; }
}
