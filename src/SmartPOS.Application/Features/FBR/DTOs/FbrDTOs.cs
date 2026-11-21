namespace SmartPOS.Application.Features.FBR.DTOs;

public class FbrInvoiceItemPayload
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal SaleValue { get; set; }
    public decimal TaxCharged { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Discount { get; set; }
    public int InvoiceType { get; set; } = 1; // 1 = New, 2 = Refund/Exchange
}

public class FbrInvoicePayload
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int POSID { get; set; }
    public string USIN { get; set; } = string.Empty;
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
    public decimal TotalSaleValue { get; set; }
    public decimal TotalTaxCharged { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalBillAmount { get; set; }
    public int PaymentMode { get; set; } = 1; // 1 = Cash, 2 = Card, 3 = GiftCard, 4 = Mixed
    public int InvoiceType { get; set; } = 1; // 1 = New, 2 = Debit Note, 3 = Credit Note
    public List<FbrInvoiceItemPayload> Items { get; set; } = new();
}

public class FbrSubmissionResult
{
    public bool Success { get; set; }
    public string? FbrInvoiceNo { get; set; }
    public string? QrCodeData { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsQueuedForRetry { get; set; }
}
