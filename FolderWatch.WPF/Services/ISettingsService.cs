using FolderWatch.WPF.Models;

namespace FolderWatch.WPF.Services;

/// <summary>
/// Interface for application settings management
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current application settings
    /// </summary>
    AppSettings Settings { get; }

    /// <summary>
    /// Loads settings from storage
    /// </summary>
    Task LoadSettingsAsync();

    /// <summary>
    /// Saves current settings to storage
    /// </summary>
    Task SaveSettingsAsync();

    /// <summary>
    /// Updates a setting and saves it
    /// </summary>
    Task UpdateSettingAsync<T>(string propertyName, T value);

    /// <summary>
    /// Event raised when settings are changed
    /// </summary>
    event Action? SettingsChanged;
}
