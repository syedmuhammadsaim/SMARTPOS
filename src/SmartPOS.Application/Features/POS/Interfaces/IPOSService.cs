using SmartPOS.Application.Features.POS.DTOs;

namespace SmartPOS.Application.Features.POS.Interfaces;

public interface IPOSService
{
    Task<SaleOrderResponseDto> CreateSaleOrderAsync(CreateSaleOrderDto dto, Guid cashierId);
    Task<HeldBillSummaryDto> HoldBillAsync(HoldBillDto dto, Guid cashierId);
    Task<CreateSaleOrderDto?> ResumeHeldBillAsync(Guid heldBillId);
    Task<CounterSessionReportDto> GenerateXReportAsync(Guid sessionId, Guid requestedBy);
    Task<CounterSessionReportDto> CloseSessionAndGenerateZReportAsync(Guid sessionId, decimal actualCashCounted, Guid closedBy);
}
