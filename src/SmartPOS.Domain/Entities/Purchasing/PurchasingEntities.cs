using SmartPOS.Domain.Common;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Inventory;

namespace SmartPOS.Domain.Entities.Purchasing;

public class Supplier : BaseTenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNo { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public string Status { get; set; } = "Active";

    public ICollection<SupplierLedger> LedgerEntries { get; set; } = new List<SupplierLedger>();
}

public class PurchaseOrder : BaseTenantEntity
{
    public string OrderNo { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Submitted, Approved, PartiallyReceived, Received, Cancelled
    public string? Notes { get; set; }

    public Supplier? Supplier { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}

public class PurchaseOrderItem : BaseTenantEntity
{
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal OrderedQty { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TotalCost { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }
    public Product? Product { get; set; }
}

public class GoodsReceipt : BaseTenantEntity
{
    public string ReceiptNo { get; set; } = string.Empty;
    public Guid? PurchaseOrderId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
    public string? SupplierInvoiceNo { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal LandedCostTotal { get; set; } // freight, duty
    public string Status { get; set; } = "Received";

    public Supplier? Supplier { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ICollection<GoodsReceiptItem> Items { get; set; } = new List<GoodsReceiptItem>();
}

public class GoodsReceiptItem : BaseTenantEntity
{
    public Guid GoodsReceiptId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal QtyReceived { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LandedCost { get; set; }
    public string? BatchNo { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public GoodsReceipt? GoodsReceipt { get; set; }
    public Product? Product { get; set; }
}

public class SupplierPayment : BaseTenantEntity
{
    public string PaymentNo { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public decimal Amount { get; set; }
    public Guid PaymentMethodId { get; set; }
    public string? InvoiceReference { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public Supplier? Supplier { get; set; }
}

public class SupplierLedger : BaseTenantEntity
{
    public Guid SupplierId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Type { get; set; } = string.Empty; // OpeningBalance, Invoice, Payment, Return, Adjustment
    public Guid? ReferenceId { get; set; }
    public decimal Debit { get; set; } // Paid / Returned
    public decimal Credit { get; set; } // Billed
    public decimal RunningBalance { get; set; }
    public string? Notes { get; set; }

    public Supplier? Supplier { get; set; }
}

public class PurchaseReturn : BaseTenantEntity
{
    public string ReturnNo { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft"; // Draft, Approved, Dispatched
    public string? DebitNoteNo { get; set; }
    public decimal TotalAmount { get; set; }

    public Supplier? Supplier { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ICollection<PurchaseReturnItem> Items { get; set; } = new List<PurchaseReturnItem>();
}

public class PurchaseReturnItem : BaseTenantEntity
{
    public Guid PurchaseReturnId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? BatchId { get; set; }
    public decimal Qty { get; set; }
    public decimal UnitCost { get; set; }

    public PurchaseReturn? PurchaseReturn { get; set; }
    public Product? Product { get; set; }
}
