using System.Collections.ObjectModel;
using System.Windows;
using ARMStored.Models;

namespace ARMStored.Views;

public partial class ProductDialog : Window
{
    public Product Product { get; private set; }
    public bool IsSaved { get; private set; }

    public ProductDialog(Product product, List<Category> categories, List<Supplier> suppliers)
    {
        InitializeComponent();
        Product = product;

        // Создаём ViewModel для диалога
        var viewModel = new ProductDialogViewModel
        {
            Product = product,
            Categories = new ObservableCollection<Category>(categories),
            Suppliers = new ObservableCollection<Supplier>(suppliers)
        };

        DataContext = viewModel;

        // Устанавливаем заголовок
        TitleText.Text = product.Id == 0 ? "➕ Добавление товара" : "✏️ Редактирование товара";
        Title = product.Id == 0 ? "Добавление товара" : "Редактирование товара";
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as ProductDialogViewModel;
        if (vm == null) return;

        // Валидация
        if (string.IsNullOrWhiteSpace(vm.Product.Name))
        {
            MessageBox.Show("Введите название товара!", "Ошибка", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (vm.Product.Price < 0)
        {
            MessageBox.Show("Цена не может быть отрицательной!", "Ошибка", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (vm.Product.Quantity < 0)
        {
            MessageBox.Show("Количество не может быть отрицательным!", "Ошибка", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsSaved = true;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsSaved = false;
        DialogResult = false;
        Close();
    }
}

// ViewModel для диалога товара
public class ProductDialogViewModel
{
    public Product Product { get; set; } = null!;
    public ObservableCollection<Category> Categories { get; set; } = new();
    public ObservableCollection<Supplier> Suppliers { get; set; } = new();
}
