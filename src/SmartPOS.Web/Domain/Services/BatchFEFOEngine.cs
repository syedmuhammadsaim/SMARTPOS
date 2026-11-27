namespace SmartPOS.Domain.Services;

public record AvailableBatch(
    Guid BatchId,
    string BatchNo,
    DateTime ExpiryDate,
    decimal QtyAvailable,
    decimal CostPrice);

public record BatchAllocation(
    Guid BatchId,
    string BatchNo,
    DateTime ExpiryDate,
    decimal AllocatedQty,
    decimal CostPrice);

public record BatchAllocationResult(
    bool IsFullyAllocated,
    decimal TotalAllocatedQty,
    decimal RemainingQtyNeeded,
    IReadOnlyList<BatchAllocation> Allocations);

public static class BatchFEFOEngine
{
    public static BatchAllocationResult Allocate(
        IEnumerable<AvailableBatch> batches,
        decimal requestedQty,
        DateTime referenceUtc)
    {
        if (requestedQty <= 0)
        {
            return new BatchAllocationResult(
                IsFullyAllocated: true,
                TotalAllocatedQty: 0m,
                RemainingQtyNeeded: 0m,
                Allocations: Array.Empty<BatchAllocation>()
            );
        }

        // FEFO: Sort by earliest ExpiryDate, filter out already expired batches
        var eligibleBatches = batches
            .Where(b => b.ExpiryDate > referenceUtc && b.QtyAvailable > 0)
            .OrderBy(b => b.ExpiryDate)
            .ToList();

        var allocations = new List<BatchAllocation>();
        var remainingQty = requestedQty;

        foreach (var batch in eligibleBatches)
        {
            if (remainingQty <= 0) break;

            var allocQty = Math.Min(batch.QtyAvailable, remainingQty);
            allocations.Add(new BatchAllocation(
                BatchId: batch.BatchId,
                BatchNo: batch.BatchNo,
                ExpiryDate: batch.ExpiryDate,
                AllocatedQty: allocQty,
                CostPrice: batch.CostPrice
            ));

            remainingQty -= allocQty;
        }

        var totalAllocated = requestedQty - remainingQty;
        return new BatchAllocationResult(
            IsFullyAllocated: remainingQty == 0,
            TotalAllocatedQty: totalAllocated,
            RemainingQtyNeeded: remainingQty,
            Allocations: allocations
        );
    }
}
