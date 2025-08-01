namespace FolderWatch.WPF.Models;

/// <summary>
/// Defines the types of actions that can be performed on files
/// </summary>
public enum RuleAction
{
    /// <summary>
    /// Copy file to destination folder
    /// </summary>
    Copy,
    
    /// <summary>
    /// Move file to destination folder
    /// </summary>
    Move,
    
    /// <summary>
    /// Rename file with new pattern
    /// </summary>
    Rename,
    
    /// <summary>
    /// Delete file permanently
    /// </summary>
    Delete,
    
    /// <summary>
    /// Add date/time to filename
    /// </summary>
    DateTime,
    
    /// <summary>
    /// Add sequential number to filename
    /// </summary>
    Numbering
}
