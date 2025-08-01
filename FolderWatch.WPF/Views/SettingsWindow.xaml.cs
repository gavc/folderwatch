using System.Windows;
using FolderWatch.WPF.ViewModels;
using FolderWatch.WPF.Services;
using MahApps.Metro.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace FolderWatch.WPF.Views;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : MetroWindow
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow()
    {
        InitializeComponent();
        
        // Get required services from App's DI container
        var app = (App)Application.Current;
        var settingsService = app.Services.GetRequiredService<ISettingsService>();
        var themeService = app.Services.GetRequiredService<IThemeService>();
        
        _viewModel = new SettingsViewModel(settingsService, themeService);
        DataContext = _viewModel;
        
        // Subscribe to dialog result changes
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>
    /// Handles property changes in the view model
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.DialogResult))
        {
            DialogResult = _viewModel.DialogResult;
            Close();
        }
    }
}