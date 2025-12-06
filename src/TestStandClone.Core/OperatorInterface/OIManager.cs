using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestStandClone.Core.OperatorInterface
{
    /// <summary>
    /// Message type for operator interface
    /// </summary>
    public enum OIMessageType
    {
        Information,
        Warning,
        Error,
        Question,
        Input,
        Progress,
        Custom
    }

    /// <summary>
    /// Message priority
    /// </summary>
    public enum OIMessagePriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    /// <summary>
    /// Response type from operator
    /// </summary>
    public enum OIResponseType
    {
        Ok,
        Cancel,
        Yes,
        No,
        Retry,
        Abort,
        Custom
    }

    /// <summary>
    /// Operator interface message
    /// </summary>
    public class OIMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public OIMessageType Type { get; set; }
        public OIMessagePriority Priority { get; set; } = OIMessagePriority.Normal;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? DetailedMessage { get; set; }
        public List<string> Buttons { get; set; } = new();
        public string? DefaultButton { get; set; }
        public bool RequiresResponse { get; set; } = true;
        public int TimeoutMs { get; set; } = 0; // 0 = no timeout
        public string? DefaultValue { get; set; }
        public List<string>? InputOptions { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? SequenceName { get; set; }
        public string? StepName { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Operator interface response
    /// </summary>
    public class OIResponse
    {
        public string MessageId { get; set; } = string.Empty;
        public OIResponseType ResponseType { get; set; }
        public string? ButtonClicked { get; set; }
        public string? InputValue { get; set; }
        public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
        public bool TimedOut { get; set; }
    }

    /// <summary>
    /// Progress update message
    /// </summary>
    public class OIProgressUpdate
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public double Progress { get; set; } // 0 to 100
        public bool IsIndeterminate { get; set; }
        public bool CanCancel { get; set; }
        public bool IsCancelled { get; set; }
    }

    /// <summary>
    /// Interface for operator interface handlers
    /// </summary>
    public interface IOIHandler
    {
        /// <summary>
        /// Handler name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Show a message and get response
        /// </summary>
        Task<OIResponse> ShowMessageAsync(OIMessage message);

        /// <summary>
        /// Show a progress dialog
        /// </summary>
        Task<OIProgressUpdate> ShowProgressAsync(OIProgressUpdate progress);

        /// <summary>
        /// Update progress
        /// </summary>
        Task UpdateProgressAsync(OIProgressUpdate progress);

        /// <summary>
        /// Close progress dialog
        /// </summary>
        Task CloseProgressAsync(string progressId);
    }

    /// <summary>
    /// Console-based operator interface handler
    /// </summary>
    public class ConsoleOIHandler : IOIHandler
    {
        public string Name => "Console Handler";

        public Task<OIResponse> ShowMessageAsync(OIMessage message)
        {
            Console.WriteLine();
            Console.WriteLine($"=== {message.Type}: {message.Title} ===");
            Console.WriteLine(message.Message);
            
            if (!string.IsNullOrEmpty(message.DetailedMessage))
            {
                Console.WriteLine($"Details: {message.DetailedMessage}");
            }

            var response = new OIResponse { MessageId = message.Id };

            if (message.RequiresResponse)
            {
                if (message.Type == OIMessageType.Input)
                {
                    Console.Write("Enter value: ");
                    response.InputValue = Console.ReadLine() ?? message.DefaultValue ?? string.Empty;
                    response.ResponseType = OIResponseType.Ok;
                }
                else if (message.Buttons.Any())
                {
                    Console.WriteLine($"Options: {string.Join(", ", message.Buttons)}");
                    Console.Write("Enter choice: ");
                    var choice = Console.ReadLine();
                    response.ButtonClicked = message.Buttons.FirstOrDefault(b => 
                        b.Equals(choice, StringComparison.OrdinalIgnoreCase)) ?? message.DefaultButton ?? message.Buttons.First();
                    response.ResponseType = MapButtonToResponseType(response.ButtonClicked);
                }
                else
                {
                    Console.WriteLine("Press Enter to continue...");
                    Console.ReadLine();
                    response.ResponseType = OIResponseType.Ok;
                }
            }

            return Task.FromResult(response);
        }

        public Task<OIProgressUpdate> ShowProgressAsync(OIProgressUpdate progress)
        {
            Console.WriteLine($"[Progress] {progress.Title}: {progress.Message} ({progress.Progress:F0}%)");
            return Task.FromResult(progress);
        }

        public Task UpdateProgressAsync(OIProgressUpdate progress)
        {
            Console.WriteLine($"[Progress] {progress.Title}: {progress.Message} ({progress.Progress:F0}%)");
            return Task.CompletedTask;
        }

        public Task CloseProgressAsync(string progressId)
        {
            Console.WriteLine($"[Progress] Completed");
            return Task.CompletedTask;
        }

        private static OIResponseType MapButtonToResponseType(string button)
        {
            return button.ToLowerInvariant() switch
            {
                "ok" => OIResponseType.Ok,
                "cancel" => OIResponseType.Cancel,
                "yes" => OIResponseType.Yes,
                "no" => OIResponseType.No,
                "retry" => OIResponseType.Retry,
                "abort" => OIResponseType.Abort,
                _ => OIResponseType.Custom
            };
        }
    }

    /// <summary>
    /// Simulated operator interface handler for testing
    /// </summary>
    public class SimulatedOIHandler : IOIHandler
    {
        public string Name => "Simulated Handler";
        
        public OIResponseType DefaultResponseType { get; set; } = OIResponseType.Ok;
        public string DefaultInputValue { get; set; } = "test_value";

        public Task<OIResponse> ShowMessageAsync(OIMessage message)
        {
            var response = new OIResponse
            {
                MessageId = message.Id,
                ResponseType = DefaultResponseType,
                ButtonClicked = message.DefaultButton ?? message.Buttons.FirstOrDefault(),
                InputValue = message.Type == OIMessageType.Input ? DefaultInputValue : null
            };

            return Task.FromResult(response);
        }

        public Task<OIProgressUpdate> ShowProgressAsync(OIProgressUpdate progress)
        {
            return Task.FromResult(progress);
        }

        public Task UpdateProgressAsync(OIProgressUpdate progress)
        {
            return Task.CompletedTask;
        }

        public Task CloseProgressAsync(string progressId)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Operator interface manager
    /// </summary>
    public class OIManager
    {
        private static readonly Lazy<OIManager> _instance = new(() => new OIManager());
        public static OIManager Instance => _instance.Value;

        private IOIHandler _handler;
        private readonly List<OIMessage> _messageHistory = new();
        private readonly Dictionary<string, OIProgressUpdate> _activeProgress = new();
        private readonly object _lock = new();
        private int _maxHistorySize = 100;

        public event EventHandler<OIMessageEventArgs>? MessageShown;
        public event EventHandler<OIResponseEventArgs>? ResponseReceived;
        public event EventHandler<OIProgressEventArgs>? ProgressUpdated;

        private OIManager()
        {
            // Default to simulated handler
            _handler = new SimulatedOIHandler();
        }

        /// <summary>
        /// Set the operator interface handler
        /// </summary>
        public void SetHandler(IOIHandler handler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Get the current handler
        /// </summary>
        public IOIHandler Handler => _handler;

        /// <summary>
        /// Maximum message history size
        /// </summary>
        public int MaxHistorySize
        {
            get => _maxHistorySize;
            set => _maxHistorySize = value;
        }

        /// <summary>
        /// Show an information message
        /// </summary>
        public Task<OIResponse> ShowInfoAsync(string title, string message, string? details = null)
        {
            return ShowMessageAsync(new OIMessage
            {
                Type = OIMessageType.Information,
                Title = title,
                Message = message,
                DetailedMessage = details,
                Buttons = new List<string> { "OK" },
                DefaultButton = "OK"
            });
        }

        /// <summary>
        /// Show a warning message
        /// </summary>
        public Task<OIResponse> ShowWarningAsync(string title, string message, string? details = null)
        {
            return ShowMessageAsync(new OIMessage
            {
                Type = OIMessageType.Warning,
                Priority = OIMessagePriority.High,
                Title = title,
                Message = message,
                DetailedMessage = details,
                Buttons = new List<string> { "OK" },
                DefaultButton = "OK"
            });
        }

        /// <summary>
        /// Show an error message
        /// </summary>
        public Task<OIResponse> ShowErrorAsync(string title, string message, string? details = null)
        {
            return ShowMessageAsync(new OIMessage
            {
                Type = OIMessageType.Error,
                Priority = OIMessagePriority.Critical,
                Title = title,
                Message = message,
                DetailedMessage = details,
                Buttons = new List<string> { "OK" },
                DefaultButton = "OK"
            });
        }

        /// <summary>
        /// Show a yes/no question
        /// </summary>
        public Task<OIResponse> ShowYesNoAsync(string title, string question)
        {
            return ShowMessageAsync(new OIMessage
            {
                Type = OIMessageType.Question,
                Title = title,
                Message = question,
                Buttons = new List<string> { "Yes", "No" },
                DefaultButton = "Yes"
            });
        }

        /// <summary>
        /// Show an input dialog
        /// </summary>
        public Task<OIResponse> ShowInputAsync(string title, string prompt, string? defaultValue = null, List<string>? options = null)
        {
            return ShowMessageAsync(new OIMessage
            {
                Type = OIMessageType.Input,
                Title = title,
                Message = prompt,
                DefaultValue = defaultValue,
                InputOptions = options,
                Buttons = new List<string> { "OK", "Cancel" },
                DefaultButton = "OK"
            });
        }

        /// <summary>
        /// Show a message
        /// </summary>
        public async Task<OIResponse> ShowMessageAsync(OIMessage message)
        {
            lock (_lock)
            {
                _messageHistory.Add(message);
                while (_messageHistory.Count > _maxHistorySize)
                {
                    _messageHistory.RemoveAt(0);
                }
            }

            MessageShown?.Invoke(this, new OIMessageEventArgs { Message = message });

            var response = await _handler.ShowMessageAsync(message);

            ResponseReceived?.Invoke(this, new OIResponseEventArgs { Message = message, Response = response });

            return response;
        }

        /// <summary>
        /// Start a progress dialog
        /// </summary>
        public async Task<string> StartProgressAsync(string title, string message, bool canCancel = false)
        {
            var progress = new OIProgressUpdate
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Message = message,
                Progress = 0,
                CanCancel = canCancel
            };

            lock (_lock)
            {
                _activeProgress[progress.Id] = progress;
            }

            await _handler.ShowProgressAsync(progress);
            ProgressUpdated?.Invoke(this, new OIProgressEventArgs { Progress = progress });

            return progress.Id;
        }

        /// <summary>
        /// Update progress
        /// </summary>
        public async Task UpdateProgressAsync(string progressId, double progress, string? message = null)
        {
            OIProgressUpdate? update;
            lock (_lock)
            {
                if (!_activeProgress.TryGetValue(progressId, out update))
                {
                    return;
                }

                update.Progress = progress;
                if (message != null)
                {
                    update.Message = message;
                }
            }

            await _handler.UpdateProgressAsync(update);
            ProgressUpdated?.Invoke(this, new OIProgressEventArgs { Progress = update });
        }

        /// <summary>
        /// Close progress dialog
        /// </summary>
        public async Task CloseProgressAsync(string progressId)
        {
            lock (_lock)
            {
                _activeProgress.Remove(progressId);
            }

            await _handler.CloseProgressAsync(progressId);
        }

        /// <summary>
        /// Get message history
        /// </summary>
        public IEnumerable<OIMessage> GetMessageHistory()
        {
            lock (_lock)
            {
                return _messageHistory.ToList();
            }
        }

        /// <summary>
        /// Clear message history
        /// </summary>
        public void ClearHistory()
        {
            lock (_lock)
            {
                _messageHistory.Clear();
            }
        }
    }

    /// <summary>
    /// OI message event arguments
    /// </summary>
    public class OIMessageEventArgs : EventArgs
    {
        public OIMessage Message { get; set; } = new();
    }

    /// <summary>
    /// OI response event arguments
    /// </summary>
    public class OIResponseEventArgs : EventArgs
    {
        public OIMessage Message { get; set; } = new();
        public OIResponse Response { get; set; } = new();
    }

    /// <summary>
    /// OI progress event arguments
    /// </summary>
    public class OIProgressEventArgs : EventArgs
    {
        public OIProgressUpdate Progress { get; set; } = new();
    }
}
