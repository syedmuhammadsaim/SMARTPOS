using SmartPOS.Domain.Enums;

namespace SmartPOS.Domain.Services;

public record TaxCalculationResult(
    decimal BaseAmount,
    decimal TaxAmount,
    decimal TotalAmount);

public static class TaxCalculationEngine
{
    public static TaxCalculationResult Calculate(
        decimal unitPrice,
        decimal quantity,
        decimal taxRatePercentage,
        bool isInclusive,
        bool isExempt,
        TaxRoundingMode roundingMode = TaxRoundingMode.RoundToNearestRupee)
    {
        if (isExempt || taxRatePercentage <= 0)
        {
            var gross = ApplyRounding(unitPrice * quantity, roundingMode);
            return new TaxCalculationResult(BaseAmount: gross, TaxAmount: 0m, TotalAmount: gross);
        }

        var rate = taxRatePercentage / 100m;
        decimal baseAmount;
        decimal taxAmount;
        decimal totalAmount;

        if (isInclusive)
        {
            totalAmount = unitPrice * quantity;
            baseAmount = totalAmount / (1m + rate);
            taxAmount = totalAmount - baseAmount;
        }
        else
        {
            baseAmount = unitPrice * quantity;
            taxAmount = baseAmount * rate;
            totalAmount = baseAmount + taxAmount;
        }

        baseAmount = ApplyRounding(baseAmount, roundingMode);
        taxAmount = ApplyRounding(taxAmount, roundingMode);
        totalAmount = ApplyRounding(totalAmount, roundingMode);

        return new TaxCalculationResult(
            BaseAmount: baseAmount,
            TaxAmount: taxAmount,
            TotalAmount: totalAmount
        );
    }

    public static decimal ApplyRounding(decimal value, TaxRoundingMode mode)
    {
        return mode switch
        {
            TaxRoundingMode.RoundToNearestRupee => Math.Round(value, 0, MidpointRounding.AwayFromZero),
            TaxRoundingMode.HalfUp => Math.Round(value, 2, MidpointRounding.AwayFromZero),
            TaxRoundingMode.HalfEven => Math.Round(value, 2, MidpointRounding.ToEven),
            _ => Math.Round(value, 2, MidpointRounding.AwayFromZero)
        };
    }
}
