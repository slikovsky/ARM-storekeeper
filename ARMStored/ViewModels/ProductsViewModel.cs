using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ARMStored.Models;
using ARMStored.Services;

namespace ARMStored.ViewModels;

public class ProductsViewModel : BaseViewModel
{
    private ObservableCollection<Product> _products = new();
    private ObservableCollection<Category> _categories = new();
    private ObservableCollection<Supplier> _suppliers = new();
    private Product? _selectedProduct;
    private string _searchText = string.Empty;
    private bool _isEditing;

    public ObservableCollection<Product> Products
    {
        get => _products;
        set => SetProperty(ref _products, value);
    }

    public ObservableCollection<Category> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    public ObservableCollection<Supplier> Suppliers
    {
        get => _suppliers;
        set => SetProperty(ref _suppliers, value);
    }

    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            SetProperty(ref _selectedProduct, value);
            IsEditing = value != null;
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            FilterProducts();
        }
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public bool IsAdmin => AuthService.IsAdmin;

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand RefreshCommand { get; }

    private readonly DatabaseService _dbService;
    private List<Product> _allProducts = new();

    public ProductsViewModel(DatabaseService dbService)
    {
        _dbService = dbService;

        AddCommand = new RelayCommand(_ => ExecuteAdd());
        EditCommand = new RelayCommand(_ => ExecuteEdit(), _ => SelectedProduct != null);
        SaveCommand = new RelayCommand(_ => ExecuteSave(), _ => CanSave());  // ← ИСПРАВЛЕНО: без параметра
        DeleteCommand = new RelayCommand(_ => ExecuteDelete(_), _ => SelectedProduct != null && IsAdmin);
        CancelCommand = new RelayCommand(_ => ExecuteCancel());
        RefreshCommand = new RelayCommand(_ => LoadData());

        LoadData();
    }

    private void LoadData()
    {
        _allProducts = _dbService.GetAllProducts();
        Products = new ObservableCollection<Product>(_allProducts);
        Categories = new ObservableCollection<Category>(_dbService.GetAllCategories());
        Suppliers = new ObservableCollection<Supplier>(_dbService.GetAllSuppliers());
        SelectedProduct = null;
    }

    private void FilterProducts()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Products = new ObservableCollection<Product>(_allProducts);
        }
        else
        {
            var filtered = _allProducts.Where(p =>
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (p.Article?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Barcode?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                p.CategoryName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            ).ToList();
            Products = new ObservableCollection<Product>(filtered);
        }
    }

    private void ExecuteAdd()
    {
        SelectedProduct = new Product
        {
            CategoryId = Categories.FirstOrDefault()?.Id ?? 0,
            SupplierId = Suppliers.FirstOrDefault()?.Id ?? 0,
            Unit = "шт",
            MinQuantity = 10,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        IsEditing = true;
    }

    private void ExecuteEdit() { }

    private bool CanSave()
    {
        return SelectedProduct != null &&
               !string.IsNullOrWhiteSpace(SelectedProduct.Name) &&
               SelectedProduct.Price >= 0 &&
               SelectedProduct.Quantity >= 0;
    }

    private void ExecuteSave()  // ← ИСПРАВЛЕНО: убран параметр object?
    {
        try
        {
            if (SelectedProduct!.Id == 0)
            {
                _dbService.AddProduct(SelectedProduct);
                MessageBox.Show("✅ Товар успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                _dbService.UpdateProduct(SelectedProduct);
                MessageBox.Show("✅ Товар успешно обновлён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExecuteDelete(object? obj)  // ← Оставлен параметр для совместимости
    {
        if (MessageBox.Show($"Удалить товар '{SelectedProduct?.Name}'?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            try
            {
                _dbService.DeleteProduct(SelectedProduct!.Id);
                MessageBox.Show("✅ Товар удалён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExecuteCancel()
    {
        SelectedProduct = null;
        LoadData();
    }
}