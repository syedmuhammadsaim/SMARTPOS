using System.Text;
using SmartPOS.Desktop.ViewModels;

namespace SmartPOS.Desktop.Services;

public static class ThermalReceiptPrinter
{
    public static string GenerateReceiptText(
        string invoiceNo,
        string branchName,
        string counterName,
        string cashierName,
        IEnumerable<CartItemViewModel> items,
        decimal subtotal,
        decimal discount,
        decimal tax,
        decimal grandTotal,
        decimal tendered,
        decimal change,
        string? fbrInvoiceNo = null)
    {
        var sb = new StringBuilder();
        const int width = 42;

        string Center(string text)
        {
            if (text.Length >= width) return text;
            var pad = (width - text.Length) / 2;
            return text.PadLeft(pad + text.Length).PadRight(width);
        }

        string Row(string col1, string col2)
        {
            var space = width - col1.Length - col2.Length;
            if (space < 1) space = 1;
            return col1 + new string(' ', space) + col2;
        }

        sb.AppendLine(Center("=========================================="));
        sb.AppendLine(Center("SMARTPOS ENTERPRISE"));
        sb.AppendLine(Center("RETAIL MANAGEMENT SYSTEM"));
        sb.AppendLine(Center(branchName));
        sb.AppendLine(Center("NTN: 1234567-8 | STRN: 12-00-1234-567-89"));
        sb.AppendLine(Center("=========================================="));
        sb.AppendLine(Row("Invoice No:", invoiceNo));
        sb.AppendLine(Row("Date/Time:", DateTime.Now.ToString("dd/MM/yyyy HH:mm")));
        sb.AppendLine(Row("Counter:", counterName));
        sb.AppendLine(Row("Cashier:", cashierName));
        sb.AppendLine(new string('-', width));
        sb.AppendLine(string.Format("{0,-20}{1,5}{2,8}{3,9}", "ITEM", "QTY", "RATE", "TOTAL"));
        sb.AppendLine(new string('-', width));

        foreach (var item in items)
        {
            var name = item.Name.Length > 20 ? item.Name[..17] + "..." : item.Name;
            sb.AppendLine(string.Format("{0,-20}{1,5:N0}{2,8:N2}{3,9:N2}", name, item.Quantity, item.UnitPrice, item.LineTotal));
            if (!string.IsNullOrWhiteSpace(item.VariantName))
            {
                sb.AppendLine($"  ({item.VariantName})");
            }
            if (item.LineDiscount > 0)
            {
                sb.AppendLine($"  Disc: -Rs.{item.LineDiscount:N2}");
            }
        }

        sb.AppendLine(new string('=', width));
        sb.AppendLine(Row("Subtotal:", $"Rs.{subtotal:N2}"));
        if (discount > 0) sb.AppendLine(Row("Total Discount:", $"-Rs.{discount:N2}"));
        if (tax > 0) sb.AppendLine(Row("Sales Tax (GST):", $"Rs.{tax:N2}"));
        sb.AppendLine(new string('-', width));
        sb.AppendLine(Row("GRAND TOTAL:", $"Rs.{grandTotal:N2}"));
        sb.AppendLine(new string('=', width));
        sb.AppendLine(Row("Cash Tendered:", $"Rs.{tendered:N2}"));
        sb.AppendLine(Row("Change Due:", $"Rs.{change:N2}"));
        sb.AppendLine(new string('-', width));

        if (!string.IsNullOrWhiteSpace(fbrInvoiceNo))
        {
            sb.AppendLine(Center($"FBR POS Invoice No: {fbrInvoiceNo}"));
            sb.AppendLine(Center("[ FBR VERIFICATION QR CODE ]"));
        }

        sb.AppendLine();
        sb.AppendLine(Center("Thank you for shopping with us!"));
        sb.AppendLine(Center("شکریہ! دوبارہ تشریف لائیں"));
        sb.AppendLine(Center("Software by SMARTPOS Enterprise"));
        sb.AppendLine(Center("=========================================="));

        return sb.ToString();
    }
}
