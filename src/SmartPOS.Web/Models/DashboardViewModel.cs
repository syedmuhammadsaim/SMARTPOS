using SmartPOS.Application.Features.Customers.DTOs;
using SmartPOS.Application.Features.Inventory.DTOs;

namespace SmartPOS.Web.Models;

public class DashboardViewModel
{
    public decimal TodaySales { get; set; }
    public int TodayTransactions { get; set; }
    public decimal TotalCustomerUdhaar { get; set; }
    public decimal TotalStockValuation { get; set; }
    public int LowStockAlertsCount { get; set; }
    public int ExpiringBatchesCount { get; set; }

    public List<StockBalanceDto> LowStockItems { get; set; } = new();
    public List<ExpiringStockAlertDto> ExpiringBatches { get; set; } = new();
    public List<CustomerAgeingReportDto> TopUdhaarCustomers { get; set; } = new();
}
