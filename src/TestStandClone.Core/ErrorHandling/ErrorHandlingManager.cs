using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.ErrorHandling
{
    #region Error Types

    /// <summary>
    /// Error severity levels
    /// </summary>
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical,
        Fatal
    }

    /// <summary>
    /// Error categories
    /// </summary>
    public enum ErrorCategory
    {
        General,
        Execution,
        Sequence,
        Step,
        Variable,
        Instrument,
        CodeModule,
        Configuration,
        License,
        Network,
        FileSystem,
        Database,
        User
    }

    /// <summary>
    /// Represents an error in the system
    /// </summary>
    public class TestStandError
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
        public ErrorCategory Category { get; set; } = ErrorCategory.General;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? Source { get; set; }
        public string? StackTrace { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
        public TestStandError? InnerError { get; set; }

        public static TestStandError FromException(Exception ex, ErrorCategory category = ErrorCategory.General)
        {
            return new TestStandError
            {
                Code = ex.HResult,
                Message = ex.Message,
                Details = ex.ToString(),
                Severity = ErrorSeverity.Error,
                Category = category,
                Source = ex.Source,
                StackTrace = ex.StackTrace,
                InnerError = ex.InnerException != null ? FromException(ex.InnerException, category) : null
            };
        }
    }

    /// <summary>
    /// Error handler interface
    /// </summary>
    public interface IErrorHandler
    {
        string Name { get; }
        bool CanHandle(TestStandError error);
        ErrorHandlerResult Handle(TestStandError error);
    }

    /// <summary>
    /// Result of error handling
    /// </summary>
    public class ErrorHandlerResult
    {
        public bool Handled { get; set; }
        public bool ShouldContinue { get; set; }
        public bool ShouldRetry { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Error recovery action
    /// </summary>
    public class ErrorRecoveryAction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Func<TestStandError, bool>? CanExecute { get; set; }
        public Action<TestStandError>? Execute { get; set; }
    }

    /// <summary>
    /// Logging error handler
    /// </summary>
    public class LoggingErrorHandler : IErrorHandler
    {
        public string Name => "Logging";

        public bool CanHandle(TestStandError error) => true;

        public ErrorHandlerResult Handle(TestStandError error)
        {
            Console.WriteLine($"[{error.Severity}] [{error.Category}] {error.Message}");
            return new ErrorHandlerResult { Handled = true, ShouldContinue = true };
        }
    }

    /// <summary>
    /// Retry error handler for transient errors
    /// </summary>
    public class RetryErrorHandler : IErrorHandler
    {
        private readonly int _maxRetries;
        private readonly Dictionary<string, int> _retryCounts = new();

        public RetryErrorHandler(int maxRetries = 3)
        {
            _maxRetries = maxRetries;
        }

        public string Name => "Retry";

        public bool CanHandle(TestStandError error)
        {
            return error.Category == ErrorCategory.Network || 
                   error.Category == ErrorCategory.Instrument;
        }

        public ErrorHandlerResult Handle(TestStandError error)
        {
            var key = $"{error.Category}_{error.Code}";
            if (!_retryCounts.ContainsKey(key))
                _retryCounts[key] = 0;

            _retryCounts[key]++;

            if (_retryCounts[key] <= _maxRetries)
            {
                return new ErrorHandlerResult 
                { 
                    Handled = true, 
                    ShouldRetry = true,
                    Message = $"Retry {_retryCounts[key]} of {_maxRetries}"
                };
            }

            _retryCounts.Remove(key);
            return new ErrorHandlerResult { Handled = false };
        }
    }

    /// <summary>
    /// Error handling manager
    /// </summary>
    public class ErrorHandlingManager
    {
        private static readonly Lazy<ErrorHandlingManager> _instance = new(() => new ErrorHandlingManager());
        public static ErrorHandlingManager Instance => _instance.Value;

        private readonly List<IErrorHandler> _handlers = new();
        private readonly List<TestStandError> _errorHistory = new();
        private readonly List<ErrorRecoveryAction> _recoveryActions = new();
        private const int MaxHistorySize = 1000;

        public event EventHandler<TestStandError>? ErrorOccurred;
        public event EventHandler<TestStandError>? ErrorHandled;

        public IReadOnlyList<TestStandError> ErrorHistory => _errorHistory.AsReadOnly();
        public IReadOnlyList<ErrorRecoveryAction> RecoveryActions => _recoveryActions.AsReadOnly();

        public ErrorHandlingManager()
        {
            // Register default handlers
            _handlers.Add(new LoggingErrorHandler());
            _handlers.Add(new RetryErrorHandler());
        }

        public void RegisterHandler(IErrorHandler handler)
        {
            _handlers.Add(handler);
        }

        public void UnregisterHandler(string name)
        {
            _handlers.RemoveAll(h => h.Name == name);
        }

        public void RegisterRecoveryAction(ErrorRecoveryAction action)
        {
            _recoveryActions.Add(action);
        }

        public ErrorHandlerResult HandleError(TestStandError error)
        {
            AddToHistory(error);
            ErrorOccurred?.Invoke(this, error);

            foreach (var handler in _handlers)
            {
                if (handler.CanHandle(error))
                {
                    var result = handler.Handle(error);
                    if (result.Handled)
                    {
                        ErrorHandled?.Invoke(this, error);
                        return result;
                    }
                }
            }

            return new ErrorHandlerResult { Handled = false };
        }

        public ErrorHandlerResult HandleException(Exception ex, ErrorCategory category = ErrorCategory.General)
        {
            var error = TestStandError.FromException(ex, category);
            return HandleError(error);
        }

        public List<ErrorRecoveryAction> GetAvailableRecoveryActions(TestStandError error)
        {
            return _recoveryActions
                .Where(a => a.CanExecute?.Invoke(error) ?? true)
                .ToList();
        }

        public void ExecuteRecoveryAction(string actionId, TestStandError error)
        {
            var action = _recoveryActions.FirstOrDefault(a => a.Id == actionId);
            action?.Execute?.Invoke(error);
        }

        public void ClearHistory()
        {
            _errorHistory.Clear();
        }

        public List<TestStandError> GetErrorsByCategory(ErrorCategory category)
        {
            return _errorHistory.Where(e => e.Category == category).ToList();
        }

        public List<TestStandError> GetErrorsBySeverity(ErrorSeverity severity)
        {
            return _errorHistory.Where(e => e.Severity >= severity).ToList();
        }

        private void AddToHistory(TestStandError error)
        {
            _errorHistory.Add(error);
            if (_errorHistory.Count > MaxHistorySize)
                _errorHistory.RemoveAt(0);
        }
    }

    #endregion
}
