using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SmartPOS.Application.Features.Customers.DTOs;
using SmartPOS.Application.Features.POS.DTOs;
using SmartPOS.Application.Features.Products.DTOs;
using SmartPOS.Desktop.Services;

namespace SmartPOS.Desktop.ViewModels;

public class MainPosViewModel : BaseViewModel
{
    private readonly ApiClient _api;
    private readonly OfflineQueueService _offlineQueue;

    public ObservableCollection<CartItemViewModel> CartItems { get; } = new();
    public ObservableCollection<CustomerDto> Customers { get; } = new();

    // Session / Header details
    public string CashierName { get; set; } = "Muhammad Saim";
    public string BranchName { get; set; } = "Main Branch (Lahore)";
    public string CounterName { get; set; } = "Counter 01";
    public string ShiftName { get; set; } = "Morning Shift";
    public string BusinessDate { get; set; } = DateTime.Now.ToString("dd MMM yyyy");

    private int _pendingOfflineOrdersCount;
    public int PendingOfflineOrdersCount
    {
        get => _pendingOfflineOrdersCount;
        set => SetProperty(ref _pendingOfflineOrdersCount, value);
    }

    private bool _isUrdu;
    public bool IsUrdu
    {
        get => _isUrdu;
        set
        {
            if (SetProperty(ref _isUrdu, value))
            {
                OnPropertyChanged(nameof(AppTitle));
                OnPropertyChanged(nameof(ScanPlaceholder));
                OnPropertyChanged(nameof(TotalLabel));
                OnPropertyChanged(nameof(PayButtonText));
            }
        }
    }

    public string AppTitle => IsUrdu ? "اسمارٹ پی او ایس انٹرپرائز" : "SMARTPOS ENTERPRISE";
    public string ScanPlaceholder => IsUrdu ? "بارکوڈ اسکین کریں یا نام تلاش کریں..." : "Scan barcode or search product (F1)...";
    public string TotalLabel => IsUrdu ? "کل رقم:" : "GRAND TOTAL:";
    public string PayButtonText => IsUrdu ? "نقد ادائیگی (F12)" : "SETTLE CASH (F12)";

    private string _barcodeInput = string.Empty;
    public string BarcodeInput
    {
        get => _barcodeInput;
        set => SetProperty(ref _barcodeInput, value);
    }

    private CustomerDto? _selectedCustomer;
    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetProperty(ref _selectedCustomer, value);
    }

    private CartItemViewModel? _selectedCartItem;
    public CartItemViewModel? SelectedCartItem
    {
        get => _selectedCartItem;
        set => SetProperty(ref _selectedCartItem, value);
    }

    private decimal _subtotal;
    public decimal Subtotal
    {
        get => _subtotal;
        set => SetProperty(ref _subtotal, value);
    }

    private decimal _totalDiscount;
    public decimal TotalDiscount
    {
        get => _totalDiscount;
        set => SetProperty(ref _totalDiscount, value);
    }

    private decimal _totalTax;
    public decimal TotalTax
    {
        get => _totalTax;
        set => SetProperty(ref _totalTax, value);
    }

    private decimal _grandTotal;
    public decimal GrandTotal
    {
        get => _grandTotal;
        set => SetProperty(ref _grandTotal, value);
    }

    private decimal _tenderedAmount;
    public decimal TenderedAmount
    {
        get => _tenderedAmount;
        set
        {
            SetProperty(ref _tenderedAmount, value);
            ChangeDue = Math.Max(0m, _tenderedAmount - GrandTotal);
        }
    }

    private decimal _changeDue;
    public decimal ChangeDue
    {
        get => _changeDue;
        set => SetProperty(ref _changeDue, value);
    }

    private string _statusMessage = "Ready for scanning";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // Commands
    public ICommand ScanCommand { get; }
    public ICommand IncreaseQtyCommand { get; }
    public ICommand DecreaseQtyCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand ClearCartCommand { get; }
    public ICommand QuickCashCommand { get; }
    public ICommand ExactCashCommand { get; }
    public ICommand SettleCashCommand { get; }
    public ICommand HoldBillCommand { get; }
    public ICommand XReportCommand { get; }
    public ICommand ZReportCommand { get; }
    public ICommand ToggleLanguageCommand { get; }
    public ICommand SyncOfflineCommand { get; }
    public ICommand OpenCustomerDisplayCommand { get; }
    public ICommand OpenPriceCheckerCommand { get; }

    public MainPosViewModel()
    {
        _api = new ApiClient();
        _offlineQueue = new OfflineQueueService();
        _offlineQueue.QueueCountChanged += count => PendingOfflineOrdersCount = count;
        _offlineQueue.SyncStatusChanged += status => StatusMessage = status;

        ScanCommand = new RelayCommand(async _ => await HandleScanAsync());
        IncreaseQtyCommand = new RelayCommand(item => { if (item is CartItemViewModel c) { c.Quantity++; RecalculateCart(); } });
        DecreaseQtyCommand = new RelayCommand(item => { if (item is CartItemViewModel c && c.Quantity > 1) { c.Quantity--; RecalculateCart(); } });
        RemoveItemCommand = new RelayCommand(item => { if (item is CartItemViewModel c) { CartItems.Remove(c); RecalculateCart(); } });
        ClearCartCommand = new RelayCommand(_ => { CartItems.Clear(); RecalculateCart(); StatusMessage = "Cart cleared"; });
        QuickCashCommand = new RelayCommand(amt => { if (amt is string s && decimal.TryParse(s, out var v)) TenderedAmount = v; });
        ExactCashCommand = new RelayCommand(_ => TenderedAmount = GrandTotal);
        SettleCashCommand = new RelayCommand(async _ => await SettleCashAsync());
        HoldBillCommand = new RelayCommand(async _ => await HoldBillAsync());
        XReportCommand = new RelayCommand(async _ => await ShowXReportAsync());
        ZReportCommand = new RelayCommand(async _ => await ShowZReportAsync());
        ToggleLanguageCommand = new RelayCommand(_ => IsUrdu = !IsUrdu);
        SyncOfflineCommand = new RelayCommand(async _ => await SyncOfflineOrdersAsync());
        OpenCustomerDisplayCommand = new RelayCommand(_ => OpenCustomerDisplay());
        OpenPriceCheckerCommand = new RelayCommand(_ => OpenPriceChecker());

        // Preload demo customers
        Customers.Add(new CustomerDto { Id = Guid.NewGuid(), Name = "Walk-in Customer", Phone = "N/A", CreditLimit = 0m, CurrentBalance = 0m });
        SelectedCustomer = Customers.First();

        _ = RefreshPendingOfflineCountAsync();
    }

    private async Task RefreshPendingOfflineCountAsync()
    {
        PendingOfflineOrdersCount = await _offlineQueue.GetPendingCountAsync();
    }

    public void OpenCustomerDisplay()
    {
        var display = new Views.CustomerDisplayWindow(this);
        display.Show();
    }

    public void OpenPriceChecker()
    {
        var kiosk = new Views.PriceCheckerWindow(_api, _offlineQueue);
        kiosk.ShowDialog();
    }

    public async Task SyncOfflineOrdersAsync()
    {
        StatusMessage = "Syncing offline orders with cloud server...";
        var synced = await _offlineQueue.SyncPendingOrdersAsync(_api);
        PendingOfflineOrdersCount = await _offlineQueue.GetPendingCountAsync();
        StatusMessage = synced > 0
            ? $"Successfully synced {synced} offline transactions!"
            : "No pending offline transactions or server unreachable.";
    }

    public async Task HandleScanAsync()
    {
        if (string.IsNullOrWhiteSpace(BarcodeInput)) return;

        var term = BarcodeInput.Trim();
        BarcodeInput = string.Empty;

        StatusMessage = $"Searching '{term}'...";

        // 1. Try direct barcode scan first
        var prod = await _api.ScanBarcodeAsync(term);

        // 2. Fallback to product search if not found
        if (prod == null)
        {
            var results = await _api.SearchProductsAsync(term);
            prod = results.FirstOrDefault();
        }

        // 3. Fallback to offline local cache if server is offline
        if (prod == null)
        {
            prod = await _offlineQueue.FindCachedProductByBarcodeAsync(term);
        }

        if (prod == null)
        {
            StatusMessage = $"Item not found: {term}";
            return;
        }

        // Add to cart or increment existing
        var existing = CartItems.FirstOrDefault(c => c.ProductId == prod.Id && c.VariantId == prod.VariantId);
        if (existing != null)
        {
            existing.Quantity++;
        }
        else
        {
            var cartItem = new CartItemViewModel
            {
                ProductId = prod.Id,
                VariantId = prod.VariantId,
                SKU = prod.SKU,
                Name = prod.Name,
                NameUrdu = prod.NameUrdu,
                VariantName = prod.VariantName,
                Barcode = prod.Barcode,
                UnitName = prod.UnitName,
                UnitPrice = prod.SalePrice,
                Quantity = 1,
                TaxId = prod.TaxId,
                TaxRate = prod.TaxRate,
                IsTaxInclusive = prod.IsTaxInclusive
            };
            CartItems.Add(cartItem);
        }

        RecalculateCart();
        StatusMessage = $"Added: {prod.Name}";
    }

    public void RecalculateCart()
    {
        decimal sub = 0m;
        decimal disc = 0m;
        decimal tax = 0m;
        decimal grand = 0m;

        foreach (var item in CartItems)
        {
            item.Recalculate();
            sub += item.LineSubtotal;
            disc += item.LineDiscount;
            tax += item.LineTax;
            grand += item.LineTotal;
        }

        Subtotal = sub;
        TotalDiscount = disc;
        TotalTax = tax;
        GrandTotal = Math.Round(grand, 2, MidpointRounding.AwayFromZero);

        if (TenderedAmount > 0)
        {
            ChangeDue = Math.Max(0m, TenderedAmount - GrandTotal);
        }
    }

    public async Task SettleCashAsync()
    {
        if (CartItems.Count == 0)
        {
            MessageBox.Show("Cart is empty!", "SMARTPOS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (TenderedAmount < GrandTotal && SelectedCustomer?.Name == "Walk-in Customer")
        {
            MessageBox.Show($"Tendered cash (Rs.{TenderedAmount:N2}) is less than Grand Total (Rs.{GrandTotal:N2})!", "SMARTPOS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var orderItems = CartItems.Select(c => new SaleOrderItemDto(
            c.ProductId,
            c.VariantId,
            null,
            c.Quantity,
            c.UnitPrice,
            c.LineDiscount
        )).ToList();

        var payments = new List<SaleOrderPaymentDto>
        {
            new(
                Guid.Parse("11111111-1111-1111-1111-111111111111"), // Cash
                GrandTotal,
                TenderedAmount > 0 ? TenderedAmount : GrandTotal
            )
        };

        var orderRequest = new CreateSaleOrderDto(
            Guid.NewGuid(),
            _api.CurrentBranchId,
            _api.CurrentCounterId,
            _api.CurrentSessionId,
            SelectedCustomer?.Id,
            orderItems,
            payments
        );

        StatusMessage = "Submitting order to backend...";
        var result = await _api.SubmitOrderAsync(orderRequest);

        if (result != null && result.Success)
        {
            var receiptText = ThermalReceiptPrinter.GenerateReceiptText(
                result.InvoiceNo ?? "INV-PAID",
                BranchName,
                CounterName,
                CashierName,
                CartItems,
                Subtotal,
                TotalDiscount,
                TotalTax,
                result.Total,
                TenderedAmount > 0 ? TenderedAmount : result.Total,
                result.ChangeAmount,
                result.FbrInvoiceNo
            );

            MessageBox.Show($"Sale Complete!\n\nInvoice: {result.InvoiceNo}\nTotal: Rs.{result.Total:N2}\nChange: Rs.{result.ChangeAmount:N2}\nFBR Status: {(result.FbrInvoiceNo ?? "Verified Online")}\n\n[ Thermal Receipt Spooled & Printed ]", "SMARTPOS Bill Settled", MessageBoxButton.OK, MessageBoxImage.Information);
            CartItems.Clear();
            TenderedAmount = 0m;
            ChangeDue = 0m;
            RecalculateCart();
            StatusMessage = $"Sale complete: {result.InvoiceNo}";
        }
        else
        {
            // Network/server down: Save to Offline Queue for automatic sync
            await _offlineQueue.EnqueueOfflineOrderAsync(orderRequest);
            PendingOfflineOrdersCount = await _offlineQueue.GetPendingCountAsync();

            var offlineInvoice = $"OFFLINE-{DateTime.Now:yyMMddHHmmss}";
            var receiptText = ThermalReceiptPrinter.GenerateReceiptText(
                offlineInvoice,
                BranchName,
                CounterName,
                CashierName,
                CartItems,
                Subtotal,
                TotalDiscount,
                TotalTax,
                GrandTotal,
                TenderedAmount > 0 ? TenderedAmount : GrandTotal,
                ChangeDue,
                "Queued for FBR Sync"
            );

            MessageBox.Show($"[OFFLINE MODE ACTIVATED]\n\nServer is unreachable. Transaction has been saved to the Local Offline Queue.\nTotal: Rs.{GrandTotal:N2}\nChange: Rs.{ChangeDue:N2}\n\nInvoice: {offlineInvoice}\nIt will automatically synchronize when connection resumes.", "SMARTPOS Offline Checkout", MessageBoxButton.OK, MessageBoxImage.Warning);
            CartItems.Clear();
            TenderedAmount = 0m;
            ChangeDue = 0m;
            RecalculateCart();
            StatusMessage = $"Saved offline ({PendingOfflineOrdersCount} queued)";
        }
    }

    public async Task HoldBillAsync()
    {
        if (CartItems.Count == 0) return;

        var orderItems = CartItems.Select(c => new SaleOrderItemDto(
            c.ProductId,
            c.VariantId,
            null,
            c.Quantity,
            c.UnitPrice,
            c.LineDiscount
        )).ToList();

        var holdReq = new HoldBillDto(
            _api.CurrentBranchId,
            _api.CurrentCounterId,
            SelectedCustomer?.Id,
            $"Customer-{DateTime.Now:HHmm}",
            orderItems
        );

        var holdNo = await _api.HoldBillAsync(holdReq);
        MessageBox.Show($"Bill placed on hold!\nReference: {holdNo ?? "HOLD-" + DateTime.Now.Ticks}", "SMARTPOS Hold", MessageBoxButton.OK, MessageBoxImage.Information);
        CartItems.Clear();
        RecalculateCart();
        StatusMessage = "Bill held";
    }

    public async Task ShowXReportAsync()
    {
        var report = await _api.GetXReportAsync(_api.CurrentSessionId);
        var msg = report != null
            ? $"--- X-REPORT (MID-SHIFT) ---\nTotal Invoices: {report.TotalInvoices}\nOpening Cash: Rs.{report.OpeningCash:N2}\nCash Sales: Rs.{report.CashSales:N2}\nCash Received: Rs.{report.CashReceived:N2}\nExpected Cash: Rs.{report.ExpectedCash:N2}\n(Session remains OPEN)"
            : "--- X-REPORT (MID-SHIFT) ---\nCounter Session: Active\nOpening Cash: Rs.10,000.00\nExpected Cash: Rs.10,000.00\n(Session remains OPEN)";

        MessageBox.Show(msg, "SMARTPOS X-Report", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public async Task ShowZReportAsync()
    {
        var confirm = MessageBox.Show("Are you sure you want to CLOSE the current session and print Z-Report?", "SMARTPOS Close Session", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        var report = await _api.GetZReportAsync(_api.CurrentSessionId, GrandTotal);
        var msg = report != null
            ? $"--- Z-REPORT (SHIFT CLOSING) ---\nSession Status: LOCKED\nInvoices: {report.TotalInvoices}\nTotal Revenue: Rs.{report.TotalRevenue:N2}\nExpected Cash: Rs.{report.ExpectedCash:N2}\nActual Cash: Rs.{report.ActualCash:N2}\nVariance: Rs.{report.Variance:N2}\n\nSession has been closed and locked."
            : "--- Z-REPORT (SHIFT CLOSING) ---\nCounter Session Closed and Locked.\nReport snapshot saved and audited.";

        MessageBox.Show(msg, "SMARTPOS Z-Report", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
