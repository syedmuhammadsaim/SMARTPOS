using SmartPOS.Domain.Common;
using SmartPOS.Domain.Entities.Organization;

namespace SmartPOS.Domain.Entities.Finance;

public class ExpenseCategory : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty; // Rent, Utilities, Refreshment, Maintenance, Salaries
    public string? Description { get; set; }
}

public class Expense : BaseTenantEntity
{
    public string ExpenseNo { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public Guid? SessionId { get; set; } // Linked to counter session if paid from till
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash"; // Cash, BankTransfer
    public string Payee { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ReceiptAttachmentUrl { get; set; }
    public string Status { get; set; } = "Approved"; // Draft, Submitted, Approved, Rejected, Paid
    public Guid? ApprovedBy { get; set; }

    public Branch? Branch { get; set; }
    public ExpenseCategory? Category { get; set; }
}

public class CashTransaction : BaseTenantEntity
{
    public Guid BranchId { get; set; }
    public Guid? SessionId { get; set; }
    public string Type { get; set; } = string.Empty; // CashIn, CashOut, BankDeposit, TillFloat, Expense
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Reference { get; set; }
    public Guid UserId { get; set; }

    public Branch? Branch { get; set; }
}

public class DailyClosing : BaseTenantEntity
{
    public Guid BranchId { get; set; }
    public DateTime BusinessDate { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public decimal TotalWallet { get; set; }
    public decimal TotalUdhaar { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalVariance { get; set; }
    public Guid ClosedBy { get; set; }
    public string Status { get; set; } = "Closed"; // Closed, Audited

    public Branch? Branch { get; set; }
}
