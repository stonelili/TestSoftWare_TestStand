using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.Dashboard
{
    public enum WidgetType { Counter, Gauge, Chart, Table, Status, Progress, Timeline }
    public enum ChartType { Line, Bar, Pie, Scatter, Area }

    public class DashboardWidget
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public WidgetType Type { get; set; } = WidgetType.Counter;
        public ChartType? ChartType { get; set; }
        public int Row { get; set; } = 0;
        public int Column { get; set; } = 0;
        public int RowSpan { get; set; } = 1;
        public int ColumnSpan { get; set; } = 1;
        public string DataSource { get; set; } = string.Empty;
        public int RefreshIntervalSeconds { get; set; } = 30;
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    public class DashboardDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Rows { get; set; } = 4;
        public int Columns { get; set; } = 4;
        public List<DashboardWidget> Widgets { get; set; } = new();
        public bool IsDefault { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class DashboardDataPoint
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class WidgetData
    {
        public string WidgetId { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public object? Value { get; set; }
        public List<DashboardDataPoint> DataPoints { get; set; } = new();
        public string Status { get; set; } = "OK";
    }

    public interface IDashboardDataProvider
    {
        string Name { get; }
        WidgetData GetData(DashboardWidget widget);
    }

    public class TestExecutionDataProvider : IDashboardDataProvider
    {
        public string Name => "TestExecution";

        public WidgetData GetData(DashboardWidget widget)
        {
            var random = new Random();
            return widget.Type switch
            {
                WidgetType.Counter => new WidgetData
                {
                    WidgetId = widget.Id,
                    Value = random.Next(100, 1000),
                    Status = "OK"
                },
                WidgetType.Gauge => new WidgetData
                {
                    WidgetId = widget.Id,
                    Value = random.NextDouble() * 100,
                    Status = "OK"
                },
                WidgetType.Chart => new WidgetData
                {
                    WidgetId = widget.Id,
                    DataPoints = Enumerable.Range(0, 10)
                        .Select(i => new DashboardDataPoint
                        {
                            Timestamp = DateTime.Now.AddHours(-i),
                            Label = $"Point {i}",
                            Value = random.Next(50, 100)
                        }).ToList()
                },
                _ => new WidgetData { WidgetId = widget.Id }
            };
        }
    }

    public class DashboardManager
    {
        private static readonly Lazy<DashboardManager> _instance = new(() => new DashboardManager());
        public static DashboardManager Instance => _instance.Value;
        private readonly Dictionary<string, DashboardDefinition> _dashboards = new();
        private readonly Dictionary<string, IDashboardDataProvider> _dataProviders = new();
        private readonly Dictionary<string, WidgetData> _widgetDataCache = new();
        private readonly object _lock = new();

        public event EventHandler<WidgetData>? WidgetDataUpdated;

        private DashboardManager()
        {
            RegisterDataProvider(new TestExecutionDataProvider());
            CreateDefaultDashboard();
        }

        private void CreateDefaultDashboard()
        {
            var dashboard = new DashboardDefinition
            {
                Name = "Main Dashboard",
                IsDefault = true,
                Widgets = new List<DashboardWidget>
                {
                    new() { Title = "Tests Today", Type = WidgetType.Counter, Row = 0, Column = 0, DataSource = "TestExecution" },
                    new() { Title = "Pass Rate", Type = WidgetType.Gauge, Row = 0, Column = 1, DataSource = "TestExecution" },
                    new() { Title = "Trend", Type = WidgetType.Chart, ChartType = ChartType.Line, Row = 1, Column = 0, ColumnSpan = 2, DataSource = "TestExecution" }
                }
            };
            lock (_lock) { _dashboards[dashboard.Id] = dashboard; }
        }

        public void RegisterDataProvider(IDashboardDataProvider provider)
        {
            lock (_lock) { _dataProviders[provider.Name] = provider; }
        }

        public DashboardDefinition CreateDashboard(string name)
        {
            var dashboard = new DashboardDefinition { Name = name };
            lock (_lock) { _dashboards[dashboard.Id] = dashboard; }
            return dashboard;
        }

        public void AddWidget(string dashboardId, DashboardWidget widget)
        {
            lock (_lock)
            {
                if (_dashboards.TryGetValue(dashboardId, out var dashboard))
                {
                    dashboard.Widgets.Add(widget);
                }
            }
        }

        public WidgetData RefreshWidget(DashboardWidget widget)
        {
            lock (_lock)
            {
                if (_dataProviders.TryGetValue(widget.DataSource, out var provider))
                {
                    var data = provider.GetData(widget);
                    _widgetDataCache[widget.Id] = data;
                    WidgetDataUpdated?.Invoke(this, data);
                    return data;
                }
                return new WidgetData { WidgetId = widget.Id, Status = "No data provider" };
            }
        }

        public Dictionary<string, WidgetData> RefreshDashboard(string dashboardId)
        {
            var result = new Dictionary<string, WidgetData>();
            lock (_lock)
            {
                if (_dashboards.TryGetValue(dashboardId, out var dashboard))
                {
                    foreach (var widget in dashboard.Widgets)
                    {
                        result[widget.Id] = RefreshWidget(widget);
                    }
                }
            }
            return result;
        }

        public DashboardDefinition? GetDefaultDashboard()
        {
            lock (_lock) { return _dashboards.Values.FirstOrDefault(d => d.IsDefault); }
        }

        public List<DashboardDefinition> GetAllDashboards()
        {
            lock (_lock) { return _dashboards.Values.ToList(); }
        }
    }
}
