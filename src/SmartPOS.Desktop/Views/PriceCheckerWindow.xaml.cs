using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SmartPOS.Desktop.Services;

namespace SmartPOS.Desktop.Views;

public partial class PriceCheckerWindow : Window
{
    private readonly ApiClient _apiClient;
    private readonly OfflineQueueService _offlineQueue;
    private readonly DispatcherTimer _resetTimer;
    private int _remainingSeconds;

    public PriceCheckerWindow(ApiClient apiClient, OfflineQueueService offlineQueue)
    {
        InitializeComponent();
        _apiClient = apiClient;
        _offlineQueue = offlineQueue;

        _resetTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _resetTimer.Tick += ResetTimer_Tick;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        TxtBarcode.Focus();
    }

    private void TxtBarcode_LostFocus(object sender, RoutedEventArgs e)
    {
        // Maintain scanner keyboard focus indefinitely in kiosk mode
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => TxtBarcode.Focus()));
    }

    private async void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            return;
        }

        if (e.Key == Key.Enter)
        {
            var barcode = TxtBarcode.Text.Trim();
            TxtBarcode.Clear();
            if (string.IsNullOrWhiteSpace(barcode)) return;

            await CheckPriceAsync(barcode);
        }
    }

    private async Task CheckPriceAsync(string barcode)
    {
        // 1. Try Live API scan first
        var product = await _apiClient.ScanBarcodeAsync(barcode);

        // 2. Fallback to cached catalog if offline
        product ??= await _offlineQueue.FindCachedProductByBarcodeAsync(barcode);

        if (product != null)
        {
            TxtProductName.Text = product.Name + (!string.IsNullOrWhiteSpace(product.VariantName) ? $" ({product.VariantName})" : "");
            TxtProductCategory.Text = $"Category: {product.CategoryName} | SKU: {product.SKU}";
            TxtPrice.Text = $"Rs. {product.SalePrice:N2}";
            TxtStockStatus.Text = product.AvailableStock > 0
                ? $"✔ In Stock ({product.AvailableStock:N0} available) • اسٹاک میں موجود ہے"
                : "⚠ Out of Stock • اسٹاک ختم ہے";

            PnlPrompt.Visibility = Visibility.Collapsed;
            PnlResult.Visibility = Visibility.Visible;

            // Start 8-second reset countdown
            _remainingSeconds = 8;
            TxtResetTimer.Text = $"Screen resets in {_remainingSeconds}s...";
            _resetTimer.Stop();
            _resetTimer.Start();
        }
        else
        {
            MessageBox.Show($"Product with barcode '{barcode}' was not found in catalog.", "Item Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ResetTimer_Tick(object? sender, EventArgs e)
    {
        _remainingSeconds--;
        if (_remainingSeconds <= 0)
        {
            _resetTimer.Stop();
            PnlResult.Visibility = Visibility.Collapsed;
            PnlPrompt.Visibility = Visibility.Visible;
        }
        else
        {
            TxtResetTimer.Text = $"Screen resets in {_remainingSeconds}s...";
        }
    }
}
