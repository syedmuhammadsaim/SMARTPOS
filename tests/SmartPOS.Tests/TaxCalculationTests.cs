using SmartPOS.Domain.Enums;
using SmartPOS.Domain.Services;
using Xunit;

namespace SmartPOS.Tests;

public class TaxCalculationTests
{
    [Fact]
    public void Inclusive_Tax_Extracts_Correct_Base_And_Tax()
    {
        // 18% GST inclusive on Rs. 118 item
        var result = TaxCalculationEngine.Calculate(
            unitPrice: 118m,
            quantity: 1m,
            taxRatePercentage: 18m,
            isInclusive: true,
            isExempt: false,
            roundingMode: TaxRoundingMode.RoundToNearestRupee
        );

        Assert.Equal(100m, result.BaseAmount);
        Assert.Equal(18m, result.TaxAmount);
        Assert.Equal(118m, result.TotalAmount);
    }

    [Fact]
    public void Exclusive_Tax_Calculates_Correct_Total_And_Tax()
    {
        // 18% GST exclusive on Rs. 100 item
        var result = TaxCalculationEngine.Calculate(
            unitPrice: 100m,
            quantity: 1m,
            taxRatePercentage: 18m,
            isInclusive: false,
            isExempt: false,
            roundingMode: TaxRoundingMode.RoundToNearestRupee
        );

        Assert.Equal(100m, result.BaseAmount);
        Assert.Equal(18m, result.TaxAmount);
        Assert.Equal(118m, result.TotalAmount);
    }

    [Fact]
    public void Tax_Exempt_Product_Has_Zero_Tax()
    {
        var result = TaxCalculationEngine.Calculate(
            unitPrice: 250m,
            quantity: 3m,
            taxRatePercentage: 18m,
            isInclusive: false,
            isExempt: true,
            roundingMode: TaxRoundingMode.RoundToNearestRupee
        );

        Assert.Equal(750m, result.BaseAmount);
        Assert.Equal(0m, result.TaxAmount);
        Assert.Equal(750m, result.TotalAmount);
    }
}
