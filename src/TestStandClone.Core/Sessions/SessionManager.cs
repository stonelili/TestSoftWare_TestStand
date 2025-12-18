// SessionManager.cs - Session management for test execution
// Supports multiple concurrent test sessions

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace TestStandClone.Core.Sessions
{
    /// <summary>
    /// Represents the state of a test session
    /// </summary>
    public enum SessionState
    {
        /// <summary>Session is idle</summary>
        Idle,
        /// <summary>Session is running</summary>
        Running,
        /// <summary>Session is paused</summary>
        Paused,
        /// <summary>Session is aborting</summary>
        Aborting,
        /// <summary>Session has completed</summary>
        Completed,
        /// <summary>Session encountered an error</summary>
        Error
    }

    /// <summary>
    /// Session event type
    /// </summary>
    public enum SessionEventType
    {
        /// <summary>Session started</summary>
        Started,
        /// <summary>Session paused</summary>
        Paused,
        /// <summary>Session resumed</summary>
        Resumed,
        /// <summary>Session aborted</summary>
        Aborted,
        /// <summary>Session completed</summary>
        Completed,
        /// <summary>Session error</summary>
        Error,
        /// <summary>Step started</summary>
        StepStarted,
        /// <summary>Step completed</summary>
        StepCompleted
    }

    /// <summary>
    /// Session event arguments
    /// </summary>
    public class SessionEventArgs : EventArgs
    {
        public string SessionId { get; }
        public SessionEventType EventType { get; }
        public string? StepId { get; }
        public string? Message { get; }
        public DateTime Timestamp { get; }

        public SessionEventArgs(string sessionId, SessionEventType eventType, string? stepId = null, string? message = null)
        {
            SessionId = sessionId;
            EventType = eventType;
            StepId = stepId;
            Message = message;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Represents a test session
    /// </summary>
    public class TestSession : INotifyPropertyChanged, IDisposable
    {
        private SessionState _state = SessionState.Idle;
        private Sequence? _sequence;
        private Context? _context;
        private Engine? _engine;
        private CancellationTokenSource? _cts;
        private int _currentStepIndex = -1;
        private DateTime _startTime;
        private DateTime _endTime;
        private readonly object _stateLock = new();

        /// <summary>Unique identifier for the session</summary>
        public string Id { get; }

        /// <summary>Display name of the session</summary>
        public string Name { get; set; }

        /// <summary>Current state of the session</summary>
        public SessionState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Associated sequence</summary>
        public Sequence? Sequence
        {
            get => _sequence;
            set
            {
                _sequence = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Execution context</summary>
        public Context? Context => _context;

        /// <summary>Current step index</summary>
        public int CurrentStepIndex
        {
            get => _currentStepIndex;
            private set
            {
                if (_currentStepIndex != value)
                {
                    _currentStepIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Session start time</summary>
        public DateTime StartTime => _startTime;

        /// <summary>Session end time</summary>
        public DateTime EndTime => _endTime;

        /// <summary>Total execution duration</summary>
        public TimeSpan Duration => _endTime > _startTime ? _endTime - _startTime : TimeSpan.Zero;

        /// <summary>User data for the session</summary>
        public Dictionary<string, object?> UserData { get; } = new();

        /// <summary>Event raised when session state changes</summary>
        public event EventHandler<SessionEventArgs>? SessionEvent;

        public event PropertyChangedEventHandler? PropertyChanged;

        public TestSession(string name)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
        }

        /// <summary>
        /// Start the session execution
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_sequence == null)
                throw new InvalidOperationException("No sequence assigned to session");

            lock (_stateLock)
            {
                if (State != SessionState.Idle)
                    throw new InvalidOperationException($"Cannot start session in state: {State}");
                
                State = SessionState.Running;
            }

            _startTime = DateTime.Now;
            _context = new Context();
            _engine = new Engine();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            RaiseSessionEvent(SessionEventType.Started);

            try
            {
                await _engine.ExecuteSequenceAsync(_sequence);
                State = SessionState.Completed;
                RaiseSessionEvent(SessionEventType.Completed);
            }
            catch (OperationCanceledException)
            {
                State = SessionState.Completed;
                RaiseSessionEvent(SessionEventType.Aborted);
            }
            catch (Exception ex)
            {
                State = SessionState.Error;
                RaiseSessionEvent(SessionEventType.Error, null, ex.Message);
            }
            finally
            {
                _endTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Pause the session
        /// </summary>
        public void Pause()
        {
            lock (_stateLock)
            {
                if (State != SessionState.Running)
                    return;

                _engine?.Pause();
                State = SessionState.Paused;
                RaiseSessionEvent(SessionEventType.Paused);
            }
        }

        /// <summary>
        /// Resume the session
        /// </summary>
        public void Resume()
        {
            lock (_stateLock)
            {
                if (State != SessionState.Paused)
                    return;

                _engine?.Resume();
                State = SessionState.Running;
                RaiseSessionEvent(SessionEventType.Resumed);
            }
        }

        /// <summary>
        /// Abort the session
        /// </summary>
        public void Abort()
        {
            lock (_stateLock)
            {
                if (State != SessionState.Running && State != SessionState.Paused)
                    return;

                State = SessionState.Aborting;
                _cts?.Cancel();
            }
        }

        /// <summary>
        /// Reset the session for re-execution
        /// </summary>
        public void Reset()
        {
            lock (_stateLock)
            {
                if (State == SessionState.Running)
                    throw new InvalidOperationException("Cannot reset a running session");

                State = SessionState.Idle;
                CurrentStepIndex = -1;
                _context = null;
                
                // Reset all step statuses
                if (_sequence != null)
                {
                    foreach (var step in _sequence.Steps)
                    {
                        step.Status = StepStatus.Idle;
                        step.ResultText = string.Empty;
                    }
                }
            }
        }

        private void RaiseSessionEvent(SessionEventType eventType, string? stepId = null, string? message = null)
        {
            SessionEvent?.Invoke(this, new SessionEventArgs(Id, eventType, stepId, message));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _cts?.Dispose();
        }
    }

    /// <summary>
    /// Session configuration
    /// </summary>
    public class SessionConfiguration
    {
        /// <summary>Maximum number of concurrent sessions</summary>
        public int MaxConcurrentSessions { get; set; } = 4;

        /// <summary>Session timeout in seconds</summary>
        public int SessionTimeoutSeconds { get; set; } = 3600;

        /// <summary>Auto-cleanup completed sessions</summary>
        public bool AutoCleanupCompletedSessions { get; set; } = true;

        /// <summary>Retention period for completed sessions in minutes</summary>
        public int CompletedSessionRetentionMinutes { get; set; } = 60;
    }

    /// <summary>
    /// Manages multiple test sessions
    /// </summary>
    public class SessionManager
    {
        private static SessionManager? _instance;
        private static readonly object _lock = new();

        private readonly ConcurrentDictionary<string, TestSession> _sessions = new();
        private SessionConfiguration _configuration = new();
        private Timer? _cleanupTimer;

        public static SessionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new SessionManager();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Event raised when any session event occurs
        /// </summary>
        public event EventHandler<SessionEventArgs>? SessionEvent;

        private SessionManager()
        {
            // Start cleanup timer
            _cleanupTimer = new Timer(CleanupCompletedSessions, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Get or set the session configuration
        /// </summary>
        public SessionConfiguration Configuration
        {
            get => _configuration;
            set => _configuration = value ?? new SessionConfiguration();
        }

        /// <summary>
        /// Create a new session
        /// </summary>
        public TestSession CreateSession(string name)
        {
            var runningCount = _sessions.Values.Count(s => s.State == SessionState.Running);
            if (runningCount >= _configuration.MaxConcurrentSessions)
            {
                throw new InvalidOperationException($"Maximum number of concurrent sessions ({_configuration.MaxConcurrentSessions}) reached");
            }

            var session = new TestSession(name);
            session.SessionEvent += (s, e) => SessionEvent?.Invoke(s, e);
            _sessions[session.Id] = session;
            
            return session;
        }

        /// <summary>
        /// Get a session by ID
        /// </summary>
        public TestSession? GetSession(string id)
        {
            _sessions.TryGetValue(id, out var session);
            return session;
        }

        /// <summary>
        /// Get all sessions
        /// </summary>
        public IEnumerable<TestSession> GetAllSessions()
        {
            return _sessions.Values;
        }

        /// <summary>
        /// Get sessions by state
        /// </summary>
        public IEnumerable<TestSession> GetSessionsByState(SessionState state)
        {
            return _sessions.Values.Where(s => s.State == state);
        }

        /// <summary>
        /// Get running session count
        /// </summary>
        public int RunningSessionCount => _sessions.Values.Count(s => s.State == SessionState.Running);

        /// <summary>
        /// Remove a session
        /// </summary>
        public bool RemoveSession(string id)
        {
            if (_sessions.TryRemove(id, out var session))
            {
                session.Dispose();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Abort all running sessions
        /// </summary>
        public void AbortAllSessions()
        {
            foreach (var session in _sessions.Values.Where(s => s.State == SessionState.Running))
            {
                session.Abort();
            }
        }

        /// <summary>
        /// Clear all completed sessions
        /// </summary>
        public void ClearCompletedSessions()
        {
            var completedIds = _sessions.Values
                .Where(s => s.State == SessionState.Completed || s.State == SessionState.Error)
                .Select(s => s.Id)
                .ToList();

            foreach (var id in completedIds)
            {
                RemoveSession(id);
            }
        }

        private void CleanupCompletedSessions(object? state)
        {
            if (!_configuration.AutoCleanupCompletedSessions)
                return;

            var cutoffTime = DateTime.Now.AddMinutes(-_configuration.CompletedSessionRetentionMinutes);
            var oldSessionIds = _sessions.Values
                .Where(s => (s.State == SessionState.Completed || s.State == SessionState.Error) && s.EndTime < cutoffTime)
                .Select(s => s.Id)
                .ToList();

            foreach (var id in oldSessionIds)
            {
                RemoveSession(id);
            }
        }

        /// <summary>
        /// Get session statistics
        /// </summary>
        public SessionStatistics GetStatistics()
        {
            var stats = new SessionStatistics();
            
            foreach (var session in _sessions.Values)
            {
                stats.TotalSessions++;
                
                switch (session.State)
                {
                    case SessionState.Idle:
                        stats.IdleSessions++;
                        break;
                    case SessionState.Running:
                        stats.RunningSessions++;
                        break;
                    case SessionState.Paused:
                        stats.PausedSessions++;
                        break;
                    case SessionState.Completed:
                        stats.CompletedSessions++;
                        break;
                    case SessionState.Error:
                        stats.ErrorSessions++;
                        break;
                }
            }

            return stats;
        }
    }

    /// <summary>
    /// Session statistics
    /// </summary>
    public class SessionStatistics
    {
        public int TotalSessions { get; set; }
        public int IdleSessions { get; set; }
        public int RunningSessions { get; set; }
        public int PausedSessions { get; set; }
        public int CompletedSessions { get; set; }
        public int ErrorSessions { get; set; }
    }
}
