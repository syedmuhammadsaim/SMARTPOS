using SmartPOS.Application.Features.FBR.DTOs;

namespace SmartPOS.Application.Features.FBR;

public interface IFbrService
{
    Task<FbrSubmissionResult> SubmitInvoiceAsync(Guid posOrderId, CancellationToken ct = default);
    Task<int> ProcessRetryQueueAsync(CancellationToken ct = default);
    Task<string> GenerateFbrQrCodePayloadAsync(string invoiceNo, decimal totalBill, decimal taxCharged, string fbrInvoiceNo, DateTime invoiceDate);
}
