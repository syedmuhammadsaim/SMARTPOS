namespace SmartPOS.Application.Features.Reports.DTOs;

public class ProfitAndLossReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Guid? BranchId { get; set; }
    public decimal GrossSalesRevenue { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal TotalReturnsRefunded { get; set; }
    public decimal NetSalesRevenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossProfitMarginPercent { get; set; }
    public decimal TotalOperatingExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public int TotalInvoicesCount { get; set; }
}

public class SalesByCategoryReportDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public decimal TotalQuantitySold { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal RevenueSharePercent { get; set; }
}

public class SalesByPaymentMethodReportDto
{
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public decimal PercentageOfTotal { get; set; }
}

public class CashierPerformanceReportDto
{
    public Guid CashierId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public int TotalInvoices { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal TotalDiscountsGiven { get; set; }
    public decimal TotalReturnsProcessed { get; set; }
    public decimal AverageBasketValue { get; set; }
}

public class InventoryValuationReportDto
{
    public int TotalDistinctSkus { get; set; }
    public decimal TotalQuantityOnHand { get; set; }
    public decimal TotalCostValuation { get; set; }
    public decimal TotalRetailValuation { get; set; }
    public decimal PotentialGrossProfit { get; set; }
}
