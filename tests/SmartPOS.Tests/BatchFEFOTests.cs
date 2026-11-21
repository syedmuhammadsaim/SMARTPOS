using SmartPOS.Domain.Services;
using Xunit;

namespace SmartPOS.Tests;

public class BatchFEFOTests
{
    [Fact]
    public void FEFO_Picks_Earliest_Expiring_Batch_First_And_Ignores_Expired()
    {
        var now = DateTime.UtcNow;
        var batchExpired = new AvailableBatch(Guid.NewGuid(), "B-EXP", now.AddDays(-10), 50m, 100m);
        var batchNear = new AvailableBatch(Guid.NewGuid(), "B-NEAR", now.AddDays(30), 20m, 100m);
        var batchFar = new AvailableBatch(Guid.NewGuid(), "B-FAR", now.AddDays(180), 50m, 105m);

        var batches = new[] { batchFar, batchExpired, batchNear };

        // Request 35 units
        var result = BatchFEFOEngine.Allocate(batches, 35m, now);

        Assert.True(result.IsFullyAllocated);
        Assert.Equal(35m, result.TotalAllocatedQty);
        Assert.Equal(0m, result.RemainingQtyNeeded);
        Assert.Equal(2, result.Allocations.Count);

        // First allocation must be B-NEAR (20 units)
        Assert.Equal("B-NEAR", result.Allocations[0].BatchNo);
        Assert.Equal(20m, result.Allocations[0].AllocatedQty);

        // Second allocation must be B-FAR (15 units)
        Assert.Equal("B-FAR", result.Allocations[1].BatchNo);
        Assert.Equal(15m, result.Allocations[1].AllocatedQty);
    }

    [Fact]
    public void FEFO_Signals_Partial_Allocation_When_Inventory_Insufficient()
    {
        var now = DateTime.UtcNow;
        var batchNear = new AvailableBatch(Guid.NewGuid(), "B-NEAR", now.AddDays(30), 10m, 100m);

        var result = BatchFEFOEngine.Allocate(new[] { batchNear }, 25m, now);

        Assert.False(result.IsFullyAllocated);
        Assert.Equal(10m, result.TotalAllocatedQty);
        Assert.Equal(15m, result.RemainingQtyNeeded);
    }
}
