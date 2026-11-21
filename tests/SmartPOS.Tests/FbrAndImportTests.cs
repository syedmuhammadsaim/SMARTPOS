using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Communication;
using SmartPOS.Application.Features.FBR;
using SmartPOS.Application.Features.ImportExport;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Inventory;
using SmartPOS.Domain.Entities.Organization;
using SmartPOS.Domain.Entities.Sales;
using SmartPOS.Domain.Entities.System;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Infrastructure.Services;
using Xunit;

namespace SmartPOS.Tests;

public class FbrAndImportTests
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
    public async Task FbrService_SubmitInvoice_ShouldGenerateFbrNumberAndQrCodePayload()
    {
        var tenantId = Guid.NewGuid();
        using var context = CreateDbContext(tenantId);
        var tenantProvider = new TestTenantProvider { CurrentBusinessId = tenantId };
        var fbrService = new FbrService(context, tenantProvider);

        var branch = new Branch { BusinessId = tenantId, Code = "BR-01", Name = "Lahore Flagship" };
        context.Branches.Add(branch);

        var fbrSetting = new FbrSetting
        {
            BusinessId = tenantId,
            BranchId = branch.Id,
            PosId = "109922",
            IsEnabled = true,
            Environment = "Sandbox"
        };
        context.FbrSettings.Add(fbrSetting);

        var order = new POSOrder
        {
            BusinessId = tenantId,
            BranchId = branch.Id,
            CounterId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            InvoiceNo = "INV-2026-000451",
            Subtotal = 1000m,
            TaxTotal = 180m,
            Total = 1180m,
            BusinessDate = DateTime.UtcNow
        };
        context.POSOrders.Add(order);
        await context.SaveChangesAsync();

        var result = await fbrService.SubmitInvoiceAsync(order.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.FbrInvoiceNo);
        Assert.NotNull(result.QrCodeData);
        Assert.Contains("POSID:100001", result.QrCodeData);
        Assert.Contains("USIN:INV-2026-000451", result.QrCodeData);
        Assert.Contains("TotalAmount:1180.00", result.QrCodeData);

        var updatedOrder = await context.POSOrders.FirstAsync(o => o.Id == order.Id);
        Assert.Equal(FbrInvoiceStatus.Submitted, updatedOrder.FbrStatus);
        Assert.Equal(result.FbrInvoiceNo, updatedOrder.FbrInvoiceNo);
    }

    [Fact]
    public async Task WhatsAppService_ShouldFormatCleanReceipt_ForPakistaniPhoneNumber()
    {
        var service = new WhatsAppService();
        var request = new WhatsAppReceiptRequest(
            Guid.NewGuid(),
            "0300-1234567",
            "Hamza Khan",
            "INV-2026-001092",
            2450.50m,
            "100001202611210009"
        );

        var result = await service.SendDigitalReceiptAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.MessageId);
        Assert.StartsWith("WA-", result.MessageId);
    }

    [Fact]
    public async Task ImportService_ShouldParseCsv_AndCreateProductsWithOpeningStock()
    {
        var tenantId = Guid.NewGuid();
        using var context = CreateDbContext(tenantId);
        var tenantProvider = new TestTenantProvider { CurrentBusinessId = tenantId };
        var importService = new ImportService(context, tenantProvider);

        var warehouse = new Warehouse
        {
            BusinessId = tenantId,
            BranchId = Guid.NewGuid(),
            Code = "WH-MAIN",
            Name = "Main Storage"
        };
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync();

        var csvContent = new StringBuilder();
        csvContent.AppendLine("SKU,Name,Barcode,Category,Unit,PurchasePrice,SalePrice,OpeningStock,BatchNo,ExpiryDate");
        csvContent.AppendLine("DISPRIN-100,Disprin Soluble 100mg,89640001001,Medicines,Box,45.00,55.00,100,B-DSP-01,2028-12-31");
        csvContent.AppendLine("LAYS-MASALA,Lays Masala 65g,89640002002,Snacks,Packet,80.00,100.00,50,,");

        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

        var importResult = await importService.ImportProductsFromCsvAsync(memoryStream, warehouse.Id);

        Assert.Equal(2, importResult.TotalRows);
        Assert.Equal(2, importResult.SuccessRows);
        Assert.Equal(0, importResult.FailedRows);

        // Verify products created
        var disprin = await context.Products.FirstOrDefaultAsync(p => p.Sku == "DISPRIN-100");
        Assert.NotNull(disprin);
        Assert.Equal("Disprin Soluble 100mg", disprin.Name);
        Assert.Equal(45.00m, disprin.PurchasePrice);
        Assert.Equal(55.00m, disprin.SalePrice);

        // Verify barcode created
        var barcode = await context.ProductBarcodes.FirstOrDefaultAsync(b => b.ProductId == disprin.Id);
        Assert.NotNull(barcode);
        Assert.Equal("89640001001", barcode.Barcode);

        // Verify batch created with opening stock
        var batch = await context.ProductBatches.FirstOrDefaultAsync(b => b.ProductId == disprin.Id);
        Assert.NotNull(batch);
        Assert.Equal("B-DSP-01", batch.BatchNo);
        Assert.Equal(100m, batch.QtyOnHand);

        // Verify stock balance created
        var balance = await context.StockBalances.FirstOrDefaultAsync(sb => sb.ProductId == disprin.Id && sb.WarehouseId == warehouse.Id);
        Assert.NotNull(balance);
        Assert.Equal(100m, balance.QtyOnHand);
    }
}
