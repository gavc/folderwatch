using System.Text.RegularExpressions;

namespace FolderWatch.WPF.Helpers;

/// <summary>
/// Provides file pattern matching functionality using glob-style wildcards
/// </summary>
public static class PatternMatcher
{
    /// <summary>
    /// Checks if a filename matches the specified pattern
    /// </summary>
    /// <param name="pattern">The pattern to match (supports * and ? wildcards)</param>
    /// <param name="fileName">The filename to test</param>
    /// <returns>True if the filename matches the pattern</returns>
    public static bool IsMatch(string pattern, string fileName)
    {
        if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(fileName))
            return false;

        // Handle multiple extensions like *.{jpg,png,gif}
        if (pattern.Contains('{') && pattern.Contains('}'))
        {
            return IsMatchWithMultipleExtensions(pattern, fileName);
        }

        // Convert glob pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace(@"\*", ".*")
            .Replace(@"\?", ".") + "$";

        return Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Handles patterns with multiple extensions like *.{jpg,png,gif}
    /// </summary>
    private static bool IsMatchWithMultipleExtensions(string pattern, string fileName)
    {
        // Extract the part before { and after }
        var braceStart = pattern.IndexOf('{');
        var braceEnd = pattern.IndexOf('}');
        
        if (braceStart == -1 || braceEnd == -1 || braceEnd <= braceStart)
            return false;

        var prefix = pattern[..braceStart];
        var suffix = pattern[(braceEnd + 1)..];
        var extensions = pattern.Substring(braceStart + 1, braceEnd - braceStart - 1)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(ext => ext.Trim());

        // Test each extension
        foreach (var extension in extensions)
        {
            var testPattern = prefix + extension + suffix;
            if (IsMatch(testPattern, fileName))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a user-friendly description of what files the pattern will match
    /// </summary>
    /// <param name="pattern">The pattern to describe</param>
    /// <returns>A description of the pattern</returns>
    public static string GetPatternDescription(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return "No pattern specified";

        return pattern switch
        {
            "*" => "All files",
            "*.*" => "All files with extensions",
            var p when p.StartsWith("*.") && !p.Substring(2).Contains('*') => $"All {p[2..].ToUpper()} files",
            var p when p.Contains('{') && p.Contains('}') => $"Files matching {p}",
            var p when p.Contains('*') => $"Files matching pattern: {p}",
            var p when p.Contains('?') => $"Files matching pattern: {p}",
            _ => $"Exact filename: {pattern}"
        };
    }

    /// <summary>
    /// Validates if a pattern is syntactically correct
    /// </summary>
    /// <param name="pattern">The pattern to validate</param>
    /// <returns>True if the pattern is valid</returns>
    public static bool IsValidPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        try
        {
            // Try to convert to regex to check for valid syntax
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace(@"\*", ".*")
                .Replace(@"\?", ".") + "$";
            
            // Test if the regex compiles
            _ = new Regex(regexPattern, RegexOptions.IgnoreCase);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
