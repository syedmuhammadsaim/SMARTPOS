using System.Text;
using SmartPOS.Application.Features.Communication;

namespace SmartPOS.Infrastructure.Services;

public class WhatsAppService : IWhatsAppService
{
    public Task<WhatsAppSendResult> SendDigitalReceiptAsync(WhatsAppReceiptRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerPhoneNumber))
        {
            return Task.FromResult(new WhatsAppSendResult(false, null, "Customer phone number is missing."));
        }

        // Clean Pakistani phone numbers (e.g. 03001234567 -> 923001234567)
        var phone = request.CustomerPhoneNumber.Trim().Replace("-", "").Replace(" ", "");
        if (phone.StartsWith("03"))
        {
            phone = "92" + phone[1..];
        }

        var sb = new StringBuilder();
        sb.AppendLine("🧾 *SMARTPOS Enterprise - Digital Receipt*");
        sb.AppendLine("================================");
        sb.AppendLine($"Hello {request.CustomerName}, thank you for shopping!");
        sb.AppendLine($"*Invoice #:* {request.InvoiceNo}");
        sb.AppendLine($"*Date:* {DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"*Grand Total:* Rs. {request.TotalAmount:N2}");
        if (!string.IsNullOrWhiteSpace(request.FbrInvoiceNo))
        {
            sb.AppendLine($"*FBR POS No:* {request.FbrInvoiceNo}");
        }
        sb.AppendLine("================================");
        sb.AppendLine("شکریہ! دوبارہ تشریف لائیں۔");

        // Simulate Gateway dispatch (returns successful message ID)
        var messageId = $"WA-{DateTime.UtcNow.Ticks}";

        return Task.FromResult(new WhatsAppSendResult(true, messageId, null));
    }
}
