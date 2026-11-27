using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.ImportExport;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Inventory;
using SmartPOS.Domain.Entities.System;
using SmartPOS.Domain.Enums;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class ImportService : IImportService
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public ImportService(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<ImportResultDto> ImportProductsFromCsvAsync(Stream csvStream, Guid warehouseId, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.CurrentBusinessId ?? Guid.Empty;
        var job = new ImportJob
        {
            BusinessId = tenantId,
            Type = "Products",
            FileName = "products_import.csv",
            Status = "InProgress"
        };
        _context.ImportJobs.Add(job);
        await _context.SaveChangesAsync(ct);

        using var reader = new StreamReader(csvStream);
        var errors = new List<string>();
        int total = 0;
        int success = 0;
        int failed = 0;

        string? headerLine = await reader.ReadLineAsync(ct);
        if (headerLine == null)
        {
            job.Status = "Failed";
            await _context.SaveChangesAsync(ct);
            return new ImportResultDto { JobId = job.Id, TotalRows = 0, ErrorMessages = new List<string> { "Empty CSV file" } };
        }

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            total++;
            var parts = line.Split(',');

            if (parts.Length < 6)
            {
                failed++;
                errors.Add($"Line {total}: Insufficient columns.");
                continue;
            }

            try
            {
                var sku = parts[0].Trim();
                var name = parts[1].Trim();
                var barcode = parts.Length > 2 ? parts[2].Trim() : string.Empty;
                var categoryName = parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) ? parts[3].Trim() : "General";
                var unitName = parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4]) ? parts[4].Trim() : "Piece";
                decimal.TryParse(parts[5].Trim(), CultureInfo.InvariantCulture, out var purchasePrice);
                decimal.TryParse(parts.Length > 6 ? parts[6].Trim() : "0", CultureInfo.InvariantCulture, out var salePrice);
                decimal.TryParse(parts.Length > 7 ? parts[7].Trim() : "0", CultureInfo.InvariantCulture, out var openingStock);
                var batchNo = parts.Length > 8 ? parts[8].Trim() : null;
                DateTime? expiryDate = null;
                if (parts.Length > 9 && DateTime.TryParse(parts[9].Trim(), out var exp))
                {
                    expiryDate = exp;
                }

                // Find or create Category
                var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name.ToLower() == categoryName.ToLower(), ct);
                if (category == null)
                {
                    category = new Category { BusinessId = tenantId, Name = categoryName, Code = categoryName[..Math.Min(4, categoryName.Length)].ToUpper() };
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync(ct);
                }

                // Find or create Unit
                var unit = await _context.Units.FirstOrDefaultAsync(u => u.Name.ToLower() == unitName.ToLower(), ct);
                if (unit == null)
                {
                    unit = new Unit { BusinessId = tenantId, Name = unitName, ShortCode = unitName[..Math.Min(3, unitName.Length)].ToUpper() };
                    _context.Units.Add(unit);
                    await _context.SaveChangesAsync(ct);
                }

                // Check duplicate SKU
                var existingProduct = await _context.Products.FirstOrDefaultAsync(p => p.Sku == sku, ct);
                if (existingProduct != null)
                {
                    failed++;
                    errors.Add($"Line {total}: SKU '{sku}' already exists.");
                    continue;
                }

                var product = new Product
                {
                    BusinessId = tenantId,
                    Sku = sku,
                    Name = name,
                    CategoryId = category.Id,
                    UnitId = unit.Id,
                    PurchasePrice = purchasePrice,
                    SalePrice = salePrice > 0 ? salePrice : purchasePrice * 1.2m,
                    WholesalePrice = purchasePrice * 1.1m,
                    TrackBatch = !string.IsNullOrWhiteSpace(batchNo) || expiryDate.HasValue,
                    TrackExpiry = expiryDate.HasValue,
                    Status = "Active"
                };
                _context.Products.Add(product);

                if (!string.IsNullOrWhiteSpace(barcode))
                {
                    _context.ProductBarcodes.Add(new ProductBarcode
                    {
                        BusinessId = tenantId,
                        ProductId = product.Id,
                        Barcode = barcode,
                        IsPrimary = true
                    });
                }

                // Opening Stock Handling
                if (openingStock > 0)
                {
                    ProductBatch? batch = null;
                    if (product.TrackBatch || !string.IsNullOrWhiteSpace(batchNo))
                    {
                        batch = new ProductBatch
                        {
                            BusinessId = tenantId,
                            ProductId = product.Id,
                            WarehouseId = warehouseId,
                            BatchNo = !string.IsNullOrWhiteSpace(batchNo) ? batchNo : $"OP-{sku}",
                            ExpiryDate = expiryDate ?? DateTime.UtcNow.AddYears(2),
                            CostPrice = purchasePrice,
                            QtyReceived = openingStock,
                            QtyOnHand = openingStock,
                            Status = "Active"
                        };
                        _context.ProductBatches.Add(batch);
                    }

                    _context.StockBalances.Add(new StockBalance
                    {
                        BusinessId = tenantId,
                        WarehouseId = warehouseId,
                        ProductId = product.Id,
                        BatchId = batch?.Id,
                        QtyOnHand = openingStock
                    });

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        BusinessId = tenantId,
                        ProductId = product.Id,
                        WarehouseId = warehouseId,
                        BatchId = batch?.Id,
                        Type = StockTransactionType.OpeningStock,
                        Qty = openingStock,
                        UnitCost = purchasePrice,
                        ReferenceType = "ImportOpeningStock",
                        ReferenceId = job.Id,
                        Note = "Bulk CSV Opening Stock Import"
                    });
                }

                await _context.SaveChangesAsync(ct);
                success++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"Line {total}: {ex.Message}");
            }
        }

        job.TotalRows = total;
        job.SuccessRows = success;
        job.ErrorRows = failed;
        job.Status = failed == 0 ? "Completed" : "CompletedWithErrors";
        await _context.SaveChangesAsync(ct);

        return new ImportResultDto
        {
            JobId = job.Id,
            TotalRows = total,
            SuccessRows = success,
            FailedRows = failed,
            ErrorMessages = errors
        };
    }
}
