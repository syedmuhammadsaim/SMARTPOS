using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartPOS.Application.Features.Returns.DTOs;
using SmartPOS.Desktop.Services;

namespace SmartPOS.Desktop.Views;

public class ReturnItemDisplayViewModel : INotifyPropertyChanged
{
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? BatchId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal OriginalQty { get; set; }
    public decimal EligibleQty { get; set; }

    private decimal _returnQty;
    public decimal ReturnQty
    {
        get => _returnQty;
        set
        {
            if (_returnQty != value)
            {
                _returnQty = value;
                OnPropertyChanged(nameof(ReturnQty));
                OnPropertyChanged(nameof(LineRefund));
            }
        }
    }

    public string Condition { get; set; } = "Restockable";
    public decimal LineRefund => ReturnQty * UnitPrice;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}

public partial class ReturnDialog : Window
{
    private readonly ApiClient _api;
    private OrderReturnEligibilityDto? _currentOrder;
    public ObservableCollection<ReturnItemDisplayViewModel> Items { get; set; } = new();

    public ReturnDialog(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        DgReturnItems.ItemsSource = Items;
    }

    private async void BtnLookup_Click(object sender, RoutedEventArgs e)
    {
        await LookupInvoice();
    }

    private async void TxtInvoiceNo_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await LookupInvoice();
        }
    }

    private async Task LookupInvoice()
    {
        var invNo = TxtInvoiceNo.Text.Trim();
        if (string.IsNullOrEmpty(invNo))
        {
            MessageBox.Show("Please enter an invoice number to look up.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Items.Clear();
        _currentOrder = await _api.GetOrderForReturnAsync(invNo);

        if (_currentOrder == null)
        {
            MessageBox.Show($"Invoice '{invNo}' not found or has no returnable items.", "Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        foreach (var it in _currentOrder.Items)
        {
            Items.Add(new ReturnItemDisplayViewModel
            {
                ProductId = it.ProductId,
                VariantId = it.VariantId,
                BatchId = it.BatchId,
                ProductName = it.ProductName,
                UnitPrice = it.UnitPrice,
                OriginalQty = it.OriginalQty,
                EligibleQty = it.EligibleQty,
                ReturnQty = 0m,
                Condition = "Restockable"
            });
        }

        UpdateEstimatedTotal();
    }

    private void BtnCalculate_Click(object sender, RoutedEventArgs e)
    {
        UpdateEstimatedTotal();
    }

    private void UpdateEstimatedTotal()
    {
        var total = Items.Sum(i => i.ReturnQty * i.UnitPrice);
        LblRefundTotal.Text = $"Rs. {total:N2}";
    }

    private async void BtnConfirmReturn_Click(object sender, RoutedEventArgs e)
    {
        if (_currentOrder == null)
        {
            MessageBox.Show("Please look up a valid order first.", "SMARTPOS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selectedItems = Items.Where(i => i.ReturnQty > 0).ToList();
        if (selectedItems.Count == 0)
        {
            MessageBox.Show("Please specify a return quantity greater than 0 for at least one item.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        foreach (var item in selectedItems)
        {
            if (item.ReturnQty > item.EligibleQty)
            {
                MessageBox.Show($"Return quantity for '{item.ProductName}' cannot exceed eligible quantity of {item.EligibleQty}.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        var refundMethod = (CmbRefundMethod.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Cash";
        var reason = string.IsNullOrWhiteSpace(TxtReason.Text) ? "Customer return" : TxtReason.Text.Trim();

        var request = new CreateSaleReturnRequest(
            OriginalOrderId: _currentOrder.OrderId,
            BranchId: _api.CurrentBranchId,
            CounterId: _api.CurrentCounterId,
            SessionId: _api.CurrentSessionId,
            RefundMethod: refundMethod,
            Reason: reason,
            ManagerOverrideId: null,
            Items: selectedItems.Select(s => new ReturnItemRequest(
                s.ProductId,
                s.VariantId,
                s.BatchId,
                s.ReturnQty,
                s.UnitPrice,
                s.Condition
            )).ToList()
        );

        var response = await _api.ProcessReturnAsync(request);
        if (response != null && response.Success)
        {
            MessageBox.Show($"Return processed successfully!\n\nReturn Invoice: {response.ReturnInvoiceNo}\nRefund: Rs. {response.RefundAmount:N2}\nMethod: {response.RefundMethod}", "Return Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show(response?.Message ?? "Failed to process return. Check manager override or branch authorization.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
