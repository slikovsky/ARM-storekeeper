using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ARMStored.Models;
using ARMStored.Services;

namespace ARMStored.ViewModels;

public class ReportsViewModel : BaseViewModel
{
    private ObservableCollection<StockReportRow> _stockReport = new();
    private ObservableCollection<TurnoverReportRow> _turnoverReport = new();
    private ObservableCollection<OperationReportRow> _operationsReport = new();

    private DateTime _filterFrom;
    private DateTime _filterTo;
    private int? _selectedOperationType;
    private int _selectedReportIndex;
    private string _statusMessage = string.Empty;

    public ObservableCollection<StockReportRow> StockReport
    {
        get => _stockReport;
        set => SetProperty(ref _stockReport, value);
    }

    public ObservableCollection<TurnoverReportRow> TurnoverReport
    {
        get => _turnoverReport;
        set => SetProperty(ref _turnoverReport, value);
    }

    public ObservableCollection<OperationReportRow> OperationsReport
    {
        get => _operationsReport;
        set => SetProperty(ref _operationsReport, value);
    }

    public DateTime FilterFrom
    {
        get => _filterFrom;
        set
        {
            SetProperty(ref _filterFrom, value);
            LoadCurrentReport();
        }
    }

    public DateTime FilterTo
    {
        get => _filterTo;
        set
        {
            SetProperty(ref _filterTo, value);
            LoadCurrentReport();
        }
    }

    public int? SelectedOperationType
    {
        get => _selectedOperationType;
        set
        {
            SetProperty(ref _selectedOperationType, value);
            if (SelectedReportIndex == 2)
                LoadOperationsReport();
        }
    }

    public int SelectedReportIndex
    {
        get => _selectedReportIndex;
        set
        {
            SetProperty(ref _selectedReportIndex, value);
            LoadCurrentReport();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public Array OperationTypes => new[]
    {
        new { Value = (int?)null, Name = "Все типы" },
        new { Value = (int?)1, Name = "📥 Приход" },
        new { Value = (int?)2, Name = "📤 Расход" },
        new { Value = (int?)3, Name = "🔄 Перемещение" },
        new { Value = (int?)4, Name = "❌ Списание" }
    };

    public ICommand RefreshCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand ExportExcelCommand { get; }

    private readonly ReportService _reportService;

    public ReportsViewModel(ReportService reportService)
    {
        _reportService = reportService;
        FilterFrom = DateTime.Now.AddMonths(-1);
        FilterTo = DateTime.Now;
        SelectedReportIndex = 0;

        RefreshCommand = new RelayCommand(_ => LoadCurrentReport());
        ExportCsvCommand = new RelayCommand(_ => ExportToCsv(), _ => CanExport());
        ExportExcelCommand = new RelayCommand(_ => ExportToExcel(), _ => CanExport());

        LoadCurrentReport();
    }

    private void LoadCurrentReport()
    {
        StatusMessage = string.Empty;
        switch (SelectedReportIndex)
        {
            case 0: LoadStockReport(); break;
            case 1: LoadTurnoverReport(); break;
            case 2: LoadOperationsReport(); break;
        }
    }

    private void LoadStockReport()
    {
        try
        {
            var data = _reportService.GetStockReport();
            StockReport = new ObservableCollection<StockReportRow>(data);
            StatusMessage = $"Загружено записей: {data.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки: {ex.Message}";
        }
    }

    private void LoadTurnoverReport()
    {
        try
        {
            var data = _reportService.GetTurnoverReport(FilterFrom, FilterTo);
            TurnoverReport = new ObservableCollection<TurnoverReportRow>(data);
            StatusMessage = $"Период: {FilterFrom:dd.MM.yyyy} - {FilterTo:dd.MM.yyyy}, записей: {data.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки: {ex.Message}";
        }
    }

    private void LoadOperationsReport()
    {
        try
        {
            var data = _reportService.GetOperationsReport(FilterFrom, FilterTo, SelectedOperationType);
            OperationsReport = new ObservableCollection<OperationReportRow>(data);
            StatusMessage = $"Период: {FilterFrom:dd.MM.yyyy} - {FilterTo:dd.MM.yyyy}, записей: {data.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки: {ex.Message}";
        }
    }

    private bool CanExport()
    {
        return SelectedReportIndex switch
        {
            0 => StockReport.Count > 0,
            1 => TurnoverReport.Count > 0,
            2 => OperationsReport.Count > 0,
            _ => false
        };
    }

    private void ExportToCsv()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
            DefaultExt = "csv"
        };

        string reportName = GetReportName();
        dialog.FileName = _reportService.GetDefaultFileName(reportName, "csv");

        if (dialog.ShowDialog() == true)
        {
            try
            {
                switch (SelectedReportIndex)
                {
                    case 0: _reportService.ExportToCsv(StockReport.ToList(), dialog.FileName); break;
                    case 1: _reportService.ExportToCsv(TurnoverReport.ToList(), dialog.FileName); break;
                    case 2: _reportService.ExportToCsv(OperationsReport.ToList(), dialog.FileName); break;
                }
                StatusMessage = $"✅ Сохранено: {dialog.FileName}";
                MessageBox.Show("Отчёт успешно экспортирован в CSV!", "Экспорт завершён",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExportToExcel()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel файлы (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
            DefaultExt = "xlsx"
        };

        string reportName = GetReportName();
        dialog.FileName = _reportService.GetDefaultFileName(reportName, "xlsx");

        if (dialog.ShowDialog() == true)
        {
            try
            {
                string sheetName = SelectedReportIndex switch
                {
                    0 => "Остатки на складе",
                    1 => "Оборот товаров",
                    2 => "Журнал операций",
                    _ => "Отчёт"
                };

                switch (SelectedReportIndex)
                {
                    case 0: _reportService.ExportToExcel(StockReport.ToList(), dialog.FileName, sheetName); break;
                    case 1: _reportService.ExportToExcel(TurnoverReport.ToList(), dialog.FileName, sheetName); break;
                    case 2: _reportService.ExportToExcel(OperationsReport.ToList(), dialog.FileName, sheetName); break;
                }
                StatusMessage = $"✅ Сохранено: {dialog.FileName}";
                MessageBox.Show("Отчёт успешно экспортирован в Excel!", "Экспорт завершён",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private string GetReportName()
    {
        return SelectedReportIndex switch
        {
            0 => "Остатки",
            1 => "Обороты",
            2 => "Операции",
            _ => "Отчет"
        };
    }
}