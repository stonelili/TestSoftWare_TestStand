using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.GoldenUnitTesting
{
    /// <summary>
    /// Golden unit status
    /// </summary>
    public enum GoldenUnitStatus
    {
        Active,
        Expired,
        Retired,
        Pending
    }

    /// <summary>
    /// Golden unit definition
    /// </summary>
    public class GoldenUnit
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SerialNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public GoldenUnitStatus Status { get; set; } = GoldenUnitStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpirationDate { get; set; }
        public string ProductFamily { get; set; } = string.Empty;
        public Dictionary<string, ExpectedResult> ExpectedResults { get; set; } = new Dictionary<string, ExpectedResult>();
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public int UsageCount { get; set; }
        public DateTime? LastUsedAt { get; set; }
    }

    /// <summary>
    /// Expected result for a step
    /// </summary>
    public class ExpectedResult
    {
        public string StepName { get; set; } = string.Empty;
        public string ExpectedStatus { get; set; } = "Passed";
        public double? ExpectedValue { get; set; }
        public double Tolerance { get; set; } = 0.01;
        public double? LowLimit { get; set; }
        public double? HighLimit { get; set; }
        public string? ExpectedString { get; set; }
        public bool IsRequired { get; set; } = true;
    }

    /// <summary>
    /// Golden unit test result
    /// </summary>
    public class GoldenUnitTestResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string GoldenUnitId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool OverallPass { get; set; }
        public List<StepComparisonResult> StepResults { get; set; } = new List<StepComparisonResult>();
        public string? Notes { get; set; }
        public string TestedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Step comparison result
    /// </summary>
    public class StepComparisonResult
    {
        public string StepName { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string ExpectedStatus { get; set; } = string.Empty;
        public string ActualStatus { get; set; } = string.Empty;
        public double? ExpectedValue { get; set; }
        public double? ActualValue { get; set; }
        public double? Deviation { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Golden unit verification schedule
    /// </summary>
    public class VerificationSchedule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string GoldenUnitId { get; set; } = string.Empty;
        public TimeSpan Interval { get; set; } = TimeSpan.FromDays(7);
        public DateTime? LastVerification { get; set; }
        public DateTime? NextVerification { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string? SequenceFile { get; set; }
    }

    /// <summary>
    /// Golden unit manager singleton
    /// </summary>
    public class GoldenUnitManager
    {
        private static readonly Lazy<GoldenUnitManager> _instance = new Lazy<GoldenUnitManager>(() => new GoldenUnitManager());
        public static GoldenUnitManager Instance => _instance.Value;

        private readonly List<GoldenUnit> _goldenUnits = new List<GoldenUnit>();
        private readonly List<GoldenUnitTestResult> _testResults = new List<GoldenUnitTestResult>();
        private readonly List<VerificationSchedule> _schedules = new List<VerificationSchedule>();
        private readonly object _lock = new object();

        public event EventHandler<GoldenUnitTestResult>? TestCompleted;
        public event EventHandler<GoldenUnit>? VerificationDue;

        private GoldenUnitManager() { }

        public void RegisterGoldenUnit(GoldenUnit unit)
        {
            lock (_lock)
            {
                _goldenUnits.RemoveAll(u => u.Id == unit.Id);
                _goldenUnits.Add(unit);
            }
        }

        public GoldenUnit? GetGoldenUnit(string serialNumber)
        {
            lock (_lock)
            {
                return _goldenUnits.FirstOrDefault(u => u.SerialNumber == serialNumber && u.Status == GoldenUnitStatus.Active);
            }
        }

        public List<GoldenUnit> GetAllGoldenUnits(GoldenUnitStatus? status = null)
        {
            lock (_lock)
            {
                var query = _goldenUnits.AsEnumerable();
                if (status.HasValue)
                    query = query.Where(u => u.Status == status.Value);
                return query.ToList();
            }
        }

        public GoldenUnitTestResult RunVerification(GoldenUnit unit, Dictionary<string, object> actualResults)
        {
            lock (_lock)
            {
                var result = new GoldenUnitTestResult
                {
                    GoldenUnitId = unit.Id,
                    OverallPass = true
                };

                foreach (var expected in unit.ExpectedResults)
                {
                    var comparison = CompareResult(expected.Value, actualResults);
                    result.StepResults.Add(comparison);

                    if (!comparison.Passed && expected.Value.IsRequired)
                        result.OverallPass = false;
                }

                unit.UsageCount++;
                unit.LastUsedAt = DateTime.UtcNow;
                _testResults.Add(result);

                TestCompleted?.Invoke(this, result);
                return result;
            }
        }

        private StepComparisonResult CompareResult(ExpectedResult expected, Dictionary<string, object> actualResults)
        {
            var comparison = new StepComparisonResult
            {
                StepName = expected.StepName,
                ExpectedStatus = expected.ExpectedStatus
            };

            if (actualResults.TryGetValue($"{expected.StepName}.Status", out var statusObj))
            {
                comparison.ActualStatus = statusObj?.ToString() ?? "";
            }

            if (actualResults.TryGetValue($"{expected.StepName}.Value", out var valueObj) && valueObj is double actualValue)
            {
                comparison.ActualValue = actualValue;
                comparison.ExpectedValue = expected.ExpectedValue;

                if (expected.ExpectedValue.HasValue)
                {
                    comparison.Deviation = Math.Abs(actualValue - expected.ExpectedValue.Value);
                    comparison.Passed = comparison.Deviation <= expected.Tolerance;
                }
                else if (expected.LowLimit.HasValue && expected.HighLimit.HasValue)
                {
                    comparison.Passed = actualValue >= expected.LowLimit.Value && actualValue <= expected.HighLimit.Value;
                }
            }
            else
            {
                comparison.Passed = comparison.ActualStatus.Equals(expected.ExpectedStatus, StringComparison.OrdinalIgnoreCase);
            }

            return comparison;
        }

        public void SetVerificationSchedule(VerificationSchedule schedule)
        {
            lock (_lock)
            {
                _schedules.RemoveAll(s => s.GoldenUnitId == schedule.GoldenUnitId);
                schedule.NextVerification = DateTime.UtcNow.Add(schedule.Interval);
                _schedules.Add(schedule);
            }
        }

        public List<GoldenUnit> GetUnitsRequiringVerification()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                return _schedules
                    .Where(s => s.IsEnabled && s.NextVerification.HasValue && s.NextVerification.Value <= now)
                    .Select(s => _goldenUnits.FirstOrDefault(u => u.Id == s.GoldenUnitId))
                    .Where(u => u != null && u.Status == GoldenUnitStatus.Active)
                    .Cast<GoldenUnit>()
                    .ToList();
            }
        }

        public void CheckSchedules()
        {
            foreach (var unit in GetUnitsRequiringVerification())
            {
                VerificationDue?.Invoke(this, unit);
            }
        }

        public List<GoldenUnitTestResult> GetTestHistory(string goldenUnitId, int count = 10)
        {
            lock (_lock)
            {
                return _testResults
                    .Where(r => r.GoldenUnitId == goldenUnitId)
                    .OrderByDescending(r => r.Timestamp)
                    .Take(count)
                    .ToList();
            }
        }

        public void RetireUnit(string goldenUnitId)
        {
            lock (_lock)
            {
                var unit = _goldenUnits.FirstOrDefault(u => u.Id == goldenUnitId);
                if (unit != null)
                    unit.Status = GoldenUnitStatus.Retired;
            }
        }
    }
}
