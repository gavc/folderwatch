using System.ComponentModel;
using System.Windows;
using FolderWatch.WPF.ViewModels;
using MahApps.Metro.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace FolderWatch.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MetroWindow
{
    private TaskbarIcon? _trayIcon;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        
        // Set the DataContext to the injected MainViewModel
        DataContext = viewModel;
        
        // Initialize tray icon programmatically for better control
        InitializeTrayIcon();
    }

    /// <summary>
    /// Initialize the system tray icon
    /// </summary>
    private void InitializeTrayIcon()
    {
        if (FindResource("TrayIcon") is TaskbarIcon trayIcon)
        {
            _trayIcon = trayIcon;
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
    /// Handles the window closing event to minimize to tray instead of closing
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        // Check if this is an explicit shutdown request
        if (System.Windows.Application.Current is App app && app.IsShuttingDown)
        {
            // Application is shutting down, clean up resources
            DisposeTrayIcon();
            
            if (DataContext is MainViewModel vm)
            {
                vm.Dispose();
            }
            base.OnClosing(e);
            return;
        }

        // For normal window close (X button), minimize to tray instead
        if (DataContext is MainViewModel viewModel)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

    /// <summary>
    /// Properly dispose of the tray icon
    /// </summary>
    private void DisposeTrayIcon()
    {
        try
        {
            if (_trayIcon is not null)
            {
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error disposing tray icon: {ex.Message}");
        }
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