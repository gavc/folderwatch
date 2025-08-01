using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FolderWatch.WPF.Models;

/// <summary>
/// Represents an individual step in a multi-step rule workflow
/// </summary>
public class RuleStep : INotifyPropertyChanged
{
    private RuleAction _action;
    private string _destination = string.Empty;
    private string _newName = string.Empty;
    private bool _enabled = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// The action to perform in this step
    /// </summary>
    public RuleAction Action
    {
        get => _action;
        set
        {
            if (_action != value)
            {
                _action = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Description));
            }
        }
    }
    
    /// <summary>
    /// The destination folder for copy/move operations
    /// </summary>
    public string Destination
    {
        get => _destination;
        set
        {
            if (_destination != value)
            {
                _destination = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Description));
            }
        }
    }
    
    /// <summary>
    /// The new name pattern for rename operations
    /// </summary>
    public string NewName
    {
        get => _newName;
        set
        {
            if (_newName != value)
            {
                _newName = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Description));
            }
        }
    }
    
    /// <summary>
    /// Whether this step is enabled for execution
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                OnPropertyChanged();
            }
        }
    }
    
    /// <summary>
    /// Description of what this step does (for UI display)
    /// </summary>
    public string Description => Action switch
    {
        RuleAction.Copy => string.IsNullOrWhiteSpace(Destination) ? "Copy to [destination]" : $"Copy to {Destination}",
        RuleAction.Move => string.IsNullOrWhiteSpace(Destination) ? "Move to [destination]" : $"Move to {Destination}",
        RuleAction.Rename => string.IsNullOrWhiteSpace(NewName) ? "Rename to [pattern]" : $"Rename to {NewName}",
        RuleAction.Delete => "Delete file",
        RuleAction.DateTime => string.IsNullOrWhiteSpace(NewName) ? "Add date/time [pattern]" : $"Add date/time: {NewName}",
        RuleAction.Numbering => string.IsNullOrWhiteSpace(NewName) ? "Add number [pattern]" : $"Add number: {NewName}",
        _ => "Unknown action"
    };

    /// <summary>
    /// Validates this step and returns any error messages
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        switch (Action)
        {
            case RuleAction.Copy:
            case RuleAction.Move:
                if (string.IsNullOrWhiteSpace(Destination))
                {
                    errors.Add($"Destination folder is required for {Action} action");
                }
                break;

            case RuleAction.Rename:
            case RuleAction.DateTime:
            case RuleAction.Numbering:
                if (string.IsNullOrWhiteSpace(NewName))
                {
                    errors.Add($"Name pattern is required for {Action} action");
                }
                break;
        }

        return errors;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
