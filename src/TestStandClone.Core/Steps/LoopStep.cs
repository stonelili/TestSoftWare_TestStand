namespace TestStandClone.Core.Steps
{
    /// <summary>
    /// A flow control step that manages loop iterations.
    /// Similar to TestStand's Loop step type.
    /// </summary>
    public class LoopStep : TestStep
    {
        private int _loopIterations = 1;

        /// <summary>
        /// The number of iterations for this loop step (specific to LoopStep).
        /// Note: This shadows the base class LoopCount which is for per-step looping.
        /// </summary>
        public int Iterations
        {
            get => _loopIterations;
            set
            {
                if (_loopIterations != value)
                {
                    _loopIterations = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Current iteration (0-based index).
        /// </summary>
        public int CurrentIteration { get; private set; } = 0;

        /// <summary>
        /// Whether the loop is starting (begin step) or ending (end step).
        /// </summary>
        public LoopType Type { get; set; } = LoopType.Begin;

        /// <summary>
        /// The matching loop step (Begin references End and vice versa).
        /// </summary>
        public LoopStep? MatchingLoopStep { get; set; }

        /// <summary>
        /// Creates a new LoopStep.
        /// </summary>
        public LoopStep()
        {
        }

        /// <summary>
        /// Creates a new LoopStep with specified parameters.
        /// </summary>
        /// <param name="name">The name of the step.</param>
        /// <param name="iterations">Number of iterations.</param>
        /// <param name="type">Loop type (Begin or End).</param>
        public LoopStep(string name, int iterations, LoopType type)
        {
            Name = name;
            Iterations = iterations;
            Type = type;
        }

        /// <summary>
        /// Resets the loop counter.
        /// </summary>
        public void ResetLoop()
        {
            CurrentIteration = 0;
        }

        /// <summary>
        /// Increments the loop counter.
        /// </summary>
        /// <returns>True if more iterations remain.</returns>
        public bool IncrementAndCheck()
        {
            CurrentIteration++;
            return CurrentIteration < Iterations;
        }

        /// <summary>
        /// Executes the loop step.
        /// </summary>
        public override Task ExecuteAsync(Context context)
        {
            if (Type == LoopType.Begin)
            {
                // Store current iteration in context
                context.SetValue($"{Name}_Iteration", CurrentIteration);
                Status = StepStatus.Passed;
                ResultText = $"Loop iteration {CurrentIteration + 1} of {Iterations}";
            }
            else // LoopType.End
            {
                Status = StepStatus.Passed;
                ResultText = $"End loop iteration {CurrentIteration + 1}";
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Type of loop step.
    /// </summary>
    public enum LoopType
    {
        Begin,
        End
    }
}
