using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;

namespace TestStandClone.Core.BatchSpecification
{
    /// <summary>
    /// Represents a batch testing specification
    /// </summary>
    public class BatchSpec
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int BatchSize { get; set; } = 1;
        public BatchMode Mode { get; set; } = BatchMode.Sequential;
        public List<BatchSlot> Slots { get; set; } = new();
        public BatchOptions Options { get; set; } = new();
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object?> Properties { get; set; } = new();
    }

    /// <summary>
    /// Batch execution modes
    /// </summary>
    public enum BatchMode
    {
        Sequential,
        Parallel,
        Synchronized,
        Pipeline
    }

    /// <summary>
    /// Represents a slot in a batch
    /// </summary>
    public class BatchSlot
    {
        public int SlotNumber { get; set; }
        public string SlotId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public SlotState State { get; set; } = SlotState.Empty;
        public string? SerialNumber { get; set; }
        public string? SequenceFile { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int Priority { get; set; } = 0;
        public Dictionary<string, object?> SlotProperties { get; set; } = new();
    }

    /// <summary>
    /// Slot states
    /// </summary>
    public enum SlotState
    {
        Empty,
        Ready,
        Running,
        Passed,
        Failed,
        Skipped,
        Error
    }

    /// <summary>
    /// Batch execution options
    /// </summary>
    public class BatchOptions
    {
        public bool WaitForAllSlots { get; set; } = true;
        public bool StopOnFirstFailure { get; set; } = false;
        public bool AllowPartialBatch { get; set; } = true;
        public int MinimumSlots { get; set; } = 1;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromHours(1);
        public bool PromptForSerialNumbers { get; set; } = true;
        public SerialNumberMode SerialNumberMode { get; set; } = SerialNumberMode.Manual;
        public string SerialNumberPattern { get; set; } = string.Empty;
        public bool ValidateSerialNumbers { get; set; } = true;
        public string? LimitsFile { get; set; }
        public string? ReportTemplate { get; set; }
        public bool GenerateBatchReport { get; set; } = true;
    }

    /// <summary>
    /// Serial number entry modes
    /// </summary>
    public enum SerialNumberMode
    {
        Manual,
        Scanner,
        AutoGenerate,
        Database
    }

    /// <summary>
    /// Batch execution result
    /// </summary>
    public class BatchResult
    {
        public string BatchId { get; set; } = Guid.NewGuid().ToString();
        public string BatchSpecId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public BatchResultStatus Status { get; set; } = BatchResultStatus.NotStarted;
        public List<SlotResult> SlotResults { get; set; } = new();
        public int TotalSlots => SlotResults.Count;
        public int PassedSlots => SlotResults.Count(r => r.Status == SlotResultStatus.Passed);
        public int FailedSlots => SlotResults.Count(r => r.Status == SlotResultStatus.Failed);
        public double PassRate => TotalSlots > 0 ? (double)PassedSlots / TotalSlots * 100 : 0;
    }

    /// <summary>
    /// Batch result status
    /// </summary>
    public enum BatchResultStatus
    {
        NotStarted,
        Running,
        Completed,
        Aborted,
        Error
    }

    /// <summary>
    /// Result for a single slot
    /// </summary>
    public class SlotResult
    {
        public int SlotNumber { get; set; }
        public string? SerialNumber { get; set; }
        public SlotResultStatus Status { get; set; } = SlotResultStatus.NotRun;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public string? ErrorMessage { get; set; }
        public List<StepResultEntry> StepResults { get; set; } = new();
    }

    /// <summary>
    /// Slot result status
    /// </summary>
    public enum SlotResultStatus
    {
        NotRun,
        Running,
        Passed,
        Failed,
        Skipped,
        Error
    }

    /// <summary>
    /// Step result within a slot
    /// </summary>
    public class StepResultEntry
    {
        public string StepName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public object? Value { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Manager for batch specifications
    /// </summary>
    public class BatchSpecificationManager
    {
        private static readonly Lazy<BatchSpecificationManager> _instance = 
            new(() => new BatchSpecificationManager());
        
        public static BatchSpecificationManager Instance => _instance.Value;

        private readonly Dictionary<string, BatchSpec> _specifications = new();
        private string _specsDirectory = "BatchSpecs";

        private BatchSpecificationManager() { }

        /// <summary>
        /// Sets the specifications directory
        /// </summary>
        public void SetSpecsDirectory(string path)
        {
            _specsDirectory = path;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Creates a new batch specification
        /// </summary>
        public BatchSpec CreateSpecification(string name, int batchSize, BatchMode mode = BatchMode.Sequential)
        {
            var spec = new BatchSpec
            {
                Name = name,
                BatchSize = batchSize,
                Mode = mode
            };

            for (int i = 0; i < batchSize; i++)
            {
                spec.Slots.Add(new BatchSlot
                {
                    SlotNumber = i + 1,
                    Name = $"Slot {i + 1}"
                });
            }

            _specifications[spec.Id] = spec;
            return spec;
        }

        /// <summary>
        /// Saves a specification to file
        /// </summary>
        public void SaveSpecification(BatchSpec spec, string? filePath = null)
        {
            var path = filePath ?? Path.Combine(_specsDirectory, $"{spec.Name}.json");
            spec.ModifiedDate = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(spec, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Loads a specification from file
        /// </summary>
        public BatchSpec LoadSpecification(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Batch specification file not found", filePath);
            }

            var json = File.ReadAllText(filePath);
            var spec = JsonSerializer.Deserialize<BatchSpec>(json);
            if (spec != null)
            {
                _specifications[spec.Id] = spec;
                return spec;
            }

            throw new InvalidOperationException("Failed to deserialize batch specification");
        }

        /// <summary>
        /// Gets a specification by ID
        /// </summary>
        public BatchSpec? GetSpecification(string id)
        {
            return _specifications.TryGetValue(id, out var spec) ? spec : null;
        }

        /// <summary>
        /// Gets all available specifications
        /// </summary>
        public IEnumerable<string> GetAvailableSpecifications()
        {
            if (Directory.Exists(_specsDirectory))
            {
                return Directory.GetFiles(_specsDirectory, "*.json")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(n => n != null)
                    .Cast<string>();
            }
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Validates a batch specification
        /// </summary>
        public BatchSpecValidationResult ValidateSpecification(BatchSpec spec)
        {
            var result = new BatchSpecValidationResult();

            if (string.IsNullOrEmpty(spec.Name))
            {
                result.Errors.Add("Batch specification must have a name");
            }

            if (spec.BatchSize <= 0)
            {
                result.Errors.Add("Batch size must be greater than 0");
            }

            if (spec.Slots.Count != spec.BatchSize)
            {
                result.Errors.Add($"Number of slots ({spec.Slots.Count}) does not match batch size ({spec.BatchSize})");
            }

            var enabledSlots = spec.Slots.Count(s => s.IsEnabled);
            if (enabledSlots < spec.Options.MinimumSlots)
            {
                result.Errors.Add($"At least {spec.Options.MinimumSlots} slots must be enabled");
            }

            if (spec.Options.Timeout <= TimeSpan.Zero)
            {
                result.Warnings.Add("Timeout is set to zero or negative, this may cause issues");
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }

        /// <summary>
        /// Creates a batch execution context
        /// </summary>
        public BatchExecutionContext CreateExecutionContext(BatchSpec spec)
        {
            return new BatchExecutionContext(spec);
        }
    }

    /// <summary>
    /// Batch specification validation result
    /// </summary>
    public class BatchSpecValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Context for batch execution
    /// </summary>
    public class BatchExecutionContext
    {
        private readonly BatchSpec _spec;
        private readonly BatchResult _result;

        public BatchExecutionContext(BatchSpec spec)
        {
            _spec = spec;
            _result = new BatchResult
            {
                BatchSpecId = spec.Id
            };
        }

        public BatchSpec Specification => _spec;
        public BatchResult Result => _result;
        public event EventHandler<SlotStateChangedEventArgs>? SlotStateChanged;
        public event EventHandler<BatchProgressEventArgs>? ProgressChanged;

        /// <summary>
        /// Sets serial number for a slot
        /// </summary>
        public void SetSerialNumber(int slotNumber, string serialNumber)
        {
            var slot = _spec.Slots.FirstOrDefault(s => s.SlotNumber == slotNumber);
            if (slot != null)
            {
                slot.SerialNumber = serialNumber;
            }
        }

        /// <summary>
        /// Sets slot state
        /// </summary>
        public void SetSlotState(int slotNumber, SlotState state)
        {
            var slot = _spec.Slots.FirstOrDefault(s => s.SlotNumber == slotNumber);
            if (slot != null)
            {
                var previousState = slot.State;
                slot.State = state;
                SlotStateChanged?.Invoke(this, new SlotStateChangedEventArgs(slotNumber, previousState, state));
            }
        }

        /// <summary>
        /// Reports progress for a slot
        /// </summary>
        public void ReportProgress(int slotNumber, int percentComplete, string message)
        {
            ProgressChanged?.Invoke(this, new BatchProgressEventArgs(slotNumber, percentComplete, message));
        }

        /// <summary>
        /// Starts batch execution
        /// </summary>
        public void Start()
        {
            _result.StartTime = DateTime.UtcNow;
            _result.Status = BatchResultStatus.Running;
        }

        /// <summary>
        /// Completes batch execution
        /// </summary>
        public void Complete()
        {
            _result.EndTime = DateTime.UtcNow;
            _result.Status = BatchResultStatus.Completed;
        }

        /// <summary>
        /// Aborts batch execution
        /// </summary>
        public void Abort()
        {
            _result.EndTime = DateTime.UtcNow;
            _result.Status = BatchResultStatus.Aborted;
        }

        /// <summary>
        /// Records a slot result
        /// </summary>
        public void RecordSlotResult(SlotResult slotResult)
        {
            _result.SlotResults.Add(slotResult);
        }

        /// <summary>
        /// Gets ready slots
        /// </summary>
        public IEnumerable<BatchSlot> GetReadySlots()
        {
            return _spec.Slots.Where(s => s.IsEnabled && s.State == SlotState.Ready);
        }

        /// <summary>
        /// Checks if all slots are ready
        /// </summary>
        public bool AreAllSlotsReady()
        {
            return _spec.Slots.All(s => !s.IsEnabled || s.State == SlotState.Ready);
        }
    }

    /// <summary>
    /// Event args for slot state change
    /// </summary>
    public class SlotStateChangedEventArgs : EventArgs
    {
        public int SlotNumber { get; }
        public SlotState PreviousState { get; }
        public SlotState NewState { get; }

        public SlotStateChangedEventArgs(int slotNumber, SlotState previousState, SlotState newState)
        {
            SlotNumber = slotNumber;
            PreviousState = previousState;
            NewState = newState;
        }
    }

    /// <summary>
    /// Event args for batch progress
    /// </summary>
    public class BatchProgressEventArgs : EventArgs
    {
        public int SlotNumber { get; }
        public int PercentComplete { get; }
        public string Message { get; }

        public BatchProgressEventArgs(int slotNumber, int percentComplete, string message)
        {
            SlotNumber = slotNumber;
            PercentComplete = percentComplete;
            Message = message;
        }
    }
}
