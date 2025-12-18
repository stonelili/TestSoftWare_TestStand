using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.TestCoverage
{
    /// <summary>
    /// Coverage level for a step.
    /// </summary>
    public enum CoverageLevel
    {
        None,
        Partial,
        Full
    }

    /// <summary>
    /// Represents coverage information for a step.
    /// </summary>
    public class StepCoverage
    {
        public string StepId { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public CoverageLevel Level { get; set; } = CoverageLevel.None;
        public int ExecutionCount { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public DateTime? LastExecuted { get; set; }
        public List<string> PathsExecuted { get; set; } = new();
    }

    /// <summary>
    /// Represents coverage information for a sequence.
    /// </summary>
    public class SequenceCoverage
    {
        public string SequenceId { get; set; } = string.Empty;
        public string SequenceName { get; set; } = string.Empty;
        public double CoveragePercentage { get; set; }
        public int TotalSteps { get; set; }
        public int CoveredSteps { get; set; }
        public int UncoveredSteps { get; set; }
        public List<StepCoverage> StepCoverages { get; set; } = new();
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Requirements coverage tracking.
    /// </summary>
    public class RequirementCoverage
    {
        public string RequirementId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> LinkedSteps { get; set; } = new();
        public CoverageLevel Level { get; set; } = CoverageLevel.None;
        public DateTime? LastVerified { get; set; }
    }

    /// <summary>
    /// Coverage report for a test session.
    /// </summary>
    public class CoverageReport
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public double OverallCoverage { get; set; }
        public int TotalSequences { get; set; }
        public int CoveredSequences { get; set; }
        public int TotalSteps { get; set; }
        public int CoveredSteps { get; set; }
        public int TotalRequirements { get; set; }
        public int CoveredRequirements { get; set; }
        public List<SequenceCoverage> SequenceCoverages { get; set; } = new();
        public List<RequirementCoverage> RequirementCoverages { get; set; } = new();
    }

    /// <summary>
    /// Manager for test coverage tracking.
    /// </summary>
    public class TestCoverageManager
    {
        private static TestCoverageManager? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, StepCoverage> _stepCoverages = new();
        private readonly Dictionary<string, SequenceCoverage> _sequenceCoverages = new();
        private readonly Dictionary<string, RequirementCoverage> _requirementCoverages = new();
        private readonly List<CoverageReport> _reports = new();

        public static TestCoverageManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new TestCoverageManager();
                    }
                }
                return _instance;
            }
        }

        private TestCoverageManager() { }

        /// <summary>
        /// Records a step execution for coverage tracking.
        /// </summary>
        public void RecordStepExecution(string stepId, string stepName, bool passed, string? path = null)
        {
            if (!_stepCoverages.TryGetValue(stepId, out var coverage))
            {
                coverage = new StepCoverage
                {
                    StepId = stepId,
                    StepName = stepName
                };
                _stepCoverages[stepId] = coverage;
            }

            coverage.ExecutionCount++;
            if (passed)
                coverage.PassCount++;
            else
                coverage.FailCount++;

            coverage.LastExecuted = DateTime.UtcNow;
            coverage.Level = CoverageLevel.Full;

            if (!string.IsNullOrEmpty(path) && !coverage.PathsExecuted.Contains(path))
            {
                coverage.PathsExecuted.Add(path);
            }
        }

        /// <summary>
        /// Gets coverage for a step.
        /// </summary>
        public StepCoverage? GetStepCoverage(string stepId)
        {
            return _stepCoverages.TryGetValue(stepId, out var coverage) ? coverage : null;
        }

        /// <summary>
        /// Calculates coverage for a sequence.
        /// </summary>
        public SequenceCoverage CalculateSequenceCoverage(string sequenceId, string sequenceName, IEnumerable<string> stepIds)
        {
            var stepIdList = stepIds.ToList();
            var coveredSteps = stepIdList.Count(id => _stepCoverages.ContainsKey(id));

            var coverage = new SequenceCoverage
            {
                SequenceId = sequenceId,
                SequenceName = sequenceName,
                TotalSteps = stepIdList.Count,
                CoveredSteps = coveredSteps,
                UncoveredSteps = stepIdList.Count - coveredSteps,
                CoveragePercentage = stepIdList.Count > 0 ? (double)coveredSteps / stepIdList.Count * 100 : 0,
                StepCoverages = stepIdList
                    .Select(id => _stepCoverages.TryGetValue(id, out var c) ? c : new StepCoverage { StepId = id })
                    .ToList()
            };

            _sequenceCoverages[sequenceId] = coverage;
            return coverage;
        }

        /// <summary>
        /// Links a requirement to steps.
        /// </summary>
        public void LinkRequirement(string requirementId, string description, IEnumerable<string> stepIds)
        {
            _requirementCoverages[requirementId] = new RequirementCoverage
            {
                RequirementId = requirementId,
                Description = description,
                LinkedSteps = stepIds.ToList()
            };
        }

        /// <summary>
        /// Updates requirement coverage based on step executions.
        /// </summary>
        public void UpdateRequirementCoverage()
        {
            foreach (var requirement in _requirementCoverages.Values)
            {
                var coveredCount = requirement.LinkedSteps.Count(id => _stepCoverages.ContainsKey(id));
                var totalCount = requirement.LinkedSteps.Count;

                if (coveredCount == 0)
                    requirement.Level = CoverageLevel.None;
                else if (coveredCount < totalCount)
                    requirement.Level = CoverageLevel.Partial;
                else
                    requirement.Level = CoverageLevel.Full;

                if (coveredCount > 0)
                {
                    requirement.LastVerified = DateTime.UtcNow;
                }
            }
        }

        /// <summary>
        /// Generates a coverage report.
        /// </summary>
        public CoverageReport GenerateReport(string name)
        {
            UpdateRequirementCoverage();

            var report = new CoverageReport
            {
                Name = name,
                TotalSequences = _sequenceCoverages.Count,
                CoveredSequences = _sequenceCoverages.Values.Count(s => s.CoveragePercentage > 0),
                TotalSteps = _stepCoverages.Count,
                CoveredSteps = _stepCoverages.Values.Count(s => s.Level != CoverageLevel.None),
                TotalRequirements = _requirementCoverages.Count,
                CoveredRequirements = _requirementCoverages.Values.Count(r => r.Level != CoverageLevel.None),
                SequenceCoverages = _sequenceCoverages.Values.ToList(),
                RequirementCoverages = _requirementCoverages.Values.ToList()
            };

            report.OverallCoverage = report.TotalSteps > 0
                ? (double)report.CoveredSteps / report.TotalSteps * 100
                : 0;

            _reports.Add(report);
            return report;
        }

        /// <summary>
        /// Gets all coverage reports.
        /// </summary>
        public IEnumerable<CoverageReport> GetReports()
        {
            return _reports;
        }

        /// <summary>
        /// Clears all coverage data.
        /// </summary>
        public void ClearCoverage()
        {
            _stepCoverages.Clear();
            _sequenceCoverages.Clear();
        }

        /// <summary>
        /// Gets uncovered steps.
        /// </summary>
        public IEnumerable<string> GetUncoveredSteps(IEnumerable<string> allStepIds)
        {
            return allStepIds.Where(id => !_stepCoverages.ContainsKey(id));
        }

        /// <summary>
        /// Gets coverage summary.
        /// </summary>
        public CoverageSummary GetSummary()
        {
            return new CoverageSummary
            {
                TotalSteps = _stepCoverages.Count,
                FullyCovered = _stepCoverages.Values.Count(s => s.Level == CoverageLevel.Full),
                PartiallyCovered = _stepCoverages.Values.Count(s => s.Level == CoverageLevel.Partial),
                NotCovered = _stepCoverages.Values.Count(s => s.Level == CoverageLevel.None),
                TotalExecutions = _stepCoverages.Values.Sum(s => s.ExecutionCount),
                TotalRequirements = _requirementCoverages.Count,
                CoveredRequirements = _requirementCoverages.Values.Count(r => r.Level != CoverageLevel.None)
            };
        }
    }

    /// <summary>
    /// Summary of coverage statistics.
    /// </summary>
    public class CoverageSummary
    {
        public int TotalSteps { get; set; }
        public int FullyCovered { get; set; }
        public int PartiallyCovered { get; set; }
        public int NotCovered { get; set; }
        public int TotalExecutions { get; set; }
        public int TotalRequirements { get; set; }
        public int CoveredRequirements { get; set; }
        public double CoveragePercentage => TotalSteps > 0 ? (double)FullyCovered / TotalSteps * 100 : 0;
    }
}
