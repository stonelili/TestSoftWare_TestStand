namespace TestStandClone.Core.Steps
{
    /// <summary>
    /// A test step that evaluates a boolean condition for pass/fail result.
    /// Similar to TestStand's Pass/Fail step type.
    /// </summary>
    public class PassFailStep : TestStep
    {
        /// <summary>
        /// The boolean expression result to evaluate.
        /// </summary>
        public Func<Context, bool>? Condition { get; set; }

        /// <summary>
        /// Static boolean value to use when Condition is null.
        /// </summary>
        public bool ExpectedResult { get; set; } = true;

        /// <summary>
        /// Creates a new PassFailStep.
        /// </summary>
        public PassFailStep()
        {
        }

        /// <summary>
        /// Creates a new PassFailStep with specified name and expected result.
        /// </summary>
        /// <param name="name">The name of the step.</param>
        /// <param name="expectedResult">The expected boolean result.</param>
        public PassFailStep(string name, bool expectedResult = true)
        {
            Name = name;
            ExpectedResult = expectedResult;
        }

        /// <summary>
        /// Executes the pass/fail test by evaluating the condition.
        /// </summary>
        public override Task ExecuteAsync(Context context)
        {
            bool result = Condition?.Invoke(context) ?? ExpectedResult;

            if (result)
            {
                Status = StepStatus.Passed;
                ResultText = "PASS";
            }
            else
            {
                Status = StepStatus.Failed;
                ResultText = "FAIL";
            }

            return Task.CompletedTask;
        }
    }
}
