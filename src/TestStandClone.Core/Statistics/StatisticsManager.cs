using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace TestStandClone.Core.Statistics
{
    /// <summary>
    /// Statistical measure type
    /// </summary>
    public enum StatisticType
    {
        /// <summary>Count of items</summary>
        Count,
        /// <summary>Sum of values</summary>
        Sum,
        /// <summary>Mean/Average</summary>
        Mean,
        /// <summary>Median</summary>
        Median,
        /// <summary>Minimum value</summary>
        Min,
        /// <summary>Maximum value</summary>
        Max,
        /// <summary>Standard deviation</summary>
        StdDev,
        /// <summary>Variance</summary>
        Variance,
        /// <summary>Range (max - min)</summary>
        Range,
        /// <summary>Pass rate percentage</summary>
        PassRate,
        /// <summary>Fail rate percentage</summary>
        FailRate,
        /// <summary>Cp (Process Capability)</summary>
        Cp,
        /// <summary>Cpk (Process Capability Index)</summary>
        Cpk
    }

    /// <summary>
    /// Data point for statistical analysis
    /// </summary>
    public class DataPoint
    {
        /// <summary>Timestamp</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
        /// <summary>Value</summary>
        public double Value { get; set; }
        /// <summary>Category or group</summary>
        public string Category { get; set; } = string.Empty;
        /// <summary>Label</summary>
        public string Label { get; set; } = string.Empty;
        /// <summary>Whether this is a passing value</summary>
        public bool Passed { get; set; } = true;
        /// <summary>Additional metadata</summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Statistical summary
    /// </summary>
    public class StatisticalSummary
    {
        /// <summary>Number of data points</summary>
        public int Count { get; set; }
        /// <summary>Sum of all values</summary>
        public double Sum { get; set; }
        /// <summary>Mean (average)</summary>
        public double Mean { get; set; }
        /// <summary>Median</summary>
        public double Median { get; set; }
        /// <summary>Mode</summary>
        public double Mode { get; set; }
        /// <summary>Minimum value</summary>
        public double Min { get; set; }
        /// <summary>Maximum value</summary>
        public double Max { get; set; }
        /// <summary>Range (max - min)</summary>
        public double Range { get; set; }
        /// <summary>Standard deviation</summary>
        public double StdDev { get; set; }
        /// <summary>Variance</summary>
        public double Variance { get; set; }
        /// <summary>Number of passing values</summary>
        public int PassCount { get; set; }
        /// <summary>Number of failing values</summary>
        public int FailCount { get; set; }
        /// <summary>Pass rate percentage</summary>
        public double PassRate { get; set; }
        /// <summary>First quartile (25th percentile)</summary>
        public double Q1 { get; set; }
        /// <summary>Third quartile (75th percentile)</summary>
        public double Q3 { get; set; }
        /// <summary>Interquartile range</summary>
        public double IQR { get; set; }
        /// <summary>Skewness</summary>
        public double Skewness { get; set; }
        /// <summary>Kurtosis</summary>
        public double Kurtosis { get; set; }
    }

    /// <summary>
    /// Process capability analysis
    /// </summary>
    public class ProcessCapability
    {
        /// <summary>Upper specification limit</summary>
        public double USL { get; set; }
        /// <summary>Lower specification limit</summary>
        public double LSL { get; set; }
        /// <summary>Target value</summary>
        public double Target { get; set; }
        /// <summary>Cp (Process Capability)</summary>
        public double Cp { get; set; }
        /// <summary>Cpk (Process Capability Index)</summary>
        public double Cpk { get; set; }
        /// <summary>Cpu (Upper Process Capability)</summary>
        public double Cpu { get; set; }
        /// <summary>Cpl (Lower Process Capability)</summary>
        public double Cpl { get; set; }
        /// <summary>Pp (Process Performance)</summary>
        public double Pp { get; set; }
        /// <summary>Ppk (Process Performance Index)</summary>
        public double Ppk { get; set; }
        /// <summary>Sigma level</summary>
        public double SigmaLevel { get; set; }
        /// <summary>Expected defects per million</summary>
        public double DPM { get; set; }
    }

    /// <summary>
    /// Trend analysis result
    /// </summary>
    public class TrendAnalysis
    {
        /// <summary>Trend direction (-1: decreasing, 0: stable, 1: increasing)</summary>
        public int Direction { get; set; }
        /// <summary>Slope of trend line</summary>
        public double Slope { get; set; }
        /// <summary>Intercept of trend line</summary>
        public double Intercept { get; set; }
        /// <summary>R-squared (coefficient of determination)</summary>
        public double RSquared { get; set; }
        /// <summary>Whether trend is statistically significant</summary>
        public bool IsSignificant { get; set; }
        /// <summary>Predicted next value</summary>
        public double PredictedNext { get; set; }
    }

    /// <summary>
    /// Statistical calculator
    /// </summary>
    public static class StatisticalCalculator
    {
        /// <summary>Calculates the mean of values</summary>
        public static double Mean(IEnumerable<double> values)
        {
            var list = values.ToList();
            if (list.Count == 0) return 0;
            return list.Average();
        }

        /// <summary>Calculates the median of values</summary>
        public static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0) return 0;
            int mid = sorted.Count / 2;
            if (sorted.Count % 2 == 0)
                return (sorted[mid - 1] + sorted[mid]) / 2;
            return sorted[mid];
        }

        /// <summary>Calculates the mode of values</summary>
        public static double Mode(IEnumerable<double> values)
        {
            var list = values.ToList();
            if (list.Count == 0) return 0;
            return list.GroupBy(v => v)
                       .OrderByDescending(g => g.Count())
                       .First().Key;
        }

        /// <summary>Calculates the standard deviation of values</summary>
        public static double StandardDeviation(IEnumerable<double> values, bool sample = true)
        {
            var list = values.ToList();
            if (list.Count <= 1) return 0;
            double mean = list.Average();
            double sumSquares = list.Sum(v => Math.Pow(v - mean, 2));
            int divisor = sample ? list.Count - 1 : list.Count;
            return Math.Sqrt(sumSquares / divisor);
        }

        /// <summary>Calculates the variance of values</summary>
        public static double Variance(IEnumerable<double> values, bool sample = true)
        {
            double stdDev = StandardDeviation(values, sample);
            return stdDev * stdDev;
        }

        /// <summary>Calculates a percentile value</summary>
        public static double Percentile(IEnumerable<double> values, double percentile)
        {
            var sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0) return 0;
            
            double index = (percentile / 100.0) * (sorted.Count - 1);
            int lower = (int)Math.Floor(index);
            int upper = (int)Math.Ceiling(index);
            
            if (lower == upper) return sorted[lower];
            
            double fraction = index - lower;
            return sorted[lower] + (sorted[upper] - sorted[lower]) * fraction;
        }

        /// <summary>Calculates skewness of values</summary>
        public static double Skewness(IEnumerable<double> values)
        {
            var list = values.ToList();
            if (list.Count < 3) return 0;
            
            double mean = list.Average();
            double stdDev = StandardDeviation(list);
            if (stdDev == 0) return 0;
            
            double n = list.Count;
            double sum = list.Sum(v => Math.Pow((v - mean) / stdDev, 3));
            return (n / ((n - 1) * (n - 2))) * sum;
        }

        /// <summary>Calculates kurtosis of values</summary>
        public static double Kurtosis(IEnumerable<double> values)
        {
            var list = values.ToList();
            if (list.Count < 4) return 0;
            
            double mean = list.Average();
            double stdDev = StandardDeviation(list);
            if (stdDev == 0) return 0;
            
            double n = list.Count;
            double sum = list.Sum(v => Math.Pow((v - mean) / stdDev, 4));
            return ((n * (n + 1)) / ((n - 1) * (n - 2) * (n - 3))) * sum -
                   (3 * (n - 1) * (n - 1)) / ((n - 2) * (n - 3));
        }

        /// <summary>Calculates a complete statistical summary</summary>
        public static StatisticalSummary CalculateSummary(IEnumerable<DataPoint> dataPoints)
        {
            var points = dataPoints.ToList();
            var values = points.Select(p => p.Value).ToList();
            
            if (values.Count == 0)
            {
                return new StatisticalSummary();
            }

            var summary = new StatisticalSummary
            {
                Count = values.Count,
                Sum = values.Sum(),
                Mean = Mean(values),
                Median = Median(values),
                Mode = Mode(values),
                Min = values.Min(),
                Max = values.Max(),
                Range = values.Max() - values.Min(),
                StdDev = StandardDeviation(values),
                Variance = Variance(values),
                PassCount = points.Count(p => p.Passed),
                FailCount = points.Count(p => !p.Passed),
                Q1 = Percentile(values, 25),
                Q3 = Percentile(values, 75),
                Skewness = Skewness(values),
                Kurtosis = Kurtosis(values)
            };

            summary.IQR = summary.Q3 - summary.Q1;
            summary.PassRate = values.Count > 0 ? (double)summary.PassCount / values.Count * 100 : 0;

            return summary;
        }

        /// <summary>Calculates process capability</summary>
        public static ProcessCapability CalculateProcessCapability(
            IEnumerable<double> values, double usl, double lsl, double target)
        {
            var list = values.ToList();
            if (list.Count < 2) return new ProcessCapability();

            double mean = Mean(list);
            double stdDev = StandardDeviation(list);
            
            if (stdDev == 0) stdDev = 0.0001; // Avoid division by zero

            double cp = (usl - lsl) / (6 * stdDev);
            double cpu = (usl - mean) / (3 * stdDev);
            double cpl = (mean - lsl) / (3 * stdDev);
            double cpk = Math.Min(cpu, cpl);

            // Sigma level approximation
            double sigmaLevel = cpk * 3;

            return new ProcessCapability
            {
                USL = usl,
                LSL = lsl,
                Target = target,
                Cp = cp,
                Cpk = cpk,
                Cpu = cpu,
                Cpl = cpl,
                Pp = cp, // Simplified, same as Cp for this calculation
                Ppk = cpk, // Simplified, same as Cpk for this calculation
                SigmaLevel = sigmaLevel,
                DPM = CalculateDPM(cpk)
            };
        }

        /// <summary>Calculates defects per million based on Cpk</summary>
        private static double CalculateDPM(double cpk)
        {
            // Simplified DPM calculation
            if (cpk >= 2.0) return 0.002;
            if (cpk >= 1.67) return 0.57;
            if (cpk >= 1.33) return 63;
            if (cpk >= 1.0) return 2700;
            if (cpk >= 0.67) return 45500;
            if (cpk >= 0.33) return 158655;
            return 500000;
        }

        /// <summary>Performs trend analysis</summary>
        public static TrendAnalysis AnalyzeTrend(IEnumerable<DataPoint> dataPoints)
        {
            var points = dataPoints.OrderBy(p => p.Timestamp).ToList();
            if (points.Count < 2) return new TrendAnalysis();

            // Linear regression
            double[] x = Enumerable.Range(0, points.Count).Select(i => (double)i).ToArray();
            double[] y = points.Select(p => p.Value).ToArray();

            double xMean = x.Average();
            double yMean = y.Average();

            double numerator = 0;
            double denominator = 0;

            for (int i = 0; i < points.Count; i++)
            {
                numerator += (x[i] - xMean) * (y[i] - yMean);
                denominator += Math.Pow(x[i] - xMean, 2);
            }

            double slope = denominator != 0 ? numerator / denominator : 0;
            double intercept = yMean - slope * xMean;

            // Calculate R-squared
            double ssTot = y.Sum(v => Math.Pow(v - yMean, 2));
            double ssRes = 0;
            for (int i = 0; i < points.Count; i++)
            {
                double predicted = slope * x[i] + intercept;
                ssRes += Math.Pow(y[i] - predicted, 2);
            }
            double rSquared = ssTot != 0 ? 1 - (ssRes / ssTot) : 0;

            return new TrendAnalysis
            {
                Direction = slope > 0.001 ? 1 : (slope < -0.001 ? -1 : 0),
                Slope = slope,
                Intercept = intercept,
                RSquared = rSquared,
                IsSignificant = rSquared > 0.5,
                PredictedNext = slope * points.Count + intercept
            };
        }
    }

    /// <summary>
    /// Statistics manager singleton
    /// </summary>
    public sealed class StatisticsManager
    {
        private static readonly Lazy<StatisticsManager> _instance = 
            new Lazy<StatisticsManager>(() => new StatisticsManager());
        
        /// <summary>Gets the singleton instance</summary>
        public static StatisticsManager Instance => _instance.Value;

        private readonly Dictionary<string, List<DataPoint>> _dataSets = new();
        private readonly object _lockObject = new object();

        private StatisticsManager() { }

        /// <summary>Adds a data point to a named dataset</summary>
        public void AddDataPoint(string datasetName, DataPoint point)
        {
            lock (_lockObject)
            {
                if (!_dataSets.ContainsKey(datasetName))
                {
                    _dataSets[datasetName] = new List<DataPoint>();
                }
                _dataSets[datasetName].Add(point);
            }
        }

        /// <summary>Adds multiple data points to a named dataset</summary>
        public void AddDataPoints(string datasetName, IEnumerable<DataPoint> points)
        {
            lock (_lockObject)
            {
                if (!_dataSets.ContainsKey(datasetName))
                {
                    _dataSets[datasetName] = new List<DataPoint>();
                }
                _dataSets[datasetName].AddRange(points);
            }
        }

        /// <summary>Gets all data points for a dataset</summary>
        public List<DataPoint> GetDataPoints(string datasetName)
        {
            lock (_lockObject)
            {
                if (_dataSets.TryGetValue(datasetName, out var points))
                {
                    return points.ToList();
                }
                return new List<DataPoint>();
            }
        }

        /// <summary>Gets a statistical summary for a dataset</summary>
        public StatisticalSummary GetSummary(string datasetName)
        {
            var points = GetDataPoints(datasetName);
            return StatisticalCalculator.CalculateSummary(points);
        }

        /// <summary>Gets process capability for a dataset</summary>
        public ProcessCapability GetProcessCapability(string datasetName, double usl, double lsl, double target)
        {
            var points = GetDataPoints(datasetName);
            var values = points.Select(p => p.Value);
            return StatisticalCalculator.CalculateProcessCapability(values, usl, lsl, target);
        }

        /// <summary>Gets trend analysis for a dataset</summary>
        public TrendAnalysis GetTrendAnalysis(string datasetName)
        {
            var points = GetDataPoints(datasetName);
            return StatisticalCalculator.AnalyzeTrend(points);
        }

        /// <summary>Clears a dataset</summary>
        public void ClearDataset(string datasetName)
        {
            lock (_lockObject)
            {
                if (_dataSets.ContainsKey(datasetName))
                {
                    _dataSets[datasetName].Clear();
                }
            }
        }

        /// <summary>Clears all datasets</summary>
        public void ClearAll()
        {
            lock (_lockObject)
            {
                _dataSets.Clear();
            }
        }

        /// <summary>Gets all dataset names</summary>
        public List<string> GetDatasetNames()
        {
            lock (_lockObject)
            {
                return _dataSets.Keys.ToList();
            }
        }

        /// <summary>Exports a dataset to CSV</summary>
        public void ExportToCsv(string datasetName, string filePath)
        {
            var points = GetDataPoints(datasetName);
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,Value,Category,Label,Passed");
            foreach (var p in points)
            {
                sb.AppendLine($"{p.Timestamp:yyyy-MM-dd HH:mm:ss},{p.Value},{p.Category},{p.Label},{p.Passed}");
            }
            File.WriteAllText(filePath, sb.ToString());
        }

        /// <summary>Exports summary to JSON</summary>
        public void ExportSummaryToJson(string datasetName, string filePath)
        {
            var summary = GetSummary(datasetName);
            string json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}
