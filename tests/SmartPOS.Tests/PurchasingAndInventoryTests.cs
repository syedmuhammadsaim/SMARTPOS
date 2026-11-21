using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Customers;
using SmartPOS.Application.Features.Customers.DTOs;
using SmartPOS.Application.Features.Inventory;
using SmartPOS.Application.Features.Inventory.DTOs;
using SmartPOS.Application.Features.Purchasing;
using SmartPOS.Application.Features.Purchasing.DTOs;
using SmartPOS.Application.Features.Sequences.Interfaces;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Customers;
using SmartPOS.Domain.Entities.Inventory;
using SmartPOS.Domain.Entities.Purchasing;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Infrastructure.Services;
using Xunit;

namespace SmartPOS.Tests;

public class PurchasingAndInventoryTests
{
    private class TestTenantProvider : ITenantProvider
    {
        public Guid? CurrentBusinessId { get; set; } = Guid.NewGuid();
        public Guid? CurrentBranchId => null;
        public Guid? CurrentUserId => null;
        public void SetTenant(Guid businessId, Guid? branchId = null)
        {
            CurrentBusinessId = businessId;
        }
    }

    private class TestSequenceService : IDocumentSequenceService
    {
        private int _counter = 1;
        public Task<string> GetNextNumberAsync(Guid businessId, Guid? branchId, Guid? counterId, string docType, string prefix)
        {
            return Task.FromResult($"{prefix}-2026-{_counter++:D6}");
        }
    }

    private SmartPOSDbContext CreateDbContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<SmartPOSDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var tenantProvider = new TestTenantProvider { CurrentBusinessId = tenantId };
        return new SmartPOSDbContext(options, tenantProvider);
    }

    [Fact]
    public async Task ProcessGoodsReceipt_ShouldCreateBatch_UpdateStock_AndCreateSupplierLedger()
    {
        var tenantId = Guid.NewGuid();
        using var context = CreateDbContext(tenantId);
        var tenantProvider = new TestTenantProvider { CurrentBusinessId = tenantId };
        var sequenceService = new TestSequenceService();
        var purchaseService = new PurchaseService(context, tenantProvider, sequenceService);

        // Seed Supplier, Warehouse, Category, Unit, Product
        var supplier = new Supplier
        {
            BusinessId = tenantId,
            Code = "SUP-001",
            Name = "National Distributors",
            CurrentBalance = 0m
        };
        context.Suppliers.Add(supplier);

        var warehouse = new Warehouse
        {
            BusinessId = tenantId,
            BranchId = Guid.NewGuid(),
            Code = "WH-01",
            Name = "Main Warehouse"
        };
        context.Warehouses.Add(warehouse);

        var category = new Category { BusinessId = tenantId, Name = "Pharma" };
        var unit = new Unit { BusinessId = tenantId, Name = "Box", ShortCode = "Bx" };
        context.Categories.Add(category);
        context.Units.Add(unit);

        var product = new Product
        {
            BusinessId = tenantId,
            Sku = "PANADOL-500",
            Name = "Panadol 500mg Tablet",
            CategoryId = category.Id,
            UnitId = unit.Id,
            PurchasePrice = 250m,
            SalePrice = 300m,
            TrackBatch = true,
            TrackExpiry = true
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var request = new GoodsReceiptRequest
        {
            SupplierId = supplier.Id,
            WarehouseId = warehouse.Id,
            SupplierInvoiceNo = "INV-ND-9921",
            InvoiceDate = DateTime.UtcNow,
            Items = new List<GoodsReceiptItemRequest>
            {
                new()
                {
                    ProductId = product.Id,
                    ReceivedQuantity = 50,
                    UnitCost = 260m,
                    SalePrice = 320m,
                    BatchNumber = "BATCH-2026A",
                    ManufactureDate = DateTime.UtcNow.AddMonths(-1),
                    ExpiryDate = DateTime.UtcNow.AddMonths(24)
                }
            }
        };

        var response = await purchaseService.ProcessGoodsReceiptAsync(request, "StoreKeeper");

        // Verify GRN response
        Assert.NotNull(response);
        Assert.Equal(50 * 260m, response.TotalAmount);

        // Verify Batch was created
        var batch = await context.ProductBatches.FirstOrDefaultAsync(b => b.BatchNo == "BATCH-2026A");
        Assert.NotNull(batch);
        Assert.Equal(50, batch.QtyOnHand);
        Assert.Equal(260m, batch.CostPrice);

        // Verify StockBalance updated
        var stock = await context.StockBalances.FirstOrDefaultAsync(s => s.ProductId == product.Id && s.WarehouseId == warehouse.Id);
        Assert.NotNull(stock);
        Assert.Equal(50, stock.QtyOnHand);

        // Verify StockTransaction logged
        var tx = await context.StockTransactions.FirstOrDefaultAsync(t => t.ProductId == product.Id);
        Assert.NotNull(tx);
        Assert.Equal(StockTransactionType.GoodsReceipt, tx.Type);
        Assert.Equal(50, tx.Qty);

        // Verify Supplier balance & ledger
        var updatedSupplier = await context.Suppliers.FirstAsync(s => s.Id == supplier.Id);
        Assert.Equal(13000m, updatedSupplier.CurrentBalance);

        var ledgerEntry = await context.SupplierLedgers.FirstOrDefaultAsync(l => l.SupplierId == supplier.Id);
        Assert.NotNull(ledgerEntry);
        Assert.Equal(13000m, ledgerEntry.Credit);
        Assert.Equal(13000m, ledgerEntry.RunningBalance);
    }

    [Fact]
    public async Task CustomerPayment_ShouldReduceBalance_AndRecordLedger()
    {
        var tenantId = Guid.NewGuid();
        using var context = CreateDbContext(tenantId);
        var tenantProvider = new TestTenantProvider { CurrentBusinessId = tenantId };
        var sequenceService = new TestSequenceService();
        var customerService = new CustomerService(context, tenantProvider, sequenceService);

        var customer = new Customer
        {
            BusinessId = tenantId,
            Name = "Ahmed Ali",
            Phone = "03001234567",
            CreditLimit = 50000m,
            CurrentBalance = 15000m // Customer owes Rs.15,000 Udhaar
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var paymentReq = new CustomerPaymentRequest
        {
            CustomerId = customer.Id,
            Amount = 5000m, // Ahmed pays Rs.5,000
            PaymentMethodId = Guid.NewGuid(),
            Notes = "Partial Udhaar payment in cash"
        };

        var receiptNo = await customerService.RecordCustomerPaymentAsync(paymentReq, "Cashier");

        Assert.NotEmpty(receiptNo);

        var updatedCustomer = await context.Customers.FirstAsync(c => c.Id == customer.Id);
        Assert.Equal(10000m, updatedCustomer.CurrentBalance);

        var ledger = await context.CustomerLedgers.FirstOrDefaultAsync(l => l.CustomerId == customer.Id);
        Assert.NotNull(ledger);
        Assert.Equal(5000m, ledger.Credit);
        Assert.Equal(10000m, ledger.RunningBalance);
    }

    [Fact]
    public async Task StockTransfer_DispatchAndReceive_ShouldAccuratelyMoveInventory()
    {
        var tenantId = Guid.NewGuid();
        using var context = CreateDbContext(tenantId);
        var tenantProvider = new TestTenantProvider { CurrentBusinessId = tenantId };
        var sequenceService = new TestSequenceService();
        var inventoryService = new InventoryService(context, tenantProvider, sequenceService);

        var warehouseA = new Warehouse { BusinessId = tenantId, BranchId = Guid.NewGuid(), Code = "WH-A", Name = "Central WH" };
        var warehouseB = new Warehouse { BusinessId = tenantId, BranchId = Guid.NewGuid(), Code = "WH-B", Name = "Gulberg Branch" };
        context.Warehouses.AddRange(warehouseA, warehouseB);

        var category = new Category { BusinessId = tenantId, Name = "Beverages" };
        var unit = new Unit { BusinessId = tenantId, Name = "Bottle", ShortCode = "Btl" };
        context.Categories.Add(category);
        context.Units.Add(unit);

        var product = new Product
        {
            BusinessId = tenantId,
            Sku = "COKE-500ML",
            Name = "Coca Cola 500ml",
            CategoryId = category.Id,
            UnitId = unit.Id,
            PurchasePrice = 80m,
            SalePrice = 100m
        };
        context.Products.Add(product);

        // Seed 100 bottles in Warehouse A
        context.StockBalances.Add(new StockBalance
        {
            BusinessId = tenantId,
            WarehouseId = warehouseA.Id,
            ProductId = product.Id,
            QtyOnHand = 100m
        });
        await context.SaveChangesAsync();

        // Transfer 40 bottles from Warehouse A to Warehouse B
        var transferReq = new CreateStockTransferRequest
        {
            SourceWarehouseId = warehouseA.Id,
            DestinationWarehouseId = warehouseB.Id,
            Notes = "Replenishment for Gulberg branch",
            Items = new List<StockTransferItemRequest>
            {
                new()
                {
                    ProductId = product.Id,
                    Quantity = 40m
                }
            }
        };

        var transferNo = await inventoryService.InitiateStockTransferAsync(transferReq, "DispatchOfficer");
        Assert.NotEmpty(transferNo);

        // Warehouse A should now have 60 left
        var stockA = await context.StockBalances.FirstAsync(sb => sb.WarehouseId == warehouseA.Id && sb.ProductId == product.Id);
        Assert.Equal(60m, stockA.QtyOnHand);

        // Receive the transfer at Warehouse B
        var transfer = await context.StockTransfers.FirstAsync(t => t.TransferNo == transferNo);
        var received = await inventoryService.ReceiveStockTransferAsync(transfer.Id, "ReceivingOfficer");
        Assert.True(received);

        // Warehouse B should now have 40 bottles
        var stockB = await context.StockBalances.FirstAsync(sb => sb.WarehouseId == warehouseB.Id && sb.ProductId == product.Id);
        Assert.Equal(40m, stockB.QtyOnHand);
    }
}
