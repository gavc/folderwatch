using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
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
