using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Features.Alerts;
using SmartPOS.Application.Features.Alerts.DTOs;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class SmartAlertService : ISmartAlertService
{
    private readonly SmartPOSDbContext _context;

    public SmartAlertService(SmartPOSDbContext context)
    {
        _context = context;
    }

    public async Task<AlertSummaryDto> GetAlertSummaryAsync(Guid? branchId = null, CancellationToken ct = default)
    {
        var lowStock = await GetLowStockAlertsAsync(branchId, ct);
        var expiry = await GetExpiryAlertsAsync(90, branchId, ct);
        var deadStock = await GetDeadStockAlertsAsync(90, branchId, ct);

        var now = DateTime.UtcNow;

        return new AlertSummaryDto
        {
            TotalLowStockCount = lowStock.Count,
            TotalExpiredCount = expiry.Count(x => x.ExpiryDate <= now),
            TotalExpiringSoonCount = expiry.Count(x => x.ExpiryDate > now),
            TotalDeadStockCount = deadStock.Count,
            TotalTiedUpCapitalInDeadStock = deadStock.Sum(x => x.TiedUpCapital),
            LowStockAlerts = lowStock.Take(50).ToList(),
            ExpiryAlerts = expiry.Take(50).ToList(),
            DeadStockAlerts = deadStock.Take(50).ToList()
        };
    }

    public async Task<List<LowStockAlertDto>> GetLowStockAlertsAsync(Guid? branchId = null, CancellationToken ct = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Status == "Active");

        var products = await query.ToListAsync(ct);

        var stockBalancesQuery = _context.StockBalances
            .AsNoTracking()
            .Include(s => s.Warehouse)
            .AsQueryable();

        if (branchId.HasValue)
        {
            stockBalancesQuery = stockBalancesQuery.Where(s => s.Warehouse != null && s.Warehouse.BranchId == branchId.Value);
        }

        var stockBalances = await stockBalancesQuery.ToListAsync(ct);
        var stockByProduct = stockBalances
            .GroupBy(s => s.ProductId)
            .ToDictionary(g => g.Key, g => new
            {
                TotalStock = g.Sum(x => x.QtyOnHand),
                WarehouseName = g.FirstOrDefault()?.Warehouse?.Name ?? "Primary Warehouse"
            });

        var alerts = new List<LowStockAlertDto>();

        foreach (var prod in products)
        {
            var currentStock = stockByProduct.TryGetValue(prod.Id, out var val) ? val.TotalStock : 0m;
            if (currentStock <= prod.MinStock)
            {
                var reorderQty = Math.Max(0m, (prod.MinStock * 2m) - currentStock);
                alerts.Add(new LowStockAlertDto
                {
                    ProductId = prod.Id,
                    Sku = prod.Sku,
                    Name = prod.Name,
                    CategoryName = prod.Category?.Name,
                    CurrentStock = currentStock,
                    MinStock = prod.MinStock,
                    ReorderSuggestedQty = reorderQty > 0 ? reorderQty : prod.MinStock,
                    WarehouseName = val?.WarehouseName ?? "Primary Warehouse"
                });
            }
        }

        return alerts.OrderBy(a => a.CurrentStock).ToList();
    }

    public async Task<List<ExpiryAlertDto>> GetExpiryAlertsAsync(int daysThreshold = 30, Guid? branchId = null, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var thresholdDate = now.AddDays(daysThreshold);

        var query = _context.ProductBatches
            .AsNoTracking()
            .Include(b => b.Product)
            .Include(b => b.Warehouse)
            .Where(b => b.QtyOnHand > 0 && b.ExpiryDate <= thresholdDate);

        if (branchId.HasValue)
        {
            query = query.Where(b => b.Warehouse != null && b.Warehouse.BranchId == branchId.Value);
        }

        var batches = await query
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(ct);

        return batches.Select(b =>
        {
            var daysRemaining = (int)Math.Ceiling((b.ExpiryDate - now).TotalDays);
            string urgency;
            if (daysRemaining <= 0)
                urgency = "Expired";
            else if (daysRemaining <= 30)
                urgency = "Critical30";
            else if (daysRemaining <= 60)
                urgency = "Warning60";
            else
                urgency = "Notice90";

            return new ExpiryAlertDto
            {
                BatchId = b.Id,
                ProductId = b.ProductId,
                Sku = b.Product?.Sku ?? string.Empty,
                Name = b.Product?.Name ?? string.Empty,
                BatchNo = b.BatchNo,
                ExpiryDate = b.ExpiryDate,
                DaysUntilExpiry = daysRemaining,
                QtyOnHand = b.QtyOnHand,
                WarehouseName = b.Warehouse?.Name ?? "Main",
                UrgencyLevel = urgency
            };
        }).ToList();
    }

    public async Task<List<DeadStockAlertDto>> GetDeadStockAlertsAsync(int inactiveDaysThreshold = 90, Guid? branchId = null, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-inactiveDaysThreshold);

        var stockBalancesQuery = _context.StockBalances
            .AsNoTracking()
            .Include(s => s.Product)
            .Include(s => s.Warehouse)
            .Where(s => s.QtyOnHand > 0);

        if (branchId.HasValue)
        {
            stockBalancesQuery = stockBalancesQuery.Where(s => s.Warehouse != null && s.Warehouse.BranchId == branchId.Value);
        }

        var stockBalances = await stockBalancesQuery.ToListAsync(ct);
        var productStocks = stockBalances
            .GroupBy(s => s.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Product = g.First().Product,
                TotalQty = g.Sum(x => x.QtyOnHand)
            })
            .Where(p => p.Product != null && p.TotalQty > 0)
            .ToList();

        var productIdsWithStock = productStocks.Select(p => p.ProductId).ToList();

        // Find last sale date for these products
        var recentSales = await _context.POSOrderItems
            .AsNoTracking()
            .Include(i => i.POSOrder)
            .Where(i => productIdsWithStock.Contains(i.ProductId) && i.POSOrder != null && i.POSOrder.BusinessDate >= cutoff)
            .Select(i => i.ProductId)
            .Distinct()
            .ToListAsync(ct);

        var recentSalesSet = new HashSet<Guid>(recentSales);

        var deadStockAlerts = new List<DeadStockAlertDto>();
        var now = DateTime.UtcNow;

        foreach (var item in productStocks)
        {
            if (!recentSalesSet.Contains(item.ProductId))
            {
                // Find most recent sale date if any
                var lastSale = await _context.POSOrderItems
                    .AsNoTracking()
                    .Include(i => i.POSOrder)
                    .Where(i => i.ProductId == item.ProductId && i.POSOrder != null)
                    .OrderByDescending(i => i.POSOrder!.BusinessDate)
                    .Select(i => (DateTime?)i.POSOrder!.BusinessDate)
                    .FirstOrDefaultAsync(ct);

                var daysWithoutSale = lastSale.HasValue
                    ? (int)Math.Max(0, (now - lastSale.Value).TotalDays)
                    : 999;

                var unitCost = item.Product!.PurchasePrice;

                deadStockAlerts.Add(new DeadStockAlertDto
                {
                    ProductId = item.ProductId,
                    Sku = item.Product.Sku,
                    Name = item.Product.Name,
                    CurrentStock = item.TotalQty,
                    UnitCost = unitCost,
                    TiedUpCapital = item.TotalQty * unitCost,
                    LastSaleDate = lastSale,
                    DaysWithoutSale = daysWithoutSale
                });
            }
        }

        return deadStockAlerts.OrderByDescending(d => d.TiedUpCapital).ToList();
    }
}
