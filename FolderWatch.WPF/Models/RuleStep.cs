namespace FolderWatch.WPF.Models;

/// <summary>
/// Represents an individual step in a multi-step rule workflow
/// </summary>
public class RuleStep
{
    /// <summary>
    /// The action to perform in this step
    /// </summary>
    public RuleAction Action { get; set; }
    
    /// <summary>
    /// The destination folder for copy/move operations
    /// </summary>
    public string Destination { get; set; } = string.Empty;
    
    /// <summary>
    /// The new name pattern for rename operations
    /// </summary>
    public string NewName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this step is enabled for execution
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Description of what this step does (for UI display)
    /// </summary>
    public string Description => Action switch
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
