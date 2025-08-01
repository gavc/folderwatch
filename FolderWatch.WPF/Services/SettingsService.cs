using System.IO;
using System.Text.Json;
using FolderWatch.WPF.Models;

namespace FolderWatch.WPF.Services;

/// <summary>
/// Manages application settings with persistence
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private AppSettings _settings;

    public event Action? SettingsChanged;

    public AppSettings Settings => _settings;

    public SettingsService()
    {
        // Store settings in AppData folder
        var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FolderWatch");
        Directory.CreateDirectory(appDataFolder);
        _settingsFilePath = Path.Combine(appDataFolder, "settings.json");
        
        _settings = new AppSettings();
    }

    /// <summary>
    /// Loads settings from storage
    /// </summary>
    public async Task LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _settings = new AppSettings();
                await SaveSettingsAsync(); // Create default settings file
                return;
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception)
        {
            // If loading fails, use default settings
            _settings = new AppSettings();
        }

        SettingsChanged?.Invoke();
    }

    /// <summary>
    /// Saves current settings to storage
    /// </summary>
    public async Task SaveSettingsAsync()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(_settings, options);
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }
        catch (Exception)
        {
            // Silently fail - we don't want to crash the app if settings can't be saved
        }
    }

    /// <summary>
    /// Updates a setting and saves it
    /// </summary>
    public async Task UpdateSettingAsync<T>(string propertyName, T value)
    {
        var property = typeof(AppSettings).GetProperty(propertyName);
        if (property is not null && property.CanWrite)
        {
            property.SetValue(_settings, value);
            await SaveSettingsAsync();
            SettingsChanged?.Invoke();
        }
    }
}
