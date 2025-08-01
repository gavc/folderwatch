using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using FolderWatch.WPF.Helpers;
using FolderWatch.WPF.Models;
using FolderWatch.WPF.Services;

namespace FolderWatch.WPF.ViewModels;

/// <summary>
/// Main view model for the application
/// </summary>
public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IRuleService _ruleService;
    private readonly IFileMonitorService _fileMonitorService;
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private bool _disposed = false;

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
                // Trigger CanExecute updates for rule-dependent commands
                CommandManager.InvalidateRequerySuggested();
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
        StartMonitoringCommand = new AsyncRelayCommand(StartMonitoringAsync, () => CanStartMonitoring);
        StopMonitoringCommand = new AsyncRelayCommand(StopMonitoringAsync, () => CanStopMonitoring);
        AddRuleCommand = new AsyncRelayCommand(AddRuleAsync);
        EditRuleCommand = new RelayCommand(() => EditRule(SelectedRule), () => SelectedRule is not null);
        DeleteRuleCommand = new AsyncRelayCommand(async () => await DeleteRuleAsync(SelectedRule), () => SelectedRule is not null);
        MoveRuleUpCommand = new AsyncRelayCommand(async () => await MoveRuleUpAsync(SelectedRule), () => CanMoveRuleUp(SelectedRule));
        MoveRuleDownCommand = new AsyncRelayCommand(async () => await MoveRuleDownAsync(SelectedRule), () => CanMoveRuleDown(SelectedRule));
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
            await Application.Current.Dispatcher.InvokeAsync(() =>
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
        try
        {
            // Use Windows Forms folder browser dialog for better compatibility
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Folder to Monitor",
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true
            };

            if (!string.IsNullOrWhiteSpace(SelectedFolderPath) && Directory.Exists(SelectedFolderPath))
            {
                dialog.SelectedPath = SelectedFolderPath;
            }

            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                SelectedFolderPath = dialog.SelectedPath;
                
                // Save to settings
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _settingsService.UpdateSettingAsync(nameof(AppSettings.LastWatchedFolder), SelectedFolderPath);
                    }
                    catch (Exception ex)
                    {
                        AddToRuleLog($"Error saving folder path to settings: {ex.Message}");
                    }
                });
                
                AddToRuleLog($"Selected folder: {SelectedFolderPath}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error browsing folders: {ex.Message}";
            AddToRuleLog($"Failed to browse folders: {ex.Message}");
        }
    }

    /// <summary>
    /// Starts folder monitoring
    /// </summary>
    private async Task StartMonitoringAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedFolderPath))
        {
            StatusMessage = "Please select a folder to monitor";
            return;
        }

        if (!Directory.Exists(SelectedFolderPath))
        {
            StatusMessage = "Selected folder does not exist";
            return;
        }

        try
        {
            StatusMessage = "Starting monitoring...";
            await _fileMonitorService.StartMonitoringAsync(SelectedFolderPath);
            StatusMessage = $"Monitoring: {SelectedFolderPath}";
            AddToRuleLog($"Started monitoring folder: {SelectedFolderPath}");
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error starting monitoring: {ex.Message}";
            StatusMessage = errorMessage;
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
            StatusMessage = "Stopping monitoring...";
            await _fileMonitorService.StopMonitoringAsync();
            StatusMessage = "Ready";
            AddToRuleLog("Stopped monitoring");
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error stopping monitoring: {ex.Message}";
            StatusMessage = errorMessage;
            AddToRuleLog($"Failed to stop monitoring: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds a new rule
    /// </summary>
    private async Task AddRuleAsync()
    {
        try
        {
            // This would open a rule editor dialog
            // For now, add a placeholder rule with better defaults
            var newRule = new Rule
            {
                Name = $"New Rule {Rules.Count + 1}",
                Pattern = "*.txt",
                Action = RuleAction.Move,
                Destination = Path.Combine(SelectedFolderPath ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Processed"),
                Enabled = true
            };

            await _ruleService.SaveRuleAsync(newRule);
            
            // Add to UI collection immediately on UI thread
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Rules.Add(newRule);
                SelectedRule = newRule; // Select the newly added rule
            });
            
            AddToRuleLog($"Added new rule: {newRule.Name}");
            StatusMessage = $"Added rule: {newRule.Name}";
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error adding rule: {ex.Message}";
            StatusMessage = errorMessage;
            AddToRuleLog(errorMessage);
        }
    }

    /// <summary>
    /// Edits an existing rule
    /// </summary>
    private void EditRule(Rule? rule)
    {
        if (rule is null) 
        {
            StatusMessage = "No rule selected for editing";
            return;
        }
        
        try
        {
            // This would open a rule editor dialog with the selected rule
            // For now, just provide feedback
            AddToRuleLog($"Edit rule: {rule.Name} (Rule editor dialog not implemented yet)");
            StatusMessage = $"Editing rule: {rule.Name} (Feature coming soon)";
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error editing rule {rule.Name}: {ex.Message}";
            StatusMessage = errorMessage;
            AddToRuleLog(errorMessage);
        }
    }

    /// <summary>
    /// Deletes a rule
    /// </summary>
    private async Task DeleteRuleAsync(Rule? rule)
    {
        if (rule is null) 
        {
            StatusMessage = "No rule selected for deletion";
            return;
        }

        try
        {
            // Find the current index for better selection management
            var currentIndex = Rules.IndexOf(rule);
            
            await _ruleService.DeleteRuleAsync(rule);
            
            // Remove from UI collection on UI thread
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Rules.Remove(rule);
                
                // Select the next rule or previous if we deleted the last one
                if (Rules.Count > 0)
                {
                    if (currentIndex < Rules.Count)
                    {
                        SelectedRule = Rules[currentIndex];
                    }
                    else if (currentIndex > 0)
                    {
                        SelectedRule = Rules[currentIndex - 1];
                    }
                    else
                    {
                        SelectedRule = Rules[0];
                    }
                }
                else
                {
                    SelectedRule = null;
                }
            });
            
            var successMessage = $"Deleted rule: {rule.Name}";
            AddToRuleLog(successMessage);
            StatusMessage = successMessage;
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error deleting rule {rule.Name}: {ex.Message}";
            StatusMessage = errorMessage;
            AddToRuleLog(errorMessage);
        }
    }

    /// <summary>
    /// Moves a rule up in the list
    /// </summary>
    private async Task MoveRuleUpAsync(Rule? rule)
    {
        if (rule is null) 
        {
            StatusMessage = "No rule selected to move up";
            return;
        }

        try
        {
            var index = Rules.IndexOf(rule);
            if (index > 0)
            {
                Rules.Move(index, index - 1);
                await SaveRuleOrderAsync();
                
                var successMessage = $"Moved rule '{rule.Name}' up";
                AddToRuleLog(successMessage);
                StatusMessage = successMessage;
            }
            else
            {
                StatusMessage = "Rule is already at the top";
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error moving rule '{rule.Name}' up: {ex.Message}";
            StatusMessage = errorMessage;
            AddToRuleLog(errorMessage);
        }
    }

    /// <summary>
    /// Moves a rule down in the list
    /// </summary>
    private async Task MoveRuleDownAsync(Rule? rule)
    {
        if (rule is null) 
        {
            StatusMessage = "No rule selected to move down";
            return;
        }

        try
        {
            var index = Rules.IndexOf(rule);
            if (index < Rules.Count - 1)
            {
                Rules.Move(index, index + 1);
                await SaveRuleOrderAsync();
                
                var successMessage = $"Moved rule '{rule.Name}' down";
                AddToRuleLog(successMessage);
                StatusMessage = successMessage;
            }
            else
            {
                StatusMessage = "Rule is already at the bottom";
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error moving rule '{rule.Name}' down: {ex.Message}";
            StatusMessage = errorMessage;
            AddToRuleLog(errorMessage);
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
            var errorMessage = $"Error saving rule order: {ex.Message}";
            StatusMessage = errorMessage;
            AddToRuleLog(errorMessage);
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
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            RuleLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            
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
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            LiveLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            
            // Keep log size manageable
            while (LiveLog.Count > 1000)
            {
                LiveLog.RemoveAt(0);
            }
        });
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
    private void ShowSettings()
    {
        try
        {
            // This would open a settings dialog
            // For now, just provide feedback
            AddToRuleLog("Settings dialog not implemented yet");
            StatusMessage = "Settings dialog feature coming soon";
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error opening settings: {ex.Message}";
            StatusMessage = errorMessage;
            AddToRuleLog(errorMessage);
        }
    }

    /// <summary>
    /// Exits the application
    /// </summary>
    private void ExitApplication()
    {
        try
        {
            // Stop monitoring first
            _ = StopMonitoringAsync();
            
            // Let the application know we're intentionally shutting down
            if (Application.Current is App app)
            {
                app.IsShuttingDown = true;
            }
        }
        catch (Exception ex)
        {
            // Log any errors but continue with shutdown
            System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex.Message}");
        }
        finally
        {
            // Shut down the application
            Application.Current.Shutdown();
        }
    }

    /// <summary>
    /// Disposes of resources used by this view model
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            // Unsubscribe from events to prevent memory leaks
            _ruleService.RuleActionLogged -= OnRuleActionLogged;
            _fileMonitorService.MonitoringStatusChanged -= OnMonitoringStatusChanged;
            _fileMonitorService.FileEventLogged -= OnFileEventLogged;

            // Stop monitoring to ensure FileSystemWatcher is cleaned up
            _ = StopMonitoringAsync();

            // If FileMonitorService is IDisposable, dispose it here
            if (_fileMonitorService is IDisposable disposableService)
            {
                disposableService.Dispose();
            }

            _disposed = true;
        }
    }
}
