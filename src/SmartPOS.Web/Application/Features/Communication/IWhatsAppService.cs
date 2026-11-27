namespace SmartPOS.Application.Features.Communication;

public record WhatsAppReceiptRequest(
    Guid POSOrderId,
    string CustomerPhoneNumber,
    string CustomerName,
    string InvoiceNo,
    decimal TotalAmount,
    string? FbrInvoiceNo = null);

public record WhatsAppSendResult(
    bool Success,
    string? MessageId,
    string? Error);

public interface IWhatsAppService
{
    Task<WhatsAppSendResult> SendDigitalReceiptAsync(WhatsAppReceiptRequest request, CancellationToken ct = default);
}
