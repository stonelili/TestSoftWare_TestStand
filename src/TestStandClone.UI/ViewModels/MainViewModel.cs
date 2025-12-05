using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TestStandClone.Core;
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
                }
            }
        }

        /// <summary>
        /// Command to run the loaded sequence.
        /// </summary>
        public ICommand RunSequenceCommand { get; }

        /// <summary>
        /// Command to reset all steps to idle.
        /// </summary>
        public ICommand ResetCommand { get; }

        /// <summary>
        /// Creates a new MainViewModel with a demo sequence.
        /// </summary>
        public MainViewModel()
        {
            _engine = new Engine();
            _loadedSequence = CreateDemoSequence();

            RunSequenceCommand = new RelayCommand(
                async () => await RunSequenceAsync(),
                () => !IsRunning
            );

            ResetCommand = new RelayCommand(
                () => LoadedSequence.Reset(),
                () => !IsRunning
            );
        }

        /// <summary>
        /// Creates a demo sequence with a mix of step types.
        /// </summary>
        private static Sequence CreateDemoSequence()
        {
            var sequence = new Sequence
            {
                Name = "Demo Test Sequence"
            };

            // Add a variety of test steps (5-10 steps as required)
            sequence.Steps.Add(new DelayStep("Initialize System", 500));
            
            sequence.Steps.Add(new NumericLimitStep("Voltage Check", 4.8, 5.2)
            {
                MinGeneratedValue = 4.5,
                MaxGeneratedValue = 5.5
            });

            sequence.Steps.Add(new DelayStep("Wait for Stabilization", 300));

            sequence.Steps.Add(new NumericLimitStep("Current Measurement", 0.5, 2.0)
            {
                MinGeneratedValue = 0.3,
                MaxGeneratedValue = 2.5
            });

            sequence.Steps.Add(new NumericLimitStep("Temperature Check", 20.0, 35.0)
            {
                MinGeneratedValue = 15.0,
                MaxGeneratedValue = 40.0
            });

            sequence.Steps.Add(new DelayStep("Processing Delay", 700));

            sequence.Steps.Add(new NumericLimitStep("Power Consumption", 1.0, 10.0)
            {
                MinGeneratedValue = 0.5,
                MaxGeneratedValue = 12.0
            });

            sequence.Steps.Add(new DelayStep("Cooldown Period", 400));

            sequence.Steps.Add(new NumericLimitStep("Final Resistance Test", 100.0, 500.0)
            {
                MinGeneratedValue = 80.0,
                MaxGeneratedValue = 550.0
            });

            sequence.Steps.Add(new DelayStep("Cleanup", 200));

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
                await _engine.ExecuteSequenceAsync(LoadedSequence);
            }
            finally
            {
                IsRunning = false;
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
