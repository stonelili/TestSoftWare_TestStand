// Copyright (c) TestStand Clone. All rights reserved.
// Process model callbacks implementation

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TestStandClone.Core.ModelCallbacks
{
    /// <summary>
    /// Model callback types similar to TestStand process model callbacks
    /// </summary>
    public enum ModelCallbackType
    {
        // Pre-UUT Loop callbacks
        PreUUTLoop,
        PostUUTLoop,
        
        // UUT callbacks
        NewUUT,
        PreUUT,
        MainSequence,
        PostUUT,
        
        // Batch callbacks
        PreBatch,
        PostBatch,
        
        // Station callbacks
        PreStation,
        PostStation,
        StationIdle,
        
        // Error callbacks
        ProcessModelError,
        SequenceError,
        
        // Logging callbacks
        ProcessModelLog,
        ReportGeneration,
        
        // Model configuration
        ModelConfiguration,
        
        // User input callbacks
        GetSerialNumber,
        GetBatchInfo,
        GetTestSocketSelection
    }

    /// <summary>
    /// Represents a model callback configuration
    /// </summary>
    public class ModelCallback
    {
        /// <summary>
        /// Type of callback
        /// </summary>
        public ModelCallbackType Type { get; set; }

        /// <summary>
        /// Name of the callback
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the callback
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Whether the callback is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Priority for execution order (lower = higher priority)
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// Sequence to execute for this callback
        /// </summary>
        public Sequence? CallbackSequence { get; set; }

        /// <summary>
        /// Action to execute for this callback
        /// </summary>
        public Func<ModelCallbackContext, CancellationToken, Task>? CallbackAction { get; set; }

        /// <summary>
        /// Whether to continue on error
        /// </summary>
        public bool ContinueOnError { get; set; } = true;

        /// <summary>
        /// Timeout in milliseconds (0 = no timeout)
        /// </summary>
        public int TimeoutMs { get; set; } = 0;
    }

    /// <summary>
    /// Context passed to model callbacks
    /// </summary>
    public class ModelCallbackContext
    {
        /// <summary>
        /// Current UUT serial number
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// Current test socket ID
        /// </summary>
        public int TestSocketId { get; set; }

        /// <summary>
        /// Current batch ID
        /// </summary>
        public string BatchId { get; set; } = string.Empty;

        /// <summary>
        /// Current sequence
        /// </summary>
        public Sequence? CurrentSequence { get; set; }

        /// <summary>
        /// Current step
        /// </summary>
        public TestStep? CurrentStep { get; set; }

        /// <summary>
        /// Overall result (true = pass)
        /// </summary>
        public bool Result { get; set; } = true;

        /// <summary>
        /// Error information if any
        /// </summary>
        public Exception? Error { get; set; }

        /// <summary>
        /// Execution context
        /// </summary>
        public Context? ExecutionContext { get; set; }

        /// <summary>
        /// Custom properties
        /// </summary>
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Get a property value
        /// </summary>
        public T? GetProperty<T>(string name)
        {
            if (Properties.TryGetValue(name, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }

        /// <summary>
        /// Set a property value
        /// </summary>
        public void SetProperty(string name, object value)
        {
            Properties[name] = value;
        }
    }

    /// <summary>
    /// Result of a callback execution
    /// </summary>
    public class ModelCallbackResult
    {
        /// <summary>
        /// Whether the callback succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Exception if any
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Execution time in milliseconds
        /// </summary>
        public double ExecutionTimeMs { get; set; }

        /// <summary>
        /// Output data from the callback
        /// </summary>
        public Dictionary<string, object> OutputData { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Create a success result
        /// </summary>
        public static ModelCallbackResult Succeeded(double executionTimeMs = 0)
        {
            return new ModelCallbackResult
            {
                Success = true,
                ExecutionTimeMs = executionTimeMs
            };
        }

        /// <summary>
        /// Create a failure result
        /// </summary>
        public static ModelCallbackResult Failed(string message, Exception? exception = null)
        {
            return new ModelCallbackResult
            {
                Success = false,
                ErrorMessage = message,
                Exception = exception
            };
        }
    }

    /// <summary>
    /// Event arguments for model callback events
    /// </summary>
    public class ModelCallbackEventArgs : EventArgs
    {
        public ModelCallback Callback { get; }
        public ModelCallbackContext Context { get; }
        public ModelCallbackResult? Result { get; }

        public ModelCallbackEventArgs(ModelCallback callback, ModelCallbackContext context, 
            ModelCallbackResult? result = null)
        {
            Callback = callback;
            Context = context;
            Result = result;
        }
    }

    /// <summary>
    /// Manages process model callbacks
    /// </summary>
    public sealed class ModelCallbackManager
    {
        private static readonly Lazy<ModelCallbackManager> _instance = 
            new Lazy<ModelCallbackManager>(() => new ModelCallbackManager());

        private readonly Dictionary<ModelCallbackType, List<ModelCallback>> _callbacks = 
            new Dictionary<ModelCallbackType, List<ModelCallback>>();

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static ModelCallbackManager Instance => _instance.Value;

        /// <summary>
        /// Event raised before a callback executes
        /// </summary>
        public event EventHandler<ModelCallbackEventArgs>? BeforeCallback;

        /// <summary>
        /// Event raised after a callback executes
        /// </summary>
        public event EventHandler<ModelCallbackEventArgs>? AfterCallback;

        /// <summary>
        /// Event raised when a callback fails
        /// </summary>
        public event EventHandler<ModelCallbackEventArgs>? CallbackError;

        private ModelCallbackManager() 
        {
            // Initialize callback lists for each type
            foreach (ModelCallbackType type in Enum.GetValues(typeof(ModelCallbackType)))
            {
                _callbacks[type] = new List<ModelCallback>();
            }
        }

        /// <summary>
        /// Register a callback
        /// </summary>
        public void RegisterCallback(ModelCallback callback)
        {
            if (!_callbacks.ContainsKey(callback.Type))
                _callbacks[callback.Type] = new List<ModelCallback>();

            _callbacks[callback.Type].Add(callback);
            _callbacks[callback.Type] = _callbacks[callback.Type]
                .OrderBy(c => c.Priority)
                .ToList();
        }

        /// <summary>
        /// Register a callback action
        /// </summary>
        public void RegisterCallback(ModelCallbackType type, string name,
            Func<ModelCallbackContext, CancellationToken, Task> action,
            int priority = 100)
        {
            RegisterCallback(new ModelCallback
            {
                Type = type,
                Name = name,
                CallbackAction = action,
                Priority = priority
            });
        }

        /// <summary>
        /// Register a callback sequence
        /// </summary>
        public void RegisterCallback(ModelCallbackType type, string name,
            Sequence sequence, int priority = 100)
        {
            RegisterCallback(new ModelCallback
            {
                Type = type,
                Name = name,
                CallbackSequence = sequence,
                Priority = priority
            });
        }

        /// <summary>
        /// Unregister a callback by name
        /// </summary>
        public bool UnregisterCallback(ModelCallbackType type, string name)
        {
            if (_callbacks.TryGetValue(type, out var list))
            {
                return list.RemoveAll(c => c.Name == name) > 0;
            }
            return false;
        }

        /// <summary>
        /// Get all callbacks of a specific type
        /// </summary>
        public IEnumerable<ModelCallback> GetCallbacks(ModelCallbackType type)
        {
            return _callbacks.TryGetValue(type, out var list) ? list.AsReadOnly() : Enumerable.Empty<ModelCallback>();
        }

        /// <summary>
        /// Execute all callbacks of a specific type
        /// </summary>
        public async Task<List<ModelCallbackResult>> ExecuteCallbacksAsync(
            ModelCallbackType type,
            ModelCallbackContext context,
            CancellationToken cancellationToken = default)
        {
            var results = new List<ModelCallbackResult>();

            if (!_callbacks.TryGetValue(type, out var callbacks))
                return results;

            foreach (var callback in callbacks.Where(c => c.IsEnabled))
            {
                var result = await ExecuteCallbackAsync(callback, context, cancellationToken);
                results.Add(result);

                if (!result.Success && !callback.ContinueOnError)
                    break;
            }

            return results;
        }

        /// <summary>
        /// Execute a single callback
        /// </summary>
        public async Task<ModelCallbackResult> ExecuteCallbackAsync(
            ModelCallback callback,
            ModelCallbackContext context,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            BeforeCallback?.Invoke(this, new ModelCallbackEventArgs(callback, context));

            try
            {
                using var cts = callback.TimeoutMs > 0
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                    : null;

                if (cts != null)
                    cts.CancelAfter(callback.TimeoutMs);

                var token = cts?.Token ?? cancellationToken;

                if (callback.CallbackAction != null)
                {
                    await callback.CallbackAction(context, token);
                }
                else if (callback.CallbackSequence != null)
                {
                    var engine = new Engine();
                    await engine.ExecuteSequenceAsync(callback.CallbackSequence);
                }

                var executionTime = (DateTime.Now - startTime).TotalMilliseconds;
                var result = ModelCallbackResult.Succeeded(executionTime);
                
                AfterCallback?.Invoke(this, new ModelCallbackEventArgs(callback, context, result));
                
                return result;
            }
            catch (OperationCanceledException)
            {
                var result = ModelCallbackResult.Failed("Callback was cancelled or timed out");
                CallbackError?.Invoke(this, new ModelCallbackEventArgs(callback, context, result));
                return result;
            }
            catch (Exception ex)
            {
                var result = ModelCallbackResult.Failed(ex.Message, ex);
                CallbackError?.Invoke(this, new ModelCallbackEventArgs(callback, context, result));
                return result;
            }
        }

        /// <summary>
        /// Clear all callbacks
        /// </summary>
        public void ClearCallbacks()
        {
            foreach (var list in _callbacks.Values)
            {
                list.Clear();
            }
        }

        /// <summary>
        /// Clear callbacks of a specific type
        /// </summary>
        public void ClearCallbacks(ModelCallbackType type)
        {
            if (_callbacks.TryGetValue(type, out var list))
            {
                list.Clear();
            }
        }

        /// <summary>
        /// Enable/disable a callback by name
        /// </summary>
        public bool SetCallbackEnabled(ModelCallbackType type, string name, bool enabled)
        {
            if (_callbacks.TryGetValue(type, out var list))
            {
                var callback = list.FirstOrDefault(c => c.Name == name);
                if (callback != null)
                {
                    callback.IsEnabled = enabled;
                    return true;
                }
            }
            return false;
        }
    }
}
