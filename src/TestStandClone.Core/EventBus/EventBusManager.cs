using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TestStandClone.Core.EventBus
{
    #region Event Bus Classes

    /// <summary>
    /// Event priority levels
    /// </summary>
    public enum EventPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Base event class
    /// </summary>
    public abstract class TestStandEvent
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; } = DateTime.Now;
        public EventPriority Priority { get; set; } = EventPriority.Normal;
        public string? Source { get; set; }
        public bool IsHandled { get; set; }
        public Dictionary<string, object> Metadata { get; } = new();
    }

    /// <summary>
    /// Generic event with typed data
    /// </summary>
    public class TestStandEvent<T> : TestStandEvent
    {
        public T? Data { get; set; }
    }

    /// <summary>
    /// Execution started event
    /// </summary>
    public class ExecutionStartedEvent : TestStandEvent
    {
        public string SequenceName { get; set; } = string.Empty;
        public string ExecutionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Execution completed event
    /// </summary>
    public class ExecutionCompletedEvent : TestStandEvent
    {
        public string SequenceName { get; set; } = string.Empty;
        public string ExecutionId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Step executed event
    /// </summary>
    public class StepExecutedEvent : TestStandEvent
    {
        public string StepName { get; set; } = string.Empty;
        public string StepId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Variable changed event
    /// </summary>
    public class VariableChangedEvent : TestStandEvent
    {
        public string VariableName { get; set; } = string.Empty;
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
    }

    /// <summary>
    /// Event subscription interface
    /// </summary>
    public interface IEventSubscription
    {
        string Id { get; }
        Type EventType { get; }
        void Invoke(TestStandEvent evt);
    }

    /// <summary>
    /// Typed event subscription
    /// </summary>
    public class EventSubscription<T> : IEventSubscription where T : TestStandEvent
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public Type EventType => typeof(T);
        public Action<T> Handler { get; }
        public Predicate<T>? Filter { get; }

        public EventSubscription(Action<T> handler, Predicate<T>? filter = null)
        {
            Handler = handler;
            Filter = filter;
        }

        public void Invoke(TestStandEvent evt)
        {
            if (evt is T typedEvent && (Filter?.Invoke(typedEvent) ?? true))
            {
                Handler(typedEvent);
            }
        }
    }

    /// <summary>
    /// Event bus for publish/subscribe pattern
    /// </summary>
    public class EventBus
    {
        private static readonly Lazy<EventBus> _instance = new(() => new EventBus());
        public static EventBus Instance => _instance.Value;

        private readonly Dictionary<Type, List<IEventSubscription>> _subscriptions = new();
        private readonly List<TestStandEvent> _eventHistory = new();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private const int MaxHistorySize = 1000;

        public event EventHandler<TestStandEvent>? EventPublished;

        public IReadOnlyList<TestStandEvent> EventHistory => _eventHistory.AsReadOnly();

        /// <summary>
        /// Subscribe to events of type T
        /// </summary>
        public string Subscribe<T>(Action<T> handler, Predicate<T>? filter = null) where T : TestStandEvent
        {
            var subscription = new EventSubscription<T>(handler, filter);
            
            _lock.Wait();
            try
            {
                if (!_subscriptions.TryGetValue(typeof(T), out var list))
                {
                    list = new List<IEventSubscription>();
                    _subscriptions[typeof(T)] = list;
                }
                list.Add(subscription);
            }
            finally
            {
                _lock.Release();
            }

            return subscription.Id;
        }

        /// <summary>
        /// Unsubscribe by subscription ID
        /// </summary>
        public bool Unsubscribe(string subscriptionId)
        {
            _lock.Wait();
            try
            {
                foreach (var list in _subscriptions.Values)
                {
                    var sub = list.FirstOrDefault(s => s.Id == subscriptionId);
                    if (sub != null)
                    {
                        list.Remove(sub);
                        return true;
                    }
                }
            }
            finally
            {
                _lock.Release();
            }
            return false;
        }

        /// <summary>
        /// Publish an event synchronously
        /// </summary>
        public void Publish(TestStandEvent evt)
        {
            AddToHistory(evt);
            EventPublished?.Invoke(this, evt);

            _lock.Wait();
            try
            {
                // Get subscriptions for this event type and base types
                var eventType = evt.GetType();
                var subscriptions = new List<IEventSubscription>();

                while (eventType != null && eventType != typeof(object))
                {
                    if (_subscriptions.TryGetValue(eventType, out var list))
                    {
                        subscriptions.AddRange(list);
                    }
                    eventType = eventType.BaseType;
                }

                // Order by priority and invoke
                foreach (var sub in subscriptions.OrderByDescending(s => 
                    evt.Priority))
                {
                    try
                    {
                        sub.Invoke(evt);
                    }
                    catch (Exception)
                    {
                        // Log but continue with other handlers
                    }
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Publish an event asynchronously
        /// </summary>
        public async Task PublishAsync(TestStandEvent evt)
        {
            await Task.Run(() => Publish(evt));
        }

        /// <summary>
        /// Publish multiple events
        /// </summary>
        public void PublishAll(IEnumerable<TestStandEvent> events)
        {
            foreach (var evt in events.OrderByDescending(e => e.Priority))
            {
                Publish(evt);
            }
        }

        /// <summary>
        /// Clear all subscriptions
        /// </summary>
        public void ClearSubscriptions()
        {
            _lock.Wait();
            try
            {
                _subscriptions.Clear();
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Clear event history
        /// </summary>
        public void ClearHistory()
        {
            _eventHistory.Clear();
        }

        /// <summary>
        /// Get events of a specific type from history
        /// </summary>
        public List<T> GetEvents<T>() where T : TestStandEvent
        {
            return _eventHistory.OfType<T>().ToList();
        }

        /// <summary>
        /// Get events within a time range
        /// </summary>
        public List<TestStandEvent> GetEvents(DateTime from, DateTime to)
        {
            return _eventHistory
                .Where(e => e.Timestamp >= from && e.Timestamp <= to)
                .ToList();
        }

        private void AddToHistory(TestStandEvent evt)
        {
            _eventHistory.Add(evt);
            if (_eventHistory.Count > MaxHistorySize)
                _eventHistory.RemoveAt(0);
        }
    }

    #endregion
}
