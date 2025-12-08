using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestStandClone.Core.AuditTrail
{
    /// <summary>
    /// Audit action type
    /// </summary>
    public enum AuditActionType
    {
        Create,
        Read,
        Update,
        Delete,
        Execute,
        Login,
        Logout,
        Export,
        Import,
        Print,
        Configure,
        Approve,
        Reject,
        Custom
    }

    /// <summary>
    /// Audit category
    /// </summary>
    public enum AuditCategory
    {
        Sequence,
        Step,
        Variable,
        User,
        Configuration,
        Execution,
        Report,
        System,
        Security,
        Data
    }

    /// <summary>
    /// Audit severity
    /// </summary>
    public enum AuditSeverity
    {
        Info,
        Warning,
        Critical,
        Security
    }

    /// <summary>
    /// Represents an audit trail entry
    /// </summary>
    public class AuditEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public AuditActionType Action { get; set; }
        public AuditCategory Category { get; set; }
        public AuditSeverity Severity { get; set; } = AuditSeverity.Info;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public Dictionary<string, string> AdditionalData { get; set; } = new();
        public bool RequiresReview { get; set; }
        public string ReviewedBy { get; set; } = string.Empty;
        public DateTime? ReviewedAt { get; set; }
        public string ReviewNotes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Audit query for searching
    /// </summary>
    public class AuditQuery
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? UserId { get; set; }
        public AuditActionType? Action { get; set; }
        public AuditCategory? Category { get; set; }
        public AuditSeverity? MinSeverity { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? SearchText { get; set; }
        public bool? RequiresReview { get; set; }
        public int MaxResults { get; set; } = 1000;
    }

    /// <summary>
    /// Audit statistics
    /// </summary>
    public class AuditStatistics
    {
        public int TotalEntries { get; set; }
        public int EntriesNeedingReview { get; set; }
        public Dictionary<AuditActionType, int> EntriesByAction { get; set; } = new();
        public Dictionary<AuditCategory, int> EntriesByCategory { get; set; } = new();
        public Dictionary<AuditSeverity, int> EntriesBySeverity { get; set; } = new();
        public Dictionary<string, int> EntriesByUser { get; set; } = new();
    }

    /// <summary>
    /// Audit trail configuration
    /// </summary>
    public class AuditConfiguration
    {
        public bool IsEnabled { get; set; } = true;
        public bool LogReads { get; set; }
        public bool LogExecutions { get; set; } = true;
        public bool RequireReviewForCritical { get; set; } = true;
        public bool RequireReviewForSecurity { get; set; } = true;
        public int RetentionDays { get; set; } = 365;
        public string StoragePath { get; set; } = string.Empty;
        public List<AuditCategory> EnabledCategories { get; set; } = new()
        {
            AuditCategory.Sequence, AuditCategory.User, AuditCategory.Configuration,
            AuditCategory.Execution, AuditCategory.Security
        };
    }

    /// <summary>
    /// Audit trail manager
    /// </summary>
    public class AuditTrailManager
    {
        private static AuditTrailManager? _instance;
        private static readonly object _lock = new();

        private readonly List<AuditEntry> _entries = new();
        private AuditConfiguration _config = new();
        private string _storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TestStandClone", "AuditTrail");

        public static AuditTrailManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new AuditTrailManager();
                    }
                }
                return _instance;
            }
        }

        public event EventHandler<AuditEntry>? EntryAdded;
        public event EventHandler<AuditEntry>? EntryReviewed;

        public AuditTrailManager()
        {
            Directory.CreateDirectory(_storagePath);
        }

        /// <summary>
        /// Configure the audit trail
        /// </summary>
        public void Configure(AuditConfiguration config)
        {
            _config = config;
            if (!string.IsNullOrEmpty(config.StoragePath))
            {
                _storagePath = config.StoragePath;
                Directory.CreateDirectory(_storagePath);
            }
        }

        /// <summary>
        /// Log an audit entry
        /// </summary>
        public AuditEntry Log(AuditActionType action, AuditCategory category, string description,
            string? entityType = null, string? entityId = null, string? entityName = null,
            string? oldValue = null, string? newValue = null, AuditSeverity severity = AuditSeverity.Info)
        {
            if (!_config.IsEnabled)
            {
                return new AuditEntry();
            }

            if (!_config.EnabledCategories.Contains(category))
            {
                return new AuditEntry();
            }

            if (action == AuditActionType.Read && !_config.LogReads)
            {
                return new AuditEntry();
            }

            if (action == AuditActionType.Execute && !_config.LogExecutions)
            {
                return new AuditEntry();
            }

            var entry = new AuditEntry
            {
                Action = action,
                Category = category,
                Severity = severity,
                Description = description,
                EntityType = entityType ?? string.Empty,
                EntityId = entityId ?? string.Empty,
                EntityName = entityName ?? string.Empty,
                OldValue = oldValue ?? string.Empty,
                NewValue = newValue ?? string.Empty,
                MachineName = Environment.MachineName
            };

            if ((_config.RequireReviewForCritical && severity == AuditSeverity.Critical) ||
                (_config.RequireReviewForSecurity && severity == AuditSeverity.Security))
            {
                entry.RequiresReview = true;
            }

            lock (_lock)
            {
                _entries.Add(entry);
            }

            EntryAdded?.Invoke(this, entry);
            return entry;
        }

        /// <summary>
        /// Log with user context
        /// </summary>
        public AuditEntry LogWithUser(string userId, string userName, AuditActionType action, 
            AuditCategory category, string description, string? entityType = null, 
            string? entityId = null, string? entityName = null)
        {
            var entry = Log(action, category, description, entityType, entityId, entityName);
            entry.UserId = userId;
            entry.UserName = userName;
            return entry;
        }

        /// <summary>
        /// Log a change with old and new values
        /// </summary>
        public AuditEntry LogChange(string entityType, string entityId, string entityName,
            object? oldValue, object? newValue, string description = "")
        {
            var oldJson = oldValue != null ? JsonSerializer.Serialize(oldValue) : string.Empty;
            var newJson = newValue != null ? JsonSerializer.Serialize(newValue) : string.Empty;

            return Log(AuditActionType.Update, AuditCategory.Data, 
                string.IsNullOrEmpty(description) ? $"Changed {entityType}" : description,
                entityType, entityId, entityName, oldJson, newJson);
        }

        /// <summary>
        /// Log a security event
        /// </summary>
        public AuditEntry LogSecurityEvent(AuditActionType action, string description, 
            string? userId = null, string? userName = null, bool isCritical = false)
        {
            var entry = Log(action, AuditCategory.Security, description, "User", userId, userName,
                severity: isCritical ? AuditSeverity.Critical : AuditSeverity.Security);
            entry.UserId = userId ?? string.Empty;
            entry.UserName = userName ?? string.Empty;
            return entry;
        }

        /// <summary>
        /// Review an audit entry
        /// </summary>
        public bool ReviewEntry(string entryId, string reviewerId, string notes)
        {
            lock (_lock)
            {
                var entry = _entries.FirstOrDefault(e => e.Id == entryId);
                if (entry == null)
                {
                    return false;
                }

                entry.ReviewedBy = reviewerId;
                entry.ReviewedAt = DateTime.Now;
                entry.ReviewNotes = notes;
                entry.RequiresReview = false;

                EntryReviewed?.Invoke(this, entry);
                return true;
            }
        }

        /// <summary>
        /// Query audit entries
        /// </summary>
        public IReadOnlyList<AuditEntry> Query(AuditQuery query)
        {
            lock (_lock)
            {
                var result = _entries.AsEnumerable();

                if (query.StartDate.HasValue)
                    result = result.Where(e => e.Timestamp >= query.StartDate.Value);

                if (query.EndDate.HasValue)
                    result = result.Where(e => e.Timestamp <= query.EndDate.Value);

                if (!string.IsNullOrEmpty(query.UserId))
                    result = result.Where(e => e.UserId == query.UserId);

                if (query.Action.HasValue)
                    result = result.Where(e => e.Action == query.Action.Value);

                if (query.Category.HasValue)
                    result = result.Where(e => e.Category == query.Category.Value);

                if (query.MinSeverity.HasValue)
                    result = result.Where(e => e.Severity >= query.MinSeverity.Value);

                if (!string.IsNullOrEmpty(query.EntityType))
                    result = result.Where(e => e.EntityType == query.EntityType);

                if (!string.IsNullOrEmpty(query.EntityId))
                    result = result.Where(e => e.EntityId == query.EntityId);

                if (!string.IsNullOrEmpty(query.SearchText))
                    result = result.Where(e => e.Description.Contains(query.SearchText, StringComparison.OrdinalIgnoreCase));

                if (query.RequiresReview.HasValue)
                    result = result.Where(e => e.RequiresReview == query.RequiresReview.Value);

                return result.OrderByDescending(e => e.Timestamp).Take(query.MaxResults).ToList();
            }
        }

        /// <summary>
        /// Get entries needing review
        /// </summary>
        public IReadOnlyList<AuditEntry> GetEntriesNeedingReview()
        {
            lock (_lock)
            {
                return _entries.Where(e => e.RequiresReview).OrderByDescending(e => e.Timestamp).ToList();
            }
        }

        /// <summary>
        /// Get statistics
        /// </summary>
        public AuditStatistics GetStatistics(DateTime? startDate = null, DateTime? endDate = null)
        {
            lock (_lock)
            {
                var entries = _entries.AsEnumerable();

                if (startDate.HasValue)
                    entries = entries.Where(e => e.Timestamp >= startDate.Value);

                if (endDate.HasValue)
                    entries = entries.Where(e => e.Timestamp <= endDate.Value);

                var list = entries.ToList();

                return new AuditStatistics
                {
                    TotalEntries = list.Count,
                    EntriesNeedingReview = list.Count(e => e.RequiresReview),
                    EntriesByAction = list.GroupBy(e => e.Action).ToDictionary(g => g.Key, g => g.Count()),
                    EntriesByCategory = list.GroupBy(e => e.Category).ToDictionary(g => g.Key, g => g.Count()),
                    EntriesBySeverity = list.GroupBy(e => e.Severity).ToDictionary(g => g.Key, g => g.Count()),
                    EntriesByUser = list.Where(e => !string.IsNullOrEmpty(e.UserName))
                        .GroupBy(e => e.UserName).ToDictionary(g => g.Key, g => g.Count())
                };
            }
        }

        /// <summary>
        /// Purge old entries
        /// </summary>
        public int PurgeOldEntries(int retentionDays = 0)
        {
            var days = retentionDays > 0 ? retentionDays : _config.RetentionDays;
            var cutoff = DateTime.Now.AddDays(-days);
            
            lock (_lock)
            {
                var toRemove = _entries.Where(e => e.Timestamp < cutoff && !e.RequiresReview).ToList();
                foreach (var entry in toRemove)
                {
                    _entries.Remove(entry);
                }
                return toRemove.Count;
            }
        }

        /// <summary>
        /// Save audit trail to file
        /// </summary>
        public async Task SaveAsync()
        {
            var filePath = Path.Combine(_storagePath, $"audit_{DateTime.Now:yyyy-MM}.json");
            List<AuditEntry> entriesToSave;
            lock (_lock)
            {
                entriesToSave = _entries.ToList();
            }
            var json = JsonSerializer.Serialize(entriesToSave, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Load audit trail from file
        /// </summary>
        public async Task LoadAsync(string? filePath = null)
        {
            var path = filePath ?? Path.Combine(_storagePath, $"audit_{DateTime.Now:yyyy-MM}.json");
            if (File.Exists(path))
            {
                var json = await File.ReadAllTextAsync(path);
                var entries = JsonSerializer.Deserialize<List<AuditEntry>>(json);
                if (entries != null)
                {
                    lock (_lock)
                    {
                        _entries.Clear();
                        _entries.AddRange(entries);
                    }
                }
            }
        }

        /// <summary>
        /// Export audit trail to CSV
        /// </summary>
        public async Task ExportToCsvAsync(string filePath, AuditQuery? query = null)
        {
            var entries = query != null ? Query(query) : _entries.ToList();
            
            var lines = new List<string>
            {
                "Timestamp,UserId,UserName,Action,Category,Severity,EntityType,EntityId,EntityName,Description"
            };

            foreach (var entry in entries)
            {
                lines.Add($"\"{entry.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{entry.UserId}\",\"{entry.UserName}\",\"{entry.Action}\",\"{entry.Category}\",\"{entry.Severity}\",\"{entry.EntityType}\",\"{entry.EntityId}\",\"{entry.EntityName}\",\"{entry.Description.Replace("\"", "\"\"")}\"");
            }

            await File.WriteAllLinesAsync(filePath, lines);
        }
    }
}
