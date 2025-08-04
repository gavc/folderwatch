using System.IO;

namespace FolderWatch.WPF.Models;

/// <summary>
/// Configuration settings for file retry operations
/// </summary>
public class FileRetrySettings
{
    /// <summary>
    /// Maximum number of retry attempts for locked files
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Initial delay in milliseconds before first retry
    /// </summary>
    public int InitialDelayMs { get; set; } = 1000;
    
    /// <summary>
    /// Maximum delay in milliseconds between retries
    /// </summary>
    public int MaxDelayMs { get; set; } = 5000;
    
    /// <summary>
    /// Multiplier for exponential backoff delay calculation
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;
    
    /// <summary>
    /// Whether to skip temporary files (e.g., .tmp, .crdownload)
    /// </summary>
    public bool SkipTemporaryFiles { get; set; } = true;
}

/// <summary>
/// Safety settings for delete operations
/// </summary>
public class DeleteSafetySettings
{
    /// <summary>
    /// Skip deleting system files
    /// </summary>
    public bool SkipSystemFiles { get; set; } = true;
    
    /// <summary>
    /// Skip deleting hidden files
    /// </summary>
    public bool SkipHiddenFiles { get; set; } = true;
    
    /// <summary>
    /// Maximum file size in bytes that can be deleted (1GB default)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 1_073_741_824; // 1GB
    
    /// <summary>
    /// Require confirmation for delete operations
    /// </summary>
    public bool RequireConfirmation { get; set; } = false;
    
    /// <summary>
    /// Move to recycle bin instead of permanent deletion
    /// </summary>
    public bool UseRecycleBin { get; set; } = true;
}

/// <summary>
/// Application settings and preferences
/// </summary>
public class AppSettings
{
    // Folder Monitoring Settings
    /// <summary>
    /// Folder path to monitor for file changes
    /// </summary>
    public string WatchFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
    
    /// <summary>
    /// Start monitoring automatically when application starts
    /// </summary>
    public bool StartOnStartup { get; set; } = false;
    
    /// <summary>
    /// Monitor subfolders recursively
    /// </summary>
    public bool MonitorSubfolders { get; set; } = false;
    
    /// <summary>
    /// Wait time in seconds for file operations to complete
    /// </summary>
    public int FileWaitTimeSeconds { get; set; } = 3;
    
    /// <summary>
    /// Process existing files when monitoring starts
    /// </summary>
    public bool ProcessExistingFiles { get; set; } = false;
    
    // Notification Settings
    /// <summary>
    /// Show system tray notifications
    /// </summary>
    public bool ShowNotifications { get; set; } = true;
    
    /// <summary>
    /// Show notification for each processed file
    /// </summary>
    public bool NotifyOnFileProcessed { get; set; } = false;
    
    // Window and Tray Settings
    /// <summary>
    /// Minimize application to system tray instead of taskbar
    /// </summary>
    public bool MinimizeToTray { get; set; } = true;
    
    /// <summary>
    /// Start application minimized to system tray
    /// </summary>
    public bool StartMinimized { get; set; } = false;
    
    /// <summary>
    /// Show balloon notifications from system tray (legacy property)
    /// </summary>
    public bool ShowTrayNotifications { get; set; } = true;
    
    /// <summary>
    /// Start application automatically with Windows
    /// </summary>
    public bool StartWithWindows { get; set; } = false;
    
    /// <summary>
    /// Close to tray instead of exiting when window is closed
    /// </summary>
    public bool CloseToTray { get; set; } = false;
    
    // Legacy property for compatibility
    /// <summary>
    /// Start application minimized to system tray (legacy)
    /// </summary>
    public bool StartMinimizedToTray { get; set; } = false;
    
    // Advanced Settings
    /// <summary>
    /// Enable detailed logging
    /// </summary>
    public bool EnableLogging { get; set; } = true;
    
    /// <summary>
    /// Path to log file
    /// </summary>
    public string LogFilePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FolderWatch", "logs", "app.log");
    
    /// <summary>
    /// Maximum number of concurrent file operations
    /// </summary>
    public int MaxConcurrentOperations { get; set; } = 5;
    
    /// <summary>
    /// File processing retry configuration
    /// </summary>
    public FileRetrySettings FileRetrySettings { get; set; } = new();
    
    /// <summary>
    /// Delete operation safety configuration
    /// </summary>
    public DeleteSafetySettings DeleteSafetySettings { get; set; } = new();
    
    // Theme Settings
    /// <summary>
    /// Selected MahApps Metro theme (Light, Dark)
    /// </summary>
    public string Theme { get; set; } = "Light";
    
    /// <summary>
    /// Selected MahApps Metro accent color
    /// </summary>
    public string AccentColor { get; set; } = "Blue";
    
    /// <summary>
    /// Last selected folder path for monitoring (legacy property)
    /// </summary>
    public string LastWatchedFolder { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether live logging is enabled (legacy property)
    /// </summary>
    public bool LiveLogEnabled { get; set; } = true;
}
