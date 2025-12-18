using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TestStandClone.Core.ProcessModels
{
    /// <summary>
    /// Base class for process models that control test execution flow.
    /// Similar to TestStand's process model system.
    /// </summary>
    public abstract class ProcessModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private ProcessModelState _state = ProcessModelState.Idle;
        private UUT? _currentUUT;

        /// <summary>
        /// Name of the process model.
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
        /// Current state of the process model.
        /// </summary>
        public ProcessModelState State
        {
            get => _state;
            protected set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Current UUT being tested.
        /// </summary>
        public UUT? CurrentUUT
        {
            get => _currentUUT;
            protected set
            {
                if (_currentUUT != value)
                {
                    _currentUUT = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Engine used for execution.
        /// </summary>
        protected Engine? Engine { get; set; }

        /// <summary>
        /// Event raised when test starts.
        /// </summary>
        public event EventHandler<UUT>? TestStarted;

        /// <summary>
        /// Event raised when test completes.
        /// </summary>
        public event EventHandler<UUT>? TestCompleted;

        /// <summary>
        /// Pre-test callback (UUT identification, setup, etc.)
        /// </summary>
        public Func<UUT, Task>? PreTestCallback { get; set; }

        /// <summary>
        /// Post-test callback (result logging, reporting, etc.)
        /// </summary>
        public Func<UUT, Sequence, Task>? PostTestCallback { get; set; }

        /// <summary>
        /// Runs the test sequence with the process model.
        /// </summary>
        public abstract Task<UUTResult> RunAsync(Sequence sequence, UUT uut);

        /// <summary>
        /// Executes pre-test operations.
        /// </summary>
        protected virtual async Task OnPreTestAsync(UUT uut)
        {
            uut.StartTime = DateTime.Now;
            TestStarted?.Invoke(this, uut);

            if (PreTestCallback != null)
            {
                await PreTestCallback(uut);
            }
        }

        /// <summary>
        /// Executes post-test operations.
        /// </summary>
        protected virtual async Task OnPostTestAsync(UUT uut, Sequence sequence)
        {
            uut.EndTime = DateTime.Now;

            // Determine UUT result from sequence
            uut.Result = sequence.Status switch
            {
                SequenceStatus.Passed => UUTResult.Passed,
                SequenceStatus.Failed => UUTResult.Failed,
                SequenceStatus.Error => UUTResult.Error,
                SequenceStatus.Aborted => UUTResult.Terminated,
                _ => UUTResult.NotTested
            };

            if (PostTestCallback != null)
            {
                await PostTestCallback(uut, sequence);
            }

            TestCompleted?.Invoke(this, uut);
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// State of process model execution.
    /// </summary>
    public enum ProcessModelState
    {
        Idle,
        Running,
        Paused,
        Completed,
        Error
    }
}
