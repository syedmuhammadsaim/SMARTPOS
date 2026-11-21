using SmartPOS.Application.Features.Inventory.DTOs;

namespace SmartPOS.Application.Features.Inventory;

public interface IInventoryService
{
    Task<List<StockBalanceDto>> GetStockBalancesAsync(Guid? warehouseId = null, bool lowStockOnly = false, CancellationToken ct = default);
    Task<List<ExpiringStockAlertDto>> GetExpiringBatchesAsync(int daysThreshold = 60, Guid? warehouseId = null, CancellationToken ct = default);
    Task<string> InitiateStockTransferAsync(CreateStockTransferRequest request, string initiatedBy, CancellationToken ct = default);
    Task<bool> ReceiveStockTransferAsync(Guid transferId, string receivedBy, CancellationToken ct = default);
    Task<bool> AdjustStockAsync(StockAdjustmentRequest request, string adjustedBy, CancellationToken ct = default);
}
