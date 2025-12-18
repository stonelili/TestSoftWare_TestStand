namespace TestStandClone.Core.Steps
{
    /// <summary>
    /// A step that calls another sequence.
    /// Similar to TestStand's Sequence Call step type.
    /// </summary>
    public class SequenceCallStep : TestStep
    {
        /// <summary>
        /// The sequence to call.
        /// </summary>
        public Sequence? TargetSequence { get; set; }

        /// <summary>
        /// The sequence file path (for external sequences).
        /// </summary>
        public string SequenceFilePath { get; set; } = string.Empty;

        /// <summary>
        /// The sequence name within the file.
        /// </summary>
        public string SequenceName { get; set; } = string.Empty;

        /// <summary>
        /// Result status of the called sequence.
        /// </summary>
        public StepStatus SequenceResult { get; private set; } = StepStatus.Idle;

        /// <summary>
        /// Creates a new SequenceCallStep.
        /// </summary>
        public SequenceCallStep()
        {
        }

        /// <summary>
        /// Creates a new SequenceCallStep with specified parameters.
        /// </summary>
        /// <param name="name">The name of the step.</param>
        /// <param name="targetSequence">The sequence to call.</param>
        public SequenceCallStep(string name, Sequence targetSequence)
        {
            Name = name;
            TargetSequence = targetSequence;
        }

        /// <summary>
        /// Executes the sequence call step.
        /// </summary>
        public override async Task ExecuteAsync(Context context)
        {
            if (TargetSequence == null)
            {
                Status = StepStatus.Error;
                ResultText = "No target sequence specified";
                return;
            }

            // Create a child engine for the sub-sequence
            var engine = new Engine();
            
            try
            {
                await engine.ExecuteSequenceAsync(TargetSequence);

                // Determine sequence result with a single pass through steps
                bool hasError = false;
                bool hasFailed = false;
                bool allPassed = true;

                foreach (var step in TargetSequence.Steps)
                {
                    if (step.Status == StepStatus.Error)
                    {
                        hasError = true;
                        break;
                    }
                    if (step.Status == StepStatus.Failed)
                    {
                        hasFailed = true;
                    }
                    if (step.Status != StepStatus.Passed)
                    {
                        allPassed = false;
                    }
                }

                if (hasError)
                {
                    SequenceResult = StepStatus.Error;
                    Status = StepStatus.Error;
                    ResultText = $"Sequence '{TargetSequence.Name}' completed with errors";
                }
                else if (hasFailed)
                {
                    SequenceResult = StepStatus.Failed;
                    Status = StepStatus.Failed;
                    ResultText = $"Sequence '{TargetSequence.Name}' failed";
                }
                else if (allPassed)
                {
                    SequenceResult = StepStatus.Passed;
                    Status = StepStatus.Passed;
                    ResultText = $"Sequence '{TargetSequence.Name}' passed";
                }
                else
                {
                    SequenceResult = StepStatus.Passed;
                    Status = StepStatus.Passed;
                    ResultText = $"Sequence '{TargetSequence.Name}' completed";
                }
            }
            catch (Exception ex)
            {
                SequenceResult = StepStatus.Error;
                Status = StepStatus.Error;
                ResultText = $"Sequence call error: {ex.Message}";
            }
        }
    }
}
