using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TestStandClone.Core.ExecutionHistory
{
    /// <summary>
    /// Execution status
    /// </summary>
    public enum ExecutionHistoryStatus
    {
        Started,
        Completed,
        Failed,
        Aborted,
        Terminated
    }

    /// <summary>
    /// A single execution history entry
    /// </summary>
    public class ExecutionEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SequenceName { get; set; } = string.Empty;
        public string SequenceFilePath { get; set; } = string.Empty;
        public string UUTSerialNumber { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
        public ExecutionHistoryStatus Status { get; set; }
        public StepStatus OverallResult { get; set; }
        public int TotalSteps { get; set; }
        public int PassedSteps { get; set; }
        public int FailedSteps { get; set; }
        public int ErrorSteps { get; set; }
        public int SkippedSteps { get; set; }
        public string Operator { get; set; } = string.Empty;
        public string StationId { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public List<StepHistoryEntry> Steps { get; set; } = new();

        /// <summary>
        /// Calculate pass rate
        /// </summary>
        public double PassRate => TotalSteps > 0 ? (double)PassedSteps / TotalSteps * 100 : 0;
    }

    /// <summary>
    /// Step history entry
    /// </summary>
    public class StepHistoryEntry
    {
        public string StepId { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public string StepType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
        public StepStatus Status { get; set; }
        public string? ResultText { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Execution history query options
    /// </summary>
    public class ExecutionHistoryQuery
    {
        public string? SequenceName { get; set; }
        public string? UUTSerialNumber { get; set; }
        public string? Operator { get; set; }
        public string? StationId { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public ExecutionHistoryStatus? Status { get; set; }
        public StepStatus? OverallResult { get; set; }
        public int MaxResults { get; set; } = 100;
        public bool OrderByDescending { get; set; } = true;
    }

    /// <summary>
    /// Execution history statistics
    /// </summary>
    public class ExecutionStatistics
    {
        public int TotalExecutions { get; set; }
        public int CompletedExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public int AbortedExecutions { get; set; }
        public double AveragePassRate { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public DateTime? FirstExecution { get; set; }
        public DateTime? LastExecution { get; set; }
        public Dictionary<string, int> ExecutionsBySequence { get; set; } = new();
        public Dictionary<string, int> ExecutionsByOperator { get; set; } = new();
        public Dictionary<string, int> ExecutionsByStation { get; set; } = new();
    }

    /// <summary>
    /// Execution history manager
    /// </summary>
    public class ExecutionHistoryManager
    {
        private static readonly Lazy<ExecutionHistoryManager> _instance = new(() => new ExecutionHistoryManager());
        public static ExecutionHistoryManager Instance => _instance.Value;

        private readonly List<ExecutionEntry> _entries = new();
        private readonly object _lock = new();
        private string _storageDirectory;
        private int _maxEntriesInMemory = 1000;

        public event EventHandler<ExecutionHistoryEventArgs>? ExecutionStarted;
        public event EventHandler<ExecutionHistoryEventArgs>? ExecutionCompleted;

        private ExecutionHistoryManager()
        {
            _storageDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TestStandClone", "ExecutionHistory");
            Directory.CreateDirectory(_storageDirectory);
        }

        /// <summary>
        /// Storage directory for persisted entries
        /// </summary>
        public string StorageDirectory
        {
            get => _storageDirectory;
            set
            {
                _storageDirectory = value;
                Directory.CreateDirectory(_storageDirectory);
            }
        }

        /// <summary>
        /// Maximum entries to keep in memory
        /// </summary>
        public int MaxEntriesInMemory
        {
            get => _maxEntriesInMemory;
            set => _maxEntriesInMemory = value;
        }

        /// <summary>
        /// Start a new execution entry
        /// </summary>
        public ExecutionEntry StartExecution(string sequenceName, string uutSerialNumber = "", string operatorName = "", string stationId = "")
        {
            var entry = new ExecutionEntry
            {
                SequenceName = sequenceName,
                UUTSerialNumber = uutSerialNumber,
                Operator = operatorName,
                StationId = stationId,
                StartTime = DateTime.UtcNow,
                Status = ExecutionHistoryStatus.Started
            };

            lock (_lock)
            {
                _entries.Add(entry);

                // Trim if needed
                while (_entries.Count > _maxEntriesInMemory)
                {
                    var oldest = _entries[0];
                    PersistEntry(oldest);
                    _entries.RemoveAt(0);
                }
            }

            ExecutionStarted?.Invoke(this, new ExecutionHistoryEventArgs { Entry = entry });

            return entry;
        }

        /// <summary>
        /// Complete an execution entry
        /// </summary>
        public void CompleteExecution(string entryId, StepStatus overallResult, string? errorMessage = null)
        {
            ExecutionEntry? entry;
            lock (_lock)
            {
                entry = _entries.FirstOrDefault(e => e.Id == entryId);
                if (entry == null) return;

                entry.EndTime = DateTime.UtcNow;
                entry.OverallResult = overallResult;
                entry.ErrorMessage = errorMessage;
                entry.Status = overallResult == StepStatus.Passed 
                    ? ExecutionHistoryStatus.Completed 
                    : ExecutionHistoryStatus.Failed;

                // Update step counts
                entry.TotalSteps = entry.Steps.Count;
                entry.PassedSteps = entry.Steps.Count(s => s.Status == StepStatus.Passed);
                entry.FailedSteps = entry.Steps.Count(s => s.Status == StepStatus.Failed);
                entry.ErrorSteps = entry.Steps.Count(s => s.Status == StepStatus.Error);
                entry.SkippedSteps = entry.Steps.Count(s => s.Status == StepStatus.Idle);
            }

            ExecutionCompleted?.Invoke(this, new ExecutionHistoryEventArgs { Entry = entry });
        }

        /// <summary>
        /// Add a step result to an execution entry
        /// </summary>
        public void AddStepResult(string entryId, string stepId, string stepName, string stepType, 
            StepStatus status, DateTime startTime, DateTime endTime, string? resultText = null, string? errorMessage = null)
        {
            lock (_lock)
            {
                var entry = _entries.FirstOrDefault(e => e.Id == entryId);
                if (entry == null) return;

                entry.Steps.Add(new StepHistoryEntry
                {
                    StepId = stepId,
                    StepName = stepName,
                    StepType = stepType,
                    StartTime = startTime,
                    EndTime = endTime,
                    Status = status,
                    ResultText = resultText,
                    ErrorMessage = errorMessage
                });
            }
        }

        /// <summary>
        /// Abort an execution
        /// </summary>
        public void AbortExecution(string entryId, string? reason = null)
        {
            lock (_lock)
            {
                var entry = _entries.FirstOrDefault(e => e.Id == entryId);
                if (entry == null) return;

                entry.EndTime = DateTime.UtcNow;
                entry.Status = ExecutionHistoryStatus.Aborted;
                entry.ErrorMessage = reason ?? "Execution aborted";
            }
        }

        /// <summary>
        /// Query execution history
        /// </summary>
        public IEnumerable<ExecutionEntry> Query(ExecutionHistoryQuery query)
        {
            IEnumerable<ExecutionEntry> results;
            lock (_lock)
            {
                results = _entries.AsEnumerable();
            }

            if (!string.IsNullOrEmpty(query.SequenceName))
            {
                results = results.Where(e => e.SequenceName.Contains(query.SequenceName, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(query.UUTSerialNumber))
            {
                results = results.Where(e => e.UUTSerialNumber.Contains(query.UUTSerialNumber, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(query.Operator))
            {
                results = results.Where(e => e.Operator.Contains(query.Operator, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(query.StationId))
            {
                results = results.Where(e => e.StationId == query.StationId);
            }

            if (query.StartDateFrom.HasValue)
            {
                results = results.Where(e => e.StartTime >= query.StartDateFrom.Value);
            }

            if (query.StartDateTo.HasValue)
            {
                results = results.Where(e => e.StartTime <= query.StartDateTo.Value);
            }

            if (query.Status.HasValue)
            {
                results = results.Where(e => e.Status == query.Status.Value);
            }

            if (query.OverallResult.HasValue)
            {
                results = results.Where(e => e.OverallResult == query.OverallResult.Value);
            }

            results = query.OrderByDescending
                ? results.OrderByDescending(e => e.StartTime)
                : results.OrderBy(e => e.StartTime);

            return results.Take(query.MaxResults).ToList();
        }

        /// <summary>
        /// Get statistics for execution history
        /// </summary>
        public ExecutionStatistics GetStatistics(DateTime? from = null, DateTime? to = null)
        {
            List<ExecutionEntry> entries;
            lock (_lock)
            {
                entries = _entries.ToList();
            }

            if (from.HasValue)
            {
                entries = entries.Where(e => e.StartTime >= from.Value).ToList();
            }

            if (to.HasValue)
            {
                entries = entries.Where(e => e.StartTime <= to.Value).ToList();
            }

            var stats = new ExecutionStatistics
            {
                TotalExecutions = entries.Count,
                CompletedExecutions = entries.Count(e => e.Status == ExecutionHistoryStatus.Completed),
                FailedExecutions = entries.Count(e => e.Status == ExecutionHistoryStatus.Failed),
                AbortedExecutions = entries.Count(e => e.Status == ExecutionHistoryStatus.Aborted),
                FirstExecution = entries.MinBy(e => e.StartTime)?.StartTime,
                LastExecution = entries.MaxBy(e => e.StartTime)?.StartTime
            };

            if (entries.Any())
            {
                stats.AveragePassRate = entries.Average(e => e.PassRate);
                stats.TotalDuration = TimeSpan.FromTicks(entries.Sum(e => e.Duration.Ticks));
                stats.AverageDuration = TimeSpan.FromTicks((long)entries.Average(e => e.Duration.Ticks));

                stats.ExecutionsBySequence = entries
                    .GroupBy(e => e.SequenceName)
                    .ToDictionary(g => g.Key, g => g.Count());

                stats.ExecutionsByOperator = entries
                    .Where(e => !string.IsNullOrEmpty(e.Operator))
                    .GroupBy(e => e.Operator)
                    .ToDictionary(g => g.Key, g => g.Count());

                stats.ExecutionsByStation = entries
                    .Where(e => !string.IsNullOrEmpty(e.StationId))
                    .GroupBy(e => e.StationId)
                    .ToDictionary(g => g.Key, g => g.Count());
            }

            return stats;
        }

        /// <summary>
        /// Get an entry by ID
        /// </summary>
        public ExecutionEntry? GetEntry(string entryId)
        {
            lock (_lock)
            {
                return _entries.FirstOrDefault(e => e.Id == entryId);
            }
        }

        /// <summary>
        /// Get recent entries
        /// </summary>
        public IEnumerable<ExecutionEntry> GetRecent(int count = 10)
        {
            lock (_lock)
            {
                return _entries
                    .OrderByDescending(e => e.StartTime)
                    .Take(count)
                    .ToList();
            }
        }

        /// <summary>
        /// Persist an entry to disk
        /// </summary>
        private void PersistEntry(ExecutionEntry entry)
        {
            try
            {
                var fileName = $"{entry.StartTime:yyyyMMdd}_{entry.Id}.json";
                var filePath = Path.Combine(_storageDirectory, fileName);
                var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            catch
            {
                // Ignore persistence errors
            }
        }

        /// <summary>
        /// Load entries from disk
        /// </summary>
        public int LoadFromDisk(DateTime? from = null, DateTime? to = null)
        {
            var loadedCount = 0;

            try
            {
                var files = Directory.GetFiles(_storageDirectory, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var entry = JsonSerializer.Deserialize<ExecutionEntry>(json);
                        if (entry == null) continue;

                        if (from.HasValue && entry.StartTime < from.Value) continue;
                        if (to.HasValue && entry.StartTime > to.Value) continue;

                        lock (_lock)
                        {
                            if (!_entries.Any(e => e.Id == entry.Id))
                            {
                                _entries.Add(entry);
                                loadedCount++;
                            }
                        }
                    }
                    catch
                    {
                        // Skip invalid files
                    }
                }
            }
            catch
            {
                // Ignore load errors
            }

            return loadedCount;
        }

        /// <summary>
        /// Clear all entries
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _entries.Clear();
            }
        }

        /// <summary>
        /// Export entries to JSON file
        /// </summary>
        public void Export(string filePath, ExecutionHistoryQuery? query = null)
        {
            var entries = query != null ? Query(query) : _entries;
            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }

    /// <summary>
    /// Execution history event arguments
    /// </summary>
    public class ExecutionHistoryEventArgs : EventArgs
    {
        public ExecutionEntry Entry { get; set; } = new();
    }
}
