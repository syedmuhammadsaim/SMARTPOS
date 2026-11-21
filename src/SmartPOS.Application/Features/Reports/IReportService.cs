using SmartPOS.Application.Features.Reports.DTOs;

namespace SmartPOS.Application.Features.Reports;

public interface IReportService
{
    Task<ProfitAndLossReportDto> GetProfitAndLossReportAsync(DateTime fromDate, DateTime toDate, Guid? branchId = null, CancellationToken ct = default);
    Task<List<SalesByCategoryReportDto>> GetSalesByCategoryReportAsync(DateTime fromDate, DateTime toDate, Guid? branchId = null, CancellationToken ct = default);
    Task<List<SalesByPaymentMethodReportDto>> GetSalesByPaymentMethodReportAsync(DateTime fromDate, DateTime toDate, Guid? branchId = null, CancellationToken ct = default);
    Task<List<CashierPerformanceReportDto>> GetCashierPerformanceReportAsync(DateTime fromDate, DateTime toDate, Guid? branchId = null, CancellationToken ct = default);
    Task<InventoryValuationReportDto> GetInventoryValuationReportAsync(Guid? warehouseId = null, CancellationToken ct = default);
}
