namespace TestStandClone.Core.Steps
{
    /// <summary>
    /// A test step that displays a message popup to the operator.
    /// Similar to TestStand's Message Popup step type.
    /// </summary>
    public class MessagePopupStep : TestStep
    {
        /// <summary>
        /// The message title.
        /// </summary>
        public string Title { get; set; } = "Message";

        /// <summary>
        /// The message content.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Available button options for the popup.
        /// </summary>
        public MessagePopupButtons Buttons { get; set; } = MessagePopupButtons.Ok;

        /// <summary>
        /// The result of the popup (which button was clicked).
        /// </summary>
        public MessagePopupResult PopupResult { get; private set; } = MessagePopupResult.None;

        /// <summary>
        /// Callback to display the popup (injected by UI layer).
        /// </summary>
        public static Func<string, string, MessagePopupButtons, Task<MessagePopupResult>>? ShowPopupAsync { get; set; }

        /// <summary>
        /// Creates a new MessagePopupStep.
        /// </summary>
        public MessagePopupStep()
        {
        }

        /// <summary>
        /// Creates a new MessagePopupStep with specified parameters.
        /// </summary>
        /// <param name="name">The name of the step.</param>
        /// <param name="title">The popup title.</param>
        /// <param name="message">The popup message.</param>
        public MessagePopupStep(string name, string title, string message)
        {
            Name = name;
            Title = title;
            Message = message;
        }

        /// <summary>
        /// Executes the message popup step.
        /// </summary>
        public override async Task ExecuteAsync(Context context)
        {
            if (ShowPopupAsync != null)
            {
                PopupResult = await ShowPopupAsync(Title, Message, Buttons);
            }
            else
            {
                // Simulate popup if no handler is registered
                PopupResult = MessagePopupResult.Ok;
            }

            Status = StepStatus.Passed;
            ResultText = $"User clicked: {PopupResult}";

            // Store result in context
            context.SetValue($"{Name}_Result", PopupResult);
        }
    }

    /// <summary>
    /// Button options for message popup.
    /// </summary>
    public enum MessagePopupButtons
    {
        Ok,
        OkCancel,
        YesNo,
        YesNoCancel
    }

    /// <summary>
    /// Result of message popup.
    /// </summary>
    public enum MessagePopupResult
    {
        None,
        Ok,
        Cancel,
        Yes,
        No
    }
}
