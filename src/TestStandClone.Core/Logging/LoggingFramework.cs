// Copyright (c) TestStand Clone. All rights reserved.
// Enhanced logging framework implementation

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestStandClone.Core.Logging
{
    /// <summary>
    /// Log level enumeration
    /// </summary>
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5
    }

    /// <summary>
    /// Log category for filtering
    /// </summary>
    public enum LogCategory
    {
        General,
        Execution,
        Step,
        Sequence,
        Engine,
        Report,
        Database,
        Instrument,
        User,
        System,
        Custom
    }

    /// <summary>
    /// Represents a log entry
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Timestamp of the log entry
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Log level
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Log category
        /// </summary>
        public LogCategory Category { get; set; }

        /// <summary>
        /// Source of the log entry
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Log message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Exception if any
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Thread ID
        /// </summary>
        public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;

        /// <summary>
        /// Additional data
        /// </summary>
        public Dictionary<string, object> Data { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Format the log entry as a string
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");
            sb.Append($"[{Level,-7}] ");
            sb.Append($"[{Category,-10}] ");
            
            if (!string.IsNullOrEmpty(Source))
                sb.Append($"[{Source}] ");
            
            sb.Append(Message);
            
            if (Exception != null)
            {
                sb.AppendLine();
                sb.Append($"  Exception: {Exception.GetType().Name}: {Exception.Message}");
                if (Exception.StackTrace != null)
                {
                    sb.AppendLine();
                    sb.Append($"  StackTrace: {Exception.StackTrace}");
                }
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Format as JSON
        /// </summary>
        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"timestamp\": \"{Timestamp:o}\",");
            sb.AppendLine($"  \"level\": \"{Level}\",");
            sb.AppendLine($"  \"category\": \"{Category}\",");
            sb.AppendLine($"  \"source\": \"{EscapeJson(Source)}\",");
            sb.AppendLine($"  \"message\": \"{EscapeJson(Message)}\",");
            sb.AppendLine($"  \"threadId\": {ThreadId}");
            
            if (Exception != null)
            {
                sb.AppendLine($"  ,\"exception\": {{");
                sb.AppendLine($"    \"type\": \"{Exception.GetType().FullName}\",");
                sb.AppendLine($"    \"message\": \"{EscapeJson(Exception.Message)}\"");
                sb.AppendLine($"  }}");
            }
            
            if (Data.Count > 0)
            {
                sb.AppendLine($"  ,\"data\": {{");
                var dataItems = Data.Select(kv => $"    \"{EscapeJson(kv.Key)}\": \"{EscapeJson(kv.Value?.ToString() ?? "")}\"");
                sb.AppendLine(string.Join(",\n", dataItems));
                sb.AppendLine($"  }}");
            }
            
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }

    /// <summary>
    /// Interface for log writers
    /// </summary>
    public interface ILogWriter : IDisposable
    {
        /// <summary>
        /// Name of the log writer
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Whether the writer is enabled
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Minimum log level to write
        /// </summary>
        LogLevel MinLevel { get; set; }

        /// <summary>
        /// Write a log entry
        /// </summary>
        void Write(LogEntry entry);

        /// <summary>
        /// Flush any buffered entries
        /// </summary>
        void Flush();
    }

    /// <summary>
    /// Console log writer
    /// </summary>
    public class ConsoleLogWriter : ILogWriter
    {
        private static readonly object _lock = new object();

        public string Name => "Console";
        public bool IsEnabled { get; set; } = true;
        public LogLevel MinLevel { get; set; } = LogLevel.Info;

        public void Write(LogEntry entry)
        {
            if (!IsEnabled || entry.Level < MinLevel)
                return;

            lock (_lock)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = GetColor(entry.Level);
                Console.WriteLine(entry.ToString());
                Console.ForegroundColor = originalColor;
            }
        }

        public void Flush() { }
        public void Dispose() { }

        private static ConsoleColor GetColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => ConsoleColor.Gray,
                LogLevel.Debug => ConsoleColor.Cyan,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Fatal => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
        }
    }

    /// <summary>
    /// File log writer
    /// </summary>
    public class FileLogWriter : ILogWriter
    {
        private readonly string _filePath;
        private readonly object _lock = new object();
        private StreamWriter? _writer;
        private readonly int _maxFileSizeBytes;
        private readonly int _maxBackupFiles;

        public string Name => "File";
        public bool IsEnabled { get; set; } = true;
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;
        public bool UseJson { get; set; } = false;

        public FileLogWriter(string filePath, int maxFileSizeBytes = 10 * 1024 * 1024, int maxBackupFiles = 5)
        {
            _filePath = filePath;
            _maxFileSizeBytes = maxFileSizeBytes;
            _maxBackupFiles = maxBackupFiles;
            
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            OpenWriter();
        }

        public void Write(LogEntry entry)
        {
            if (!IsEnabled || entry.Level < MinLevel)
                return;

            lock (_lock)
            {
                CheckRotation();
                
                var text = UseJson ? entry.ToJson() : entry.ToString();
                _writer?.WriteLine(text);
            }
        }

        public void Flush()
        {
            lock (_lock)
            {
                _writer?.Flush();
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _writer?.Dispose();
                _writer = null;
            }
        }

        private void OpenWriter()
        {
            _writer = new StreamWriter(_filePath, append: true, Encoding.UTF8)
            {
                AutoFlush = true
            };
        }

        private void CheckRotation()
        {
            if (_writer == null)
                return;

            var fileInfo = new FileInfo(_filePath);
            if (!fileInfo.Exists || fileInfo.Length < _maxFileSizeBytes)
                return;

            _writer.Dispose();
            RotateFiles();
            OpenWriter();
        }

        private void RotateFiles()
        {
            for (int i = _maxBackupFiles - 1; i >= 1; i--)
            {
                var source = $"{_filePath}.{i}";
                var target = $"{_filePath}.{i + 1}";
                
                if (File.Exists(target))
                    File.Delete(target);
                
                if (File.Exists(source))
                    File.Move(source, target);
            }

            var firstBackup = $"{_filePath}.1";
            if (File.Exists(firstBackup))
                File.Delete(firstBackup);
            
            if (File.Exists(_filePath))
                File.Move(_filePath, firstBackup);
        }
    }

    /// <summary>
    /// Memory log writer for debugging
    /// </summary>
    public class MemoryLogWriter : ILogWriter
    {
        private readonly ConcurrentQueue<LogEntry> _entries = new ConcurrentQueue<LogEntry>();
        private readonly int _maxEntries;

        public string Name => "Memory";
        public bool IsEnabled { get; set; } = true;
        public LogLevel MinLevel { get; set; } = LogLevel.Trace;

        public MemoryLogWriter(int maxEntries = 10000)
        {
            _maxEntries = maxEntries;
        }

        public void Write(LogEntry entry)
        {
            if (!IsEnabled || entry.Level < MinLevel)
                return;

            _entries.Enqueue(entry);
            
            while (_entries.Count > _maxEntries)
            {
                _entries.TryDequeue(out _);
            }
        }

        public void Flush() { }
        public void Dispose() { }

        public IEnumerable<LogEntry> GetEntries() => _entries.ToList();
        
        public IEnumerable<LogEntry> GetEntries(LogLevel minLevel) => 
            _entries.Where(e => e.Level >= minLevel);
        
        public IEnumerable<LogEntry> GetEntries(LogCategory category) => 
            _entries.Where(e => e.Category == category);

        public void Clear()
        {
            while (_entries.TryDequeue(out _)) { }
        }
    }

    /// <summary>
    /// Async buffered log writer
    /// </summary>
    public class AsyncLogWriter : ILogWriter, IDisposable
    {
        private readonly ILogWriter _innerWriter;
        private readonly BlockingCollection<LogEntry> _queue;
        private readonly Task _writerTask;
        private readonly CancellationTokenSource _cts;

        public string Name => $"Async({_innerWriter.Name})";
        public bool IsEnabled { get; set; } = true;
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;

        public AsyncLogWriter(ILogWriter innerWriter, int bufferSize = 1000)
        {
            _innerWriter = innerWriter;
            _queue = new BlockingCollection<LogEntry>(bufferSize);
            _cts = new CancellationTokenSource();
            
            _writerTask = Task.Factory.StartNew(
                ProcessQueue, 
                _cts.Token, 
                TaskCreationOptions.LongRunning, 
                TaskScheduler.Default);
        }

        public void Write(LogEntry entry)
        {
            if (!IsEnabled || entry.Level < MinLevel)
                return;

            if (!_queue.IsAddingCompleted)
            {
                try
                {
                    _queue.Add(entry);
                }
                catch (InvalidOperationException)
                {
                    // Queue was completed
                }
            }
        }

        public void Flush()
        {
            // Wait for queue to drain
            while (_queue.Count > 0)
            {
                Thread.Sleep(10);
            }
            _innerWriter.Flush();
        }

        public void Dispose()
        {
            _queue.CompleteAdding();
            _cts.Cancel();
            
            try
            {
                _writerTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch { }
            
            _innerWriter.Dispose();
            _queue.Dispose();
            _cts.Dispose();
        }

        private void ProcessQueue()
        {
            foreach (var entry in _queue.GetConsumingEnumerable(_cts.Token))
            {
                try
                {
                    _innerWriter.Write(entry);
                }
                catch
                {
                    // Ignore write errors in async context
                }
            }
        }
    }

    /// <summary>
    /// Central logging manager
    /// </summary>
    public sealed class Logger
    {
        private static readonly Lazy<Logger> _instance = 
            new Lazy<Logger>(() => new Logger());

        private readonly List<ILogWriter> _writers = new List<ILogWriter>();
        private readonly object _lock = new object();
        private LogLevel _globalMinLevel = LogLevel.Trace;
        private readonly HashSet<LogCategory> _enabledCategories;

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static Logger Instance => _instance.Value;

        /// <summary>
        /// Global minimum log level
        /// </summary>
        public LogLevel GlobalMinLevel
        {
            get => _globalMinLevel;
            set => _globalMinLevel = value;
        }

        /// <summary>
        /// Event raised when a log entry is written
        /// </summary>
        public event EventHandler<LogEntry>? LogWritten;

        private Logger()
        {
            _enabledCategories = new HashSet<LogCategory>(Enum.GetValues(typeof(LogCategory)).Cast<LogCategory>());
        }

        /// <summary>
        /// Add a log writer
        /// </summary>
        public void AddWriter(ILogWriter writer)
        {
            lock (_lock)
            {
                _writers.Add(writer);
            }
        }

        /// <summary>
        /// Remove a log writer
        /// </summary>
        public bool RemoveWriter(string name)
        {
            lock (_lock)
            {
                var writer = _writers.FirstOrDefault(w => w.Name == name);
                if (writer != null)
                {
                    _writers.Remove(writer);
                    writer.Dispose();
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Enable a log category
        /// </summary>
        public void EnableCategory(LogCategory category)
        {
            lock (_lock)
            {
                _enabledCategories.Add(category);
            }
        }

        /// <summary>
        /// Disable a log category
        /// </summary>
        public void DisableCategory(LogCategory category)
        {
            lock (_lock)
            {
                _enabledCategories.Remove(category);
            }
        }

        /// <summary>
        /// Write a log entry
        /// </summary>
        public void Log(LogEntry entry)
        {
            if (entry.Level < _globalMinLevel)
                return;

            lock (_lock)
            {
                if (!_enabledCategories.Contains(entry.Category))
                    return;

                foreach (var writer in _writers)
                {
                    try
                    {
                        writer.Write(entry);
                    }
                    catch
                    {
                        // Ignore individual writer errors
                    }
                }
            }

            LogWritten?.Invoke(this, entry);
        }

        /// <summary>
        /// Write a log message
        /// </summary>
        public void Log(LogLevel level, LogCategory category, string source, string message, 
            Exception? exception = null)
        {
            Log(new LogEntry
            {
                Level = level,
                Category = category,
                Source = source,
                Message = message,
                Exception = exception
            });
        }

        // Convenience methods
        public void Trace(string message, string source = "") => 
            Log(LogLevel.Trace, LogCategory.General, source, message);
        
        public void Debug(string message, string source = "") => 
            Log(LogLevel.Debug, LogCategory.General, source, message);
        
        public void Info(string message, string source = "") => 
            Log(LogLevel.Info, LogCategory.General, source, message);
        
        public void Warning(string message, string source = "") => 
            Log(LogLevel.Warning, LogCategory.General, source, message);
        
        public void Error(string message, Exception? exception = null, string source = "") => 
            Log(LogLevel.Error, LogCategory.General, source, message, exception);
        
        public void Fatal(string message, Exception? exception = null, string source = "") => 
            Log(LogLevel.Fatal, LogCategory.General, source, message, exception);

        // Category-specific methods
        public void LogExecution(LogLevel level, string message, string source = "") => 
            Log(level, LogCategory.Execution, source, message);
        
        public void LogStep(LogLevel level, string stepName, string message) => 
            Log(level, LogCategory.Step, stepName, message);
        
        public void LogSequence(LogLevel level, string sequenceName, string message) => 
            Log(level, LogCategory.Sequence, sequenceName, message);
        
        public void LogEngine(LogLevel level, string message) => 
            Log(level, LogCategory.Engine, "Engine", message);
        
        public void LogInstrument(LogLevel level, string instrumentName, string message) => 
            Log(level, LogCategory.Instrument, instrumentName, message);

        /// <summary>
        /// Flush all writers
        /// </summary>
        public void Flush()
        {
            lock (_lock)
            {
                foreach (var writer in _writers)
                {
                    try
                    {
                        writer.Flush();
                    }
                    catch
                    {
                        // Ignore flush errors
                    }
                }
            }
        }

        /// <summary>
        /// Clear all writers
        /// </summary>
        public void ClearWriters()
        {
            lock (_lock)
            {
                foreach (var writer in _writers)
                {
                    try
                    {
                        writer.Dispose();
                    }
                    catch
                    {
                        // Ignore dispose errors
                    }
                }
                _writers.Clear();
            }
        }
    }
}
