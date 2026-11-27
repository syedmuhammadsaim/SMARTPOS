using SmartPOS.Application.Features.Purchasing.DTOs;

namespace SmartPOS.Application.Features.Purchasing;

public interface IPurchaseService
{
    Task<List<SupplierDto>> GetAllSuppliersAsync(string? searchTerm = null, CancellationToken ct = default);
    Task<SupplierDto?> GetSupplierByIdAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken ct = default);
    Task<GoodsReceiptResponseDto> ProcessGoodsReceiptAsync(GoodsReceiptRequest request, string receivedBy, CancellationToken ct = default);
    Task<string> RecordSupplierPaymentAsync(SupplierPaymentRequest request, string recordedBy, CancellationToken ct = default);
    Task<List<SupplierLedgerEntryDto>> GetSupplierLedgerAsync(Guid supplierId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
}
