using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestStandClone.Core.FixtureManagement
{
    /// <summary>
    /// Fixture status enumeration
    /// </summary>
    public enum FixtureStatus
    {
        Ready,
        InUse,
        NeedsCalibration,
        NeedsMaintenance,
        Error,
        Offline
    }

    /// <summary>
    /// Fixture type enumeration
    /// </summary>
    public enum FixtureType
    {
        TestFixture,
        LoadingFixture,
        UnloadingFixture,
        CalibrationFixture,
        ReferenceFixture,
        Custom
    }

    /// <summary>
    /// Represents a test fixture
    /// </summary>
    public class Fixture
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public FixtureType Type { get; set; }
        public FixtureStatus Status { get; set; } = FixtureStatus.Ready;
        public string Location { get; set; } = string.Empty;
        public DateTime? LastCalibrationDate { get; set; }
        public DateTime? NextCalibrationDate { get; set; }
        public int UsageCount { get; set; }
        public int MaxUsageBeforeMaintenance { get; set; } = 10000;
        public Dictionary<string, object> Properties { get; set; } = new();
        public List<string> SupportedProducts { get; set; } = new();
        public string CurrentOperator { get; set; } = string.Empty;
    }

    /// <summary>
    /// Fixture usage record
    /// </summary>
    public class FixtureUsageRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FixtureId { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Result { get; set; } = string.Empty;
        public int UnitsProcessed { get; set; }
    }

    /// <summary>
    /// Fixture maintenance record
    /// </summary>
    public class FixtureMaintenanceRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FixtureId { get; set; } = string.Empty;
        public DateTime MaintenanceDate { get; set; }
        public string MaintenanceType { get; set; } = string.Empty;
        public string TechnicianId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public Dictionary<string, object> Results { get; set; } = new();
    }

    /// <summary>
    /// Fixture IO operation
    /// </summary>
    public class FixtureIOOperation
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Func<Fixture, Task<bool>>? Execute { get; set; }
    }

    /// <summary>
    /// Fixture manager for managing test fixtures
    /// </summary>
    public class FixtureManager
    {
        private static FixtureManager? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, Fixture> _fixtures = new();
        private readonly List<FixtureUsageRecord> _usageRecords = new();
        private readonly List<FixtureMaintenanceRecord> _maintenanceRecords = new();
        private readonly Dictionary<string, List<FixtureIOOperation>> _fixtureOperations = new();

        public static FixtureManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new FixtureManager();
                    }
                }
                return _instance;
            }
        }

        public event EventHandler<Fixture>? FixtureStatusChanged;
        public event EventHandler<Fixture>? FixtureCalibrationDue;
        public event EventHandler<Fixture>? FixtureMaintenanceDue;

        /// <summary>
        /// Register a fixture
        /// </summary>
        public void RegisterFixture(Fixture fixture)
        {
            lock (_lock)
            {
                _fixtures[fixture.Id] = fixture;
            }
        }

        /// <summary>
        /// Get a fixture by ID
        /// </summary>
        public Fixture? GetFixture(string fixtureId)
        {
            _fixtures.TryGetValue(fixtureId, out var fixture);
            return fixture;
        }

        /// <summary>
        /// Get all fixtures
        /// </summary>
        public IReadOnlyList<Fixture> GetAllFixtures()
        {
            lock (_lock)
            {
                return _fixtures.Values.ToList();
            }
        }

        /// <summary>
        /// Get fixtures by type
        /// </summary>
        public IReadOnlyList<Fixture> GetFixturesByType(FixtureType type)
        {
            lock (_lock)
            {
                return _fixtures.Values.Where(f => f.Type == type).ToList();
            }
        }

        /// <summary>
        /// Get available fixtures for a product
        /// </summary>
        public IReadOnlyList<Fixture> GetAvailableFixturesForProduct(string productId)
        {
            lock (_lock)
            {
                return _fixtures.Values
                    .Where(f => f.Status == FixtureStatus.Ready && 
                               (f.SupportedProducts.Count == 0 || f.SupportedProducts.Contains(productId)))
                    .ToList();
            }
        }

        /// <summary>
        /// Start using a fixture
        /// </summary>
        public FixtureUsageRecord StartFixtureUsage(string fixtureId, string operatorId, string productId)
        {
            if (!_fixtures.TryGetValue(fixtureId, out var fixture))
            {
                throw new InvalidOperationException($"Fixture {fixtureId} not found");
            }

            fixture.Status = FixtureStatus.InUse;
            fixture.CurrentOperator = operatorId;

            var record = new FixtureUsageRecord
            {
                FixtureId = fixtureId,
                OperatorId = operatorId,
                ProductId = productId,
                StartTime = DateTime.Now
            };

            lock (_lock)
            {
                _usageRecords.Add(record);
            }

            FixtureStatusChanged?.Invoke(this, fixture);
            return record;
        }

        /// <summary>
        /// End fixture usage
        /// </summary>
        public void EndFixtureUsage(string recordId, string result, int unitsProcessed)
        {
            lock (_lock)
            {
                var record = _usageRecords.FirstOrDefault(r => r.Id == recordId);
                if (record != null)
                {
                    record.EndTime = DateTime.Now;
                    record.Result = result;
                    record.UnitsProcessed = unitsProcessed;

                    if (_fixtures.TryGetValue(record.FixtureId, out var fixture))
                    {
                        fixture.Status = FixtureStatus.Ready;
                        fixture.CurrentOperator = string.Empty;
                        fixture.UsageCount += unitsProcessed;

                        if (fixture.UsageCount >= fixture.MaxUsageBeforeMaintenance)
                        {
                            fixture.Status = FixtureStatus.NeedsMaintenance;
                            FixtureMaintenanceDue?.Invoke(this, fixture);
                        }

                        if (fixture.NextCalibrationDate.HasValue && DateTime.Now >= fixture.NextCalibrationDate.Value)
                        {
                            fixture.Status = FixtureStatus.NeedsCalibration;
                            FixtureCalibrationDue?.Invoke(this, fixture);
                        }

                        FixtureStatusChanged?.Invoke(this, fixture);
                    }
                }
            }
        }

        /// <summary>
        /// Record fixture maintenance
        /// </summary>
        public FixtureMaintenanceRecord RecordMaintenance(string fixtureId, string maintenanceType, 
            string technicianId, string description, bool passed)
        {
            var record = new FixtureMaintenanceRecord
            {
                FixtureId = fixtureId,
                MaintenanceDate = DateTime.Now,
                MaintenanceType = maintenanceType,
                TechnicianId = technicianId,
                Description = description,
                Passed = passed
            };

            lock (_lock)
            {
                _maintenanceRecords.Add(record);

                if (_fixtures.TryGetValue(fixtureId, out var fixture))
                {
                    if (passed)
                    {
                        fixture.Status = FixtureStatus.Ready;
                        fixture.UsageCount = 0;
                    }
                    else
                    {
                        fixture.Status = FixtureStatus.Error;
                    }
                    FixtureStatusChanged?.Invoke(this, fixture);
                }
            }

            return record;
        }

        /// <summary>
        /// Record fixture calibration
        /// </summary>
        public void RecordCalibration(string fixtureId, DateTime calibrationDate, DateTime nextCalibrationDate, bool passed)
        {
            if (_fixtures.TryGetValue(fixtureId, out var fixture))
            {
                lock (_lock)
                {
                    fixture.LastCalibrationDate = calibrationDate;
                    fixture.NextCalibrationDate = nextCalibrationDate;
                    fixture.Status = passed ? FixtureStatus.Ready : FixtureStatus.NeedsCalibration;
                }
                FixtureStatusChanged?.Invoke(this, fixture);
            }
        }

        /// <summary>
        /// Register fixture IO operations
        /// </summary>
        public void RegisterFixtureOperations(string fixtureId, IEnumerable<FixtureIOOperation> operations)
        {
            lock (_lock)
            {
                _fixtureOperations[fixtureId] = operations.ToList();
            }
        }

        /// <summary>
        /// Execute a fixture operation
        /// </summary>
        public async Task<bool> ExecuteFixtureOperationAsync(string fixtureId, string operationName)
        {
            if (!_fixtureOperations.TryGetValue(fixtureId, out var operations))
            {
                return false;
            }

            var operation = operations.FirstOrDefault(o => o.Name == operationName);
            if (operation?.Execute == null)
            {
                return false;
            }

            var fixture = GetFixture(fixtureId);
            if (fixture == null)
            {
                return false;
            }

            return await operation.Execute(fixture);
        }

        /// <summary>
        /// Get usage history for a fixture
        /// </summary>
        public IReadOnlyList<FixtureUsageRecord> GetUsageHistory(string fixtureId, DateTime? startDate = null, DateTime? endDate = null)
        {
            lock (_lock)
            {
                var query = _usageRecords.Where(r => r.FixtureId == fixtureId);
                
                if (startDate.HasValue)
                    query = query.Where(r => r.StartTime >= startDate.Value);
                
                if (endDate.HasValue)
                    query = query.Where(r => r.StartTime <= endDate.Value);
                
                return query.ToList();
            }
        }

        /// <summary>
        /// Get maintenance history for a fixture
        /// </summary>
        public IReadOnlyList<FixtureMaintenanceRecord> GetMaintenanceHistory(string fixtureId)
        {
            lock (_lock)
            {
                return _maintenanceRecords.Where(r => r.FixtureId == fixtureId).ToList();
            }
        }
    }
}
