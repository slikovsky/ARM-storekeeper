using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ARMStored.Services;
using ARMStored.Views;

namespace ARMStored.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            SetProperty(ref _errorMessage, value);
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ICommand LoginCommand { get; }
    public ICommand CloseCommand { get; }

    private readonly AuthService _authService;

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
        LoginCommand = new RelayCommand(ExecuteLogin, _ => CanExecuteLogin());
        CloseCommand = new RelayCommand(_ => Application.Current.Shutdown());
    }

    private bool CanExecuteLogin()
    {
        return !string.IsNullOrWhiteSpace(Username) && 
               !string.IsNullOrWhiteSpace(Password) && 
               !IsLoading;
    }

    private void ExecuteLogin(object? obj)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        if (_authService.Authenticate(Username, Password))
        {
            var mainWindow = App.ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            foreach (Window window in Application.Current.Windows)
            {
                if (window is LoginWindow)
                {
                    window.Close();
                    break;
                }
            }
        }
        else
        {
            ErrorMessage = "❌ Неверный логин или пароль";
        }

        IsLoading = false;
    }
}