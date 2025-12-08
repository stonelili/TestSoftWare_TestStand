// Phase 20: Traceability System - Full product and test traceability
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.TraceabilitySystem
{
    /// <summary>
    /// Traceability event type
    /// </summary>
    public enum TraceabilityEventType
    {
        Created,
        Modified,
        Tested,
        Passed,
        Failed,
        Reworked,
        Shipped,
        Returned,
        Scrapped
    }

    /// <summary>
    /// Represents a traceable unit
    /// </summary>
    public class TraceableUnit
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string LotNumber { get; set; } = string.Empty;
        public string WorkOrder { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Attributes { get; set; } = new();
        public List<ComponentLink> Components { get; set; } = new();
    }

    /// <summary>
    /// Component link for bill of materials tracking
    /// </summary>
    public class ComponentLink
    {
        public string ComponentSerialNumber { get; set; } = string.Empty;
        public string ComponentType { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Traceability event record
    /// </summary>
    public class TraceabilityEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SerialNumber { get; set; } = string.Empty;
        public TraceabilityEventType EventType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Station { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// Test result record for traceability
    /// </summary>
    public class TraceableTestResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SerialNumber { get; set; } = string.Empty;
        public string TestSequence { get; set; } = string.Empty;
        public string TestStation { get; set; } = string.Empty;
        public DateTime TestTime { get; set; } = DateTime.UtcNow;
        public bool Passed { get; set; }
        public TimeSpan Duration { get; set; }
        public List<TraceableStepResult> StepResults { get; set; } = new();
    }

    /// <summary>
    /// Step result for traceability
    /// </summary>
    public class TraceableStepResult
    {
        public string StepName { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public object? MeasuredValue { get; set; }
        public object? LowLimit { get; set; }
        public object? HighLimit { get; set; }
        public string Units { get; set; } = string.Empty;
    }

    /// <summary>
    /// Genealogy record showing parent-child relationships
    /// </summary>
    public class GenealogyRecord
    {
        public string ParentSerialNumber { get; set; } = string.Empty;
        public string ChildSerialNumber { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Manages traceability
    /// </summary>
    public class TraceabilityManager
    {
        private static readonly Lazy<TraceabilityManager> _instance = new(() => new TraceabilityManager());
        public static TraceabilityManager Instance => _instance.Value;

        private readonly Dictionary<string, TraceableUnit> _units = new();
        private readonly List<TraceabilityEvent> _events = new();
        private readonly List<TraceableTestResult> _testResults = new();
        private readonly List<GenealogyRecord> _genealogy = new();
        private readonly object _lock = new();

        private TraceabilityManager() { }

        public void RegisterUnit(TraceableUnit unit)
        {
            lock (_lock)
            {
                _units[unit.SerialNumber] = unit;
                RecordEvent(unit.SerialNumber, TraceabilityEventType.Created, "Unit registered");
            }
        }

        public TraceableUnit? GetUnit(string serialNumber)
        {
            lock (_lock)
            {
                return _units.TryGetValue(serialNumber, out var unit) ? unit : null;
            }
        }

        public void RecordEvent(string serialNumber, TraceabilityEventType eventType, string description, Dictionary<string, object>? data = null)
        {
            lock (_lock)
            {
                _events.Add(new TraceabilityEvent
                {
                    SerialNumber = serialNumber,
                    EventType = eventType,
                    Description = description,
                    Data = data ?? new Dictionary<string, object>()
                });
            }
        }

        public List<TraceabilityEvent> GetEventHistory(string serialNumber)
        {
            lock (_lock)
            {
                return _events.Where(e => e.SerialNumber == serialNumber).OrderBy(e => e.Timestamp).ToList();
            }
        }

        public void RecordTestResult(TraceableTestResult result)
        {
            lock (_lock)
            {
                _testResults.Add(result);
                RecordEvent(result.SerialNumber,
                    result.Passed ? TraceabilityEventType.Passed : TraceabilityEventType.Failed,
                    $"Test {result.TestSequence} {(result.Passed ? "passed" : "failed")}");
            }
        }

        public List<TraceableTestResult> GetTestHistory(string serialNumber)
        {
            lock (_lock)
            {
                return _testResults.Where(r => r.SerialNumber == serialNumber).OrderBy(r => r.TestTime).ToList();
            }
        }

        public void LinkComponent(string parentSerial, string childSerial, string componentType, string position)
        {
            lock (_lock)
            {
                if (_units.TryGetValue(parentSerial, out var parent))
                {
                    parent.Components.Add(new ComponentLink
                    {
                        ComponentSerialNumber = childSerial,
                        ComponentType = componentType,
                        Position = position
                    });
                }

                _genealogy.Add(new GenealogyRecord
                {
                    ParentSerialNumber = parentSerial,
                    ChildSerialNumber = childSerial,
                    Relationship = componentType
                });
            }
        }

        public List<string> GetComponentSerialNumbers(string parentSerial)
        {
            lock (_lock)
            {
                return _genealogy.Where(g => g.ParentSerialNumber == parentSerial).Select(g => g.ChildSerialNumber).ToList();
            }
        }

        public List<string> GetParentSerialNumbers(string childSerial)
        {
            lock (_lock)
            {
                return _genealogy.Where(g => g.ChildSerialNumber == childSerial).Select(g => g.ParentSerialNumber).ToList();
            }
        }

        public List<TraceableUnit> QueryUnits(string? productId = null, string? lotNumber = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            lock (_lock)
            {
                var query = _units.Values.AsEnumerable();
                if (!string.IsNullOrEmpty(productId))
                    query = query.Where(u => u.ProductId == productId);
                if (!string.IsNullOrEmpty(lotNumber))
                    query = query.Where(u => u.LotNumber == lotNumber);
                if (fromDate.HasValue)
                    query = query.Where(u => u.CreatedAt >= fromDate.Value);
                if (toDate.HasValue)
                    query = query.Where(u => u.CreatedAt <= toDate.Value);
                return query.ToList();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _units.Clear();
                _events.Clear();
                _testResults.Clear();
                _genealogy.Clear();
            }
        }
    }
}
