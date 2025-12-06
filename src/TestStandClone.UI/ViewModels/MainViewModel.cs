using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TestStandClone.Core;
using TestStandClone.Core.Steps;
using TestStandClone.UI.Commands;

namespace TestStandClone.UI.ViewModels
{
    /// <summary>
    /// Main ViewModel for the TestStand application.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly Engine _engine;
        private Sequence _loadedSequence;
        private bool _isRunning;
        private TestStep? _selectedStep;
        private string _executionStatus = "Ready";

        /// <summary>
        /// The currently loaded test sequence.
        /// </summary>
        public Sequence LoadedSequence
        {
            get => _loadedSequence;
            set
            {
                if (_loadedSequence != value)
                {
                    _loadedSequence = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The currently selected step.
        /// </summary>
        public TestStep? SelectedStep
        {
            get => _selectedStep;
            set
            {
                if (_selectedStep != value)
                {
                    _selectedStep = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates whether a sequence is currently running.
        /// </summary>
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanRun));
                    OnPropertyChanged(nameof(CanPause));
                    OnPropertyChanged(nameof(CanResume));
                    OnPropertyChanged(nameof(CanAbort));
                    OnPropertyChanged(nameof(CanStepOver));
                }
            }
        }

        /// <summary>
        /// Current execution status text.
        /// </summary>
        public string ExecutionStatus
        {
            get => _executionStatus;
            set
            {
                if (_executionStatus != value)
                {
                    _executionStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The execution engine instance.
        /// </summary>
        public Engine Engine => _engine;

        #region Execution State Properties

        public bool CanRun => !IsRunning;
        public bool CanPause => IsRunning && !_engine.IsPaused;
        public bool CanResume => IsRunning && _engine.IsPaused;
        public bool CanAbort => IsRunning;
        public bool CanStepOver => IsRunning && _engine.IsPaused;

        #endregion

        #region Commands

        /// <summary>
        /// Command to run the loaded sequence.
        /// </summary>
        public ICommand RunSequenceCommand { get; }

        /// <summary>
        /// Command to pause execution.
        /// </summary>
        public ICommand PauseCommand { get; }

        /// <summary>
        /// Command to resume execution.
        /// </summary>
        public ICommand ResumeCommand { get; }

        /// <summary>
        /// Command to abort execution.
        /// </summary>
        public ICommand AbortCommand { get; }

        /// <summary>
        /// Command to execute a single step.
        /// </summary>
        public ICommand StepOverCommand { get; }

        /// <summary>
        /// Command to reset all steps to idle.
        /// </summary>
        public ICommand ResetCommand { get; }

        /// <summary>
        /// Command to toggle breakpoint on selected step.
        /// </summary>
        public ICommand ToggleBreakpointCommand { get; }

        #endregion

        /// <summary>
        /// Creates a new MainViewModel with a demo sequence.
        /// </summary>
        public MainViewModel()
        {
            _engine = new Engine();
            _loadedSequence = CreateDemoSequence();

            // Subscribe to engine events
            _engine.ExecutionPaused += (s, e) => 
            {
                ExecutionStatus = "Paused";
                RefreshCommandStates();
            };
            _engine.ExecutionResumed += (s, e) => 
            {
                ExecutionStatus = "Running";
                RefreshCommandStates();
            };
            _engine.ExecutionAborted += (s, e) => 
            {
                ExecutionStatus = "Aborted";
                RefreshCommandStates();
            };
            _engine.BeforeStepExecute += (s, e) => 
            {
                ExecutionStatus = $"Executing: {e.Step.Name}";
            };

            // Initialize commands
            RunSequenceCommand = new RelayCommand(
                async () => await RunSequenceAsync(),
                () => CanRun
            );

            PauseCommand = new RelayCommand(
                () => _engine.Pause(),
                () => CanPause
            );

            ResumeCommand = new RelayCommand(
                () => _engine.Resume(),
                () => CanResume
            );

            AbortCommand = new RelayCommand(
                () => _engine.Abort(),
                () => CanAbort
            );

            StepOverCommand = new RelayCommand(
                () => _engine.StepOver(),
                () => CanStepOver
            );

            ResetCommand = new RelayCommand(
                () => 
                {
                    LoadedSequence.Reset();
                    ExecutionStatus = "Ready";
                },
                () => !IsRunning
            );

            ToggleBreakpointCommand = new RelayCommand(
                () => 
                {
                    if (SelectedStep != null)
                    {
                        SelectedStep.HasBreakpoint = !SelectedStep.HasBreakpoint;
                    }
                },
                () => SelectedStep != null
            );
        }

        private void RefreshCommandStates()
        {
            OnPropertyChanged(nameof(CanRun));
            OnPropertyChanged(nameof(CanPause));
            OnPropertyChanged(nameof(CanResume));
            OnPropertyChanged(nameof(CanAbort));
            OnPropertyChanged(nameof(CanStepOver));
        }

        /// <summary>
        /// Creates a demo sequence with a mix of step types including Setup, Main, Cleanup.
        /// </summary>
        private static Sequence CreateDemoSequence()
        {
            var sequence = new Sequence
            {
                Name = "Demo Test Sequence",
                Description = "A demonstration sequence with Setup, Main, and Cleanup groups"
            };

            // Setup Steps
            sequence.SetupSteps.Add(new DelayStep("Initialize Hardware", 300));
            sequence.SetupSteps.Add(new ActionStep("Load Configuration", ctx => 
            {
                ctx.SetValue("Config_Version", "1.0");
                ctx.SetValue("Test_Count", 0);
            }));

            // Main Test Steps
            sequence.MainSteps.Add(new NumericLimitStep("Voltage Check", 4.8, 5.2)
            {
                MinGeneratedValue = 4.5,
                MaxGeneratedValue = 5.5
            });

            sequence.MainSteps.Add(new DelayStep("Wait for Stabilization", 200));

            sequence.MainSteps.Add(new NumericLimitStep("Current Measurement", 0.5, 2.0)
            {
                MinGeneratedValue = 0.3,
                MaxGeneratedValue = 2.5
            });

            sequence.MainSteps.Add(new PassFailStep("Self Test", true));

            sequence.MainSteps.Add(new StringValueStep("Version Check", "1.0")
            {
                ActualValue = "1.0"
            });

            sequence.MainSteps.Add(new NumericLimitStep("Temperature Check", 20.0, 35.0)
            {
                MinGeneratedValue = 15.0,
                MaxGeneratedValue = 40.0
            });

            sequence.MainSteps.Add(new DelayStep("Processing Delay", 400));

            sequence.MainSteps.Add(new NumericLimitStep("Power Consumption", 1.0, 10.0)
            {
                MinGeneratedValue = 0.5,
                MaxGeneratedValue = 12.0
            });

            // Cleanup Steps
            sequence.CleanupSteps.Add(new DelayStep("Cooldown Period", 200));
            sequence.CleanupSteps.Add(new ActionStep("Release Resources", ctx => 
            {
                // Cleanup logic
            }));

            // Sync to legacy Steps collection
            sequence.SyncStepsCollection();

            return sequence;
        }

        /// <summary>
        /// Runs the loaded sequence asynchronously.
        /// </summary>
        private async Task RunSequenceAsync()
        {
            if (IsRunning)
            {
                return;
            }

            try
            {
                IsRunning = true;
                ExecutionStatus = "Running";
                await _engine.ExecuteSequenceAsync(LoadedSequence);
                
                ExecutionStatus = LoadedSequence.Status switch
                {
                    SequenceStatus.Passed => "Passed",
                    SequenceStatus.Failed => "Failed",
                    SequenceStatus.Error => "Error",
                    SequenceStatus.Aborted => "Aborted",
                    _ => "Completed"
                };
            }
            finally
            {
                IsRunning = false;
                RefreshCommandStates();
            }
        }

        #region INotifyPropertyChanged Implementation

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
