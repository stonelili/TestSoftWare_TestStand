using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace TestStandClone.Core.Profiling
{
    /// <summary>
    /// Profile event type
    /// </summary>
    public enum ProfileEventType
    {
        StepStart,
        StepEnd,
        SequenceStart,
        SequenceEnd,
        EngineStart,
        EngineEnd,
        CallbackStart,
        CallbackEnd,
        ExpressionEval,
        Custom
    }

    /// <summary>
    /// A single profile event
    /// </summary>
    public class ProfileEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ProfileEventType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public long ElapsedTicks { get; set; }
        public double ElapsedMs => ElapsedTicks * 1000.0 / Stopwatch.Frequency;
        public int ThreadId { get; set; } = Thread.CurrentThread.ManagedThreadId;
        public string? ParentId { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public long MemoryUsage { get; set; }
    }

    /// <summary>
    /// Profile session containing all events
    /// </summary>
    public class ProfileSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
        public List<ProfileEvent> Events { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Get events by type
        /// </summary>
        public IEnumerable<ProfileEvent> GetEventsByType(ProfileEventType type)
        {
            return Events.Where(e => e.Type == type);
        }

        /// <summary>
        /// Get events by category
        /// </summary>
        public IEnumerable<ProfileEvent> GetEventsByCategory(string category)
        {
            return Events.Where(e => e.Category == category);
        }

        /// <summary>
        /// Get step timing statistics
        /// </summary>
        public ProfileStatistics GetStatistics()
        {
            var stats = new ProfileStatistics();

            var stepEvents = Events.Where(e => e.Type == ProfileEventType.StepEnd).ToList();
            if (stepEvents.Any())
            {
                stats.TotalSteps = stepEvents.Count;
                stats.TotalStepTimeMs = stepEvents.Sum(e => e.ElapsedMs);
                stats.AverageStepTimeMs = stepEvents.Average(e => e.ElapsedMs);
                stats.MinStepTimeMs = stepEvents.Min(e => e.ElapsedMs);
                stats.MaxStepTimeMs = stepEvents.Max(e => e.ElapsedMs);

                stats.SlowestSteps = stepEvents
                    .OrderByDescending(e => e.ElapsedMs)
                    .Take(10)
                    .Select(e => new StepTiming { Name = e.Name, ElapsedMs = e.ElapsedMs })
                    .ToList();

                stats.StepCountByCategory = stepEvents
                    .GroupBy(e => e.Category)
                    .ToDictionary(g => g.Key, g => g.Count());

                stats.TimeByCategory = stepEvents
                    .GroupBy(e => e.Category)
                    .ToDictionary(g => g.Key, g => g.Sum(e => e.ElapsedMs));
            }

            stats.TotalDurationMs = Duration.TotalMilliseconds;
            stats.PeakMemoryBytes = Events.Count > 0 ? Events.Max(e => e.MemoryUsage) : 0;

            return stats;
        }
    }

    /// <summary>
    /// Step timing information
    /// </summary>
    public class StepTiming
    {
        public string Name { get; set; } = string.Empty;
        public double ElapsedMs { get; set; }
    }

    /// <summary>
    /// Profile statistics
    /// </summary>
    public class ProfileStatistics
    {
        public int TotalSteps { get; set; }
        public double TotalStepTimeMs { get; set; }
        public double AverageStepTimeMs { get; set; }
        public double MinStepTimeMs { get; set; }
        public double MaxStepTimeMs { get; set; }
        public double TotalDurationMs { get; set; }
        public long PeakMemoryBytes { get; set; }
        public List<StepTiming> SlowestSteps { get; set; } = new();
        public Dictionary<string, int> StepCountByCategory { get; set; } = new();
        public Dictionary<string, double> TimeByCategory { get; set; } = new();
    }

    /// <summary>
    /// Scope for automatic profiling with using statement
    /// </summary>
    public class ProfileScope : IDisposable
    {
        private readonly ExecutionProfiler _profiler;
        private readonly ProfileEventType _endType;
        private readonly string _name;
        private readonly string _category;
        private readonly string _parentId;
        private readonly Stopwatch _stopwatch;
        private readonly string _eventId;

        public ProfileScope(ExecutionProfiler profiler, ProfileEventType startType, ProfileEventType endType, 
            string name, string category = "", string? parentId = null)
        {
            _profiler = profiler;
            _endType = endType;
            _name = name;
            _category = category;
            _parentId = parentId ?? string.Empty;
            _stopwatch = Stopwatch.StartNew();
            _eventId = Guid.NewGuid().ToString();

            _profiler.RecordEvent(new ProfileEvent
            {
                Id = _eventId,
                Type = startType,
                Name = name,
                Category = category,
                ParentId = parentId,
                MemoryUsage = GC.GetTotalMemory(false)
            });
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _profiler.RecordEvent(new ProfileEvent
            {
                Id = Guid.NewGuid().ToString(),
                Type = _endType,
                Name = _name,
                Category = _category,
                ParentId = _parentId,
                ElapsedTicks = _stopwatch.ElapsedTicks,
                MemoryUsage = GC.GetTotalMemory(false)
            });
        }
    }

    /// <summary>
    /// Execution profiler for performance analysis
    /// </summary>
    public class ExecutionProfiler
    {
        private static readonly Lazy<ExecutionProfiler> _instance = new(() => new ExecutionProfiler());
        public static ExecutionProfiler Instance => _instance.Value;

        private ProfileSession? _currentSession;
        private readonly ConcurrentQueue<ProfileEvent> _eventQueue = new();
        private readonly List<ProfileSession> _completedSessions = new();
        private readonly object _lock = new();
        private bool _isEnabled = true;
        private int _maxCompletedSessions = 100;

        public event EventHandler<ProfileEventArgs>? EventRecorded;
        public event EventHandler<ProfileSessionEventArgs>? SessionStarted;
        public event EventHandler<ProfileSessionEventArgs>? SessionEnded;

        private ExecutionProfiler() { }

        /// <summary>
        /// Enable or disable profiling
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// Maximum number of completed sessions to keep
        /// </summary>
        public int MaxCompletedSessions
        {
            get => _maxCompletedSessions;
            set => _maxCompletedSessions = value;
        }

        /// <summary>
        /// Current active session
        /// </summary>
        public ProfileSession? CurrentSession => _currentSession;

        /// <summary>
        /// Start a new profiling session
        /// </summary>
        public ProfileSession StartSession(string name = "")
        {
            lock (_lock)
            {
                if (_currentSession != null)
                {
                    EndSession();
                }

                _currentSession = new ProfileSession
                {
                    Name = string.IsNullOrEmpty(name) ? $"Session_{DateTime.UtcNow:yyyyMMdd_HHmmss}" : name,
                    StartTime = DateTime.UtcNow
                };

                SessionStarted?.Invoke(this, new ProfileSessionEventArgs { Session = _currentSession });

                return _currentSession;
            }
        }

        /// <summary>
        /// End the current session
        /// </summary>
        public ProfileSession? EndSession()
        {
            lock (_lock)
            {
                if (_currentSession == null)
                {
                    return null;
                }

                // Flush queued events
                while (_eventQueue.TryDequeue(out var evt))
                {
                    _currentSession.Events.Add(evt);
                }

                _currentSession.EndTime = DateTime.UtcNow;
                _completedSessions.Add(_currentSession);

                // Trim old sessions
                while (_completedSessions.Count > _maxCompletedSessions)
                {
                    _completedSessions.RemoveAt(0);
                }

                var session = _currentSession;
                _currentSession = null;

                SessionEnded?.Invoke(this, new ProfileSessionEventArgs { Session = session });

                return session;
            }
        }

        /// <summary>
        /// Record a profile event
        /// </summary>
        public void RecordEvent(ProfileEvent evt)
        {
            if (!_isEnabled) return;

            _eventQueue.Enqueue(evt);
            EventRecorded?.Invoke(this, new ProfileEventArgs { Event = evt });

            // Periodically flush to session
            if (_eventQueue.Count > 100)
            {
                FlushEvents();
            }
        }

        /// <summary>
        /// Flush queued events to the current session
        /// </summary>
        private void FlushEvents()
        {
            lock (_lock)
            {
                if (_currentSession == null) return;

                while (_eventQueue.TryDequeue(out var evt))
                {
                    _currentSession.Events.Add(evt);
                }
            }
        }

        /// <summary>
        /// Profile a step
        /// </summary>
        public ProfileScope ProfileStep(string stepName, string category = "Step", string? parentId = null)
        {
            return new ProfileScope(this, ProfileEventType.StepStart, ProfileEventType.StepEnd, 
                stepName, category, parentId);
        }

        /// <summary>
        /// Profile a sequence
        /// </summary>
        public ProfileScope ProfileSequence(string sequenceName, string? parentId = null)
        {
            return new ProfileScope(this, ProfileEventType.SequenceStart, ProfileEventType.SequenceEnd, 
                sequenceName, "Sequence", parentId);
        }

        /// <summary>
        /// Profile the engine
        /// </summary>
        public ProfileScope ProfileEngine(string name = "Engine")
        {
            return new ProfileScope(this, ProfileEventType.EngineStart, ProfileEventType.EngineEnd, 
                name, "Engine");
        }

        /// <summary>
        /// Profile a callback
        /// </summary>
        public ProfileScope ProfileCallback(string callbackName, string? parentId = null)
        {
            return new ProfileScope(this, ProfileEventType.CallbackStart, ProfileEventType.CallbackEnd, 
                callbackName, "Callback", parentId);
        }

        /// <summary>
        /// Record a custom event
        /// </summary>
        public void RecordCustomEvent(string name, string category, Dictionary<string, object>? metadata = null)
        {
            RecordEvent(new ProfileEvent
            {
                Type = ProfileEventType.Custom,
                Name = name,
                Category = category,
                Metadata = metadata ?? new Dictionary<string, object>(),
                MemoryUsage = GC.GetTotalMemory(false)
            });
        }

        /// <summary>
        /// Get all completed sessions
        /// </summary>
        public IEnumerable<ProfileSession> GetCompletedSessions()
        {
            lock (_lock)
            {
                return _completedSessions.ToList();
            }
        }

        /// <summary>
        /// Get a session by ID
        /// </summary>
        public ProfileSession? GetSession(string sessionId)
        {
            lock (_lock)
            {
                if (_currentSession?.Id == sessionId)
                {
                    return _currentSession;
                }

                return _completedSessions.FirstOrDefault(s => s.Id == sessionId);
            }
        }

        /// <summary>
        /// Clear all completed sessions
        /// </summary>
        public void ClearCompletedSessions()
        {
            lock (_lock)
            {
                _completedSessions.Clear();
            }
        }

        /// <summary>
        /// Export session to JSON
        /// </summary>
        public string ExportSessionToJson(string sessionId)
        {
            var session = GetSession(sessionId);
            if (session == null)
            {
                return "{}";
            }

            return JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Generate a text report for a session
        /// </summary>
        public string GenerateTextReport(string sessionId)
        {
            var session = GetSession(sessionId);
            if (session == null)
            {
                return "Session not found";
            }

            var stats = session.GetStatistics();
            var sb = new StringBuilder();

            sb.AppendLine("=== Execution Profile Report ===");
            sb.AppendLine();
            sb.AppendLine($"Session: {session.Name}");
            sb.AppendLine($"ID: {session.Id}");
            sb.AppendLine($"Start: {session.StartTime:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"End: {session.EndTime:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"Duration: {session.Duration.TotalMilliseconds:F2} ms");
            sb.AppendLine();

            sb.AppendLine("--- Statistics ---");
            sb.AppendLine($"Total Steps: {stats.TotalSteps}");
            sb.AppendLine($"Total Step Time: {stats.TotalStepTimeMs:F2} ms");
            sb.AppendLine($"Average Step Time: {stats.AverageStepTimeMs:F2} ms");
            sb.AppendLine($"Min Step Time: {stats.MinStepTimeMs:F2} ms");
            sb.AppendLine($"Max Step Time: {stats.MaxStepTimeMs:F2} ms");
            sb.AppendLine($"Peak Memory: {stats.PeakMemoryBytes / 1024.0 / 1024.0:F2} MB");
            sb.AppendLine();

            if (stats.SlowestSteps.Any())
            {
                sb.AppendLine("--- Slowest Steps ---");
                foreach (var step in stats.SlowestSteps)
                {
                    sb.AppendLine($"  {step.Name}: {step.ElapsedMs:F2} ms");
                }
                sb.AppendLine();
            }

            if (stats.TimeByCategory.Any())
            {
                sb.AppendLine("--- Time by Category ---");
                foreach (var kvp in stats.TimeByCategory.OrderByDescending(x => x.Value))
                {
                    var count = stats.StepCountByCategory.GetValueOrDefault(kvp.Key, 0);
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value:F2} ms ({count} steps)");
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Profile event arguments
    /// </summary>
    public class ProfileEventArgs : EventArgs
    {
        public ProfileEvent Event { get; set; } = new();
    }

    /// <summary>
    /// Profile session event arguments
    /// </summary>
    public class ProfileSessionEventArgs : EventArgs
    {
        public ProfileSession Session { get; set; } = new();
    }
}
