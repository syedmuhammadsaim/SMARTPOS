using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Alerts.DTOs;
using SmartPOS.Application.Features.Loyalty.DTOs;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Customers;
using SmartPOS.Domain.Entities.Finance;
using SmartPOS.Domain.Entities.Inventory;
using SmartPOS.Domain.Entities.Organization;
using SmartPOS.Domain.Entities.PromotionsAndLoyalty;
using SmartPOS.Domain.Entities.Sales;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Infrastructure.Services;
using Xunit;

namespace SmartPOS.Tests;

public class AlertsReportsLoyaltyTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _cashierId = Guid.NewGuid();

    private SmartPOSDbContext CreateDbContext(ITenantProvider tenantProvider)
    {
        var options = new DbContextOptionsBuilder<SmartPOSDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new SmartPOSDbContext(options, tenantProvider);
    }

    private class MockTenantProvider : ITenantProvider
    {
        public Guid? CurrentBusinessId { get; }
        public Guid? CurrentBranchId { get; }
        public Guid? CurrentUserId { get; }

        public MockTenantProvider(Guid tenantId)
        {
            CurrentBusinessId = tenantId;
        }

        public void SetTenant(Guid businessId, Guid? branchId = null) { }
    }

    [Fact]
    public async Task SmartAlertService_Detects_LowStock_Expiry_And_DeadStock()
    {
        var tenant = new MockTenantProvider(_tenantId);
        using var context = CreateDbContext(tenant);

        var warehouse = new Warehouse { Id = _warehouseId, BusinessId = _tenantId, BranchId = _branchId, Name = "Main Warehouse" };
        context.Warehouses.Add(warehouse);

        var category = new Category { Id = Guid.NewGuid(), BusinessId = _tenantId, Name = "Medicines" };
        var unit = new Unit { Id = Guid.NewGuid(), BusinessId = _tenantId, Name = "Pcs", ShortCode = "pcs" };
        context.Categories.Add(category);
        context.Units.Add(unit);

        // Product 1: Low Stock (Stock 3 <= MinStock 10)
        var prod1 = new Product { Id = Guid.NewGuid(), BusinessId = _tenantId, Sku = "SKU-LOW", Name = "Low Stock Panadol", CategoryId = category.Id, UnitId = unit.Id, MinStock = 10m, PurchasePrice = 50m, SalePrice = 70m, Status = "Active" };
        var bal1 = new StockBalance { Id = Guid.NewGuid(), BusinessId = _tenantId, ProductId = prod1.Id, WarehouseId = _warehouseId, QtyOnHand = 3m, Warehouse = warehouse, Product = prod1 };

        // Product 2: Healthy Stock
        var prod2 = new Product { Id = Guid.NewGuid(), BusinessId = _tenantId, Sku = "SKU-OK", Name = "Amoxicillin Healthy", CategoryId = category.Id, UnitId = unit.Id, MinStock = 5m, PurchasePrice = 100m, SalePrice = 140m, Status = "Active" };
        var bal2 = new StockBalance { Id = Guid.NewGuid(), BusinessId = _tenantId, ProductId = prod2.Id, WarehouseId = _warehouseId, QtyOnHand = 50m, Warehouse = warehouse, Product = prod2 };

        // Product 3: Dead Stock (Stock 20, but no sales)
        var prod3 = new Product { Id = Guid.NewGuid(), BusinessId = _tenantId, Sku = "SKU-DEAD", Name = "Old Syrup Slow", CategoryId = category.Id, UnitId = unit.Id, MinStock = 2m, PurchasePrice = 200m, SalePrice = 250m, Status = "Active" };
        var bal3 = new StockBalance { Id = Guid.NewGuid(), BusinessId = _tenantId, ProductId = prod3.Id, WarehouseId = _warehouseId, QtyOnHand = 20m, Warehouse = warehouse, Product = prod3 };

        context.Products.AddRange(prod1, prod2, prod3);
        context.StockBalances.AddRange(bal1, bal2, bal3);

        // Batch Expiry
        var now = DateTime.UtcNow;
        var batchExpired = new ProductBatch
        {
            Id = Guid.NewGuid(),
            BusinessId = _tenantId,
            ProductId = prod1.Id,
            WarehouseId = _warehouseId,
            BatchNo = "B-EXP",
            ExpiryDate = now.AddDays(-2),
            QtyOnHand = 5m,
            Warehouse = warehouse,
            Product = prod1
        };
        var batchCritical = new ProductBatch
        {
            Id = Guid.NewGuid(),
            BusinessId = _tenantId,
            ProductId = prod2.Id,
            WarehouseId = _warehouseId,
            BatchNo = "B-CRIT",
            ExpiryDate = now.AddDays(15),
            QtyOnHand = 12m,
            Warehouse = warehouse,
            Product = prod2
        };
        context.ProductBatches.AddRange(batchExpired, batchCritical);

        await context.SaveChangesAsync();

        var alertService = new SmartAlertService(context);

        // 1. Check Low Stock
        var lowStockAlerts = await alertService.GetLowStockAlertsAsync();
        Assert.Single(lowStockAlerts);
        Assert.Equal("SKU-LOW", lowStockAlerts[0].Sku);
        Assert.Equal(3m, lowStockAlerts[0].CurrentStock);

        // 2. Check Expiry
        var expiryAlerts = await alertService.GetExpiryAlertsAsync(30);
        Assert.Equal(2, expiryAlerts.Count);
        Assert.Contains(expiryAlerts, b => b.UrgencyLevel == "Expired");
        Assert.Contains(expiryAlerts, b => b.UrgencyLevel == "Critical30");

        // 3. Check Dead Stock
        var deadStockAlerts = await alertService.GetDeadStockAlertsAsync(90);
        Assert.Contains(deadStockAlerts, d => d.Sku == "SKU-DEAD");
        var deadItem = deadStockAlerts.First(d => d.Sku == "SKU-DEAD");
        Assert.Equal(20m * 200m, deadItem.TiedUpCapital); // 4,000 Rs.
    }

    [Fact]
    public async Task ReportService_Calculates_ProfitAndLoss_And_Valuation_Accurately()
    {
        var tenant = new MockTenantProvider(_tenantId);
        using var context = CreateDbContext(tenant);

        var category = new Category { Id = Guid.NewGuid(), BusinessId = _tenantId, Name = "Beverages" };
        context.Categories.Add(category);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = _tenantId,
            Sku = "COCA-500",
            Name = "Coca Cola 500ml",
            PurchasePrice = 60m,
            SalePrice = 100m,
            CategoryId = category.Id,
            Category = category,
            Status = "Active"
        };
        context.Products.Add(product);

        // Stock Balance
        var warehouse = new Warehouse { Id = _warehouseId, BusinessId = _tenantId, BranchId = _branchId, Name = "Store Front" };
        context.Warehouses.Add(warehouse);

        context.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(),
            BusinessId = _tenantId,
            ProductId = product.Id,
            WarehouseId = _warehouseId,
            QtyOnHand = 100m,
            Product = product,
            Warehouse = warehouse
        });

        // 1 Order: 10 items @ Rs. 100 = Rs. 1,000, Discount = Rs. 50, COGS = 10 * Rs. 60 = Rs. 600
        var order = new POSOrder
        {
            Id = Guid.NewGuid(),
            BusinessId = _tenantId,
            BranchId = _branchId,
            CounterId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            CashierId = _cashierId,
            BusinessDate = DateTime.UtcNow.Date,
            Subtotal = 1000m,
            DiscountTotal = 50m,
            TaxTotal = 0m,
            Total = 950m,
            PaidAmount = 950m,
            Status = OrderStatus.Completed
        };
        order.Items.Add(new POSOrderItem
        {
            Id = Guid.NewGuid(),
            BusinessId = _tenantId,
            POSOrderId = order.Id,
            ProductId = product.Id,
            Qty = 10m,
            PriceSnapshot = 100m,
            TaxSnapshot = 0m,
            DiscountAmount = 50m,
            LineTotal = 950m,
            NameSnapshot = product.Name,
            Product = product
        });
        context.POSOrders.Add(order);

        // Operating Expense: Rs. 100
        var expCat = new ExpenseCategory { Id = Guid.NewGuid(), BusinessId = _tenantId, Name = "Refreshments" };
        context.ExpenseCategories.Add(expCat);
        context.Expenses.Add(new Expense
        {
            Id = Guid.NewGuid(),
            BusinessId = _tenantId,
            BranchId = _branchId,
            CategoryId = expCat.Id,
            Amount = 100m,
            Payee = "Tea Boy",
            Status = "Approved",
            CreatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var reportService = new ReportService(context);

        // 1. P&L Report
        var pnl = await reportService.GetProfitAndLossReportAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        Assert.Equal(1000m, pnl.GrossSalesRevenue);
        Assert.Equal(50m, pnl.TotalDiscounts);
        Assert.Equal(950m, pnl.NetSalesRevenue);
        Assert.Equal(600m, pnl.CostOfGoodsSold);
        Assert.Equal(350m, pnl.GrossProfit); // 950 - 600 = 350
        Assert.Equal(100m, pnl.TotalOperatingExpenses);
        Assert.Equal(250m, pnl.NetProfit); // 350 - 100 = 250

        // 2. Inventory Valuation
        var val = await reportService.GetInventoryValuationReportAsync();
        Assert.Equal(1, val.TotalDistinctSkus);
        Assert.Equal(100m, val.TotalQuantityOnHand);
        Assert.Equal(6000m, val.TotalCostValuation);   // 100 * 60
        Assert.Equal(10000m, val.TotalRetailValuation); // 100 * 100
        Assert.Equal(4000m, val.PotentialGrossProfit);  // 10000 - 6000
    }

    [Fact]
    public async Task LoyaltyService_EarnsPoints_UpgradesTier_And_RedeemsPoints()
    {
        var tenant = new MockTenantProvider(_tenantId);
        using var context = CreateDbContext(tenant);

        // Membership Tiers: Bronze (0), Silver (500), Gold (2000)
        var bronze = new MembershipTier { Id = Guid.NewGuid(), BusinessId = _tenantId, Name = "Bronze", MinPoints = 0, EarnMultiplier = 1.0m, DiscountPercent = 0 };
        var silver = new MembershipTier { Id = Guid.NewGuid(), BusinessId = _tenantId, Name = "Silver", MinPoints = 500, EarnMultiplier = 1.25m, DiscountPercent = 2m };
        var gold = new MembershipTier { Id = Guid.NewGuid(), BusinessId = _tenantId, Name = "Gold", MinPoints = 2000, EarnMultiplier = 1.5m, DiscountPercent = 5m };
        context.MembershipTiers.AddRange(bronze, silver, gold);

        var rule = new LoyaltyRule
        {
            Id = Guid.NewGuid(),
            BusinessId = _tenantId,
            EarnPerAmount = 100m, // 1 point per Rs. 100
            PointValue = 1m,      // 1 point = Rs. 1
            MinRedeemPoints = 50m
        };
        context.LoyaltyRules.Add(rule);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            BusinessId = _tenantId,
            Name = "Ahmed Ali",
            Phone = "03001234567"
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var loyaltyService = new LoyaltyService(context, tenant);

        // 1. Customer buys Rs. 60,000 bill
        // 60,000 / 100 = 600 points * 1.0 (Bronze) = 600 points
        // Lifetime points becomes 600 >= 500 -> Auto tier upgrade to SILVER!
        var pointsEarned = await loyaltyService.EarnPointsForOrderAsync(new EarnPointsRequestDto
        {
            CustomerId = customer.Id,
            POSOrderId = Guid.NewGuid(),
            AmountPaid = 60000m
        });

        Assert.Equal(600m, pointsEarned);

        var profile = await loyaltyService.GetCustomerLoyaltyProfileAsync(customer.Id);
        Assert.Equal("Silver", profile.TierName);
        Assert.Equal(600m, profile.PointsBalance);
        Assert.Equal(600m, profile.LifetimePoints);
        Assert.Equal(1400m, profile.PointsNeededForNextTier); // 2000 (Gold) - 600 = 1400

        // 2. Redeem 100 points -> Rs. 100 discount
        var redemption = await loyaltyService.RedeemPointsAsync(new RedeemPointsRequestDto
        {
            CustomerId = customer.Id,
            PointsToRedeem = 100m
        });

        Assert.True(redemption.Success);
        Assert.Equal(100m, redemption.DiscountAmount);
        Assert.Equal(500m, redemption.RemainingPoints);

        // 3. Validation: Try to redeem under minimum threshold (40 points < 50 minimum)
        var invalidRedeem = await loyaltyService.RedeemPointsAsync(new RedeemPointsRequestDto
        {
            CustomerId = customer.Id,
            PointsToRedeem = 40m
        });
        Assert.False(invalidRedeem.Success);
    }
}
