using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.DataCorrelation
{
    public enum CorrelationType { Pearson, Spearman, Kendall }
    public enum TrendType { None, Increasing, Decreasing, Cyclic, Random }

    public class DataPoint
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string SerialNumber { get; set; } = string.Empty;
        public string ParameterName { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class CorrelationResult
    {
        public string Parameter1 { get; set; } = string.Empty;
        public string Parameter2 { get; set; } = string.Empty;
        public CorrelationType Type { get; set; } = CorrelationType.Pearson;
        public double Coefficient { get; set; }
        public double PValue { get; set; }
        public int SampleSize { get; set; }
        public bool IsSignificant => PValue < 0.05;
        public DateTime CalculatedAt { get; set; } = DateTime.Now;
    }

    public class TrendAnalysisResult
    {
        public string ParameterName { get; set; } = string.Empty;
        public TrendType Trend { get; set; } = TrendType.None;
        public double Slope { get; set; }
        public double Intercept { get; set; }
        public double RSquared { get; set; }
        public int SampleSize { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class DataCorrelationManager
    {
        private static readonly Lazy<DataCorrelationManager> _instance = new(() => new DataCorrelationManager());
        public static DataCorrelationManager Instance => _instance.Value;
        private readonly List<DataPoint> _dataPoints = new();
        private readonly List<CorrelationResult> _correlationResults = new();
        private readonly object _lock = new();

        private DataCorrelationManager() { }

        public void AddDataPoint(DataPoint point)
        {
            lock (_lock) { _dataPoints.Add(point); }
        }

        public void AddDataPoints(IEnumerable<DataPoint> points)
        {
            lock (_lock) { _dataPoints.AddRange(points); }
        }

        public CorrelationResult CalculateCorrelation(string param1, string param2, CorrelationType type = CorrelationType.Pearson)
        {
            lock (_lock)
            {
                var data1 = _dataPoints.Where(p => p.ParameterName == param1).OrderBy(p => p.Timestamp).Select(p => p.Value).ToList();
                var data2 = _dataPoints.Where(p => p.ParameterName == param2).OrderBy(p => p.Timestamp).Select(p => p.Value).ToList();

                int n = Math.Min(data1.Count, data2.Count);
                if (n < 3) return new CorrelationResult { Parameter1 = param1, Parameter2 = param2, SampleSize = n };

                data1 = data1.Take(n).ToList();
                data2 = data2.Take(n).ToList();

                double coefficient = CalculatePearsonCorrelation(data1, data2);
                double pValue = CalculatePValue(coefficient, n);

                var result = new CorrelationResult
                {
                    Parameter1 = param1,
                    Parameter2 = param2,
                    Type = type,
                    Coefficient = coefficient,
                    PValue = pValue,
                    SampleSize = n
                };

                _correlationResults.Add(result);
                return result;
            }
        }

        private static double CalculatePearsonCorrelation(List<double> x, List<double> y)
        {
            int n = x.Count;
            double sumX = x.Sum();
            double sumY = y.Sum();
            double sumXY = x.Zip(y, (a, b) => a * b).Sum();
            double sumX2 = x.Sum(a => a * a);
            double sumY2 = y.Sum(a => a * a);

            double numerator = n * sumXY - sumX * sumY;
            double denominator = Math.Sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));

            return denominator == 0 ? 0 : numerator / denominator;
        }

        private static double CalculatePValue(double r, int n)
        {
            if (n < 3) return 1.0;
            double t = r * Math.Sqrt((n - 2) / (1 - r * r));
            return 2 * (1 - NormalCDF(Math.Abs(t)));
        }

        private static double NormalCDF(double x)
        {
            double a1 = 0.254829592, a2 = -0.284496736, a3 = 1.421413741;
            double a4 = -1.453152027, a5 = 1.061405429, p = 0.3275911;
            int sign = x < 0 ? -1 : 1;
            x = Math.Abs(x) / Math.Sqrt(2);
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);
            return 0.5 * (1.0 + sign * y);
        }

        public TrendAnalysisResult AnalyzeTrend(string parameterName, DateTime? startDate = null, DateTime? endDate = null)
        {
            lock (_lock)
            {
                var data = _dataPoints.Where(p => p.ParameterName == parameterName);
                if (startDate.HasValue) data = data.Where(p => p.Timestamp >= startDate.Value);
                if (endDate.HasValue) data = data.Where(p => p.Timestamp <= endDate.Value);

                var orderedData = data.OrderBy(p => p.Timestamp).ToList();
                if (orderedData.Count < 3) return new TrendAnalysisResult { ParameterName = parameterName };

                var x = Enumerable.Range(0, orderedData.Count).Select(i => (double)i).ToList();
                var y = orderedData.Select(p => p.Value).ToList();

                double slope, intercept, rSquared;
                CalculateLinearRegression(x, y, out slope, out intercept, out rSquared);

                var trend = slope switch
                {
                    > 0.01 => TrendType.Increasing,
                    < -0.01 => TrendType.Decreasing,
                    _ => TrendType.None
                };

                return new TrendAnalysisResult
                {
                    ParameterName = parameterName,
                    Trend = trend,
                    Slope = slope,
                    Intercept = intercept,
                    RSquared = rSquared,
                    SampleSize = orderedData.Count,
                    StartDate = orderedData.First().Timestamp,
                    EndDate = orderedData.Last().Timestamp
                };
            }
        }

        private static void CalculateLinearRegression(List<double> x, List<double> y, out double slope, out double intercept, out double rSquared)
        {
            int n = x.Count;
            double sumX = x.Sum(), sumY = y.Sum();
            double sumXY = x.Zip(y, (a, b) => a * b).Sum();
            double sumX2 = x.Sum(a => a * a);
            double meanY = sumY / n;

            slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            intercept = (sumY - slope * sumX) / n;

            double s = slope;
            double i = intercept;
            var predicted = x.Select(xi => s * xi + i).ToList();
            double ssRes = y.Zip(predicted, (yi, pi) => Math.Pow(yi - pi, 2)).Sum();
            double ssTot = y.Select(yi => Math.Pow(yi - meanY, 2)).Sum();
            rSquared = ssTot == 0 ? 0 : 1 - ssRes / ssTot;
        }

        public List<CorrelationResult> GetAllCorrelations() { lock (_lock) { return _correlationResults.ToList(); } }
        public List<DataPoint> GetDataPoints(string parameterName) { lock (_lock) { return _dataPoints.Where(p => p.ParameterName == parameterName).ToList(); } }
    }
}
