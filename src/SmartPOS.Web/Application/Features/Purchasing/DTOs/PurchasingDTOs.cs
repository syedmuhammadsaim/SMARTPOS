using SmartPOS.Domain.Enums;

namespace SmartPOS.Application.Features.Purchasing.DTOs;

public class SupplierDto
{
    public Guid Id { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? NTN { get; set; }
    public string? STRN { get; set; }
    public decimal CurrentBalance { get; set; } // Positive = we owe supplier
    public int CreditDays { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSupplierRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? NTN { get; set; }
    public string? STRN { get; set; }
    public int CreditDays { get; set; } = 30;
}

public class CreatePurchaseOrderRequest
{
    public Guid SupplierId { get; set; }
    public Guid BranchId { get; set; }
    public DateTime ExpectedDeliveryDate { get; set; }
    public string? Notes { get; set; }
    public List<PurchaseOrderItemRequest> Items { get; set; } = new();
}

public class PurchaseOrderItemRequest
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TaxRate { get; set; }
}

public class GoodsReceiptRequest
{
    public Guid? PurchaseOrderId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid WarehouseId { get; set; }
    public string SupplierInvoiceNo { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string? Notes { get; set; }
    public List<GoodsReceiptItemRequest> Items { get; set; } = new();
}

public class GoodsReceiptItemRequest
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal SalePrice { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class GoodsReceiptResponseDto
{
    public Guid Id { get; set; }
    public string GRNNumber { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
}

public class SupplierPaymentRequest
{
    public Guid SupplierId { get; set; }
    public decimal Amount { get; set; }
    public Guid PaymentMethodId { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
}

public class SupplierLedgerEntryDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Debit { get; set; }   // Payment to supplier / Return (decreases liability)
    public decimal Credit { get; set; }  // Goods received (increases liability)
    public decimal Balance { get; set; } // Running balance owed to supplier
}
