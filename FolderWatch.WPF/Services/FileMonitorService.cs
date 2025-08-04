using System.IO;
using FolderWatch.WPF.Helpers;
using FolderWatch.WPF.Models;

namespace FolderWatch.WPF.Services;

/// <summary>
/// Monitors file system changes and processes files according to rules
/// </summary>
public class FileMonitorService : IFileMonitorService, IDisposable
{
    private readonly IRuleService _ruleService;
    private readonly ISettingsService _settingsService;
    private FileSystemWatcher? _watcher;
    private readonly SemaphoreSlim _processingLock = new(1, 1);

    public bool IsMonitoring => _watcher?.EnableRaisingEvents == true;
    public string? MonitoredPath { get; private set; }

    public event Action<bool>? MonitoringStatusChanged;
    public event Action<string>? FileEventLogged;

    public FileMonitorService(IRuleService ruleService, ISettingsService settingsService)
    {
        _ruleService = ruleService ?? throw new ArgumentNullException(nameof(ruleService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    }

    /// <summary>
    /// Starts monitoring the specified folder
    /// </summary>
    public async Task StartMonitoringAsync(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path cannot be empty", nameof(folderPath));

        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

        await StopMonitoringAsync();

        _watcher = new FileSystemWatcher(folderPath)
        {
            NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName,
            IncludeSubdirectories = false // Only watch the specified folder, not subdirectories
        };

        _watcher.Created += OnFileCreated;
        _watcher.Renamed += OnFileRenamed;
        _watcher.Error += OnWatcherError;

        _watcher.EnableRaisingEvents = true;
        MonitoredPath = folderPath;

        LogEvent($"Started monitoring: {folderPath}");
        MonitoringStatusChanged?.Invoke(true);
    }

    /// <summary>
    /// Stops monitoring
    /// </summary>
    public async Task StopMonitoringAsync()
    {
        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnFileCreated;
            _watcher.Renamed -= OnFileRenamed;
            _watcher.Error -= OnWatcherError;
            _watcher.Dispose();
            _watcher = null;

            LogEvent($"Stopped monitoring: {MonitoredPath}");
            MonitoredPath = null;
            MonitoringStatusChanged?.Invoke(false);
        }

        // Wait for any ongoing processing to complete
        await _processingLock.WaitAsync();
        _processingLock.Release();
    }

    /// <summary>
    /// Handles file creation events
    /// </summary>
    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        _ = Task.Run(() => ProcessFileAsync(e.FullPath, "Created"));
    }

    /// <summary>
    /// Handles file rename events (often indicates completion of downloads)
    /// </summary>
    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        _ = Task.Run(() => ProcessFileAsync(e.FullPath, "Renamed"));
    }

    /// <summary>
    /// Handles watcher errors
    /// </summary>
    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        LogEvent($"Monitoring error: {e.GetException().Message}", isError: true);
    }

    /// <summary>
    /// Processes a file according to the configured rules
    /// </summary>
    private async Task ProcessFileAsync(string filePath, string eventType)
    {
        await _processingLock.WaitAsync();
        try
        {
            LogEvent($"{eventType}: {Path.GetFileName(filePath)}");

            // Check file readiness
            var settings = _settingsService.Settings;
            var readiness = FileAccessibilityChecker.CheckFileReadiness(filePath, msg => LogEvent(msg));

            if (!readiness.IsReady)
            {
                if (readiness.Reason == FileNotReadyReason.TemporaryFile && settings.FileRetrySettings.SkipTemporaryFiles)
                {
                    LogEvent($"Skipping temporary file: {Path.GetFileName(filePath)}");
                    return;
                }

                if (readiness.Reason == FileNotReadyReason.FileNotAccessible)
                {
                    // Wait for file to become accessible
                    var accessResult = await FileAccessibilityChecker.WaitForFileAccessAsync(
                        filePath,
                        settings.FileRetrySettings.MaxRetries,
                        settings.FileRetrySettings.InitialDelayMs,
                        settings.FileRetrySettings.MaxDelayMs,
                        settings.FileRetrySettings.BackoffMultiplier);

                    if (!accessResult.IsAccessible)
                    {
                        LogEvent($"File remained inaccessible: {accessResult.Message}", isError: true);
                        return;
                    }

                    LogEvent($"File became accessible after {accessResult.RetriesUsed} retries");
                }
                else
                {
                    LogEvent($"File not ready: {readiness.Message}");
                    return;
                }
            }

            // Process with rules
            await ProcessWithRulesAsync(filePath);
        }
        catch (Exception ex)
        {
            LogEvent($"Error processing file {Path.GetFileName(filePath)}: {ex.Message}", isError: true);
        }
        finally
        {
            _processingLock.Release();
        }
    }

    /// <summary>
    /// Applies rules to process the file
    /// </summary>
    private async Task ProcessWithRulesAsync(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var rules = await _ruleService.GetRulesAsync();
        var matchedRules = 0;

        foreach (var rule in rules.Where(r => r.Enabled))
        {
            if (PatternMatcher.IsMatch(rule.Pattern, fileName))
            {
                matchedRules++;
                LogEvent($"Matched rule '{rule.Name}' for file: {fileName}");
                
                try
                {
                    var processedPath = filePath;
                    
                    if (rule.HasMultipleSteps)
                    {
                        processedPath = await ExecuteRuleStepsAsync(rule, filePath);
                    }
                    else
                    {
                        processedPath = await ExecuteSingleRuleAsync(rule, filePath);
                    }
                    
                    LogEvent($"Successfully processed {fileName} with rule '{rule.Name}'");
                    
                    // Only process with the first matching rule
                    return;
                }
                catch (Exception ex)
                {
                    LogEvent($"Error executing rule '{rule.Name}' on {fileName}: {ex.Message}", isError: true);
                    // Continue with next rule if this one fails
                }
            }
        }

        if (matchedRules == 0)
        {
            LogEvent($"No matching rules found for: {fileName}");
        }
    }

    /// <summary>
    /// Executes a single-action rule
    /// </summary>
    private async Task<string> ExecuteSingleRuleAsync(Rule rule, string filePath)
    {
        LogEvent($"Executing single action: {rule.Action}");
        return await ExecuteActionAsync(rule.Action, filePath, rule.Destination, rule.NewName);
    }

    /// <summary>
    /// Executes a multi-step rule
    /// </summary>
    private async Task<string> ExecuteRuleStepsAsync(Rule rule, string filePath)
    {
        var currentPath = filePath;
        var enabledSteps = rule.Steps.Where(s => s.Enabled).ToList();
        
        LogEvent($"Executing {enabledSteps.Count} steps for rule '{rule.Name}'");
        
        for (int i = 0; i < enabledSteps.Count; i++)
        {
            var step = enabledSteps[i];
            try
            {
                LogEvent($"Step {i + 1}/{enabledSteps.Count}: {step.Action}");
                var newPath = await ExecuteActionAsync(step.Action, currentPath, step.Destination, step.NewName);
                
                if (!string.IsNullOrEmpty(newPath))
                {
                    currentPath = newPath;
                }
                
                // If file was deleted, no further steps can be executed
                if (step.Action == RuleAction.Delete && string.IsNullOrEmpty(newPath))
                {
                    LogEvent($"File deleted in step {i + 1}, stopping further processing");
                    break;
                }
            }
            catch (Exception ex)
            {
                LogEvent($"Error in step {i + 1} ({step.Action}): {ex.Message}", isError: true);
                throw; // Re-throw to stop processing this rule
            }
        }
        
        return currentPath;
    }

    /// <summary>
    /// Executes a specific file action
    /// </summary>
    private async Task<string> ExecuteActionAsync(RuleAction action, string filePath, string destination, string newName)
    {
        return action switch
        {
            RuleAction.Copy => await CopyFileAsync(filePath, destination, newName),
            RuleAction.Move => await MoveFileAsync(filePath, destination, newName),
            RuleAction.Rename => await RenameFileAsync(filePath, newName),
            RuleAction.Delete => await DeleteFileAsync(filePath),
            RuleAction.DateTime => await AddDateTimeToFileAsync(filePath, newName),
            RuleAction.Numbering => await AddNumberToFileAsync(filePath, newName),
            _ => filePath
        };
    }

    /// <summary>
    /// Copies a file to the specified destination with conflict resolution
    /// </summary>
    private async Task<string> CopyFileAsync(string sourcePath, string destination, string newName)
    {
        if (string.IsNullOrWhiteSpace(destination))
            return sourcePath;

        Directory.CreateDirectory(destination);
        
        var fileName = !string.IsNullOrWhiteSpace(newName) 
            ? RenamePatternProcessor.ProcessPattern(newName, sourcePath)
            : Path.GetFileName(sourcePath);
            
        var destPath = Path.Combine(destination, fileName);
        
        // Handle file conflicts
        destPath = GetUniqueFilePath(destPath);
        
        await Task.Run(() => File.Copy(sourcePath, destPath, false)); // Don't overwrite, use unique name instead
        LogEvent($"Copied to: {destPath}");
        
        return destPath;
    }

    /// <summary>
    /// Moves a file to the specified destination with conflict resolution
    /// </summary>
    private async Task<string> MoveFileAsync(string sourcePath, string destination, string newName)
    {
        if (string.IsNullOrWhiteSpace(destination))
            return sourcePath;

        Directory.CreateDirectory(destination);
        
        var fileName = !string.IsNullOrWhiteSpace(newName) 
            ? RenamePatternProcessor.ProcessPattern(newName, sourcePath)
            : Path.GetFileName(sourcePath);
            
        var destPath = Path.Combine(destination, fileName);
        
        // Handle file conflicts
        destPath = GetUniqueFilePath(destPath);
        
        await Task.Run(() => File.Move(sourcePath, destPath, false)); // Don't overwrite, use unique name instead
        LogEvent($"Moved to: {destPath}");
        
        return destPath;
    }

    /// <summary>
    /// Renames a file using pattern-based renaming with variable support
    /// </summary>
    private async Task<string> RenameFileAsync(string filePath, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return filePath;

        var directory = Path.GetDirectoryName(filePath) ?? "";
        
        // Process the pattern to substitute variables
        var newFileName = RenamePatternProcessor.ProcessPattern(pattern, filePath);
        var newPath = Path.Combine(directory, newFileName);
        
        // Handle file conflicts
        newPath = GetUniqueFilePath(newPath);
        
        await Task.Run(() => File.Move(filePath, newPath, true));
        LogEvent($"Renamed to: {Path.GetFileName(newPath)}");
        
        return newPath;
    }

    /// <summary>
    /// Deletes a file with safety checks
    /// </summary>
    private async Task<string> DeleteFileAsync(string filePath)
    {
        try
        {
            var settings = _settingsService.Settings;
            var deleteSettings = settings.DeleteSafetySettings;
            
            // Safety check: Don't delete system or hidden files based on settings
            var fileInfo = new FileInfo(filePath);
            
            if (deleteSettings.SkipSystemFiles && fileInfo.Attributes.HasFlag(FileAttributes.System))
            {
                LogEvent($"Skipped deleting system file: {Path.GetFileName(filePath)}", isError: true);
                return filePath;
            }
            
            if (deleteSettings.SkipHiddenFiles && fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
            {
                LogEvent($"Skipped deleting hidden file: {Path.GetFileName(filePath)}", isError: true);
                return filePath;
            }

            // Safety check: Don't delete files larger than configured limit
            if (fileInfo.Length > deleteSettings.MaxFileSizeBytes)
            {
                LogEvent($"Skipped deleting large file (>{deleteSettings.MaxFileSizeBytes / 1_048_576}MB): {Path.GetFileName(filePath)}", isError: true);
                return filePath;
            }

            await Task.Run(() => File.Delete(filePath));
            LogEvent($"Deleted: {Path.GetFileName(filePath)}");
            
            return "";
        }
        catch (Exception ex)
        {
            LogEvent($"Error deleting file {Path.GetFileName(filePath)}: {ex.Message}", isError: true);
            return filePath; // Return original path if deletion failed
        }
    }

    /// <summary>
    /// Gets a unique file path by appending a number if the file already exists
    /// </summary>
    /// <param name="originalPath">The original file path</param>
    /// <returns>A unique file path</returns>
    private static string GetUniqueFilePath(string originalPath)
    {
        if (!File.Exists(originalPath))
            return originalPath;

        var directory = Path.GetDirectoryName(originalPath) ?? "";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
        var extension = Path.GetExtension(originalPath);
        
        int counter = 1;
        string newPath;
        
        do
        {
            var newFileName = $"{fileNameWithoutExt}_{counter:000}{extension}";
            newPath = Path.Combine(directory, newFileName);
            counter++;
        }
        while (File.Exists(newPath) && counter < 1000); // Prevent infinite loop
        
        return newPath;
    }

    /// <summary>
    /// Adds date/time to filename using pattern-based renaming
    /// </summary>
    private async Task<string> AddDateTimeToFileAsync(string filePath, string pattern)
    {
        var effectivePattern = string.IsNullOrWhiteSpace(pattern) ? "{datetime:yyyyMMdd_HHmmss}_{filename}" : pattern;
        return await RenameFileAsync(filePath, effectivePattern);
    }

    /// <summary>
    /// Adds sequential number to filename using pattern-based renaming
    /// </summary>
    private async Task<string> AddNumberToFileAsync(string filePath, string pattern)
    {
        var effectivePattern = string.IsNullOrWhiteSpace(pattern) ? "{filename}_{counter:000}" : pattern;
        return await RenameFileAsync(filePath, effectivePattern);
    }

    /// <summary>
    /// Logs an event message
    /// </summary>
    private void LogEvent(string message, bool isError = false)
    {
        var logMessage = $"[{DateTime.Now:G}] {message}";
        FileEventLogged?.Invoke(logMessage);
    }

    /// <summary>
    /// Disposes of resources used by this service
    /// </summary>
    public void Dispose()
    {
        // Stop the file watcher without waiting for async completion
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnFileCreated;
            _watcher.Renamed -= OnFileRenamed;
            _watcher.Error -= OnWatcherError;
            _watcher.Dispose();
            _watcher = null;
        }
        
        // Dispose the semaphore
        _processingLock?.Dispose();
    }
}
