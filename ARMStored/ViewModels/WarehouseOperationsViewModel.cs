using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ARMStored.Models;
using ARMStored.Services;

namespace ARMStored.ViewModels;

public class WarehouseOperationsViewModel : BaseViewModel
{
    private ObservableCollection<WarehouseOperation> _operations = new();
    private ObservableCollection<Product> _products = new();
    private WarehouseOperation? _selectedOperation;
    private WarehouseOperation? _newOperation;
    private bool _isAdding;
    private DateTime _filterFrom;
    private DateTime _filterTo;

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

    public WarehouseOperation? NewOperation
    {
        get => _newOperation;
        set => SetProperty(ref _newOperation, value);
    }

    public bool IsAdding
    {
        get => _isAdding;
        set => SetProperty(ref _isAdding, value);
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

    public Array OperationTypes => Enum.GetValues(typeof(OperationType));

    public ICommand AddOperationCommand { get; }
    public ICommand SaveOperationCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand RefreshCommand { get; }

    private readonly DatabaseService _dbService;

    public WarehouseOperationsViewModel(DatabaseService dbService)
    {
        _dbService = dbService;
        FilterFrom = DateTime.Now.AddMonths(-1);
        FilterTo = DateTime.Now;

        AddOperationCommand = new RelayCommand(_ => ExecuteAdd());
        SaveOperationCommand = new RelayCommand(_ => ExecuteSave(), _ => CanSave());
        CancelCommand = new RelayCommand(_ => ExecuteCancel());
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
        NewOperation = new WarehouseOperation
        {
            OperationDate = DateTime.Now,
            Type = OperationType.Incoming,
            UnitPrice = 0
        };
        IsAdding = true;
    }

    private bool CanSave()
    {
        return NewOperation != null &&
               NewOperation.ProductId > 0 &&
               NewOperation.Quantity > 0 &&
               NewOperation.UnitPrice >= 0;
    }

    private void ExecuteSave()
    {
        try
        {
            var product = Products.FirstOrDefault(p => p.Id == NewOperation!.ProductId);
            if (product == null)
            {
                MessageBox.Show("Выберите товар!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if ((NewOperation!.Type == OperationType.Outgoing || NewOperation.Type == OperationType.WriteOff)
                && product.Quantity < NewOperation.Quantity)
            {
                MessageBox.Show($"Недостаточно товара на складе! Доступно: {product.Quantity}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _dbService.AddOperation(NewOperation, AuthService.CurrentUser!.Id);
            MessageBox.Show("✅ Операция выполнена успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            IsAdding = false;
            LoadProducts();
            LoadOperations();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExecuteCancel()
    {
        IsAdding = false;
        NewOperation = null;
    }
}