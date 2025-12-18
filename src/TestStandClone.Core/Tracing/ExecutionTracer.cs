using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace TestStandClone.Core.Tracing
{
    /// <summary>
    /// Types of trace events
    /// </summary>
    public enum TraceEventType
    {
        SequenceStart,
        SequenceEnd,
        StepStart,
        StepEnd,
        StatusChange,
        VariableChange,
        CallbackInvoked,
        Error,
        Warning,
        Info,
        Debug,
        Custom
    }

    /// <summary>
    /// Represents a single trace event
    /// </summary>
    public class TraceEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public TraceEventType EventType { get; set; }
        public string ThreadId { get; set; } = Environment.CurrentManagedThreadId.ToString();
        public string SequenceName { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public StepStatus? StepStatus { get; set; }
        public TimeSpan? Duration { get; set; }
        public int Depth { get; set; } = 0;
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// Trace output format
    /// </summary>
    public enum TraceOutputFormat
    {
        Text,
        Json,
        Csv,
        Xml
    }

    /// <summary>
    /// Trace filter options
    /// </summary>
    public class TraceFilter
    {
        public HashSet<TraceEventType> IncludeEventTypes { get; set; } = new();
        public HashSet<TraceEventType> ExcludeEventTypes { get; set; } = new();
        public string? SequenceNameFilter { get; set; }
        public string? StepNameFilter { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? MaxEvents { get; set; }

        public bool Matches(TraceEvent evt)
        {
            if (ExcludeEventTypes.Contains(evt.EventType)) return false;
            if (IncludeEventTypes.Count > 0 && !IncludeEventTypes.Contains(evt.EventType)) return false;
            if (!string.IsNullOrEmpty(SequenceNameFilter) && !evt.SequenceName.Contains(SequenceNameFilter, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrEmpty(StepNameFilter) && !evt.StepName.Contains(StepNameFilter, StringComparison.OrdinalIgnoreCase)) return false;
            if (StartTime.HasValue && evt.Timestamp < StartTime.Value) return false;
            if (EndTime.HasValue && evt.Timestamp > EndTime.Value) return false;
            return true;
        }
    }

    /// <summary>
    /// Interface for trace listeners
    /// </summary>
    public interface ITraceListener
    {
        void OnTraceEvent(TraceEvent evt);
        void Flush();
        void Close();
    }

    /// <summary>
    /// Console trace listener
    /// </summary>
    public class ConsoleTraceListener : ITraceListener
    {
        private readonly TraceFilter _filter = new();

        public ConsoleTraceListener(TraceFilter? filter = null)
        {
            if (filter != null) _filter = filter;
        }

        public void OnTraceEvent(TraceEvent evt)
        {
            if (!_filter.Matches(evt)) return;

            var indent = new string(' ', evt.Depth * 2);
            var prefix = evt.EventType switch
            {
                TraceEventType.SequenceStart => "[SEQ START]",
                TraceEventType.SequenceEnd => "[SEQ END]",
                TraceEventType.StepStart => "[STEP >>]",
                TraceEventType.StepEnd => "[STEP <<]",
                TraceEventType.Error => "[ERROR]",
                TraceEventType.Warning => "[WARN]",
                TraceEventType.Info => "[INFO]",
                TraceEventType.Debug => "[DEBUG]",
                _ => $"[{evt.EventType}]"
            };

            Console.WriteLine($"{evt.Timestamp:HH:mm:ss.fff} {prefix} {indent}{evt.Message}");
        }

        public void Flush() { }
        public void Close() { }
    }

    /// <summary>
    /// File trace listener
    /// </summary>
    public class FileTraceListener : ITraceListener, IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly TraceOutputFormat _format;
        private readonly TraceFilter _filter = new();
        private bool _disposed;
        private readonly object _lock = new();

        public FileTraceListener(string filePath, TraceOutputFormat format = TraceOutputFormat.Text, TraceFilter? filter = null)
        {
            _format = format;
            if (filter != null) _filter = filter;

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _writer = new StreamWriter(filePath, append: false);
            WriteHeader();
        }

        private void WriteHeader()
        {
            switch (_format)
            {
                case TraceOutputFormat.Csv:
                    _writer.WriteLine("Timestamp,EventType,ThreadId,SequenceName,StepName,Status,Duration,Message");
                    break;
                case TraceOutputFormat.Json:
                    _writer.WriteLine("[");
                    break;
                case TraceOutputFormat.Xml:
                    _writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    _writer.WriteLine("<TraceLog>");
                    break;
            }
        }

        public void OnTraceEvent(TraceEvent evt)
        {
            if (!_filter.Matches(evt)) return;
            lock (_lock)
            {
                if (_disposed) return;

                switch (_format)
                {
                    case TraceOutputFormat.Text:
                        var indent = new string(' ', evt.Depth * 2);
                        var duration = evt.Duration.HasValue ? $" ({evt.Duration.Value.TotalMilliseconds:F2}ms)" : "";
                        _writer.WriteLine($"{evt.Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{evt.EventType}] {indent}{evt.Message}{duration}");
                        break;

                    case TraceOutputFormat.Csv:
                        _writer.WriteLine($"{evt.Timestamp:O},{evt.EventType},{evt.ThreadId},{EscapeCsv(evt.SequenceName)},{EscapeCsv(evt.StepName)},{evt.StepStatus},{evt.Duration?.TotalMilliseconds},{EscapeCsv(evt.Message)}");
                        break;

                    case TraceOutputFormat.Json:
                        var json = JsonSerializer.Serialize(evt);
                        _writer.WriteLine($"  {json},");
                        break;

                    case TraceOutputFormat.Xml:
                        _writer.WriteLine($"  <Event Timestamp=\"{evt.Timestamp:O}\" Type=\"{evt.EventType}\">");
                        _writer.WriteLine($"    <Sequence>{System.Security.SecurityElement.Escape(evt.SequenceName)}</Sequence>");
                        _writer.WriteLine($"    <Step>{System.Security.SecurityElement.Escape(evt.StepName)}</Step>");
                        _writer.WriteLine($"    <Message>{System.Security.SecurityElement.Escape(evt.Message)}</Message>");
                        _writer.WriteLine($"  </Event>");
                        break;
                }
            }
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        public void Flush()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    _writer.Flush();
                }
            }
        }

        public void Close()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    switch (_format)
                    {
                        case TraceOutputFormat.Json:
                            _writer.WriteLine("]");
                            break;
                        case TraceOutputFormat.Xml:
                            _writer.WriteLine("</TraceLog>");
                            break;
                    }
                    _writer.Close();
                    _disposed = true;
                }
            }
        }

        public void Dispose()
        {
            Close();
        }
    }

    /// <summary>
    /// Memory trace listener for in-memory trace collection
    /// </summary>
    public class MemoryTraceListener : ITraceListener
    {
        private readonly ConcurrentQueue<TraceEvent> _events = new();
        private readonly int _maxEvents;
        private readonly TraceFilter _filter = new();

        public MemoryTraceListener(int maxEvents = 10000, TraceFilter? filter = null)
        {
            _maxEvents = maxEvents;
            if (filter != null) _filter = filter;
        }

        public void OnTraceEvent(TraceEvent evt)
        {
            if (!_filter.Matches(evt)) return;

            _events.Enqueue(evt);

            // Remove old events if over limit
            while (_events.Count > _maxEvents)
            {
                _events.TryDequeue(out _);
            }
        }

        public IEnumerable<TraceEvent> GetEvents() => _events.ToArray();

        public IEnumerable<TraceEvent> GetEvents(TraceFilter filter)
        {
            return _events.Where(e => filter.Matches(e));
        }

        public void Clear()
        {
            while (_events.TryDequeue(out _)) { }
        }

        public void Flush() { }
        public void Close() { }
    }

    /// <summary>
    /// Manages execution tracing for sequences and steps
    /// Similar to TestStand Execution Trace
    /// </summary>
    public class ExecutionTracer
    {
        private static readonly Lazy<ExecutionTracer> _instance = new(() => new ExecutionTracer());
        public static ExecutionTracer Instance => _instance.Value;

        private readonly List<ITraceListener> _listeners = new();
        private readonly object _listenersLock = new();
        private bool _isEnabled = true;
        private int _currentDepth = 0;

        private ExecutionTracer() { }

        /// <summary>
        /// Whether tracing is enabled
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// Add a trace listener
        /// </summary>
        public void AddListener(ITraceListener listener)
        {
            lock (_listenersLock)
            {
                _listeners.Add(listener);
            }
        }

        /// <summary>
        /// Remove a trace listener
        /// </summary>
        public void RemoveListener(ITraceListener listener)
        {
            lock (_listenersLock)
            {
                _listeners.Remove(listener);
                listener.Close();
            }
        }

        /// <summary>
        /// Clear all listeners
        /// </summary>
        public void ClearListeners()
        {
            lock (_listenersLock)
            {
                foreach (var listener in _listeners)
                {
                    listener.Close();
                }
                _listeners.Clear();
            }
        }

        /// <summary>
        /// Trace a sequence start
        /// </summary>
        public void TraceSequenceStart(string sequenceName)
        {
            Trace(new TraceEvent
            {
                EventType = TraceEventType.SequenceStart,
                SequenceName = sequenceName,
                Message = $"Sequence '{sequenceName}' started",
                Depth = _currentDepth
            });
            _currentDepth++;
        }

        /// <summary>
        /// Trace a sequence end
        /// </summary>
        public void TraceSequenceEnd(string sequenceName, StepStatus status, TimeSpan duration)
        {
            _currentDepth = Math.Max(0, _currentDepth - 1);
            Trace(new TraceEvent
            {
                EventType = TraceEventType.SequenceEnd,
                SequenceName = sequenceName,
                StepStatus = status,
                Duration = duration,
                Message = $"Sequence '{sequenceName}' ended with status: {status}",
                Depth = _currentDepth
            });
        }

        /// <summary>
        /// Trace a step start
        /// </summary>
        public void TraceStepStart(string sequenceName, string stepName)
        {
            Trace(new TraceEvent
            {
                EventType = TraceEventType.StepStart,
                SequenceName = sequenceName,
                StepName = stepName,
                Message = $"Step '{stepName}' started",
                Depth = _currentDepth
            });
        }

        /// <summary>
        /// Trace a step end
        /// </summary>
        public void TraceStepEnd(string sequenceName, string stepName, StepStatus status, TimeSpan duration)
        {
            Trace(new TraceEvent
            {
                EventType = TraceEventType.StepEnd,
                SequenceName = sequenceName,
                StepName = stepName,
                StepStatus = status,
                Duration = duration,
                Message = $"Step '{stepName}' ended with status: {status}",
                Depth = _currentDepth
            });
        }

        /// <summary>
        /// Trace a status change
        /// </summary>
        public void TraceStatusChange(string sequenceName, string stepName, StepStatus oldStatus, StepStatus newStatus)
        {
            Trace(new TraceEvent
            {
                EventType = TraceEventType.StatusChange,
                SequenceName = sequenceName,
                StepName = stepName,
                StepStatus = newStatus,
                Message = $"Step '{stepName}' status changed from {oldStatus} to {newStatus}",
                Depth = _currentDepth
            });
        }

        /// <summary>
        /// Trace an error
        /// </summary>
        public void TraceError(string sequenceName, string stepName, string message, Exception? ex = null)
        {
            var evt = new TraceEvent
            {
                EventType = TraceEventType.Error,
                SequenceName = sequenceName,
                StepName = stepName,
                Message = message,
                Depth = _currentDepth
            };

            if (ex != null)
            {
                evt.Data["ExceptionType"] = ex.GetType().Name;
                evt.Data["ExceptionMessage"] = ex.Message;
                evt.Data["StackTrace"] = ex.StackTrace ?? "";
            }

            Trace(evt);
        }

        /// <summary>
        /// Trace a warning
        /// </summary>
        public void TraceWarning(string message)
        {
            Trace(new TraceEvent
            {
                EventType = TraceEventType.Warning,
                Message = message,
                Depth = _currentDepth
            });
        }

        /// <summary>
        /// Trace info message
        /// </summary>
        public void TraceInfo(string message)
        {
            Trace(new TraceEvent
            {
                EventType = TraceEventType.Info,
                Message = message,
                Depth = _currentDepth
            });
        }

        /// <summary>
        /// Trace debug message
        /// </summary>
        public void TraceDebug(string message)
        {
            Trace(new TraceEvent
            {
                EventType = TraceEventType.Debug,
                Message = message,
                Depth = _currentDepth
            });
        }

        /// <summary>
        /// Trace a custom event
        /// </summary>
        public void TraceCustom(string message, Dictionary<string, object>? data = null)
        {
            var evt = new TraceEvent
            {
                EventType = TraceEventType.Custom,
                Message = message,
                Depth = _currentDepth
            };

            if (data != null)
            {
                evt.Data = data;
            }

            Trace(evt);
        }

        /// <summary>
        /// Send trace event to all listeners
        /// </summary>
        private void Trace(TraceEvent evt)
        {
            if (!_isEnabled) return;

            lock (_listenersLock)
            {
                foreach (var listener in _listeners)
                {
                    try
                    {
                        listener.OnTraceEvent(evt);
                    }
                    catch
                    {
                        // Ignore listener errors
                    }
                }
            }
        }

        /// <summary>
        /// Flush all listeners
        /// </summary>
        public void Flush()
        {
            lock (_listenersLock)
            {
                foreach (var listener in _listeners)
                {
                    listener.Flush();
                }
            }
        }

        /// <summary>
        /// Reset depth counter
        /// </summary>
        public void ResetDepth()
        {
            _currentDepth = 0;
        }
    }
}
