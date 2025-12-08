using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestStandClone.Core.OperatorPrompt
{
    /// <summary>
    /// Prompt types
    /// </summary>
    public enum PromptType
    {
        Information,
        Confirmation,
        TextInput,
        NumericInput,
        Selection,
        MultiSelection,
        FileSelection,
        Password,
        Custom
    }

    /// <summary>
    /// Prompt response status
    /// </summary>
    public enum PromptResponseStatus
    {
        Pending,
        Accepted,
        Cancelled,
        Timeout,
        Error
    }

    /// <summary>
    /// Selection option
    /// </summary>
    public class SelectionOption
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; } = string.Empty;
        public string? Description { get; set; }
        public object? Value { get; set; }
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// Operator prompt definition
    /// </summary>
    public class OperatorPromptDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public PromptType Type { get; set; } = PromptType.Information;
        public List<SelectionOption> Options { get; set; } = new List<SelectionOption>();
        public TimeSpan? Timeout { get; set; }
        public string? DefaultValue { get; set; }
        public bool IsRequired { get; set; } = true;
        public string? ValidationPattern { get; set; }
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public string? ImagePath { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Operator prompt response
    /// </summary>
    public class OperatorPromptResponse
    {
        public string PromptId { get; set; } = string.Empty;
        public PromptResponseStatus Status { get; set; } = PromptResponseStatus.Pending;
        public object? Value { get; set; }
        public List<string> SelectedOptionIds { get; set; } = new List<string>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TimeSpan ResponseTime { get; set; }
        public string? OperatorId { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Prompt handler interface
    /// </summary>
    public interface IPromptHandler
    {
        Task<OperatorPromptResponse> ShowPromptAsync(OperatorPromptDefinition prompt);
    }

    /// <summary>
    /// Console prompt handler
    /// </summary>
    public class ConsolePromptHandler : IPromptHandler
    {
        public Task<OperatorPromptResponse> ShowPromptAsync(OperatorPromptDefinition prompt)
        {
            Console.WriteLine($"\n=== {prompt.Title} ===");
            Console.WriteLine(prompt.Message);

            var response = new OperatorPromptResponse { PromptId = prompt.Id };
            var startTime = DateTime.UtcNow;

            try
            {
                switch (prompt.Type)
                {
                    case PromptType.Information:
                        Console.WriteLine("[Press Enter to continue]");
                        Console.ReadLine();
                        response.Status = PromptResponseStatus.Accepted;
                        break;

                    case PromptType.Confirmation:
                        Console.Write("[Y/N]: ");
                        var confirm = Console.ReadLine()?.Trim().ToUpper();
                        response.Value = confirm == "Y";
                        response.Status = confirm == "Y" || confirm == "N" ? PromptResponseStatus.Accepted : PromptResponseStatus.Cancelled;
                        break;

                    case PromptType.TextInput:
                    case PromptType.Password:
                        Console.Write("> ");
                        response.Value = Console.ReadLine();
                        response.Status = PromptResponseStatus.Accepted;
                        break;

                    case PromptType.NumericInput:
                        Console.Write($"[{prompt.MinValue ?? double.MinValue} - {prompt.MaxValue ?? double.MaxValue}]: ");
                        if (double.TryParse(Console.ReadLine(), out var num))
                        {
                            response.Value = num;
                            response.Status = PromptResponseStatus.Accepted;
                        }
                        else
                        {
                            response.Status = PromptResponseStatus.Error;
                            response.ErrorMessage = "Invalid number";
                        }
                        break;

                    case PromptType.Selection:
                        for (int i = 0; i < prompt.Options.Count; i++)
                            Console.WriteLine($"  {i + 1}. {prompt.Options[i].Text}");
                        Console.Write("Select: ");
                        if (int.TryParse(Console.ReadLine(), out var idx) && idx >= 1 && idx <= prompt.Options.Count)
                        {
                            response.SelectedOptionIds.Add(prompt.Options[idx - 1].Id);
                            response.Value = prompt.Options[idx - 1].Value;
                            response.Status = PromptResponseStatus.Accepted;
                        }
                        else
                        {
                            response.Status = PromptResponseStatus.Error;
                        }
                        break;

                    default:
                        response.Status = PromptResponseStatus.Accepted;
                        break;
                }
            }
            catch (Exception ex)
            {
                response.Status = PromptResponseStatus.Error;
                response.ErrorMessage = ex.Message;
            }

            response.ResponseTime = DateTime.UtcNow - startTime;
            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// Simulated prompt handler for testing
    /// </summary>
    public class SimulatedPromptHandler : IPromptHandler
    {
        public Dictionary<PromptType, object> SimulatedResponses { get; set; } = new Dictionary<PromptType, object>();

        public Task<OperatorPromptResponse> ShowPromptAsync(OperatorPromptDefinition prompt)
        {
            var response = new OperatorPromptResponse
            {
                PromptId = prompt.Id,
                Status = PromptResponseStatus.Accepted,
                ResponseTime = TimeSpan.FromMilliseconds(100)
            };

            if (SimulatedResponses.TryGetValue(prompt.Type, out var value))
                response.Value = value;
            else if (prompt.DefaultValue != null)
                response.Value = prompt.DefaultValue;
            else if (prompt.Options.Any())
            {
                var defaultOption = prompt.Options.FirstOrDefault(o => o.IsDefault) ?? prompt.Options.First();
                response.SelectedOptionIds.Add(defaultOption.Id);
                response.Value = defaultOption.Value;
            }

            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// Operator prompt manager singleton
    /// </summary>
    public class OperatorPromptManager
    {
        private static readonly Lazy<OperatorPromptManager> _instance = new Lazy<OperatorPromptManager>(() => new OperatorPromptManager());
        public static OperatorPromptManager Instance => _instance.Value;

        private IPromptHandler _handler = new ConsolePromptHandler();
        private readonly List<OperatorPromptResponse> _history = new List<OperatorPromptResponse>();
        private readonly object _lock = new object();

        public event EventHandler<OperatorPromptDefinition>? PromptRequested;
        public event EventHandler<OperatorPromptResponse>? PromptResponded;

        private OperatorPromptManager() { }

        public void SetHandler(IPromptHandler handler) { _handler = handler; }

        public async Task<OperatorPromptResponse> ShowPromptAsync(OperatorPromptDefinition prompt)
        {
            PromptRequested?.Invoke(this, prompt);
            var response = await _handler.ShowPromptAsync(prompt);
            lock (_lock) { _history.Add(response); }
            PromptResponded?.Invoke(this, response);
            return response;
        }

        public async Task<bool> ConfirmAsync(string title, string message)
        {
            var prompt = new OperatorPromptDefinition { Title = title, Message = message, Type = PromptType.Confirmation };
            var response = await ShowPromptAsync(prompt);
            return response.Status == PromptResponseStatus.Accepted && response.Value is true;
        }

        public async Task<string?> GetTextInputAsync(string title, string message, string? defaultValue = null)
        {
            var prompt = new OperatorPromptDefinition { Title = title, Message = message, Type = PromptType.TextInput, DefaultValue = defaultValue };
            var response = await ShowPromptAsync(prompt);
            return response.Status == PromptResponseStatus.Accepted ? response.Value?.ToString() : null;
        }

        public async Task<double?> GetNumericInputAsync(string title, string message, double? min = null, double? max = null)
        {
            var prompt = new OperatorPromptDefinition { Title = title, Message = message, Type = PromptType.NumericInput, MinValue = min, MaxValue = max };
            var response = await ShowPromptAsync(prompt);
            return response.Status == PromptResponseStatus.Accepted && response.Value is double d ? d : null;
        }

        public async Task<T?> GetSelectionAsync<T>(string title, string message, List<(string text, T value)> options)
        {
            var prompt = new OperatorPromptDefinition
            {
                Title = title,
                Message = message,
                Type = PromptType.Selection,
                Options = options.Select((o, i) => new SelectionOption { Text = o.text, Value = o.value, IsDefault = i == 0 }).ToList()
            };
            var response = await ShowPromptAsync(prompt);
            return response.Status == PromptResponseStatus.Accepted && response.Value is T t ? t : default;
        }

        public List<OperatorPromptResponse> GetHistory() { lock (_lock) { return _history.ToList(); } }
        public void ClearHistory() { lock (_lock) { _history.Clear(); } }
    }
}
