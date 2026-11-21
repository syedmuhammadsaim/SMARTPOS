using SmartPOS.Domain.Services;
using Xunit;

namespace SmartPOS.Tests;

public class CashReconciliationTests
{
    [Fact]
    public void Blueprint_Example_Calculation_Matches_Expected_And_Variance()
    {
        // Example from Section 9:
        // Rs. 10,000 opening + Rs. 25,000 cash sales - Rs. 1,000 refunds - Rs. 500 expenses = Rs. 33,500 expected.
        // Actual Rs. 33,000 gives -Rs. 500 variance.
        var input = new CashReconciliationInput(
            OpeningCash: 10_000m,
            CashSales: 25_000m,
            CashReceived: 0m,
            CashRefunds: 1_000m,
            CashExpenses: 500m,
            CashPaidOut: 0m,
            ActualCash: 33_000m
        );

        var result = CashReconciliationEngine.Calculate(input);

        Assert.Equal(33_500m, result.ExpectedCash);
        Assert.Equal(-500m, result.Variance);
        Assert.False(result.IsBalanced);
        Assert.True(result.HasShortage);
        Assert.False(result.HasExcess);
    }

    [Fact]
    public void Udhaar_Customer_Payment_Included_In_Cash_Received()
    {
        var input = new CashReconciliationInput(
            OpeningCash: 5_000m,
            CashSales: 20_000m,
            CashReceived: 3_500m, // Udhaar recovery
            CashRefunds: 500m,
            CashExpenses: 1_000m,
            CashPaidOut: 2_000m,  // Cash drop / safe deposit
            ActualCash: 25_000m
        );

        var result = CashReconciliationEngine.Calculate(input);

        // Expected = 5000 + 20000 + 3500 - 500 - 1000 - 2000 = 25000
        Assert.Equal(25_000m, result.ExpectedCash);
        Assert.Equal(0m, result.Variance);
        Assert.True(result.IsBalanced);
        Assert.False(result.HasShortage);
    }
}
