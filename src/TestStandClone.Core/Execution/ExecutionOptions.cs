// =============================================================================
// ExecutionOptions.cs - Step and sequence execution options
// Part of TestStandClone.Core - TestStand-like test automation framework
// =============================================================================

using System;
using System.ComponentModel;

namespace TestStandClone.Core.Execution
{
    /// <summary>
    /// Execution options for steps.
    /// Similar to TestStand's step Run Options.
    /// </summary>
    public class StepExecutionOptions : INotifyPropertyChanged
    {
        private RunMode _runMode = RunMode.Normal;
        private bool _ignoreRuntimeErrors;
        private bool _recordResult = true;
        private int _timeoutMs;
        private bool _useTimeLimit;
        private int _loopCount = 1;
        private bool _loopOnPass;
        private bool _loopOnFail;
        private int _loopDelay;

        /// <summary>
        /// Gets or sets the run mode.
        /// </summary>
        public RunMode RunMode
        {
            get => _runMode;
            set { _runMode = value; OnPropertyChanged(nameof(RunMode)); }
        }

        /// <summary>
        /// Gets or sets whether to ignore runtime errors.
        /// </summary>
        public bool IgnoreRuntimeErrors
        {
            get => _ignoreRuntimeErrors;
            set { _ignoreRuntimeErrors = value; OnPropertyChanged(nameof(IgnoreRuntimeErrors)); }
        }

        /// <summary>
        /// Gets or sets whether to record the result.
        /// </summary>
        public bool RecordResult
        {
            get => _recordResult;
            set { _recordResult = value; OnPropertyChanged(nameof(RecordResult)); }
        }

        /// <summary>
        /// Gets or sets the step timeout in milliseconds.
        /// </summary>
        public int TimeoutMs
        {
            get => _timeoutMs;
            set { _timeoutMs = value; OnPropertyChanged(nameof(TimeoutMs)); }
        }

        /// <summary>
        /// Gets or sets whether to enforce time limit.
        /// </summary>
        public bool UseTimeLimit
        {
            get => _useTimeLimit;
            set { _useTimeLimit = value; OnPropertyChanged(nameof(UseTimeLimit)); }
        }

        /// <summary>
        /// Gets or sets the number of times to loop.
        /// </summary>
        public int LoopCount
        {
            get => _loopCount;
            set { _loopCount = Math.Max(1, value); OnPropertyChanged(nameof(LoopCount)); }
        }

        /// <summary>
        /// Gets or sets whether to loop while passing.
        /// </summary>
        public bool LoopOnPass
        {
            get => _loopOnPass;
            set { _loopOnPass = value; OnPropertyChanged(nameof(LoopOnPass)); }
        }

        /// <summary>
        /// Gets or sets whether to loop while failing.
        /// </summary>
        public bool LoopOnFail
        {
            get => _loopOnFail;
            set { _loopOnFail = value; OnPropertyChanged(nameof(LoopOnFail)); }
        }

        /// <summary>
        /// Gets or sets the delay between loop iterations in milliseconds.
        /// </summary>
        public int LoopDelay
        {
            get => _loopDelay;
            set { _loopDelay = Math.Max(0, value); OnPropertyChanged(nameof(LoopDelay)); }
        }

        /// <summary>
        /// Gets or sets the loop exit expression.
        /// </summary>
        public string LoopExitExpression { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the step priority (for parallel execution).
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether the step can run in parallel.
        /// </summary>
        public bool CanRunInParallel { get; set; } = false;

        /// <summary>
        /// Creates default execution options.
        /// </summary>
        public StepExecutionOptions() { }

        /// <summary>
        /// Creates a copy of the execution options.
        /// </summary>
        public StepExecutionOptions Clone()
        {
            return new StepExecutionOptions
            {
                RunMode = RunMode,
                IgnoreRuntimeErrors = IgnoreRuntimeErrors,
                RecordResult = RecordResult,
                TimeoutMs = TimeoutMs,
                UseTimeLimit = UseTimeLimit,
                LoopCount = LoopCount,
                LoopOnPass = LoopOnPass,
                LoopOnFail = LoopOnFail,
                LoopDelay = LoopDelay,
                LoopExitExpression = LoopExitExpression,
                Priority = Priority,
                CanRunInParallel = CanRunInParallel
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Run mode for steps.
    /// </summary>
    public enum RunMode
    {
        /// <summary>Normal execution</summary>
        Normal,
        /// <summary>Skip this step</summary>
        Skip,
        /// <summary>Force pass regardless of result</summary>
        ForcePass,
        /// <summary>Force fail regardless of result</summary>
        ForceFail
    }

    /// <summary>
    /// Execution options for sequences.
    /// Similar to TestStand's sequence properties.
    /// </summary>
    public class SequenceExecutionOptions
    {
        /// <summary>
        /// Gets or sets the default step failure action.
        /// </summary>
        public FailureAction DefaultFailureAction { get; set; } = FailureAction.Continue;

        /// <summary>
        /// Gets or sets whether to run cleanup on abort.
        /// </summary>
        public bool RunCleanupOnAbort { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to run cleanup on error.
        /// </summary>
        public bool RunCleanupOnError { get; set; } = true;

        /// <summary>
        /// Gets or sets the sequence timeout in milliseconds.
        /// </summary>
        public int TimeoutMs { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether to record sequence results.
        /// </summary>
        public bool RecordResults { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to allow parallel step execution.
        /// </summary>
        public bool AllowParallelExecution { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum parallel steps.
        /// </summary>
        public int MaxParallelSteps { get; set; } = 4;

        /// <summary>
        /// Gets or sets whether to trace execution.
        /// </summary>
        public bool TraceExecution { get; set; } = false;

        /// <summary>
        /// Gets or sets the trace level.
        /// </summary>
        public TraceLevel TraceLevel { get; set; } = TraceLevel.Basic;

        /// <summary>
        /// Gets or sets whether to break on first failure.
        /// </summary>
        public bool BreakOnFailure { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to prompt for UUT serial number.
        /// </summary>
        public bool PromptForSerialNumber { get; set; } = true;

        /// <summary>
        /// Gets or sets the interactive execution mode.
        /// </summary>
        public InteractiveMode InteractiveMode { get; set; } = InteractiveMode.None;
    }

    /// <summary>
    /// Failure actions.
    /// </summary>
    public enum FailureAction
    {
        /// <summary>Continue to next step</summary>
        Continue,
        /// <summary>Stop execution immediately</summary>
        Stop,
        /// <summary>Jump to cleanup</summary>
        GotoCleanup,
        /// <summary>Retry the step</summary>
        Retry,
        /// <summary>Ask user what to do</summary>
        Prompt
    }

    /// <summary>
    /// Trace levels.
    /// </summary>
    public enum TraceLevel
    {
        /// <summary>No tracing</summary>
        None,
        /// <summary>Basic tracing (step start/end)</summary>
        Basic,
        /// <summary>Detailed tracing (includes parameters)</summary>
        Detailed,
        /// <summary>Verbose tracing (includes internal state)</summary>
        Verbose
    }

    /// <summary>
    /// Interactive execution modes.
    /// </summary>
    public enum InteractiveMode
    {
        /// <summary>No interactive mode</summary>
        None,
        /// <summary>Single step mode</summary>
        SingleStep,
        /// <summary>Break at breakpoints only</summary>
        BreakpointsOnly,
        /// <summary>Break on failure</summary>
        BreakOnFailure
    }

    /// <summary>
    /// Execution state information.
    /// </summary>
    public class ExecutionState
    {
        /// <summary>
        /// Gets or sets the current execution phase.
        /// </summary>
        public ExecutionPhase Phase { get; set; } = ExecutionPhase.Idle;

        /// <summary>
        /// Gets or sets the current sequence.
        /// </summary>
        public Sequence? CurrentSequence { get; set; }

        /// <summary>
        /// Gets or sets the current step index.
        /// </summary>
        public int CurrentStepIndex { get; set; } = -1;

        /// <summary>
        /// Gets or sets the current step.
        /// </summary>
        public TestStep? CurrentStep { get; set; }

        /// <summary>
        /// Gets or sets the current group name.
        /// </summary>
        public string CurrentGroup { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the loop iteration.
        /// </summary>
        public int LoopIteration { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether execution is paused.
        /// </summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// Gets or sets whether execution is aborted.
        /// </summary>
        public bool IsAborted { get; set; }

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets the elapsed time.
        /// </summary>
        public TimeSpan ElapsedTime => EndTime > StartTime ? EndTime - StartTime : DateTime.Now - StartTime;

        /// <summary>
        /// Gets or sets the total step count.
        /// </summary>
        public int TotalSteps { get; set; }

        /// <summary>
        /// Gets or sets the completed step count.
        /// </summary>
        public int CompletedSteps { get; set; }

        /// <summary>
        /// Gets or sets the passed step count.
        /// </summary>
        public int PassedSteps { get; set; }

        /// <summary>
        /// Gets or sets the failed step count.
        /// </summary>
        public int FailedSteps { get; set; }

        /// <summary>
        /// Gets the progress percentage.
        /// </summary>
        public double Progress => TotalSteps > 0 ? (double)CompletedSteps / TotalSteps * 100 : 0;

        /// <summary>
        /// Gets or sets the last error message.
        /// </summary>
        public string LastError { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UUT serial number.
        /// </summary>
        public string UUTSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// Resets the execution state.
        /// </summary>
        public void Reset()
        {
            Phase = ExecutionPhase.Idle;
            CurrentStepIndex = -1;
            CurrentStep = null;
            LoopIteration = 0;
            IsPaused = false;
            IsAborted = false;
            StartTime = DateTime.MinValue;
            EndTime = DateTime.MinValue;
            CompletedSteps = 0;
            PassedSteps = 0;
            FailedSteps = 0;
            LastError = string.Empty;
        }
    }

    /// <summary>
    /// Execution phases.
    /// </summary>
    public enum ExecutionPhase
    {
        /// <summary>Not running</summary>
        Idle,
        /// <summary>Initializing</summary>
        Initializing,
        /// <summary>Running setup</summary>
        Setup,
        /// <summary>Running main</summary>
        Main,
        /// <summary>Running cleanup</summary>
        Cleanup,
        /// <summary>Finalizing</summary>
        Finalizing,
        /// <summary>Completed</summary>
        Completed,
        /// <summary>Aborted</summary>
        Aborted,
        /// <summary>Error</summary>
        Error
    }
}
