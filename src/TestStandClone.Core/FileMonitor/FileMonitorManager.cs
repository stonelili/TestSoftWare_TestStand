using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TestStandClone.Core.FileMonitor
{
    #region File Monitor Classes

    /// <summary>
    /// File change type
    /// </summary>
    public enum FileChangeType
    {
        Created,
        Modified,
        Deleted,
        Renamed
    }

    /// <summary>
    /// File change event
    /// </summary>
    public class FileChangeEvent
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string FilePath { get; set; } = string.Empty;
        public string? OldPath { get; set; }
        public FileChangeType ChangeType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public long FileSize { get; set; }
        public string? FileExtension { get; set; }
    }

    /// <summary>
    /// File monitor configuration
    /// </summary>
    public class FileMonitorConfig
    {
        public string Path { get; set; } = string.Empty;
        public string Filter { get; set; } = "*.*";
        public bool IncludeSubdirectories { get; set; } = true;
        public bool NotifyOnCreate { get; set; } = true;
        public bool NotifyOnModify { get; set; } = true;
        public bool NotifyOnDelete { get; set; } = true;
        public bool NotifyOnRename { get; set; } = true;
        public int DebounceDurationMs { get; set; } = 500;
    }

    /// <summary>
    /// File monitor handler interface
    /// </summary>
    public interface IFileChangeHandler
    {
        string Name { get; }
        bool CanHandle(FileChangeEvent evt);
        Task HandleAsync(FileChangeEvent evt);
    }

    /// <summary>
    /// Sequence file change handler
    /// </summary>
    public class SequenceFileChangeHandler : IFileChangeHandler
    {
        public string Name => "Sequence File Handler";

        public bool CanHandle(FileChangeEvent evt)
        {
            return evt.FileExtension?.ToLowerInvariant() == ".seq" ||
                   evt.FileExtension?.ToLowerInvariant() == ".json";
        }

        public Task HandleAsync(FileChangeEvent evt)
        {
            Console.WriteLine($"[Sequence] File {evt.ChangeType}: {evt.FilePath}");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Monitored folder
    /// </summary>
    public class MonitoredFolder
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public FileMonitorConfig Config { get; set; } = new();
        public FileSystemWatcher? Watcher { get; set; }
        public bool IsActive { get; set; }
        public DateTime StartedAt { get; set; }
        public int EventCount { get; set; }
    }

    /// <summary>
    /// File monitor manager
    /// </summary>
    public class FileMonitorManager
    {
        private static readonly Lazy<FileMonitorManager> _instance = 
            new(() => new FileMonitorManager());
        public static FileMonitorManager Instance => _instance.Value;

        private readonly Dictionary<string, MonitoredFolder> _monitors = new();
        private readonly List<IFileChangeHandler> _handlers = new();
        private readonly List<FileChangeEvent> _eventHistory = new();
        private readonly Dictionary<string, Timer> _debounceTimers = new();
        private const int MaxHistorySize = 1000;

        public event EventHandler<FileChangeEvent>? FileChanged;

        public IReadOnlyDictionary<string, MonitoredFolder> Monitors => _monitors;
        public IReadOnlyList<FileChangeEvent> EventHistory => _eventHistory.AsReadOnly();

        public FileMonitorManager()
        {
            // Register default handlers
            _handlers.Add(new SequenceFileChangeHandler());
        }

        public void RegisterHandler(IFileChangeHandler handler)
        {
            _handlers.Add(handler);
        }

        public void UnregisterHandler(string name)
        {
            _handlers.RemoveAll(h => h.Name == name);
        }

        public string StartMonitoring(FileMonitorConfig config)
        {
            if (!Directory.Exists(config.Path))
                throw new DirectoryNotFoundException($"Directory not found: {config.Path}");

            var folder = new MonitoredFolder
            {
                Config = config,
                StartedAt = DateTime.Now,
                IsActive = true
            };

            var watcher = new FileSystemWatcher(config.Path, config.Filter)
            {
                IncludeSubdirectories = config.IncludeSubdirectories,
                EnableRaisingEvents = true
            };

            if (config.NotifyOnCreate)
                watcher.Created += (s, e) => OnFileChanged(folder, e.FullPath, FileChangeType.Created);
            
            if (config.NotifyOnModify)
                watcher.Changed += (s, e) => OnFileChanged(folder, e.FullPath, FileChangeType.Modified);
            
            if (config.NotifyOnDelete)
                watcher.Deleted += (s, e) => OnFileChanged(folder, e.FullPath, FileChangeType.Deleted);
            
            if (config.NotifyOnRename)
                watcher.Renamed += (s, e) => OnFileRenamed(folder, e.FullPath, e.OldFullPath);

            folder.Watcher = watcher;
            _monitors[folder.Id] = folder;

            return folder.Id;
        }

        public void StopMonitoring(string monitorId)
        {
            if (_monitors.TryGetValue(monitorId, out var folder))
            {
                folder.Watcher?.Dispose();
                folder.IsActive = false;
                _monitors.Remove(monitorId);
            }
        }

        public void StopAllMonitoring()
        {
            var ids = _monitors.Keys.ToList();
            foreach (var id in ids)
            {
                StopMonitoring(id);
            }
        }

        public void PauseMonitoring(string monitorId)
        {
            if (_monitors.TryGetValue(monitorId, out var folder) && folder.Watcher != null)
            {
                folder.Watcher.EnableRaisingEvents = false;
                folder.IsActive = false;
            }
        }

        public void ResumeMonitoring(string monitorId)
        {
            if (_monitors.TryGetValue(monitorId, out var folder) && folder.Watcher != null)
            {
                folder.Watcher.EnableRaisingEvents = true;
                folder.IsActive = true;
            }
        }

        private void OnFileChanged(MonitoredFolder folder, string path, FileChangeType changeType)
        {
            // Debounce rapid changes
            var key = $"{folder.Id}:{path}";
            
            if (_debounceTimers.TryGetValue(key, out var existingTimer))
            {
                existingTimer.Dispose();
            }

            _debounceTimers[key] = new Timer(_ =>
            {
                _debounceTimers.Remove(key);
                ProcessFileChange(folder, path, changeType);
            }, null, folder.Config.DebounceDurationMs, Timeout.Infinite);
        }

        private void OnFileRenamed(MonitoredFolder folder, string newPath, string oldPath)
        {
            var evt = new FileChangeEvent
            {
                FilePath = newPath,
                OldPath = oldPath,
                ChangeType = FileChangeType.Renamed,
                FileExtension = Path.GetExtension(newPath)
            };

            ProcessEvent(folder, evt);
        }

        private void ProcessFileChange(MonitoredFolder folder, string path, FileChangeType changeType)
        {
            var evt = new FileChangeEvent
            {
                FilePath = path,
                ChangeType = changeType,
                FileExtension = Path.GetExtension(path)
            };

            if (changeType != FileChangeType.Deleted && File.Exists(path))
            {
                try
                {
                    evt.FileSize = new FileInfo(path).Length;
                }
                catch { }
            }

            ProcessEvent(folder, evt);
        }

        private void ProcessEvent(MonitoredFolder folder, FileChangeEvent evt)
        {
            folder.EventCount++;
            AddToHistory(evt);
            FileChanged?.Invoke(this, evt);

            // Invoke handlers
            foreach (var handler in _handlers)
            {
                if (handler.CanHandle(evt))
                {
                    Task.Run(() => handler.HandleAsync(evt));
                }
            }
        }

        private void AddToHistory(FileChangeEvent evt)
        {
            _eventHistory.Add(evt);
            if (_eventHistory.Count > MaxHistorySize)
                _eventHistory.RemoveAt(0);
        }

        public void ClearHistory()
        {
            _eventHistory.Clear();
        }

        public List<FileChangeEvent> GetEventsByPath(string pathPattern)
        {
            return _eventHistory
                .Where(e => e.FilePath.Contains(pathPattern, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public List<FileChangeEvent> GetEventsByType(FileChangeType changeType)
        {
            return _eventHistory.Where(e => e.ChangeType == changeType).ToList();
        }
    }

    #endregion
}
