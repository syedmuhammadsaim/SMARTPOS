using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Customers;
using SmartPOS.Domain.Entities.Finance;
using SmartPOS.Domain.Entities.Inventory;
using SmartPOS.Domain.Entities.Organization;
using SmartPOS.Domain.Entities.Purchasing;
using SmartPOS.Domain.Entities.Sales;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;
using Xunit;

namespace SmartPOS.Tests;

public class MultiTenantSaaSIsolationTests
{
    private (SmartPOSDbContext context, string dbName) CreateSharedInMemoryDatabase(TestTenantProvider tenantProvider, string? existingDbName = null)
    {
        var dbName = existingDbName ?? Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<SmartPOSDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return (new SmartPOSDbContext(options, tenantProvider), dbName);
    }

    [Fact]
    public async Task CrossTenant_Isolation_Products_Never_Leak_Between_Tenants()
    {
        var tenantA = new TestTenantProvider { CurrentBusinessId = Guid.NewGuid() };
        var tenantB = new TestTenantProvider { CurrentBusinessId = Guid.NewGuid() };

        // 1. Tenant A writes a product to shared DB
        var (contextA, sharedDbName) = CreateSharedInMemoryDatabase(tenantA);
        await using (contextA)
        {
            contextA.Products.Add(new Product
            {
                BusinessId = tenantA.CurrentBusinessId!.Value,
                Name = "Tenant A Confidential Formula",
                Sku = "TA-001",
                PurchasePrice = 500m,
                SalePrice = 1200m,
                Status = "Active"
            });
            await contextA.SaveChangesAsync();
        }

        // 2. Tenant B queries the exact same database
        var (contextB, _) = CreateSharedInMemoryDatabase(tenantB, sharedDbName);
        await using (contextB)
        {
            var visibleProducts = await contextB.Products.ToListAsync();
            Assert.Empty(visibleProducts);

            var directLookup = await contextB.Products.FirstOrDefaultAsync(p => p.Sku == "TA-001");
            Assert.Null(directLookup);
        }
    }

    [Fact]
    public async Task CrossTenant_Isolation_SalesOrders_Are_Strictly_Partitioned()
    {
        var tenantA = new TestTenantProvider { CurrentBusinessId = Guid.NewGuid() };
        var tenantB = new TestTenantProvider { CurrentBusinessId = Guid.NewGuid() };

        var (contextA, sharedDbName) = CreateSharedInMemoryDatabase(tenantA);
        await using (contextA)
        {
            contextA.POSOrders.Add(new POSOrder
            {
                BusinessId = tenantA.CurrentBusinessId!.Value,
                InvoiceNo = "INV-2026-999999",
                BranchId = Guid.NewGuid(),
                CounterId = Guid.NewGuid(),
                SessionId = Guid.NewGuid(),
                CashierId = Guid.NewGuid(),
                BusinessDate = DateTime.UtcNow.Date,
                Subtotal = 1500m,
                Total = 1500m,
                Status = OrderStatus.Completed
            });
            await contextA.SaveChangesAsync();
        }

        var (contextB, _) = CreateSharedInMemoryDatabase(tenantB, sharedDbName);
        await using (contextB)
        {
            var bOrders = await contextB.POSOrders.ToListAsync();
            Assert.Empty(bOrders);
        }
    }

    [Fact]
    public async Task CrossTenant_Isolation_Customers_And_Ledgers_Are_Private()
    {
        var tenantA = new TestTenantProvider { CurrentBusinessId = Guid.NewGuid() };
        var tenantB = new TestTenantProvider { CurrentBusinessId = Guid.NewGuid() };

        var (contextA, sharedDbName) = CreateSharedInMemoryDatabase(tenantA);
        await using (contextA)
        {
            contextA.Customers.Add(new Customer
            {
                BusinessId = tenantA.CurrentBusinessId!.Value,
                Name = "High Net-Worth Client A",
                Phone = "0321-9999999",
                CurrentBalance = 50000m,
                Status = "Active"
            });
            await contextA.SaveChangesAsync();
        }

        var (contextB, _) = CreateSharedInMemoryDatabase(tenantB, sharedDbName);
        await using (contextB)
        {
            var bCustomers = await contextB.Customers.ToListAsync();
            Assert.Empty(bCustomers);
        }
    }

    [Fact]
    public async Task CrossTenant_Isolation_Expenses_And_Inventory_Balances_Isolated()
    {
        var tenantA = new TestTenantProvider { CurrentBusinessId = Guid.NewGuid() };
        var tenantB = new TestTenantProvider { CurrentBusinessId = Guid.NewGuid() };

        var (contextA, sharedDbName) = CreateSharedInMemoryDatabase(tenantA);
        await using (contextA)
        {
            contextA.Expenses.Add(new Expense
            {
                BusinessId = tenantA.CurrentBusinessId!.Value,
                ExpenseNo = "EXP-999",
                BranchId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                Amount = 85000m,
                PaymentMethod = "BankTransfer",
                Status = "Approved"
            });

            contextA.StockBalances.Add(new StockBalance
            {
                BusinessId = tenantA.CurrentBusinessId!.Value,
                WarehouseId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                QtyOnHand = 250m
            });

            await contextA.SaveChangesAsync();
        }

        var (contextB, _) = CreateSharedInMemoryDatabase(tenantB, sharedDbName);
        await using (contextB)
        {
            var bExpenses = await contextB.Expenses.ToListAsync();
            var bStock = await contextB.StockBalances.ToListAsync();

            Assert.Empty(bExpenses);
            Assert.Empty(bStock);
        }
    }
}
