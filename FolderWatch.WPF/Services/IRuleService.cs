using FolderWatch.WPF.Models;

namespace FolderWatch.WPF.Services;

/// <summary>
/// Interface for rule management operations
/// </summary>
public interface IRuleService
{
    /// <summary>
    /// Gets all rules
    /// </summary>
    Task<IEnumerable<Rule>> GetRulesAsync();

    /// <summary>
    /// Saves a rule (adds new or updates existing)
    /// </summary>
    Task SaveRuleAsync(Rule rule);

    /// <summary>
    /// Deletes a rule
    /// </summary>
    Task DeleteRuleAsync(Rule rule);

    /// <summary>
    /// Reorders rules
    /// </summary>
    Task ReorderRulesAsync(IEnumerable<Rule> rules);

    /// <summary>
    /// Tests a rule against a filename without executing actions
    /// </summary>
    /// <param name="rule">The rule to test</param>
    /// <param name="fileName">The filename to test against</param>
    /// <returns>Test result with match status and preview of actions</returns>
    Task<RuleTestResult> TestRuleAsync(Rule rule, string fileName);

    /// <summary>
    /// Event raised when a rule action is logged
    /// </summary>
    event Action<string>? RuleActionLogged;
}
