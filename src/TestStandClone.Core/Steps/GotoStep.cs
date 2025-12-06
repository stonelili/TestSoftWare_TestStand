namespace TestStandClone.Core.Steps
{
    /// <summary>
    /// A flow control step that jumps to a label.
    /// Similar to TestStand's Goto step type.
    /// </summary>
    public class GotoStep : TestStep, IFlowControlStep
    {
        /// <summary>
        /// The target label ID to jump to.
        /// </summary>
        public string TargetLabelId { get; set; } = string.Empty;

        /// <summary>
        /// Optional condition for conditional goto.
        /// </summary>
        public Func<Context, bool>? Condition { get; set; }

        /// <summary>
        /// Gets whether this step requests a flow control change.
        /// </summary>
        public bool RequestsFlowChange { get; private set; }

        /// <summary>
        /// Creates a new GotoStep.
        /// </summary>
        public GotoStep()
        {
        }

        /// <summary>
        /// Creates a new GotoStep with specified target label.
        /// </summary>
        /// <param name="name">The name of the step.</param>
        /// <param name="targetLabelId">The target label ID.</param>
        public GotoStep(string name, string targetLabelId)
        {
            Name = name;
            TargetLabelId = targetLabelId;
        }

        /// <summary>
        /// Executes the goto step.
        /// </summary>
        public override Task ExecuteAsync(Context context)
        {
            bool shouldGoto = Condition?.Invoke(context) ?? true;

            if (shouldGoto)
            {
                RequestsFlowChange = true;
                Status = StepStatus.Passed;
                ResultText = $"Goto: {TargetLabelId}";
            }
            else
            {
                RequestsFlowChange = false;
                Status = StepStatus.Passed;
                ResultText = "Condition not met, continuing";
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Interface for steps that can change execution flow.
    /// </summary>
    public interface IFlowControlStep
    {
        bool RequestsFlowChange { get; }
        string TargetLabelId { get; }
    }
}
