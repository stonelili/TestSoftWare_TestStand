namespace TestStandClone.Core
{
    /// <summary>
    /// Defines the possible status states for a test step.
    /// </summary>
    public enum StepStatus
    {
        /// <summary>
        /// Step has not started execution.
        /// </summary>
        Idle,

        /// <summary>
        /// Step is currently executing.
        /// </summary>
        Running,

        /// <summary>
        /// Step completed successfully.
        /// </summary>
        Passed,

        /// <summary>
        /// Step completed but did not meet the pass criteria.
        /// </summary>
        Failed,

        /// <summary>
        /// Step encountered an error during execution.
        /// </summary>
        Error
    }
}
