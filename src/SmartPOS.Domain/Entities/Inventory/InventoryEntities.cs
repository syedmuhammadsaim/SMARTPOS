using SmartPOS.Domain.Common;
using SmartPOS.Domain.Enums;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Organization;

namespace SmartPOS.Domain.Entities.Inventory;

public class Warehouse : BaseTenantEntity
{
    public Guid BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string Status { get; set; } = "Active";

    public Branch? Branch { get; set; }
}

public class ProductBatch : BaseTenantEntity
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public string BatchNo { get; set; } = string.Empty;
    public DateTime? ManufactureDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal CostPrice { get; set; }
    public decimal QtyReceived { get; set; }
    public decimal QtyOnHand { get; set; }
    public Guid? GoodsReceiptItemId { get; set; }
    public string Status { get; set; } = "Active"; // Active, NearExpiry, Expired, Recalled

    public Product? Product { get; set; }
    public ProductVariant? Variant { get; set; }
    public Warehouse? Warehouse { get; set; }
}

public class StockBalance : BaseTenantEntity
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? BatchId { get; set; }
    public decimal QtyOnHand { get; set; }

    public Product? Product { get; set; }
    public ProductVariant? Variant { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ProductBatch? Batch { get; set; }
}

public class StockTransaction : BaseTenantEntity
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? BatchId { get; set; }
    public StockTransactionType Type { get; set; }
    public decimal Qty { get; set; } // Positive for stock-in, Negative for stock-out
    public decimal UnitCost { get; set; }
    public string ReferenceType { get; set; } = string.Empty; // POSOrder, GoodsReceipt, Transfer, CountAdjustment, ExpiryWriteOff
    public Guid ReferenceId { get; set; }
    public string? Note { get; set; }

    public Product? Product { get; set; }
    public ProductVariant? Variant { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ProductBatch? Batch { get; set; }
}

public class StockTransfer : BaseTenantEntity
{
    public string TransferNo { get; set; } = string.Empty;
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, PendingApproval, Approved, Dispatched, Received, Rejected
    public string? Reason { get; set; }
    public Guid? DispatchedBy { get; set; }
    public DateTime? DispatchedAtUtc { get; set; }
    public Guid? ReceivedBy { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }

    public Warehouse? FromWarehouse { get; set; }
    public Warehouse? ToWarehouse { get; set; }
    public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
}

public class StockTransferItem : BaseTenantEntity
{
    public Guid StockTransferId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? BatchId { get; set; }
    public decimal Qty { get; set; }
    public decimal ReceivedQty { get; set; }

    public StockTransfer? StockTransfer { get; set; }
    public Product? Product { get; set; }
    public ProductBatch? Batch { get; set; }
}

public class StockCount : BaseTenantEntity
{
    public Guid WarehouseId { get; set; }
    public string CountNo { get; set; } = string.Empty;
    public DateTime CountDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Draft"; // Draft, InProgress, Submitted, Approved, Rejected
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }

    public Warehouse? Warehouse { get; set; }
    public ICollection<StockCountItem> Items { get; set; } = new List<StockCountItem>();
}

public class StockCountItem : BaseTenantEntity
{
    public Guid StockCountId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? BatchId { get; set; }
    public decimal SystemQty { get; set; }
    public decimal CountedQty { get; set; }
    public decimal VarianceQty => CountedQty - SystemQty;
    public decimal UnitCost { get; set; }

    public StockCount? StockCount { get; set; }
    public Product? Product { get; set; }
}
