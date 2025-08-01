using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using FolderWatch.WPF.Helpers;
using FolderWatch.WPF.Models;
using FolderWatch.WPF.Services;
using Microsoft.Win32;

namespace FolderWatch.WPF.ViewModels;

/// <summary>
/// ViewModel for settings management
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;

    public SettingsViewModel(ISettingsService settingsService, IThemeService themeService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

        LoadSettings();
        InitializeCommands();
        InitializeThemeData();
    }

    private AppSettings _currentSettings = new();

    public AppSettings CurrentSettings
    {
        get => _currentSettings;
        set => SetProperty(ref _currentSettings, value);
    }

    // Individual setting properties for binding
    public string WatchFolder
    {
        get => CurrentSettings.WatchFolder;
        set
        {
            if (CurrentSettings.WatchFolder != value)
            {
                CurrentSettings.WatchFolder = value;
                OnPropertyChanged();
            }
        }
    }

    public bool StartOnStartup
    {
        get => CurrentSettings.StartOnStartup;
        set
        {
            if (CurrentSettings.StartOnStartup != value)
            {
                CurrentSettings.StartOnStartup = value;
                OnPropertyChanged();
            }
        }
    }

    public bool MonitorSubfolders
    {
        get => CurrentSettings.MonitorSubfolders;
        set
        {
            if (CurrentSettings.MonitorSubfolders != value)
            {
                CurrentSettings.MonitorSubfolders = value;
                OnPropertyChanged();
            }
        }
    }

    public int FileWaitTimeSeconds
    {
        get => CurrentSettings.FileWaitTimeSeconds;
        set
        {
            if (CurrentSettings.FileWaitTimeSeconds != value)
            {
                CurrentSettings.FileWaitTimeSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ProcessExistingFiles
    {
        get => CurrentSettings.ProcessExistingFiles;
        set
        {
            if (CurrentSettings.ProcessExistingFiles != value)
            {
                CurrentSettings.ProcessExistingFiles = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ShowNotifications
    {
        get => CurrentSettings.ShowNotifications;
        set
        {
            if (CurrentSettings.ShowNotifications != value)
            {
                CurrentSettings.ShowNotifications = value;
                OnPropertyChanged();
            }
        }
    }

    public bool NotifyOnFileProcessed
    {
        get => CurrentSettings.NotifyOnFileProcessed;
        set
        {
            if (CurrentSettings.NotifyOnFileProcessed != value)
            {
                CurrentSettings.NotifyOnFileProcessed = value;
                OnPropertyChanged();
            }
        }
    }

    public bool MinimizeToTray
    {
        get => CurrentSettings.MinimizeToTray;
        set
        {
            if (CurrentSettings.MinimizeToTray != value)
            {
                CurrentSettings.MinimizeToTray = value;
                OnPropertyChanged();
            }
        }
    }

    public bool StartMinimized
    {
        get => CurrentSettings.StartMinimized;
        set
        {
            if (CurrentSettings.StartMinimized != value)
            {
                CurrentSettings.StartMinimized = value;
                OnPropertyChanged();
            }
        }
    }

    public bool EnableLogging
    {
        get => CurrentSettings.EnableLogging;
        set
        {
            if (CurrentSettings.EnableLogging != value)
            {
                CurrentSettings.EnableLogging = value;
                OnPropertyChanged();
            }
        }
    }

    public string LogFilePath
    {
        get => CurrentSettings.LogFilePath;
        set
        {
            if (CurrentSettings.LogFilePath != value)
            {
                CurrentSettings.LogFilePath = value;
                OnPropertyChanged();
            }
        }
    }

    public int MaxConcurrentOperations
    {
        get => CurrentSettings.MaxConcurrentOperations;
        set
        {
            if (CurrentSettings.MaxConcurrentOperations != value)
            {
                CurrentSettings.MaxConcurrentOperations = value;
                OnPropertyChanged();
            }
        }
    }

    // Theme Management
    private ObservableCollection<string> _availableThemes = [];
    private ObservableCollection<string> _availableAccents = [];
    private string _selectedTheme = "Light";
    private string _selectedAccent = "Blue";

    public ObservableCollection<string> AvailableThemes
    {
        get => _availableThemes;
        set => SetProperty(ref _availableThemes, value);
    }

    public ObservableCollection<string> AvailableAccents
    {
        get => _availableAccents;
        set => SetProperty(ref _availableAccents, value);
    }

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
            {
                CurrentSettings.Theme = value;
                _themeService.ChangeTheme(value);
            }
        }
    }

    public string SelectedAccent
    {
        get => _selectedAccent;
        set
        {
            if (SetProperty(ref _selectedAccent, value))
            {
                CurrentSettings.AccentColor = value;
                _themeService.ChangeAccent(value);
            }
        }
    }

    // Commands
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand ResetCommand { get; private set; } = null!;
    public ICommand BrowseFolderCommand { get; private set; } = null!;

    private void InitializeCommands()
    {
        SaveCommand = new RelayCommand(SaveSettings);
        CancelCommand = new RelayCommand(CancelSettings);
        ResetCommand = new RelayCommand(ResetSettings);
        BrowseFolderCommand = new RelayCommand(BrowseFolder);
    }

    private void InitializeThemeData()
    {
        AvailableThemes = new ObservableCollection<string>(_themeService.AvailableThemes);
        AvailableAccents = new ObservableCollection<string>(_themeService.AvailableAccents);
        
        SelectedTheme = CurrentSettings.Theme;
        SelectedAccent = CurrentSettings.AccentColor;
    }

    private async void LoadSettings()
    {
        await _settingsService.LoadSettingsAsync();
        CurrentSettings = _settingsService.Settings;
        
        // Notify all properties that they may have changed
        OnPropertyChanged(nameof(WatchFolder));
        OnPropertyChanged(nameof(StartOnStartup));
        OnPropertyChanged(nameof(MonitorSubfolders));
        OnPropertyChanged(nameof(FileWaitTimeSeconds));
        OnPropertyChanged(nameof(ProcessExistingFiles));
        OnPropertyChanged(nameof(ShowNotifications));
        OnPropertyChanged(nameof(NotifyOnFileProcessed));
        OnPropertyChanged(nameof(MinimizeToTray));
        OnPropertyChanged(nameof(StartMinimized));
        OnPropertyChanged(nameof(EnableLogging));
        OnPropertyChanged(nameof(LogFilePath));
        OnPropertyChanged(nameof(MaxConcurrentOperations));
    }

    private void BrowseFolder()
    {
        try
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select folder to monitor"
            };

            if (!string.IsNullOrWhiteSpace(WatchFolder) && Directory.Exists(WatchFolder))
            {
                dialog.InitialDirectory = WatchFolder;
            }

            if (dialog.ShowDialog() == true)
            {
                WatchFolder = dialog.FolderName;
            }
        }
        catch (Exception)
        {
            // Fallback message if folder dialog fails
            System.Windows.MessageBox.Show(
                "Please enter the folder path manually in the text field.",
                "Folder Browser",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }

    private async void SaveSettings()
    {
        try
        {
            // Update the settings service's settings object
            _settingsService.Settings.WatchFolder = CurrentSettings.WatchFolder;
            _settingsService.Settings.StartOnStartup = CurrentSettings.StartOnStartup;
            _settingsService.Settings.MonitorSubfolders = CurrentSettings.MonitorSubfolders;
            _settingsService.Settings.FileWaitTimeSeconds = CurrentSettings.FileWaitTimeSeconds;
            _settingsService.Settings.ProcessExistingFiles = CurrentSettings.ProcessExistingFiles;
            _settingsService.Settings.ShowNotifications = CurrentSettings.ShowNotifications;
            _settingsService.Settings.NotifyOnFileProcessed = CurrentSettings.NotifyOnFileProcessed;
            _settingsService.Settings.MinimizeToTray = CurrentSettings.MinimizeToTray;
            _settingsService.Settings.StartMinimized = CurrentSettings.StartMinimized;
            _settingsService.Settings.EnableLogging = CurrentSettings.EnableLogging;
            _settingsService.Settings.LogFilePath = CurrentSettings.LogFilePath;
            _settingsService.Settings.MaxConcurrentOperations = CurrentSettings.MaxConcurrentOperations;
            _settingsService.Settings.Theme = CurrentSettings.Theme;
            _settingsService.Settings.AccentColor = CurrentSettings.AccentColor;
            
            await _settingsService.SaveSettingsAsync();
            
            // Close dialog with success
            DialogResult = true;
        }
        catch (Exception ex)
        {
            // Handle save error
            System.Windows.MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void CancelSettings()
    {
        // Reload original settings to revert any changes
        LoadSettings();
        
        // Close dialog without saving
        DialogResult = false;
    }

    private void ResetSettings()
    {
        CurrentSettings = new AppSettings();
        LoadSettings(); // Refresh all bound properties
        
        SelectedTheme = CurrentSettings.Theme;
        SelectedAccent = CurrentSettings.AccentColor;
        _themeService.ChangeTheme(SelectedTheme, SelectedAccent);
    }

    // Dialog result for closing the window
    public bool? DialogResult { get; set; }
}
