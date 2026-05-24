using System.Windows;
using System.Windows.Controls;
using ARMStored.ViewModels;

namespace ARMStored.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        PasswordBox.PasswordChanged += (s, e) =>
        {
            viewModel.Password = PasswordBox.Password;
        };
    }
}