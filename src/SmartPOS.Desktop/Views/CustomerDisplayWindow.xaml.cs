using System.Windows;
using SmartPOS.Desktop.ViewModels;

namespace SmartPOS.Desktop.Views;

public partial class CustomerDisplayWindow : Window
{
    public CustomerDisplayWindow(MainPosViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
