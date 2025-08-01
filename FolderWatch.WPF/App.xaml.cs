using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FolderWatch.WPF.Services;
using FolderWatch.WPF.ViewModels;
using MahApps.Metro.Controls.Dialogs;

namespace FolderWatch.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// Main application class with dependency injection setup
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    
    /// <summary>
    /// Flag to indicate if the application is in the process of shutting down
    /// </summary>
    public bool IsShuttingDown { get; set; }

    /// <summary>
    /// Gets the current application instance cast to App
    /// </summary>
    public static new App Current => (App)Application.Current;

    /// <summary>
    /// Gets the service provider for dependency injection
    /// </summary>
    public IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("Services not initialized");

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Initialize dependency injection container
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        await _host.StartAsync();

        // Create and show the main window
        var mainWindow = GetService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        // Set shutdown flag to inform other components
        IsShuttingDown = true;
        
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    /// <summary>
    /// Configures services for dependency injection
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    private void ConfigureServices(IServiceCollection services)
    {
        // Register MahApps.Metro dialog coordinator
        services.AddSingleton<IDialogCoordinator, DialogCoordinator>();

        // Register application services
        services.AddSingleton<IRuleService, RuleService>();
        services.AddSingleton<IFileMonitorService, FileMonitorService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ISettingsService, SettingsService>();

        // Register ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Register the main window
        services.AddSingleton<MainWindow>();
    }

    /// <summary>
    /// Gets a service of the specified type
    /// </summary>
    /// <typeparam name="T">Type of service to get</typeparam>
    /// <returns>Service instance</returns>
    public T GetService<T>() where T : class => Services.GetRequiredService<T>();
}

