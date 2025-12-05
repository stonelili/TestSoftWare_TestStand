namespace TestStandClone.Core
{
    /// <summary>
    /// A test step that generates a random value and compares it against limits.
    /// </summary>
    public class NumericLimitStep : TestStep
    {
        /// <summary>
        /// The lower limit for the comparison.
        /// </summary>
        public double LowerLimit { get; set; } = 0.0;

        /// <summary>
        /// The upper limit for the comparison.
        /// </summary>
        public double UpperLimit { get; set; } = 100.0;

        /// <summary>
        /// The minimum value that can be generated (for simulation).
        /// </summary>
        public double MinGeneratedValue { get; set; } = 0.0;

        /// <summary>
        /// The maximum value that can be generated (for simulation).
        /// </summary>
        public double MaxGeneratedValue { get; set; } = 100.0;

        /// <summary>
        /// The measured value after execution.
        /// </summary>
        public double MeasuredValue { get; private set; }

        /// <summary>
        /// Creates a new NumericLimitStep with default limits.
        /// </summary>
        public NumericLimitStep()
        {
        }

        /// <summary>
        /// Creates a new NumericLimitStep with specified parameters.
        /// </summary>
        /// <param name="name">The name of the step.</param>
        /// <param name="lowerLimit">The lower limit for pass criteria.</param>
        /// <param name="upperLimit">The upper limit for pass criteria.</param>
        public NumericLimitStep(string name, double lowerLimit, double upperLimit)
        {
            Name = name;
            LowerLimit = lowerLimit;
            UpperLimit = upperLimit;
        }

        /// <summary>
        /// Executes the numeric limit test by generating a random value and comparing against limits.
        /// </summary>
        /// <param name="context">The execution context.</param>
        public override Task ExecuteAsync(Context context)
        {
            // Generate a random value within the specified range using thread-safe Random.Shared
            MeasuredValue = MinGeneratedValue + (Random.Shared.NextDouble() * (MaxGeneratedValue - MinGeneratedValue));
            
            // Round to 2 decimal places for display
            MeasuredValue = Math.Round(MeasuredValue, 2);

            // Check if value is within limits
            bool passed = MeasuredValue >= LowerLimit && MeasuredValue <= UpperLimit;

            if (passed)
            {
                Status = StepStatus.Passed;
                ResultText = $"Value: {MeasuredValue} (Limits: {LowerLimit} - {UpperLimit}) [PASS]";
            }
            else
            {
                Status = StepStatus.Failed;
                ResultText = $"Value: {MeasuredValue} (Limits: {LowerLimit} - {UpperLimit}) [FAIL]";
            }

            // Store the measured value in context for potential use by other steps
            context.SetValue($"{Name}_MeasuredValue", MeasuredValue);

            return Task.CompletedTask;
        }
    }
}
