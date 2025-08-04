namespace FolderWatch.WPF.Models;

/// <summary>
/// Represents the result of testing a rule against a filename
/// </summary>
public class RuleTestResult
{
    /// <summary>
    /// Whether the rule pattern matched the filename
    /// </summary>
    public bool IsMatch { get; set; }

    /// <summary>
    /// The rule that was tested
    /// </summary>
    public Rule Rule { get; set; } = new();

    /// <summary>
    /// The filename that was tested
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Preview of actions that would be executed
    /// </summary>
    public List<RuleActionPreview> ActionPreviews { get; set; } = new();

    /// <summary>
    /// Any validation errors found during testing
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Whether the rule passed validation
    /// </summary>
    public bool IsValid => ValidationErrors.Count == 0;

    /// <summary>
    /// Summary description of the test result
    /// </summary>
    public string Summary => IsMatch 
        ? $"✓ Rule '{Rule.Name}' matches '{FileName}' - {ActionPreviews.Count} actions would execute"
        : $"✗ Rule '{Rule.Name}' does not match '{FileName}'";
}

/// <summary>
/// Represents a preview of a rule action that would be executed
/// </summary>
public class RuleActionPreview
{
    /// <summary>
    /// The action that would be performed
    /// </summary>
    public RuleAction Action { get; set; }

    /// <summary>
    /// The current file path at this step
    /// </summary>
    public string CurrentPath { get; set; } = string.Empty;

    /// <summary>
    /// The resulting file path after this action
    /// </summary>
    public string ResultPath { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this action would do
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this action would succeed
    /// </summary>
    public bool WouldSucceed { get; set; } = true;

    /// <summary>
    /// Error message if the action would fail
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether this is a potentially destructive action (like delete)
    /// </summary>
    public bool IsDestructive => Action == RuleAction.Delete;

    /// <summary>
    /// Whether this action has warnings (like overwriting existing files)
    /// </summary>
    public bool HasWarnings { get; set; }

    /// <summary>
    /// Warning message if applicable
    /// </summary>
    public string? WarningMessage { get; set; }
}