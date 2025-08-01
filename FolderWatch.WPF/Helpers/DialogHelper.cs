using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;

namespace FolderWatch.WPF.Helpers;

/// <summary>
/// Helper class for showing confirmation dialogs
/// </summary>
public static class DialogHelper
{
    /// <summary>
    /// Shows a confirmation dialog with Yes/No buttons
    /// </summary>
    /// <param name="window">The owner window</param>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Dialog message</param>
    /// <returns>True if user clicked Yes, false otherwise</returns>
    public static async Task<bool> ShowConfirmationAsync(Window window, string title, string message)
    {
        if (window is MetroWindow metroWindow)
        {
            var result = await metroWindow.ShowMessageAsync(
                title, 
                message, 
                MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings
                {
                    AffirmativeButtonText = "Yes",
                    NegativeButtonText = "No",
                    DialogTitleFontSize = 16,
                    DialogMessageFontSize = 14
                });
            
            return result == MessageDialogResult.Affirmative;
        }
        else
        {
            // Fallback to standard MessageBox
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
    }

    /// <summary>
    /// Shows an error dialog
    /// </summary>
    /// <param name="window">The owner window</param>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Error message</param>
    public static async Task ShowErrorAsync(Window window, string title, string message)
    {
        if (window is MetroWindow metroWindow)
        {
            await metroWindow.ShowMessageAsync(
                title, 
                message, 
                MessageDialogStyle.Affirmative,
                new MetroDialogSettings
                {
                    AffirmativeButtonText = "OK",
                    DialogTitleFontSize = 16,
                    DialogMessageFontSize = 14
                });
        }
        else
        {
            // Fallback to standard MessageBox
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Shows an information dialog
    /// </summary>
    /// <param name="window">The owner window</param>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Information message</param>
    public static async Task ShowInformationAsync(Window window, string title, string message)
    {
        if (window is MetroWindow metroWindow)
        {
            await metroWindow.ShowMessageAsync(
                title, 
                message, 
                MessageDialogStyle.Affirmative,
                new MetroDialogSettings
                {
                    AffirmativeButtonText = "OK",
                    DialogTitleFontSize = 16,
                    DialogMessageFontSize = 14
                });
        }
        else
        {
            // Fallback to standard MessageBox
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// Shows a progress dialog
    /// </summary>
    /// <param name="window">The owner window</param>
    /// <param name="title">Dialog title</param>
    /// <param name="message">Progress message</param>
    /// <returns>Progress dialog controller</returns>
    public static async Task<ProgressDialogController?> ShowProgressAsync(Window window, string title, string message)
    {
        if (window is MetroWindow metroWindow)
        {
            return await metroWindow.ShowProgressAsync(title, message, isCancelable: false);
        }
        
        return null;
    }
}