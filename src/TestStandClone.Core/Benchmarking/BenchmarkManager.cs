using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TestStandClone.Core.Benchmarking
{
    /// <summary>
    /// Represents a benchmark run.
    /// </summary>
    public class BenchmarkRun
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public int Iterations { get; set; }
        public List<BenchmarkMeasurement> Measurements { get; set; } = new();
    }

    /// <summary>
    /// Represents a single benchmark measurement.
    /// </summary>
    public class BenchmarkMeasurement
    {
        public int Iteration { get; set; }
        public TimeSpan Duration { get; set; }
        public long MemoryBefore { get; set; }
        public long MemoryAfter { get; set; }
        public long MemoryDelta => MemoryAfter - MemoryBefore;
        public Dictionary<string, object> CustomMetrics { get; set; } = new();
    }

    /// <summary>
    /// Statistics for benchmark results.
    /// </summary>
    public class BenchmarkStatistics
    {
        public string BenchmarkName { get; set; } = string.Empty;
        public int TotalIterations { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MedianDuration { get; set; }
        public TimeSpan StdDeviation { get; set; }
        public double OperationsPerSecond { get; set; }
        public long AverageMemoryDelta { get; set; }
    }

    /// <summary>
    /// Configuration for benchmarking.
    /// </summary>
    public class BenchmarkConfig
    {
        public int WarmupIterations { get; set; } = 5;
        public int MeasuredIterations { get; set; } = 100;
        public bool CollectGarbage { get; set; } = true;
        public bool MeasureMemory { get; set; } = true;
        public TimeSpan MaxDuration { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Manager for benchmarking.
    /// </summary>
    public class BenchmarkManager
    {
        private static BenchmarkManager? _instance;
        private static readonly object _lock = new();

        private readonly List<BenchmarkRun> _runs = new();

        public static BenchmarkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new BenchmarkManager();
                    }
                }
                return _instance;
            }
        }

        private BenchmarkManager() { }

        /// <summary>
        /// Runs a benchmark.
        /// </summary>
        public BenchmarkRun RunBenchmark(string name, Action action, BenchmarkConfig? config = null)
        {
            config ??= new BenchmarkConfig();
            
            var run = new BenchmarkRun
            {
                Name = name,
                Iterations = config.MeasuredIterations,
                StartTime = DateTime.UtcNow
            };

            // Warmup
            for (int i = 0; i < config.WarmupIterations; i++)
            {
                action();
            }

            // Force GC before measurements
            if (config.CollectGarbage)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            var stopwatch = new Stopwatch();

            // Measured iterations
            for (int i = 0; i < config.MeasuredIterations; i++)
            {
                var measurement = new BenchmarkMeasurement { Iteration = i + 1 };

                if (config.MeasureMemory)
                {
                    measurement.MemoryBefore = GC.GetTotalMemory(false);
                }

                stopwatch.Restart();
                action();
                stopwatch.Stop();

                measurement.Duration = stopwatch.Elapsed;

                if (config.MeasureMemory)
                {
                    measurement.MemoryAfter = GC.GetTotalMemory(false);
                }

                run.Measurements.Add(measurement);
            }

            run.EndTime = DateTime.UtcNow;
            _runs.Add(run);

            return run;
        }

        /// <summary>
        /// Calculates statistics for a benchmark run.
        /// </summary>
        public BenchmarkStatistics CalculateStatistics(BenchmarkRun run)
        {
            var durations = run.Measurements.Select(m => m.Duration.TotalMilliseconds).OrderBy(d => d).ToList();
            var count = durations.Count;

            if (count == 0)
            {
                return new BenchmarkStatistics { BenchmarkName = run.Name };
            }

            var average = durations.Average();
            var median = count % 2 == 0
                ? (durations[count / 2 - 1] + durations[count / 2]) / 2
                : durations[count / 2];

            var variance = durations.Sum(d => Math.Pow(d - average, 2)) / count;
            var stdDev = Math.Sqrt(variance);

            return new BenchmarkStatistics
            {
                BenchmarkName = run.Name,
                TotalIterations = count,
                MinDuration = TimeSpan.FromMilliseconds(durations.Min()),
                MaxDuration = TimeSpan.FromMilliseconds(durations.Max()),
                AverageDuration = TimeSpan.FromMilliseconds(average),
                MedianDuration = TimeSpan.FromMilliseconds(median),
                StdDeviation = TimeSpan.FromMilliseconds(stdDev),
                OperationsPerSecond = average > 0 ? 1000 / average : 0,
                AverageMemoryDelta = (long)run.Measurements.Average(m => m.MemoryDelta)
            };
        }

        /// <summary>
        /// Gets all benchmark runs.
        /// </summary>
        public IEnumerable<BenchmarkRun> GetAllRuns()
        {
            return _runs;
        }

        /// <summary>
        /// Gets a benchmark run by ID.
        /// </summary>
        public BenchmarkRun? GetRun(string runId)
        {
            return _runs.FirstOrDefault(r => r.Id == runId);
        }

        /// <summary>
        /// Compares two benchmark runs.
        /// </summary>
        public BenchmarkComparison CompareBenchmarks(string runId1, string runId2)
        {
            var run1 = GetRun(runId1);
            var run2 = GetRun(runId2);

            if (run1 == null || run2 == null)
            {
                return new BenchmarkComparison { IsValid = false };
            }

            var stats1 = CalculateStatistics(run1);
            var stats2 = CalculateStatistics(run2);

            var avgChange = (stats2.AverageDuration.TotalMilliseconds - stats1.AverageDuration.TotalMilliseconds)
                / stats1.AverageDuration.TotalMilliseconds * 100;

            return new BenchmarkComparison
            {
                IsValid = true,
                Run1Name = run1.Name,
                Run2Name = run2.Name,
                Run1Stats = stats1,
                Run2Stats = stats2,
                AverageDurationChangePercent = avgChange,
                IsFaster = avgChange < 0
            };
        }

        /// <summary>
        /// Clears all benchmark runs.
        /// </summary>
        public void ClearRuns()
        {
            _runs.Clear();
        }
    }

    /// <summary>
    /// Comparison result between two benchmarks.
    /// </summary>
    public class BenchmarkComparison
    {
        public bool IsValid { get; set; }
        public string Run1Name { get; set; } = string.Empty;
        public string Run2Name { get; set; } = string.Empty;
        public BenchmarkStatistics? Run1Stats { get; set; }
        public BenchmarkStatistics? Run2Stats { get; set; }
        public double AverageDurationChangePercent { get; set; }
        public bool IsFaster { get; set; }
    }
}
