using FolderWatch.WPF.Models;

namespace FolderWatch.WPF.Services;

/// <summary>
/// Interface for file monitoring operations
/// </summary>
public interface IFileMonitorService
{
    /// <summary>
    /// Gets whether monitoring is currently active
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Gets the currently monitored folder path
    /// </summary>
    string? MonitoredPath { get; }

    /// <summary>
    /// Starts monitoring the specified folder
    /// </summary>
    Task StartMonitoringAsync(string folderPath);

    /// <summary>
    /// Stops monitoring
    /// </summary>
    Task StopMonitoringAsync();

    /// <summary>
    /// Event raised when monitoring status changes
    /// </summary>
    event Action<bool>? MonitoringStatusChanged;

    /// <summary>
    /// Event raised when a file event occurs
    /// </summary>
    event Action<string>? FileEventLogged;
}
