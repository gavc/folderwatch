using System.Windows;
using FolderWatch.WPF.Services;
using FolderWatch.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FolderWatch.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    /// <summary>
    /// Gets the current services provider
    /// </summary>
    public IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("Services not initialized");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Build the host with dependency injection
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        // Start the host
        await _host.StartAsync();

        // Create and show main window
        var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();
        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };

        mainWindow.Show();
        MainWindow = mainWindow;
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    /// <summary>
    /// Configures the dependency injection services
    /// </summary>
    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // Register services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IRuleService, RuleService>();
        services.AddSingleton<IFileMonitorService, FileMonitorService>();
        services.AddSingleton<IThemeService, ThemeService>();

        // Register view models
        services.AddTransient<MainViewModel>();
    }
}