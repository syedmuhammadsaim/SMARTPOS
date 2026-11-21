using System.Text;

namespace SmartPOS.Desktop.Services;

/// <summary>
/// Generates ESC/POS byte commands for 80mm/58mm thermal receipt printers and cash drawers.
/// </summary>
public class EscPosCommandBuilder
{
    private readonly List<byte> _buffer = new();

    // Standard ESC/POS Command Constants
    private static readonly byte[] EscInit = { 0x1B, 0x40 }; // ESC @
    private static readonly byte[] AlignLeft = { 0x1B, 0x61, 0x00 }; // ESC a 0
    private static readonly byte[] AlignCenter = { 0x1B, 0x61, 0x01 }; // ESC a 1
    private static readonly byte[] AlignRight = { 0x1B, 0x61, 0x02 }; // ESC a 2
    private static readonly byte[] BoldOn = { 0x1B, 0x45, 0x01 }; // ESC E 1
    private static readonly byte[] BoldOff = { 0x1B, 0x45, 0x00 }; // ESC E 0
    private static readonly byte[] DoubleHeightOn = { 0x1B, 0x21, 0x10 };
    private static readonly byte[] DoubleWidthOn = { 0x1B, 0x21, 0x20 };
    private static readonly byte[] NormalText = { 0x1B, 0x21, 0x00 };
    private static readonly byte[] FeedPaper = { 0x1B, 0x64, 0x03 }; // Feed 3 lines
    private static readonly byte[] CutPaper = { 0x1D, 0x56, 0x42, 0x00 }; // GS V 66 0
    private static readonly byte[] OpenDrawer = { 0x1B, 0x70, 0x00, 0x19, 0xFA }; // ESC p 0 25 250

    public EscPosCommandBuilder Initialize()
    {
        _buffer.AddRange(EscInit);
        return this;
    }

    public EscPosCommandBuilder SetAlign(Alignment align)
    {
        _buffer.AddRange(align switch
        {
            Alignment.Center => AlignCenter,
            Alignment.Right => AlignRight,
            _ => AlignLeft
        });
        return this;
    }

    public EscPosCommandBuilder SetBold(bool enabled)
    {
        _buffer.AddRange(enabled ? BoldOn : BoldOff);
        return this;
    }

    public EscPosCommandBuilder SetDoubleSize(bool enabled)
    {
        _buffer.AddRange(enabled ? new byte[] { 0x1B, 0x21, 0x30 } : NormalText);
        return this;
    }

    public EscPosCommandBuilder PrintText(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        _buffer.AddRange(bytes);
        return this;
    }

    public EscPosCommandBuilder PrintLine(string text = "")
    {
        if (!string.IsNullOrEmpty(text))
        {
            PrintText(text);
        }
        _buffer.AddRange(new byte[] { 0x0A }); // LF
        return this;
    }

    public EscPosCommandBuilder PrintQrCode(string qrContent)
    {
        // ESC/POS native QR Code Model 2 print commands
        var qrBytes = Encoding.UTF8.GetBytes(qrContent);
        int length = qrBytes.Length + 3;
        byte pL = (byte)(length % 256);
        byte pH = (byte)(length / 256);

        // Function 165: QR Code Model
        _buffer.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00 });
        // Function 167: QR Code Module Size (5 dots)
        _buffer.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x05 });
        // Function 169: Error Correction Level (Level M = 49)
        _buffer.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x31 });
        // Function 180: Store data in symbol storage area
        _buffer.AddRange(new byte[] { 0x1D, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30 });
        _buffer.AddRange(qrBytes);
        // Function 181: Print the symbol data
        _buffer.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 });

        return this;
    }

    public EscPosCommandBuilder KickCashDrawer()
    {
        _buffer.AddRange(OpenDrawer);
        return this;
    }

    public EscPosCommandBuilder Cut()
    {
        _buffer.AddRange(FeedPaper);
        _buffer.AddRange(CutPaper);
        return this;
    }

    public byte[] Build() => _buffer.ToArray();

    public enum Alignment
    {
        Left,
        Center,
        Right
    }
}
