using System.Windows;
using FolderWatch.WPF.Models;
using FolderWatch.WPF.ViewModels;
using MahApps.Metro.Controls;

namespace FolderWatch.WPF.Views;

/// <summary>
/// Interaction logic for RuleEditorDialog.xaml
/// </summary>
public partial class RuleEditorDialog : MetroWindow
{
    private readonly RuleEditorViewModel _viewModel;

    public RuleEditorDialog(Rule? rule = null)
    {
        InitializeComponent();
        
        _viewModel = new RuleEditorViewModel();
        DataContext = _viewModel;
        
        // Initialize with the rule if provided
        _viewModel.Initialize(rule);
        
        // Subscribe to dialog result changes
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>
    /// Gets the configured rule if the dialog was accepted
    /// </summary>
    public Rule? Result => _viewModel.DialogResult ? _viewModel.CreateRule() : null;

    /// <summary>
    /// Handles property changes in the view model
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RuleEditorViewModel.DialogResult))
        {
            DialogResult = _viewModel.DialogResult;
            Close();
        }
    }

    /// <summary>
    /// Clean up resources when the dialog is closed
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.Dispose();
        base.OnClosed(e);
    }

    /// <summary>
    /// Shows the rule editor dialog
    /// </summary>
    /// <param name="owner">The owner window</param>
    /// <param name="rule">The rule to edit (null for new rule)</param>
    /// <returns>The configured rule if accepted, null if cancelled</returns>
    public static Rule? ShowDialog(Window owner, Rule? rule = null)
    {
        var dialog = new RuleEditorDialog(rule)
        {
            Owner = owner
        };
        
        return dialog.ShowDialog() == true ? dialog.Result : null;
    }
}