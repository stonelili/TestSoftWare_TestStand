using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestStandClone.Core.Synchronization
{
    /// <summary>
    /// Types of synchronization objects available in TestStand Clone
    /// </summary>
    public enum SyncObjectType
    {
        /// <summary>Named semaphore for limiting concurrent access</summary>
        Semaphore,
        /// <summary>Named mutex for exclusive access</summary>
        Mutex,
        /// <summary>Named lock for critical sections</summary>
        Lock,
        /// <summary>Rendezvous point for synchronizing multiple threads</summary>
        Rendezvous,
        /// <summary>Queue for ordered processing</summary>
        Queue,
        /// <summary>Notification object for signaling</summary>
        Notification,
        /// <summary>Batch synchronization for groups</summary>
        BatchSync
    }

    /// <summary>
    /// Information about a synchronization object
    /// </summary>
    public class SyncObjectInfo
    {
        public string Name { get; set; } = string.Empty;
        public SyncObjectType Type { get; set; }
        public int Count { get; set; }
        public int WaitingCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Rendezvous point for synchronizing multiple execution threads
    /// </summary>
    public class RendezvousPoint
    {
        private readonly int _expectedCount;
        private int _arrivedCount;
        private readonly TaskCompletionSource<bool> _allArrived = new();
        private readonly object _lock = new();

        public string Name { get; }
        public int ExpectedCount => _expectedCount;
        public int ArrivedCount { get { lock (_lock) return _arrivedCount; } }

        public RendezvousPoint(string name, int expectedCount)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _expectedCount = expectedCount > 0 ? expectedCount : throw new ArgumentOutOfRangeException(nameof(expectedCount));
        }

        /// <summary>
        /// Arrive at the rendezvous point and wait for all others
        /// </summary>
        public async Task<bool> ArriveAndWaitAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _arrivedCount++;
                if (_arrivedCount >= _expectedCount)
                {
                    _allArrived.TrySetResult(true);
                }
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var completedTask = await Task.WhenAny(_allArrived.Task, Task.Delay(Timeout.Infinite, cts.Token));
            
            return completedTask == _allArrived.Task;
        }

        /// <summary>
        /// Reset the rendezvous point for reuse
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _arrivedCount = 0;
            }
        }
    }

    /// <summary>
    /// Notification object for signaling between threads
    /// </summary>
    public class NotificationObject
    {
        private readonly TaskCompletionSource<object?> _notification = new();
        private readonly object _lock = new();
        private bool _isSignaled;

        public string Name { get; }
        public bool IsSignaled { get { lock (_lock) return _isSignaled; } }

        public NotificationObject(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Signal the notification object
        /// </summary>
        public void Signal(object? data = null)
        {
            lock (_lock)
            {
                _isSignaled = true;
            }
            _notification.TrySetResult(data);
        }

        /// <summary>
        /// Wait for the notification to be signaled
        /// </summary>
        public async Task<object?> WaitAsync(int timeoutMs = -1, CancellationToken cancellationToken = default)
        {
            if (timeoutMs < 0)
            {
                return await _notification.Task;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);

            try
            {
                return await _notification.Task.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Manages synchronization objects for parallel test execution
    /// Similar to TestStand's synchronization system
    /// </summary>
    public class SynchronizationManager
    {
        private static readonly Lazy<SynchronizationManager> _instance = new(() => new SynchronizationManager());
        public static SynchronizationManager Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _mutexes = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private readonly ConcurrentDictionary<string, RendezvousPoint> _rendezvousPoints = new();
        private readonly ConcurrentDictionary<string, ConcurrentQueue<object>> _queues = new();
        private readonly ConcurrentDictionary<string, NotificationObject> _notifications = new();
        private readonly ConcurrentDictionary<string, SyncObjectInfo> _objectInfos = new();

        private SynchronizationManager() { }

        #region Semaphore Operations

        /// <summary>
        /// Create or get a named semaphore
        /// </summary>
        public SemaphoreSlim GetOrCreateSemaphore(string name, int initialCount = 1, int maxCount = 1)
        {
            return _semaphores.GetOrAdd(name, _ =>
            {
                var sem = new SemaphoreSlim(initialCount, maxCount);
                _objectInfos[name] = new SyncObjectInfo
                {
                    Name = name,
                    Type = SyncObjectType.Semaphore,
                    Count = maxCount
                };
                return sem;
            });
        }

        /// <summary>
        /// Acquire a semaphore with optional timeout
        /// </summary>
        public async Task<bool> AcquireSemaphoreAsync(string name, int timeoutMs = -1, CancellationToken cancellationToken = default)
        {
            var semaphore = GetOrCreateSemaphore(name);
            return await semaphore.WaitAsync(timeoutMs, cancellationToken);
        }

        /// <summary>
        /// Release a semaphore
        /// </summary>
        public void ReleaseSemaphore(string name)
        {
            if (_semaphores.TryGetValue(name, out var semaphore))
            {
                semaphore.Release();
            }
        }

        #endregion

        #region Mutex Operations

        /// <summary>
        /// Create or get a named mutex (exclusive lock)
        /// </summary>
        public SemaphoreSlim GetOrCreateMutex(string name)
        {
            return _mutexes.GetOrAdd(name, _ =>
            {
                var mutex = new SemaphoreSlim(1, 1);
                _objectInfos[name] = new SyncObjectInfo
                {
                    Name = name,
                    Type = SyncObjectType.Mutex,
                    Count = 1
                };
                return mutex;
            });
        }

        /// <summary>
        /// Lock a mutex with optional timeout
        /// </summary>
        public async Task<bool> LockMutexAsync(string name, int timeoutMs = -1, CancellationToken cancellationToken = default)
        {
            var mutex = GetOrCreateMutex(name);
            return await mutex.WaitAsync(timeoutMs, cancellationToken);
        }

        /// <summary>
        /// Unlock a mutex
        /// </summary>
        public void UnlockMutex(string name)
        {
            if (_mutexes.TryGetValue(name, out var mutex))
            {
                mutex.Release();
            }
        }

        #endregion

        #region Lock Operations

        /// <summary>
        /// Acquire a named lock with optional timeout
        /// </summary>
        public async Task<bool> AcquireLockAsync(string name, int timeoutMs = -1, CancellationToken cancellationToken = default)
        {
            var lockObj = _locks.GetOrAdd(name, _ =>
            {
                _objectInfos[name] = new SyncObjectInfo
                {
                    Name = name,
                    Type = SyncObjectType.Lock,
                    Count = 1
                };
                return new SemaphoreSlim(1, 1);
            });
            return await lockObj.WaitAsync(timeoutMs, cancellationToken);
        }

        /// <summary>
        /// Release a named lock
        /// </summary>
        public void ReleaseLock(string name)
        {
            if (_locks.TryGetValue(name, out var lockObj))
            {
                lockObj.Release();
            }
        }

        #endregion

        #region Rendezvous Operations

        /// <summary>
        /// Create a rendezvous point for synchronizing multiple threads
        /// </summary>
        public RendezvousPoint CreateRendezvous(string name, int expectedCount)
        {
            var rendezvous = new RendezvousPoint(name, expectedCount);
            _rendezvousPoints[name] = rendezvous;
            _objectInfos[name] = new SyncObjectInfo
            {
                Name = name,
                Type = SyncObjectType.Rendezvous,
                Count = expectedCount
            };
            return rendezvous;
        }

        /// <summary>
        /// Get an existing rendezvous point
        /// </summary>
        public RendezvousPoint? GetRendezvous(string name)
        {
            _rendezvousPoints.TryGetValue(name, out var rendezvous);
            return rendezvous;
        }

        /// <summary>
        /// Arrive at a rendezvous point and wait for all others
        /// </summary>
        public async Task<bool> RendezvousAsync(string name, CancellationToken cancellationToken = default)
        {
            var rendezvous = GetRendezvous(name);
            if (rendezvous == null)
            {
                return false;
            }
            return await rendezvous.ArriveAndWaitAsync(cancellationToken);
        }

        #endregion

        #region Queue Operations

        /// <summary>
        /// Enqueue an object to a named queue
        /// </summary>
        public void Enqueue(string queueName, object item)
        {
            var queue = _queues.GetOrAdd(queueName, _ =>
            {
                _objectInfos[queueName] = new SyncObjectInfo
                {
                    Name = queueName,
                    Type = SyncObjectType.Queue
                };
                return new ConcurrentQueue<object>();
            });
            queue.Enqueue(item);
        }

        /// <summary>
        /// Dequeue an object from a named queue
        /// </summary>
        public bool TryDequeue(string queueName, out object? item)
        {
            item = null;
            if (_queues.TryGetValue(queueName, out var queue))
            {
                return queue.TryDequeue(out item);
            }
            return false;
        }

        /// <summary>
        /// Get queue count
        /// </summary>
        public int GetQueueCount(string queueName)
        {
            if (_queues.TryGetValue(queueName, out var queue))
            {
                return queue.Count;
            }
            return 0;
        }

        #endregion

        #region Notification Operations

        /// <summary>
        /// Create a notification object
        /// </summary>
        public NotificationObject CreateNotification(string name)
        {
            var notification = new NotificationObject(name);
            _notifications[name] = notification;
            _objectInfos[name] = new SyncObjectInfo
            {
                Name = name,
                Type = SyncObjectType.Notification
            };
            return notification;
        }

        /// <summary>
        /// Get an existing notification object
        /// </summary>
        public NotificationObject? GetNotification(string name)
        {
            _notifications.TryGetValue(name, out var notification);
            return notification;
        }

        /// <summary>
        /// Signal a notification object
        /// </summary>
        public void SignalNotification(string name, object? data = null)
        {
            if (_notifications.TryGetValue(name, out var notification))
            {
                notification.Signal(data);
            }
        }

        /// <summary>
        /// Wait for a notification
        /// </summary>
        public async Task<object?> WaitForNotificationAsync(string name, int timeoutMs = -1, CancellationToken cancellationToken = default)
        {
            if (_notifications.TryGetValue(name, out var notification))
            {
                return await notification.WaitAsync(timeoutMs, cancellationToken);
            }
            return null;
        }

        #endregion

        #region Management

        /// <summary>
        /// Get information about all synchronization objects
        /// </summary>
        public IEnumerable<SyncObjectInfo> GetAllSyncObjects()
        {
            return _objectInfos.Values;
        }

        /// <summary>
        /// Remove a synchronization object
        /// </summary>
        public bool RemoveSyncObject(string name)
        {
            _objectInfos.TryRemove(name, out _);
            _semaphores.TryRemove(name, out var sem);
            sem?.Dispose();
            _mutexes.TryRemove(name, out var mutex);
            mutex?.Dispose();
            _locks.TryRemove(name, out var lockObj);
            lockObj?.Dispose();
            _rendezvousPoints.TryRemove(name, out _);
            _queues.TryRemove(name, out _);
            _notifications.TryRemove(name, out _);
            return true;
        }

        /// <summary>
        /// Clear all synchronization objects
        /// </summary>
        public void ClearAll()
        {
            foreach (var sem in _semaphores.Values) sem.Dispose();
            foreach (var mutex in _mutexes.Values) mutex.Dispose();
            foreach (var lockObj in _locks.Values) lockObj.Dispose();
            
            _semaphores.Clear();
            _mutexes.Clear();
            _locks.Clear();
            _rendezvousPoints.Clear();
            _queues.Clear();
            _notifications.Clear();
            _objectInfos.Clear();
        }

        #endregion
    }
}
