using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ARMStored.Models;
using ARMStored.Services;
using ARMStored.Views;

namespace ARMStored.ViewModels;

public class WarehouseOperationsViewModel : BaseViewModel
{
    private ObservableCollection<WarehouseOperation> _operations = new();
    private ObservableCollection<Product> _products = new();
    private WarehouseOperation? _selectedOperation;
    private DateTime _filterFrom;
    private DateTime _filterTo;
    private int? _selectedOperationType;

    public ObservableCollection<WarehouseOperation> Operations
    {
        get => _operations;
        set => SetProperty(ref _operations, value);
    }

    public ObservableCollection<Product> Products
    {
        get => _products;
        set => SetProperty(ref _products, value);
    }

    public WarehouseOperation? SelectedOperation
    {
        get => _selectedOperation;
        set => SetProperty(ref _selectedOperation, value);
    }

    public DateTime FilterFrom
    {
        get => _filterFrom;
        set
        {
            SetProperty(ref _filterFrom, value);
            LoadOperations();
        }
    }

    public DateTime FilterTo
    {
        get => _filterTo;
        set
        {
            SetProperty(ref _filterTo, value);
            LoadOperations();
        }
    }

    public int? SelectedOperationType
    {
        get => _selectedOperationType;
        set
        {
            SetProperty(ref _selectedOperationType, value);
            LoadOperations();
        }
    }

    public Array OperationTypes => new[] 
    { 
        new { Value = (int?)null, Name = "Все типы" },
        new { Value = (int?)1, Name = "Приход" },
        new { Value = (int?)2, Name = "Расход" },
        new { Value = (int?)3, Name = "Перемещение" },
        new { Value = (int?)4, Name = "Списание" }
    };

    public ICommand AddOperationCommand { get; }
    public ICommand RefreshCommand { get; }

    private readonly DatabaseService _dbService;

    public WarehouseOperationsViewModel(DatabaseService dbService)
    {
        _dbService = dbService;
        FilterFrom = DateTime.Now.AddMonths(-1);
        FilterTo = DateTime.Now;

        AddOperationCommand = new RelayCommand(_ => ExecuteAdd());
        RefreshCommand = new RelayCommand(_ => LoadOperations());

        LoadProducts();
        LoadOperations();
    }

    private void LoadProducts()
    {
        Products = new ObservableCollection<Product>(_dbService.GetAllProducts());
    }

    private void LoadOperations()
    {
        var allOps = _dbService.GetAllOperations();
        Operations = new ObservableCollection<WarehouseOperation>(
            allOps.Where(o => o.OperationDate >= FilterFrom && o.OperationDate <= FilterTo));
    }

    private void ExecuteAdd()
    {
        var newOperation = new WarehouseOperation
        {
            OperationDate = DateTime.Now,
            Type = OperationType.Incoming,
            UnitPrice = 0
        };

        var dialog = new OperationDialog(newOperation, Products.ToList());
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _dbService.AddOperation(dialog.Operation, AuthService.CurrentUser!.Id);
                MessageBox.Show("✅ Операция выполнена успешно!", "Успех", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadProducts();
                LoadOperations();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
