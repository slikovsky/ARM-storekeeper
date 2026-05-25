using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ARMStored.Services;
using ARMStored.ViewModels;
using ARMStored.Views;

namespace ARMStored;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        // Устанавливаем русскую культуру для всего приложения
        var russianCulture = new CultureInfo("ru-RU");
        CultureInfo.CurrentCulture = russianCulture;
        CultureInfo.CurrentUICulture = russianCulture;
        CultureInfo.DefaultThreadCurrentCulture = russianCulture;
        CultureInfo.DefaultThreadCurrentUICulture = russianCulture;

        // Устанавливаем культуру для XAML-привязок (важно для StringFormat=C)
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(
                XmlLanguage.GetLanguage(russianCulture.IetfLanguageTag)));

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Сервисы
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<AuthService>();
        services.AddSingleton<ReportService>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<WarehouseOperationsViewModel>();
        services.AddTransient<ReportsViewModel>();

        // Views
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();
    }
}