using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.EnvironmentalMonitoring
{
    public enum EnvironmentParameterType { Temperature, Humidity, Pressure, Vibration, Voltage, Current, Power }
    public enum EnvironmentAlertLevel { Normal, Warning, Critical }

    public class EnvironmentReading
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SensorId { get; set; } = string.Empty;
        public EnvironmentParameterType ParameterType { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public EnvironmentAlertLevel AlertLevel { get; set; } = EnvironmentAlertLevel.Normal;
    }

    public class EnvironmentSensor
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public EnvironmentParameterType ParameterType { get; set; }
        public string Unit { get; set; } = string.Empty;
        public double MinWarning { get; set; }
        public double MaxWarning { get; set; }
        public double MinCritical { get; set; }
        public double MaxCritical { get; set; }
        public double CurrentValue { get; set; }
        public DateTime LastReading { get; set; } = DateTime.Now;
        public bool IsOnline { get; set; } = true;
    }

    public class EnvironmentAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SensorId { get; set; } = string.Empty;
        public string SensorName { get; set; } = string.Empty;
        public EnvironmentParameterType ParameterType { get; set; }
        public double Value { get; set; }
        public EnvironmentAlertLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime TriggeredAt { get; set; } = DateTime.Now;
        public bool IsAcknowledged { get; set; }
        public string? AcknowledgedBy { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
    }

    public class EnvironmentStatistics
    {
        public string SensorId { get; set; } = string.Empty;
        public EnvironmentParameterType ParameterType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double AverageValue { get; set; }
        public int ReadingCount { get; set; }
        public int WarningCount { get; set; }
        public int CriticalCount { get; set; }
    }

    public class EnvironmentalMonitoringManager
    {
        private static readonly Lazy<EnvironmentalMonitoringManager> _instance = new Lazy<EnvironmentalMonitoringManager>(() => new EnvironmentalMonitoringManager());
        public static EnvironmentalMonitoringManager Instance => _instance.Value;

        private readonly Dictionary<string, EnvironmentSensor> _sensors = new Dictionary<string, EnvironmentSensor>();
        private readonly List<EnvironmentReading> _readings = new List<EnvironmentReading>();
        private readonly List<EnvironmentAlert> _alerts = new List<EnvironmentAlert>();
        private const int MaxReadingsToStore = 10000;

        public event EventHandler<EnvironmentReading>? ReadingReceived;
        public event EventHandler<EnvironmentAlert>? AlertTriggered;

        private EnvironmentalMonitoringManager() { }

        public EnvironmentSensor RegisterSensor(string name, string location, EnvironmentParameterType type, string unit, double minWarning, double maxWarning, double minCritical, double maxCritical)
        {
            var sensor = new EnvironmentSensor { Name = name, Location = location, ParameterType = type, Unit = unit, MinWarning = minWarning, MaxWarning = maxWarning, MinCritical = minCritical, MaxCritical = maxCritical };
            _sensors[sensor.Id] = sensor;
            return sensor;
        }

        public void UnregisterSensor(string sensorId) => _sensors.Remove(sensorId);
        public EnvironmentSensor? GetSensor(string id) { _sensors.TryGetValue(id, out var s); return s; }
        public IEnumerable<EnvironmentSensor> GetAllSensors() => _sensors.Values.ToList();
        public IEnumerable<EnvironmentSensor> GetSensorsByType(EnvironmentParameterType type) => _sensors.Values.Where(s => s.ParameterType == type).ToList();

        public void RecordReading(string sensorId, double value)
        {
            if (!_sensors.TryGetValue(sensorId, out var sensor)) return;

            var alertLevel = EnvironmentAlertLevel.Normal;
            if (value <= sensor.MinCritical || value >= sensor.MaxCritical) alertLevel = EnvironmentAlertLevel.Critical;
            else if (value <= sensor.MinWarning || value >= sensor.MaxWarning) alertLevel = EnvironmentAlertLevel.Warning;

            var reading = new EnvironmentReading { SensorId = sensorId, ParameterType = sensor.ParameterType, Value = value, Unit = sensor.Unit, AlertLevel = alertLevel };
            _readings.Add(reading);
            if (_readings.Count > MaxReadingsToStore) _readings.RemoveRange(0, _readings.Count - MaxReadingsToStore);

            sensor.CurrentValue = value;
            sensor.LastReading = DateTime.Now;
            ReadingReceived?.Invoke(this, reading);

            if (alertLevel != EnvironmentAlertLevel.Normal)
            {
                var alert = new EnvironmentAlert { SensorId = sensorId, SensorName = sensor.Name, ParameterType = sensor.ParameterType, Value = value, Level = alertLevel, Message = $"{sensor.Name} is {alertLevel}: {value} {sensor.Unit}" };
                _alerts.Add(alert);
                AlertTriggered?.Invoke(this, alert);
            }
        }

        public IEnumerable<EnvironmentReading> GetReadings(string? sensorId = null, DateTime? since = null, int limit = 100)
        {
            var q = _readings.AsEnumerable();
            if (sensorId != null) q = q.Where(r => r.SensorId == sensorId);
            if (since.HasValue) q = q.Where(r => r.Timestamp >= since.Value);
            return q.OrderByDescending(r => r.Timestamp).Take(limit).ToList();
        }

        public IEnumerable<EnvironmentAlert> GetAlerts(bool unacknowledgedOnly = false, EnvironmentAlertLevel? level = null)
        {
            var q = _alerts.AsEnumerable();
            if (unacknowledgedOnly) q = q.Where(a => !a.IsAcknowledged);
            if (level.HasValue) q = q.Where(a => a.Level == level.Value);
            return q.OrderByDescending(a => a.TriggeredAt).ToList();
        }

        public void AcknowledgeAlert(string alertId, string acknowledgedBy)
        {
            var alert = _alerts.FirstOrDefault(a => a.Id == alertId);
            if (alert != null) { alert.IsAcknowledged = true; alert.AcknowledgedBy = acknowledgedBy; alert.AcknowledgedAt = DateTime.Now; }
        }

        public EnvironmentStatistics GetStatistics(string sensorId, DateTime startTime, DateTime endTime)
        {
            var readings = _readings.Where(r => r.SensorId == sensorId && r.Timestamp >= startTime && r.Timestamp <= endTime).ToList();
            if (readings.Count == 0) return new EnvironmentStatistics { SensorId = sensorId, StartTime = startTime, EndTime = endTime };
            return new EnvironmentStatistics
            {
                SensorId = sensorId,
                ParameterType = readings.First().ParameterType,
                StartTime = startTime,
                EndTime = endTime,
                MinValue = readings.Min(r => r.Value),
                MaxValue = readings.Max(r => r.Value),
                AverageValue = readings.Average(r => r.Value),
                ReadingCount = readings.Count,
                WarningCount = readings.Count(r => r.AlertLevel == EnvironmentAlertLevel.Warning),
                CriticalCount = readings.Count(r => r.AlertLevel == EnvironmentAlertLevel.Critical)
            };
        }

        public Dictionary<string, double> GetCurrentReadings()
        {
            return _sensors.Values.ToDictionary(s => s.Name, s => s.CurrentValue);
        }

        public void SetSensorOnlineStatus(string sensorId, bool isOnline)
        {
            if (_sensors.TryGetValue(sensorId, out var sensor)) sensor.IsOnline = isOnline;
        }
    }
}
