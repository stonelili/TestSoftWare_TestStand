// Phase 20: Thermal Management - Monitor and control thermal conditions
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.ThermalManagement
{
    /// <summary>
    /// Thermal zone status
    /// </summary>
    public enum ThermalStatus
    {
        Normal,
        Warning,
        Critical,
        Shutdown
    }

    /// <summary>
    /// Represents a thermal zone
    /// </summary>
    public class ThermalZone
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double CurrentTemperature { get; set; }
        public double TargetTemperature { get; set; }
        public double MinTemperature { get; set; } = -40;
        public double MaxTemperature { get; set; } = 125;
        public double WarningThreshold { get; set; } = 85;
        public double CriticalThreshold { get; set; } = 100;
        public ThermalStatus Status { get; set; } = ThermalStatus.Normal;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Thermal reading record
    /// </summary>
    public class ThermalReading
    {
        public string ZoneId { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public ThermalStatus Status { get; set; }
    }

    /// <summary>
    /// Thermal profile for temperature cycling
    /// </summary>
    public class ThermalProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public List<ThermalProfileStep> Steps { get; set; } = new();
        public int CycleCount { get; set; } = 1;
    }

    /// <summary>
    /// Thermal profile step
    /// </summary>
    public class ThermalProfileStep
    {
        public double TargetTemperature { get; set; }
        public TimeSpan RampDuration { get; set; }
        public TimeSpan SoakDuration { get; set; }
        public double Tolerance { get; set; } = 2.0;
    }

    /// <summary>
    /// Thermal alert
    /// </summary>
    public class ThermalAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ZoneId { get; set; } = string.Empty;
        public ThermalStatus Severity { get; set; }
        public double Temperature { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool Acknowledged { get; set; }
    }

    /// <summary>
    /// Manages thermal conditions
    /// </summary>
    public class ThermalManagementManager
    {
        private static readonly Lazy<ThermalManagementManager> _instance = new(() => new ThermalManagementManager());
        public static ThermalManagementManager Instance => _instance.Value;

        private readonly Dictionary<string, ThermalZone> _zones = new();
        private readonly Dictionary<string, ThermalProfile> _profiles = new();
        private readonly List<ThermalReading> _readings = new();
        private readonly List<ThermalAlert> _alerts = new();
        private readonly object _lock = new();

        public event EventHandler<ThermalAlert>? AlertRaised;

        private ThermalManagementManager() { }

        public void RegisterZone(ThermalZone zone)
        {
            lock (_lock)
            {
                _zones[zone.Id] = zone;
            }
        }

        public ThermalZone? GetZone(string zoneId)
        {
            lock (_lock)
            {
                return _zones.TryGetValue(zoneId, out var zone) ? zone : null;
            }
        }

        public List<ThermalZone> GetAllZones()
        {
            lock (_lock)
            {
                return _zones.Values.ToList();
            }
        }

        public void UpdateTemperature(string zoneId, double temperature)
        {
            lock (_lock)
            {
                if (_zones.TryGetValue(zoneId, out var zone))
                {
                    zone.CurrentTemperature = temperature;
                    zone.LastUpdated = DateTime.UtcNow;

                    var newStatus = DetermineStatus(zone, temperature);
                    if (newStatus != zone.Status && newStatus != ThermalStatus.Normal)
                    {
                        var alert = new ThermalAlert
                        {
                            ZoneId = zoneId,
                            Severity = newStatus,
                            Temperature = temperature,
                            Message = $"Temperature {temperature:F1}°C in zone {zone.Name}"
                        };
                        _alerts.Add(alert);
                        AlertRaised?.Invoke(this, alert);
                    }
                    zone.Status = newStatus;

                    _readings.Add(new ThermalReading
                    {
                        ZoneId = zoneId,
                        Temperature = temperature,
                        Status = newStatus
                    });
                }
            }
        }

        private ThermalStatus DetermineStatus(ThermalZone zone, double temperature)
        {
            if (temperature >= zone.CriticalThreshold || temperature <= zone.MinTemperature)
                return ThermalStatus.Critical;
            if (temperature >= zone.WarningThreshold)
                return ThermalStatus.Warning;
            return ThermalStatus.Normal;
        }

        public void RegisterProfile(ThermalProfile profile)
        {
            lock (_lock)
            {
                _profiles[profile.Id] = profile;
            }
        }

        public ThermalProfile? GetProfile(string profileId)
        {
            lock (_lock)
            {
                return _profiles.TryGetValue(profileId, out var profile) ? profile : null;
            }
        }

        public List<ThermalReading> GetReadingHistory(string zoneId, TimeSpan period)
        {
            lock (_lock)
            {
                var cutoff = DateTime.UtcNow - period;
                return _readings.Where(r => r.ZoneId == zoneId && r.Timestamp >= cutoff).ToList();
            }
        }

        public List<ThermalAlert> GetActiveAlerts()
        {
            lock (_lock)
            {
                return _alerts.Where(a => !a.Acknowledged).ToList();
            }
        }

        public void AcknowledgeAlert(string alertId)
        {
            lock (_lock)
            {
                var alert = _alerts.FirstOrDefault(a => a.Id == alertId);
                if (alert != null) alert.Acknowledged = true;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _zones.Clear();
                _profiles.Clear();
                _readings.Clear();
                _alerts.Clear();
            }
        }
    }
}
