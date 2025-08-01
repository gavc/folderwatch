namespace FolderWatch.WPF.Services;

/// <summary>
/// Interface for theme management operations
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the current theme name
    /// </summary>
    string CurrentTheme { get; }

    /// <summary>
    /// Gets the current accent color
    /// </summary>
    string CurrentAccent { get; }

    /// <summary>
    /// Gets available theme names
    /// </summary>
    IEnumerable<string> AvailableThemes { get; }

    /// <summary>
    /// Gets available accent color names
    /// </summary>
    IEnumerable<string> AvailableAccents { get; }

    /// <summary>
    /// Changes the application theme
    /// </summary>
    void ChangeTheme(string themeName);

    /// <summary>
    /// Changes the application accent color
    /// </summary>
    void ChangeAccent(string accentName);

    /// <summary>
    /// Changes both theme and accent
    /// </summary>
    void ChangeTheme(string themeName, string accentName);

    /// <summary>
    /// Event raised when the theme changes
    /// </summary>
    event Action<string, string>? ThemeChanged;
}
