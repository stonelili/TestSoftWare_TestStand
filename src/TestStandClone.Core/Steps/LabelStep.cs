namespace TestStandClone.Core.Steps
{
    /// <summary>
    /// A marker step that serves as a label for Goto steps.
    /// Similar to TestStand's Label step type.
    /// </summary>
    public class LabelStep : TestStep
    {
        /// <summary>
        /// The label identifier.
        /// </summary>
        public string LabelId { get; set; } = string.Empty;

        /// <summary>
        /// Creates a new LabelStep.
        /// </summary>
        public LabelStep()
        {
        }

        /// <summary>
        /// Creates a new LabelStep with specified label ID.
        /// </summary>
        /// <param name="name">The name of the step.</param>
        /// <param name="labelId">The label identifier.</param>
        public LabelStep(string name, string labelId)
        {
            Name = name;
            LabelId = labelId;
        }

        /// <summary>
        /// Executes the label step (no-op, just a marker).
        /// </summary>
        public override Task ExecuteAsync(Context context)
        {
            Status = StepStatus.Passed;
            ResultText = $"Label: {LabelId}";
            return Task.CompletedTask;
        }
    }
}
