using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;
using ARMStored.Services;
using ARMStored.Views;

namespace ARMStored.ViewModels;

public class MainViewModel : BaseViewModel
{
    private object? _currentView;
    private string _userName = string.Empty;
    private string _userRole = string.Empty;

    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    public string UserRole
    {
        get => _userRole;
        set => SetProperty(ref _userRole, value);
    }

    public bool IsAdmin => AuthService.IsAdmin;

    public ICommand ShowProductsCommand { get; }
    public ICommand ShowOperationsCommand { get; }
    public ICommand LogoutCommand { get; }

    public MainViewModel()
    {
        UserName = AuthService.CurrentUser?.FullName ?? "Неизвестный";
        UserRole = AuthService.CurrentUser?.Role == "admin" ? "Администратор" : "Кладовщик";

        // Создаём UserControl напрямую, а не ViewModel
        ShowProductsCommand = new RelayCommand(_ => ShowProducts());
        ShowOperationsCommand = new RelayCommand(_ => ShowOperations());
        LogoutCommand = new RelayCommand(_ => ExecuteLogout());

        // Показываем страницу товаров по умолчанию
        ShowProducts();
    }

    private void ShowProducts()
    {
        var page = new ProductsPage();
        page.DataContext = App.ServiceProvider.GetRequiredService<ProductsViewModel>();
        CurrentView = page;
    }

    private void ShowOperations()
    {
        var page = new WarehouseOperationsPage();
        page.DataContext = App.ServiceProvider.GetRequiredService<WarehouseOperationsViewModel>();
        CurrentView = page;
    }

    private void ExecuteLogout()
    {
        AuthService.Logout();
        var loginWindow = App.ServiceProvider.GetRequiredService<LoginWindow>();
        loginWindow.Show();

        foreach (var window in Application.Current.Windows)
        {
            if (window is MainWindow)
            {
                (window as MainWindow)?.Close();
                break;
            }
        }
    }
}