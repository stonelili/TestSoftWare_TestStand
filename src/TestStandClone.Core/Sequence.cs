using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TestStandClone.Core
{
    /// <summary>
    /// Represents a sequence of test steps to be executed.
    /// </summary>
    public class Sequence : INotifyPropertyChanged
    {
        private string _name = string.Empty;

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
        /// Collection of test steps in this sequence.
        /// </summary>
        public ObservableCollection<TestStep> Steps { get; } = new ObservableCollection<TestStep>();

        /// <summary>
        /// Resets all steps in the sequence to Idle status.
        /// </summary>
        public void Reset()
        {
            foreach (var step in Steps)
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
}
