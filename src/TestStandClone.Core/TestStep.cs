using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TestStandClone.Core
{
    /// <summary>
    /// Abstract base class for all test steps.
    /// Implements INotifyPropertyChanged for UI data binding.
    /// </summary>
    public abstract class TestStep : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private Guid _id = Guid.NewGuid();
        private StepStatus _status = StepStatus.Idle;
        private string _resultText = string.Empty;
        private bool _hasBreakpoint;
        private DateTime? _startTime;
        private DateTime? _endTime;
        private string _description = string.Empty;
        private bool _isEnabled = true;
        private string _precondition = string.Empty;
        private PostAction _onPass = PostAction.Continue;
        private PostAction _onFail = PostAction.Continue;
        private string _gotoStepOnPass = string.Empty;
        private string _gotoStepOnFail = string.Empty;
        private int _loopCount = 1;
        private bool _loopOnPass;
        private bool _loopOnFail;
        private int _currentLoopIteration;
        private string _comment = string.Empty;

        /// <summary>
        /// The name of the test step.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Unique identifier for the step.
        /// </summary>
        public Guid Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Current execution status of the step.
        /// </summary>
        public StepStatus Status
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

        /// <summary>
        /// Result text describing the outcome of step execution.
        /// </summary>
        public string ResultText
        {
            get => _resultText;
            set
            {
                if (_resultText != value)
                {
                    _resultText = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Description of the test step.
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Comment for the test step.
        /// </summary>
        public string Comment
        {
            get => _comment;
            set
            {
                if (_comment != value)
                {
                    _comment = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Whether this step has a breakpoint set.
        /// </summary>
        public bool HasBreakpoint
        {
            get => _hasBreakpoint;
            set
            {
                if (_hasBreakpoint != value)
                {
                    _hasBreakpoint = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Whether this step is enabled for execution.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        #region Preconditions

        /// <summary>
        /// Precondition expression that must evaluate to true for step to execute.
        /// If empty, step always executes.
        /// </summary>
        public string Precondition
        {
            get => _precondition;
            set
            {
                if (_precondition != value)
                {
                    _precondition = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Evaluates the precondition expression.
        /// Returns true if step should execute.
        /// </summary>
        public bool EvaluatePrecondition(Context context)
        {
            if (string.IsNullOrWhiteSpace(Precondition))
            {
                return true; // No precondition = always execute
            }

            // Simple precondition evaluation
            // Supports: "Step.StepName.Status == Passed", "true", "false"
            var condition = Precondition.Trim();
            var conditionLower = condition.ToLower();
            
            if (conditionLower == "true") return true;
            if (conditionLower == "false") return false;

            // Check for step status conditions: Step.StepName.Status == Passed
            if (conditionLower.Contains(".status"))
            {
                // Parse the condition: Step.StepName.Status == Passed
                var match = System.Text.RegularExpressions.Regex.Match(
                    condition, 
                    @"Step\.(\w+)\.Status\s*==\s*(\w+)", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (match.Success)
                {
                    string stepName = match.Groups[1].Value;
                    string expectedStatus = match.Groups[2].Value;
                    
                    // Get the step status from context
                    var stepStatus = context.GetValue<StepStatus?>($"Step_{stepName}_Status");
                    
                    if (stepStatus.HasValue)
                    {
                        return expectedStatus.ToLower() switch
                        {
                            "passed" => stepStatus.Value == StepStatus.Passed,
                            "failed" => stepStatus.Value == StepStatus.Failed,
                            "error" => stepStatus.Value == StepStatus.Error,
                            "running" => stepStatus.Value == StepStatus.Running,
                            "idle" => stepStatus.Value == StepStatus.Idle,
                            _ => true
                        };
                    }
                }
                
                // Also check for precondition result set by engine
                var preconditionResult = context.GetValue<bool?>("PreconditionResult");
                if (preconditionResult.HasValue)
                {
                    return preconditionResult.Value;
                }
            }

            return true;
        }

        #endregion

        #region Post Actions

        /// <summary>
        /// Action to take when step passes.
        /// </summary>
        public PostAction OnPass
        {
            get => _onPass;
            set
            {
                if (_onPass != value)
                {
                    _onPass = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Action to take when step fails.
        /// </summary>
        public PostAction OnFail
        {
            get => _onFail;
            set
            {
                if (_onFail != value)
                {
                    _onFail = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Target step name for Goto action on pass.
        /// </summary>
        public string GotoStepOnPass
        {
            get => _gotoStepOnPass;
            set
            {
                if (_gotoStepOnPass != value)
                {
                    _gotoStepOnPass = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Target step name for Goto action on fail.
        /// </summary>
        public string GotoStepOnFail
        {
            get => _gotoStepOnFail;
            set
            {
                if (_gotoStepOnFail != value)
                {
                    _gotoStepOnFail = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Loop Settings

        /// <summary>
        /// Number of times to loop this step.
        /// </summary>
        public int LoopCount
        {
            get => _loopCount;
            set
            {
                if (_loopCount != value && value >= 1)
                {
                    _loopCount = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Whether to loop while step passes.
        /// </summary>
        public bool LoopOnPass
        {
            get => _loopOnPass;
            set
            {
                if (_loopOnPass != value)
                {
                    _loopOnPass = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Whether to loop while step fails.
        /// </summary>
        public bool LoopOnFail
        {
            get => _loopOnFail;
            set
            {
                if (_loopOnFail != value)
                {
                    _loopOnFail = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Current loop iteration (1-based).
        /// </summary>
        public int CurrentLoopIteration
        {
            get => _currentLoopIteration;
            set
            {
                if (_currentLoopIteration != value)
                {
                    _currentLoopIteration = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Timing

        /// <summary>
        /// Start time of step execution.
        /// </summary>
        public DateTime? StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ExecutionTime));
                }
            }
        }

        /// <summary>
        /// End time of step execution.
        /// </summary>
        public DateTime? EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime != value)
                {
                    _endTime = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ExecutionTime));
                }
            }
        }

        /// <summary>
        /// Total execution time for this step.
        /// </summary>
        public TimeSpan? ExecutionTime => EndTime.HasValue && StartTime.HasValue 
            ? EndTime.Value - StartTime.Value 
            : null;

        #endregion

        /// <summary>
        /// Executes the test step asynchronously.
        /// </summary>
        /// <param name="context">The execution context for data sharing.</param>
        /// <returns>A task representing the async operation.</returns>
        public abstract Task ExecuteAsync(Context context);

        /// <summary>
        /// Resets the step to idle state.
        /// </summary>
        public virtual void Reset()
        {
            Status = StepStatus.Idle;
            ResultText = string.Empty;
            StartTime = null;
            EndTime = null;
            CurrentLoopIteration = 0;
        }

        #region INotifyPropertyChanged Implementation

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">Name of the changed property.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Action to take after step execution.
    /// </summary>
    public enum PostAction
    {
        /// <summary>
        /// Continue to next step.
        /// </summary>
        Continue,

        /// <summary>
        /// Go to a specific step.
        /// </summary>
        Goto,

        /// <summary>
        /// Terminate the sequence.
        /// </summary>
        Terminate,

        /// <summary>
        /// Terminate and set sequence to failed.
        /// </summary>
        TerminateWithFail,

        /// <summary>
        /// Skip to next iteration (in loop).
        /// </summary>
        SkipToNextIteration,

        /// <summary>
        /// Call a callback sequence.
        /// </summary>
        CallCallback
    }
}
