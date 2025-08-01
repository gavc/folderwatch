using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Windows;
using System.Windows.Input;
using FolderWatch.WPF.Helpers;
using FolderWatch.WPF.Models;
using Microsoft.Win32;

namespace FolderWatch.WPF.ViewModels;

/// <summary>
/// View model for the rule editor dialog
/// </summary>
public class RuleEditorViewModel : ViewModelBase
{
    private string _name = string.Empty;
    private string _pattern = string.Empty;
    private bool _enabled = true;
    private RuleStep? _selectedStep;
    private string _validationError = string.Empty;

    /// <summary>
    /// The rule being edited (null for new rules)
    /// </summary>
    public Rule? OriginalRule { get; private set; }

    /// <summary>
    /// Rule name
    /// </summary>
    [Required(ErrorMessage = "Rule name is required")]
    [StringLength(100, ErrorMessage = "Rule name must be 100 characters or less")]
    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                ValidateProperty(value);
                OnPropertyChanged(nameof(IsValid));
            }
        }
    }

    /// <summary>
    /// File pattern to match
    /// </summary>
    [Required(ErrorMessage = "File pattern is required")]
    public string Pattern
    {
        get => _pattern;
        set
        {
            if (SetProperty(ref _pattern, value))
            {
                ValidateProperty(value);
                OnPropertyChanged(nameof(IsValid));
            }
        }
    }

    /// <summary>
    /// Whether the rule is enabled
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    /// <summary>
    /// Collection of rule steps for multi-action workflows
    /// </summary>
    public ObservableCollection<RuleStep> Steps { get; } = new();

    /// <summary>
    /// Currently selected step
    /// </summary>
    public RuleStep? SelectedStep
    {
        get => _selectedStep;
        set
        {
            if (SetProperty(ref _selectedStep, value))
            {
                OnPropertyChanged(nameof(HasSelectedStep));
            }
        }
    }

    /// <summary>
    /// Available action types
    /// </summary>
    public RuleAction[] AvailableActions { get; } = Enum.GetValues<RuleAction>();

    /// <summary>
    /// Validation error message
    /// </summary>
    public string ValidationError
    {
        get => _validationError;
        private set => SetProperty(ref _validationError, value);
    }

    /// <summary>
    /// Whether the form is valid
    /// </summary>
    public bool IsValid => string.IsNullOrEmpty(ValidationError) && 
                          !string.IsNullOrWhiteSpace(Name) && 
                          !string.IsNullOrWhiteSpace(Pattern);

    /// <summary>
    /// Whether a step is selected
    /// </summary>
    public bool HasSelectedStep => SelectedStep is not null;

    /// <summary>
    /// Whether the dialog was accepted
    /// </summary>
    public bool DialogResult { get; private set; }

    // Commands
    public ICommand AddStepCommand { get; }
    public ICommand EditStepCommand { get; }
    public ICommand DeleteStepCommand { get; }
    public ICommand MoveStepUpCommand { get; }
    public ICommand MoveStepDownCommand { get; }
    public ICommand BrowseDestinationCommand { get; }
    public ICommand AcceptCommand { get; }
    public ICommand CancelCommand { get; }

    public RuleEditorViewModel()
    {
        // Initialize commands
        AddStepCommand = new RelayCommand(AddStep);
        EditStepCommand = new RelayCommand<RuleStep>(EditStep, step => step is not null);
        DeleteStepCommand = new RelayCommand<RuleStep>(DeleteStep, step => step is not null);
        MoveStepUpCommand = new RelayCommand<RuleStep>(MoveStepUp, CanMoveStepUp);
        MoveStepDownCommand = new RelayCommand<RuleStep>(MoveStepDown, CanMoveStepDown);
        BrowseDestinationCommand = new RelayCommand<RuleStep>(BrowseDestination, step => step is not null);
        AcceptCommand = new RelayCommand(Accept, () => IsValid);
        CancelCommand = new RelayCommand(Cancel);
    }

    /// <summary>
    /// Initializes the view model for editing an existing rule
    /// </summary>
    /// <param name="rule">The rule to edit</param>
    public void Initialize(Rule? rule = null)
    {
        OriginalRule = rule;
        
        if (rule is not null)
        {
            Name = rule.Name;
            Pattern = rule.Pattern;
            Enabled = rule.Enabled;
            
            // Load steps from rule
            Steps.Clear();
            foreach (var step in rule.Steps)
            {
                Steps.Add(new RuleStep
                {
                    Action = step.Action,
                    Destination = step.Destination,
                    NewName = step.NewName,
                    Enabled = step.Enabled
                });
            }
        }
        else
        {
            // New rule defaults
            Name = $"New Rule {DateTime.Now:HHmmss}";
            Pattern = "*.*";
            Enabled = true;
            Steps.Clear();
        }
        
        ValidateAll();
    }

    /// <summary>
    /// Creates a rule object from the current form data
    /// </summary>
    /// <returns>The configured rule</returns>
    public Rule CreateRule()
    {
        var rule = new Rule
        {
            Name = Name.Trim(),
            Pattern = Pattern.Trim(),
            Enabled = Enabled
        };

        // Copy steps
        rule.Steps.Clear();
        foreach (var step in Steps)
        {
            rule.Steps.Add(new RuleStep
            {
                Action = step.Action,
                Destination = step.Destination?.Trim() ?? string.Empty,
                NewName = step.NewName?.Trim() ?? string.Empty,
                Enabled = step.Enabled
            });
        }

        return rule;
    }

    /// <summary>
    /// Adds a new step to the workflow
    /// </summary>
    private void AddStep()
    {
        var newStep = new RuleStep
        {
            Action = RuleAction.Move,
            Enabled = true
        };
        
        Steps.Add(newStep);
        SelectedStep = newStep;
    }

    /// <summary>
    /// Edits the selected step (placeholder for future step editor dialog)
    /// </summary>
    private void EditStep(RuleStep? step)
    {
        if (step is null) return;
        
        // For now, just select the step for inline editing
        SelectedStep = step;
    }

    /// <summary>
    /// Deletes the specified step
    /// </summary>
    private void DeleteStep(RuleStep? step)
    {
        if (step is null) return;
        
        var index = Steps.IndexOf(step);
        Steps.Remove(step);
        
        // Select next step or previous if at end
        if (Steps.Count > 0)
        {
            if (index < Steps.Count)
            {
                SelectedStep = Steps[index];
            }
            else if (index > 0)
            {
                SelectedStep = Steps[index - 1];
            }
        }
        else
        {
            SelectedStep = null;
        }
    }

    /// <summary>
    /// Moves a step up in the list
    /// </summary>
    private void MoveStepUp(RuleStep? step)
    {
        if (step is null) return;
        
        var index = Steps.IndexOf(step);
        if (index > 0)
        {
            Steps.Move(index, index - 1);
        }
    }

    /// <summary>
    /// Moves a step down in the list
    /// </summary>
    private void MoveStepDown(RuleStep? step)
    {
        if (step is null) return;
        
        var index = Steps.IndexOf(step);
        if (index < Steps.Count - 1)
        {
            Steps.Move(index, index + 1);
        }
    }

    /// <summary>
    /// Checks if a step can be moved up
    /// </summary>
    private bool CanMoveStepUp(RuleStep? step)
    {
        return step is not null && Steps.IndexOf(step) > 0;
    }

    /// <summary>
    /// Checks if a step can be moved down
    /// </summary>
    private bool CanMoveStepDown(RuleStep? step)
    {
        return step is not null && Steps.IndexOf(step) < Steps.Count - 1;
    }

    /// <summary>
    /// Opens folder browser for destination selection
    /// </summary>
    private void BrowseDestination(RuleStep? step)
    {
        if (step is null) return;
        
        try
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Destination Folder"
            };

            if (!string.IsNullOrWhiteSpace(step.Destination) && Directory.Exists(step.Destination))
            {
                dialog.InitialDirectory = step.Destination;
            }

            if (dialog.ShowDialog() == true)
            {
                step.Destination = dialog.FolderName;
            }
        }
        catch (Exception)
        {
            // Fallback to FolderBrowserDialog or let user type path manually
            // This handles cases where OpenFolderDialog is not available
            MessageBox.Show(
                "Please enter the destination folder path manually in the text field.",
                "Folder Browser",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// Accepts the dialog and validates input
    /// </summary>
    private void Accept()
    {
        ValidateAll();
        
        if (IsValid)
        {
            DialogResult = true;
        }
    }

    /// <summary>
    /// Cancels the dialog
    /// </summary>
    private void Cancel()
    {
        DialogResult = false;
    }

    /// <summary>
    /// Validates all form fields
    /// </summary>
    private void ValidateAll()
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Rule name is required");
        }
        else if (Name.Length > 100)
        {
            errors.Add("Rule name must be 100 characters or less");
        }
        
        if (string.IsNullOrWhiteSpace(Pattern))
        {
            errors.Add("File pattern is required");
        }
        else
        {
            var patternErrors = ValidationHelper.ValidateFilePattern(Pattern);
            errors.AddRange(patternErrors);
        }
        
        // Validate steps
        for (int i = 0; i < Steps.Count; i++)
        {
            var step = Steps[i];
            var stepErrors = step.Validate();
            foreach (var error in stepErrors)
            {
                errors.Add($"Step {i + 1}: {error}");
            }
        }
        
        ValidationError = string.Join(Environment.NewLine, errors);
        OnPropertyChanged(nameof(IsValid));
    }

    /// <summary>
    /// Validates a single step (kept for backward compatibility)
    /// </summary>
    private static List<string> ValidateStep(RuleStep step, int stepNumber)
    {
        var stepErrors = step.Validate();
        return stepErrors.Select(error => $"Step {stepNumber}: {error}").ToList();
    }

    /// <summary>
    /// Validates a property value using data annotations
    /// </summary>
    private void ValidateProperty(object value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        // This is a simplified validation - in a real app you might use a validation framework
        ValidateAll();
    }
}