// Phase 20: Data Visualization - Charts and graphs for test data
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.DataVisualization
{
    /// <summary>
    /// Chart type enumeration
    /// </summary>
    public enum ChartType
    {
        Line,
        Bar,
        Scatter,
        Histogram,
        Pie,
        Area,
        BoxPlot,
        Pareto
    }

    /// <summary>
    /// Represents a data point for visualization
    /// </summary>
    public class DataPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string? Label { get; set; }
        public string? Category { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a data series for charts
    /// </summary>
    public class DataSeries
    {
        public string Name { get; set; } = string.Empty;
        public List<DataPoint> Points { get; set; } = new();
        public string Color { get; set; } = "#000000";
        public bool Visible { get; set; } = true;
        public string LineStyle { get; set; } = "Solid";
        public int LineWidth { get; set; } = 2;
        public string MarkerStyle { get; set; } = "Circle";
    }

    /// <summary>
    /// Represents a chart configuration
    /// </summary>
    public class ChartConfiguration
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public ChartType Type { get; set; } = ChartType.Line;
        public string XAxisLabel { get; set; } = string.Empty;
        public string YAxisLabel { get; set; } = string.Empty;
        public List<DataSeries> Series { get; set; } = new();
        public bool ShowLegend { get; set; } = true;
        public bool ShowGrid { get; set; } = true;
        public double? XMin { get; set; }
        public double? XMax { get; set; }
        public double? YMin { get; set; }
        public double? YMax { get; set; }
        public int Width { get; set; } = 800;
        public int Height { get; set; } = 600;
    }

    /// <summary>
    /// Represents histogram bins
    /// </summary>
    public class HistogramBin
    {
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// Histogram result
    /// </summary>
    public class HistogramResult
    {
        public List<HistogramBin> Bins { get; set; } = new();
        public double Mean { get; set; }
        public double StandardDeviation { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// Manages data visualization
    /// </summary>
    public class DataVisualizationManager
    {
        private static readonly Lazy<DataVisualizationManager> _instance = new(() => new DataVisualizationManager());
        public static DataVisualizationManager Instance => _instance.Value;

        private readonly Dictionary<string, ChartConfiguration> _charts = new();
        private readonly object _lock = new();

        private DataVisualizationManager() { }

        public void RegisterChart(ChartConfiguration chart)
        {
            lock (_lock)
            {
                _charts[chart.Id] = chart;
            }
        }

        public ChartConfiguration? GetChart(string chartId)
        {
            lock (_lock)
            {
                return _charts.TryGetValue(chartId, out var chart) ? chart : null;
            }
        }

        public List<ChartConfiguration> GetAllCharts()
        {
            lock (_lock)
            {
                return _charts.Values.ToList();
            }
        }

        public HistogramResult CreateHistogram(List<double> data, int binCount = 10)
        {
            if (data.Count == 0)
                return new HistogramResult();

            var min = data.Min();
            var max = data.Max();
            var binWidth = (max - min) / binCount;
            var bins = new List<HistogramBin>();

            for (int i = 0; i < binCount; i++)
            {
                var lower = min + i * binWidth;
                var upper = min + (i + 1) * binWidth;
                var count = data.Count(d => d >= lower && (i == binCount - 1 ? d <= upper : d < upper));
                bins.Add(new HistogramBin
                {
                    LowerBound = lower,
                    UpperBound = upper,
                    Count = count,
                    Percentage = (double)count / data.Count * 100
                });
            }

            var mean = data.Average();
            var variance = data.Sum(d => Math.Pow(d - mean, 2)) / data.Count;

            return new HistogramResult
            {
                Bins = bins,
                Mean = mean,
                StandardDeviation = Math.Sqrt(variance),
                TotalCount = data.Count
            };
        }

        public ChartConfiguration CreateLineChart(string title, List<double> xValues, List<double> yValues, string seriesName = "Series1")
        {
            var points = xValues.Zip(yValues, (x, y) => new DataPoint { X = x, Y = y }).ToList();
            var chart = new ChartConfiguration
            {
                Title = title,
                Type = ChartType.Line,
                Series = new List<DataSeries>
                {
                    new DataSeries { Name = seriesName, Points = points }
                }
            };
            RegisterChart(chart);
            return chart;
        }

        public ChartConfiguration CreateScatterChart(string title, List<DataPoint> points, string seriesName = "Series1")
        {
            var chart = new ChartConfiguration
            {
                Title = title,
                Type = ChartType.Scatter,
                Series = new List<DataSeries>
                {
                    new DataSeries { Name = seriesName, Points = points }
                }
            };
            RegisterChart(chart);
            return chart;
        }

        public ChartConfiguration CreateBarChart(string title, List<string> categories, List<double> values)
        {
            var points = categories.Zip(values, (c, v) => new DataPoint { X = categories.IndexOf(c), Y = v, Label = c }).ToList();
            var chart = new ChartConfiguration
            {
                Title = title,
                Type = ChartType.Bar,
                Series = new List<DataSeries>
                {
                    new DataSeries { Name = "Data", Points = points }
                }
            };
            RegisterChart(chart);
            return chart;
        }

        public void Clear()
        {
            lock (_lock)
            {
                _charts.Clear();
            }
        }
    }
}
