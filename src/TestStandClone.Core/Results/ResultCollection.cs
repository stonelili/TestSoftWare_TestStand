// ResultCollection.cs - Enhanced result data collection
// Provides comprehensive test result management

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TestStandClone.Core.Results
{
    /// <summary>
    /// Represents the overall result status
    /// </summary>
    public enum ResultStatus
    {
        /// <summary>Test passed all criteria</summary>
        Passed,
        /// <summary>Test failed one or more criteria</summary>
        Failed,
        /// <summary>Test encountered an error</summary>
        Error,
        /// <summary>Test was skipped</summary>
        Skipped,
        /// <summary>Test was terminated</summary>
        Terminated,
        /// <summary>Test result is pending</summary>
        Pending
    }

    /// <summary>
    /// Represents a single measurement result
    /// </summary>
    public class MeasurementResult : INotifyPropertyChanged
    {
        private double _value;
        private ResultStatus _status;

        /// <summary>Unique identifier</summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        /// <summary>Measurement name</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Measurement unit</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>Measured value</summary>
        public double Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Low limit</summary>
        public double? LowLimit { get; set; }

        /// <summary>High limit</summary>
        public double? HighLimit { get; set; }

        /// <summary>Result status</summary>
        public ResultStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Timestamp when measurement was taken</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>Additional properties</summary>
        public Dictionary<string, object?> Properties { get; set; } = new();

        /// <summary>
        /// Evaluate the result against limits
        /// </summary>
        public void Evaluate()
        {
            if (LowLimit.HasValue && Value < LowLimit.Value)
            {
                Status = ResultStatus.Failed;
            }
            else if (HighLimit.HasValue && Value > HighLimit.Value)
            {
                Status = ResultStatus.Failed;
            }
            else
            {
                Status = ResultStatus.Passed;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a step result
    /// </summary>
    public class StepResultData
    {
        /// <summary>Step ID</summary>
        public string StepId { get; set; } = string.Empty;

        /// <summary>Step name</summary>
        public string StepName { get; set; } = string.Empty;

        /// <summary>Step type</summary>
        public string StepType { get; set; } = string.Empty;

        /// <summary>Result status</summary>
        public ResultStatus Status { get; set; } = ResultStatus.Pending;

        /// <summary>Result text/message</summary>
        public string ResultText { get; set; } = string.Empty;

        /// <summary>Start time</summary>
        public DateTime StartTime { get; set; }

        /// <summary>End time</summary>
        public DateTime EndTime { get; set; }

        /// <summary>Execution duration</summary>
        public TimeSpan Duration => EndTime > StartTime ? EndTime - StartTime : TimeSpan.Zero;

        /// <summary>Measurements collected by this step</summary>
        public List<MeasurementResult> Measurements { get; set; } = new();

        /// <summary>Loop iteration (if step was looped)</summary>
        public int LoopIteration { get; set; }

        /// <summary>Error message (if any)</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>Additional properties</summary>
        public Dictionary<string, object?> Properties { get; set; } = new();
    }

    /// <summary>
    /// Represents a sequence execution result
    /// </summary>
    public class SequenceResultData
    {
        /// <summary>Unique execution ID</summary>
        public string ExecutionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Sequence name</summary>
        public string SequenceName { get; set; } = string.Empty;

        /// <summary>Sequence file path</summary>
        public string SequenceFilePath { get; set; } = string.Empty;

        /// <summary>UUT serial number</summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>Overall result status</summary>
        public ResultStatus Status { get; set; } = ResultStatus.Pending;

        /// <summary>Start time</summary>
        public DateTime StartTime { get; set; }

        /// <summary>End time</summary>
        public DateTime EndTime { get; set; }

        /// <summary>Total execution duration</summary>
        public TimeSpan Duration => EndTime > StartTime ? EndTime - StartTime : TimeSpan.Zero;

        /// <summary>Step results</summary>
        public List<StepResultData> StepResults { get; set; } = new();

        /// <summary>Station name</summary>
        public string StationName { get; set; } = string.Empty;

        /// <summary>Operator name</summary>
        public string OperatorName { get; set; } = string.Empty;

        /// <summary>Test socket index</summary>
        public int TestSocket { get; set; }

        /// <summary>Additional properties</summary>
        public Dictionary<string, object?> Properties { get; set; } = new();

        /// <summary>
        /// Calculate the overall status from step results
        /// </summary>
        public void CalculateOverallStatus()
        {
            if (StepResults.Any(r => r.Status == ResultStatus.Error))
            {
                Status = ResultStatus.Error;
            }
            else if (StepResults.Any(r => r.Status == ResultStatus.Failed))
            {
                Status = ResultStatus.Failed;
            }
            else if (StepResults.All(r => r.Status == ResultStatus.Passed || r.Status == ResultStatus.Skipped))
            {
                Status = ResultStatus.Passed;
            }
            else
            {
                Status = ResultStatus.Pending;
            }
        }

        /// <summary>
        /// Get pass count
        /// </summary>
        public int PassCount => StepResults.Count(r => r.Status == ResultStatus.Passed);

        /// <summary>
        /// Get fail count
        /// </summary>
        public int FailCount => StepResults.Count(r => r.Status == ResultStatus.Failed);

        /// <summary>
        /// Get error count
        /// </summary>
        public int ErrorCount => StepResults.Count(r => r.Status == ResultStatus.Error);
    }

    /// <summary>
    /// Result collection query options
    /// </summary>
    public class ResultQueryOptions
    {
        /// <summary>Filter by serial number</summary>
        public string? SerialNumber { get; set; }

        /// <summary>Filter by sequence name</summary>
        public string? SequenceName { get; set; }

        /// <summary>Filter by status</summary>
        public ResultStatus? Status { get; set; }

        /// <summary>Filter by date range start</summary>
        public DateTime? StartDate { get; set; }

        /// <summary>Filter by date range end</summary>
        public DateTime? EndDate { get; set; }

        /// <summary>Filter by station name</summary>
        public string? StationName { get; set; }

        /// <summary>Maximum number of results to return</summary>
        public int? MaxResults { get; set; }

        /// <summary>Sort by field</summary>
        public string? SortBy { get; set; }

        /// <summary>Sort ascending</summary>
        public bool SortAscending { get; set; } = false;
    }

    /// <summary>
    /// Result statistics
    /// </summary>
    public class ResultStatistics
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int ErrorTests { get; set; }
        public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;
        public TimeSpan AverageTestTime { get; set; }
        public TimeSpan TotalTestTime { get; set; }
        public DateTime FirstTestTime { get; set; }
        public DateTime LastTestTime { get; set; }
    }

    /// <summary>
    /// Manages result collection and storage
    /// </summary>
    public class ResultCollectionManager
    {
        private static ResultCollectionManager? _instance;
        private static readonly object _lock = new();

        private readonly ConcurrentDictionary<string, SequenceResultData> _results = new();
        private readonly List<SequenceResultData> _recentResults = new();
        private readonly object _recentLock = new();
        private string _storageDirectory = string.Empty;

        public static ResultCollectionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ResultCollectionManager();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Event raised when a result is added
        /// </summary>
        public event EventHandler<SequenceResultData>? ResultAdded;

        private ResultCollectionManager() { }

        /// <summary>
        /// Set the storage directory for results
        /// </summary>
        public void SetStorageDirectory(string directory)
        {
            _storageDirectory = directory;
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }
        }

        /// <summary>
        /// Add a result to the collection
        /// </summary>
        public void AddResult(SequenceResultData result)
        {
            result.CalculateOverallStatus();
            _results[result.ExecutionId] = result;

            lock (_recentLock)
            {
                _recentResults.Add(result);
                
                // Keep only recent results in memory (e.g., last 1000)
                if (_recentResults.Count > 1000)
                {
                    _recentResults.RemoveAt(0);
                }
            }

            ResultAdded?.Invoke(this, result);
        }

        /// <summary>
        /// Get a result by execution ID
        /// </summary>
        public SequenceResultData? GetResult(string executionId)
        {
            _results.TryGetValue(executionId, out var result);
            return result;
        }

        /// <summary>
        /// Query results based on options
        /// </summary>
        public IEnumerable<SequenceResultData> QueryResults(ResultQueryOptions options)
        {
            IEnumerable<SequenceResultData> query;

            lock (_recentLock)
            {
                query = _recentResults.AsEnumerable();
            }

            if (!string.IsNullOrEmpty(options.SerialNumber))
            {
                query = query.Where(r => r.SerialNumber.Contains(options.SerialNumber, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(options.SequenceName))
            {
                query = query.Where(r => r.SequenceName.Contains(options.SequenceName, StringComparison.OrdinalIgnoreCase));
            }

            if (options.Status.HasValue)
            {
                query = query.Where(r => r.Status == options.Status.Value);
            }

            if (options.StartDate.HasValue)
            {
                query = query.Where(r => r.StartTime >= options.StartDate.Value);
            }

            if (options.EndDate.HasValue)
            {
                query = query.Where(r => r.EndTime <= options.EndDate.Value);
            }

            if (!string.IsNullOrEmpty(options.StationName))
            {
                query = query.Where(r => r.StationName == options.StationName);
            }

            // Sort
            query = options.SortBy switch
            {
                "StartTime" => options.SortAscending ? query.OrderBy(r => r.StartTime) : query.OrderByDescending(r => r.StartTime),
                "Duration" => options.SortAscending ? query.OrderBy(r => r.Duration) : query.OrderByDescending(r => r.Duration),
                "Status" => options.SortAscending ? query.OrderBy(r => r.Status) : query.OrderByDescending(r => r.Status),
                _ => query.OrderByDescending(r => r.StartTime)
            };

            if (options.MaxResults.HasValue)
            {
                query = query.Take(options.MaxResults.Value);
            }

            return query.ToList();
        }

        /// <summary>
        /// Get statistics for results
        /// </summary>
        public ResultStatistics GetStatistics(ResultQueryOptions? options = null)
        {
            var results = options != null ? QueryResults(options) : _recentResults;
            var resultList = results.ToList();

            var stats = new ResultStatistics
            {
                TotalTests = resultList.Count,
                PassedTests = resultList.Count(r => r.Status == ResultStatus.Passed),
                FailedTests = resultList.Count(r => r.Status == ResultStatus.Failed),
                ErrorTests = resultList.Count(r => r.Status == ResultStatus.Error)
            };

            if (resultList.Count > 0)
            {
                stats.TotalTestTime = TimeSpan.FromTicks(resultList.Sum(r => r.Duration.Ticks));
                stats.AverageTestTime = TimeSpan.FromTicks(stats.TotalTestTime.Ticks / resultList.Count);
                stats.FirstTestTime = resultList.Min(r => r.StartTime);
                stats.LastTestTime = resultList.Max(r => r.EndTime);
            }

            return stats;
        }

        /// <summary>
        /// Save a result to file
        /// </summary>
        public async Task SaveResultAsync(SequenceResultData result)
        {
            if (string.IsNullOrEmpty(_storageDirectory))
            {
                _storageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TestStandClone", "Results");
                if (!Directory.Exists(_storageDirectory))
                {
                    Directory.CreateDirectory(_storageDirectory);
                }
            }

            var fileName = $"{result.SerialNumber}_{result.StartTime:yyyyMMdd_HHmmss}_{result.ExecutionId}.json";
            var filePath = Path.Combine(_storageDirectory, fileName);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var json = JsonSerializer.Serialize(result, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Load results from storage directory
        /// </summary>
        public async Task LoadResultsAsync()
        {
            if (string.IsNullOrEmpty(_storageDirectory) || !Directory.Exists(_storageDirectory))
                return;

            var files = Directory.GetFiles(_storageDirectory, "*.json");
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var result = JsonSerializer.Deserialize<SequenceResultData>(json, options);
                    if (result != null)
                    {
                        _results[result.ExecutionId] = result;
                        lock (_recentLock)
                        {
                            _recentResults.Add(result);
                        }
                    }
                }
                catch
                {
                    // Skip invalid files
                }
            }
        }

        /// <summary>
        /// Export results to CSV
        /// </summary>
        public async Task ExportToCsvAsync(string filePath, ResultQueryOptions? options = null)
        {
            var results = options != null ? QueryResults(options) : _recentResults;
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("ExecutionId,SerialNumber,SequenceName,Status,StartTime,EndTime,Duration,PassCount,FailCount,ErrorCount,StationName,OperatorName");

            foreach (var result in results)
            {
                sb.AppendLine($"\"{result.ExecutionId}\",\"{result.SerialNumber}\",\"{result.SequenceName}\",{result.Status},{result.StartTime:yyyy-MM-dd HH:mm:ss},{result.EndTime:yyyy-MM-dd HH:mm:ss},{result.Duration.TotalSeconds:F2},{result.PassCount},{result.FailCount},{result.ErrorCount},\"{result.StationName}\",\"{result.OperatorName}\"");
            }

            await File.WriteAllTextAsync(filePath, sb.ToString());
        }

        /// <summary>
        /// Clear all results
        /// </summary>
        public void Clear()
        {
            _results.Clear();
            lock (_recentLock)
            {
                _recentResults.Clear();
            }
        }
    }
}
