using System.Windows;
using ControlzEx.Theming;

namespace FolderWatch.WPF.Services;

/// <summary>
/// Manages MahApps.Metro theme changes
/// </summary>
public class ThemeService : IThemeService
{
    public string CurrentTheme { get; private set; } = "Light";
    public string CurrentAccent { get; private set; } = "Blue";

    public event Action<string, string>? ThemeChanged;

    /// <summary>
    /// Available theme names (Light/Dark)
    /// </summary>
    public IEnumerable<string> AvailableThemes => new[] { "Light", "Dark" };

    /// <summary>
    /// Available accent color names
    /// </summary>
    public IEnumerable<string> AvailableAccents => new[]
    {
        "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald",
        "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta",
        "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna"
    };

    /// <summary>
    /// Changes the application theme
    /// </summary>
    public void ChangeTheme(string themeName)
    {
        ChangeTheme(themeName, CurrentAccent);
    }

    /// <summary>
    /// Changes the application accent color
    /// </summary>
    public void ChangeAccent(string accentName)
    {
        ChangeTheme(CurrentTheme, accentName);
    }

    /// <summary>
    /// Changes both theme and accent
    /// </summary>
    public void ChangeTheme(string themeName, string accentName)
    {
        if (!AvailableThemes.Contains(themeName))
            throw new ArgumentException($"Invalid theme name: {themeName}", nameof(themeName));

        if (!AvailableAccents.Contains(accentName))
            throw new ArgumentException($"Invalid accent name: {accentName}", nameof(accentName));

        try
        {
            // Apply the theme change to the current application using ControlzEx
            ThemeManager.Current.ChangeTheme(Application.Current, $"{themeName}.{accentName}");
            
            CurrentTheme = themeName;
            CurrentAccent = accentName;
            
            ThemeChanged?.Invoke(CurrentTheme, CurrentAccent);
        }
        catch (Exception)
        {
            // If theme change fails, silently continue with current theme
        }
    }
}
