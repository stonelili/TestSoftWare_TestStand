using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.TestTimeCalculator
{
    /// <summary>
    /// Calculates estimated test execution time
    /// </summary>
    public class TestTimeCalculator
    {
        private readonly Dictionary<string, TimeSpan> _stepTimeHistory = new();
        private readonly Dictionary<string, List<TimeSpan>> _stepTimeRecords = new();

        /// <summary>
        /// Estimates the total time for a sequence
        /// </summary>
        public TestTimeEstimate EstimateSequenceTime(Sequence sequence, EstimationOptions? options = null)
        {
            options ??= new EstimationOptions();
            var estimate = new TestTimeEstimate
            {
                SequenceName = sequence.Name,
                StepCount = sequence.Steps.Count
            };

            foreach (var step in sequence.Steps)
            {
                var stepEstimate = EstimateStepTime(step, options);
                estimate.StepEstimates.Add(stepEstimate);
                estimate.TotalTime += stepEstimate.EstimatedTime;
            }

            estimate.MinimumTime = TimeSpan.FromTicks((long)(estimate.TotalTime.Ticks * (1 - options.VarianceFactor)));
            estimate.MaximumTime = TimeSpan.FromTicks((long)(estimate.TotalTime.Ticks * (1 + options.VarianceFactor)));

            return estimate;
        }

        /// <summary>
        /// Estimates time for a single step
        /// </summary>
        public StepTimeEstimate EstimateStepTime(TestStep step, EstimationOptions? options = null)
        {
            options ??= new EstimationOptions();
            var estimate = new StepTimeEstimate
            {
                StepName = step.Name,
                StepType = step.GetType().Name
            };

            if (options.UseHistoricalData && _stepTimeHistory.TryGetValue(step.Name, out var historicalTime))
            {
                estimate.EstimatedTime = historicalTime;
                estimate.EstimationMethod = "Historical";
                estimate.Confidence = CalculateConfidence(step.Name);
            }
            else
            {
                estimate.EstimatedTime = GetDefaultStepTime(step);
                estimate.EstimationMethod = "Default";
                estimate.Confidence = 0.5;
            }

            return estimate;
        }

        /// <summary>
        /// Records actual execution time for a step
        /// </summary>
        public void RecordStepTime(string stepName, TimeSpan actualTime)
        {
            if (!_stepTimeRecords.ContainsKey(stepName))
            {
                _stepTimeRecords[stepName] = new List<TimeSpan>();
            }

            _stepTimeRecords[stepName].Add(actualTime);

            var times = _stepTimeRecords[stepName];
            var average = TimeSpan.FromTicks((long)times.Average(t => t.Ticks));
            _stepTimeHistory[stepName] = average;
        }

        private TimeSpan GetDefaultStepTime(TestStep step)
        {
            var typeName = step.GetType().Name;

            return typeName switch
            {
                "DelayStep" => TimeSpan.FromMilliseconds(500),
                "NumericLimitStep" => TimeSpan.FromMilliseconds(100),
                "StringValueStep" => TimeSpan.FromMilliseconds(50),
                "PassFailStep" => TimeSpan.FromMilliseconds(50),
                "ActionStep" => TimeSpan.FromMilliseconds(200),
                "MessagePopupStep" => TimeSpan.FromSeconds(5),
                "InstrumentStep" => TimeSpan.FromMilliseconds(500),
                "InstrumentMeasureStep" => TimeSpan.FromMilliseconds(1000),
                "SequenceCallStep" => TimeSpan.FromSeconds(10),
                "LoopStep" => TimeSpan.FromMilliseconds(10),
                "IfStep" => TimeSpan.FromMilliseconds(10),
                "SwitchStep" => TimeSpan.FromMilliseconds(10),
                "ForLoopStep" => TimeSpan.FromMilliseconds(10),
                "WhileLoopStep" => TimeSpan.FromMilliseconds(10),
                "CodeModuleStep" => TimeSpan.FromSeconds(1),
                "FileIOStep" => TimeSpan.FromMilliseconds(100),
                _ => TimeSpan.FromMilliseconds(100)
            };
        }

        private double CalculateConfidence(string stepName)
        {
            if (_stepTimeRecords.TryGetValue(stepName, out var records) && records.Count > 0)
            {
                var confidence = Math.Min(1.0, records.Count / 10.0);
                
                if (records.Count > 1)
                {
                    var average = records.Average(t => t.Ticks);
                    var variance = records.Sum(t => Math.Pow(t.Ticks - average, 2)) / records.Count;
                    var stdDev = Math.Sqrt(variance);
                    var cv = average > 0 ? stdDev / average : 0;
                    confidence *= Math.Max(0.5, 1 - cv);
                }

                return confidence;
            }
            return 0.5;
        }

        /// <summary>
        /// Clears all historical data
        /// </summary>
        public void ClearHistory()
        {
            _stepTimeHistory.Clear();
            _stepTimeRecords.Clear();
        }

        /// <summary>
        /// Gets statistics for a step
        /// </summary>
        public StepTimeStatistics? GetStepStatistics(string stepName)
        {
            if (!_stepTimeRecords.TryGetValue(stepName, out var records) || records.Count == 0)
            {
                return null;
            }

            var stats = new StepTimeStatistics
            {
                StepName = stepName,
                SampleCount = records.Count,
                MinTime = TimeSpan.FromTicks(records.Min(t => t.Ticks)),
                MaxTime = TimeSpan.FromTicks(records.Max(t => t.Ticks)),
                AverageTime = TimeSpan.FromTicks((long)records.Average(t => t.Ticks)),
                TotalTime = TimeSpan.FromTicks(records.Sum(t => t.Ticks))
            };

            if (records.Count > 1)
            {
                var average = records.Average(t => t.Ticks);
                var variance = records.Sum(t => Math.Pow(t.Ticks - average, 2)) / records.Count;
                stats.StandardDeviation = TimeSpan.FromTicks((long)Math.Sqrt(variance));
            }

            return stats;
        }

        /// <summary>
        /// Calculates remaining time based on current progress
        /// </summary>
        public TimeSpan CalculateRemainingTime(Sequence sequence, int completedSteps, TimeSpan elapsedTime)
        {
            if (completedSteps <= 0 || completedSteps >= sequence.Steps.Count)
            {
                return TimeSpan.Zero;
            }

            var avgTimePerStep = TimeSpan.FromTicks(elapsedTime.Ticks / completedSteps);
            var remainingSteps = sequence.Steps.Count - completedSteps;

            return TimeSpan.FromTicks(avgTimePerStep.Ticks * remainingSteps);
        }
    }

    /// <summary>
    /// Options for time estimation
    /// </summary>
    public class EstimationOptions
    {
        public bool UseHistoricalData { get; set; } = true;
        public double VarianceFactor { get; set; } = 0.2;
        public bool IncludeSubSequences { get; set; } = true;
        public bool AccountForRetries { get; set; } = true;
        public int MaxRetryCount { get; set; } = 3;
    }

    /// <summary>
    /// Estimate for a complete test sequence
    /// </summary>
    public class TestTimeEstimate
    {
        public string SequenceName { get; set; } = string.Empty;
        public int StepCount { get; set; }
        public TimeSpan TotalTime { get; set; }
        public TimeSpan MinimumTime { get; set; }
        public TimeSpan MaximumTime { get; set; }
        public List<StepTimeEstimate> StepEstimates { get; set; } = new();
        public DateTime EstimatedCompletionTime => DateTime.Now + TotalTime;
    }

    /// <summary>
    /// Estimate for a single step
    /// </summary>
    public class StepTimeEstimate
    {
        public string StepName { get; set; } = string.Empty;
        public string StepType { get; set; } = string.Empty;
        public TimeSpan EstimatedTime { get; set; }
        public string EstimationMethod { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }

    /// <summary>
    /// Statistics for step execution times
    /// </summary>
    public class StepTimeStatistics
    {
        public string StepName { get; set; } = string.Empty;
        public int SampleCount { get; set; }
        public TimeSpan MinTime { get; set; }
        public TimeSpan MaxTime { get; set; }
        public TimeSpan AverageTime { get; set; }
        public TimeSpan StandardDeviation { get; set; }
        public TimeSpan TotalTime { get; set; }
    }

    /// <summary>
    /// Manager for test time calculations
    /// </summary>
    public class TestTimeManager
    {
        private static readonly Lazy<TestTimeManager> _instance = 
            new(() => new TestTimeManager());
        
        public static TestTimeManager Instance => _instance.Value;

        private readonly TestTimeCalculator _calculator = new();

        private TestTimeManager() { }

        public TestTimeCalculator Calculator => _calculator;

        /// <summary>
        /// Estimates time for a sequence
        /// </summary>
        public TestTimeEstimate EstimateTime(Sequence sequence)
        {
            return _calculator.EstimateSequenceTime(sequence);
        }

        /// <summary>
        /// Records step execution time
        /// </summary>
        public void RecordTime(string stepName, TimeSpan time)
        {
            _calculator.RecordStepTime(stepName, time);
        }

        /// <summary>
        /// Gets remaining time estimate
        /// </summary>
        public TimeSpan GetRemainingTime(Sequence sequence, int completedSteps, TimeSpan elapsed)
        {
            return _calculator.CalculateRemainingTime(sequence, completedSteps, elapsed);
        }
    }
}
