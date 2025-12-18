using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.MaintenanceTracking
{
    public enum MaintenanceType { Preventive, Corrective, Calibration, Cleaning, Inspection, Upgrade }
    public enum MaintenanceStatus { Scheduled, InProgress, Completed, Overdue, Cancelled }
    public enum MaintenancePriority { Low, Medium, High, Critical }

    public class MaintenanceTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public MaintenanceType Type { get; set; }
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;
        public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;
        public string EquipmentId { get; set; } = string.Empty;
        public string EquipmentName { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; } = DateTime.Now;
        public DateTime? CompletedDate { get; set; }
        public string AssignedTo { get; set; } = string.Empty;
        public string CompletedBy { get; set; } = string.Empty;
        public int EstimatedDurationMinutes { get; set; }
        public int ActualDurationMinutes { get; set; }
        public List<string> Checklist { get; set; } = new List<string>();
        public List<bool> ChecklistCompleted { get; set; } = new List<bool>();
        public string Notes { get; set; } = string.Empty;
        public List<string> PartsUsed { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class MaintenanceSchedule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;
        public MaintenanceType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public int IntervalDays { get; set; } = 30;
        public int IntervalUsageHours { get; set; }
        public int IntervalUsageCount { get; set; }
        public DateTime LastPerformed { get; set; }
        public DateTime NextDue { get; set; }
        public bool IsEnabled { get; set; } = true;
        public List<string> DefaultChecklist { get; set; } = new List<string>();
    }

    public class EquipmentDowntime
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EquipmentId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }
        public double DurationHours { get; set; }
        public bool IsPlanned { get; set; }
        public string MaintenanceTaskId { get; set; } = string.Empty;
    }

    public class SparePart
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PartNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int QuantityInStock { get; set; }
        public int MinimumStock { get; set; }
        public int ReorderQuantity { get; set; }
        public double UnitCost { get; set; }
        public List<string> CompatibleEquipment { get; set; } = new List<string>();
        public string Location { get; set; } = string.Empty;
    }

    public class MaintenanceStatistics
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int InProgressTasks { get; set; }
        public double AverageCompletionTimeMinutes { get; set; }
        public double TotalDowntimeHours { get; set; }
        public double PlannedDowntimeHours { get; set; }
        public double UnplannedDowntimeHours { get; set; }
        public Dictionary<MaintenanceType, int> TasksByType { get; set; } = new Dictionary<MaintenanceType, int>();
    }

    public class MaintenanceTrackingManager
    {
        private static readonly Lazy<MaintenanceTrackingManager> _instance = new Lazy<MaintenanceTrackingManager>(() => new MaintenanceTrackingManager());
        public static MaintenanceTrackingManager Instance => _instance.Value;

        private readonly Dictionary<string, MaintenanceTask> _tasks = new Dictionary<string, MaintenanceTask>();
        private readonly Dictionary<string, MaintenanceSchedule> _schedules = new Dictionary<string, MaintenanceSchedule>();
        private readonly List<EquipmentDowntime> _downtimes = new List<EquipmentDowntime>();
        private readonly Dictionary<string, SparePart> _spareParts = new Dictionary<string, SparePart>();

        public event EventHandler<MaintenanceTask>? TaskCreated;
        public event EventHandler<MaintenanceTask>? TaskCompleted;
        public event EventHandler<MaintenanceTask>? TaskOverdue;
        public event EventHandler<SparePart>? LowStockAlert;

        private MaintenanceTrackingManager() { }

        public MaintenanceTask CreateTask(string name, MaintenanceType type, string equipmentId, string equipmentName, DateTime scheduledDate)
        {
            var task = new MaintenanceTask { Name = name, Type = type, EquipmentId = equipmentId, EquipmentName = equipmentName, ScheduledDate = scheduledDate };
            _tasks[task.Id] = task;
            TaskCreated?.Invoke(this, task);
            return task;
        }

        public void UpdateTask(MaintenanceTask task) => _tasks[task.Id] = task;
        public MaintenanceTask? GetTask(string id) { _tasks.TryGetValue(id, out var t); return t; }
        public IEnumerable<MaintenanceTask> GetAllTasks() => _tasks.Values.ToList();

        public IEnumerable<MaintenanceTask> GetTasksByStatus(MaintenanceStatus status) => _tasks.Values.Where(t => t.Status == status).ToList();
        public IEnumerable<MaintenanceTask> GetTasksByEquipment(string equipmentId) => _tasks.Values.Where(t => t.EquipmentId == equipmentId).ToList();
        public IEnumerable<MaintenanceTask> GetOverdueTasks() => _tasks.Values.Where(t => t.Status == MaintenanceStatus.Scheduled && t.ScheduledDate < DateTime.Now).ToList();
        public IEnumerable<MaintenanceTask> GetUpcomingTasks(int days = 7) => _tasks.Values.Where(t => t.Status == MaintenanceStatus.Scheduled && t.ScheduledDate >= DateTime.Now && t.ScheduledDate <= DateTime.Now.AddDays(days)).OrderBy(t => t.ScheduledDate).ToList();

        public void StartTask(string taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                task.Status = MaintenanceStatus.InProgress;
            }
        }

        public void CompleteTask(string taskId, string completedBy, int actualDurationMinutes, string notes = "")
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                task.Status = MaintenanceStatus.Completed;
                task.CompletedDate = DateTime.Now;
                task.CompletedBy = completedBy;
                task.ActualDurationMinutes = actualDurationMinutes;
                task.Notes = notes;
                TaskCompleted?.Invoke(this, task);
            }
        }

        public void CancelTask(string taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                task.Status = MaintenanceStatus.Cancelled;
            }
        }

        public MaintenanceSchedule CreateSchedule(string name, string equipmentId, MaintenanceType type, int intervalDays)
        {
            var schedule = new MaintenanceSchedule { Name = name, EquipmentId = equipmentId, Type = type, IntervalDays = intervalDays, LastPerformed = DateTime.Now, NextDue = DateTime.Now.AddDays(intervalDays) };
            _schedules[schedule.Id] = schedule;
            return schedule;
        }

        public void UpdateSchedule(MaintenanceSchedule schedule) => _schedules[schedule.Id] = schedule;
        public MaintenanceSchedule? GetSchedule(string id) { _schedules.TryGetValue(id, out var s); return s; }
        public IEnumerable<MaintenanceSchedule> GetAllSchedules() => _schedules.Values.ToList();
        public IEnumerable<MaintenanceSchedule> GetSchedulesByEquipment(string equipmentId) => _schedules.Values.Where(s => s.EquipmentId == equipmentId).ToList();
        public IEnumerable<MaintenanceSchedule> GetDueSchedules() => _schedules.Values.Where(s => s.IsEnabled && s.NextDue <= DateTime.Now).ToList();

        public void RecordSchedulePerformed(string scheduleId)
        {
            if (_schedules.TryGetValue(scheduleId, out var schedule))
            {
                schedule.LastPerformed = DateTime.Now;
                schedule.NextDue = DateTime.Now.AddDays(schedule.IntervalDays);
            }
        }

        public EquipmentDowntime StartDowntime(string equipmentId, string reason, bool isPlanned, string? maintenanceTaskId = null)
        {
            var downtime = new EquipmentDowntime { EquipmentId = equipmentId, Reason = reason, IsPlanned = isPlanned, MaintenanceTaskId = maintenanceTaskId ?? string.Empty };
            _downtimes.Add(downtime);
            return downtime;
        }

        public void EndDowntime(string downtimeId)
        {
            var downtime = _downtimes.FirstOrDefault(d => d.Id == downtimeId);
            if (downtime != null)
            {
                downtime.EndTime = DateTime.Now;
                downtime.DurationHours = (downtime.EndTime.Value - downtime.StartTime).TotalHours;
            }
        }

        public IEnumerable<EquipmentDowntime> GetDowntimes(string? equipmentId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var q = _downtimes.AsEnumerable();
            if (equipmentId != null) q = q.Where(d => d.EquipmentId == equipmentId);
            if (startDate.HasValue) q = q.Where(d => d.StartTime >= startDate.Value);
            if (endDate.HasValue) q = q.Where(d => d.StartTime <= endDate.Value);
            return q.OrderByDescending(d => d.StartTime).ToList();
        }

        public SparePart AddSparePart(string partNumber, string name, int quantity, int minStock, double unitCost)
        {
            var part = new SparePart { PartNumber = partNumber, Name = name, QuantityInStock = quantity, MinimumStock = minStock, UnitCost = unitCost };
            _spareParts[part.Id] = part;
            return part;
        }

        public void UpdateSparePartStock(string partId, int quantity)
        {
            if (_spareParts.TryGetValue(partId, out var part))
            {
                part.QuantityInStock = quantity;
                if (part.QuantityInStock <= part.MinimumStock) LowStockAlert?.Invoke(this, part);
            }
        }

        public void UseSparePart(string partId, int quantity)
        {
            if (_spareParts.TryGetValue(partId, out var part))
            {
                part.QuantityInStock = Math.Max(0, part.QuantityInStock - quantity);
                if (part.QuantityInStock <= part.MinimumStock) LowStockAlert?.Invoke(this, part);
            }
        }

        public IEnumerable<SparePart> GetAllSpareParts() => _spareParts.Values.ToList();
        public IEnumerable<SparePart> GetLowStockParts() => _spareParts.Values.Where(p => p.QuantityInStock <= p.MinimumStock).ToList();

        public MaintenanceStatistics GetStatistics(DateTime startDate, DateTime endDate)
        {
            var tasks = _tasks.Values.Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate).ToList();
            var downtimes = _downtimes.Where(d => d.StartTime >= startDate && d.StartTime <= endDate).ToList();
            var completedTasks = tasks.Where(t => t.Status == MaintenanceStatus.Completed).ToList();

            return new MaintenanceStatistics
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalTasks = tasks.Count,
                CompletedTasks = completedTasks.Count,
                OverdueTasks = tasks.Count(t => t.Status == MaintenanceStatus.Scheduled && t.ScheduledDate < DateTime.Now),
                InProgressTasks = tasks.Count(t => t.Status == MaintenanceStatus.InProgress),
                AverageCompletionTimeMinutes = completedTasks.Count > 0 ? completedTasks.Average(t => t.ActualDurationMinutes) : 0,
                TotalDowntimeHours = downtimes.Sum(d => d.DurationHours),
                PlannedDowntimeHours = downtimes.Where(d => d.IsPlanned).Sum(d => d.DurationHours),
                UnplannedDowntimeHours = downtimes.Where(d => !d.IsPlanned).Sum(d => d.DurationHours),
                TasksByType = tasks.GroupBy(t => t.Type).ToDictionary(g => g.Key, g => g.Count())
            };
        }

        public void CheckOverdueTasks()
        {
            foreach (var task in _tasks.Values.Where(t => t.Status == MaintenanceStatus.Scheduled && t.ScheduledDate < DateTime.Now))
            {
                task.Status = MaintenanceStatus.Overdue;
                TaskOverdue?.Invoke(this, task);
            }
        }

        public void DeleteTask(string id) => _tasks.Remove(id);
        public void DeleteSchedule(string id) => _schedules.Remove(id);
    }
}
