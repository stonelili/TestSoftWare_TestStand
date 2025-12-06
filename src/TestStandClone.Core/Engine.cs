using System.ComponentModel;
using System.Runtime.CompilerServices;
using TestStandClone.Core.Steps;

namespace TestStandClone.Core
{
    /// <summary>
    /// Execution engine for running test sequences.
    /// Similar to TestStand's execution engine with support for pause, resume, abort, and breakpoints.
    /// </summary>
    public class Engine : INotifyPropertyChanged
    {
        private bool _isPaused;
        private bool _isAborted;
        private bool _isSingleStepping;
        private TestStep? _currentStep;
        private readonly SemaphoreSlim _pauseSemaphore = new SemaphoreSlim(0, 1);
        private readonly object _lockObject = new object();

        /// <summary>
        /// Event raised when execution is paused.
        /// </summary>
        public event EventHandler? ExecutionPaused;

        /// <summary>
        /// Event raised when execution is resumed.
        /// </summary>
        public event EventHandler? ExecutionResumed;

        /// <summary>
        /// Event raised when execution is aborted.
        /// </summary>
        public event EventHandler? ExecutionAborted;

        /// <summary>
        /// Event raised before each step executes.
        /// </summary>
        public event EventHandler<StepEventArgs>? BeforeStepExecute;

        /// <summary>
        /// Event raised after each step executes.
        /// </summary>
        public event EventHandler<StepEventArgs>? AfterStepExecute;

        /// <summary>
        /// Safely releases the pause semaphore if not already released.
        /// </summary>
        private void TryReleaseSemaphore()
        {
            try
            {
                _pauseSemaphore.Release();
            }
            catch (SemaphoreFullException)
            {
                // Semaphore was already released - this is expected when multiple
                // resume/abort calls occur before the waiting thread can acquire it
            }
        }

        /// <summary>
        /// Gets whether execution is currently paused.
        /// </summary>
        public bool IsPaused
        {
            get => _isPaused;
            private set
            {
                if (_isPaused != value)
                {
                    _isPaused = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets whether execution has been aborted.
        /// </summary>
        public bool IsAborted
        {
            get => _isAborted;
            private set
            {
                if (_isAborted != value)
                {
                    _isAborted = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the currently executing step.
        /// </summary>
        public TestStep? CurrentStep
        {
            get => _currentStep;
            private set
            {
                if (_currentStep != value)
                {
                    _currentStep = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Pauses execution.
        /// </summary>
        public void Pause()
        {
            lock (_lockObject)
            {
                if (!IsPaused && !IsAborted)
                {
                    IsPaused = true;
                    ExecutionPaused?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Resumes execution after pause.
        /// </summary>
        public void Resume()
        {
            lock (_lockObject)
            {
                if (IsPaused)
                {
                    IsPaused = false;
                    _isSingleStepping = false;
                    TryReleaseSemaphore();
                    ExecutionResumed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Executes a single step and pauses.
        /// </summary>
        public void StepOver()
        {
            lock (_lockObject)
            {
                if (IsPaused)
                {
                    _isSingleStepping = true;
                    TryReleaseSemaphore();
                }
            }
        }

        /// <summary>
        /// Aborts execution.
        /// </summary>
        public void Abort()
        {
            lock (_lockObject)
            {
                IsAborted = true;
                if (IsPaused)
                {
                    IsPaused = false;
                    TryReleaseSemaphore();
                }
                ExecutionAborted?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Resets the engine state.
        /// </summary>
        public void Reset()
        {
            lock (_lockObject)
            {
                IsPaused = false;
                IsAborted = false;
                _isSingleStepping = false;
                CurrentStep = null;
            }
        }

        /// <summary>
        /// Executes all steps in a sequence asynchronously with Setup, Main, Cleanup groups.
        /// </summary>
        /// <param name="sequence">The sequence to execute.</param>
        /// <returns>A task representing the async operation.</returns>
        public async Task ExecuteSequenceAsync(Sequence sequence)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            Reset();

            // Create a context for this execution
            var context = new Context();

            // Reset all steps before execution
            sequence.Reset();
            sequence.Status = SequenceStatus.Running;
            sequence.StartTime = DateTime.Now;

            bool setupPassed = true;
            bool mainPassed = true;

            try
            {
                // Execute Setup steps
                if (sequence.SetupSteps.Count > 0)
                {
                    setupPassed = await ExecuteStepGroupAsync(sequence.SetupSteps, context);
                }

                // Execute Main steps only if Setup passed
                if (setupPassed && !IsAborted)
                {
                    if (sequence.MainSteps.Count > 0)
                    {
                        mainPassed = await ExecuteStepGroupAsync(sequence.MainSteps, context);
                    }
                    else if (sequence.Steps.Count > 0)
                    {
                        // Fallback to Steps for backward compatibility
                        mainPassed = await ExecuteStepGroupAsync(sequence.Steps, context);
                    }
                }

                // Always execute Cleanup steps
                if (sequence.CleanupSteps.Count > 0 && !IsAborted)
                {
                    await ExecuteStepGroupAsync(sequence.CleanupSteps, context);
                }

                // Set final sequence status
                sequence.EndTime = DateTime.Now;
                if (IsAborted)
                {
                    sequence.Status = SequenceStatus.Aborted;
                }
                else if (!setupPassed || !mainPassed)
                {
                    sequence.Status = SequenceStatus.Failed;
                }
                else
                {
                    sequence.Status = SequenceStatus.Passed;
                }
            }
            catch (Exception)
            {
                sequence.EndTime = DateTime.Now;
                sequence.Status = SequenceStatus.Error;
                throw;
            }
        }

        /// <summary>
        /// Executes a group of steps.
        /// </summary>
        private async Task<bool> ExecuteStepGroupAsync(IList<TestStep> steps, Context context)
        {
            bool allPassed = true;
            int currentIndex = 0;

            while (currentIndex < steps.Count && !IsAborted)
            {
                var step = steps[currentIndex];
                CurrentStep = step;

                // Check for breakpoint
                if (step.HasBreakpoint)
                {
                    Pause();
                }

                // Wait if paused
                await WaitIfPausedAsync();

                if (IsAborted)
                {
                    step.Status = StepStatus.Idle;
                    step.ResultText = "Aborted";
                    break;
                }

                BeforeStepExecute?.Invoke(this, new StepEventArgs(step));

                try
                {
                    // Update status to Running
                    step.Status = StepStatus.Running;
                    step.ResultText = "Executing...";
                    step.StartTime = DateTime.Now;

                    // Execute the step
                    await step.ExecuteAsync(context);

                    step.EndTime = DateTime.Now;

                    // If status wasn't set by the step, mark as Passed
                    if (step.Status == StepStatus.Running)
                    {
                        step.Status = StepStatus.Passed;
                    }

                    // Check for flow control
                    if (step is IFlowControlStep flowControl && flowControl.RequestsFlowChange)
                    {
                        int targetIndex = FindLabelIndex(steps, flowControl.TargetLabelId);
                        if (targetIndex >= 0)
                        {
                            currentIndex = targetIndex;
                            continue;
                        }
                    }

                    // Track if any step failed
                    if (step.Status == StepStatus.Failed || step.Status == StepStatus.Error)
                    {
                        allPassed = false;
                    }
                }
                catch (Exception ex)
                {
                    // Handle execution errors
                    step.EndTime = DateTime.Now;
                    step.Status = StepStatus.Error;
                    step.ResultText = $"Error: {ex.Message}";
                    allPassed = false;
                }

                AfterStepExecute?.Invoke(this, new StepEventArgs(step));

                // Auto-pause after step if single-stepping
                if (_isSingleStepping)
                {
                    _isSingleStepping = false;
                    Pause();
                }

                currentIndex++;
            }

            CurrentStep = null;
            return allPassed;
        }

        /// <summary>
        /// Finds the index of a label step in the step list.
        /// </summary>
        private static int FindLabelIndex(IList<TestStep> steps, string labelId)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] is LabelStep label && label.LabelId == labelId)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Waits if execution is paused.
        /// </summary>
        private async Task WaitIfPausedAsync()
        {
            while (IsPaused && !IsAborted)
            {
                await _pauseSemaphore.WaitAsync();
            }
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Event arguments for step execution events.
    /// </summary>
    public class StepEventArgs : EventArgs
    {
        public TestStep Step { get; }

        public StepEventArgs(TestStep step)
        {
            Step = step;
        }
    }
}
