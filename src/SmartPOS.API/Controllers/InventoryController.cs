using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Inventory;
using SmartPOS.Application.Features.Inventory.DTOs;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("balances")]
    public async Task<ActionResult<List<StockBalanceDto>>> GetStockBalances(
        [FromQuery] Guid? warehouseId,
        [FromQuery] bool lowStockOnly,
        CancellationToken ct)
    {
        var balances = await _inventoryService.GetStockBalancesAsync(warehouseId, lowStockOnly, ct);
        return Ok(balances);
    }

    [HttpGet("expiring-batches")]
    public async Task<ActionResult<List<ExpiringStockAlertDto>>> GetExpiringBatches(
        [FromQuery] int daysThreshold = 60,
        [FromQuery] Guid? warehouseId = null,
        CancellationToken ct = default)
    {
        var batches = await _inventoryService.GetExpiringBatchesAsync(daysThreshold, warehouseId, ct);
        return Ok(batches);
    }

    [HttpPost("transfers")]
    public async Task<ActionResult> InitiateTransfer([FromBody] CreateStockTransferRequest request, CancellationToken ct)
    {
        var user = User.Identity?.Name ?? "InventoryManager";
        var transferNo = await _inventoryService.InitiateStockTransferAsync(request, user, ct);
        return Ok(new { transferNumber = transferNo, message = "Stock transfer initiated and dispatched." });
    }

    [HttpPost("transfers/{id:guid}/receive")]
    public async Task<ActionResult> ReceiveTransfer(Guid id, CancellationToken ct)
    {
        var user = User.Identity?.Name ?? "InventoryManager";
        var success = await _inventoryService.ReceiveStockTransferAsync(id, user, ct);
        if (!success)
            return BadRequest(new { message = "Transfer cannot be received or does not exist." });

        return Ok(new { message = "Stock transfer received and credited to warehouse inventory." });
    }

    [HttpPost("adjustments")]
    public async Task<ActionResult> AdjustStock([FromBody] StockAdjustmentRequest request, CancellationToken ct)
    {
        var user = User.Identity?.Name ?? "InventoryManager";
        var success = await _inventoryService.AdjustStockAsync(request, user, ct);
        if (!success)
            return BadRequest(new { message = "Failed to adjust stock." });

        return Ok(new { message = "Stock physical adjustment recorded." });
    }
}
