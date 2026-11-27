namespace SmartPOS.Application.Features.Alerts.DTOs;

public class LowStockAlertDto
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinStock { get; set; }
    public decimal ReorderSuggestedQty { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
}

public class ExpiryAlertDto
{
    public Guid BatchId { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BatchNo { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
    public decimal QtyOnHand { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string UrgencyLevel { get; set; } = string.Empty; // Expired, Critical30, Warning60, Notice90
}

public class DeadStockAlertDto
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TiedUpCapital { get; set; }
    public DateTime? LastSaleDate { get; set; }
    public int DaysWithoutSale { get; set; }
}

public class AlertSummaryDto
{
    public int TotalLowStockCount { get; set; }
    public int TotalExpiredCount { get; set; }
    public int TotalExpiringSoonCount { get; set; }
    public int TotalDeadStockCount { get; set; }
    public decimal TotalTiedUpCapitalInDeadStock { get; set; }

    public List<LowStockAlertDto> LowStockAlerts { get; set; } = new();
    public List<ExpiryAlertDto> ExpiryAlerts { get; set; } = new();
    public List<DeadStockAlertDto> DeadStockAlerts { get; set; } = new();
}
