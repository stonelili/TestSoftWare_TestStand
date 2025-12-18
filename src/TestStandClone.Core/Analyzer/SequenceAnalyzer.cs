using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestStandClone.Core.Analyzer
{
    /// <summary>
    /// Issue severity level
    /// </summary>
    public enum IssueSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Issue category
    /// </summary>
    public enum IssueCategory
    {
        Performance,
        Logic,
        Configuration,
        BestPractice,
        Security,
        Compatibility,
        Resource
    }

    /// <summary>
    /// An analysis issue found in a sequence
    /// </summary>
    public class AnalysisIssue
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public IssueSeverity Severity { get; set; }
        public IssueCategory Category { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? StepId { get; set; }
        public string? StepName { get; set; }
        public string? SequenceName { get; set; }
        public string? Suggestion { get; set; }
        public int? LineNumber { get; set; }
    }

    /// <summary>
    /// Analysis result containing all issues
    /// </summary>
    public class AnalysisResult
    {
        public string SequenceName { get; set; } = string.Empty;
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan AnalysisDuration { get; set; }
        public List<AnalysisIssue> Issues { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Get issues by severity
        /// </summary>
        public IEnumerable<AnalysisIssue> GetIssuesBySeverity(IssueSeverity severity)
        {
            return Issues.Where(i => i.Severity == severity);
        }

        /// <summary>
        /// Get issues by category
        /// </summary>
        public IEnumerable<AnalysisIssue> GetIssuesByCategory(IssueCategory category)
        {
            return Issues.Where(i => i.Category == category);
        }

        /// <summary>
        /// Check if there are any errors or critical issues
        /// </summary>
        public bool HasCriticalIssues => Issues.Any(i => 
            i.Severity == IssueSeverity.Error || i.Severity == IssueSeverity.Critical);

        /// <summary>
        /// Generate a summary
        /// </summary>
        public string GetSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Analysis Results for: {SequenceName}");
            sb.AppendLine($"Analyzed at: {AnalyzedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Duration: {AnalysisDuration.TotalMilliseconds:F2} ms");
            sb.AppendLine();
            sb.AppendLine("Issue Summary:");
            sb.AppendLine($"  Critical: {Issues.Count(i => i.Severity == IssueSeverity.Critical)}");
            sb.AppendLine($"  Errors: {Issues.Count(i => i.Severity == IssueSeverity.Error)}");
            sb.AppendLine($"  Warnings: {Issues.Count(i => i.Severity == IssueSeverity.Warning)}");
            sb.AppendLine($"  Info: {Issues.Count(i => i.Severity == IssueSeverity.Info)}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Interface for sequence analyzers
    /// </summary>
    public interface ISequenceAnalyzer
    {
        /// <summary>
        /// Analyzer name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Categories this analyzer checks
        /// </summary>
        IEnumerable<IssueCategory> Categories { get; }

        /// <summary>
        /// Analyze a sequence
        /// </summary>
        IEnumerable<AnalysisIssue> Analyze(Sequence sequence);
    }

    /// <summary>
    /// Performance analyzer
    /// </summary>
    public class PerformanceAnalyzer : ISequenceAnalyzer
    {
        public string Name => "Performance Analyzer";
        public IEnumerable<IssueCategory> Categories => new[] { IssueCategory.Performance };

        public IEnumerable<AnalysisIssue> Analyze(Sequence sequence)
        {
            var issues = new List<AnalysisIssue>();

            // Check for excessive delays
            foreach (var step in sequence.Steps)
            {
                if (step is DelayStep delayStep && delayStep.DelayMilliseconds > 10000)
                {
                    issues.Add(new AnalysisIssue
                    {
                        Severity = IssueSeverity.Warning,
                        Category = IssueCategory.Performance,
                        Title = "Long Delay Step",
                        Description = $"Step '{step.Name}' has a delay of {delayStep.DelayMilliseconds}ms which may impact performance.",
                        StepId = step.Id.ToString(),
                        StepName = step.Name,
                        SequenceName = sequence.Name,
                        Suggestion = "Consider breaking long delays into smaller steps or using async operations."
                    });
                }
            }

            // Check for too many steps
            if (sequence.Steps.Count > 100)
            {
                issues.Add(new AnalysisIssue
                {
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.Performance,
                    Title = "Large Sequence",
                    Description = $"Sequence has {sequence.Steps.Count} steps which may impact performance and maintainability.",
                    SequenceName = sequence.Name,
                    Suggestion = "Consider breaking the sequence into smaller sub-sequences."
                });
            }

            return issues;
        }
    }

    /// <summary>
    /// Logic analyzer for flow control issues
    /// </summary>
    public class LogicAnalyzer : ISequenceAnalyzer
    {
        public string Name => "Logic Analyzer";
        public IEnumerable<IssueCategory> Categories => new[] { IssueCategory.Logic };

        public IEnumerable<AnalysisIssue> Analyze(Sequence sequence)
        {
            var issues = new List<AnalysisIssue>();

            // Check for duplicate step names
            var duplicateNames = sequence.Steps
                .GroupBy(s => s.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var name in duplicateNames)
            {
                issues.Add(new AnalysisIssue
                {
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.Logic,
                    Title = "Duplicate Step Name",
                    Description = $"Multiple steps have the name '{name}'.",
                    SequenceName = sequence.Name,
                    Suggestion = "Use unique names for each step to avoid confusion."
                });
            }

            // Check for empty sequence
            if (sequence.Steps.Count == 0)
            {
                issues.Add(new AnalysisIssue
                {
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.Logic,
                    Title = "Empty Sequence",
                    Description = "Sequence contains no steps.",
                    SequenceName = sequence.Name,
                    Suggestion = "Add steps to the sequence or remove it if not needed."
                });
            }

            return issues;
        }
    }

    /// <summary>
    /// Best practices analyzer
    /// </summary>
    public class BestPracticesAnalyzer : ISequenceAnalyzer
    {
        public string Name => "Best Practices Analyzer";
        public IEnumerable<IssueCategory> Categories => new[] { IssueCategory.BestPractice };

        public IEnumerable<AnalysisIssue> Analyze(Sequence sequence)
        {
            var issues = new List<AnalysisIssue>();

            // Check for steps without descriptions
            foreach (var step in sequence.Steps.Where(s => string.IsNullOrEmpty(s.Name) || s.Name.StartsWith("Step_")))
            {
                issues.Add(new AnalysisIssue
                {
                    Severity = IssueSeverity.Info,
                    Category = IssueCategory.BestPractice,
                    Title = "Step Missing Meaningful Name",
                    Description = $"Step '{step.Name}' could have a more descriptive name.",
                    StepId = step.Id.ToString(),
                    StepName = step.Name,
                    SequenceName = sequence.Name,
                    Suggestion = "Use descriptive names that explain what the step does."
                });
            }

            // Check for sequence naming
            if (string.IsNullOrEmpty(sequence.Name) || sequence.Name == "New Sequence")
            {
                issues.Add(new AnalysisIssue
                {
                    Severity = IssueSeverity.Info,
                    Category = IssueCategory.BestPractice,
                    Title = "Sequence Missing Meaningful Name",
                    Description = "Sequence should have a descriptive name.",
                    SequenceName = sequence.Name,
                    Suggestion = "Use a name that describes the purpose of the sequence."
                });
            }

            return issues;
        }
    }

    /// <summary>
    /// Sequence analyzer manager
    /// </summary>
    public class SequenceAnalyzerManager
    {
        private static readonly Lazy<SequenceAnalyzerManager> _instance = new(() => new SequenceAnalyzerManager());
        public static SequenceAnalyzerManager Instance => _instance.Value;

        private readonly List<ISequenceAnalyzer> _analyzers = new();
        private readonly object _lock = new();

        public event EventHandler<AnalysisEventArgs>? AnalysisStarted;
        public event EventHandler<AnalysisEventArgs>? AnalysisCompleted;

        private SequenceAnalyzerManager()
        {
            // Register default analyzers
            RegisterAnalyzer(new PerformanceAnalyzer());
            RegisterAnalyzer(new LogicAnalyzer());
            RegisterAnalyzer(new BestPracticesAnalyzer());
        }

        /// <summary>
        /// Register an analyzer
        /// </summary>
        public void RegisterAnalyzer(ISequenceAnalyzer analyzer)
        {
            lock (_lock)
            {
                if (!_analyzers.Any(a => a.GetType() == analyzer.GetType()))
                {
                    _analyzers.Add(analyzer);
                }
            }
        }

        /// <summary>
        /// Unregister an analyzer
        /// </summary>
        public void UnregisterAnalyzer<T>() where T : ISequenceAnalyzer
        {
            lock (_lock)
            {
                var analyzer = _analyzers.FirstOrDefault(a => a is T);
                if (analyzer != null)
                {
                    _analyzers.Remove(analyzer);
                }
            }
        }

        /// <summary>
        /// Get all registered analyzers
        /// </summary>
        public IEnumerable<ISequenceAnalyzer> Analyzers
        {
            get
            {
                lock (_lock)
                {
                    return _analyzers.ToList();
                }
            }
        }

        /// <summary>
        /// Analyze a sequence with all registered analyzers
        /// </summary>
        public AnalysisResult Analyze(Sequence sequence)
        {
            var startTime = DateTime.UtcNow;
            var result = new AnalysisResult
            {
                SequenceName = sequence.Name
            };

            AnalysisStarted?.Invoke(this, new AnalysisEventArgs { Sequence = sequence });

            List<ISequenceAnalyzer> analyzers;
            lock (_lock)
            {
                analyzers = _analyzers.ToList();
            }

            foreach (var analyzer in analyzers)
            {
                try
                {
                    var issues = analyzer.Analyze(sequence);
                    result.Issues.AddRange(issues);
                }
                catch (Exception ex)
                {
                    result.Issues.Add(new AnalysisIssue
                    {
                        Severity = IssueSeverity.Error,
                        Category = IssueCategory.Configuration,
                        Title = "Analyzer Error",
                        Description = $"Analyzer '{analyzer.Name}' failed: {ex.Message}",
                        SequenceName = sequence.Name
                    });
                }
            }

            result.AnalysisDuration = DateTime.UtcNow - startTime;
            result.AnalyzedAt = DateTime.UtcNow;

            AnalysisCompleted?.Invoke(this, new AnalysisEventArgs 
            { 
                Sequence = sequence, 
                Result = result 
            });

            return result;
        }

        /// <summary>
        /// Analyze a sequence with specific analyzers
        /// </summary>
        public AnalysisResult Analyze(Sequence sequence, IEnumerable<IssueCategory> categories)
        {
            var startTime = DateTime.UtcNow;
            var result = new AnalysisResult
            {
                SequenceName = sequence.Name
            };

            List<ISequenceAnalyzer> analyzers;
            lock (_lock)
            {
                analyzers = _analyzers
                    .Where(a => a.Categories.Any(c => categories.Contains(c)))
                    .ToList();
            }

            foreach (var analyzer in analyzers)
            {
                try
                {
                    var issues = analyzer.Analyze(sequence);
                    result.Issues.AddRange(issues);
                }
                catch (Exception ex)
                {
                    result.Issues.Add(new AnalysisIssue
                    {
                        Severity = IssueSeverity.Error,
                        Category = IssueCategory.Configuration,
                        Title = "Analyzer Error",
                        Description = $"Analyzer '{analyzer.Name}' failed: {ex.Message}",
                        SequenceName = sequence.Name
                    });
                }
            }

            result.AnalysisDuration = DateTime.UtcNow - startTime;
            result.AnalyzedAt = DateTime.UtcNow;

            return result;
        }

        /// <summary>
        /// Generate a report from analysis results
        /// </summary>
        public string GenerateReport(AnalysisResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Sequence Analysis Report ===");
            sb.AppendLine();
            sb.AppendLine(result.GetSummary());
            sb.AppendLine();

            if (result.Issues.Any())
            {
                sb.AppendLine("=== Issues ===");
                sb.AppendLine();

                foreach (var severity in new[] { IssueSeverity.Critical, IssueSeverity.Error, IssueSeverity.Warning, IssueSeverity.Info })
                {
                    var issues = result.GetIssuesBySeverity(severity).ToList();
                    if (issues.Any())
                    {
                        sb.AppendLine($"--- {severity} ({issues.Count}) ---");
                        foreach (var issue in issues)
                        {
                            sb.AppendLine($"  [{issue.Category}] {issue.Title}");
                            sb.AppendLine($"    {issue.Description}");
                            if (!string.IsNullOrEmpty(issue.StepName))
                            {
                                sb.AppendLine($"    Step: {issue.StepName}");
                            }
                            if (!string.IsNullOrEmpty(issue.Suggestion))
                            {
                                sb.AppendLine($"    Suggestion: {issue.Suggestion}");
                            }
                            sb.AppendLine();
                        }
                    }
                }
            }
            else
            {
                sb.AppendLine("No issues found.");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Analysis event arguments
    /// </summary>
    public class AnalysisEventArgs : EventArgs
    {
        public Sequence Sequence { get; set; } = new();
        public AnalysisResult? Result { get; set; }
    }
}
