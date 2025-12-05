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
