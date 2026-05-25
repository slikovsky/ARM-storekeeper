using System.Windows;
using ARMStored.ViewModels;

namespace ARMStored.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}