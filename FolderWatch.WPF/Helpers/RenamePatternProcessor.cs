using System.Text.RegularExpressions;

namespace FolderWatch.WPF.Helpers;

/// <summary>
/// Processes rename patterns with variable substitution
/// </summary>
public static class RenamePatternProcessor
{
    private static readonly Regex VariableRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled);
    private static int _fileCounter = 1;
    private static readonly object _counterLock = new();

    /// <summary>
    /// Processes a rename pattern and substitutes variables
    /// </summary>
    /// <param name="pattern">The pattern containing variables like {filename}, {datetime}, {counter}</param>
    /// <param name="originalFilePath">The original file path</param>
    /// <param name="useDateTime">Custom datetime to use (optional)</param>
    /// <returns>The processed filename</returns>
    public static string ProcessPattern(string pattern, string originalFilePath, DateTime? useDateTime = null)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return Path.GetFileName(originalFilePath);

        var fileName = Path.GetFileNameWithoutExtension(originalFilePath);
        var extension = Path.GetExtension(originalFilePath);
        var currentDateTime = useDateTime ?? DateTime.Now;

        // Replace variables in the pattern
        var result = VariableRegex.Replace(pattern, match =>
        {
            var variable = match.Groups[1].Value.ToLowerInvariant();
            
            return variable switch
            {
                "filename" => fileName,
                "extension" => extension.TrimStart('.'),
                "fullextension" => extension,
                var dt when dt.StartsWith("datetime") => ProcessDateTimeVariable(dt, currentDateTime),
                var dt when dt.StartsWith("date") => ProcessDateTimeVariable(dt, currentDateTime),
                var dt when dt.StartsWith("time") => ProcessDateTimeVariable(dt, currentDateTime),
                var c when c.StartsWith("counter") => ProcessCounterVariable(c),
                var guid when guid == "guid" => Guid.NewGuid().ToString("N")[..8], // Short GUID
                var guidfull when guidfull == "guidfull" => Guid.NewGuid().ToString(),
                _ => match.Value // Keep original if not recognized
            };
        });

        // Ensure we have an extension if the original file had one
        if (!string.IsNullOrEmpty(extension) && !result.EndsWith(extension))
        {
            result += extension;
        }

        return result;
    }

    /// <summary>
    /// Processes datetime variables with custom formats
    /// </summary>
    /// <param name="variable">The datetime variable (e.g., "datetime:yyyyMMdd")</param>
    /// <param name="dateTime">The datetime to format</param>
    /// <returns>The formatted datetime string</returns>
    private static string ProcessDateTimeVariable(string variable, DateTime dateTime)
    {
        // Extract format if present (e.g., "datetime:yyyyMMdd")
        var parts = variable.Split(':', 2);
        var format = parts.Length > 1 ? parts[1] : GetDefaultDateTimeFormat(parts[0]);
        
        try
        {
            return dateTime.ToString(format);
        }
        catch
        {
            // Fallback to default format if custom format is invalid
            return dateTime.ToString(GetDefaultDateTimeFormat(parts[0]));
        }
    }

    /// <summary>
    /// Gets the default format for datetime variables
    /// </summary>
    /// <param name="variableType">The type of datetime variable</param>
    /// <returns>The default format string</returns>
    private static string GetDefaultDateTimeFormat(string variableType)
    {
        return variableType switch
        {
            "date" => "yyyy-MM-dd",
            "time" => "HH-mm-ss",
            "datetime" => "yyyyMMdd_HHmmss",
            _ => "yyyyMMdd_HHmmss"
        };
    }

    /// <summary>
    /// Processes counter variables with custom formats
    /// </summary>
    /// <param name="variable">The counter variable (e.g., "counter:000")</param>
    /// <returns>The formatted counter string</returns>
    private static string ProcessCounterVariable(string variable)
    {
        // Extract format if present (e.g., "counter:000")
        var parts = variable.Split(':', 2);
        var format = parts.Length > 1 ? parts[1] : "000";
        
        int counterValue;
        lock (_counterLock)
        {
            counterValue = _fileCounter++;
        }
        
        try
        {
            return counterValue.ToString(format);
        }
        catch
        {
            // Fallback to simple number if format is invalid
            return counterValue.ToString();
        }
    }

    /// <summary>
    /// Resets the file counter
    /// </summary>
    public static void ResetCounter()
    {
        lock (_counterLock)
        {
            _fileCounter = 1;
        }
    }

    /// <summary>
    /// Gets available variable examples for user guidance
    /// </summary>
    /// <returns>List of variable examples with descriptions</returns>
    public static List<(string Variable, string Description, string Example)> GetVariableExamples()
    {
        var now = DateTime.Now;
        var sampleFile = "document.txt";
        
        return new List<(string, string, string)>
        {
            ("{filename}", "Original filename without extension", "document"),
            ("{extension}", "File extension without dot", "txt"),
            ("{fullextension}", "File extension with dot", ".txt"),
            ("{datetime}", "Current date and time", ProcessPattern("{datetime}", sampleFile, now)),
            ("{date}", "Current date", ProcessPattern("{date}", sampleFile, now)),
            ("{time}", "Current time", ProcessPattern("{time}", sampleFile, now)),
            ("{datetime:yyyyMMdd}", "Custom date format", ProcessPattern("{datetime:yyyyMMdd}", sampleFile, now)),
            ("{datetime:HH-mm-ss}", "Custom time format", ProcessPattern("{datetime:HH-mm-ss}", sampleFile, now)),
            ("{counter}", "Sequential number", "001"),
            ("{counter:00}", "2-digit counter", "01"),
            ("{counter:0000}", "4-digit counter", "0001"),
            ("{guid}", "Short unique ID", "a1b2c3d4"),
            ("{guidfull}", "Full unique ID", "12345678-1234-1234-1234-123456789abc")
        };
    }

    /// <summary>
    /// Validates a rename pattern
    /// </summary>
    /// <param name="pattern">The pattern to validate</param>
    /// <returns>List of validation errors</returns>
    public static List<string> ValidatePattern(string pattern)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(pattern))
        {
            errors.Add("Pattern cannot be empty");
            return errors;
        }

        // Check for invalid filename characters (excluding variables)
        var patternWithoutVariables = VariableRegex.Replace(pattern, "X");
        var invalidChars = Path.GetInvalidFileNameChars();
        if (patternWithoutVariables.IndexOfAny(invalidChars) >= 0)
        {
            errors.Add("Pattern contains invalid filename characters");
        }

        // Validate variable syntax
        var matches = VariableRegex.Matches(pattern);
        foreach (Match match in matches)
        {
            var variable = match.Groups[1].Value.ToLowerInvariant();
            if (!IsValidVariable(variable))
            {
                errors.Add($"Unknown variable: {{{variable}}}");
            }
        }

        return errors;
    }

    /// <summary>
    /// Checks if a variable is valid
    /// </summary>
    /// <param name="variable">The variable to check</param>
    /// <returns>True if the variable is valid</returns>
    private static bool IsValidVariable(string variable)
    {
        return variable switch
        {
            "filename" => true,
            "extension" => true,
            "fullextension" => true,
            "guid" => true,
            "guidfull" => true,
            var dt when dt.StartsWith("datetime") => true,
            var d when d.StartsWith("date") => true,
            var t when t.StartsWith("time") => true,
            var c when c.StartsWith("counter") => true,
            _ => false
        };
    }
}