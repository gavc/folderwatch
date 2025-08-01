using System.ComponentModel;
using System.Windows;
using FolderWatch.WPF.ViewModels;
using MahApps.Metro.Controls;

namespace FolderWatch.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MetroWindow
{
    public MainWindow()
    {
        InitializeComponent();
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
        // Get if this is a real shutdown or just a minimize to tray
        bool isRealShutdown = false;
        
        // Try to get the shutdown mode or use the exit command directly triggered
        if (Application.Current is App app)
        {
            isRealShutdown = app.IsShuttingDown;
        }
        
        // Check if we should minimize to tray instead of closing
        if (DataContext is MainViewModel viewModel && !isRealShutdown)
        {
            // For normal window close, just hide instead of closing
            e.Cancel = true;
            Hide();
            return;
        }

        // Application is shutting down, clean up resources
        if (DataContext is MainViewModel vm)
        {
            vm.Dispose();
        }

        base.OnClosing(e);
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