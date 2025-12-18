using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace TestStandClone.Core.Migration
{
    /// <summary>
    /// Migration status
    /// </summary>
    public enum MigrationStatus
    {
        /// <summary>Migration pending</summary>
        Pending,
        /// <summary>Migration in progress</summary>
        InProgress,
        /// <summary>Migration completed successfully</summary>
        Completed,
        /// <summary>Migration failed</summary>
        Failed,
        /// <summary>Migration partially completed</summary>
        Partial
    }

    /// <summary>
    /// Type of migration
    /// </summary>
    public enum MigrationType
    {
        /// <summary>Sequence file format upgrade</summary>
        SequenceUpgrade,
        /// <summary>Configuration migration</summary>
        Configuration,
        /// <summary>Database migration</summary>
        Database,
        /// <summary>Step type migration</summary>
        StepType,
        /// <summary>Full system migration</summary>
        FullSystem
    }

    /// <summary>
    /// Migration issue severity
    /// </summary>
    public enum IssueSeverity
    {
        /// <summary>Informational</summary>
        Info,
        /// <summary>Warning - can proceed</summary>
        Warning,
        /// <summary>Error - may cause problems</summary>
        Error,
        /// <summary>Critical - cannot proceed</summary>
        Critical
    }

    /// <summary>
    /// Represents a migration issue or warning
    /// </summary>
    public class MigrationIssue
    {
        /// <summary>Issue severity</summary>
        public IssueSeverity Severity { get; set; }
        /// <summary>Issue code</summary>
        public string Code { get; set; } = string.Empty;
        /// <summary>Issue message</summary>
        public string Message { get; set; } = string.Empty;
        /// <summary>Location (file, step, etc.)</summary>
        public string Location { get; set; } = string.Empty;
        /// <summary>Suggested resolution</summary>
        public string Resolution { get; set; } = string.Empty;
        /// <summary>Whether issue was resolved</summary>
        public bool Resolved { get; set; }
    }

    /// <summary>
    /// Migration task definition
    /// </summary>
    public class MigrationTask
    {
        /// <summary>Unique identifier</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>Task name</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Task description</summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>Migration type</summary>
        public MigrationType Type { get; set; }
        /// <summary>Source version</summary>
        public string SourceVersion { get; set; } = string.Empty;
        /// <summary>Target version</summary>
        public string TargetVersion { get; set; } = string.Empty;
        /// <summary>Source path</summary>
        public string SourcePath { get; set; } = string.Empty;
        /// <summary>Target path</summary>
        public string TargetPath { get; set; } = string.Empty;
        /// <summary>Status</summary>
        public MigrationStatus Status { get; set; } = MigrationStatus.Pending;
        /// <summary>Progress percentage (0-100)</summary>
        public int Progress { get; set; }
        /// <summary>Start time</summary>
        public DateTime? StartTime { get; set; }
        /// <summary>End time</summary>
        public DateTime? EndTime { get; set; }
        /// <summary>Issues found during migration</summary>
        public List<MigrationIssue> Issues { get; set; } = new();
        /// <summary>Items processed</summary>
        public int ItemsProcessed { get; set; }
        /// <summary>Total items</summary>
        public int TotalItems { get; set; }
        /// <summary>Error message if failed</summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Migration report
    /// </summary>
    public class MigrationReport
    {
        /// <summary>Report ID</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>Task ID</summary>
        public string TaskId { get; set; } = string.Empty;
        /// <summary>Generated at</summary>
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        /// <summary>Overall status</summary>
        public MigrationStatus Status { get; set; }
        /// <summary>Summary</summary>
        public string Summary { get; set; } = string.Empty;
        /// <summary>All issues</summary>
        public List<MigrationIssue> Issues { get; set; } = new();
        /// <summary>Statistics</summary>
        public Dictionary<string, int> Statistics { get; set; } = new();
        /// <summary>Detailed log</summary>
        public List<string> Log { get; set; } = new();
    }

    /// <summary>
    /// Interface for migration handlers
    /// </summary>
    public interface IMigrationHandler
    {
        /// <summary>Gets the migration type this handler supports</summary>
        MigrationType Type { get; }
        /// <summary>Gets supported source versions</summary>
        string[] SupportedSourceVersions { get; }
        /// <summary>Gets the target version</summary>
        string TargetVersion { get; }
        /// <summary>Analyzes migration requirements</summary>
        List<MigrationIssue> Analyze(MigrationTask task);
        /// <summary>Executes the migration</summary>
        MigrationReport Execute(MigrationTask task, Action<int>? progressCallback = null);
        /// <summary>Validates the migration result</summary>
        bool Validate(MigrationTask task);
    }

    /// <summary>
    /// Sequence file migration handler
    /// </summary>
    public class SequenceMigrationHandler : IMigrationHandler
    {
        /// <inheritdoc/>
        public MigrationType Type => MigrationType.SequenceUpgrade;
        
        /// <inheritdoc/>
        public string[] SupportedSourceVersions => new[] { "1.0", "1.1", "1.2", "2.0" };
        
        /// <inheritdoc/>
        public string TargetVersion => "3.0";

        /// <inheritdoc/>
        public List<MigrationIssue> Analyze(MigrationTask task)
        {
            var issues = new List<MigrationIssue>();
            
            if (!File.Exists(task.SourcePath))
            {
                issues.Add(new MigrationIssue
                {
                    Severity = IssueSeverity.Critical,
                    Code = "SEQ001",
                    Message = "Source file not found",
                    Location = task.SourcePath,
                    Resolution = "Verify the source file path"
                });
                return issues;
            }

            try
            {
                string content = File.ReadAllText(task.SourcePath);
                
                // Check for deprecated features
                if (content.Contains("\"StepType\": \"LegacyStep\""))
                {
                    issues.Add(new MigrationIssue
                    {
                        Severity = IssueSeverity.Warning,
                        Code = "SEQ002",
                        Message = "Deprecated step type 'LegacyStep' found",
                        Location = task.SourcePath,
                        Resolution = "LegacyStep will be converted to ActionStep"
                    });
                }

                // Check for old property names
                if (content.Contains("\"oldPropertyName\""))
                {
                    issues.Add(new MigrationIssue
                    {
                        Severity = IssueSeverity.Info,
                        Code = "SEQ003",
                        Message = "Old property naming convention detected",
                        Location = task.SourcePath,
                        Resolution = "Properties will be renamed to new convention"
                    });
                }
            }
            catch (Exception ex)
            {
                issues.Add(new MigrationIssue
                {
                    Severity = IssueSeverity.Error,
                    Code = "SEQ099",
                    Message = $"Error analyzing file: {ex.Message}",
                    Location = task.SourcePath
                });
            }

            return issues;
        }

        /// <inheritdoc/>
        public MigrationReport Execute(MigrationTask task, Action<int>? progressCallback = null)
        {
            var report = new MigrationReport
            {
                TaskId = task.Id,
                Status = MigrationStatus.InProgress
            };

            try
            {
                task.Status = MigrationStatus.InProgress;
                task.StartTime = DateTime.Now;
                progressCallback?.Invoke(10);

                // Read source file
                string content = File.ReadAllText(task.SourcePath);
                report.Log.Add($"Read source file: {task.SourcePath}");
                progressCallback?.Invoke(30);

                // Perform migrations
                content = MigrateContent(content, task.SourceVersion, report);
                progressCallback?.Invoke(70);

                // Write to target
                string targetDir = Path.GetDirectoryName(task.TargetPath) ?? ".";
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                
                File.WriteAllText(task.TargetPath, content);
                report.Log.Add($"Written target file: {task.TargetPath}");
                progressCallback?.Invoke(90);

                // Update task status
                task.Status = MigrationStatus.Completed;
                task.EndTime = DateTime.Now;
                task.Progress = 100;
                
                report.Status = MigrationStatus.Completed;
                report.Summary = $"Successfully migrated from v{task.SourceVersion} to v{task.TargetVersion}";
                progressCallback?.Invoke(100);
            }
            catch (Exception ex)
            {
                task.Status = MigrationStatus.Failed;
                task.ErrorMessage = ex.Message;
                report.Status = MigrationStatus.Failed;
                report.Summary = $"Migration failed: {ex.Message}";
                report.Log.Add($"Error: {ex.Message}");
            }

            report.Issues.AddRange(task.Issues);
            return report;
        }

        /// <inheritdoc/>
        public bool Validate(MigrationTask task)
        {
            if (!File.Exists(task.TargetPath)) return false;

            try
            {
                string content = File.ReadAllText(task.TargetPath);
                // Basic validation - check if it's valid JSON
                JsonDocument.Parse(content);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string MigrateContent(string content, string sourceVersion, MigrationReport report)
        {
            // Apply version-specific migrations
            if (sourceVersion == "1.0" || sourceVersion == "1.1")
            {
                content = content.Replace("\"StepType\": \"LegacyStep\"", "\"StepType\": \"ActionStep\"");
                report.Log.Add("Converted LegacyStep to ActionStep");
            }

            // Update version number
            content = content.Replace($"\"Version\": \"{sourceVersion}\"", "\"Version\": \"3.0\"");
            report.Log.Add($"Updated version from {sourceVersion} to 3.0");

            return content;
        }
    }

    /// <summary>
    /// Migration manager singleton
    /// </summary>
    public sealed class MigrationManager
    {
        private static readonly Lazy<MigrationManager> _instance = 
            new Lazy<MigrationManager>(() => new MigrationManager());
        
        /// <summary>Gets the singleton instance</summary>
        public static MigrationManager Instance => _instance.Value;

        private readonly Dictionary<MigrationType, IMigrationHandler> _handlers = new();
        private readonly List<MigrationTask> _tasks = new();
        private readonly List<MigrationReport> _reports = new();
        private readonly object _lockObject = new object();

        /// <summary>Event raised when migration progress changes</summary>
        public event EventHandler<(string TaskId, int Progress)>? ProgressChanged;

        /// <summary>Event raised when migration completes</summary>
        public event EventHandler<MigrationReport>? MigrationCompleted;

        private MigrationManager()
        {
            // Register default handlers
            RegisterHandler(new SequenceMigrationHandler());
        }

        /// <summary>Registers a migration handler</summary>
        public void RegisterHandler(IMigrationHandler handler)
        {
            lock (_lockObject)
            {
                _handlers[handler.Type] = handler;
            }
        }

        /// <summary>Gets all registered handlers</summary>
        public IReadOnlyDictionary<MigrationType, IMigrationHandler> Handlers => _handlers;

        /// <summary>Gets all migration tasks</summary>
        public IReadOnlyList<MigrationTask> Tasks => _tasks.AsReadOnly();

        /// <summary>Gets all migration reports</summary>
        public IReadOnlyList<MigrationReport> Reports => _reports.AsReadOnly();

        /// <summary>Creates a new migration task</summary>
        public MigrationTask CreateTask(MigrationType type, string sourcePath, string targetPath,
            string sourceVersion, string targetVersion)
        {
            var task = new MigrationTask
            {
                Type = type,
                SourcePath = sourcePath,
                TargetPath = targetPath,
                SourceVersion = sourceVersion,
                TargetVersion = targetVersion,
                Name = $"Migrate {Path.GetFileName(sourcePath)}",
                Description = $"Migrate from v{sourceVersion} to v{targetVersion}"
            };

            lock (_lockObject)
            {
                _tasks.Add(task);
            }

            return task;
        }

        /// <summary>Analyzes a migration task</summary>
        public List<MigrationIssue> Analyze(string taskId)
        {
            lock (_lockObject)
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null) return new List<MigrationIssue>();

                if (_handlers.TryGetValue(task.Type, out var handler))
                {
                    var issues = handler.Analyze(task);
                    task.Issues.AddRange(issues);
                    return issues;
                }

                return new List<MigrationIssue>
                {
                    new MigrationIssue
                    {
                        Severity = IssueSeverity.Critical,
                        Code = "MIG001",
                        Message = $"No handler registered for migration type: {task.Type}"
                    }
                };
            }
        }

        /// <summary>Executes a migration task</summary>
        public MigrationReport Execute(string taskId)
        {
            MigrationTask? task;
            IMigrationHandler? handler;

            lock (_lockObject)
            {
                task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null)
                {
                    return new MigrationReport
                    {
                        Status = MigrationStatus.Failed,
                        Summary = "Task not found"
                    };
                }

                _handlers.TryGetValue(task.Type, out handler);
            }

            if (handler == null)
            {
                return new MigrationReport
                {
                    TaskId = taskId,
                    Status = MigrationStatus.Failed,
                    Summary = $"No handler for migration type: {task.Type}"
                };
            }

            var report = handler.Execute(task, progress =>
            {
                task.Progress = progress;
                ProgressChanged?.Invoke(this, (taskId, progress));
            });

            lock (_lockObject)
            {
                _reports.Add(report);
            }

            MigrationCompleted?.Invoke(this, report);
            return report;
        }

        /// <summary>Validates a migration result</summary>
        public bool Validate(string taskId)
        {
            lock (_lockObject)
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                if (task == null) return false;

                if (_handlers.TryGetValue(task.Type, out var handler))
                {
                    return handler.Validate(task);
                }

                return false;
            }
        }

        /// <summary>Gets a migration task by ID</summary>
        public MigrationTask? GetTask(string taskId)
        {
            lock (_lockObject)
            {
                return _tasks.FirstOrDefault(t => t.Id == taskId);
            }
        }

        /// <summary>Gets a migration report by task ID</summary>
        public MigrationReport? GetReport(string taskId)
        {
            lock (_lockObject)
            {
                return _reports.FirstOrDefault(r => r.TaskId == taskId);
            }
        }

        /// <summary>Exports a migration report</summary>
        public void ExportReport(string taskId, string filePath, string format = "json")
        {
            var report = GetReport(taskId);
            if (report == null) return;

            string content;
            if (format.ToLower() == "json")
            {
                content = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Migration Report - {report.Id}");
                sb.AppendLine($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Status: {report.Status}");
                sb.AppendLine($"Summary: {report.Summary}");
                sb.AppendLine();
                sb.AppendLine("Issues:");
                foreach (var issue in report.Issues)
                {
                    sb.AppendLine($"  [{issue.Severity}] {issue.Code}: {issue.Message}");
                }
                sb.AppendLine();
                sb.AppendLine("Log:");
                foreach (var line in report.Log)
                {
                    sb.AppendLine($"  {line}");
                }
                content = sb.ToString();
            }

            File.WriteAllText(filePath, content);
        }

        /// <summary>Gets migration statistics</summary>
        public Dictionary<string, int> GetStatistics()
        {
            lock (_lockObject)
            {
                return new Dictionary<string, int>
                {
                    ["TotalTasks"] = _tasks.Count,
                    ["Pending"] = _tasks.Count(t => t.Status == MigrationStatus.Pending),
                    ["InProgress"] = _tasks.Count(t => t.Status == MigrationStatus.InProgress),
                    ["Completed"] = _tasks.Count(t => t.Status == MigrationStatus.Completed),
                    ["Failed"] = _tasks.Count(t => t.Status == MigrationStatus.Failed),
                    ["TotalReports"] = _reports.Count
                };
            }
        }
    }
}
