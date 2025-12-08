// Phase 20: Test Plan Management - Manage test plans and test configurations
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.TestPlanManagement
{
    /// <summary>
    /// Test plan status enumeration
    /// </summary>
    public enum TestPlanStatus
    {
        Draft,
        Active,
        Suspended,
        Archived
    }

    /// <summary>
    /// Test plan execution mode
    /// </summary>
    public enum TestPlanExecutionMode
    {
        Sequential,
        Parallel,
        Random
    }

    /// <summary>
    /// Represents a test step in a test plan
    /// </summary>
    public class TestPlanStep
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string SequenceFile { get; set; } = string.Empty;
        public string TestProgram { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsMandatory { get; set; } = true;
        public TimeSpan? Timeout { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public List<string> PreConditions { get; set; } = new();
        public List<string> PostConditions { get; set; } = new();
    }

    /// <summary>
    /// Represents a test plan
    /// </summary>
    public class TestPlan
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public TestPlanStatus Status { get; set; } = TestPlanStatus.Draft;
        public TestPlanExecutionMode ExecutionMode { get; set; } = TestPlanExecutionMode.Sequential;
        public List<TestPlanStep> Steps { get; set; } = new();
        public List<string> ApplicableProducts { get; set; } = new();
        public Dictionary<string, object> GlobalParameters { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Test plan execution result
    /// </summary>
    public class TestPlanExecutionResult
    {
        public string PlanId { get; set; } = string.Empty;
        public string ExecutionId { get; set; } = Guid.NewGuid().ToString();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Passed { get; set; }
        public List<TestPlanStepResult> StepResults { get; set; } = new();
        public Dictionary<string, object> OutputParameters { get; set; } = new();
    }

    /// <summary>
    /// Test plan step result
    /// </summary>
    public class TestPlanStepResult
    {
        public string StepId { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public TimeSpan Duration { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, object> Results { get; set; } = new();
    }

    /// <summary>
    /// Manages test plans
    /// </summary>
    public class TestPlanManager
    {
        private static readonly Lazy<TestPlanManager> _instance = new(() => new TestPlanManager());
        public static TestPlanManager Instance => _instance.Value;

        private readonly Dictionary<string, TestPlan> _plans = new();
        private readonly List<TestPlanExecutionResult> _executionHistory = new();
        private readonly object _lock = new();

        private TestPlanManager() { }

        public void RegisterPlan(TestPlan plan)
        {
            lock (_lock)
            {
                plan.ModifiedAt = DateTime.UtcNow;
                _plans[plan.Id] = plan;
            }
        }

        public TestPlan? GetPlan(string planId)
        {
            lock (_lock)
            {
                return _plans.TryGetValue(planId, out var plan) ? plan : null;
            }
        }

        public List<TestPlan> GetAllPlans()
        {
            lock (_lock)
            {
                return _plans.Values.ToList();
            }
        }

        public List<TestPlan> GetActivePlans()
        {
            lock (_lock)
            {
                return _plans.Values.Where(p => p.Status == TestPlanStatus.Active).ToList();
            }
        }

        public List<TestPlan> GetPlansForProduct(string productId)
        {
            lock (_lock)
            {
                return _plans.Values.Where(p => p.ApplicableProducts.Contains(productId)).ToList();
            }
        }

        public void AddStep(string planId, TestPlanStep step)
        {
            lock (_lock)
            {
                if (_plans.TryGetValue(planId, out var plan))
                {
                    step.Order = plan.Steps.Count;
                    plan.Steps.Add(step);
                    plan.ModifiedAt = DateTime.UtcNow;
                }
            }
        }

        public void RemoveStep(string planId, string stepId)
        {
            lock (_lock)
            {
                if (_plans.TryGetValue(planId, out var plan))
                {
                    plan.Steps.RemoveAll(s => s.Id == stepId);
                    plan.ModifiedAt = DateTime.UtcNow;
                }
            }
        }

        public void RecordExecution(TestPlanExecutionResult result)
        {
            lock (_lock)
            {
                _executionHistory.Add(result);
            }
        }

        public List<TestPlanExecutionResult> GetExecutionHistory(string planId)
        {
            lock (_lock)
            {
                return _executionHistory.Where(r => r.PlanId == planId).ToList();
            }
        }

        public void UpdatePlanStatus(string planId, TestPlanStatus status)
        {
            lock (_lock)
            {
                if (_plans.TryGetValue(planId, out var plan))
                {
                    plan.Status = status;
                    plan.ModifiedAt = DateTime.UtcNow;
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _plans.Clear();
                _executionHistory.Clear();
            }
        }
    }
}
