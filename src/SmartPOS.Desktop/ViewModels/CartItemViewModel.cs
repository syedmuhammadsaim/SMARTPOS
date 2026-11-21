namespace SmartPOS.Desktop.ViewModels;

public class CartItemViewModel : BaseViewModel
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameUrdu { get; set; }
    public string? VariantName { get; set; }
    public string? Barcode { get; set; }
    public string UnitName { get; set; } = "Pcs";

    private decimal _unitPrice;
    public decimal UnitPrice
    {
        get => _unitPrice;
        set { SetProperty(ref _unitPrice, value); Recalculate(); }
    }

    private decimal _quantity = 1;
    public decimal Quantity
    {
        get => _quantity;
        set { SetProperty(ref _quantity, value); Recalculate(); }
    }

    private decimal _discountPercent;
    public decimal DiscountPercent
    {
        get => _discountPercent;
        set { SetProperty(ref _discountPercent, value); Recalculate(); }
    }

    public Guid? TaxId { get; set; }
    public decimal TaxRate { get; set; }
    public bool IsTaxInclusive { get; set; } = true;

    private decimal _lineSubtotal;
    public decimal LineSubtotal
    {
        get => _lineSubtotal;
        set => SetProperty(ref _lineSubtotal, value);
    }

    private decimal _lineDiscount;
    public decimal LineDiscount
    {
        get => _lineDiscount;
        set => SetProperty(ref _lineDiscount, value);
    }

    private decimal _lineTax;
    public decimal LineTax
    {
        get => _lineTax;
        set => SetProperty(ref _lineTax, value);
    }

    private decimal _lineTotal;
    public decimal LineTotal
    {
        get => _lineTotal;
        set => SetProperty(ref _lineTotal, value);
    }

    public void Recalculate()
    {
        var gross = Quantity * UnitPrice;
        var discount = gross * (DiscountPercent / 100m);
        var net = gross - discount;

        decimal tax = 0m;
        if (TaxRate > 0)
        {
            if (IsTaxInclusive)
            {
                tax = net - (net / (1 + (TaxRate / 100m)));
            }
            else
            {
                tax = net * (TaxRate / 100m);
            }
        }

        LineSubtotal = gross;
        LineDiscount = discount;
        LineTax = tax;
        LineTotal = IsTaxInclusive ? net : (net + tax);
    }
}
