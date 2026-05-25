using System.Windows;
using System.Windows.Controls;

namespace ARMStored.Views;

public partial class ReportsPage : UserControl
{
    public ReportsPage()
    {
        InitializeComponent();
    }

    private void StockReport_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ReportsViewModel vm)
            vm.SelectedReportIndex = 0;

        StockDataGrid.Visibility = Visibility.Visible;
        TurnoverDataGrid.Visibility = Visibility.Collapsed;
        OperationsDataGrid.Visibility = Visibility.Collapsed;
        OperationTypeFilterPanel.Visibility = Visibility.Collapsed; // Скрываем фильтр типа операции
    }

    private void TurnoverReport_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ReportsViewModel vm)
            vm.SelectedReportIndex = 1;

        StockDataGrid.Visibility = Visibility.Collapsed;
        TurnoverDataGrid.Visibility = Visibility.Visible;
        OperationsDataGrid.Visibility = Visibility.Collapsed;
        OperationTypeFilterPanel.Visibility = Visibility.Collapsed; // Скрываем фильтр типа операции
    }

    private void OperationsReport_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ReportsViewModel vm)
            vm.SelectedReportIndex = 2;

        StockDataGrid.Visibility = Visibility.Collapsed;
        TurnoverDataGrid.Visibility = Visibility.Collapsed;
        OperationsDataGrid.Visibility = Visibility.Visible;
        OperationTypeFilterPanel.Visibility = Visibility.Visible; // Показываем фильтр типа операции
    }
}
