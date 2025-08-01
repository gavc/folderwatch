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
        // Check if we should minimize to tray instead of closing
        if (DataContext is MainViewModel viewModel)
        {
            // For now, just hide the window - in a real app, you'd check settings
            e.Cancel = true;
            Hide();
            return;
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