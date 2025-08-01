using System.Text.RegularExpressions;

namespace FolderWatch.WPF.Helpers;

/// <summary>
/// Utility class for validating file patterns and rules
/// </summary>
public static class ValidationHelper
{
    private static readonly Regex ValidPatternRegex = new(@"^[^<>:""/\\|?*]+$", RegexOptions.Compiled);
    private static readonly Regex PathRegex = new(@"^[a-zA-Z]:\\(?:[^<>:""/\\|?*]+\\)*[^<>:""/\\|?*]*$", RegexOptions.Compiled);

    /// <summary>
    /// Validates a file pattern (supports wildcards)
    /// </summary>
    /// <param name="pattern">The pattern to validate</param>
    /// <returns>List of validation errors</returns>
    public static List<string> ValidateFilePattern(string pattern)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(pattern))
        {
            errors.Add("File pattern cannot be empty");
            return errors;
        }

        // Check for invalid characters (excluding wildcards)
        var patternWithoutWildcards = pattern.Replace("*", "").Replace("?", "");
        if (!ValidPatternRegex.IsMatch(patternWithoutWildcards))
        {
            errors.Add("File pattern contains invalid characters");
        }

        // Check for valid wildcard usage
        if (pattern.Contains("**"))
        {
            errors.Add("Double wildcards (**) are not supported");
        }

        return errors;
    }

    /// <summary>
    /// Validates a file path
    /// </summary>
    /// <param name="path">The path to validate</param>
    /// <param name="checkExists">Whether to check if the path exists</param>
    /// <returns>List of validation errors</returns>
    public static List<string> ValidatePath(string path, bool checkExists = false)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add("Path cannot be empty");
            return errors;
        }

        try
        {
            // Basic path validation
            if (!Path.IsPathFullyQualified(path))
            {
                errors.Add("Path must be a fully qualified path (e.g., C:\\folder)");
            }

            // Check for invalid characters
            var invalidChars = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0)
            {
                errors.Add("Path contains invalid characters");
            }

            // Check if path exists (if requested)
            if (checkExists && !Directory.Exists(path))
            {
                errors.Add("Path does not exist");
            }
        }
        catch (Exception)
        {
            errors.Add("Invalid path format");
        }

        return errors;
    }

    /// <summary>
    /// Validates a rename pattern
    /// </summary>
    /// <param name="pattern">The rename pattern to validate</param>
    /// <returns>List of validation errors</returns>
    public static List<string> ValidateRenamePattern(string pattern)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(pattern))
        {
            errors.Add("Rename pattern cannot be empty");
            return errors;
        }

        // Check for invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        if (pattern.IndexOfAny(invalidChars) >= 0)
        {
            errors.Add("Rename pattern contains invalid filename characters");
        }

        return errors;
    }

    /// <summary>
    /// Gets example patterns for user guidance
    /// </summary>
    /// <returns>List of example patterns with descriptions</returns>
    public static List<(string Pattern, string Description)> GetPatternExamples()
    {
        return new List<(string, string)>
        {
            ("*.*", "All files"),
            ("*.txt", "All text files"),
            ("*.jpg", "All JPEG images"),
            ("photo_*.*", "Files starting with 'photo_'"),
            ("document*.pdf", "PDF files starting with 'document'"),
            ("report_2024_*.*", "Files starting with 'report_2024_'"),
            ("*.log", "All log files"),
            ("backup_*.zip", "ZIP files starting with 'backup_'")
        };
    }

    /// <summary>
    /// Gets example rename patterns for user guidance
    /// </summary>
    /// <returns>List of example rename patterns with descriptions</returns>
    public static List<(string Pattern, string Description)> GetRenamePatternExamples()
    {
        return new List<(string, string)>
        {
            ("{filename}_processed", "Add '_processed' suffix"),
            ("backup_{filename}", "Add 'backup_' prefix"),
            ("{filename}_{datetime:yyyyMMdd}", "Add date suffix"),
            ("{filename}_{datetime:HHmmss}", "Add time suffix"),
            ("archive_{counter:000}_{filename}", "Add counter with prefix"),
            ("{filename}_v{counter}", "Add version number"),
            ("processed_{datetime:yyyy-MM-dd}_{filename}", "Add date prefix"),
            ("{filename}.backup", "Add '.backup' extension")
        };
    }
}