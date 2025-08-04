using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace FolderWatch.WPF.Views;

/// <summary>
/// Simple input dialog for getting text input from the user
/// </summary>
public partial class InputDialog : MetroWindow
{
    /// <summary>
    /// Gets the text entered by the user
    /// </summary>
    public string InputText => InputTextBox.Text;

    /// <summary>
    /// Initializes a new instance of the InputDialog
    /// </summary>
    /// <param name="title">The dialog title</param>
    /// <param name="prompt">The prompt text</param>
    /// <param name="defaultValue">The default input value</param>
    public InputDialog(string title, string prompt, string defaultValue = "")
    {
        InitializeComponent();
        
        Title = title;
        PromptTextBlock.Text = prompt;
        InputTextBox.Text = defaultValue;
        
        // Focus the text box and select all text
        Loaded += (s, e) =>
        {
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        };
    }

    /// <summary>
    /// Handles the OK button click
    /// </summary>
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Handles the Cancel button click
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Handles key down events in the input text box
    /// </summary>
    private void InputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OkButton_Click(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            CancelButton_Click(sender, e);
        }
    }
}