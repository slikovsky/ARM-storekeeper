using System.Windows;
using System.Windows.Controls;

namespace ARMStored.Views;

public partial class ReportsPage : UserControl
{
    public ReportsPage()
    {
        InitializeComponent();

        // Устанавливаем начальное состояние после инициализации компонентов
        SetStockReportState();
    }

    private void StockReport_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ReportsViewModel vm)
            vm.SelectedReportIndex = 0;

        SetStockReportState();
    }

    private void TurnoverReport_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ReportsViewModel vm)
            vm.SelectedReportIndex = 1;

        StockDataGrid.Visibility = Visibility.Collapsed;
        TurnoverDataGrid.Visibility = Visibility.Visible;
        OperationsDataGrid.Visibility = Visibility.Collapsed;
        OperationTypeFilterPanel.Visibility = Visibility.Collapsed;
        DateFilterPanel.Visibility = Visibility.Collapsed;
    }

    private void OperationsReport_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ReportsViewModel vm)
            vm.SelectedReportIndex = 2;

        StockDataGrid.Visibility = Visibility.Collapsed;
        TurnoverDataGrid.Visibility = Visibility.Collapsed;
        OperationsDataGrid.Visibility = Visibility.Visible;
        OperationTypeFilterPanel.Visibility = Visibility.Visible;
        DateFilterPanel.Visibility = Visibility.Visible;
    }

    private void SetStockReportState()
    {
        StockDataGrid.Visibility = Visibility.Visible;
        TurnoverDataGrid.Visibility = Visibility.Collapsed;
        OperationsDataGrid.Visibility = Visibility.Collapsed;
        OperationTypeFilterPanel.Visibility = Visibility.Collapsed;
        DateFilterPanel.Visibility = Visibility.Collapsed;
    }
}