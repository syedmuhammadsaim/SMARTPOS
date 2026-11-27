using SmartPOS.Application.Features.Alerts.DTOs;

namespace SmartPOS.Application.Features.Alerts;

public interface ISmartAlertService
{
    Task<AlertSummaryDto> GetAlertSummaryAsync(Guid? branchId = null, CancellationToken ct = default);
    Task<List<LowStockAlertDto>> GetLowStockAlertsAsync(Guid? branchId = null, CancellationToken ct = default);
    Task<List<ExpiryAlertDto>> GetExpiryAlertsAsync(int daysThreshold = 30, Guid? branchId = null, CancellationToken ct = default);
    Task<List<DeadStockAlertDto>> GetDeadStockAlertsAsync(int inactiveDaysThreshold = 90, Guid? branchId = null, CancellationToken ct = default);
}
