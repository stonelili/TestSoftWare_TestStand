using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestStandClone.Core.Automation
{
    /// <summary>
    /// Type of automation trigger.
    /// </summary>
    public enum TriggerType
    {
        Manual,
        Timer,
        FileWatch,
        HttpWebhook,
        MessageQueue,
        DatabaseChange,
        Custom
    }

    /// <summary>
    /// Type of automation action.
    /// </summary>
    public enum ActionType
    {
        ExecuteSequence,
        RunScript,
        SendNotification,
        CopyFiles,
        GenerateReport,
        DatabaseOperation,
        HttpRequest,
        Custom
    }

    /// <summary>
    /// Status of an automation rule.
    /// </summary>
    public enum RuleStatus
    {
        Active,
        Inactive,
        Error,
        Running
    }

    /// <summary>
    /// Represents an automation trigger configuration.
    /// </summary>
    public class AutomationTrigger
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public TriggerType Type { get; set; } = TriggerType.Manual;
        public Dictionary<string, string> Configuration { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Represents an automation action configuration.
    /// </summary>
    public class AutomationAction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public ActionType Type { get; set; } = ActionType.ExecuteSequence;
        public Dictionary<string, string> Parameters { get; set; } = new();
        public int Order { get; set; }
        public bool ContinueOnError { get; set; }
    }

    /// <summary>
    /// Represents an automation rule with triggers and actions.
    /// </summary>
    public class AutomationRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RuleStatus Status { get; set; } = RuleStatus.Inactive;
        public List<AutomationTrigger> Triggers { get; set; } = new();
        public List<AutomationAction> Actions { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastExecutedAt { get; set; }
        public int ExecutionCount { get; set; }
    }

    /// <summary>
    /// Result of automation rule execution.
    /// </summary>
    public class AutomationExecutionResult
    {
        public string RuleId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public List<ActionExecutionResult> ActionResults { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of a single action execution.
    /// </summary>
    public class ActionExecutionResult
    {
        public string ActionId { get; set; } = string.Empty;
        public string ActionName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Output { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Interface for action executors.
    /// </summary>
    public interface IActionExecutor
    {
        ActionType SupportedType { get; }
        Task<ActionExecutionResult> ExecuteAsync(AutomationAction action, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Executor for sequence execution actions.
    /// </summary>
    public class ExecuteSequenceActionExecutor : IActionExecutor
    {
        public ActionType SupportedType => ActionType.ExecuteSequence;

        public Task<ActionExecutionResult> ExecuteAsync(AutomationAction action, CancellationToken cancellationToken = default)
        {
            var result = new ActionExecutionResult
            {
                ActionId = action.Id,
                ActionName = action.Name,
                Success = true,
                Output = $"Executed sequence: {action.Parameters.GetValueOrDefault("SequencePath", "default")}"
            };
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// Executor for notification actions.
    /// </summary>
    public class SendNotificationActionExecutor : IActionExecutor
    {
        public ActionType SupportedType => ActionType.SendNotification;

        public Task<ActionExecutionResult> ExecuteAsync(AutomationAction action, CancellationToken cancellationToken = default)
        {
            var result = new ActionExecutionResult
            {
                ActionId = action.Id,
                ActionName = action.Name,
                Success = true,
                Output = $"Notification sent: {action.Parameters.GetValueOrDefault("Message", "")}"
            };
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// Executor for report generation actions.
    /// </summary>
    public class GenerateReportActionExecutor : IActionExecutor
    {
        public ActionType SupportedType => ActionType.GenerateReport;

        public Task<ActionExecutionResult> ExecuteAsync(AutomationAction action, CancellationToken cancellationToken = default)
        {
            var result = new ActionExecutionResult
            {
                ActionId = action.Id,
                ActionName = action.Name,
                Success = true,
                Output = $"Report generated: {action.Parameters.GetValueOrDefault("OutputPath", "report.html")}"
            };
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// Manager for automation rules.
    /// </summary>
    public class AutomationManager
    {
        private static AutomationManager? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, AutomationRule> _rules = new();
        private readonly Dictionary<ActionType, IActionExecutor> _executors = new();
        private readonly List<AutomationExecutionResult> _executionHistory = new();

        public static AutomationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new AutomationManager();
                    }
                }
                return _instance;
            }
        }

        private AutomationManager()
        {
            RegisterBuiltInExecutors();
        }

        private void RegisterBuiltInExecutors()
        {
            RegisterExecutor(new ExecuteSequenceActionExecutor());
            RegisterExecutor(new SendNotificationActionExecutor());
            RegisterExecutor(new GenerateReportActionExecutor());
        }

        /// <summary>
        /// Registers an action executor.
        /// </summary>
        public void RegisterExecutor(IActionExecutor executor)
        {
            _executors[executor.SupportedType] = executor;
        }

        /// <summary>
        /// Adds a new automation rule.
        /// </summary>
        public void AddRule(AutomationRule rule)
        {
            _rules[rule.Id] = rule;
        }

        /// <summary>
        /// Gets an automation rule by ID.
        /// </summary>
        public AutomationRule? GetRule(string ruleId)
        {
            return _rules.TryGetValue(ruleId, out var rule) ? rule : null;
        }

        /// <summary>
        /// Gets all automation rules.
        /// </summary>
        public IEnumerable<AutomationRule> GetAllRules()
        {
            return _rules.Values;
        }

        /// <summary>
        /// Removes an automation rule.
        /// </summary>
        public bool RemoveRule(string ruleId)
        {
            return _rules.Remove(ruleId);
        }

        /// <summary>
        /// Activates an automation rule.
        /// </summary>
        public void ActivateRule(string ruleId)
        {
            if (_rules.TryGetValue(ruleId, out var rule))
            {
                rule.Status = RuleStatus.Active;
            }
        }

        /// <summary>
        /// Deactivates an automation rule.
        /// </summary>
        public void DeactivateRule(string ruleId)
        {
            if (_rules.TryGetValue(ruleId, out var rule))
            {
                rule.Status = RuleStatus.Inactive;
            }
        }

        /// <summary>
        /// Executes an automation rule.
        /// </summary>
        public async Task<AutomationExecutionResult> ExecuteRuleAsync(string ruleId, CancellationToken cancellationToken = default)
        {
            if (!_rules.TryGetValue(ruleId, out var rule))
            {
                return new AutomationExecutionResult
                {
                    RuleId = ruleId,
                    Success = false,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow,
                    ErrorMessage = "Rule not found"
                };
            }

            var result = new AutomationExecutionResult
            {
                RuleId = ruleId,
                StartTime = DateTime.UtcNow
            };

            rule.Status = RuleStatus.Running;

            try
            {
                foreach (var action in rule.Actions.OrderBy(a => a.Order))
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    if (_executors.TryGetValue(action.Type, out var executor))
                    {
                        var actionResult = await executor.ExecuteAsync(action, cancellationToken);
                        result.ActionResults.Add(actionResult);

                        if (!actionResult.Success && !action.ContinueOnError)
                        {
                            result.Success = false;
                            result.ErrorMessage = actionResult.ErrorMessage;
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(result.ErrorMessage))
                {
                    result.Success = true;
                }

                rule.LastExecutedAt = DateTime.UtcNow;
                rule.ExecutionCount++;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                rule.Status = RuleStatus.Error;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
                if (rule.Status == RuleStatus.Running)
                {
                    rule.Status = RuleStatus.Active;
                }
                _executionHistory.Add(result);
            }

            return result;
        }

        /// <summary>
        /// Gets execution history for a rule.
        /// </summary>
        public IEnumerable<AutomationExecutionResult> GetExecutionHistory(string? ruleId = null)
        {
            return ruleId == null
                ? _executionHistory
                : _executionHistory.Where(r => r.RuleId == ruleId);
        }
    }
}
