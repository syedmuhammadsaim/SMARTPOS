using SmartPOS.Domain.Entities.Identity;
using SmartPOS.Domain.Entities.Organization;
using SmartPOS.Domain.Entities.SaaS;
using SmartPOS.Infrastructure.Identity;

namespace SmartPOS.Web.Models;

public class SuperAdminDashboardViewModel
{
    public int TotalCompanies { get; set; }
    public int ActiveCompanies { get; set; }
    public int SuspendedCompanies { get; set; }
    public int TotalUsers { get; set; }
    public int TotalBranches { get; set; }
    public decimal TotalPlatformSales { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public List<Business> RecentCompanies { get; set; } = new();
    public List<SubscriptionPlan> Plans { get; set; } = new();
    public List<AuditLog> RecentAuditLogs { get; set; } = new();
}

public class CompanyDetailsViewModel
{
    public Business Company { get; set; } = null!;
    public SubscriptionPlan? Plan { get; set; }
    public List<Branch> Branches { get; set; } = new();
    public List<Counter> Counters { get; set; } = new();
    public List<ApplicationUser> Users { get; set; } = new();
    public int TotalSalesCount { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public int TotalProductsCount { get; set; }
}

public class CompanyDashboardViewModel
{
    public string CompanyName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "PKR";
    
    // Revenue & Financial Metrics
    public decimal TodayRevenue { get; set; }
    public int TodayOrders { get; set; }
    public decimal MonthRevenue { get; set; }
    public int MonthOrders { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal NetProfit { get; set; }

    // Inventory & Catalog Counts
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public decimal TotalStockValuation { get; set; }

    // Contacts
    public int TotalCustomers { get; set; }
    public decimal TotalCustomerUdhaar { get; set; }
    public int TotalSuppliers { get; set; }

    // Chart Data
    public List<string> SalesTrendDays { get; set; } = new();
    public List<decimal> SalesTrendAmounts { get; set; } = new();
    public Dictionary<string, decimal> PaymentMethodDistribution { get; set; } = new();

    // Tables
    public List<SmartPOS.Domain.Entities.Sales.POSOrder> RecentOrders { get; set; } = new();
    public List<BranchMetricDto> BranchPerformances { get; set; } = new();
}

public class BranchMetricDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int OrdersCount { get; set; }
    public decimal SalesAmount { get; set; }
}
