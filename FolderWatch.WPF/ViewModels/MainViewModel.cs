using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using FolderWatch.WPF.Helpers;
using FolderWatch.WPF.Models;
using FolderWatch.WPF.Services;
using FolderWatch.WPF.Views;
using Microsoft.Win32;

namespace FolderWatch.WPF.ViewModels;

/// <summary>
/// Main view model for the application
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly IRuleService _ruleService;
    private readonly IFileMonitorService _fileMonitorService;
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;

    private string _selectedFolderPath = string.Empty;
    private bool _isMonitoring = false;
    private string _statusMessage = "Ready";
    private bool _liveLogEnabled = true;
    private Rule? _selectedRule;

    // Observable collections for UI binding
    public ObservableCollection<Rule> Rules { get; } = new();
    public ObservableCollection<string> RuleLog { get; } = new();
    public ObservableCollection<string> LiveLog { get; } = new();

    // Commands
    public ICommand BrowseFolderCommand { get; }
    public ICommand StartMonitoringCommand { get; }
    public ICommand StopMonitoringCommand { get; }
    public ICommand AddRuleCommand { get; }
    public ICommand EditRuleCommand { get; }
    public ICommand DeleteRuleCommand { get; }
    public ICommand MoveRuleUpCommand { get; }
    public ICommand MoveRuleDownCommand { get; }
    public ICommand ToggleLiveLogCommand { get; }
    public ICommand ClearRuleLogCommand { get; }
    public ICommand ClearLiveLogCommand { get; }
    public ICommand ShowWindowCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand ExitApplicationCommand { get; }

    // Properties
    public string SelectedFolderPath
    {
        get => _selectedFolderPath;
        set
        {
            if (SetProperty(ref _selectedFolderPath, value))
            {
                OnPropertyChanged(nameof(CanStartMonitoring));
                OnPropertyChanged(nameof(CanStopMonitoring));
            }
        }
    }

    public bool IsMonitoring
    {
        get => _isMonitoring;
        set
        {
            if (SetProperty(ref _isMonitoring, value))
            {
                OnPropertyChanged(nameof(CanStartMonitoring));
                OnPropertyChanged(nameof(CanStopMonitoring));
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool LiveLogEnabled
    {
        get => _liveLogEnabled;
        set => SetProperty(ref _liveLogEnabled, value);
    }

    public Rule? SelectedRule
    {
        get => _selectedRule;
        set
        {
            if (SetProperty(ref _selectedRule, value))
            {
                // Notify command can-execute changed
                ((RelayCommand<Rule>)EditRuleCommand).RaiseCanExecuteChanged();
                ((RelayCommand<Rule>)DeleteRuleCommand).RaiseCanExecuteChanged();
                ((RelayCommand<Rule>)MoveRuleUpCommand).RaiseCanExecuteChanged();
                ((RelayCommand<Rule>)MoveRuleDownCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanStartMonitoring => !IsMonitoring && !string.IsNullOrWhiteSpace(SelectedFolderPath) && Directory.Exists(SelectedFolderPath);
    public bool CanStopMonitoring => IsMonitoring;

    public MainViewModel(
        IRuleService ruleService,
        IFileMonitorService fileMonitorService,
        ISettingsService settingsService,
        IThemeService themeService)
    {
        _ruleService = ruleService ?? throw new ArgumentNullException(nameof(ruleService));
        _fileMonitorService = fileMonitorService ?? throw new ArgumentNullException(nameof(fileMonitorService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

        // Initialize commands
        BrowseFolderCommand = new RelayCommand(BrowseFolder);
        StartMonitoringCommand = new RelayCommand(async () => await StartMonitoringAsync(), () => CanStartMonitoring);
        StopMonitoringCommand = new RelayCommand(async () => await StopMonitoringAsync(), () => CanStopMonitoring);
        AddRuleCommand = new RelayCommand(AddRule);
        EditRuleCommand = new RelayCommand<Rule>(EditRule, _ => SelectedRule is not null);
        DeleteRuleCommand = new RelayCommand<Rule>(async _ => await DeleteRuleAsync(SelectedRule), _ => SelectedRule is not null);
        MoveRuleUpCommand = new RelayCommand<Rule>(async _ => await MoveRuleUpAsync(SelectedRule), _ => CanMoveRuleUp(SelectedRule));
        MoveRuleDownCommand = new RelayCommand<Rule>(async _ => await MoveRuleDownAsync(SelectedRule), _ => CanMoveRuleDown(SelectedRule));
        ToggleLiveLogCommand = new RelayCommand(() => LiveLogEnabled = !LiveLogEnabled);
        ClearRuleLogCommand = new RelayCommand(() => RuleLog.Clear());
        ClearLiveLogCommand = new RelayCommand(() => LiveLog.Clear());
        ShowWindowCommand = new RelayCommand(ShowWindow);
        ShowSettingsCommand = new RelayCommand(ShowSettings);
        ExitApplicationCommand = new RelayCommand(ExitApplication);

        // Subscribe to service events
        _ruleService.RuleActionLogged += OnRuleActionLogged;
        _fileMonitorService.MonitoringStatusChanged += OnMonitoringStatusChanged;
        _fileMonitorService.FileEventLogged += OnFileEventLogged;

        // Initialize
        _ = InitializeAsync();
    }

    /// <summary>
    /// Initializes the view model with data and settings
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            // Load settings and apply theme
            await _settingsService.LoadSettingsAsync();
            var settings = _settingsService.Settings;
            
            _themeService.ChangeTheme(settings.Theme, settings.AccentColor);
            SelectedFolderPath = settings.LastWatchedFolder;
            LiveLogEnabled = settings.LiveLogEnabled;

            // Load rules
            await LoadRulesAsync();

            StatusMessage = "Ready";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Initialization error: {ex.Message}";
        }
    }

    /// <summary>
    /// Loads rules from the service
    /// </summary>
    private async Task LoadRulesAsync()
    {
        try
        {
            var rules = await _ruleService.GetRulesAsync();
            
            // Ensure UI updates happen on the UI thread
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Rules.Clear();
                foreach (var rule in rules)
                {
                    Rules.Add(rule);
                }
            });
        }
        catch (Exception ex)
        {
            AddToRuleLog($"Error loading rules: {ex.Message}");
        }
    }

    /// <summary>
    /// Opens folder browser dialog
    /// </summary>
    private void BrowseFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Folder to Monitor"
        };

        if (!string.IsNullOrWhiteSpace(SelectedFolderPath))
        {
            dialog.InitialDirectory = SelectedFolderPath;
        }

        if (dialog.ShowDialog() == true)
        {
            SelectedFolderPath = dialog.FolderName;
            
            // Save to settings
            _ = Task.Run(async () =>
            {
                await _settingsService.UpdateSettingAsync(nameof(AppSettings.LastWatchedFolder), SelectedFolderPath);
            });
        }
    }

    /// <summary>
    /// Starts folder monitoring
    /// </summary>
    private async Task StartMonitoringAsync()
    {
        try
        {
            await _fileMonitorService.StartMonitoringAsync(SelectedFolderPath);
            StatusMessage = $"Monitoring: {SelectedFolderPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error starting monitoring: {ex.Message}";
            AddToRuleLog($"Failed to start monitoring: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops folder monitoring
    /// </summary>
    private async Task StopMonitoringAsync()
    {
        try
        {
            await _fileMonitorService.StopMonitoringAsync();
            StatusMessage = "Ready";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error stopping monitoring: {ex.Message}";
            AddToRuleLog($"Failed to stop monitoring: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds a new rule using the rule editor dialog
    /// </summary>
    private async void AddRule()
    {
        try
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow is null)
            {
                AddToRuleLog("Error: Could not find main window for dialog");
                return;
            }

            var newRule = RuleEditorDialog.ShowDialog(mainWindow);
            if (newRule is not null)
            {
                await _ruleService.SaveRuleAsync(newRule);
                
                // Add to UI collection immediately on UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Rules.Add(newRule);
                    // Select the newly added rule
                    SelectedRule = newRule;
                });
                
                AddToRuleLog($"Added new rule: {newRule.Name}");
            }
        }
        catch (Exception ex)
        {
            AddToRuleLog($"Error adding rule: {ex.Message}");
            await ShowErrorAsync("Add Rule Error", $"Failed to add rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Edits an existing rule using the rule editor dialog
    /// </summary>
    private async void EditRule(Rule? _)
    {
        var rule = SelectedRule;
        if (rule is null) return;

        try
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow is null)
            {
                AddToRuleLog("Error: Could not find main window for dialog");
                return;
            }

            var editedRule = RuleEditorDialog.ShowDialog(mainWindow, rule);
            if (editedRule is not null)
            {
                await _ruleService.SaveRuleAsync(editedRule);
                
                // Update UI collection on UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var index = Rules.IndexOf(rule);
                    if (index >= 0)
                    {
                        Rules[index] = editedRule;
                        // Update selected rule to the new instance
                        SelectedRule = editedRule;
                    }
                });
                
                AddToRuleLog($"Updated rule: {editedRule.Name}");
            }
        }
        catch (Exception ex)
        {
            AddToRuleLog($"Error editing rule: {ex.Message}");
            await ShowErrorAsync("Edit Rule Error", $"Failed to edit rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a rule with confirmation dialog and progress indication
    /// </summary>
    private async Task DeleteRuleAsync(Rule? rule)
    {
        if (rule is null) return;

        try
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow is null)
            {
                AddToRuleLog("Error: Could not find main window for dialog");
                return;
            }

            // Show confirmation dialog
            var confirmed = await DialogHelper.ShowConfirmationAsync(
                mainWindow,
                "Delete Rule",
                $"Are you sure you want to delete the rule '{rule.Name}'?\n\nThis action cannot be undone.");

            if (!confirmed)
            {
                AddToRuleLog($"Delete rule cancelled: {rule.Name}");
                return;
            }

            // Show progress for long-running operation
            var progressDialog = await DialogHelper.ShowProgressAsync(
                mainWindow, 
                "Deleting Rule", 
                "Please wait while the rule is being deleted...");

            try
            {
                await _ruleService.DeleteRuleAsync(rule);
                
                // Remove from UI collection on UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Rules.Remove(rule);
                    // Clear selection if this was the selected rule
                    if (SelectedRule == rule)
                    {
                        SelectedRule = null;
                    }
                });
                
                AddToRuleLog($"Deleted rule: {rule.Name}");
            }
            finally
            {
                // Close progress dialog
                if (progressDialog is not null)
                {
                    await progressDialog.CloseAsync();
                }
            }
        }
        catch (Exception ex)
        {
            AddToRuleLog($"Error deleting rule: {ex.Message}");
            await ShowErrorAsync("Delete Rule Error", $"Failed to delete rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Moves a rule up in the list with improved UI updates
    /// </summary>
    private async Task MoveRuleUpAsync(Rule? rule)
    {
        if (rule is null) return;

        try
        {
            var index = Rules.IndexOf(rule);
            if (index > 0)
            {
                // Update UI immediately
                Rules.Move(index, index - 1);
                
                // Save the new order
                await SaveRuleOrderAsync();
                
                AddToRuleLog($"Moved rule up: {rule.Name}");
            }
        }
        catch (Exception ex)
        {
            AddToRuleLog($"Error moving rule up: {ex.Message}");
            await ShowErrorAsync("Move Rule Error", $"Failed to move rule up: {ex.Message}");
        }
    }

    /// <summary>
    /// Moves a rule down in the list with improved UI updates
    /// </summary>
    private async Task MoveRuleDownAsync(Rule? rule)
    {
        if (rule is null) return;

        try
        {
            var index = Rules.IndexOf(rule);
            if (index < Rules.Count - 1)
            {
                // Update UI immediately
                Rules.Move(index, index + 1);
                
                // Save the new order
                await SaveRuleOrderAsync();
                
                AddToRuleLog($"Moved rule down: {rule.Name}");
            }
        }
        catch (Exception ex)
        {
            AddToRuleLog($"Error moving rule down: {ex.Message}");
            await ShowErrorAsync("Move Rule Error", $"Failed to move rule down: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a rule can be moved up
    /// </summary>
    private bool CanMoveRuleUp(Rule? rule)
    {
        return rule is not null && Rules.IndexOf(rule) > 0;
    }

    /// <summary>
    /// Checks if a rule can be moved down
    /// </summary>
    private bool CanMoveRuleDown(Rule? rule)
    {
        return rule is not null && Rules.IndexOf(rule) < Rules.Count - 1;
    }

    /// <summary>
    /// Saves the current rule order
    /// </summary>
    private async Task SaveRuleOrderAsync()
    {
        try
        {
            await _ruleService.ReorderRulesAsync(Rules);
        }
        catch (Exception ex)
        {
            AddToRuleLog($"Error saving rule order: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles rule action log events
    /// </summary>
    private void OnRuleActionLogged(string message)
    {
        App.Current.Dispatcher.Invoke(() => AddToRuleLog(message));
    }

    /// <summary>
    /// Handles monitoring status change events
    /// </summary>
    private void OnMonitoringStatusChanged(bool isMonitoring)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            IsMonitoring = isMonitoring;
            OnPropertyChanged(nameof(CanStartMonitoring));
            OnPropertyChanged(nameof(CanStopMonitoring));
        });
    }

    /// <summary>
    /// Handles file event log events
    /// </summary>
    private void OnFileEventLogged(string message)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            if (LiveLogEnabled)
            {
                AddToLiveLog(message);
            }
        });
    }

    /// <summary>
    /// Adds a message to the rule log
    /// </summary>
    private void AddToRuleLog(string message)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            RuleLog.Add(message);
            
            // Keep log size manageable
            while (RuleLog.Count > 1000)
            {
                RuleLog.RemoveAt(0);
            }
        });
    }

    /// <summary>
    /// Adds a message to the live log
    /// </summary>
    private void AddToLiveLog(string message)
    {
        LiveLog.Add(message);
        
        // Keep log size manageable
        while (LiveLog.Count > 1000)
        {
            LiveLog.RemoveAt(0);
        }
    }

    /// <summary>
    /// Shows the main window
    /// </summary>
    private void ShowWindow()
    {
        if (App.Current.MainWindow is MainWindow mainWindow)
        {
            mainWindow.ShowAndActivate();
        }
    }

    /// <summary>
    /// Shows the settings dialog
    /// </summary>
    private async void ShowSettings()
    {
        try
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow is null)
            {
                AddToRuleLog("Error: Could not find main window for settings dialog");
                return;
            }

            // Create and show settings window
            var settingsWindow = new SettingsWindow
            {
                Owner = mainWindow
            };
            
            var result = settingsWindow.ShowDialog();
            if (result == true)
            {
                AddToRuleLog("Settings updated successfully");
                
                // Reload settings that might affect monitoring
                var settings = _settingsService.Settings;
                if (!string.IsNullOrEmpty(settings.LastWatchedFolder) && 
                    settings.LastWatchedFolder != SelectedFolderPath)
                {
                    SelectedFolderPath = settings.LastWatchedFolder;
                }
            }
        }
        catch (Exception ex)
        {
            AddToRuleLog($"Error opening settings: {ex.Message}");
            await ShowErrorAsync("Settings Error", $"Failed to open settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Exits the application
    /// </summary>
    private void ExitApplication()
    {
        App.Current.Shutdown();
    }

    /// <summary>
    /// Shows an error dialog to the user
    /// </summary>
    private async Task ShowErrorAsync(string title, string message)
    {
        try
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow is not null)
            {
                await DialogHelper.ShowErrorAsync(mainWindow, title, message);
            }
        }
        catch
        {
            // Fallback - if dialog fails, at least log it
            AddToRuleLog($"Error: {title} - {message}");
        }
    }

    /// <summary>
    /// Shows an information dialog to the user
    /// </summary>
    private async Task ShowInformationAsync(string title, string message)
    {
        try
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow is not null)
            {
                await DialogHelper.ShowInformationAsync(mainWindow, title, message);
            }
        }
        catch
        {
            // Fallback - if dialog fails, at least log it
            AddToRuleLog($"Info: {title} - {message}");
        }
    }
}
