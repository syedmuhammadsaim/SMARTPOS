using System.IO;
using SmartPOS.Application.Features.POS.DTOs;
using SmartPOS.Application.Features.Products.DTOs;
using SmartPOS.Desktop.Services;
using Xunit;

namespace SmartPOS.Tests;

public class DesktopFeaturesTests
{
    [Fact]
    public void EscPosCommandBuilder_ShouldGenerateValidEscPosByteSequence()
    {
        var builder = new EscPosCommandBuilder();
        var bytes = builder
            .Initialize()
            .SetAlign(EscPosCommandBuilder.Alignment.Center)
            .SetBold(true)
            .PrintLine("SMARTPOS ENTERPRISE")
            .SetBold(false)
            .PrintQrCode("POSID:100001|USIN:INV-001|Total:500.00")
            .KickCashDrawer()
            .Cut()
            .Build();

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);

        // Check ESC @ init bytes
        Assert.Equal(0x1B, bytes[0]);
        Assert.Equal(0x40, bytes[1]);

        // Check ESC p (open cash drawer) exists in stream
        Assert.Contains((byte)0x70, bytes);
        // Check GS V (cut paper) exists in stream
        Assert.Contains((byte)0x56, bytes);
    }

    [Fact]
    public async Task OfflineQueueService_ShouldQueueAndRetrieveOrders()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "SmartPOS_Test_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            var queueService = new OfflineQueueService(tempDir);

            var sampleOrder = new CreateSaleOrderDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                new List<SaleOrderItemDto>
                {
                    new(Guid.NewGuid(), null, null, 2, 250m, 0m)
                },
                new List<SaleOrderPaymentDto>
                {
                    new(Guid.NewGuid(), 500m, 500m)
                }
            );

            await queueService.EnqueueOfflineOrderAsync(sampleOrder);

            var pendingCount = await queueService.GetPendingCountAsync();
            Assert.Equal(1, pendingCount);

            var pendingOrders = await queueService.GetPendingOrdersAsync();
            Assert.Single(pendingOrders);
            Assert.Equal(sampleOrder.ClientTransactionId, pendingOrders[0].ClientTransactionId);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task OfflineQueueService_ShouldSaveAndLookupCachedCatalog()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "SmartPOS_CatalogTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            var queueService = new OfflineQueueService(tempDir);

            var products = new List<ProductSearchResultDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    SKU = "DRK-001",
                    Name = "Nestle Pure Life 1.5L",
                    Barcode = "8964000112233",
                    SalePrice = 120m,
                    AvailableStock = 45,
                    CategoryName = "Beverages",
                    UnitName = "Bottle"
                }
            };

            await queueService.SaveCachedCatalogAsync(products);

            var found = await queueService.FindCachedProductByBarcodeAsync("8964000112233");
            Assert.NotNull(found);
            Assert.Equal("DRK-001", found.SKU);
            Assert.Equal(120m, found.SalePrice);

            var searchResults = await queueService.SearchCachedCatalogAsync("Nestle");
            Assert.Single(searchResults);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
