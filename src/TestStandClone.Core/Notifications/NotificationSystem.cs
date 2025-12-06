// NotificationSystem.cs - Event notifications and alerts
// Provides notification management for test events

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TestStandClone.Core.Notifications
{
    /// <summary>
    /// Notification severity level
    /// </summary>
    public enum NotificationSeverity
    {
        /// <summary>Informational notification</summary>
        Information,
        /// <summary>Warning notification</summary>
        Warning,
        /// <summary>Error notification</summary>
        Error,
        /// <summary>Critical notification</summary>
        Critical,
        /// <summary>Success notification</summary>
        Success
    }

    /// <summary>
    /// Notification category
    /// </summary>
    public enum NotificationCategory
    {
        /// <summary>General notification</summary>
        General,
        /// <summary>Test execution related</summary>
        Execution,
        /// <summary>Hardware/instrument related</summary>
        Hardware,
        /// <summary>System related</summary>
        System,
        /// <summary>User action required</summary>
        UserAction,
        /// <summary>Report related</summary>
        Report,
        /// <summary>Security related</summary>
        Security
    }

    /// <summary>
    /// Represents a notification
    /// </summary>
    public class Notification
    {
        /// <summary>Unique identifier</summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        /// <summary>Notification title</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Notification message</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Severity level</summary>
        public NotificationSeverity Severity { get; set; } = NotificationSeverity.Information;

        /// <summary>Category</summary>
        public NotificationCategory Category { get; set; } = NotificationCategory.General;

        /// <summary>Timestamp</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>Source of the notification</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>Whether the notification has been read</summary>
        public bool IsRead { get; set; } = false;

        /// <summary>Whether the notification has been dismissed</summary>
        public bool IsDismissed { get; set; } = false;

        /// <summary>Related sequence ID</summary>
        public string? SequenceId { get; set; }

        /// <summary>Related step ID</summary>
        public string? StepId { get; set; }

        /// <summary>Additional data</summary>
        public Dictionary<string, object?> Data { get; set; } = new();

        /// <summary>Actions available for this notification</summary>
        public List<NotificationAction> Actions { get; set; } = new();
    }

    /// <summary>
    /// Represents an action that can be taken on a notification
    /// </summary>
    public class NotificationAction
    {
        /// <summary>Action name</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Action display text</summary>
        public string DisplayText { get; set; } = string.Empty;

        /// <summary>Action callback</summary>
        public Action<Notification>? Callback { get; set; }
    }

    /// <summary>
    /// Interface for notification handlers
    /// </summary>
    public interface INotificationHandler
    {
        /// <summary>Handle a notification</summary>
        Task HandleAsync(Notification notification, CancellationToken cancellationToken = default);
        
        /// <summary>Whether the handler accepts the notification</summary>
        bool CanHandle(Notification notification);
    }

    /// <summary>
    /// Console notification handler
    /// </summary>
    public class ConsoleNotificationHandler : INotificationHandler
    {
        public bool CanHandle(Notification notification) => true;

        public Task HandleAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            var color = notification.Severity switch
            {
                NotificationSeverity.Information => ConsoleColor.White,
                NotificationSeverity.Warning => ConsoleColor.Yellow,
                NotificationSeverity.Error => ConsoleColor.Red,
                NotificationSeverity.Critical => ConsoleColor.DarkRed,
                NotificationSeverity.Success => ConsoleColor.Green,
                _ => ConsoleColor.White
            };

            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"[{notification.Timestamp:HH:mm:ss}] [{notification.Severity}] {notification.Title}: {notification.Message}");
            Console.ForegroundColor = originalColor;

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// File notification handler
    /// </summary>
    public class FileNotificationHandler : INotificationHandler
    {
        private readonly string _filePath;
        private readonly object _writeLock = new();

        public FileNotificationHandler(string filePath)
        {
            _filePath = filePath;
            
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public bool CanHandle(Notification notification) => true;

        public Task HandleAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            var logLine = $"{notification.Timestamp:yyyy-MM-dd HH:mm:ss}|{notification.Severity}|{notification.Category}|{notification.Title}|{notification.Message}|{notification.Source}";
            
            lock (_writeLock)
            {
                File.AppendAllText(_filePath, logLine + Environment.NewLine);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Webhook notification handler for external integrations
    /// </summary>
    public class WebhookNotificationHandler : INotificationHandler
    {
        private readonly string _webhookUrl;
        private readonly NotificationSeverity _minimumSeverity;

        public WebhookNotificationHandler(string webhookUrl, NotificationSeverity minimumSeverity = NotificationSeverity.Warning)
        {
            _webhookUrl = webhookUrl;
            _minimumSeverity = minimumSeverity;
        }

        public bool CanHandle(Notification notification)
        {
            return notification.Severity >= _minimumSeverity;
        }

        public async Task HandleAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                var payload = new
                {
                    id = notification.Id,
                    title = notification.Title,
                    message = notification.Message,
                    severity = notification.Severity.ToString(),
                    category = notification.Category.ToString(),
                    timestamp = notification.Timestamp.ToString("O"),
                    source = notification.Source
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                await client.PostAsync(_webhookUrl, content, cancellationToken);
            }
            catch
            {
                // Silently fail for webhook errors
            }
        }
    }

    /// <summary>
    /// Notification filter
    /// </summary>
    public class NotificationFilter
    {
        /// <summary>Minimum severity to show</summary>
        public NotificationSeverity? MinimumSeverity { get; set; }

        /// <summary>Categories to include</summary>
        public List<NotificationCategory>? Categories { get; set; }

        /// <summary>Only unread notifications</summary>
        public bool? UnreadOnly { get; set; }

        /// <summary>Only undismissed notifications</summary>
        public bool? UndismissedOnly { get; set; }

        /// <summary>Since date</summary>
        public DateTime? Since { get; set; }

        /// <summary>Maximum number of results</summary>
        public int? MaxResults { get; set; }
    }

    /// <summary>
    /// Notification manager singleton
    /// </summary>
    public class NotificationManager
    {
        private static NotificationManager? _instance;
        private static readonly object _lock = new();

        private readonly ConcurrentDictionary<string, Notification> _notifications = new();
        private readonly List<Notification> _recentNotifications = new();
        private readonly object _recentLock = new();
        private readonly List<INotificationHandler> _handlers = new();
        private int _maxRecentNotifications = 500;

        public static NotificationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new NotificationManager();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Event raised when a notification is added
        /// </summary>
        public event EventHandler<Notification>? NotificationAdded;

        private NotificationManager()
        {
            // Add default console handler
            _handlers.Add(new ConsoleNotificationHandler());
        }

        /// <summary>
        /// Maximum recent notifications to keep in memory
        /// </summary>
        public int MaxRecentNotifications
        {
            get => _maxRecentNotifications;
            set => _maxRecentNotifications = Math.Max(100, value);
        }

        /// <summary>
        /// Add a notification handler
        /// </summary>
        public void AddHandler(INotificationHandler handler)
        {
            lock (_handlers)
            {
                _handlers.Add(handler);
            }
        }

        /// <summary>
        /// Remove a notification handler
        /// </summary>
        public void RemoveHandler(INotificationHandler handler)
        {
            lock (_handlers)
            {
                _handlers.Remove(handler);
            }
        }

        /// <summary>
        /// Clear all handlers
        /// </summary>
        public void ClearHandlers()
        {
            lock (_handlers)
            {
                _handlers.Clear();
            }
        }

        /// <summary>
        /// Send a notification
        /// </summary>
        public async Task NotifyAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            _notifications[notification.Id] = notification;

            lock (_recentLock)
            {
                _recentNotifications.Add(notification);
                while (_recentNotifications.Count > _maxRecentNotifications)
                {
                    _recentNotifications.RemoveAt(0);
                }
            }

            NotificationAdded?.Invoke(this, notification);

            // Handle notification with all registered handlers
            List<INotificationHandler> handlers;
            lock (_handlers)
            {
                handlers = _handlers.ToList();
            }

            foreach (var handler in handlers.Where(h => h.CanHandle(notification)))
            {
                try
                {
                    await handler.HandleAsync(notification, cancellationToken);
                }
                catch
                {
                    // Continue with other handlers
                }
            }
        }

        /// <summary>
        /// Send a simple notification
        /// </summary>
        public Task NotifyAsync(string title, string message, NotificationSeverity severity = NotificationSeverity.Information, CancellationToken cancellationToken = default)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Severity = severity
            };
            return NotifyAsync(notification, cancellationToken);
        }

        /// <summary>
        /// Send an information notification
        /// </summary>
        public Task InfoAsync(string title, string message, CancellationToken cancellationToken = default)
        {
            return NotifyAsync(title, message, NotificationSeverity.Information, cancellationToken);
        }

        /// <summary>
        /// Send a warning notification
        /// </summary>
        public Task WarnAsync(string title, string message, CancellationToken cancellationToken = default)
        {
            return NotifyAsync(title, message, NotificationSeverity.Warning, cancellationToken);
        }

        /// <summary>
        /// Send an error notification
        /// </summary>
        public Task ErrorAsync(string title, string message, CancellationToken cancellationToken = default)
        {
            return NotifyAsync(title, message, NotificationSeverity.Error, cancellationToken);
        }

        /// <summary>
        /// Send a success notification
        /// </summary>
        public Task SuccessAsync(string title, string message, CancellationToken cancellationToken = default)
        {
            return NotifyAsync(title, message, NotificationSeverity.Success, cancellationToken);
        }

        /// <summary>
        /// Get a notification by ID
        /// </summary>
        public Notification? GetNotification(string id)
        {
            _notifications.TryGetValue(id, out var notification);
            return notification;
        }

        /// <summary>
        /// Get notifications with filter
        /// </summary>
        public IEnumerable<Notification> GetNotifications(NotificationFilter? filter = null)
        {
            IEnumerable<Notification> query;

            lock (_recentLock)
            {
                query = _recentNotifications.AsEnumerable();
            }

            if (filter != null)
            {
                if (filter.MinimumSeverity.HasValue)
                {
                    query = query.Where(n => n.Severity >= filter.MinimumSeverity.Value);
                }

                if (filter.Categories != null && filter.Categories.Count > 0)
                {
                    query = query.Where(n => filter.Categories.Contains(n.Category));
                }

                if (filter.UnreadOnly == true)
                {
                    query = query.Where(n => !n.IsRead);
                }

                if (filter.UndismissedOnly == true)
                {
                    query = query.Where(n => !n.IsDismissed);
                }

                if (filter.Since.HasValue)
                {
                    query = query.Where(n => n.Timestamp >= filter.Since.Value);
                }

                if (filter.MaxResults.HasValue)
                {
                    query = query.Take(filter.MaxResults.Value);
                }
            }

            return query.OrderByDescending(n => n.Timestamp).ToList();
        }

        /// <summary>
        /// Get unread notification count
        /// </summary>
        public int UnreadCount
        {
            get
            {
                lock (_recentLock)
                {
                    return _recentNotifications.Count(n => !n.IsRead && !n.IsDismissed);
                }
            }
        }

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        public void MarkAsRead(string id)
        {
            if (_notifications.TryGetValue(id, out var notification))
            {
                notification.IsRead = true;
            }
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        public void MarkAllAsRead()
        {
            foreach (var notification in _notifications.Values)
            {
                notification.IsRead = true;
            }
        }

        /// <summary>
        /// Dismiss a notification
        /// </summary>
        public void Dismiss(string id)
        {
            if (_notifications.TryGetValue(id, out var notification))
            {
                notification.IsDismissed = true;
            }
        }

        /// <summary>
        /// Clear all notifications
        /// </summary>
        public void Clear()
        {
            _notifications.Clear();
            lock (_recentLock)
            {
                _recentNotifications.Clear();
            }
        }
    }
}
