// Phase 20: OEE Tracking - Overall Equipment Effectiveness tracking
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.OEETracking
{
    /// <summary>
    /// Equipment state enumeration
    /// </summary>
    public enum EquipmentState
    {
        Running,
        Idle,
        PlannedDowntime,
        UnplannedDowntime,
        Setup,
        Changeover
    }

    /// <summary>
    /// Represents an equipment state record
    /// </summary>
    public class StateRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EquipmentId { get; set; } = string.Empty;
        public EquipmentState State { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Reason { get; set; } = string.Empty;
        public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
    }

    /// <summary>
    /// Represents production count data
    /// </summary>
    public class ProductionCount
    {
        public string EquipmentId { get; set; } = string.Empty;
        public DateTime Period { get; set; }
        public int TotalCount { get; set; }
        public int GoodCount { get; set; }
        public int DefectCount { get; set; }
        public int RetestCount { get; set; }
    }

    /// <summary>
    /// OEE calculation result
    /// </summary>
    public class OEEResult
    {
        public string EquipmentId { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public double Availability { get; set; }
        public double Performance { get; set; }
        public double Quality { get; set; }
        public double OEE { get; set; }
        public TimeSpan PlannedProductionTime { get; set; }
        public TimeSpan ActualProductionTime { get; set; }
        public TimeSpan DownTime { get; set; }
        public int TotalUnits { get; set; }
        public int GoodUnits { get; set; }
        public int DefectUnits { get; set; }
    }

    /// <summary>
    /// Downtime reason category
    /// </summary>
    public class DowntimeCategory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsPlanned { get; set; }
        public string ParentId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Manages OEE tracking
    /// </summary>
    public class OEETrackingManager
    {
        private static readonly Lazy<OEETrackingManager> _instance = new(() => new OEETrackingManager());
        public static OEETrackingManager Instance => _instance.Value;

        private readonly Dictionary<string, List<StateRecord>> _stateRecords = new();
        private readonly Dictionary<string, List<ProductionCount>> _productionCounts = new();
        private readonly Dictionary<string, DowntimeCategory> _downtimeCategories = new();
        private readonly Dictionary<string, double> _idealCycleTimes = new();
        private readonly object _lock = new();

        private OEETrackingManager() { }

        public void SetIdealCycleTime(string equipmentId, double seconds)
        {
            lock (_lock)
            {
                _idealCycleTimes[equipmentId] = seconds;
            }
        }

        public void RecordStateChange(string equipmentId, EquipmentState state, string reason = "")
        {
            lock (_lock)
            {
                if (!_stateRecords.ContainsKey(equipmentId))
                    _stateRecords[equipmentId] = new List<StateRecord>();

                var records = _stateRecords[equipmentId];
                var currentRecord = records.LastOrDefault(r => r.EndTime == null);
                if (currentRecord != null)
                    currentRecord.EndTime = DateTime.UtcNow;

                records.Add(new StateRecord
                {
                    EquipmentId = equipmentId,
                    State = state,
                    StartTime = DateTime.UtcNow,
                    Reason = reason
                });
            }
        }

        public void RecordProduction(string equipmentId, int total, int good, int defect)
        {
            lock (_lock)
            {
                if (!_productionCounts.ContainsKey(equipmentId))
                    _productionCounts[equipmentId] = new List<ProductionCount>();

                _productionCounts[equipmentId].Add(new ProductionCount
                {
                    EquipmentId = equipmentId,
                    Period = DateTime.UtcNow,
                    TotalCount = total,
                    GoodCount = good,
                    DefectCount = defect
                });
            }
        }

        public OEEResult CalculateOEE(string equipmentId, DateTime periodStart, DateTime periodEnd)
        {
            lock (_lock)
            {
                var result = new OEEResult
                {
                    EquipmentId = equipmentId,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd
                };

                var plannedTime = periodEnd - periodStart;
                result.PlannedProductionTime = plannedTime;

                // Calculate availability
                var records = _stateRecords.GetValueOrDefault(equipmentId, new List<StateRecord>());
                var downtimeRecords = records.Where(r =>
                    r.State == EquipmentState.UnplannedDowntime &&
                    r.StartTime >= periodStart && r.StartTime <= periodEnd).ToList();

                var downtime = TimeSpan.Zero;
                foreach (var record in downtimeRecords)
                {
                    var end = record.EndTime ?? periodEnd;
                    downtime += end - record.StartTime;
                }
                result.DownTime = downtime;
                result.ActualProductionTime = plannedTime - downtime;
                result.Availability = plannedTime.TotalMinutes > 0
                    ? result.ActualProductionTime.TotalMinutes / plannedTime.TotalMinutes * 100
                    : 0;

                // Calculate performance and quality from production counts
                var counts = _productionCounts.GetValueOrDefault(equipmentId, new List<ProductionCount>());
                var periodCounts = counts.Where(c => c.Period >= periodStart && c.Period <= periodEnd).ToList();

                result.TotalUnits = periodCounts.Sum(c => c.TotalCount);
                result.GoodUnits = periodCounts.Sum(c => c.GoodCount);
                result.DefectUnits = periodCounts.Sum(c => c.DefectCount);

                var idealCycleTime = _idealCycleTimes.GetValueOrDefault(equipmentId, 60);
                var idealOutput = result.ActualProductionTime.TotalSeconds / idealCycleTime;
                result.Performance = idealOutput > 0 ? (result.TotalUnits / idealOutput) * 100 : 0;
                result.Quality = result.TotalUnits > 0 ? ((double)result.GoodUnits / result.TotalUnits) * 100 : 0;

                result.OEE = (result.Availability / 100) * (result.Performance / 100) * (result.Quality / 100) * 100;

                return result;
            }
        }

        public void RegisterDowntimeCategory(DowntimeCategory category)
        {
            lock (_lock)
            {
                _downtimeCategories[category.Id] = category;
            }
        }

        public List<DowntimeCategory> GetDowntimeCategories()
        {
            lock (_lock)
            {
                return _downtimeCategories.Values.ToList();
            }
        }

        public Dictionary<string, TimeSpan> GetDowntimeBreakdown(string equipmentId, DateTime periodStart, DateTime periodEnd)
        {
            lock (_lock)
            {
                var breakdown = new Dictionary<string, TimeSpan>();
                var records = _stateRecords.GetValueOrDefault(equipmentId, new List<StateRecord>());

                foreach (var record in records.Where(r =>
                    r.State == EquipmentState.UnplannedDowntime &&
                    r.StartTime >= periodStart && r.StartTime <= periodEnd))
                {
                    var reason = string.IsNullOrEmpty(record.Reason) ? "Unknown" : record.Reason;
                    if (!breakdown.ContainsKey(reason))
                        breakdown[reason] = TimeSpan.Zero;
                    breakdown[reason] += record.Duration;
                }

                return breakdown;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _stateRecords.Clear();
                _productionCounts.Clear();
                _downtimeCategories.Clear();
                _idealCycleTimes.Clear();
            }
        }
    }
}
