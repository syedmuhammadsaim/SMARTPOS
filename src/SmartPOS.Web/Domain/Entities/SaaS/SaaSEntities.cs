using SmartPOS.Domain.Common;

namespace SmartPOS.Domain.Entities.SaaS;

public class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty; // e.g. Free Trial, Starter, Professional, Enterprise
    public string Code { get; set; } = string.Empty; // e.g. TRIAL, STARTER, PRO, ENTERPRISE
    public string? Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public string Currency { get; set; } = "PKR";
    public int MaxBranches { get; set; } = 1;
    public int MaxCounters { get; set; } = 2;
    public int MaxUsers { get; set; } = 5;
    public int MaxProducts { get; set; } = 500;
    public bool HasAdvancedReports { get; set; } = true;
    public bool HasFbrIntegration { get; set; } = false;
    public bool HasBarcodePrinting { get; set; } = true;
    public bool HasMultiCounter { get; set; } = true;
    public bool HasApiAccess { get; set; } = false;
    public int TrialDays { get; set; } = 14;
    public int SortOrder { get; set; } = 1;
    public bool IsPopular { get; set; } = false;
}

public class SubscriptionPayment : BaseEntity
{
    public Guid BusinessId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PKR";
    public string PaymentMethod { get; set; } = "Manual"; // JazzCash, EasyPaisa, BankTransfer, Card, Manual
    public string? PaymentReference { get; set; }
    public string? GatewayTransactionId { get; set; }
    public string Status { get; set; } = "Completed"; // Pending, Completed, Failed, Refunded
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}

public class SubscriptionInvoice : BaseEntity
{
    public Guid BusinessId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "PKR";
    public string Status { get; set; } = "Paid"; // Draft, Unpaid, Paid, Cancelled
    public DateTime IssueDateUtc { get; set; } = DateTime.UtcNow;
    public DateTime DueDateUtc { get; set; } = DateTime.UtcNow.AddDays(7);
    public DateTime? PaidDateUtc { get; set; }
}

public class PlatformSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = "General"; // General, Security, Billing, Notification
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; } = false;
}

public class SupportTicket : BaseEntity
{
    public Guid BusinessId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = "General"; // Technical, Billing, FeatureRequest, Account
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Urgent
    public string Status { get; set; } = "Open"; // Open, InProgress, Resolved, Closed
    public Guid CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public Guid? AssignedToUserId { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }

    public ICollection<SupportTicketMessage> Messages { get; set; } = new List<SupportTicketMessage>();
}

public class SupportTicketMessage : BaseEntity
{
    public Guid SupportTicketId { get; set; }
    public Guid SenderUserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsAdminReply { get; set; } = false;

    public SupportTicket? SupportTicket { get; set; }
}

public class Notification : BaseTenantEntity
{
    public Guid? UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info"; // Info, Success, Warning, Danger
    public string? TargetUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAtUtc { get; set; }
}

public class OfflineSyncRecord : BaseTenantEntity
{
    public Guid BranchId { get; set; }
    public Guid CounterId { get; set; }
    public string SyncBatchId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty; // POSOrder, Customer, Payment
    public string Operation { get; set; } = "Insert"; // Insert, Update, Delete
    public string ClientRecordId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string Status { get; set; } = "Applied"; // Pending, Applied, Conflict, Failed
    public string? ErrorMessage { get; set; }
    public DateTime SyncedAtUtc { get; set; } = DateTime.UtcNow;
}
