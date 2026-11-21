namespace SmartPOS.Domain.Services;

public record CashReconciliationInput(
    decimal OpeningCash,
    decimal CashSales,
    decimal CashReceived,
    decimal CashRefunds,
    decimal CashExpenses,
    decimal CashPaidOut,
    decimal ActualCash);

public record CashReconciliationResult(
    decimal ExpectedCash,
    decimal Variance,
    bool IsBalanced,
    bool HasShortage,
    bool HasExcess);

public static class CashReconciliationEngine
{
    /// <summary>
    /// Computes Expected Cash and Variance according to SMARTPOS Enterprise formula:
    /// Expected Cash = Opening Cash + Cash Sales + Cash Received - Cash Refunds - Cash Expenses - Cash Paid Out
    /// Actual Cash Variance = Actual Cash - Expected Cash
    /// </summary>
    public static CashReconciliationResult Calculate(CashReconciliationInput input)
    {
        var expectedCash = input.OpeningCash 
            + input.CashSales 
            + input.CashReceived 
            - input.CashRefunds 
            - input.CashExpenses 
            - input.CashPaidOut;

        var variance = input.ActualCash - expectedCash;

        return new CashReconciliationResult(
            ExpectedCash: expectedCash,
            Variance: variance,
            IsBalanced: variance == 0m,
            HasShortage: variance < 0m,
            HasExcess: variance > 0m
        );
    }
}
