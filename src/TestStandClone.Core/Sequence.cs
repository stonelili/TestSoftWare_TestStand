using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TestStandClone.Core
{
    /// <summary>
    /// Represents a sequence of test steps to be executed.
    /// Similar to TestStand's Sequence structure with Setup, Main, and Cleanup groups.
    /// </summary>
    public class Sequence : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _description = string.Empty;
        private SequenceStatus _status = SequenceStatus.Idle;
        private DateTime? _startTime;
        private DateTime? _endTime;

        /// <summary>
        /// The name of the sequence.
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
        /// Description of the sequence.
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
        /// Overall status of the sequence.
        /// </summary>
        public SequenceStatus Status
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
        /// Start time of execution.
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
        /// End time of execution.
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
        /// Total execution time.
        /// </summary>
        public TimeSpan? ExecutionTime => EndTime.HasValue && StartTime.HasValue 
            ? EndTime.Value - StartTime.Value 
            : null;

        /// <summary>
        /// Setup steps - executed before Main steps.
        /// </summary>
        public ObservableCollection<TestStep> SetupSteps { get; } = new ObservableCollection<TestStep>();

        /// <summary>
        /// Main test steps.
        /// </summary>
        public ObservableCollection<TestStep> MainSteps { get; } = new ObservableCollection<TestStep>();

        /// <summary>
        /// Cleanup steps - executed after Main steps (always runs).
        /// </summary>
        public ObservableCollection<TestStep> CleanupSteps { get; } = new ObservableCollection<TestStep>();

        /// <summary>
        /// All steps combined (for backward compatibility and UI binding).
        /// </summary>
        public ObservableCollection<TestStep> Steps { get; } = new ObservableCollection<TestStep>();

        /// <summary>
        /// Synchronizes the Steps collection with Setup, Main, and Cleanup steps.
        /// </summary>
        public void SyncStepsCollection()
        {
            Steps.Clear();
            foreach (var step in SetupSteps) Steps.Add(step);
            foreach (var step in MainSteps) Steps.Add(step);
            foreach (var step in CleanupSteps) Steps.Add(step);
        }

        /// <summary>
        /// Resets all steps in the sequence to Idle status.
        /// </summary>
        public void Reset()
        {
            Status = SequenceStatus.Idle;
            StartTime = null;
            EndTime = null;

            ResetSteps(SetupSteps);
            ResetSteps(MainSteps);
            ResetSteps(CleanupSteps);
            ResetSteps(Steps);
        }

        private static void ResetSteps(ObservableCollection<TestStep> steps)
        {
            foreach (var step in steps)
            {
                step.Status = StepStatus.Idle;
                step.ResultText = string.Empty;
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
        /// <param name="propertyName">Name of the changed property.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Status of sequence execution.
    /// </summary>
    public enum SequenceStatus
    {
        Idle,
        Running,
        Paused,
        Passed,
        Failed,
        Error,
        Aborted
    }
}
