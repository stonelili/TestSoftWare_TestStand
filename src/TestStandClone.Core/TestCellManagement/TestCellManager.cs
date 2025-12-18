// Phase 20: Test Cell Management - Manage test cell configurations and execution
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.TestCellManagement
{
    /// <summary>
    /// Test cell status enumeration
    /// </summary>
    public enum TestCellStatus
    {
        Offline,
        Idle,
        Running,
        Paused,
        Error,
        Maintenance
    }

    /// <summary>
    /// Test cell type enumeration
    /// </summary>
    public enum TestCellType
    {
        SingleSocket,
        MultiSocket,
        Parallel,
        Handler
    }

    /// <summary>
    /// Represents a test cell configuration
    /// </summary>
    public class TestCell
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TestCellType Type { get; set; } = TestCellType.SingleSocket;
        public TestCellStatus Status { get; set; } = TestCellStatus.Offline;
        public string Location { get; set; } = string.Empty;
        public int SocketCount { get; set; } = 1;
        public List<string> AssignedTestPrograms { get; set; } = new();
        public List<string> AssignedProducts { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
        public int TotalExecutions { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }

        public double PassRate => TotalExecutions > 0 ? (double)PassCount / TotalExecutions * 100 : 0;
    }

    /// <summary>
    /// Test cell execution statistics
    /// </summary>
    public class TestCellStatistics
    {
        public string CellId { get; set; } = string.Empty;
        public int TotalExecutions { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public double Utilization { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    /// <summary>
    /// Test cell group for organizing cells
    /// </summary>
    public class TestCellGroup
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> CellIds { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// Manages test cells
    /// </summary>
    public class TestCellManager
    {
        private static readonly Lazy<TestCellManager> _instance = new(() => new TestCellManager());
        public static TestCellManager Instance => _instance.Value;

        private readonly Dictionary<string, TestCell> _cells = new();
        private readonly Dictionary<string, TestCellGroup> _groups = new();
        private readonly object _lock = new();

        private TestCellManager() { }

        public void RegisterCell(TestCell cell)
        {
            lock (_lock)
            {
                _cells[cell.Id] = cell;
            }
        }

        public TestCell? GetCell(string cellId)
        {
            lock (_lock)
            {
                return _cells.TryGetValue(cellId, out var cell) ? cell : null;
            }
        }

        public List<TestCell> GetAllCells()
        {
            lock (_lock)
            {
                return _cells.Values.ToList();
            }
        }

        public List<TestCell> GetCellsByStatus(TestCellStatus status)
        {
            lock (_lock)
            {
                return _cells.Values.Where(c => c.Status == status).ToList();
            }
        }

        public void UpdateCellStatus(string cellId, TestCellStatus status)
        {
            lock (_lock)
            {
                if (_cells.TryGetValue(cellId, out var cell))
                {
                    cell.Status = status;
                    cell.LastActiveAt = DateTime.UtcNow;
                }
            }
        }

        public void RecordExecution(string cellId, bool passed)
        {
            lock (_lock)
            {
                if (_cells.TryGetValue(cellId, out var cell))
                {
                    cell.TotalExecutions++;
                    if (passed) cell.PassCount++;
                    else cell.FailCount++;
                    cell.LastActiveAt = DateTime.UtcNow;
                }
            }
        }

        public TestCellStatistics GetStatistics(string cellId)
        {
            lock (_lock)
            {
                if (_cells.TryGetValue(cellId, out var cell))
                {
                    return new TestCellStatistics
                    {
                        CellId = cellId,
                        TotalExecutions = cell.TotalExecutions,
                        PassCount = cell.PassCount,
                        FailCount = cell.FailCount
                    };
                }
                return new TestCellStatistics { CellId = cellId };
            }
        }

        public void RegisterGroup(TestCellGroup group)
        {
            lock (_lock)
            {
                _groups[group.Id] = group;
            }
        }

        public void AddCellToGroup(string groupId, string cellId)
        {
            lock (_lock)
            {
                if (_groups.TryGetValue(groupId, out var group) && !group.CellIds.Contains(cellId))
                {
                    group.CellIds.Add(cellId);
                }
            }
        }

        public List<TestCell> GetCellsInGroup(string groupId)
        {
            lock (_lock)
            {
                if (_groups.TryGetValue(groupId, out var group))
                {
                    return group.CellIds.Where(id => _cells.ContainsKey(id)).Select(id => _cells[id]).ToList();
                }
                return new List<TestCell>();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _cells.Clear();
                _groups.Clear();
            }
        }
    }
}
