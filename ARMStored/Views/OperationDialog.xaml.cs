using System.Windows;
using ARMStored.Models;

namespace ARMStored.Views;

public partial class OperationDialog : Window
{
    public WarehouseOperation Operation { get; private set; }
    public bool IsSaved { get; private set; }

    public OperationDialog(WarehouseOperation operation, List<Product> products)
    {
        InitializeComponent();
        Operation = operation;
        DataContext = new OperationDialogViewModel
        {
            Operation = operation,
            Products = products,
            OperationTypes = Enum.GetValues(typeof(OperationType)).Cast<OperationType>().ToList()
        };
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as OperationDialogViewModel;
        if (vm == null) return;

        // Валидация
        if (vm.ProductId <= 0)
        {
            MessageBox.Show("Выберите товар!", "Ошибка", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (vm.Quantity <= 0)
        {
            MessageBox.Show("Количество должно быть больше 0!", "Ошибка", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (vm.UnitPrice < 0)
        {
            MessageBox.Show("Цена не может быть отрицательной!", "Ошибка", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Проверка остатка при расходе
        var product = vm.Products.FirstOrDefault(p => p.Id == vm.ProductId);
        if (product != null && (vm.SelectedOperationType == OperationType.Outgoing || vm.SelectedOperationType == OperationType.WriteOff))
        {
            if (product.Quantity < vm.Quantity)
            {
                MessageBox.Show($"Недостаточно товара на складе! Доступно: {product.Quantity}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        Operation.ProductId = vm.ProductId;
        Operation.Type = vm.SelectedOperationType;
        Operation.Quantity = vm.Quantity;
        Operation.UnitPrice = vm.UnitPrice;
        Operation.Notes = vm.Notes;

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

// ViewModel для диалога операции
public class OperationDialogViewModel
{
    public WarehouseOperation Operation { get; set; } = null!;
    public List<Product> Products { get; set; } = new();
    public List<OperationType> OperationTypes { get; set; } = new();

    public OperationType SelectedOperationType { get; set; } = OperationType.Incoming;
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; } = 0;
    public string Notes { get; set; } = string.Empty;
}
