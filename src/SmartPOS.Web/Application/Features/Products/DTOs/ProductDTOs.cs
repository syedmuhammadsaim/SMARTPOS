using SmartPOS.Domain.Enums;

namespace SmartPOS.Application.Features.Products.DTOs;

public class ProductSearchResultDto
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameUrdu { get; set; }
    public string? Barcode { get; set; }
    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }
    public decimal SalePrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public decimal PurchasePrice { get; set; }
    public Guid? TaxId { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsTaxInclusive { get; set; }
    public ProductType ProductType { get; set; }
    public bool TrackBatch { get; set; }
    public bool TrackExpiry { get; set; }
    public decimal AvailableStock { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
}

public class CreateProductRequest
{
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameUrdu { get; set; }
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid UnitId { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public decimal MinimumPrice { get; set; }
    public Guid? TaxId { get; set; }
    public ProductType ProductType { get; set; } = ProductType.Standard;
    public bool TrackBatch { get; set; }
    public bool TrackExpiry { get; set; }
    public decimal AlertQuantity { get; set; } = 5;
    public string? PrimaryBarcode { get; set; }
    public List<CreateVariantRequest>? Variants { get; set; }
    public List<string>? AdditionalBarcodes { get; set; }
}

public class CreateVariantRequest
{
    public string SKU { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal? PurchasePriceOverride { get; set; }
    public decimal? SalePriceOverride { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
}

public class ProductDetailDto
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameUrdu { get; set; }
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public Guid? BrandId { get; set; }
    public string? BrandName { get; set; }
    public Guid UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public decimal MinimumPrice { get; set; }
    public Guid? TaxId { get; set; }
    public decimal TaxRate { get; set; }
    public ProductType ProductType { get; set; }
    public bool TrackBatch { get; set; }
    public bool TrackExpiry { get; set; }
    public decimal AlertQuantity { get; set; }
    public bool IsActive { get; set; }
    public List<string> Barcodes { get; set; } = new();
    public List<ProductVariantDto> Variants { get; set; } = new();
    public List<ProductBatchDto> Batches { get; set; } = new();
}

public class ProductVariantDto
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal? SalePriceOverride { get; set; }
    public decimal? PurchasePriceOverride { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
}

public class ProductBatchDto
{
    public Guid Id { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ManufactureDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal CurrentStock { get; set; }
    public bool IsExpired => ExpiryDate <= DateTime.UtcNow;
    public int DaysUntilExpiry => (ExpiryDate - DateTime.UtcNow).Days;
}
