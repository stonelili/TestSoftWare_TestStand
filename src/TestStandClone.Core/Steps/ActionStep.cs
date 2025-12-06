namespace TestStandClone.Core.Steps
{
    /// <summary>
    /// A test step that executes an action without pass/fail evaluation.
    /// Similar to TestStand's Action step type.
    /// </summary>
    public class ActionStep : TestStep
    {
        /// <summary>
        /// The action to execute.
        /// </summary>
        public Func<Context, Task>? Action { get; set; }

        /// <summary>
        /// Synchronous action alternative.
        /// </summary>
        public Action<Context>? SyncAction { get; set; }

        /// <summary>
        /// Creates a new ActionStep.
        /// </summary>
        public ActionStep()
        {
        }

        /// <summary>
        /// Creates a new ActionStep with specified name.
        /// </summary>
        /// <param name="name">The name of the step.</param>
        public ActionStep(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Creates a new ActionStep with specified name and action.
        /// </summary>
        /// <param name="name">The name of the step.</param>
        /// <param name="action">The action to execute.</param>
        public ActionStep(string name, Action<Context> action)
        {
            Name = name;
            SyncAction = action;
        }

        /// <summary>
        /// Executes the action step.
        /// </summary>
        public override async Task ExecuteAsync(Context context)
        {
            if (Action != null)
            {
                await Action(context);
            }
            else if (SyncAction != null)
            {
                SyncAction(context);
            }

            Status = StepStatus.Passed;
            ResultText = "Action completed";
        }
    }
}
