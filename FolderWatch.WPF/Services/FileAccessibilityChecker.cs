using System.IO;

namespace FolderWatch.WPF.Services;

/// <summary>
/// Result of a file access check operation
/// </summary>
/// <param name="IsAccessible">Whether the file is accessible</param>
/// <param name="Message">Descriptive message about the result</param>
/// <param name="RetriesUsed">Number of retry attempts used</param>
public record FileAccessResult(bool IsAccessible, string Message, int RetriesUsed);

/// <summary>
/// Result of a file readiness check
/// </summary>
/// <param name="IsReady">Whether the file is ready for processing</param>
/// <param name="Reason">Reason why the file is not ready</param>
/// <param name="Message">Descriptive message about the result</param>
public record FileReadinessResult(bool IsReady, FileNotReadyReason Reason, string Message);

/// <summary>
/// Reasons why a file might not be ready for processing
/// </summary>
public enum FileNotReadyReason
{
    None,
    InvalidPath,
    TemporaryFile,
    FileNotFound,
    FileNotAccessible
}

/// <summary>
/// Provides robust file accessibility checking with temporary file detection and retry logic
/// </summary>
public static class FileAccessibilityChecker
{
    /// <summary>
    /// Default configuration for retry operations
    /// </summary>
    public static class DefaultRetryConfig
    {
        public const int MaxRetries = 3;
        public const int InitialDelayMs = 1000;
        public const int MaxDelayMs = 5000;
        public const double BackoffMultiplier = 2.0;
    }

    /// <summary>
    /// Known temporary file extensions that should be skipped
    /// </summary>
    private static readonly HashSet<string> TemporaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Browser downloads
        ".crdownload",    // Chrome
        ".download",      // Firefox
        ".part",          // Firefox/general
        ".partial",       // General
        ".opdownload",    // Opera
        ".safari-download", // Safari
        
        // Torrent clients
        ".!ut",           // uTorrent
        ".bc!",           // BitComet
        
        // Generic temporary
        ".tmp",
        ".temp",
        ".downloading"
    };

    /// <summary>
    /// Checks if a file appears to be a temporary or incomplete download
    /// </summary>
    /// <param name="filePath">Path to the file to check</param>
    /// <returns>True if the file appears to be temporary</returns>
    public static bool IsTemporaryFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        var extension = Path.GetExtension(filePath);
        return TemporaryExtensions.Contains(extension);
    }

    /// <summary>
    /// Checks if a file is accessible (not locked by another process)
    /// </summary>
    /// <param name="filePath">Path to the file to check</param>
    /// <returns>True if the file is accessible</returns>
    public static bool IsFileAccessible(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            return true;
        }
        catch (IOException)
        {
            // File is locked or inaccessible
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            // No permission to access file
            return false;
        }
        catch
        {
            // Other errors
            return false;
        }
    }

    /// <summary>
    /// Performs a comprehensive readiness check on a file
    /// </summary>
    /// <param name="filePath">Path to the file to check</param>
    /// <param name="logAction">Optional action to log messages</param>
    /// <returns>Result indicating whether the file is ready for processing</returns>
    public static FileReadinessResult CheckFileReadiness(string filePath, Action<string>? logAction = null)
    {
        // Basic path validation
        if (string.IsNullOrWhiteSpace(filePath))
        {
            logAction?.Invoke("Invalid file path provided");
            return new FileReadinessResult(false, FileNotReadyReason.InvalidPath, "Invalid file path");
        }

        // Check if file exists
        if (!File.Exists(filePath))
        {
            logAction?.Invoke($"File not found: {filePath}");
            return new FileReadinessResult(false, FileNotReadyReason.FileNotFound, "File does not exist");
        }

        // Check for temporary file
        if (IsTemporaryFile(filePath))
        {
            logAction?.Invoke($"Temporary file detected: {Path.GetFileName(filePath)}");
            return new FileReadinessResult(false, FileNotReadyReason.TemporaryFile, "File appears to be temporary");
        }

        // Check file accessibility
        if (!IsFileAccessible(filePath))
        {
            logAction?.Invoke($"File is not accessible: {Path.GetFileName(filePath)}");
            return new FileReadinessResult(false, FileNotReadyReason.FileNotAccessible, "File is locked or inaccessible");
        }

        logAction?.Invoke($"File is ready for processing: {Path.GetFileName(filePath)}");
        return new FileReadinessResult(true, FileNotReadyReason.None, "File is ready");
    }

    /// <summary>
    /// Waits for a file to become accessible with exponential backoff retry logic
    /// </summary>
    /// <param name="filePath">Path to the file to wait for</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds</param>
    /// <param name="maxDelayMs">Maximum delay in milliseconds</param>
    /// <param name="backoffMultiplier">Multiplier for exponential backoff</param>
    /// <param name="cancellationToken">Cancellation token to stop waiting</param>
    /// <returns>Result indicating whether the file became accessible</returns>
    public static async Task<FileAccessResult> WaitForFileAccessAsync(
        string filePath,
        int maxRetries = DefaultRetryConfig.MaxRetries,
        int initialDelayMs = DefaultRetryConfig.InitialDelayMs,
        int maxDelayMs = DefaultRetryConfig.MaxDelayMs,
        double backoffMultiplier = DefaultRetryConfig.BackoffMultiplier,
        CancellationToken cancellationToken = default)
    {
        var retries = 0;
        var delay = initialDelayMs;

        while (retries < maxRetries && !cancellationToken.IsCancellationRequested)
        {
            if (IsFileAccessible(filePath))
            {
                return new FileAccessResult(true, "File is now accessible", retries);
            }

            retries++;
            
            if (retries < maxRetries)
            {
                await Task.Delay(delay, cancellationToken);
                delay = Math.Min((int)(delay * backoffMultiplier), maxDelayMs);
            }
        }

        var message = cancellationToken.IsCancellationRequested 
            ? "Operation was cancelled" 
            : $"File remained inaccessible after {maxRetries} attempts";
            
        return new FileAccessResult(false, message, retries);
    }
}
