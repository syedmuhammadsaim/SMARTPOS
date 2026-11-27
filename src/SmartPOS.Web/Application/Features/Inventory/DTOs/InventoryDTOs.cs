namespace SmartPOS.Application.Features.Inventory.DTOs;

public class StockBalanceDto
{
    public Guid ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal AvailableQuantity => QuantityOnHand - QuantityReserved;
    public decimal AverageCostPrice { get; set; }
    public decimal TotalValuation => QuantityOnHand * AverageCostPrice;
    public decimal AlertQuantity { get; set; }
    public bool IsLowStock => QuantityOnHand <= AlertQuantity;
}

public class ExpiringStockAlertDto
{
    public Guid ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int DaysRemaining => (ExpiryDate - DateTime.UtcNow).Days;
    public decimal Quantity { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
}

public class CreateStockTransferRequest
{
    public Guid SourceWarehouseId { get; set; }
    public Guid DestinationWarehouseId { get; set; }
    public string? Notes { get; set; }
    public List<StockTransferItemRequest> Items { get; set; } = new();
}

public class StockTransferItemRequest
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? BatchId { get; set; }
    public decimal Quantity { get; set; }
}

public class StockTransferDto
{
    public Guid Id { get; set; }
    public string TransferNumber { get; set; } = string.Empty;
    public string SourceWarehouseName { get; set; } = string.Empty;
    public string DestinationWarehouseName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalQuantity { get; set; }
}

public class StockAdjustmentRequest
{
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? BatchId { get; set; }
    public decimal PhysicalQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
}
