using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPOS.Application.Features.Purchasing;
using SmartPOS.Application.Features.Purchasing.DTOs;

namespace SmartPOS.API.Controllers;

[ApiController]
[Route("api/v1/purchasing")]
[Authorize]
public class PurchasingController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;

    public PurchasingController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    [HttpGet("suppliers")]
    public async Task<ActionResult<List<SupplierDto>>> GetSuppliers([FromQuery] string? search, CancellationToken ct)
    {
        var suppliers = await _purchaseService.GetAllSuppliersAsync(search, ct);
        return Ok(suppliers);
    }

    [HttpGet("suppliers/{id:guid}")]
    public async Task<ActionResult<SupplierDto>> GetSupplierById(Guid id, CancellationToken ct)
    {
        var supplier = await _purchaseService.GetSupplierByIdAsync(id, ct);
        if (supplier == null)
            return NotFound(new { message = $"Supplier {id} not found." });

        return Ok(supplier);
    }

    [HttpPost("suppliers")]
    public async Task<ActionResult<Guid>> CreateSupplier([FromBody] CreateSupplierRequest request, CancellationToken ct)
    {
        var id = await _purchaseService.CreateSupplierAsync(request, ct);
        return CreatedAtAction(nameof(GetSupplierById), new { id }, id);
    }

    [HttpPost("goods-receipts")]
    public async Task<ActionResult<GoodsReceiptResponseDto>> ProcessGoodsReceipt([FromBody] GoodsReceiptRequest request, CancellationToken ct)
    {
        var user = User.Identity?.Name ?? "StoreKeeper";
        var result = await _purchaseService.ProcessGoodsReceiptAsync(request, user, ct);
        return Ok(result);
    }

    [HttpPost("payments")]
    public async Task<ActionResult> RecordSupplierPayment([FromBody] SupplierPaymentRequest request, CancellationToken ct)
    {
        var user = User.Identity?.Name ?? "Accountant";
        var paymentNo = await _purchaseService.RecordSupplierPaymentAsync(request, user, ct);
        return Ok(new { paymentNumber = paymentNo, message = "Supplier payment recorded successfully." });
    }

    [HttpGet("suppliers/{id:guid}/ledger")]
    public async Task<ActionResult<List<SupplierLedgerEntryDto>>> GetSupplierLedger(
        Guid id,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct)
    {
        var ledger = await _purchaseService.GetSupplierLedgerAsync(id, fromDate, toDate, ct);
        return Ok(ledger);
    }
}
