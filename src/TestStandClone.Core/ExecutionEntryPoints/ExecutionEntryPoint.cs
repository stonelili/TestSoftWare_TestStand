using TestStandClone.Core.ProcessModels;

namespace TestStandClone.Core.ExecutionEntryPoints
{
    /// <summary>
    /// Defines the mode of execution entry point.
    /// </summary>
    public enum ExecutionMode
    {
        /// <summary>
        /// Single pass execution - run once and stop.
        /// </summary>
        SinglePass,

        /// <summary>
        /// UUT loop - continuously run for each UUT until stopped.
        /// </summary>
        UUTLoop,

        /// <summary>
        /// Batch mode - run for a specified number of UUTs.
        /// </summary>
        Batch,

        /// <summary>
        /// Run selected steps only.
        /// </summary>
        RunSelectedSteps,

        /// <summary>
        /// Run main sequence only (skip setup/cleanup).
        /// </summary>
        MainSequenceOnly,

        /// <summary>
        /// Debug mode - start paused and single step.
        /// </summary>
        Debug
    }

    /// <summary>
    /// Configuration for an execution entry point.
    /// </summary>
    public class ExecutionEntryPointConfig
    {
        /// <summary>
        /// Name of the entry point.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the entry point.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Execution mode.
        /// </summary>
        public ExecutionMode Mode { get; set; } = ExecutionMode.SinglePass;

        /// <summary>
        /// Number of UUTs to test (for Batch mode).
        /// </summary>
        public int BatchSize { get; set; } = 1;

        /// <summary>
        /// Whether to prompt for serial number before each UUT.
        /// </summary>
        public bool PromptForSerialNumber { get; set; } = true;

        /// <summary>
        /// Whether to run setup sequence.
        /// </summary>
        public bool RunSetup { get; set; } = true;

        /// <summary>
        /// Whether to run cleanup sequence.
        /// </summary>
        public bool RunCleanup { get; set; } = true;

        /// <summary>
        /// Whether to log results to database.
        /// </summary>
        public bool LogToDatabase { get; set; } = true;

        /// <summary>
        /// Whether to generate report after execution.
        /// </summary>
        public bool GenerateReport { get; set; } = false;

        /// <summary>
        /// Process model to use.
        /// </summary>
        public ProcessModelType ProcessModel { get; set; } = ProcessModelType.Sequential;

        /// <summary>
        /// Number of test sockets (for parallel execution).
        /// </summary>
        public int NumberOfTestSockets { get; set; } = 1;
    }

    /// <summary>
    /// Defines the type of process model.
    /// </summary>
    public enum ProcessModelType
    {
        Sequential,
        Parallel,
        Batch
    }

    /// <summary>
    /// Manages execution entry points similar to TestStand.
    /// </summary>
    public class ExecutionEntryPointManager
    {
        private readonly Engine _engine;
        private readonly Dictionary<string, ExecutionEntryPointConfig> _entryPoints;
        private bool _isExecuting;
        private bool _stopRequested;
        private int _uutsProcessed;
        private int _uutsPassed;
        private int _uutsFailed;

        /// <summary>
        /// Event raised when a UUT execution is completed.
        /// </summary>
        public event EventHandler<UUTCompletedEventArgs>? UUTCompleted;

        /// <summary>
        /// Event raised when serial number input is requested.
        /// </summary>
        public event EventHandler<SerialNumberRequestEventArgs>? SerialNumberRequested;

        /// <summary>
        /// Event raised when execution stops.
        /// </summary>
        public event EventHandler? ExecutionStopped;

        /// <summary>
        /// Gets whether execution is in progress.
        /// </summary>
        public bool IsExecuting => _isExecuting;

        /// <summary>
        /// Gets the number of UUTs processed.
        /// </summary>
        public int UUTsProcessed => _uutsProcessed;

        /// <summary>
        /// Gets the number of UUTs passed.
        /// </summary>
        public int UUTsPassed => _uutsPassed;

        /// <summary>
        /// Gets the number of UUTs failed.
        /// </summary>
        public int UUTsFailed => _uutsFailed;

        /// <summary>
        /// Creates a new ExecutionEntryPointManager.
        /// </summary>
        public ExecutionEntryPointManager(Engine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _entryPoints = new Dictionary<string, ExecutionEntryPointConfig>();
            InitializeDefaultEntryPoints();
        }

        /// <summary>
        /// Initializes default entry points.
        /// </summary>
        private void InitializeDefaultEntryPoints()
        {
            // Single Pass
            _entryPoints["Single Pass"] = new ExecutionEntryPointConfig
            {
                Name = "Single Pass",
                Description = "Execute the sequence once for a single UUT",
                Mode = ExecutionMode.SinglePass,
                PromptForSerialNumber = true,
                RunSetup = true,
                RunCleanup = true
            };

            // UUT Loop
            _entryPoints["Test UUTs"] = new ExecutionEntryPointConfig
            {
                Name = "Test UUTs",
                Description = "Continuously test UUTs until stopped",
                Mode = ExecutionMode.UUTLoop,
                PromptForSerialNumber = true,
                RunSetup = true,
                RunCleanup = true
            };

            // Debug
            _entryPoints["Debug"] = new ExecutionEntryPointConfig
            {
                Name = "Debug",
                Description = "Execute in debug mode with single stepping",
                Mode = ExecutionMode.Debug,
                PromptForSerialNumber = false,
                RunSetup = true,
                RunCleanup = true
            };

            // Main Sequence Only
            _entryPoints["Run MainSequence"] = new ExecutionEntryPointConfig
            {
                Name = "Run MainSequence",
                Description = "Execute main sequence only, skip setup and cleanup",
                Mode = ExecutionMode.MainSequenceOnly,
                PromptForSerialNumber = false,
                RunSetup = false,
                RunCleanup = false
            };

            // Batch
            _entryPoints["Batch Test"] = new ExecutionEntryPointConfig
            {
                Name = "Batch Test",
                Description = "Test a batch of UUTs",
                Mode = ExecutionMode.Batch,
                BatchSize = 10,
                PromptForSerialNumber = true,
                RunSetup = true,
                RunCleanup = true
            };
        }

        /// <summary>
        /// Gets all available entry points.
        /// </summary>
        public IReadOnlyDictionary<string, ExecutionEntryPointConfig> GetEntryPoints()
        {
            return _entryPoints;
        }

        /// <summary>
        /// Gets an entry point by name.
        /// </summary>
        public ExecutionEntryPointConfig? GetEntryPoint(string name)
        {
            return _entryPoints.TryGetValue(name, out var config) ? config : null;
        }

        /// <summary>
        /// Adds a custom entry point.
        /// </summary>
        public void AddEntryPoint(ExecutionEntryPointConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            _entryPoints[config.Name] = config;
        }

        /// <summary>
        /// Executes using the specified entry point.
        /// </summary>
        public async Task ExecuteAsync(string entryPointName, Sequence sequence)
        {
            if (!_entryPoints.TryGetValue(entryPointName, out var config))
            {
                throw new ArgumentException($"Entry point '{entryPointName}' not found.");
            }

            await ExecuteAsync(config, sequence);
        }

        /// <summary>
        /// Executes using the specified entry point configuration.
        /// </summary>
        public async Task ExecuteAsync(ExecutionEntryPointConfig config, Sequence sequence)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            _isExecuting = true;
            _stopRequested = false;
            _uutsProcessed = 0;
            _uutsPassed = 0;
            _uutsFailed = 0;

            try
            {
                switch (config.Mode)
                {
                    case ExecutionMode.SinglePass:
                        await ExecuteSinglePassAsync(config, sequence);
                        break;

                    case ExecutionMode.UUTLoop:
                        await ExecuteUUTLoopAsync(config, sequence);
                        break;

                    case ExecutionMode.Batch:
                        await ExecuteBatchAsync(config, sequence);
                        break;

                    case ExecutionMode.Debug:
                        await ExecuteDebugAsync(config, sequence);
                        break;

                    case ExecutionMode.MainSequenceOnly:
                        await ExecuteMainOnlyAsync(config, sequence);
                        break;

                    default:
                        await ExecuteSinglePassAsync(config, sequence);
                        break;
                }
            }
            finally
            {
                _isExecuting = false;
                ExecutionStopped?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Stops the current execution loop.
        /// </summary>
        public void Stop()
        {
            _stopRequested = true;
            _engine.Abort();
        }

        /// <summary>
        /// Executes a single pass.
        /// </summary>
        private async Task ExecuteSinglePassAsync(ExecutionEntryPointConfig config, Sequence sequence)
        {
            string serialNumber = string.Empty;
            
            if (config.PromptForSerialNumber)
            {
                serialNumber = await RequestSerialNumberAsync();
                if (string.IsNullOrEmpty(serialNumber) && _stopRequested)
                {
                    return;
                }
            }

            await ExecuteSequenceAsync(sequence, serialNumber, config);
        }

        /// <summary>
        /// Executes in UUT loop mode.
        /// </summary>
        private async Task ExecuteUUTLoopAsync(ExecutionEntryPointConfig config, Sequence sequence)
        {
            while (!_stopRequested)
            {
                string serialNumber = string.Empty;
                
                if (config.PromptForSerialNumber)
                {
                    serialNumber = await RequestSerialNumberAsync();
                    if (string.IsNullOrEmpty(serialNumber) && _stopRequested)
                    {
                        break;
                    }
                }

                await ExecuteSequenceAsync(sequence, serialNumber, config);

                if (_stopRequested)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Executes in batch mode.
        /// </summary>
        private async Task ExecuteBatchAsync(ExecutionEntryPointConfig config, Sequence sequence)
        {
            for (int i = 0; i < config.BatchSize && !_stopRequested; i++)
            {
                string serialNumber = string.Empty;
                
                if (config.PromptForSerialNumber)
                {
                    serialNumber = await RequestSerialNumberAsync();
                    if (string.IsNullOrEmpty(serialNumber) && _stopRequested)
                    {
                        break;
                    }
                }

                await ExecuteSequenceAsync(sequence, serialNumber, config);
            }
        }

        /// <summary>
        /// Executes in debug mode.
        /// </summary>
        private async Task ExecuteDebugAsync(ExecutionEntryPointConfig config, Sequence sequence)
        {
            // Start paused for debugging
            _engine.Pause();
            await ExecuteSequenceAsync(sequence, "DEBUG", config);
        }

        /// <summary>
        /// Executes main sequence only.
        /// </summary>
        private async Task ExecuteMainOnlyAsync(ExecutionEntryPointConfig config, Sequence sequence)
        {
            // Temporarily disable setup and cleanup
            var setupSteps = sequence.SetupSteps.ToList();
            var cleanupSteps = sequence.CleanupSteps.ToList();

            sequence.SetupSteps.Clear();
            sequence.CleanupSteps.Clear();

            try
            {
                await ExecuteSequenceAsync(sequence, string.Empty, config);
            }
            finally
            {
                // Restore setup and cleanup
                foreach (var step in setupSteps)
                {
                    sequence.SetupSteps.Add(step);
                }
                foreach (var step in cleanupSteps)
                {
                    sequence.CleanupSteps.Add(step);
                }
            }
        }

        /// <summary>
        /// Executes the sequence with the specified configuration.
        /// </summary>
        private async Task ExecuteSequenceAsync(Sequence sequence, string serialNumber, ExecutionEntryPointConfig config)
        {
            sequence.Reset();

            await _engine.ExecuteSequenceAsync(sequence);

            _uutsProcessed++;
            if (sequence.Status == SequenceStatus.Passed)
            {
                _uutsPassed++;
            }
            else
            {
                _uutsFailed++;
            }

            UUTCompleted?.Invoke(this, new UUTCompletedEventArgs(
                serialNumber,
                sequence.Status == SequenceStatus.Passed,
                sequence.Status.ToString()
            ));
        }

        /// <summary>
        /// Requests a serial number from the user.
        /// </summary>
        private Task<string> RequestSerialNumberAsync()
        {
            var tcs = new TaskCompletionSource<string>();
            var args = new SerialNumberRequestEventArgs(tcs);
            SerialNumberRequested?.Invoke(this, args);
            
            // If no handler is registered, return empty
            if (SerialNumberRequested == null)
            {
                tcs.SetResult(string.Empty);
            }

            return tcs.Task;
        }
    }

    /// <summary>
    /// Event args for UUT completion.
    /// </summary>
    public class UUTCompletedEventArgs : EventArgs
    {
        public string SerialNumber { get; }
        public bool Passed { get; }
        public string Status { get; }

        public UUTCompletedEventArgs(string serialNumber, bool passed, string status)
        {
            SerialNumber = serialNumber;
            Passed = passed;
            Status = status;
        }
    }

    /// <summary>
    /// Event args for serial number request.
    /// </summary>
    public class SerialNumberRequestEventArgs : EventArgs
    {
        private readonly TaskCompletionSource<string> _tcs;

        public SerialNumberRequestEventArgs(TaskCompletionSource<string> tcs)
        {
            _tcs = tcs;
        }

        /// <summary>
        /// Sets the serial number.
        /// </summary>
        public void SetSerialNumber(string serialNumber)
        {
            _tcs.TrySetResult(serialNumber);
        }

        /// <summary>
        /// Cancels the request.
        /// </summary>
        public void Cancel()
        {
            _tcs.TrySetResult(string.Empty);
        }
    }
}
