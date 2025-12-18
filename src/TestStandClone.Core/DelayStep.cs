namespace TestStandClone.Core
{
    /// <summary>
    /// A test step that introduces a delay, simulating a time-consuming operation.
    /// </summary>
    public class DelayStep : TestStep
    {
        /// <summary>
        /// The duration of the delay in milliseconds.
        /// </summary>
        public int DelayMilliseconds { get; set; } = 1000;

        /// <summary>
        /// Creates a new DelayStep with default delay of 1 second.
        /// </summary>
        public DelayStep()
        {
        }

        /// <summary>
        /// Creates a new DelayStep with the specified delay.
        /// </summary>
        /// <param name="name">The name of the step.</param>
        /// <param name="delayMs">The delay duration in milliseconds.</param>
        public DelayStep(string name, int delayMs)
        {
            Name = name;
            DelayMilliseconds = delayMs;
        }

        /// <summary>
        /// Executes the delay step by waiting for the specified duration.
        /// </summary>
        /// <param name="context">The execution context.</param>
        public override async Task ExecuteAsync(Context context)
        {
            ResultText = $"Waiting {DelayMilliseconds}ms...";
            
            await Task.Delay(DelayMilliseconds);
            
            Status = StepStatus.Passed;
            ResultText = $"Completed delay of {DelayMilliseconds}ms";
        }
    }
}
