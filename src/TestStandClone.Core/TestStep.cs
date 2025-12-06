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

        /// <summary>
        /// Executes the test step asynchronously.
        /// </summary>
        /// <param name="context">The execution context for data sharing.</param>
        /// <returns>A task representing the async operation.</returns>
        public abstract Task ExecuteAsync(Context context);

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
}
