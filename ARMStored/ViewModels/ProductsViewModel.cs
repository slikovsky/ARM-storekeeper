using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ARMStored.Models;
using ARMStored.Services;
using ARMStored.Views;

namespace ARMStored.ViewModels;

public class ProductsViewModel : BaseViewModel
{
    private ObservableCollection<Product> _products = new();
    private ObservableCollection<Category> _categories = new ();
    private ObservableCollection<Supplier> _suppliers = new ();
    private Product? _selectedProduct;
    private string _searchText = string.Empty;

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
        set => SetProperty(ref _selectedProduct, value);
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

    public bool IsAdmin => AuthService.IsAdmin;

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }

    private readonly DatabaseService _dbService;
    private List<Product> _allProducts = new();

    public ProductsViewModel(DatabaseService dbService)
    {
        _dbService = dbService;

        AddCommand = new RelayCommand(_ => ExecuteAdd(), _ => IsAdmin);
        EditCommand = new RelayCommand(_ => ExecuteEdit(), _ => SelectedProduct != null);
        DeleteCommand = new RelayCommand(_ => ExecuteDelete(), _ => SelectedProduct != null && IsAdmin);
        RefreshCommand = new RelayCommand(_ => LoadData());

        LoadData();
    }

    private void LoadData()
    {
        _allProducts = _dbService.GetAllProducts();
        Products = new ObservableCollection<Product>(_allProducts);
        Categories = new ObservableCollection< Category > (_dbService.GetAllCategories());
        Suppliers = new ObservableCollection< Supplier > (_dbService.GetAllSuppliers());
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
        var newProduct = new Product
        {
            CategoryId = Categories.FirstOrDefault()?.Id ?? 0,
            SupplierId = Suppliers.FirstOrDefault()?.Id ?? 0,
            Unit = "шт",
            MinQuantity = 10,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var dialog = new ProductDialog(newProduct, Categories.ToList(), Suppliers.ToList());
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            try
            {
                // Проверяем, есть ли товар с таким артикулом (включая удалённые)
                if (!string.IsNullOrEmpty(dialog.Product.Article))
                {
                    var existingProduct = _dbService.GetProductByArticle(dialog.Product.Article);
                    if (existingProduct != null)
                    {
                        if (existingProduct.IsDeleted)
                        {
                            // Восстанавливаем удалённый товар
                            existingProduct.Name = dialog.Product.Name;
                            existingProduct.Barcode = dialog.Product.Barcode;
                            existingProduct.CategoryId = dialog.Product.CategoryId;
                            existingProduct.SupplierId = dialog.Product.SupplierId;
                            existingProduct.Price = dialog.Product.Price;
                            existingProduct.Quantity = dialog.Product.Quantity;
                            existingProduct.MinQuantity = dialog.Product.MinQuantity;
                            existingProduct.Unit = dialog.Product.Unit;
                            existingProduct.Description = dialog.Product.Description;

                            _dbService.RestoreProduct(existingProduct);
                            MessageBox.Show("✅ Товар восстановлен с сохранением истории операций!", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show($"❌ Товар с артикулом '{dialog.Product.Article}' уже существует!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                    else
                    {
                        _dbService.AddProduct(dialog.Product);
                        MessageBox.Show("✅ Товар успешно добавлен!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    _dbService.AddProduct(dialog.Product);
                    MessageBox.Show("✅ Товар успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExecuteEdit()
    {
        if (SelectedProduct == null) return;

        var editProduct = new Product
        {
            Id = SelectedProduct.Id,
            Name = SelectedProduct.Name,
            Article = SelectedProduct.Article,
            Barcode = SelectedProduct.Barcode,
            CategoryId = SelectedProduct.CategoryId,
            SupplierId = SelectedProduct.SupplierId,
            Price = SelectedProduct.Price,
            Quantity = SelectedProduct.Quantity,
            MinQuantity = SelectedProduct.MinQuantity,
            Unit = SelectedProduct.Unit,
            Description = SelectedProduct.Description,
            CreatedAt = SelectedProduct.CreatedAt,
            UpdatedAt = SelectedProduct.UpdatedAt
        };

        var dialog = new ProductDialog(editProduct, Categories.ToList(), Suppliers.ToList());
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _dbService.UpdateProduct(dialog.Product);
                MessageBox.Show("✅ Товар успешно обновлён!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExecuteDelete()
    {
        if (SelectedProduct == null) return;

        if (MessageBox.Show($"Удалить товар '{SelectedProduct.Name}'?\n\nИстория операций будет сохранена. Товар можно восстановить, добавив его снова с тем же артикулом.", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            try
            {
                _dbService.DeleteProduct(SelectedProduct.Id);
                MessageBox.Show("✅ Товар удалён. История операций сохранена.\n\nДля восстановления добавьте товар с тем же артикулом.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}