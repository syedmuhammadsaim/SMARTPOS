using SmartPOS.Application.Features.Returns.DTOs;

namespace SmartPOS.Application.Features.Returns;

public interface IReturnService
{
    Task<OrderReturnEligibilityDto?> GetOrderForReturnAsync(string invoiceNo, CancellationToken ct = default);
    Task<SaleReturnResponse> ProcessSaleReturnAsync(CreateSaleReturnRequest request, CancellationToken ct = default);
}
