using System.Collections.ObjectModel;

namespace FolderWatch.WPF.Models;

/// <summary>
/// Represents a file processing rule with pattern matching and actions
/// </summary>
public class Rule
{
    /// <summary>
    /// Display name for the rule
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// File pattern to match (supports wildcards like *.txt, photo_*.jpg)
    /// </summary>
    public string Pattern { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this rule is enabled for processing
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Primary action to perform (for simple rules)
    /// </summary>
    public RuleAction Action { get; set; } = RuleAction.Move;
    
    /// <summary>
    /// Destination folder for simple copy/move operations
    /// </summary>
    public string Destination { get; set; } = string.Empty;
    
    /// <summary>
    /// New name pattern for simple rename operations
    /// </summary>
    public string NewName { get; set; } = string.Empty;
    
    /// <summary>
    /// Multi-step workflow (takes precedence over simple action if not empty)
    /// </summary>
    public ObservableCollection<RuleStep> Steps { get; set; } = new();
    
    /// <summary>
    /// Indicates whether this rule uses multi-step processing
    /// </summary>
    public bool HasMultipleSteps => Steps.Count > 0;
    
    /// <summary>
    /// Gets a description of the rule for UI display
    /// </summary>
    public string Description => HasMultipleSteps 
        ? $"{Steps.Count} step workflow" 
        : Action switch
        {
            RuleAction.Copy => $"Copy to {Destination}",
            RuleAction.Move => $"Move to {Destination}",
            RuleAction.Rename => $"Rename to {NewName}",
            RuleAction.Delete => "Delete file",
            RuleAction.DateTime => $"Add date/time: {NewName}",
            RuleAction.Numbering => $"Add number: {NewName}",
            _ => "Unknown action"
        };
}
