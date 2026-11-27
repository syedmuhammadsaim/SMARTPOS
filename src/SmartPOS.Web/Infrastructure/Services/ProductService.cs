using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Common.Interfaces;
using SmartPOS.Application.Features.Products;
using SmartPOS.Application.Features.Products.DTOs;
using SmartPOS.Domain.Entities.Catalog;
using SmartPOS.Domain.Entities.Inventory;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly SmartPOSDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public ProductService(SmartPOSDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<List<ProductSearchResultDto>> SearchProductsAsync(string query, Guid? branchId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<ProductSearchResultDto>();

        var normalized = query.Trim().ToLower();

        // 1. Search products by SKU or Name or Urdu Name
        var productMatches = await _context.Products
            .AsNoTracking()
            .Include(p => p.Unit)
            .Include(p => p.Category)
            .Include(p => p.Tax)
            .Include(p => p.Barcodes)
            .Where(p => p.Status == "Active" &&
                        (p.Sku.ToLower().Contains(normalized) ||
                         p.Name.ToLower().Contains(normalized) ||
                         (p.NameUrdu != null && p.NameUrdu.Contains(normalized)) ||
                         p.Barcodes.Any(b => b.Barcode.ToLower() == normalized)))
            .Take(25)
            .ToListAsync(ct);

        var results = new List<ProductSearchResultDto>();

        foreach (var p in productMatches)
        {
            // Stock query
            var totalStock = await _context.StockBalances
                .Where(sb => sb.ProductId == p.Id)
                .SumAsync(sb => (decimal?)sb.QtyOnHand, ct) ?? 0m;

            var primaryBarcode = p.Barcodes.FirstOrDefault(b => b.IsPrimary)?.Barcode ?? p.Barcodes.FirstOrDefault()?.Barcode;

            results.Add(new ProductSearchResultDto
            {
                Id = p.Id,
                SKU = p.Sku,
                Name = p.Name,
                NameUrdu = p.NameUrdu,
                Barcode = primaryBarcode,
                SalePrice = p.SalePrice,
                WholesalePrice = p.WholesalePrice,
                PurchasePrice = p.PurchasePrice,
                TaxId = p.TaxId,
                TaxRate = p.Tax?.Rate ?? 0m,
                IsTaxInclusive = p.Tax?.IsInclusive ?? true,
                ProductType = p.Type,
                TrackBatch = p.TrackBatch,
                TrackExpiry = p.TrackExpiry,
                AvailableStock = totalStock,
                UnitName = p.Unit?.Name ?? "Pcs",
                CategoryName = p.Category?.Name ?? "General"
            });
        }

        return results;
    }

    public async Task<ProductSearchResultDto?> ScanBarcodeAsync(string barcode, Guid? branchId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return null;

        var cleanBarcode = barcode.Trim();

        var barcodeEntry = await _context.ProductBarcodes
            .AsNoTracking()
            .Include(b => b.Product)
                .ThenInclude(p => p!.Unit)
            .Include(b => b.Product)
                .ThenInclude(p => p!.Category)
            .Include(b => b.Product)
                .ThenInclude(p => p!.Tax)
            .Include(b => b.Variant)
            .FirstOrDefaultAsync(b => b.Barcode == cleanBarcode, ct);

        if (barcodeEntry?.Product == null)
        {
            // Check direct SKU match
            var directProduct = await _context.Products
                .AsNoTracking()
                .Include(p => p.Unit)
                .Include(p => p.Category)
                .Include(p => p.Tax)
                .Include(p => p.Barcodes)
                .FirstOrDefaultAsync(p => p.Sku == cleanBarcode && p.Status == "Active", ct);

            if (directProduct == null)
                return null;

            var stock = await _context.StockBalances
                .Where(sb => sb.ProductId == directProduct.Id)
                .SumAsync(sb => (decimal?)sb.QtyOnHand, ct) ?? 0m;

            return new ProductSearchResultDto
            {
                Id = directProduct.Id,
                SKU = directProduct.Sku,
                Name = directProduct.Name,
                NameUrdu = directProduct.NameUrdu,
                Barcode = directProduct.Barcodes.FirstOrDefault()?.Barcode,
                SalePrice = directProduct.SalePrice,
                WholesalePrice = directProduct.WholesalePrice,
                PurchasePrice = directProduct.PurchasePrice,
                TaxId = directProduct.TaxId,
                TaxRate = directProduct.Tax?.Rate ?? 0m,
                IsTaxInclusive = directProduct.Tax?.IsInclusive ?? true,
                ProductType = directProduct.Type,
                TrackBatch = directProduct.TrackBatch,
                TrackExpiry = directProduct.TrackExpiry,
                AvailableStock = stock,
                UnitName = directProduct.Unit?.Name ?? "Pcs",
                CategoryName = directProduct.Category?.Name ?? "General"
            };
        }

        var prod = barcodeEntry.Product;
        var variant = barcodeEntry.Variant;
        var salePrice = variant?.PriceOverride ?? prod.SalePrice;

        var availableStock = await _context.StockBalances
            .Where(sb => sb.ProductId == prod.Id && (variant == null || sb.VariantId == variant.Id))
            .SumAsync(sb => (decimal?)sb.QtyOnHand, ct) ?? 0m;

        return new ProductSearchResultDto
        {
            Id = prod.Id,
            SKU = variant?.VariantSku ?? prod.Sku,
            Name = prod.Name,
            NameUrdu = prod.NameUrdu,
            Barcode = barcodeEntry.Barcode,
            VariantId = variant?.Id,
            VariantName = variant != null ? $"{variant.Size} {variant.Color}".Trim() : null,
            SalePrice = salePrice,
            WholesalePrice = prod.WholesalePrice,
            PurchasePrice = prod.PurchasePrice,
            TaxId = prod.TaxId,
            TaxRate = prod.Tax?.Rate ?? 0m,
            IsTaxInclusive = prod.Tax?.IsInclusive ?? true,
            ProductType = prod.Type,
            TrackBatch = prod.TrackBatch,
            TrackExpiry = prod.TrackExpiry,
            AvailableStock = availableStock,
            UnitName = prod.Unit?.Name ?? "Pcs",
            CategoryName = prod.Category?.Name ?? "General"
        };
    }

    public async Task<ProductDetailDto?> GetProductByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.Tax)
            .Include(p => p.Variants)
            .Include(p => p.Barcodes)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product == null)
            return null;

        var batches = await _context.ProductBatches
            .AsNoTracking()
            .Where(b => b.ProductId == id && b.QtyOnHand > 0)
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new ProductBatchDto
            {
                Id = b.Id,
                BatchNumber = b.BatchNo,
                ManufactureDate = b.ManufactureDate,
                ExpiryDate = b.ExpiryDate,
                CostPrice = b.CostPrice,
                CurrentStock = b.QtyOnHand
            })
            .ToListAsync(ct);

        return new ProductDetailDto
        {
            Id = product.Id,
            SKU = product.Sku,
            Name = product.Name,
            NameUrdu = product.NameUrdu,
            Description = null,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name,
            UnitId = product.UnitId,
            UnitName = product.Unit?.Name ?? string.Empty,
            PurchasePrice = product.PurchasePrice,
            SalePrice = product.SalePrice,
            WholesalePrice = product.WholesalePrice,
            MinimumPrice = product.PurchasePrice,
            TaxId = product.TaxId,
            TaxRate = product.Tax?.Rate ?? 0m,
            ProductType = product.Type,
            TrackBatch = product.TrackBatch,
            TrackExpiry = product.TrackExpiry,
            AlertQuantity = product.MinStock,
            IsActive = product.Status == "Active",
            Barcodes = product.Barcodes.Select(b => b.Barcode).ToList(),
            Variants = product.Variants.Select(v => new ProductVariantDto
            {
                Id = v.Id,
                SKU = v.VariantSku,
                VariantName = $"{v.Size} {v.Color}".Trim(),
                SalePriceOverride = v.PriceOverride,
                Size = v.Size,
                Color = v.Color
            }).ToList(),
            Batches = batches
        };
    }

    public async Task<Guid> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.CurrentBusinessId ?? Guid.Empty;

        var product = new Product
        {
            BusinessId = tenantId,
            Sku = request.SKU,
            Name = request.Name,
            NameUrdu = request.NameUrdu,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            UnitId = request.UnitId,
            PurchasePrice = request.PurchasePrice,
            SalePrice = request.SalePrice,
            WholesalePrice = request.WholesalePrice,
            MinStock = request.AlertQuantity,
            TaxId = request.TaxId,
            Type = request.ProductType,
            TrackBatch = request.TrackBatch,
            TrackExpiry = request.TrackExpiry,
            Status = "Active"
        };

        _context.Products.Add(product);

        if (!string.IsNullOrWhiteSpace(request.PrimaryBarcode))
        {
            _context.ProductBarcodes.Add(new ProductBarcode
            {
                BusinessId = tenantId,
                ProductId = product.Id,
                Barcode = request.PrimaryBarcode.Trim(),
                IsPrimary = true
            });
        }

        if (request.AdditionalBarcodes != null)
        {
            foreach (var bc in request.AdditionalBarcodes.Where(b => !string.IsNullOrWhiteSpace(b)))
            {
                _context.ProductBarcodes.Add(new ProductBarcode
                {
                    BusinessId = tenantId,
                    ProductId = product.Id,
                    Barcode = bc.Trim(),
                    IsPrimary = false
                });
            }
        }

        if (request.Variants != null)
        {
            foreach (var vr in request.Variants)
            {
                var variant = new ProductVariant
                {
                    BusinessId = tenantId,
                    ProductId = product.Id,
                    VariantSku = vr.SKU,
                    Size = vr.Size,
                    Color = vr.Color,
                    PriceOverride = vr.SalePriceOverride
                };
                _context.ProductVariants.Add(variant);

                if (!string.IsNullOrWhiteSpace(vr.Barcode))
                {
                    _context.ProductBarcodes.Add(new ProductBarcode
                    {
                        BusinessId = tenantId,
                        ProductId = product.Id,
                        VariantId = variant.Id,
                        Barcode = vr.Barcode.Trim(),
                        IsPrimary = true
                    });
                }
            }
        }

        await _context.SaveChangesAsync(ct);
        return product.Id;
    }

    public async Task<bool> UpdateProductPriceAsync(Guid id, decimal newSalePrice, decimal newPurchasePrice, string changedBy, CancellationToken ct = default)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product == null)
            return false;

        var history = new ProductPriceHistory
        {
            BusinessId = product.BusinessId,
            ProductId = product.Id,
            OldPrice = product.SalePrice,
            NewPrice = newSalePrice,
            ChangedBy = Guid.Empty
        };

        product.SalePrice = newSalePrice;
        product.PurchasePrice = newPurchasePrice;

        _context.ProductPriceHistories.Add(history);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<ProductBatchDto>> GetProductBatchesAsync(Guid productId, Guid? warehouseId = null, CancellationToken ct = default)
    {
        var query = _context.ProductBatches
            .AsNoTracking()
            .Where(b => b.ProductId == productId && b.QtyOnHand > 0);

        if (warehouseId.HasValue)
            query = query.Where(b => b.WarehouseId == warehouseId.Value);

        return await query
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new ProductBatchDto
            {
                Id = b.Id,
                BatchNumber = b.BatchNo,
                ManufactureDate = b.ManufactureDate,
                ExpiryDate = b.ExpiryDate,
                CostPrice = b.CostPrice,
                CurrentStock = b.QtyOnHand
            })
            .ToListAsync(ct);
    }
}
