using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;

namespace TestStandClone.Core.DatabaseViewer
{
    /// <summary>
    /// Database query filter
    /// </summary>
    public class DatabaseQuery
    {
        public string? SequenceName { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public string? StepName { get; set; }
        public int? MaxResults { get; set; }
        public int? Skip { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;
    }

    /// <summary>
    /// Database record for viewing
    /// </summary>
    public class DatabaseRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime ExecutionTime { get; set; }
        public string SequenceName { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public int TotalSteps { get; set; }
        public int PassedSteps { get; set; }
        public int FailedSteps { get; set; }
        public string? OperatorName { get; set; }
        public string? StationId { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
        public List<StepRecord> StepResults { get; set; } = new List<StepRecord>();
    }

    /// <summary>
    /// Step record for viewing
    /// </summary>
    public class StepRecord
    {
        public string StepName { get; set; } = string.Empty;
        public string StepType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public object? MeasuredValue { get; set; }
        public object? LowLimit { get; set; }
        public object? HighLimit { get; set; }
        public string? Unit { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Query result
    /// </summary>
    public class QueryResult
    {
        public List<DatabaseRecord> Records { get; set; } = new List<DatabaseRecord>();
        public int TotalCount { get; set; }
        public int ReturnedCount { get; set; }
        public TimeSpan QueryDuration { get; set; }
    }

    /// <summary>
    /// Database statistics
    /// </summary>
    public class DatabaseStats
    {
        public int TotalRecords { get; set; }
        public int TotalPassed { get; set; }
        public int TotalFailed { get; set; }
        public double PassRate => TotalRecords > 0 ? (double)TotalPassed / TotalRecords * 100 : 0;
        public DateTime? OldestRecord { get; set; }
        public DateTime? NewestRecord { get; set; }
        public Dictionary<string, int> RecordsBySequence { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> RecordsByDate { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, double> PassRateBySequence { get; set; } = new Dictionary<string, double>();
    }

    /// <summary>
    /// Database viewer for browsing test results
    /// </summary>
    public class DatabaseViewer
    {
        private readonly List<DatabaseRecord> _records = new List<DatabaseRecord>();
        private readonly string _databasePath;

        public DatabaseViewer(string databasePath = "")
        {
            _databasePath = databasePath;
        }

        /// <summary>
        /// Load records from the database
        /// </summary>
        public void LoadRecords()
        {
            if (string.IsNullOrEmpty(_databasePath) || !File.Exists(_databasePath))
            {
                // Generate sample data for testing
                GenerateSampleData();
                return;
            }

            try
            {
                var json = File.ReadAllText(_databasePath);
                var records = JsonSerializer.Deserialize<List<DatabaseRecord>>(json);
                if (records != null)
                {
                    _records.Clear();
                    _records.AddRange(records);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading database: {ex.Message}");
            }
        }

        /// <summary>
        /// Query records with filter
        /// </summary>
        public QueryResult Query(DatabaseQuery query)
        {
            var startTime = DateTime.Now;
            var filtered = _records.AsEnumerable();

            // Apply filters
            if (!string.IsNullOrEmpty(query.SequenceName))
            {
                filtered = filtered.Where(r => 
                    r.SequenceName.Contains(query.SequenceName, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(query.SerialNumber))
            {
                filtered = filtered.Where(r => 
                    r.SerialNumber.Contains(query.SerialNumber, StringComparison.OrdinalIgnoreCase));
            }

            if (query.StartDate.HasValue)
            {
                filtered = filtered.Where(r => r.ExecutionTime >= query.StartDate.Value);
            }

            if (query.EndDate.HasValue)
            {
                filtered = filtered.Where(r => r.ExecutionTime <= query.EndDate.Value);
            }

            if (!string.IsNullOrEmpty(query.Status))
            {
                filtered = filtered.Where(r => 
                    r.Status.Equals(query.Status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(query.StepName))
            {
                filtered = filtered.Where(r => 
                    r.StepResults.Any(s => 
                        s.StepName.Contains(query.StepName, StringComparison.OrdinalIgnoreCase)));
            }

            // Sort
            filtered = query.SortBy?.ToLower() switch
            {
                "sequencename" => query.SortDescending 
                    ? filtered.OrderByDescending(r => r.SequenceName)
                    : filtered.OrderBy(r => r.SequenceName),
                "serialnumber" => query.SortDescending 
                    ? filtered.OrderByDescending(r => r.SerialNumber)
                    : filtered.OrderBy(r => r.SerialNumber),
                "status" => query.SortDescending 
                    ? filtered.OrderByDescending(r => r.Status)
                    : filtered.OrderBy(r => r.Status),
                "duration" => query.SortDescending 
                    ? filtered.OrderByDescending(r => r.Duration)
                    : filtered.OrderBy(r => r.Duration),
                _ => query.SortDescending 
                    ? filtered.OrderByDescending(r => r.ExecutionTime)
                    : filtered.OrderBy(r => r.ExecutionTime)
            };

            var totalCount = filtered.Count();

            // Pagination
            if (query.Skip.HasValue)
            {
                filtered = filtered.Skip(query.Skip.Value);
            }

            if (query.MaxResults.HasValue)
            {
                filtered = filtered.Take(query.MaxResults.Value);
            }

            var results = filtered.ToList();

            return new QueryResult
            {
                Records = results,
                TotalCount = totalCount,
                ReturnedCount = results.Count,
                QueryDuration = DateTime.Now - startTime
            };
        }

        /// <summary>
        /// Get a single record by ID
        /// </summary>
        public DatabaseRecord? GetRecord(string id)
        {
            return _records.FirstOrDefault(r => r.Id == id);
        }

        /// <summary>
        /// Get database statistics
        /// </summary>
        public DatabaseStats GetStatistics()
        {
            var stats = new DatabaseStats
            {
                TotalRecords = _records.Count,
                TotalPassed = _records.Count(r => r.Status.Equals("Passed", StringComparison.OrdinalIgnoreCase)),
                TotalFailed = _records.Count(r => r.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase)),
                OldestRecord = _records.OrderBy(r => r.ExecutionTime).FirstOrDefault()?.ExecutionTime,
                NewestRecord = _records.OrderByDescending(r => r.ExecutionTime).FirstOrDefault()?.ExecutionTime
            };

            // Records by sequence
            stats.RecordsBySequence = _records
                .GroupBy(r => r.SequenceName)
                .ToDictionary(g => g.Key, g => g.Count());

            // Records by date
            stats.RecordsByDate = _records
                .GroupBy(r => r.ExecutionTime.Date.ToString("yyyy-MM-dd"))
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            // Pass rate by sequence
            foreach (var group in _records.GroupBy(r => r.SequenceName))
            {
                var total = group.Count();
                var passed = group.Count(r => r.Status.Equals("Passed", StringComparison.OrdinalIgnoreCase));
                stats.PassRateBySequence[group.Key] = total > 0 ? (double)passed / total * 100 : 0;
            }

            return stats;
        }

        /// <summary>
        /// Export results to CSV
        /// </summary>
        public string ExportToCsv(IEnumerable<DatabaseRecord> records)
        {
            var lines = new List<string>
            {
                "ExecutionTime,SequenceName,SerialNumber,Status,Duration,PassedSteps,FailedSteps"
            };

            foreach (var record in records)
            {
                lines.Add($"{record.ExecutionTime:yyyy-MM-dd HH:mm:ss},{record.SequenceName}," +
                          $"{record.SerialNumber},{record.Status},{record.Duration.TotalSeconds:F2}," +
                          $"{record.PassedSteps},{record.FailedSteps}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Get unique sequence names
        /// </summary>
        public List<string> GetSequenceNames()
        {
            return _records.Select(r => r.SequenceName).Distinct().OrderBy(n => n).ToList();
        }

        /// <summary>
        /// Get unique serial numbers
        /// </summary>
        public List<string> GetSerialNumbers()
        {
            return _records.Select(r => r.SerialNumber).Distinct().OrderBy(n => n).ToList();
        }

        private void GenerateSampleData()
        {
            var random = new Random();
            var sequences = new[] { "MainSequence", "CalibrationSequence", "FunctionalTest", "DiagnosticsSequence" };
            var statuses = new[] { "Passed", "Passed", "Passed", "Failed", "Error" }; // Weighted toward pass

            for (int i = 0; i < 100; i++)
            {
                var seq = sequences[random.Next(sequences.Length)];
                var status = statuses[random.Next(statuses.Length)];
                var steps = random.Next(5, 20);
                var failed = status == "Passed" ? 0 : random.Next(1, 3);
                var passed = steps - failed;

                _records.Add(new DatabaseRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    ExecutionTime = DateTime.Now.AddDays(-random.Next(0, 30)).AddHours(-random.Next(0, 24)),
                    SequenceName = seq,
                    SerialNumber = $"SN{random.Next(10000, 99999)}",
                    Status = status,
                    Duration = TimeSpan.FromSeconds(random.Next(10, 600)),
                    TotalSteps = steps,
                    PassedSteps = passed,
                    FailedSteps = failed
                });
            }
        }
    }

    /// <summary>
    /// Database viewer manager singleton
    /// </summary>
    public class DatabaseViewerManager
    {
        private static readonly Lazy<DatabaseViewerManager> _instance = 
            new Lazy<DatabaseViewerManager>(() => new DatabaseViewerManager());
        
        public static DatabaseViewerManager Instance => _instance.Value;

        private readonly Dictionary<string, DatabaseViewer> _viewers = 
            new Dictionary<string, DatabaseViewer>();
        
        private DatabaseViewer? _defaultViewer;

        private DatabaseViewerManager() { }

        /// <summary>
        /// Get the default viewer
        /// </summary>
        public DatabaseViewer DefaultViewer
        {
            get
            {
                if (_defaultViewer == null)
                {
                    _defaultViewer = new DatabaseViewer();
                    _defaultViewer.LoadRecords();
                }
                return _defaultViewer;
            }
        }

        /// <summary>
        /// Get or create a viewer for a specific database
        /// </summary>
        public DatabaseViewer GetViewer(string databasePath)
        {
            if (!_viewers.ContainsKey(databasePath))
            {
                var viewer = new DatabaseViewer(databasePath);
                viewer.LoadRecords();
                _viewers[databasePath] = viewer;
            }
            return _viewers[databasePath];
        }

        /// <summary>
        /// Remove a viewer
        /// </summary>
        public void RemoveViewer(string databasePath)
        {
            _viewers.Remove(databasePath);
        }
    }
}
