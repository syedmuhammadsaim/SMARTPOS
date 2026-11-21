using SmartPOS.Domain.Common;
using SmartPOS.Domain.Enums;

namespace SmartPOS.Domain.Entities.Pricing;

public class Tax : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; } // e.g. 18.00 for 18% GST
    public bool IsInclusive { get; set; }
    public TaxRoundingMode RoundingMode { get; set; } = TaxRoundingMode.RoundToNearestRupee;
    public bool IsExempt { get; set; }
}

public class PriceList : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Retail"; // Retail, Wholesale, BranchSpecific
    public string CurrencyCode { get; set; } = "PKR";
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsDefault { get; set; }

    public ICollection<PriceListItem> Items { get; set; } = new List<PriceListItem>();
}

public class PriceListItem : BaseTenantEntity
{
    public Guid PriceListId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? UnitId { get; set; }
    public decimal Price { get; set; }
    public decimal MinQty { get; set; } = 1m;

    public PriceList? PriceList { get; set; }
}

public class CustomerGroup : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty; // Retail, VIP, Wholesale
    public Guid? DefaultPriceListId { get; set; }
    public decimal DefaultDiscountPercent { get; set; }

    public PriceList? DefaultPriceList { get; set; }
}
