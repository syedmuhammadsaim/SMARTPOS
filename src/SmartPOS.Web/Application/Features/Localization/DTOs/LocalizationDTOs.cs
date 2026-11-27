namespace SmartPOS.Application.Features.Localization.DTOs;

public record ReceiptTemplateDto(
    Guid Id,
    Guid? BranchId,
    string Name,
    string? HeaderText,
    string? FooterText,
    bool ShowQr,
    string Language, // English, Urdu, UrduEnglish
    int PaperWidthMm,
    string? UrduGreeting,
    string? UrduReturnPolicy
);

public record UpsertReceiptTemplateRequest(
    Guid? BranchId,
    string Name,
    string? HeaderText,
    string? FooterText,
    bool ShowQr,
    string Language,
    int PaperWidthMm,
    string? UrduGreeting,
    string? UrduReturnPolicy
);

public record BilingualReceiptPayload(
    string BusinessName,
    string? BranchName,
    string? NtnNumber,
    string? StrnNumber,
    string InvoiceNo,
    DateTime Date,
    string CashierName,
    string CounterName,
    List<BilingualReceiptItemPayload> Items,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    decimal PaidAmount,
    decimal ChangeAmount,
    string? FbrInvoiceNo,
    string? FbrQrCode,
    string Language,
    string? UrduGreeting,
    string? UrduReturnPolicy
);

public record BilingualReceiptItemPayload(
    string NameEnglish,
    string? NameUrdu,
    decimal Qty,
    decimal UnitPrice,
    decimal LineTotal
);
