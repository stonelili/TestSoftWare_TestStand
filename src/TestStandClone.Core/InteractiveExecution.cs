using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace TestStandClone.Core
{
    /// <summary>
    /// Interactive execution modes
    /// </summary>
    public enum InteractiveMode
    {
        /// <summary>Normal execution</summary>
        Normal,
        /// <summary>Run to cursor (selected step)</summary>
        RunToCursor,
        /// <summary>Run from cursor</summary>
        RunFromCursor,
        /// <summary>Run selected steps only</summary>
        RunSelectedSteps,
        /// <summary>Skip step and continue</summary>
        SkipStep,
        /// <summary>Force pass current step</summary>
        ForcePass,
        /// <summary>Force fail current step</summary>
        ForceFail,
        /// <summary>Restart current step</summary>
        RestartStep
    }

    /// <summary>
    /// Watch variable for monitoring during execution
    /// </summary>
    public class WatchVariable : INotifyPropertyChanged
    {
        private object? _value;
        private DateTime _lastUpdated;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name { get; set; } = string.Empty;
        public string Expression { get; set; } = string.Empty;
        
        public object? Value
        {
            get => _value;
            set { _value = value; _lastUpdated = DateTime.Now; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayValue)); }
        }
        
        public string DisplayValue => _value?.ToString() ?? "(null)";
        public DateTime LastUpdated => _lastUpdated;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Expression watch window for debugging
    /// </summary>
    public class WatchWindow : INotifyPropertyChanged
    {
        private readonly List<WatchVariable> _watches = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public IReadOnlyList<WatchVariable> Watches => _watches;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Add a watch expression
        /// </summary>
        public WatchVariable AddWatch(string name, string expression)
        {
            var watch = new WatchVariable { Name = name, Expression = expression };
            _watches.Add(watch);
            OnPropertyChanged(nameof(Watches));
            return watch;
        }

        /// <summary>
        /// Remove a watch
        /// </summary>
        public bool RemoveWatch(WatchVariable watch)
        {
            var result = _watches.Remove(watch);
            OnPropertyChanged(nameof(Watches));
            return result;
        }

        /// <summary>
        /// Clear all watches
        /// </summary>
        public void ClearWatches()
        {
            _watches.Clear();
            OnPropertyChanged(nameof(Watches));
        }

        /// <summary>
        /// Update all watch values from context
        /// </summary>
        public void UpdateFromContext(Context context)
        {
            foreach (var watch in _watches)
            {
                try
                {
                    var value = context.GetValue<object>(watch.Expression);
                    watch.Value = value;
                }
                catch
                {
                    watch.Value = "(error)";
                }
            }
        }
    }

    /// <summary>
    /// Execution stack frame for debugging
    /// </summary>
    public class StackFrame
    {
        public string SequenceName { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public int StepIndex { get; set; }
        public string Group { get; set; } = "Main";
        public DateTime EntryTime { get; set; } = DateTime.Now;
        public Dictionary<string, object> LocalVariables { get; set; } = new();
    }

    /// <summary>
    /// Call stack for execution debugging
    /// </summary>
    public class CallStack : INotifyPropertyChanged
    {
        private readonly Stack<StackFrame> _frames = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public IEnumerable<StackFrame> Frames => _frames;
        public int Depth => _frames.Count;
        public StackFrame? CurrentFrame => _frames.Count > 0 ? _frames.Peek() : null;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Push a new frame onto the stack
        /// </summary>
        public void Push(StackFrame frame)
        {
            _frames.Push(frame);
            OnPropertyChanged(nameof(Frames));
            OnPropertyChanged(nameof(Depth));
            OnPropertyChanged(nameof(CurrentFrame));
        }

        /// <summary>
        /// Pop the current frame from the stack
        /// </summary>
        public StackFrame? Pop()
        {
            if (_frames.Count == 0) return null;
            var frame = _frames.Pop();
            OnPropertyChanged(nameof(Frames));
            OnPropertyChanged(nameof(Depth));
            OnPropertyChanged(nameof(CurrentFrame));
            return frame;
        }

        /// <summary>
        /// Clear the stack
        /// </summary>
        public void Clear()
        {
            _frames.Clear();
            OnPropertyChanged(nameof(Frames));
            OnPropertyChanged(nameof(Depth));
            OnPropertyChanged(nameof(CurrentFrame));
        }
    }

    /// <summary>
    /// Interactive execution controller
    /// Provides debugging-like features for sequence execution
    /// </summary>
    public class InteractiveExecutionController : INotifyPropertyChanged
    {
        private InteractiveMode _mode = InteractiveMode.Normal;
        private int _targetStepIndex = -1;
        private HashSet<string> _selectedStepIds = new();
        private bool _isPaused;
        private TestStep? _currentStep;
        private readonly TaskCompletionSource<bool> _resumeSignal = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<TestStep>? StepExecutionRequested;
        public event EventHandler<string>? MessageReceived;

        public WatchWindow WatchWindow { get; } = new();
        public CallStack CallStack { get; } = new();

        public InteractiveMode Mode
        {
            get => _mode;
            set { _mode = value; OnPropertyChanged(); }
        }

        public bool IsPaused
        {
            get => _isPaused;
            set { _isPaused = value; OnPropertyChanged(); }
        }

        public TestStep? CurrentStep
        {
            get => _currentStep;
            set { _currentStep = value; OnPropertyChanged(); }
        }

        public int TargetStepIndex
        {
            get => _targetStepIndex;
            set { _targetStepIndex = value; OnPropertyChanged(); }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Set mode to run to a specific step
        /// </summary>
        public void RunToCursor(int stepIndex)
        {
            Mode = InteractiveMode.RunToCursor;
            TargetStepIndex = stepIndex;
        }

        /// <summary>
        /// Set mode to run from a specific step
        /// </summary>
        public void RunFromCursor(int stepIndex)
        {
            Mode = InteractiveMode.RunFromCursor;
            TargetStepIndex = stepIndex;
        }

        /// <summary>
        /// Set selected steps to run
        /// </summary>
        public void RunSelectedSteps(IEnumerable<string> stepIds)
        {
            Mode = InteractiveMode.RunSelectedSteps;
            _selectedStepIds = new HashSet<string>(stepIds);
        }

        /// <summary>
        /// Check if a step should be executed based on current mode
        /// </summary>
        public bool ShouldExecuteStep(int stepIndex, TestStep step)
        {
            switch (Mode)
            {
                case InteractiveMode.RunToCursor:
                    return stepIndex <= TargetStepIndex;
                    
                case InteractiveMode.RunFromCursor:
                    return stepIndex >= TargetStepIndex;
                    
                case InteractiveMode.RunSelectedSteps:
                    return _selectedStepIds.Contains(step.Id.ToString());
                    
                case InteractiveMode.SkipStep:
                    if (step == CurrentStep)
                    {
                        Mode = InteractiveMode.Normal;
                        return false;
                    }
                    return true;
                    
                default:
                    return true;
            }
        }

        /// <summary>
        /// Force the current step to pass
        /// </summary>
        public void ForcePassCurrentStep()
        {
            if (CurrentStep != null)
            {
                CurrentStep.Status = StepStatus.Passed;
                CurrentStep.ResultText = "Forced Pass";
                MessageReceived?.Invoke(this, $"Step '{CurrentStep.Name}' forced to Pass");
            }
        }

        /// <summary>
        /// Force the current step to fail
        /// </summary>
        public void ForceFailCurrentStep()
        {
            if (CurrentStep != null)
            {
                CurrentStep.Status = StepStatus.Failed;
                CurrentStep.ResultText = "Forced Fail";
                MessageReceived?.Invoke(this, $"Step '{CurrentStep.Name}' forced to Fail");
            }
        }

        /// <summary>
        /// Skip the current step
        /// </summary>
        public void SkipCurrentStep()
        {
            if (CurrentStep != null)
            {
                CurrentStep.Status = StepStatus.Idle;
                CurrentStep.ResultText = "Skipped";
                Mode = InteractiveMode.SkipStep;
                MessageReceived?.Invoke(this, $"Step '{CurrentStep.Name}' skipped");
            }
        }

        /// <summary>
        /// Request restart of current step
        /// </summary>
        public void RestartCurrentStep()
        {
            if (CurrentStep != null)
            {
                StepExecutionRequested?.Invoke(this, CurrentStep);
                MessageReceived?.Invoke(this, $"Step '{CurrentStep.Name}' restarted");
            }
        }

        /// <summary>
        /// Reset to normal mode
        /// </summary>
        public void Reset()
        {
            Mode = InteractiveMode.Normal;
            TargetStepIndex = -1;
            _selectedStepIds.Clear();
            IsPaused = false;
            CurrentStep = null;
            CallStack.Clear();
        }

        /// <summary>
        /// Enter a sequence (push stack frame)
        /// </summary>
        public void EnterSequence(string sequenceName, string stepName, int stepIndex, string group = "Main")
        {
            CallStack.Push(new StackFrame
            {
                SequenceName = sequenceName,
                StepName = stepName,
                StepIndex = stepIndex,
                Group = group
            });
        }

        /// <summary>
        /// Exit the current sequence (pop stack frame)
        /// </summary>
        public StackFrame? ExitSequence()
        {
            return CallStack.Pop();
        }

        /// <summary>
        /// Update watches from context
        /// </summary>
        public void UpdateWatches(Context context)
        {
            WatchWindow.UpdateFromContext(context);
        }

        /// <summary>
        /// Add a quick watch
        /// </summary>
        public void QuickWatch(string expression, Context context)
        {
            var watch = WatchWindow.AddWatch(expression, expression);
            try
            {
                watch.Value = context.GetValue<object>(expression);
            }
            catch
            {
                watch.Value = "(error)";
            }
        }
    }

    /// <summary>
    /// Execution options for interactive debugging
    /// </summary>
    public class InteractiveExecutionOptions
    {
        /// <summary>Enable interactive mode</summary>
        public bool EnableInteractiveMode { get; set; } = false;
        
        /// <summary>Break on first step</summary>
        public bool BreakOnFirstStep { get; set; } = false;
        
        /// <summary>Break on error</summary>
        public bool BreakOnError { get; set; } = true;
        
        /// <summary>Break on failure</summary>
        public bool BreakOnFailure { get; set; } = false;
        
        /// <summary>Log watch values on each step</summary>
        public bool LogWatchValues { get; set; } = false;
        
        /// <summary>Enable call stack tracking</summary>
        public bool TrackCallStack { get; set; } = true;
        
        /// <summary>Maximum call stack depth</summary>
        public int MaxCallStackDepth { get; set; } = 100;
    }
}
