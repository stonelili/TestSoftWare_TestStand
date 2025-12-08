using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.RoutingEngine
{
    public enum RoutingDecision { Continue, Skip, Abort, Goto, Retry }
    public enum RoutingConditionType { StepResult, VariableValue, Expression, ProductType, Random }

    public class RoutingCondition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public RoutingConditionType Type { get; set; } = RoutingConditionType.StepResult;
        public string Expression { get; set; } = string.Empty;
        public string TargetStepId { get; set; } = string.Empty;
        public string TargetValue { get; set; } = string.Empty;
        public RoutingDecision Decision { get; set; } = RoutingDecision.Continue;
        public string GotoStepId { get; set; } = string.Empty;
        public int Priority { get; set; } = 0;
    }

    public class RoutingRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public List<RoutingCondition> Conditions { get; set; } = new();
        public RoutingDecision DefaultDecision { get; set; } = RoutingDecision.Continue;
        public string SequenceId { get; set; } = string.Empty;
    }

    public class RoutingResult
    {
        public RoutingDecision Decision { get; set; } = RoutingDecision.Continue;
        public string? GotoStepId { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public string ConditionName { get; set; } = string.Empty;
        public DateTime EvaluatedAt { get; set; } = DateTime.Now;
    }

    public class RoutingContext
    {
        public string StepId { get; set; } = string.Empty;
        public bool StepPassed { get; set; } = true;
        public string ProductType { get; set; } = string.Empty;
        public Dictionary<string, object> Variables { get; set; } = new();
        public Dictionary<string, bool> StepResults { get; set; } = new();
    }

    public class RoutingEngineManager
    {
        private static readonly Lazy<RoutingEngineManager> _instance = new(() => new RoutingEngineManager());
        public static RoutingEngineManager Instance => _instance.Value;
        private readonly Dictionary<string, RoutingRule> _rules = new();
        private readonly List<RoutingResult> _history = new();
        private readonly object _lock = new();

        public event EventHandler<RoutingResult>? RoutingDecisionMade;

        private RoutingEngineManager() { }

        public void RegisterRule(RoutingRule rule)
        {
            lock (_lock) { _rules[rule.Id] = rule; }
        }

        public RoutingResult EvaluateRouting(RoutingContext context, string sequenceId)
        {
            lock (_lock)
            {
                var applicableRules = _rules.Values
                    .Where(r => r.IsEnabled && r.SequenceId == sequenceId)
                    .OrderByDescending(r => r.Conditions.Max(c => c.Priority));

                foreach (var rule in applicableRules)
                {
                    foreach (var condition in rule.Conditions.OrderByDescending(c => c.Priority))
                    {
                        if (EvaluateCondition(condition, context))
                        {
                            var result = new RoutingResult
                            {
                                Decision = condition.Decision,
                                GotoStepId = condition.Decision == RoutingDecision.Goto ? condition.GotoStepId : null,
                                RuleName = rule.Name,
                                ConditionName = condition.Name
                            };
                            _history.Add(result);
                            RoutingDecisionMade?.Invoke(this, result);
                            return result;
                        }
                    }
                }

                return new RoutingResult { Decision = RoutingDecision.Continue };
            }
        }

        private static bool EvaluateCondition(RoutingCondition condition, RoutingContext context)
        {
            return condition.Type switch
            {
                RoutingConditionType.StepResult => 
                    context.StepResults.TryGetValue(condition.TargetStepId, out var result) && 
                    result.ToString() == condition.TargetValue,
                RoutingConditionType.ProductType => 
                    context.ProductType == condition.TargetValue,
                _ => false
            };
        }

        public List<RoutingResult> GetRoutingHistory(int count = 100)
        {
            lock (_lock) { return _history.TakeLast(count).ToList(); }
        }

        public List<RoutingRule> GetAllRules()
        {
            lock (_lock) { return _rules.Values.ToList(); }
        }
    }
}
