using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.ExecutionViewer
{
    /// <summary>
    /// Execution event type
    /// </summary>
    public enum ExecutionEventType
    {
        SequenceStarted,
        SequenceCompleted,
        StepStarted,
        StepCompleted,
        StepPassed,
        StepFailed,
        StepError,
        Paused,
        Resumed,
        Aborted,
        BreakpointHit,
        VariableChanged,
        Message
    }

    /// <summary>
    /// Execution event data
    /// </summary>
    public class ExecutionEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ExecutionEventType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string SequenceId { get; set; } = string.Empty;
        public string SequenceName { get; set; } = string.Empty;
        public string? StepId { get; set; }
        public string? StepName { get; set; }
        public int? StepIndex { get; set; }
        public string? Message { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public TimeSpan? Duration { get; set; }
    }

    /// <summary>
    /// Execution timeline entry
    /// </summary>
    public class TimelineEntry
    {
        public string StepName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public string Status { get; set; } = string.Empty;
        public string Color { get; set; } = "#808080";
        public int Depth { get; set; }
    }

    /// <summary>
    /// Execution viewer for real-time monitoring
    /// </summary>
    public class ExecutionViewerModel
    {
        private readonly List<ExecutionEvent> _events = new List<ExecutionEvent>();
        private readonly List<TimelineEntry> _timeline = new List<TimelineEntry>();
        private readonly Dictionary<string, object> _liveVariables = new Dictionary<string, object>();
        private readonly Stack<string> _callStack = new Stack<string>();
        private string _currentStatus = "Idle";
        private string? _currentStepName;
        private int _stepCount;
        private int _completedSteps;
        private int _passedSteps;
        private int _failedSteps;
        private DateTime? _startTime;

        public event EventHandler<ExecutionEvent>? EventOccurred;
        public event EventHandler<string>? StatusChanged;
        public event EventHandler<string>? CurrentStepChanged;

        public IReadOnlyList<ExecutionEvent> Events => _events.AsReadOnly();
        public IReadOnlyList<TimelineEntry> Timeline => _timeline.AsReadOnly();
        public IReadOnlyDictionary<string, object> LiveVariables => _liveVariables;
        public IEnumerable<string> CallStack => _callStack;
        public string CurrentStatus => _currentStatus;
        public string? CurrentStepName => _currentStepName;
        public int StepCount => _stepCount;
        public int CompletedSteps => _completedSteps;
        public int PassedSteps => _passedSteps;
        public int FailedSteps => _failedSteps;
        public DateTime? StartTime => _startTime;
        public TimeSpan? ElapsedTime => _startTime.HasValue ? DateTime.Now - _startTime.Value : null;
        public double Progress => _stepCount > 0 ? (double)_completedSteps / _stepCount * 100 : 0;

        /// <summary>
        /// Record an execution event
        /// </summary>
        public void RecordEvent(ExecutionEvent evt)
        {
            _events.Add(evt);

            switch (evt.Type)
            {
                case ExecutionEventType.SequenceStarted:
                    _startTime = evt.Timestamp;
                    SetStatus("Running");
                    _callStack.Push(evt.SequenceName);
                    break;

                case ExecutionEventType.SequenceCompleted:
                    if (_callStack.Count > 0) _callStack.Pop();
                    if (_callStack.Count == 0) SetStatus("Completed");
                    break;

                case ExecutionEventType.StepStarted:
                    _currentStepName = evt.StepName;
                    CurrentStepChanged?.Invoke(this, evt.StepName ?? "");
                    break;

                case ExecutionEventType.StepCompleted:
                    _completedSteps++;
                    AddTimelineEntry(evt);
                    break;

                case ExecutionEventType.StepPassed:
                    _passedSteps++;
                    break;

                case ExecutionEventType.StepFailed:
                    _failedSteps++;
                    break;

                case ExecutionEventType.Paused:
                    SetStatus("Paused");
                    break;

                case ExecutionEventType.Resumed:
                    SetStatus("Running");
                    break;

                case ExecutionEventType.Aborted:
                    SetStatus("Aborted");
                    break;

                case ExecutionEventType.VariableChanged:
                    if (evt.Data.TryGetValue("Name", out var name) && 
                        evt.Data.TryGetValue("Value", out var value))
                    {
                        _liveVariables[name.ToString()!] = value;
                    }
                    break;
            }

            EventOccurred?.Invoke(this, evt);
        }

        /// <summary>
        /// Set total step count
        /// </summary>
        public void SetStepCount(int count)
        {
            _stepCount = count;
        }

        /// <summary>
        /// Update a live variable
        /// </summary>
        public void UpdateVariable(string name, object value)
        {
            _liveVariables[name] = value;
            RecordEvent(new ExecutionEvent
            {
                Type = ExecutionEventType.VariableChanged,
                Data = new Dictionary<string, object>
                {
                    ["Name"] = name,
                    ["Value"] = value
                }
            });
        }

        /// <summary>
        /// Reset the viewer state
        /// </summary>
        public void Reset()
        {
            _events.Clear();
            _timeline.Clear();
            _liveVariables.Clear();
            _callStack.Clear();
            _currentStatus = "Idle";
            _currentStepName = null;
            _stepCount = 0;
            _completedSteps = 0;
            _passedSteps = 0;
            _failedSteps = 0;
            _startTime = null;
            StatusChanged?.Invoke(this, "Idle");
        }

        /// <summary>
        /// Get events filtered by type
        /// </summary>
        public List<ExecutionEvent> GetEventsByType(ExecutionEventType type)
        {
            return _events.Where(e => e.Type == type).ToList();
        }

        /// <summary>
        /// Get events in a time range
        /// </summary>
        public List<ExecutionEvent> GetEventsInRange(DateTime start, DateTime end)
        {
            return _events.Where(e => e.Timestamp >= start && e.Timestamp <= end).ToList();
        }

        private void SetStatus(string status)
        {
            _currentStatus = status;
            StatusChanged?.Invoke(this, status);
        }

        private void AddTimelineEntry(ExecutionEvent evt)
        {
            // Find matching start event
            var startEvent = _events.LastOrDefault(e => 
                e.Type == ExecutionEventType.StepStarted && 
                e.StepId == evt.StepId);

            if (startEvent != null)
            {
                var color = evt.Data.TryGetValue("Status", out var status)
                    ? GetStatusColor(status.ToString() ?? "Unknown")
                    : "#808080";

                _timeline.Add(new TimelineEntry
                {
                    StepName = evt.StepName ?? "Unknown",
                    StartTime = startEvent.Timestamp,
                    EndTime = evt.Timestamp,
                    Status = status?.ToString() ?? "Unknown",
                    Color = color,
                    Depth = _callStack.Count
                });
            }
        }

        private static string GetStatusColor(string status)
        {
            return status.ToLower() switch
            {
                "passed" => "#28A745",
                "failed" => "#DC3545",
                "error" => "#FFC107",
                "running" => "#007BFF",
                "skipped" => "#6C757D",
                _ => "#808080"
            };
        }
    }

    /// <summary>
    /// Execution statistics calculator
    /// </summary>
    public class ExecutionStatisticsCalculator
    {
        public ExecutionStats Calculate(IEnumerable<ExecutionEvent> events)
        {
            var eventList = events.ToList();
            var stats = new ExecutionStats();

            // Find sequence events
            var startEvent = eventList.FirstOrDefault(e => e.Type == ExecutionEventType.SequenceStarted);
            var endEvent = eventList.LastOrDefault(e => 
                e.Type == ExecutionEventType.SequenceCompleted || 
                e.Type == ExecutionEventType.Aborted);

            if (startEvent != null && endEvent != null)
            {
                stats.TotalDuration = endEvent.Timestamp - startEvent.Timestamp;
            }

            // Count steps
            var stepStarts = eventList.Where(e => e.Type == ExecutionEventType.StepStarted).ToList();
            var stepCompletes = eventList.Where(e => e.Type == ExecutionEventType.StepCompleted).ToList();

            stats.TotalSteps = stepStarts.Count;
            stats.CompletedSteps = stepCompletes.Count;
            stats.PassedSteps = eventList.Count(e => e.Type == ExecutionEventType.StepPassed);
            stats.FailedSteps = eventList.Count(e => e.Type == ExecutionEventType.StepFailed);
            stats.ErrorSteps = eventList.Count(e => e.Type == ExecutionEventType.StepError);

            // Calculate step durations
            foreach (var startEvt in stepStarts)
            {
                var completeEvt = stepCompletes.FirstOrDefault(e => e.StepId == startEvt.StepId);
                if (completeEvt != null)
                {
                    var duration = completeEvt.Timestamp - startEvt.Timestamp;
                    stats.StepDurations[startEvt.StepName ?? "Unknown"] = duration;
                }
            }

            // Find slowest steps
            stats.SlowestSteps = stats.StepDurations
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            return stats;
        }
    }

    /// <summary>
    /// Execution statistics
    /// </summary>
    public class ExecutionStats
    {
        public TimeSpan TotalDuration { get; set; }
        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public int PassedSteps { get; set; }
        public int FailedSteps { get; set; }
        public int ErrorSteps { get; set; }
        public double PassRate => TotalSteps > 0 ? (double)PassedSteps / TotalSteps * 100 : 0;
        public Dictionary<string, TimeSpan> StepDurations { get; set; } = new Dictionary<string, TimeSpan>();
        public Dictionary<string, TimeSpan> SlowestSteps { get; set; } = new Dictionary<string, TimeSpan>();
    }

    /// <summary>
    /// Execution viewer manager singleton
    /// </summary>
    public class ExecutionViewerManager
    {
        private static readonly Lazy<ExecutionViewerManager> _instance = 
            new Lazy<ExecutionViewerManager>(() => new ExecutionViewerManager());
        
        public static ExecutionViewerManager Instance => _instance.Value;

        private readonly Dictionary<string, ExecutionViewerModel> _viewers = 
            new Dictionary<string, ExecutionViewerModel>();
        
        private ExecutionViewerModel _defaultViewer = new ExecutionViewerModel();

        private ExecutionViewerManager() { }

        public ExecutionViewerModel DefaultViewer => _defaultViewer;

        /// <summary>
        /// Get or create a viewer for a session
        /// </summary>
        public ExecutionViewerModel GetViewer(string sessionId)
        {
            if (!_viewers.ContainsKey(sessionId))
            {
                _viewers[sessionId] = new ExecutionViewerModel();
            }
            return _viewers[sessionId];
        }

        /// <summary>
        /// Remove a viewer
        /// </summary>
        public void RemoveViewer(string sessionId)
        {
            _viewers.Remove(sessionId);
        }

        /// <summary>
        /// Calculate statistics for a session
        /// </summary>
        public ExecutionStats GetStatistics(string sessionId)
        {
            var viewer = GetViewer(sessionId);
            var calculator = new ExecutionStatisticsCalculator();
            return calculator.Calculate(viewer.Events);
        }
    }
}
