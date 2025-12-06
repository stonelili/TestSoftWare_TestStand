using System.Text.Json;

namespace TestStandClone.Core.Database
{
    /// <summary>
    /// Represents a test execution result record for database storage.
    /// </summary>
    public class TestResultRecord
    {
        /// <summary>
        /// Unique identifier for this result record.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// UUT serial number.
        /// </summary>
        public string UUTSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// Sequence name.
        /// </summary>
        public string SequenceName { get; set; } = string.Empty;

        /// <summary>
        /// Sequence file path.
        /// </summary>
        public string SequenceFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Overall test result.
        /// </summary>
        public string Result { get; set; } = string.Empty;

        /// <summary>
        /// Test start time.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Test end time.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Total execution time in seconds.
        /// </summary>
        public double ExecutionTimeSeconds { get; set; }

        /// <summary>
        /// Station ID.
        /// </summary>
        public string StationId { get; set; } = string.Empty;

        /// <summary>
        /// Operator name.
        /// </summary>
        public string OperatorName { get; set; } = string.Empty;

        /// <summary>
        /// Number of steps that passed.
        /// </summary>
        public int StepsPassed { get; set; }

        /// <summary>
        /// Number of steps that failed.
        /// </summary>
        public int StepsFailed { get; set; }

        /// <summary>
        /// Total number of steps executed.
        /// </summary>
        public int TotalSteps { get; set; }

        /// <summary>
        /// Step results in JSON format.
        /// </summary>
        public string StepResultsJson { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a single step result for database storage.
    /// </summary>
    public class StepResultRecord
    {
        /// <summary>
        /// Step name.
        /// </summary>
        public string StepName { get; set; } = string.Empty;

        /// <summary>
        /// Step type.
        /// </summary>
        public string StepType { get; set; } = string.Empty;

        /// <summary>
        /// Step status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Result text.
        /// </summary>
        public string ResultText { get; set; } = string.Empty;

        /// <summary>
        /// Execution time in milliseconds.
        /// </summary>
        public double ExecutionTimeMs { get; set; }

        /// <summary>
        /// Measured value (for numeric limit steps).
        /// </summary>
        public double? MeasuredValue { get; set; }

        /// <summary>
        /// Low limit (for numeric limit steps).
        /// </summary>
        public double? LowLimit { get; set; }

        /// <summary>
        /// High limit (for numeric limit steps).
        /// </summary>
        public double? HighLimit { get; set; }
    }

    /// <summary>
    /// Simple JSON-based test result database.
    /// Stores test results to JSON files, providing a file-based alternative to SQLite.
    /// </summary>
    public class TestResultDatabase : IDisposable
    {
        private readonly string _databasePath;
        private readonly object _lockObject = new object();
        private List<TestResultRecord> _records = new List<TestResultRecord>();

        /// <summary>
        /// Creates a new TestResultDatabase instance.
        /// </summary>
        /// <param name="databasePath">Path to the database file.</param>
        public TestResultDatabase(string databasePath)
        {
            _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
            LoadDatabase();
        }

        /// <summary>
        /// Loads the database from disk.
        /// </summary>
        private void LoadDatabase()
        {
            lock (_lockObject)
            {
                if (File.Exists(_databasePath))
                {
                    try
                    {
                        var json = File.ReadAllText(_databasePath);
                        _records = JsonSerializer.Deserialize<List<TestResultRecord>>(json) ?? new List<TestResultRecord>();
                    }
                    catch
                    {
                        _records = new List<TestResultRecord>();
                    }
                }
            }
        }

        /// <summary>
        /// Saves the database to disk.
        /// </summary>
        private void SaveDatabase()
        {
            lock (_lockObject)
            {
                var json = JsonSerializer.Serialize(_records, new JsonSerializerOptions { WriteIndented = true });
                var directory = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(_databasePath, json);
            }
        }

        /// <summary>
        /// Logs a test result to the database.
        /// </summary>
        /// <param name="sequence">The executed sequence.</param>
        /// <param name="uutSerialNumber">UUT serial number.</param>
        /// <param name="stationId">Station ID.</param>
        /// <param name="operatorName">Operator name.</param>
        /// <param name="sequenceFilePath">Path to the sequence file.</param>
        /// <returns>The created result record.</returns>
        public TestResultRecord LogResult(
            Sequence sequence,
            string uutSerialNumber = "",
            string stationId = "",
            string operatorName = "",
            string sequenceFilePath = "")
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            var stepResults = new List<StepResultRecord>();
            int passed = 0;
            int failed = 0;

            foreach (var step in sequence.Steps)
            {
                var stepRecord = new StepResultRecord
                {
                    StepName = step.Name,
                    StepType = step.GetType().Name,
                    Status = step.Status.ToString(),
                    ResultText = step.ResultText ?? string.Empty,
                    ExecutionTimeMs = step.ExecutionTime?.TotalMilliseconds ?? 0
                };

                if (step is NumericLimitStep numericStep)
                {
                    stepRecord.MeasuredValue = numericStep.MeasuredValue;
                    stepRecord.LowLimit = numericStep.LowerLimit;
                    stepRecord.HighLimit = numericStep.UpperLimit;
                }

                stepResults.Add(stepRecord);

                if (step.Status == StepStatus.Passed)
                {
                    passed++;
                }
                else if (step.Status == StepStatus.Failed || step.Status == StepStatus.Error)
                {
                    failed++;
                }
            }

            var record = new TestResultRecord
            {
                UUTSerialNumber = uutSerialNumber,
                SequenceName = sequence.Name,
                SequenceFilePath = sequenceFilePath,
                Result = sequence.Status.ToString(),
                StartTime = sequence.StartTime ?? DateTime.Now,
                EndTime = sequence.EndTime ?? DateTime.Now,
                ExecutionTimeSeconds = sequence.ExecutionTime?.TotalSeconds ?? 0,
                StationId = stationId,
                OperatorName = operatorName,
                StepsPassed = passed,
                StepsFailed = failed,
                TotalSteps = sequence.Steps.Count,
                StepResultsJson = JsonSerializer.Serialize(stepResults)
            };

            lock (_lockObject)
            {
                _records.Add(record);
            }

            SaveDatabase();
            return record;
        }

        /// <summary>
        /// Gets all test result records.
        /// </summary>
        public IReadOnlyList<TestResultRecord> GetAllResults()
        {
            lock (_lockObject)
            {
                return _records.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Gets test results for a specific UUT.
        /// </summary>
        public IReadOnlyList<TestResultRecord> GetResultsByUUT(string serialNumber)
        {
            lock (_lockObject)
            {
                return _records
                    .Where(r => r.UUTSerialNumber.Equals(serialNumber, StringComparison.OrdinalIgnoreCase))
                    .ToList()
                    .AsReadOnly();
            }
        }

        /// <summary>
        /// Gets test results within a date range.
        /// </summary>
        public IReadOnlyList<TestResultRecord> GetResultsByDateRange(DateTime startDate, DateTime endDate)
        {
            lock (_lockObject)
            {
                return _records
                    .Where(r => r.StartTime >= startDate && r.StartTime <= endDate)
                    .ToList()
                    .AsReadOnly();
            }
        }

        /// <summary>
        /// Gets test results by sequence name.
        /// </summary>
        public IReadOnlyList<TestResultRecord> GetResultsBySequence(string sequenceName)
        {
            lock (_lockObject)
            {
                return _records
                    .Where(r => r.SequenceName.Equals(sequenceName, StringComparison.OrdinalIgnoreCase))
                    .ToList()
                    .AsReadOnly();
            }
        }

        /// <summary>
        /// Clears all results from the database.
        /// </summary>
        public void ClearAllResults()
        {
            lock (_lockObject)
            {
                _records.Clear();
            }
            SaveDatabase();
        }

        /// <summary>
        /// Gets the pass rate for a specific sequence.
        /// </summary>
        public double GetPassRate(string sequenceName)
        {
            lock (_lockObject)
            {
                var sequenceResults = _records
                    .Where(r => r.SequenceName.Equals(sequenceName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (sequenceResults.Count == 0)
                {
                    return 0.0;
                }

                int passed = sequenceResults.Count(r => r.Result == "Passed");
                return (double)passed / sequenceResults.Count * 100.0;
            }
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            // Ensure data is saved before disposal
            SaveDatabase();
            GC.SuppressFinalize(this);
        }
    }
}
