using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Features.Reports;
using SmartPOS.Application.Features.Reports.DTOs;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly SmartPOSDbContext _context;

    public ReportService(SmartPOSDbContext context)
    {
        _context = context;
    }

    public async Task<ProfitAndLossReportDto> GetProfitAndLossReportAsync(DateTime fromDate, DateTime toDate, Guid? branchId = null, CancellationToken ct = default)
    {
        var ordersQuery = _context.POSOrders
            .AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Where(o => o.Status == OrderStatus.Completed && o.BusinessDate >= fromDate.Date && o.BusinessDate <= toDate.Date);

        if (branchId.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.BranchId == branchId.Value);
        }

        var orders = await ordersQuery.ToListAsync(ct);

        decimal grossSales = 0m;
        decimal totalDiscounts = 0m;
        decimal cogs = 0m;

        foreach (var order in orders)
        {
            totalDiscounts += order.DiscountTotal;
            foreach (var item in order.Items)
            {
                grossSales += item.Qty * item.PriceSnapshot;
                var unitCost = item.Product?.PurchasePrice ?? 0m;
                cogs += item.Qty * unitCost;
            }
        }

        // Returns
        var returnsQuery = _context.SaleReturns
            .AsNoTracking()
            .Where(r => r.CreatedAtUtc >= fromDate.Date && r.CreatedAtUtc <= toDate.Date.AddDays(1));

        if (branchId.HasValue)
        {
            returnsQuery = returnsQuery.Where(r => r.BranchId == branchId.Value);
        }

        var totalReturns = await returnsQuery.SumAsync(r => (decimal?)r.RefundAmount, ct) ?? 0m;

        var netSales = grossSales - totalDiscounts - totalReturns;
        var grossProfit = netSales - cogs;
        var marginPercent = grossSales > 0 ? Math.Round((grossProfit / grossSales) * 100m, 2) : 0m;

        // Operating Expenses
        var expensesQuery = _context.Expenses
            .AsNoTracking()
            .Where(e => (e.Status == "Approved" || e.Status == "Paid") && e.CreatedAtUtc >= fromDate.Date && e.CreatedAtUtc <= toDate.Date.AddDays(1));

        if (branchId.HasValue)
        {
            expensesQuery = expensesQuery.Where(e => e.BranchId == branchId.Value);
        }

        var totalExpenses = await expensesQuery.SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;
        var netProfit = grossProfit - totalExpenses;

        return new ProfitAndLossReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            BranchId = branchId,
            GrossSalesRevenue = grossSales,
            TotalDiscounts = totalDiscounts,
            TotalReturnsRefunded = totalReturns,
            NetSalesRevenue = netSales,
            CostOfGoodsSold = cogs,
            GrossProfit = grossProfit,
            GrossProfitMarginPercent = marginPercent,
            TotalOperatingExpenses = totalExpenses,
            NetProfit = netProfit,
            TotalInvoicesCount = orders.Count
        };
    }

    public async Task<List<SalesByCategoryReportDto>> GetSalesByCategoryReportAsync(DateTime fromDate, DateTime toDate, Guid? branchId = null, CancellationToken ct = default)
    {
        var itemsQuery = _context.POSOrderItems
            .AsNoTracking()
            .Include(i => i.POSOrder)
            .Include(i => i.Product)
                .ThenInclude(p => p!.Category)
            .Where(i => i.POSOrder != null && i.POSOrder.Status == OrderStatus.Completed
                        && i.POSOrder.BusinessDate >= fromDate.Date && i.POSOrder.BusinessDate <= toDate.Date);

        if (branchId.HasValue)
        {
            itemsQuery = itemsQuery.Where(i => i.POSOrder!.BranchId == branchId.Value);
        }

        var items = await itemsQuery.ToListAsync(ct);
        var totalRev = items.Sum(i => i.LineTotal);

        var grouped = items
            .GroupBy(i => i.Product?.Category?.Name ?? "Uncategorized")
            .Select(g =>
            {
                var categoryRevenue = g.Sum(x => x.LineTotal);
                var categoryQty = g.Sum(x => x.Qty);
                var categoryCogs = g.Sum(x => x.Qty * (x.Product?.PurchasePrice ?? 0m));
                var profit = categoryRevenue - categoryCogs;
                var share = totalRev > 0 ? Math.Round((categoryRevenue / totalRev) * 100m, 2) : 0m;

                return new SalesByCategoryReportDto
                {
                    CategoryName = g.Key,
                    TotalRevenue = categoryRevenue,
                    TotalQuantitySold = categoryQty,
                    CostOfGoodsSold = categoryCogs,
                    GrossProfit = profit,
                    RevenueSharePercent = share
                };
            })
            .OrderByDescending(c => c.TotalRevenue)
            .ToList();

        return grouped;
    }

    public async Task<List<SalesByPaymentMethodReportDto>> GetSalesByPaymentMethodReportAsync(DateTime fromDate, DateTime toDate, Guid? branchId = null, CancellationToken ct = default)
    {
        var paymentsQuery = _context.Payments
            .AsNoTracking()
            .Include(p => p.POSOrder)
            .Include(p => p.PaymentMethod)
            .Where(p => p.POSOrder != null && p.POSOrder.Status == OrderStatus.Completed
                        && p.POSOrder.BusinessDate >= fromDate.Date && p.POSOrder.BusinessDate <= toDate.Date);

        if (branchId.HasValue)
        {
            paymentsQuery = paymentsQuery.Where(p => p.POSOrder!.BranchId == branchId.Value);
        }

        var payments = await paymentsQuery.ToListAsync(ct);
        var totalSum = payments.Sum(p => p.Amount);

        return payments
            .GroupBy(p => p.PaymentMethod?.Name ?? "Cash")
            .Select(g =>
            {
                var amt = g.Sum(x => x.Amount);
                var pct = totalSum > 0 ? Math.Round((amt / totalSum) * 100m, 2) : 0m;
                return new SalesByPaymentMethodReportDto
                {
                    PaymentMethod = g.Key,
                    TotalAmount = amt,
                    TransactionCount = g.Count(),
                    PercentageOfTotal = pct
                };
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();
    }

    public async Task<List<CashierPerformanceReportDto>> GetCashierPerformanceReportAsync(DateTime fromDate, DateTime toDate, Guid? branchId = null, CancellationToken ct = default)
    {
        var ordersQuery = _context.POSOrders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed && o.BusinessDate >= fromDate.Date && o.BusinessDate <= toDate.Date);

        if (branchId.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.BranchId == branchId.Value);
        }

        var orders = await ordersQuery.ToListAsync(ct);

        var cashierIds = orders.Select(o => o.CashierId).Distinct().ToList();
        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => cashierIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => $"{e.FirstName} {e.LastName}".Trim(), ct);

        return orders
            .GroupBy(o => o.CashierId)
            .Select(g =>
            {
                var cashierName = employees.TryGetValue(g.Key, out var name) ? name : $"Cashier #{g.Key.ToString()[..6]}";
                var invoiceCount = g.Count();
                var totalSales = g.Sum(x => x.Total);
                var totalDiscounts = g.Sum(x => x.DiscountTotal);
                var avgBasket = invoiceCount > 0 ? Math.Round(totalSales / invoiceCount, 2) : 0m;

                return new CashierPerformanceReportDto
                {
                    CashierId = g.Key,
                    CashierName = cashierName,
                    TotalInvoices = invoiceCount,
                    TotalSalesAmount = totalSales,
                    TotalDiscountsGiven = totalDiscounts,
                    TotalReturnsProcessed = 0m,
                    AverageBasketValue = avgBasket
                };
            })
            .OrderByDescending(c => c.TotalSalesAmount)
            .ToList();
    }

    public async Task<InventoryValuationReportDto> GetInventoryValuationReportAsync(Guid? warehouseId = null, CancellationToken ct = default)
    {
        var query = _context.StockBalances
            .AsNoTracking()
            .Include(s => s.Product)
            .Where(s => s.QtyOnHand > 0);

        if (warehouseId.HasValue)
        {
            query = query.Where(s => s.WarehouseId == warehouseId.Value);
        }

        var balances = await query.ToListAsync(ct);

        var distinctSkus = balances.Select(b => b.ProductId).Distinct().Count();
        var totalQty = balances.Sum(b => b.QtyOnHand);

        decimal totalCost = 0m;
        decimal totalRetail = 0m;

        foreach (var bal in balances)
        {
            if (bal.Product != null)
            {
                totalCost += bal.QtyOnHand * bal.Product.PurchasePrice;
                totalRetail += bal.QtyOnHand * bal.Product.SalePrice;
            }
        }

        return new InventoryValuationReportDto
        {
            TotalDistinctSkus = distinctSkus,
            TotalQuantityOnHand = totalQty,
            TotalCostValuation = totalCost,
            TotalRetailValuation = totalRetail,
            PotentialGrossProfit = totalRetail - totalCost
        };
    }
}
