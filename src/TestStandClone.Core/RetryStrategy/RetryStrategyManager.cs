using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestStandClone.Core.RetryStrategy
{
    /// <summary>
    /// Retry trigger conditions
    /// </summary>
    public enum RetryTrigger
    {
        OnFail,
        OnError,
        OnTimeout,
        OnSpecificStatus,
        Always
    }

    /// <summary>
    /// Backoff strategy types
    /// </summary>
    public enum BackoffType
    {
        None,
        Linear,
        Exponential,
        Fixed
    }

    /// <summary>
    /// Retry strategy definition
    /// </summary>
    public class RetryStrategyDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public int MaxRetries { get; set; } = 3;
        public RetryTrigger Trigger { get; set; } = RetryTrigger.OnFail;
        public BackoffType Backoff { get; set; } = BackoffType.None;
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);
        public double BackoffMultiplier { get; set; } = 2.0;
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(1);
        public List<string> TargetStepPatterns { get; set; } = new List<string>();
        public bool IsEnabled { get; set; } = true;
        public string? SuccessCondition { get; set; }
        public bool ResetOnSuccess { get; set; } = true;
    }

    /// <summary>
    /// Retry attempt record
    /// </summary>
    public class RetryAttempt
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string StepId { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public int AttemptNumber { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan DelayBeforeRetry { get; set; }
    }

    /// <summary>
    /// Retry context for a step
    /// </summary>
    public class RetryContext
    {
        public string StepId { get; set; } = string.Empty;
        public RetryStrategyDefinition Strategy { get; set; } = null!;
        public int CurrentAttempt { get; set; }
        public List<RetryAttempt> Attempts { get; set; } = new List<RetryAttempt>();
        public bool IsCompleted { get; set; }
        public bool WasSuccessful { get; set; }
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Retry statistics
    /// </summary>
    public class RetryStatistics
    {
        public int TotalRetries { get; set; }
        public int SuccessfulRetries { get; set; }
        public int FailedRetries { get; set; }
        public double SuccessRate => TotalRetries > 0 ? (double)SuccessfulRetries / TotalRetries * 100 : 0;
        public TimeSpan TotalRetryTime { get; set; }
        public Dictionary<string, int> RetriesByStep { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// Retry strategy manager singleton
    /// </summary>
    public class RetryStrategyManager
    {
        private static readonly Lazy<RetryStrategyManager> _instance = new Lazy<RetryStrategyManager>(() => new RetryStrategyManager());
        public static RetryStrategyManager Instance => _instance.Value;

        private readonly List<RetryStrategyDefinition> _strategies = new List<RetryStrategyDefinition>();
        private readonly Dictionary<string, RetryContext> _activeContexts = new Dictionary<string, RetryContext>();
        private readonly List<RetryAttempt> _allAttempts = new List<RetryAttempt>();
        private readonly object _lock = new object();

        public event EventHandler<RetryAttempt>? RetryStarted;
        public event EventHandler<RetryAttempt>? RetryCompleted;
        public event EventHandler<RetryContext>? MaxRetriesReached;

        private RetryStrategyManager()
        {
            InitializeDefaultStrategies();
        }

        private void InitializeDefaultStrategies()
        {
            _strategies.Add(new RetryStrategyDefinition
            {
                Name = "Default Retry",
                MaxRetries = 3,
                Trigger = RetryTrigger.OnFail,
                Backoff = BackoffType.Exponential,
                InitialDelay = TimeSpan.FromSeconds(1)
            });
        }

        public void RegisterStrategy(RetryStrategyDefinition strategy)
        {
            lock (_lock)
            {
                _strategies.RemoveAll(s => s.Id == strategy.Id);
                _strategies.Add(strategy);
            }
        }

        public RetryStrategyDefinition? GetStrategy(string name)
        {
            lock (_lock)
            {
                return _strategies.FirstOrDefault(s => s.Name == name && s.IsEnabled);
            }
        }

        public bool ShouldRetry(string stepId, string stepName, string status, out TimeSpan delay)
        {
            delay = TimeSpan.Zero;
            lock (_lock)
            {
                var strategy = FindMatchingStrategy(stepName);
                if (strategy == null) return false;

                if (!_activeContexts.TryGetValue(stepId, out var context))
                {
                    context = new RetryContext { StepId = stepId, Strategy = strategy };
                    _activeContexts[stepId] = context;
                }

                if (context.CurrentAttempt >= strategy.MaxRetries)
                {
                    context.IsCompleted = true;
                    MaxRetriesReached?.Invoke(this, context);
                    return false;
                }

                if (!ShouldTriggerRetry(strategy.Trigger, status))
                    return false;

                context.CurrentAttempt++;
                delay = CalculateDelay(strategy, context.CurrentAttempt);

                var attempt = new RetryAttempt
                {
                    StepId = stepId,
                    StepName = stepName,
                    AttemptNumber = context.CurrentAttempt,
                    Status = status,
                    DelayBeforeRetry = delay
                };
                context.Attempts.Add(attempt);
                _allAttempts.Add(attempt);

                RetryStarted?.Invoke(this, attempt);
                return true;
            }
        }

        public void RecordRetryResult(string stepId, bool success, TimeSpan duration, string? errorMessage = null)
        {
            lock (_lock)
            {
                if (!_activeContexts.TryGetValue(stepId, out var context)) return;

                var lastAttempt = context.Attempts.LastOrDefault();
                if (lastAttempt != null)
                {
                    lastAttempt.Duration = duration;
                    lastAttempt.ErrorMessage = errorMessage;
                    lastAttempt.Status = success ? "Passed" : "Failed";
                    RetryCompleted?.Invoke(this, lastAttempt);
                }

                if (success && context.Strategy.ResetOnSuccess)
                {
                    context.WasSuccessful = true;
                    context.IsCompleted = true;
                    _activeContexts.Remove(stepId);
                }
            }
        }

        private RetryStrategyDefinition? FindMatchingStrategy(string stepName)
        {
            return _strategies
                .Where(s => s.IsEnabled)
                .FirstOrDefault(s => s.TargetStepPatterns.Count == 0 ||
                    s.TargetStepPatterns.Any(p => stepName.Contains(p, StringComparison.OrdinalIgnoreCase)));
        }

        private bool ShouldTriggerRetry(RetryTrigger trigger, string status)
        {
            return trigger switch
            {
                RetryTrigger.OnFail => status.Equals("Failed", StringComparison.OrdinalIgnoreCase),
                RetryTrigger.OnError => status.Equals("Error", StringComparison.OrdinalIgnoreCase),
                RetryTrigger.OnTimeout => status.Equals("Timeout", StringComparison.OrdinalIgnoreCase),
                RetryTrigger.Always => true,
                _ => false
            };
        }

        private TimeSpan CalculateDelay(RetryStrategyDefinition strategy, int attemptNumber)
        {
            var delay = strategy.Backoff switch
            {
                BackoffType.None => TimeSpan.Zero,
                BackoffType.Fixed => strategy.InitialDelay,
                BackoffType.Linear => TimeSpan.FromTicks(strategy.InitialDelay.Ticks * attemptNumber),
                BackoffType.Exponential => TimeSpan.FromTicks((long)(strategy.InitialDelay.Ticks * Math.Pow(strategy.BackoffMultiplier, attemptNumber - 1))),
                _ => TimeSpan.Zero
            };
            return delay > strategy.MaxDelay ? strategy.MaxDelay : delay;
        }

        public RetryStatistics GetStatistics()
        {
            lock (_lock)
            {
                var stats = new RetryStatistics
                {
                    TotalRetries = _allAttempts.Count,
                    SuccessfulRetries = _allAttempts.Count(a => a.Status == "Passed"),
                    FailedRetries = _allAttempts.Count(a => a.Status == "Failed"),
                    TotalRetryTime = TimeSpan.FromTicks(_allAttempts.Sum(a => a.Duration.Ticks + a.DelayBeforeRetry.Ticks))
                };

                foreach (var group in _allAttempts.GroupBy(a => a.StepName))
                {
                    stats.RetriesByStep[group.Key] = group.Count();
                }

                return stats;
            }
        }

        public void ClearContext(string stepId) { lock (_lock) { _activeContexts.Remove(stepId); } }
        public void ClearAllContexts() { lock (_lock) { _activeContexts.Clear(); } }
    }
}
