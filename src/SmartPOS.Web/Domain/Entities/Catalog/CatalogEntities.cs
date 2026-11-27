using SmartPOS.Domain.Common;
using SmartPOS.Domain.Enums;
using SmartPOS.Domain.Entities.Pricing;

namespace SmartPOS.Domain.Entities.Catalog;

public class Category : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public Guid? ParentCategoryId { get; set; }

    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Brand : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Unit : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty; // Piece, Kg, Liter, Carton, Dozen
    public string ShortCode { get; set; } = string.Empty; // Pcs, Kg, Ltr, Ctn, Dzn
    public bool AllowDecimals { get; set; }
}

public class Product : BaseTenantEntity
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameUrdu { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid UnitId { get; set; }
    public Guid? TaxId { get; set; }
    public ProductType Type { get; set; } = ProductType.Standard;
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public decimal MinStock { get; set; } = 5m;
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = "Active";

    // Blueprint v2 flags
    public bool TrackBatch { get; set; }
    public bool TrackExpiry { get; set; }

    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
    public Unit? Unit { get; set; }
    public Tax? Tax { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductBarcode> Barcodes { get; set; } = new List<ProductBarcode>();
    public ICollection<ProductPriceHistory> PriceHistories { get; set; } = new List<ProductPriceHistory>();
    public ICollection<ProductUnitConversion> UnitConversions { get; set; } = new List<ProductUnitConversion>();
}

public class ProductVariant : BaseTenantEntity
{
    public Guid ProductId { get; set; }
    public string VariantSku { get; set; } = string.Empty;
    public string? Size { get; set; }
    public string? Color { get; set; }
    public string? Attribute1 { get; set; }
    public string? Attribute2 { get; set; }
    public decimal? PriceOverride { get; set; }

    public Product? Product { get; set; }
}

public class ProductBarcode : BaseTenantEntity
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public bool IsPrimary { get; set; } = true;

    public Product? Product { get; set; }
    public ProductVariant? Variant { get; set; }
}

public class ProductBundleItem : BaseTenantEntity
{
    public Guid BundleProductId { get; set; }
    public Guid ComponentProductId { get; set; }
    public decimal Quantity { get; set; }

    public Product? BundleProduct { get; set; }
    public Product? ComponentProduct { get; set; }
}

public class ProductPriceHistory : BaseTenantEntity
{
    public Guid ProductId { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public Guid ChangedBy { get; set; }

    public Product? Product { get; set; }
}

public class ProductUnitConversion : BaseTenantEntity
{
    public Guid ProductId { get; set; }
    public Guid UnitId { get; set; }
    public decimal ConversionFactor { get; set; } // e.g. 1 Carton = 12 Pieces
    public string? Barcode { get; set; }

    public Product? Product { get; set; }
    public Unit? Unit { get; set; }
}
