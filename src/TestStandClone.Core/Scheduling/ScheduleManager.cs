using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TestStandClone.Core.Scheduling
{
    /// <summary>
    /// Schedule status
    /// </summary>
    public enum ScheduleStatus
    {
        /// <summary>Schedule is pending</summary>
        Pending,
        /// <summary>Schedule is active</summary>
        Active,
        /// <summary>Schedule is running</summary>
        Running,
        /// <summary>Schedule is paused</summary>
        Paused,
        /// <summary>Schedule is completed</summary>
        Completed,
        /// <summary>Schedule is cancelled</summary>
        Cancelled,
        /// <summary>Schedule has failed</summary>
        Failed
    }

    /// <summary>
    /// Recurrence type for scheduled tasks
    /// </summary>
    public enum RecurrenceType
    {
        /// <summary>One-time execution</summary>
        Once,
        /// <summary>Every minute</summary>
        Minutely,
        /// <summary>Every hour</summary>
        Hourly,
        /// <summary>Every day</summary>
        Daily,
        /// <summary>Every week</summary>
        Weekly,
        /// <summary>Every month</summary>
        Monthly,
        /// <summary>Custom interval</summary>
        Custom
    }

    /// <summary>
    /// Scheduled task definition
    /// </summary>
    public class ScheduledTask
    {
        /// <summary>Unique identifier</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>Task name</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Task description</summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>Sequence file to execute</summary>
        public string SequenceFile { get; set; } = string.Empty;
        /// <summary>Entry point to use</summary>
        public string EntryPoint { get; set; } = "SinglePass";
        /// <summary>Schedule start time</summary>
        public DateTime StartTime { get; set; } = DateTime.Now;
        /// <summary>Schedule end time (null = no end)</summary>
        public DateTime? EndTime { get; set; }
        /// <summary>Recurrence type</summary>
        public RecurrenceType Recurrence { get; set; } = RecurrenceType.Once;
        /// <summary>Custom interval in minutes</summary>
        public int CustomIntervalMinutes { get; set; } = 60;
        /// <summary>Days of week for weekly recurrence</summary>
        public List<DayOfWeek> DaysOfWeek { get; set; } = new();
        /// <summary>Day of month for monthly recurrence</summary>
        public int DayOfMonth { get; set; } = 1;
        /// <summary>Status of the task</summary>
        public ScheduleStatus Status { get; set; } = ScheduleStatus.Pending;
        /// <summary>Priority (1=highest)</summary>
        public int Priority { get; set; } = 5;
        /// <summary>Last execution time</summary>
        public DateTime? LastExecutionTime { get; set; }
        /// <summary>Next scheduled execution time</summary>
        public DateTime? NextExecutionTime { get; set; }
        /// <summary>Number of times executed</summary>
        public int ExecutionCount { get; set; }
        /// <summary>Number of successful executions</summary>
        public int SuccessCount { get; set; }
        /// <summary>Number of failed executions</summary>
        public int FailureCount { get; set; }
        /// <summary>Maximum retries on failure</summary>
        public int MaxRetries { get; set; } = 0;
        /// <summary>Whether to skip if missed</summary>
        public bool SkipIfMissed { get; set; } = true;
        /// <summary>Parameters to pass to sequence</summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
        /// <summary>Tags for categorization</summary>
        public List<string> Tags { get; set; } = new();
        /// <summary>User who created the task</summary>
        public string CreatedBy { get; set; } = string.Empty;
        /// <summary>Creation timestamp</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Calculates the next execution time based on recurrence</summary>
        public void CalculateNextExecutionTime()
        {
            DateTime baseTime = LastExecutionTime ?? StartTime;
            
            switch (Recurrence)
            {
                case RecurrenceType.Once:
                    if (LastExecutionTime == null && DateTime.Now < StartTime)
                        NextExecutionTime = StartTime;
                    else
                        NextExecutionTime = null;
                    break;
                    
                case RecurrenceType.Minutely:
                    NextExecutionTime = baseTime.AddMinutes(1);
                    break;
                    
                case RecurrenceType.Hourly:
                    NextExecutionTime = baseTime.AddHours(1);
                    break;
                    
                case RecurrenceType.Daily:
                    NextExecutionTime = baseTime.AddDays(1);
                    break;
                    
                case RecurrenceType.Weekly:
                    DateTime next = baseTime.AddDays(1);
                    while (!DaysOfWeek.Contains(next.DayOfWeek))
                    {
                        next = next.AddDays(1);
                        if (next > baseTime.AddDays(8)) break;
                    }
                    NextExecutionTime = next;
                    break;
                    
                case RecurrenceType.Monthly:
                    DateTime nextMonth = baseTime.AddMonths(1);
                    int day = Math.Min(DayOfMonth, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
                    NextExecutionTime = new DateTime(nextMonth.Year, nextMonth.Month, day,
                        baseTime.Hour, baseTime.Minute, baseTime.Second);
                    break;
                    
                case RecurrenceType.Custom:
                    NextExecutionTime = baseTime.AddMinutes(CustomIntervalMinutes);
                    break;
            }

            // Check end time
            if (EndTime.HasValue && NextExecutionTime > EndTime)
            {
                NextExecutionTime = null;
                Status = ScheduleStatus.Completed;
            }
        }

        /// <summary>Checks if the task is due to run</summary>
        public bool IsDue => Status == ScheduleStatus.Active && 
                            NextExecutionTime.HasValue && 
                            DateTime.Now >= NextExecutionTime;
    }

    /// <summary>
    /// Execution log entry for a scheduled task
    /// </summary>
    public class ScheduleExecutionLog
    {
        /// <summary>Log entry ID</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>Task ID</summary>
        public string TaskId { get; set; } = string.Empty;
        /// <summary>Task name</summary>
        public string TaskName { get; set; } = string.Empty;
        /// <summary>Scheduled time</summary>
        public DateTime ScheduledTime { get; set; }
        /// <summary>Actual start time</summary>
        public DateTime StartTime { get; set; }
        /// <summary>End time</summary>
        public DateTime EndTime { get; set; }
        /// <summary>Duration in seconds</summary>
        public double DurationSeconds { get; set; }
        /// <summary>Whether execution was successful</summary>
        public bool Success { get; set; }
        /// <summary>Error message if failed</summary>
        public string ErrorMessage { get; set; } = string.Empty;
        /// <summary>Result summary</summary>
        public string ResultSummary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Interface for schedule persistence
    /// </summary>
    public interface IScheduleStore
    {
        /// <summary>Saves a task</summary>
        void SaveTask(ScheduledTask task);
        /// <summary>Loads a task by ID</summary>
        ScheduledTask? LoadTask(string id);
        /// <summary>Loads all tasks</summary>
        List<ScheduledTask> LoadAllTasks();
        /// <summary>Deletes a task</summary>
        bool DeleteTask(string id);
        /// <summary>Saves an execution log</summary>
        void SaveLog(ScheduleExecutionLog log);
        /// <summary>Gets execution logs for a task</summary>
        List<ScheduleExecutionLog> GetLogs(string taskId, int maxCount = 100);
    }

    /// <summary>
    /// JSON file-based schedule store
    /// </summary>
    public class JsonScheduleStore : IScheduleStore
    {
        private readonly string _taskDirectory;
        private readonly string _logDirectory;

        /// <summary>Creates a new JSON schedule store</summary>
        public JsonScheduleStore(string baseDirectory)
        {
            _taskDirectory = Path.Combine(baseDirectory, "tasks");
            _logDirectory = Path.Combine(baseDirectory, "logs");
            
            if (!Directory.Exists(_taskDirectory)) Directory.CreateDirectory(_taskDirectory);
            if (!Directory.Exists(_logDirectory)) Directory.CreateDirectory(_logDirectory);
        }

        /// <inheritdoc/>
        public void SaveTask(ScheduledTask task)
        {
            string path = Path.Combine(_taskDirectory, $"{task.Id}.json");
            string json = JsonSerializer.Serialize(task, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        /// <inheritdoc/>
        public ScheduledTask? LoadTask(string id)
        {
            string path = Path.Combine(_taskDirectory, $"{id}.json");
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ScheduledTask>(json);
        }

        /// <inheritdoc/>
        public List<ScheduledTask> LoadAllTasks()
        {
            var tasks = new List<ScheduledTask>();
            if (!Directory.Exists(_taskDirectory)) return tasks;

            foreach (string file in Directory.GetFiles(_taskDirectory, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var task = JsonSerializer.Deserialize<ScheduledTask>(json);
                    if (task != null) tasks.Add(task);
                }
                catch { /* Skip invalid files */ }
            }
            return tasks;
        }

        /// <inheritdoc/>
        public bool DeleteTask(string id)
        {
            string path = Path.Combine(_taskDirectory, $"{id}.json");
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public void SaveLog(ScheduleExecutionLog log)
        {
            string path = Path.Combine(_logDirectory, $"{log.TaskId}_{log.StartTime:yyyyMMddHHmmss}.json");
            string json = JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        /// <inheritdoc/>
        public List<ScheduleExecutionLog> GetLogs(string taskId, int maxCount = 100)
        {
            var logs = new List<ScheduleExecutionLog>();
            if (!Directory.Exists(_logDirectory)) return logs;

            var files = Directory.GetFiles(_logDirectory, $"{taskId}_*.json")
                .OrderByDescending(f => f)
                .Take(maxCount);

            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var log = JsonSerializer.Deserialize<ScheduleExecutionLog>(json);
                    if (log != null) logs.Add(log);
                }
                catch { /* Skip invalid files */ }
            }
            return logs;
        }
    }

    /// <summary>
    /// Schedule manager singleton for managing scheduled tasks
    /// </summary>
    public sealed class ScheduleManager
    {
        private static readonly Lazy<ScheduleManager> _instance = 
            new Lazy<ScheduleManager>(() => new ScheduleManager());
        
        /// <summary>Gets the singleton instance</summary>
        public static ScheduleManager Instance => _instance.Value;

        private IScheduleStore _store;
        private readonly List<ScheduledTask> _tasks = new();
        private readonly object _lockObject = new object();
        private CancellationTokenSource? _schedulerCts;
        private Task? _schedulerTask;
        private bool _isRunning;

        /// <summary>Event raised when a task starts</summary>
        public event EventHandler<ScheduledTask>? TaskStarted;

        /// <summary>Event raised when a task completes</summary>
        public event EventHandler<ScheduleExecutionLog>? TaskCompleted;

        /// <summary>Event raised when a task fails</summary>
        public event EventHandler<ScheduleExecutionLog>? TaskFailed;

        /// <summary>Action to execute a sequence (set by application)</summary>
        public Func<string, string, Dictionary<string, object>, Task<bool>>? ExecuteSequenceAction { get; set; }

        private ScheduleManager()
        {
            _store = new JsonScheduleStore("schedule");
            LoadAllTasks();
        }

        /// <summary>Sets the schedule store</summary>
        public void SetStore(IScheduleStore store)
        {
            _store = store;
            LoadAllTasks();
        }

        /// <summary>Gets all scheduled tasks</summary>
        public IReadOnlyList<ScheduledTask> Tasks => _tasks.AsReadOnly();

        /// <summary>Whether the scheduler is running</summary>
        public bool IsRunning => _isRunning;

        /// <summary>Starts the scheduler</summary>
        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _schedulerCts = new CancellationTokenSource();
            _schedulerTask = RunSchedulerAsync(_schedulerCts.Token);
        }

        /// <summary>Stops the scheduler</summary>
        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _schedulerCts?.Cancel();
            _schedulerTask?.Wait(5000);
        }

        /// <summary>Adds a scheduled task</summary>
        public void AddTask(ScheduledTask task)
        {
            lock (_lockObject)
            {
                task.CalculateNextExecutionTime();
                _store.SaveTask(task);
                _tasks.Add(task);
            }
        }

        /// <summary>Updates a scheduled task</summary>
        public void UpdateTask(ScheduledTask task)
        {
            lock (_lockObject)
            {
                var existing = _tasks.FirstOrDefault(t => t.Id == task.Id);
                if (existing != null)
                {
                    _tasks.Remove(existing);
                }
                task.CalculateNextExecutionTime();
                _store.SaveTask(task);
                _tasks.Add(task);
            }
        }

        /// <summary>Removes a scheduled task</summary>
        public bool RemoveTask(string taskId)
        {
            lock (_lockObject)
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task != null)
                {
                    _tasks.Remove(task);
                    return _store.DeleteTask(taskId);
                }
                return false;
            }
        }

        /// <summary>Gets a task by ID</summary>
        public ScheduledTask? GetTask(string taskId)
        {
            lock (_lockObject)
            {
                return _tasks.FirstOrDefault(t => t.Id == taskId);
            }
        }

        /// <summary>Activates a task</summary>
        public void ActivateTask(string taskId)
        {
            lock (_lockObject)
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task != null)
                {
                    task.Status = ScheduleStatus.Active;
                    task.CalculateNextExecutionTime();
                    _store.SaveTask(task);
                }
            }
        }

        /// <summary>Deactivates a task</summary>
        public void DeactivateTask(string taskId)
        {
            lock (_lockObject)
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task != null)
                {
                    task.Status = ScheduleStatus.Paused;
                    _store.SaveTask(task);
                }
            }
        }

        /// <summary>Gets execution logs for a task</summary>
        public List<ScheduleExecutionLog> GetTaskLogs(string taskId, int maxCount = 100)
        {
            return _store.GetLogs(taskId, maxCount);
        }

        /// <summary>Gets tasks due to run</summary>
        public List<ScheduledTask> GetDueTasks()
        {
            lock (_lockObject)
            {
                return _tasks.Where(t => t.IsDue).OrderBy(t => t.Priority).ToList();
            }
        }

        /// <summary>Loads all tasks from store</summary>
        private void LoadAllTasks()
        {
            lock (_lockObject)
            {
                _tasks.Clear();
                _tasks.AddRange(_store.LoadAllTasks());
            }
        }

        /// <summary>Runs the scheduler loop</summary>
        private async Task RunSchedulerAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var dueTasks = GetDueTasks();
                    foreach (var task in dueTasks)
                    {
                        if (ct.IsCancellationRequested) break;
                        await ExecuteTaskAsync(task);
                    }

                    await Task.Delay(1000, ct); // Check every second
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Continue running on errors
                }
            }
        }

        /// <summary>Executes a scheduled task</summary>
        private async Task ExecuteTaskAsync(ScheduledTask task)
        {
            var log = new ScheduleExecutionLog
            {
                TaskId = task.Id,
                TaskName = task.Name,
                ScheduledTime = task.NextExecutionTime ?? DateTime.Now,
                StartTime = DateTime.Now
            };

            try
            {
                task.Status = ScheduleStatus.Running;
                task.ExecutionCount++;
                TaskStarted?.Invoke(this, task);

                bool success = true;
                if (ExecuteSequenceAction != null)
                {
                    success = await ExecuteSequenceAction(task.SequenceFile, task.EntryPoint, task.Parameters);
                }

                log.EndTime = DateTime.Now;
                log.DurationSeconds = (log.EndTime - log.StartTime).TotalSeconds;
                log.Success = success;

                if (success)
                {
                    task.SuccessCount++;
                    log.ResultSummary = "Execution completed successfully";
                }
                else
                {
                    task.FailureCount++;
                    log.ResultSummary = "Execution failed";
                    TaskFailed?.Invoke(this, log);
                }

                task.LastExecutionTime = log.StartTime;
                task.CalculateNextExecutionTime();
                
                if (task.NextExecutionTime == null)
                {
                    task.Status = ScheduleStatus.Completed;
                }
                else
                {
                    task.Status = ScheduleStatus.Active;
                }

                TaskCompleted?.Invoke(this, log);
            }
            catch (Exception ex)
            {
                log.EndTime = DateTime.Now;
                log.DurationSeconds = (log.EndTime - log.StartTime).TotalSeconds;
                log.Success = false;
                log.ErrorMessage = ex.Message;
                task.FailureCount++;
                task.Status = ScheduleStatus.Failed;
                TaskFailed?.Invoke(this, log);
            }

            _store.SaveTask(task);
            _store.SaveLog(log);
        }

        /// <summary>Runs a task immediately</summary>
        public async Task RunNowAsync(string taskId)
        {
            var task = GetTask(taskId);
            if (task != null)
            {
                await ExecuteTaskAsync(task);
            }
        }

        /// <summary>Gets schedule statistics</summary>
        public Dictionary<string, int> GetStatistics()
        {
            lock (_lockObject)
            {
                return new Dictionary<string, int>
                {
                    ["Total"] = _tasks.Count,
                    ["Active"] = _tasks.Count(t => t.Status == ScheduleStatus.Active),
                    ["Pending"] = _tasks.Count(t => t.Status == ScheduleStatus.Pending),
                    ["Running"] = _tasks.Count(t => t.Status == ScheduleStatus.Running),
                    ["Paused"] = _tasks.Count(t => t.Status == ScheduleStatus.Paused),
                    ["Completed"] = _tasks.Count(t => t.Status == ScheduleStatus.Completed),
                    ["Failed"] = _tasks.Count(t => t.Status == ScheduleStatus.Failed)
                };
            }
        }
    }
}
