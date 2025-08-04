using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using FolderWatch.WPF.Helpers;
using FolderWatch.WPF.Models;

namespace FolderWatch.WPF.Services;

/// <summary>
/// Manages rule persistence and thread-safe access for concurrent file processing
/// </summary>
public class RuleService : IRuleService, IDisposable
{
    private readonly ReaderWriterLockSlim _rulesLock = new();
    private readonly List<Rule> _rules = new();
    private readonly string _rulesFilePath;

    public event Action<string>? RuleActionLogged;

    public RuleService()
    {
        // Store rules in AppData folder
        var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FolderWatch");
        Directory.CreateDirectory(appDataFolder);
        _rulesFilePath = Path.Combine(appDataFolder, "rules.json");
    }

    /// <summary>
    /// Gets all rules in a thread-safe manner
    /// </summary>
    public async Task<IEnumerable<Rule>> GetRulesAsync()
    {
        await LoadRulesIfNeededAsync();
        
        _rulesLock.EnterReadLock();
        try
        {
            return _rules.ToList(); // Return a copy to avoid external modification
        }
        finally
        {
            _rulesLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Saves a rule (adds new or updates existing)
    /// </summary>
    public async Task SaveRuleAsync(Rule rule)
    {
        if (rule is null)
            throw new ArgumentNullException(nameof(rule));

        await LoadRulesIfNeededAsync();

        _rulesLock.EnterWriteLock();
        try
        {
            var existingIndex = _rules.FindIndex(r => r.Name.Equals(rule.Name, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                _rules[existingIndex] = rule;
                LogAction($"Updated rule: {rule.Name}");
            }
            else
            {
                _rules.Add(rule);
                LogAction($"Added rule: {rule.Name}");
            }
        }
        finally
        {
            _rulesLock.ExitWriteLock();
        }

        await SaveRulesToFileAsync();
    }

    /// <summary>
    /// Deletes a rule
    /// </summary>
    public async Task DeleteRuleAsync(Rule rule)
    {
        if (rule is null)
            throw new ArgumentNullException(nameof(rule));

        _rulesLock.EnterWriteLock();
        try
        {
            var removed = _rules.RemoveAll(r => r.Name.Equals(rule.Name, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                LogAction($"Deleted rule: {rule.Name}");
            }
        }
        finally
        {
            _rulesLock.ExitWriteLock();
        }

        await SaveRulesToFileAsync();
    }

    /// <summary>
    /// Reorders rules to match the provided sequence
    /// </summary>
    public async Task ReorderRulesAsync(IEnumerable<Rule> rules)
    {
        if (rules is null)
            throw new ArgumentNullException(nameof(rules));

        _rulesLock.EnterWriteLock();
        try
        {
            _rules.Clear();
            _rules.AddRange(rules);
            LogAction("Rules reordered");
        }
        finally
        {
            _rulesLock.ExitWriteLock();
        }

        await SaveRulesToFileAsync();
    }

    /// <summary>
    /// Executes an action with read access to rules
    /// </summary>
    public void ExecuteWithReadLock(Action<IReadOnlyList<Rule>> action)
    {
        _rulesLock.EnterReadLock();
        try
        {
            action(_rules.AsReadOnly());
        }
        finally
        {
            _rulesLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Loads rules from file if not already loaded
    /// </summary>
    private async Task LoadRulesIfNeededAsync()
    {
        _rulesLock.EnterReadLock();
        var hasRules = _rules.Count > 0;
        _rulesLock.ExitReadLock();

        if (hasRules)
            return;

        await LoadRulesFromFileAsync();
    }

    /// <summary>
    /// Loads rules from the JSON file
    /// </summary>
    private async Task LoadRulesFromFileAsync()
    {
        try
        {
            if (!File.Exists(_rulesFilePath))
            {
                LogAction("No rules file found, starting with empty rule set");
                return;
            }

            var json = await File.ReadAllTextAsync(_rulesFilePath);
            var loadedRules = JsonSerializer.Deserialize<List<Rule>>(json) ?? new List<Rule>();

            _rulesLock.EnterWriteLock();
            try
            {
                _rules.Clear();
                _rules.AddRange(loadedRules);
                LogAction($"Loaded {_rules.Count} rules from file");
            }
            finally
            {
                _rulesLock.ExitWriteLock();
            }
        }
        catch (Exception ex)
        {
            LogAction($"Error loading rules: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves rules to the JSON file
    /// </summary>
    private async Task SaveRulesToFileAsync()
    {
        try
        {
            List<Rule> rulesToSave;
            _rulesLock.EnterReadLock();
            try
            {
                rulesToSave = _rules.ToList();
            }
            finally
            {
                _rulesLock.ExitReadLock();
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(rulesToSave, options);
            await File.WriteAllTextAsync(_rulesFilePath, json);
            LogAction($"Saved {rulesToSave.Count} rules to file");
        }
        catch (Exception ex)
        {
            LogAction($"Error saving rules: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests a rule against a filename without executing actions
    /// </summary>
    /// <param name="rule">The rule to test</param>
    /// <param name="fileName">The filename to test against</param>
    /// <returns>Test result with match status and preview of actions</returns>
    public async Task<RuleTestResult> TestRuleAsync(Rule rule, string fileName)
    {
        var result = new RuleTestResult
        {
            Rule = rule,
            FileName = fileName,
            IsMatch = PatternMatcher.IsMatch(rule.Pattern, fileName)
        };

        if (!result.IsMatch)
        {
            return result;
        }

        // Validate the rule
        result.ValidationErrors.AddRange(ValidateRule(rule));

        if (result.IsValid)
        {
            // Generate action previews
            result.ActionPreviews = await GenerateActionPreviewsAsync(rule, fileName);
        }

        return result;
    }

    /// <summary>
    /// Validates a rule and returns any errors
    /// </summary>
    /// <param name="rule">The rule to validate</param>
    /// <returns>List of validation errors</returns>
    private static List<string> ValidateRule(Rule rule)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(rule.Name))
        {
            errors.Add("Rule name is required");
        }

        if (string.IsNullOrWhiteSpace(rule.Pattern))
        {
            errors.Add("File pattern is required");
        }
        else if (!PatternMatcher.IsValidPattern(rule.Pattern))
        {
            errors.Add("File pattern is invalid");
        }

        // Validate steps if using multi-step workflow
        if (rule.HasMultipleSteps)
        {
            for (int i = 0; i < rule.Steps.Count; i++)
            {
                var step = rule.Steps[i];
                var stepErrors = step.Validate();
                foreach (var error in stepErrors)
                {
                    errors.Add($"Step {i + 1}: {error}");
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Generates previews of what actions would be performed
    /// </summary>
    /// <param name="rule">The rule to preview</param>
    /// <param name="fileName">The filename to use for the preview</param>
    /// <returns>List of action previews</returns>
    private async Task<List<RuleActionPreview>> GenerateActionPreviewsAsync(Rule rule, string fileName)
    {
        var previews = new List<RuleActionPreview>();
        var currentPath = fileName; // Start with just the filename

        if (rule.HasMultipleSteps)
        {
            // Preview multi-step workflow
            foreach (var step in rule.Steps.Where(s => s.Enabled))
            {
                var preview = await GenerateActionPreviewAsync(step.Action, currentPath, step.Destination, step.NewName);
                previews.Add(preview);
                currentPath = preview.ResultPath;
            }
        }
        else
        {
            // Preview single action
            var preview = await GenerateActionPreviewAsync(rule.Action, currentPath, rule.Destination, rule.NewName);
            previews.Add(preview);
        }

        return previews;
    }

    /// <summary>
    /// Generates a preview for a single action
    /// </summary>
    /// <param name="action">The action to preview</param>
    /// <param name="currentPath">The current file path</param>
    /// <param name="destination">The destination folder (for copy/move)</param>
    /// <param name="newName">The new name pattern (for rename operations)</param>
    /// <returns>Action preview</returns>
    private async Task<RuleActionPreview> GenerateActionPreviewAsync(RuleAction action, string currentPath, string destination, string newName)
    {
        var preview = new RuleActionPreview
        {
            Action = action,
            CurrentPath = currentPath
        };

        try
        {
            switch (action)
            {
                case RuleAction.Copy:
                    preview.ResultPath = await PreviewCopyActionAsync(currentPath, destination, newName);
                    preview.Description = $"Copy to: {preview.ResultPath}";
                    break;

                case RuleAction.Move:
                    preview.ResultPath = await PreviewMoveActionAsync(currentPath, destination, newName);
                    preview.Description = $"Move to: {preview.ResultPath}";
                    break;

                case RuleAction.Rename:
                    preview.ResultPath = await PreviewRenameActionAsync(currentPath, newName);
                    preview.Description = $"Rename to: {Path.GetFileName(preview.ResultPath)}";
                    break;

                case RuleAction.Delete:
                    preview.ResultPath = "";
                    preview.Description = "Delete file";
                    preview.HasWarnings = true;
                    preview.WarningMessage = "This action will permanently delete the file";
                    break;

                case RuleAction.DateTime:
                    var datePattern = string.IsNullOrWhiteSpace(newName) ? "{datetime:yyyyMMdd_HHmmss}_{filename}" : newName;
                    preview.ResultPath = await PreviewRenameActionAsync(currentPath, datePattern);
                    preview.Description = $"Add date/time: {Path.GetFileName(preview.ResultPath)}";
                    break;

                case RuleAction.Numbering:
                    var numberPattern = string.IsNullOrWhiteSpace(newName) ? "{filename}_{counter:000}" : newName;
                    preview.ResultPath = await PreviewRenameActionAsync(currentPath, numberPattern);
                    preview.Description = $"Add number: {Path.GetFileName(preview.ResultPath)}";
                    break;

                default:
                    preview.ResultPath = currentPath;
                    preview.Description = "Unknown action";
                    preview.WouldSucceed = false;
                    preview.ErrorMessage = "Unknown action type";
                    break;
            }
        }
        catch (Exception ex)
        {
            preview.WouldSucceed = false;
            preview.ErrorMessage = ex.Message;
            preview.Description = $"Error: {ex.Message}";
        }

        return preview;
    }

    /// <summary>
    /// Previews a copy action
    /// </summary>
    private async Task<string> PreviewCopyActionAsync(string currentPath, string destination, string newName)
    {
        if (string.IsNullOrWhiteSpace(destination))
            return currentPath;

        var fileName = !string.IsNullOrWhiteSpace(newName) 
            ? RenamePatternProcessor.ProcessPattern(newName, currentPath)
            : Path.GetFileName(currentPath);

        return await Task.FromResult(Path.Combine(destination, fileName));
    }

    /// <summary>
    /// Previews a move action
    /// </summary>
    private async Task<string> PreviewMoveActionAsync(string currentPath, string destination, string newName)
    {
        return await PreviewCopyActionAsync(currentPath, destination, newName);
    }

    /// <summary>
    /// Previews a rename action
    /// </summary>
    private async Task<string> PreviewRenameActionAsync(string currentPath, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return currentPath;

        var directory = Path.GetDirectoryName(currentPath) ?? "";
        var newFileName = RenamePatternProcessor.ProcessPattern(pattern, currentPath);
        
        return await Task.FromResult(Path.Combine(directory, newFileName));
    }

    /// <summary>
    /// Logs an action message
    /// </summary>
    private void LogAction(string message)
    {
        RuleActionLogged?.Invoke($"[{DateTime.Now:G}] {message}");
    }

    public void Dispose()
    {
        _rulesLock?.Dispose();
    }
}
