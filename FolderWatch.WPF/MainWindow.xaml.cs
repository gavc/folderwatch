using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using FolderWatch.WPF.ViewModels;
using MahApps.Metro.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace FolderWatch.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// Handles main window display, tray icon management, and proper resource disposal
/// </summary>
public partial class MainWindow : MetroWindow, IDisposable
{
    private TaskbarIcon? _trayIcon;
    private readonly MainViewModel _viewModel;
    private bool _disposed = false;

    /// <summary>
    /// Initializes the main window with dependency injection and tray icon setup
    /// </summary>
    /// <param name="viewModel">The main view model injected via DI</param>
    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        
        InitializeComponent();
        
        // Set the DataContext to the injected MainViewModel
        DataContext = _viewModel;
        
        // Initialize tray icon programmatically for better disposal control
        InitializeTrayIcon();
    }

    /// <summary>
    /// Initialize the system tray icon programmatically with proper event binding
    /// </summary>
    private void InitializeTrayIcon()
    {
        try
        {
            _trayIcon = new TaskbarIcon
            {
                ToolTipText = "FolderWatch - Click to open",
                Visibility = Visibility.Visible,
                MenuActivation = PopupActivationMode.RightClick
            };

            // Create context menu programmatically
            var contextMenu = new ContextMenu();
            
            // Open menu item
            var openItem = new MenuItem { Header = "Show FolderWatch", FontWeight = FontWeights.Bold };
            openItem.SetBinding(MenuItem.CommandProperty, new System.Windows.Data.Binding("ShowWindowCommand") { Source = _viewModel });
            contextMenu.Items.Add(openItem);
            
            contextMenu.Items.Add(new Separator());
            
            // Start Monitoring menu item
            var startItem = new MenuItem { Header = "Start Monitoring" };
            startItem.SetBinding(MenuItem.CommandProperty, new System.Windows.Data.Binding("StartMonitoringCommand") { Source = _viewModel });
            startItem.SetBinding(MenuItem.IsEnabledProperty, new System.Windows.Data.Binding("CanStartMonitoring") { Source = _viewModel });
            contextMenu.Items.Add(startItem);
            
            // Stop Monitoring menu item
            var stopItem = new MenuItem { Header = "Stop Monitoring" };
            stopItem.SetBinding(MenuItem.CommandProperty, new System.Windows.Data.Binding("StopMonitoringCommand") { Source = _viewModel });
            stopItem.SetBinding(MenuItem.IsEnabledProperty, new System.Windows.Data.Binding("CanStopMonitoring") { Source = _viewModel });
            contextMenu.Items.Add(stopItem);
            
            contextMenu.Items.Add(new Separator());
            
            // Settings menu item
            var settingsItem = new MenuItem { Header = "Settings..." };
            settingsItem.SetBinding(MenuItem.CommandProperty, new System.Windows.Data.Binding("ShowSettingsCommand") { Source = _viewModel });
            contextMenu.Items.Add(settingsItem);
            
            // Exit menu item
            var exitItem = new MenuItem { Header = "Exit Application" };
            exitItem.SetBinding(MenuItem.CommandProperty, new System.Windows.Data.Binding("ExitApplicationCommand") { Source = _viewModel });
            contextMenu.Items.Add(exitItem);
            
            _trayIcon.ContextMenu = contextMenu;
            
            // Bind left click command
            _trayIcon.SetBinding(TaskbarIcon.LeftClickCommandProperty, new System.Windows.Data.Binding("ShowWindowCommand") { Source = _viewModel });
            
            System.Diagnostics.Debug.WriteLine("Tray icon initialized successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing tray icon: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows and activates the window (brings it to front if minimized)
    /// </summary>
    public void ShowAndActivate()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Show();
        Activate();
        Focus();
    }

    /// <summary>
    /// Handles the window closing event to minimize to tray or perform actual shutdown
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        // Check if this is an explicit shutdown request
        if (System.Windows.Application.Current is App app && app.IsShuttingDown)
        {
            System.Diagnostics.Debug.WriteLine("Window closing during application shutdown");
            
            // Application is shutting down, clean up resources
            DisposeTrayIcon();
            
            // Don't cancel the closing
            base.OnClosing(e);
            return;
        }

        // For normal window close (X button), minimize to tray instead
        System.Diagnostics.Debug.WriteLine("Window close requested, minimizing to tray");
        e.Cancel = true;
        Hide();
    }

    /// <summary>
    /// Properly dispose of the tray icon and all event subscriptions
    /// </summary>
    private void DisposeTrayIcon()
    {
        try
        {
            if (_trayIcon is not null)
            {
                System.Diagnostics.Debug.WriteLine("Disposing tray icon");
                
                // Clear event handlers to prevent memory leaks
                _trayIcon.LeftClickCommand = null;
                _trayIcon.ContextMenu = null;
                
                // Dispose the tray icon
                _trayIcon.Dispose();
                _trayIcon = null;
                
                System.Diagnostics.Debug.WriteLine("Tray icon disposed successfully");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error disposing tray icon: {ex.Message}");
        }
    }

    /// <summary>
    /// Implements IDisposable for proper resource cleanup
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            System.Diagnostics.Debug.WriteLine("Disposing MainWindow");
            
            DisposeTrayIcon();
            
            // Dispose the view model if it implements IDisposable
            if (_viewModel is IDisposable disposableViewModel)
            {
                disposableViewModel.Dispose();
            }
            
            _disposed = true;
            System.Diagnostics.Debug.WriteLine("MainWindow disposed successfully");
        }
        
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Handles the window state changed event
    /// </summary>
    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            // Hide from taskbar when minimized
            ShowInTaskbar = false;
        }
        else
        {
            // Show in taskbar when restored
            ShowInTaskbar = true;
        }

        base.OnStateChanged(e);
    }
}