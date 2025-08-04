using System.Configuration;
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FolderWatch.WPF.Services;
using FolderWatch.WPF.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using System.Diagnostics;

namespace FolderWatch.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// Main application class with dependency injection setup and centralized shutdown logic
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;
    private static readonly object _shutdownLock = new();
    private bool _isShuttingDown = false;
    
    /// <summary>
    /// Flag to indicate if the application is in the process of shutting down
    /// </summary>
    public bool IsShuttingDown 
    { 
        get 
        { 
            lock (_shutdownLock) 
            { 
                return _isShuttingDown; 
            } 
        } 
        private set 
        { 
            lock (_shutdownLock) 
            { 
                _isShuttingDown = value; 
            } 
        } 
    }

    /// <summary>
    /// Gets the current application instance cast to App
    /// </summary>
    public static new App Current => (App)System.Windows.Application.Current;

    /// <summary>
    /// Gets the service provider for dependency injection
    /// </summary>
    public IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("Services not initialized");

    protected override async void OnStartup(System.Windows.StartupEventArgs e)
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

    protected override async void OnExit(System.Windows.ExitEventArgs e)
    {
        await PerformShutdownAsync();
        base.OnExit(e);
    }

    /// <summary>
    /// Centralized shutdown method with comprehensive cleanup, logging, and safety timeout
    /// 
    /// This method coordinates the entire application shutdown process:
    /// 1. Sets thread-safe shutdown flag to prevent multiple shutdown attempts
    /// 2. Disposes MainViewModel and all its resources (FileMonitorService, event subscriptions)
    /// 3. Closes MainWindow and disposes tray icon properly
    /// 4. Stops and disposes the dependency injection host
    /// 5. Verifies resource cleanup completion
    /// 6. Implements 10-second safety timeout to force exit if graceful shutdown hangs
    /// 7. Provides comprehensive logging for troubleshooting
    /// 
    /// All exit paths (menu, tray icon, keyboard shortcuts) should use this method
    /// to ensure consistent and reliable application termination.
    /// </summary>
    /// <returns>True if shutdown completed successfully, false if timeout occurred</returns>
    public async Task<bool> InitiateShutdownAsync()
    {
        // Thread-safe check to prevent multiple shutdown attempts
        lock (_shutdownLock)
        {
            if (_isShuttingDown)
            {
                LogShutdown("Shutdown already in progress, ignoring additional request");
                return false;
            }
            _isShuttingDown = true;
        }

        LogShutdown("=== Application shutdown initiated ===");
        
        try
        {
            // Start shutdown process with timeout for safety
            var shutdownTask = PerformShutdownAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10)); // 10 second safety timeout
            
            var completedTask = await Task.WhenAny(shutdownTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                LogShutdown("WARNING: Shutdown timeout reached, forcing application exit");
                Environment.Exit(1);
                return false;
            }
            
            LogShutdown("Graceful shutdown completed, calling Application.Shutdown");
            
            // Ensure we're on the UI thread for Application.Shutdown
            if (Dispatcher.CheckAccess())
            {
                Shutdown(0);
            }
            else
            {
                await Dispatcher.BeginInvoke(() => Shutdown(0));
            }
            
            return true;
        }
        catch (Exception ex)
        {
            LogShutdown($"ERROR during shutdown initiation: {ex.Message}");
            
            // Force exit as last resort
            try
            {
                LogShutdown("Attempting force exit");
                Environment.Exit(1);
            }
            catch
            {
                // Ultimate fallback
                Environment.FailFast("Application shutdown failed completely");
            }
            
            return false;
        }
    }

    /// <summary>
    /// Performs the actual shutdown cleanup operations
    /// </summary>
    private async Task PerformShutdownAsync()
    {
        try
        {
            LogShutdown("Starting resource cleanup");
            
            // Get the main window and dispose its resources
            var mainWindow = GetService<MainWindow>();
            if (mainWindow?.DataContext is IDisposable disposableViewModel)
            {
                LogShutdown("Disposing MainViewModel");
                disposableViewModel.Dispose();
            }
            
            // Dispose of the main window itself (implements IDisposable)
            LogShutdown("Disposing MainWindow");
            if (mainWindow is IDisposable disposableWindow)
            {
                disposableWindow.Dispose();
            }
            else
            {
                mainWindow?.Close();
            }
            
            LogShutdown("Stopping host services");
            
            if (_host is not null)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5)); // Give 5 seconds for graceful shutdown
                LogShutdown("Host stopped successfully");
                
                _host.Dispose();
                LogShutdown("Host disposed successfully");
                _host = null;
            }
            
            // Verify resource cleanup
            await VerifyResourceCleanupAsync();
            
            LogShutdown("=== Shutdown cleanup completed successfully ===");
        }
        catch (Exception ex)
        {
            LogShutdown($"ERROR during shutdown cleanup: {ex.Message}");
            LogShutdown($"Stack trace: {ex.StackTrace}");
            throw; // Re-throw to be handled by the calling method
        }
    }

    /// <summary>
    /// Verifies that critical resources have been properly cleaned up
    /// </summary>
    private async Task VerifyResourceCleanupAsync()
    {
        try
        {
            LogShutdown("Verifying resource cleanup");
            
            // Check for any remaining FileSystemWatcher instances
            var fileMonitorService = _host?.Services.GetService<IFileMonitorService>();
            if (fileMonitorService?.IsMonitoring == true)
            {
                LogShutdown("WARNING: FileMonitorService still monitoring, attempting to stop");
                if (fileMonitorService is IDisposable disposableMonitor)
                {
                    disposableMonitor.Dispose();
                }
            }
            
            // Small delay to allow final cleanup
            await Task.Delay(100);
            
            LogShutdown("Resource cleanup verification completed");
        }
        catch (Exception ex)
        {
            LogShutdown($"Error during resource verification: {ex.Message}");
        }
    }

    /// <summary>
    /// Logs shutdown progress and errors for diagnostics
    /// </summary>
    /// <param name="message">The message to log</param>
    private static void LogShutdown(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] SHUTDOWN: {message}";
        
        // Log to debug output
        Debug.WriteLine(logMessage);
        
        // Also log to console if available
        try
        {
            Console.WriteLine(logMessage);
        }
        catch
        {
            // Ignore console errors during shutdown
        }
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
        services.AddTransient<MainViewModel>();
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

