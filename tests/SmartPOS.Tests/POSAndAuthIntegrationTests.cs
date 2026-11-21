using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Auth.DTOs;
using SmartPOS.Application.Features.POS.DTOs;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Identity;
using SmartPOS.Domain.Entities.Inventory;
using SmartPOS.Domain.Entities.Organization;
using SmartPOS.Domain.Entities.Sales;
using SmartPOS.Domain.Entities.ShiftsAndSessions;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Infrastructure.Identity;
using SmartPOS.Infrastructure.Services;
using Xunit;

namespace SmartPOS.Tests;

public class TestTenantProvider : ITenantProvider
{
    public Guid? CurrentBusinessId { get; set; } = Guid.NewGuid();
    public Guid? CurrentBranchId { get; set; } = Guid.NewGuid();
    public Guid? CurrentUserId { get; set; } = Guid.NewGuid();
    public void SetTenant(Guid businessId, Guid? branchId = null)
    {
        CurrentBusinessId = businessId;
        CurrentBranchId = branchId;
    }
}

public class POSAndAuthIntegrationTests
{
    private SmartPOSDbContext CreateInMemoryContext(TestTenantProvider tenantProvider)
    {
        var options = new DbContextOptionsBuilder<SmartPOSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new SmartPOSDbContext(options, tenantProvider);
    }

    [Fact]
    public async Task DocumentSequence_Generates_Sequential_Numbers()
    {
        var tenant = new TestTenantProvider();
        using var context = CreateInMemoryContext(tenant);
        var sequenceService = new DocumentSequenceService(context);

        var num1 = await sequenceService.GetNextNumberAsync(tenant.CurrentBusinessId!.Value, tenant.CurrentBranchId, null, "POSInvoice", "INV");
        var num2 = await sequenceService.GetNextNumberAsync(tenant.CurrentBusinessId!.Value, tenant.CurrentBranchId, null, "POSInvoice", "INV");

        var year = DateTime.UtcNow.Year;
        Assert.Equal($"INV-{year}-000001", num1);
        Assert.Equal($"INV-{year}-000002", num2);
    }

    [Fact]
    public async Task POS_Order_Creation_Is_Idempotent_With_ClientTransactionId()
    {
        var tenant = new TestTenantProvider();
        using var context = CreateInMemoryContext(tenant);
        var sequenceService = new DocumentSequenceService(context);
        var posService = new POSService(context, sequenceService, tenant);

        var businessId = tenant.CurrentBusinessId!.Value;
        var branchId = tenant.CurrentBranchId!.Value;

        // Seed branch, warehouse, session, product, stock
        var branch = new Branch { Id = branchId, BusinessId = businessId, Code = "BR01", Name = "Main Branch", InvoicePrefix = "INV" };
        var warehouse = new Warehouse { Id = Guid.NewGuid(), BusinessId = businessId, BranchId = branchId, Code = "WH01", Name = "Main WH", Status = "Active" };
        var session = new CounterSession
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            BranchId = branchId,
            CounterId = Guid.NewGuid(),
            ShiftId = Guid.NewGuid(),
            BusinessDate = DateTime.UtcNow.Date,
            OpeningCash = 5000m,
            Status = CounterSessionStatus.Open
        };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Sku = "SKU-001",
            Name = "Milk Pack 1L",
            SalePrice = 280m,
            PurchasePrice = 250m,
            UnitId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            Status = "Active"
        };
        var stock = new StockBalance
        {
            BusinessId = businessId,
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            QtyOnHand = 100m
        };

        context.Branches.Add(branch);
        context.Warehouses.Add(warehouse);
        context.CounterSessions.Add(session);
        context.Products.Add(product);
        context.StockBalances.Add(stock);
        await context.SaveChangesAsync();

        var clientTxId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();

        var orderDto = new CreateSaleOrderDto(
            ClientTransactionId: clientTxId,
            BranchId: branchId,
            CounterId: session.CounterId,
            SessionId: session.Id,
            CustomerId: null,
            Items: new List<SaleOrderItemDto>
            {
                new(product.Id, null, null, 2m, 280m, 0m)
            },
            Payments: new List<SaleOrderPaymentDto>
            {
                new(Guid.NewGuid(), 560m, 600m)
            }
        );

        // First attempt
        var response1 = await posService.CreateSaleOrderAsync(orderDto, cashierId);
        Assert.True(response1.Success);
        Assert.NotNull(response1.OrderId);

        // Second attempt with exact same ClientTransactionId (simulating network retry)
        var response2 = await posService.CreateSaleOrderAsync(orderDto, cashierId);
        Assert.True(response2.Success);
        Assert.Equal(response1.OrderId, response2.OrderId);
        Assert.Equal(response1.InvoiceNo, response2.InvoiceNo);
        Assert.Contains("Idempotency", response2.Message);

        // Confirm only 1 order exists in database
        var orderCount = await context.POSOrders.CountAsync();
        Assert.Equal(1, orderCount);
    }
}
