using SmartPOS.Domain.Common;

namespace SmartPOS.Domain.Entities.System;

public class FbrSetting : BaseTenantEntity
{
    public Guid BranchId { get; set; }
    public string PosId { get; set; } = string.Empty;
    public string TokenEncrypted { get; set; } = string.Empty;
    public string Environment { get; set; } = "Sandbox"; // Sandbox, Production
    public bool IsEnabled { get; set; }
    public string? ProvincialAuthority { get; set; } // PRA, SRB, KPRA, BRA
}

public class FbrInvoiceLog : BaseTenantEntity
{
    public Guid POSOrderId { get; set; }
    public string? FbrInvoiceNo { get; set; }
    public string? QrCodeData { get; set; }
    public string RequestJson { get; set; } = string.Empty;
    public string? ResponseJson { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Success, Failed, Retrying
    public int RetryCount { get; set; }
    public DateTime? SentAtUtc { get; set; }
}

public class DocumentSequence : BaseTenantEntity
{
    public Guid? BranchId { get; set; }
    public Guid? CounterId { get; set; }
    public string DocType { get; set; } = string.Empty; // POSInvoice, GoodsReceipt, PurchaseOrder, StockTransfer, Expense, CashReceipt
    public string Prefix { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public long LastNumber { get; set; }
    public string ResetRule { get; set; } = "Yearly"; // Yearly, Monthly, Never
}

public class IdempotencyKey : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string? ResponseJson { get; set; }
    public int StatusCode { get; set; } = 200;
}

public class ImportJob : BaseTenantEntity
{
    public string Type { get; set; } = string.Empty; // Products, OpeningStock, Customers, Suppliers
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int ErrorRows { get; set; }
    public string? ErrorReportPath { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Failed
}

public class ReceiptTemplate : BaseTenantEntity
{
    public Guid? BranchId { get; set; }
    public string Name { get; set; } = "Default Thermal 80mm";
    public string? HeaderText { get; set; }
    public string? FooterText { get; set; }
    public bool ShowQr { get; set; } = true;
    public string Language { get; set; } = "UrduEnglish"; // English, Urdu, UrduEnglish
    public int PaperWidthMm { get; set; } = 80; // 80 or 58
}

public class LabelTemplate : BaseTenantEntity
{
    public string Name { get; set; } = "Standard 50x25mm";
    public int WidthMm { get; set; } = 50;
    public int HeightMm { get; set; } = 25;
    public string LayoutJson { get; set; } = string.Empty;
}

public class DeviceRegistration : BaseTenantEntity
{
    public Guid CounterId { get; set; }
    public string DeviceId { get; set; } = string.Empty; // Machine Guid / MAC
    public string? PrinterName { get; set; }
    public string ScannerSuffix { get; set; } = "CR"; // CR, CRLF, TAB
    public string? DrawerCommand { get; set; } = "27,112,0,25,250"; // Standard ESC/POS drawer kick
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
}

public class AppVersion : BaseEntity
{
    public string Platform { get; set; } = "DesktopWPF";
    public string Version { get; set; } = "1.0.0";
    public string DownloadUrl { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public string? ReleaseNotes { get; set; }
}

public class Feedback : BaseTenantEntity
{
    public Guid BranchId { get; set; }
    public Guid? POSOrderId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public int RatingStars { get; set; } // 1-5
    public string? Comments { get; set; }
}

public class Complaint : BaseTenantEntity
{
    public string ComplaintNo { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public string CustomerContact { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? AssignedToManagerId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Resolved, Closed
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}

public class Subscription : BaseEntity
{
    public Guid BusinessId { get; set; }
    public string Plan { get; set; } = "Enterprise"; // Starter, Professional, Enterprise
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddYears(1);
    public int MaxBranches { get; set; } = 10;
    public int MaxCounters { get; set; } = 50;
    public string Status { get; set; } = "Active";
}

public class LicenseKey : BaseEntity
{
    public Guid BusinessId { get; set; }
    public string Key { get; set; } = string.Empty;
    public int DeviceLimit { get; set; } = 10;
    public DateTime ExpiresAt { get; set; }
}
