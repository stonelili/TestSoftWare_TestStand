namespace TestStandClone.Core
{
    /// <summary>
    /// Execution engine for running test sequences.
    /// </summary>
    public class Engine
    {
        /// <summary>
        /// Executes all steps in a sequence asynchronously.
        /// </summary>
        /// <param name="sequence">The sequence to execute.</param>
        /// <returns>A task representing the async operation.</returns>
        public async Task ExecuteSequenceAsync(Sequence sequence)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            // Create a context for this execution
            var context = new Context();

            // Reset all steps before execution
            sequence.Reset();

            // Execute each step in order
            foreach (var step in sequence.Steps)
            {
                try
                {
                    // Update status to Running
                    step.Status = StepStatus.Running;
                    step.ResultText = "Executing...";

                    // Execute the step
                    await step.ExecuteAsync(context);

                    // If status wasn't set by the step, mark as Passed
                    if (step.Status == StepStatus.Running)
                    {
                        step.Status = StepStatus.Passed;
                    }
                }
                catch (Exception ex)
                {
                    // Handle execution errors
                    step.Status = StepStatus.Error;
                    step.ResultText = $"Error: {ex.Message}";
                }
            }
        }
    }
}
