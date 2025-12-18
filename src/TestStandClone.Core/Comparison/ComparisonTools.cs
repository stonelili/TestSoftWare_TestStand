using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.Comparison
{
    /// <summary>
    /// Comparison result type
    /// </summary>
    public enum DifferenceType
    {
        Added,
        Removed,
        Modified,
        Unchanged
    }

    /// <summary>
    /// Single difference item
    /// </summary>
    public class DifferenceItem
    {
        public string Path { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public DifferenceType Type { get; set; }
        public object? LeftValue { get; set; }
        public object? RightValue { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Comparison result
    /// </summary>
    public class ComparisonResult
    {
        public string LeftName { get; set; } = string.Empty;
        public string RightName { get; set; } = string.Empty;
        public DateTime ComparisonTime { get; set; } = DateTime.Now;
        public List<DifferenceItem> Differences { get; set; } = new List<DifferenceItem>();
        public bool AreEqual => Differences.Count == 0;
        public int AddedCount => Differences.Count(d => d.Type == DifferenceType.Added);
        public int RemovedCount => Differences.Count(d => d.Type == DifferenceType.Removed);
        public int ModifiedCount => Differences.Count(d => d.Type == DifferenceType.Modified);

        public string Summary => $"{AddedCount} added, {RemovedCount} removed, {ModifiedCount} modified";
    }

    /// <summary>
    /// Step comparison data
    /// </summary>
    public class StepComparisonData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string StepType { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public string? Precondition { get; set; }
        public string? PostActionOnPass { get; set; }
        public string? PostActionOnFail { get; set; }
    }

    /// <summary>
    /// Sequence comparison data
    /// </summary>
    public class SequenceComparisonData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public List<StepComparisonData> SetupSteps { get; set; } = new List<StepComparisonData>();
        public List<StepComparisonData> MainSteps { get; set; } = new List<StepComparisonData>();
        public List<StepComparisonData> CleanupSteps { get; set; } = new List<StepComparisonData>();
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Compares two steps for differences
    /// </summary>
    public class StepComparer
    {
        private readonly HashSet<string> _ignoredProperties = new HashSet<string>
        {
            "Id", "LastModified", "ExecutionTime"
        };

        public void IgnoreProperty(string propertyName)
        {
            _ignoredProperties.Add(propertyName);
        }

        public ComparisonResult Compare(StepComparisonData left, StepComparisonData right)
        {
            var result = new ComparisonResult
            {
                LeftName = left.Name,
                RightName = right.Name
            };

            // Compare name
            if (left.Name != right.Name)
            {
                result.Differences.Add(new DifferenceItem
                {
                    Path = "Step",
                    PropertyName = "Name",
                    Type = DifferenceType.Modified,
                    LeftValue = left.Name,
                    RightValue = right.Name,
                    Description = "Step name changed"
                });
            }

            // Compare step type
            if (left.StepType != right.StepType)
            {
                result.Differences.Add(new DifferenceItem
                {
                    Path = "Step",
                    PropertyName = "StepType",
                    Type = DifferenceType.Modified,
                    LeftValue = left.StepType,
                    RightValue = right.StepType,
                    Description = "Step type changed"
                });
            }

            // Compare properties
            CompareProperties(left.Properties, right.Properties, "Properties", result.Differences);

            // Compare precondition
            if (left.Precondition != right.Precondition)
            {
                result.Differences.Add(new DifferenceItem
                {
                    Path = "Step",
                    PropertyName = "Precondition",
                    Type = DifferenceType.Modified,
                    LeftValue = left.Precondition,
                    RightValue = right.Precondition,
                    Description = "Precondition changed"
                });
            }

            return result;
        }

        private void CompareProperties(
            Dictionary<string, object> left, 
            Dictionary<string, object> right, 
            string basePath,
            List<DifferenceItem> differences)
        {
            var allKeys = left.Keys.Union(right.Keys).Distinct();

            foreach (var key in allKeys)
            {
                if (_ignoredProperties.Contains(key)) continue;

                var leftHas = left.TryGetValue(key, out var leftValue);
                var rightHas = right.TryGetValue(key, out var rightValue);

                if (leftHas && !rightHas)
                {
                    differences.Add(new DifferenceItem
                    {
                        Path = basePath,
                        PropertyName = key,
                        Type = DifferenceType.Removed,
                        LeftValue = leftValue,
                        Description = $"Property '{key}' removed"
                    });
                }
                else if (!leftHas && rightHas)
                {
                    differences.Add(new DifferenceItem
                    {
                        Path = basePath,
                        PropertyName = key,
                        Type = DifferenceType.Added,
                        RightValue = rightValue,
                        Description = $"Property '{key}' added"
                    });
                }
                else if (!Equals(leftValue, rightValue))
                {
                    differences.Add(new DifferenceItem
                    {
                        Path = basePath,
                        PropertyName = key,
                        Type = DifferenceType.Modified,
                        LeftValue = leftValue,
                        RightValue = rightValue,
                        Description = $"Property '{key}' changed"
                    });
                }
            }
        }
    }

    /// <summary>
    /// Compares two sequences for differences
    /// </summary>
    public class SequenceComparer
    {
        private readonly StepComparer _stepComparer = new StepComparer();

        public ComparisonResult Compare(SequenceComparisonData left, SequenceComparisonData right)
        {
            var result = new ComparisonResult
            {
                LeftName = left.Name,
                RightName = right.Name
            };

            // Compare name
            if (left.Name != right.Name)
            {
                result.Differences.Add(new DifferenceItem
                {
                    Path = "Sequence",
                    PropertyName = "Name",
                    Type = DifferenceType.Modified,
                    LeftValue = left.Name,
                    RightValue = right.Name,
                    Description = "Sequence name changed"
                });
            }

            // Compare version
            if (left.Version != right.Version)
            {
                result.Differences.Add(new DifferenceItem
                {
                    Path = "Sequence",
                    PropertyName = "Version",
                    Type = DifferenceType.Modified,
                    LeftValue = left.Version,
                    RightValue = right.Version,
                    Description = "Version changed"
                });
            }

            // Compare step groups
            CompareStepList(left.SetupSteps, right.SetupSteps, "Setup", result.Differences);
            CompareStepList(left.MainSteps, right.MainSteps, "Main", result.Differences);
            CompareStepList(left.CleanupSteps, right.CleanupSteps, "Cleanup", result.Differences);

            // Compare variables
            CompareDict(left.Variables, right.Variables, "Variables", result.Differences);

            // Compare parameters
            CompareDict(left.Parameters, right.Parameters, "Parameters", result.Differences);

            return result;
        }

        private void CompareStepList(
            List<StepComparisonData> left,
            List<StepComparisonData> right,
            string groupName,
            List<DifferenceItem> differences)
        {
            // Create lookup by name
            var leftByName = left.ToDictionary(s => s.Name, s => s);
            var rightByName = right.ToDictionary(s => s.Name, s => s);

            // Find added steps
            foreach (var step in right)
            {
                if (!leftByName.ContainsKey(step.Name))
                {
                    differences.Add(new DifferenceItem
                    {
                        Path = groupName,
                        PropertyName = step.Name,
                        Type = DifferenceType.Added,
                        RightValue = step,
                        Description = $"Step '{step.Name}' added"
                    });
                }
            }

            // Find removed steps
            foreach (var step in left)
            {
                if (!rightByName.ContainsKey(step.Name))
                {
                    differences.Add(new DifferenceItem
                    {
                        Path = groupName,
                        PropertyName = step.Name,
                        Type = DifferenceType.Removed,
                        LeftValue = step,
                        Description = $"Step '{step.Name}' removed"
                    });
                }
            }

            // Compare matching steps
            foreach (var leftStep in left)
            {
                if (rightByName.TryGetValue(leftStep.Name, out var rightStep))
                {
                    var stepResult = _stepComparer.Compare(leftStep, rightStep);
                    foreach (var diff in stepResult.Differences)
                    {
                        diff.Path = $"{groupName}/{leftStep.Name}/{diff.Path}";
                        differences.Add(diff);
                    }
                }
            }
        }

        private void CompareDict(
            Dictionary<string, object> left,
            Dictionary<string, object> right,
            string basePath,
            List<DifferenceItem> differences)
        {
            var allKeys = left.Keys.Union(right.Keys).Distinct();

            foreach (var key in allKeys)
            {
                var leftHas = left.TryGetValue(key, out var leftValue);
                var rightHas = right.TryGetValue(key, out var rightValue);

                if (leftHas && !rightHas)
                {
                    differences.Add(new DifferenceItem
                    {
                        Path = basePath,
                        PropertyName = key,
                        Type = DifferenceType.Removed,
                        LeftValue = leftValue,
                        Description = $"{basePath} '{key}' removed"
                    });
                }
                else if (!leftHas && rightHas)
                {
                    differences.Add(new DifferenceItem
                    {
                        Path = basePath,
                        PropertyName = key,
                        Type = DifferenceType.Added,
                        RightValue = rightValue,
                        Description = $"{basePath} '{key}' added"
                    });
                }
                else if (!Equals(leftValue, rightValue))
                {
                    differences.Add(new DifferenceItem
                    {
                        Path = basePath,
                        PropertyName = key,
                        Type = DifferenceType.Modified,
                        LeftValue = leftValue,
                        RightValue = rightValue,
                        Description = $"{basePath} '{key}' changed"
                    });
                }
            }
        }
    }

    /// <summary>
    /// Result comparison data
    /// </summary>
    public class ResultComparisonData
    {
        public string ExecutionId { get; set; } = string.Empty;
        public string SequenceName { get; set; } = string.Empty;
        public DateTime ExecutionTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public List<StepResultComparisonData> StepResults { get; set; } = new List<StepResultComparisonData>();
    }

    /// <summary>
    /// Step result comparison data
    /// </summary>
    public class StepResultComparisonData
    {
        public string StepName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public object? MeasuredValue { get; set; }
        public object? ExpectedValue { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Compares test results
    /// </summary>
    public class ResultComparer
    {
        public ComparisonResult Compare(ResultComparisonData left, ResultComparisonData right)
        {
            var result = new ComparisonResult
            {
                LeftName = $"{left.SequenceName} ({left.ExecutionTime:g})",
                RightName = $"{right.SequenceName} ({right.ExecutionTime:g})"
            };

            // Compare overall status
            if (left.Status != right.Status)
            {
                result.Differences.Add(new DifferenceItem
                {
                    Path = "Result",
                    PropertyName = "Status",
                    Type = DifferenceType.Modified,
                    LeftValue = left.Status,
                    RightValue = right.Status,
                    Description = "Overall status changed"
                });
            }

            // Compare step results
            var leftByName = left.StepResults.ToDictionary(s => s.StepName, s => s);
            var rightByName = right.StepResults.ToDictionary(s => s.StepName, s => s);

            foreach (var stepName in leftByName.Keys.Union(rightByName.Keys).Distinct())
            {
                var leftHas = leftByName.TryGetValue(stepName, out var leftStep);
                var rightHas = rightByName.TryGetValue(stepName, out var rightStep);

                if (leftHas && rightHas)
                {
                    // Compare step status
                    if (leftStep!.Status != rightStep!.Status)
                    {
                        result.Differences.Add(new DifferenceItem
                        {
                            Path = $"Steps/{stepName}",
                            PropertyName = "Status",
                            Type = DifferenceType.Modified,
                            LeftValue = leftStep.Status,
                            RightValue = rightStep.Status,
                            Description = $"Step '{stepName}' status changed"
                        });
                    }

                    // Compare measured values
                    if (!Equals(leftStep.MeasuredValue, rightStep.MeasuredValue))
                    {
                        result.Differences.Add(new DifferenceItem
                        {
                            Path = $"Steps/{stepName}",
                            PropertyName = "MeasuredValue",
                            Type = DifferenceType.Modified,
                            LeftValue = leftStep.MeasuredValue,
                            RightValue = rightStep.MeasuredValue,
                            Description = $"Step '{stepName}' measured value changed"
                        });
                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Comparison manager singleton
    /// </summary>
    public class ComparisonManager
    {
        private static readonly Lazy<ComparisonManager> _instance = 
            new Lazy<ComparisonManager>(() => new ComparisonManager());
        
        public static ComparisonManager Instance => _instance.Value;

        private readonly StepComparer _stepComparer = new StepComparer();
        private readonly SequenceComparer _sequenceComparer = new SequenceComparer();
        private readonly ResultComparer _resultComparer = new ResultComparer();
        private readonly List<ComparisonResult> _history = new List<ComparisonResult>();

        private ComparisonManager() { }

        public StepComparer StepComparer => _stepComparer;
        public SequenceComparer SequenceComparer => _sequenceComparer;
        public ResultComparer ResultComparer => _resultComparer;
        public IReadOnlyList<ComparisonResult> History => _history.AsReadOnly();

        /// <summary>
        /// Compare two steps
        /// </summary>
        public ComparisonResult CompareSteps(StepComparisonData left, StepComparisonData right)
        {
            var result = _stepComparer.Compare(left, right);
            _history.Add(result);
            return result;
        }

        /// <summary>
        /// Compare two sequences
        /// </summary>
        public ComparisonResult CompareSequences(SequenceComparisonData left, SequenceComparisonData right)
        {
            var result = _sequenceComparer.Compare(left, right);
            _history.Add(result);
            return result;
        }

        /// <summary>
        /// Compare two results
        /// </summary>
        public ComparisonResult CompareResults(ResultComparisonData left, ResultComparisonData right)
        {
            var result = _resultComparer.Compare(left, right);
            _history.Add(result);
            return result;
        }

        /// <summary>
        /// Clear comparison history
        /// </summary>
        public void ClearHistory()
        {
            _history.Clear();
        }
    }
}
