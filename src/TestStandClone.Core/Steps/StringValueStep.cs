namespace TestStandClone.Core.Steps
{
    /// <summary>
    /// A test step that compares a string value against expected values.
    /// Similar to TestStand's String Value step type.
    /// </summary>
    public class StringValueStep : TestStep
    {
        /// <summary>
        /// Function to get the actual string value.
        /// </summary>
        public Func<Context, string>? ValueSource { get; set; }

        /// <summary>
        /// Static string value to use when ValueSource is null.
        /// </summary>
        public string ActualValue { get; set; } = string.Empty;

        /// <summary>
        /// The expected string value for comparison.
        /// </summary>
        public string ExpectedValue { get; set; } = string.Empty;

        /// <summary>
        /// Comparison type for string matching.
        /// </summary>
        public StringComparison ComparisonType { get; set; } = StringComparison.OrdinalIgnoreCase;

        /// <summary>
        /// Creates a new StringValueStep.
        /// </summary>
        public StringValueStep()
        {
        }

        /// <summary>
        /// Creates a new StringValueStep with specified parameters.
        /// </summary>
        /// <param name="name">The name of the step.</param>
        /// <param name="expectedValue">The expected string value.</param>
        public StringValueStep(string name, string expectedValue)
        {
            Name = name;
            ExpectedValue = expectedValue;
        }

        /// <summary>
        /// Executes the string comparison test.
        /// </summary>
        public override Task ExecuteAsync(Context context)
        {
            string value = ValueSource?.Invoke(context) ?? ActualValue;
            bool passed = string.Equals(value, ExpectedValue, ComparisonType);

            if (passed)
            {
                Status = StepStatus.Passed;
                ResultText = $"Value: '{value}' matches expected '{ExpectedValue}'";
            }
            else
            {
                Status = StepStatus.Failed;
                ResultText = $"Value: '{value}' does not match expected '{ExpectedValue}'";
            }

            // Store result in context
            context.SetValue($"{Name}_Value", value);

            return Task.CompletedTask;
        }
    }
}
