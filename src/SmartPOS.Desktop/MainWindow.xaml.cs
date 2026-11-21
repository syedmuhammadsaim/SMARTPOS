using System.Windows;
using System.Windows.Input;
using SmartPOS.Desktop.ViewModels;

namespace SmartPOS.Desktop;

public partial class MainWindow : Window
{
    private MainPosViewModel ViewModel => (MainPosViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => TxtBarcode.Focus();
    }

    private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ViewModel.ScanCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F1:
                TxtBarcode.Focus();
                e.Handled = true;
                break;
            case Key.F2:
                ViewModel.HoldBillCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F3:
                ViewModel.ClearCartCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F4:
                ViewModel.XReportCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F5:
                ViewModel.ZReportCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F12:
                ViewModel.SettleCashCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
