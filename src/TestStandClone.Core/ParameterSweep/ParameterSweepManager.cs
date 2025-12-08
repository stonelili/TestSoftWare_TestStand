// Phase 20: Parameter Sweep - Automated parameter sweep testing
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.ParameterSweep
{
    /// <summary>
    /// Sweep type enumeration
    /// </summary>
    public enum SweepType
    {
        Linear,
        Logarithmic,
        Custom,
        Random
    }

    /// <summary>
    /// Represents a sweep parameter definition
    /// </summary>
    public class SweepParameter
    {
        public string Name { get; set; } = string.Empty;
        public SweepType Type { get; set; } = SweepType.Linear;
        public double StartValue { get; set; }
        public double EndValue { get; set; }
        public double StepValue { get; set; } = 1.0;
        public int StepCount { get; set; } = 10;
        public List<double> CustomValues { get; set; } = new();
        public string Unit { get; set; } = string.Empty;

        public List<double> GenerateValues()
        {
            var values = new List<double>();
            switch (Type)
            {
                case SweepType.Linear:
                    for (double v = StartValue; v <= EndValue; v += StepValue)
                        values.Add(v);
                    break;
                case SweepType.Logarithmic:
                    var logStart = Math.Log10(StartValue);
                    var logEnd = Math.Log10(EndValue);
                    var logStep = (logEnd - logStart) / (StepCount - 1);
                    for (int i = 0; i < StepCount; i++)
                        values.Add(Math.Pow(10, logStart + i * logStep));
                    break;
                case SweepType.Custom:
                    values.AddRange(CustomValues);
                    break;
                case SweepType.Random:
                    var random = new Random();
                    for (int i = 0; i < StepCount; i++)
                        values.Add(StartValue + random.NextDouble() * (EndValue - StartValue));
                    break;
            }
            return values;
        }
    }

    /// <summary>
    /// Represents a sweep configuration
    /// </summary>
    public class SweepConfiguration
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<SweepParameter> Parameters { get; set; } = new();
        public string TargetSequence { get; set; } = string.Empty;
        public bool ParallelExecution { get; set; }
        public int MaxParallelRuns { get; set; } = 4;
        public bool StopOnFail { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a single sweep point result
    /// </summary>
    public class SweepPointResult
    {
        public int PointIndex { get; set; }
        public Dictionary<string, double> ParameterValues { get; set; } = new();
        public bool Passed { get; set; }
        public Dictionary<string, object> Results { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public DateTime ExecutedAt { get; set; }
    }

    /// <summary>
    /// Represents a sweep execution result
    /// </summary>
    public class SweepResult
    {
        public string ConfigurationId { get; set; } = string.Empty;
        public string ExecutionId { get; set; } = Guid.NewGuid().ToString();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<SweepPointResult> PointResults { get; set; } = new();
        public bool Completed { get; set; }
        public int TotalPoints => PointResults.Count;
        public int PassedPoints => PointResults.Count(r => r.Passed);
        public int FailedPoints => PointResults.Count(r => !r.Passed);
    }

    /// <summary>
    /// Manages parameter sweep testing
    /// </summary>
    public class ParameterSweepManager
    {
        private static readonly Lazy<ParameterSweepManager> _instance = new(() => new ParameterSweepManager());
        public static ParameterSweepManager Instance => _instance.Value;

        private readonly Dictionary<string, SweepConfiguration> _configurations = new();
        private readonly List<SweepResult> _results = new();
        private readonly object _lock = new();

        private ParameterSweepManager() { }

        public void RegisterConfiguration(SweepConfiguration config)
        {
            lock (_lock)
            {
                _configurations[config.Id] = config;
            }
        }

        public SweepConfiguration? GetConfiguration(string configId)
        {
            lock (_lock)
            {
                return _configurations.TryGetValue(configId, out var config) ? config : null;
            }
        }

        public List<SweepConfiguration> GetAllConfigurations()
        {
            lock (_lock)
            {
                return _configurations.Values.ToList();
            }
        }

        public List<Dictionary<string, double>> GenerateSweepPoints(string configId)
        {
            lock (_lock)
            {
                if (!_configurations.TryGetValue(configId, out var config))
                    return new List<Dictionary<string, double>>();

                var parameterValues = config.Parameters.ToDictionary(p => p.Name, p => p.GenerateValues());
                return GenerateCombinations(parameterValues);
            }
        }

        private List<Dictionary<string, double>> GenerateCombinations(Dictionary<string, List<double>> parameterValues)
        {
            var result = new List<Dictionary<string, double>>();
            if (parameterValues.Count == 0) return result;

            var keys = parameterValues.Keys.ToList();
            GenerateCombinationsRecursive(parameterValues, keys, 0, new Dictionary<string, double>(), result);
            return result;
        }

        private void GenerateCombinationsRecursive(
            Dictionary<string, List<double>> parameterValues,
            List<string> keys,
            int index,
            Dictionary<string, double> current,
            List<Dictionary<string, double>> result)
        {
            if (index == keys.Count)
            {
                result.Add(new Dictionary<string, double>(current));
                return;
            }

            var key = keys[index];
            foreach (var value in parameterValues[key])
            {
                current[key] = value;
                GenerateCombinationsRecursive(parameterValues, keys, index + 1, current, result);
            }
            current.Remove(key);
        }

        public void RecordResult(SweepResult result)
        {
            lock (_lock)
            {
                _results.Add(result);
            }
        }

        public List<SweepResult> GetResults(string configId)
        {
            lock (_lock)
            {
                return _results.Where(r => r.ConfigurationId == configId).ToList();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _configurations.Clear();
                _results.Clear();
            }
        }
    }
}
