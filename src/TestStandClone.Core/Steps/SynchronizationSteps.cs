using System;
using System.Threading;
using System.Threading.Tasks;
using TestStandClone.Core.Synchronization;

namespace TestStandClone.Core.Steps
{
    /// <summary>
    /// Step that acquires a named semaphore
    /// </summary>
    public class SemaphoreAcquireStep : TestStep
    {
        public string SemaphoreName { get; set; } = "DefaultSemaphore";
        public int MaxCount { get; set; } = 1;
        public int InitialCount { get; set; } = 1;
        public int TimeoutMs { get; set; } = -1;

        public SemaphoreAcquireStep() { Name = "Semaphore Acquire"; }
        public SemaphoreAcquireStep(string name, string semaphoreName, int timeoutMs = -1)
        {
            Name = name;
            SemaphoreName = semaphoreName;
            TimeoutMs = timeoutMs;
        }

        public override async Task ExecuteAsync(Context context)
        {
            var manager = SynchronizationManager.Instance;
            manager.GetOrCreateSemaphore(SemaphoreName, InitialCount, MaxCount);
            
            var acquired = await manager.AcquireSemaphoreAsync(SemaphoreName, TimeoutMs, CancellationToken.None);
            
            if (acquired)
            {
                Status = StepStatus.Passed;
                ResultText = $"Acquired semaphore '{SemaphoreName}'";
            }
            else
            {
                Status = StepStatus.Failed;
                ResultText = $"Timeout acquiring semaphore '{SemaphoreName}'";
            }
        }
    }

    /// <summary>
    /// Step that releases a named semaphore
    /// </summary>
    public class SemaphoreReleaseStep : TestStep
    {
        public string SemaphoreName { get; set; } = "DefaultSemaphore";

        public SemaphoreReleaseStep() { Name = "Semaphore Release"; }
        public SemaphoreReleaseStep(string name, string semaphoreName)
        {
            Name = name;
            SemaphoreName = semaphoreName;
        }

        public override Task ExecuteAsync(Context context)
        {
            var manager = SynchronizationManager.Instance;
            manager.ReleaseSemaphore(SemaphoreName);
            
            Status = StepStatus.Passed;
            ResultText = $"Released semaphore '{SemaphoreName}'";
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Step that acquires a named lock (mutex)
    /// </summary>
    public class LockAcquireStep : TestStep
    {
        public string LockName { get; set; } = "DefaultLock";
        public int TimeoutMs { get; set; } = -1;

        public LockAcquireStep() { Name = "Lock Acquire"; }
        public LockAcquireStep(string name, string lockName, int timeoutMs = -1)
        {
            Name = name;
            LockName = lockName;
            TimeoutMs = timeoutMs;
        }

        public override async Task ExecuteAsync(Context context)
        {
            var manager = SynchronizationManager.Instance;
            var acquired = await manager.AcquireLockAsync(LockName, TimeoutMs, CancellationToken.None);
            
            if (acquired)
            {
                Status = StepStatus.Passed;
                ResultText = $"Acquired lock '{LockName}'";
            }
            else
            {
                Status = StepStatus.Failed;
                ResultText = $"Timeout acquiring lock '{LockName}'";
            }
        }
    }

    /// <summary>
    /// Step that releases a named lock
    /// </summary>
    public class LockReleaseStep : TestStep
    {
        public string LockName { get; set; } = "DefaultLock";

        public LockReleaseStep() { Name = "Lock Release"; }
        public LockReleaseStep(string name, string lockName)
        {
            Name = name;
            LockName = lockName;
        }

        public override Task ExecuteAsync(Context context)
        {
            var manager = SynchronizationManager.Instance;
            manager.ReleaseLock(LockName);
            
            Status = StepStatus.Passed;
            ResultText = $"Released lock '{LockName}'";
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Step that waits at a rendezvous point for other threads
    /// </summary>
    public class RendezvousStep : TestStep
    {
        public string RendezvousName { get; set; } = "DefaultRendezvous";
        public int ExpectedCount { get; set; } = 2;
        public bool CreateIfNotExists { get; set; } = true;

        public RendezvousStep() { Name = "Rendezvous"; }
        public RendezvousStep(string name, string rendezvousName, int expectedCount = 2)
        {
            Name = name;
            RendezvousName = rendezvousName;
            ExpectedCount = expectedCount;
        }

        public override async Task ExecuteAsync(Context context)
        {
            var manager = SynchronizationManager.Instance;
            
            var rendezvous = manager.GetRendezvous(RendezvousName);
            if (rendezvous == null && CreateIfNotExists)
            {
                rendezvous = manager.CreateRendezvous(RendezvousName, ExpectedCount);
            }
            
            if (rendezvous == null)
            {
                Status = StepStatus.Error;
                ResultText = $"Rendezvous '{RendezvousName}' not found";
                return;
            }
            
            ResultText = $"Waiting at rendezvous '{RendezvousName}' ({rendezvous.ArrivedCount + 1}/{rendezvous.ExpectedCount})";
            var success = await rendezvous.ArriveAndWaitAsync(CancellationToken.None);
            
            if (success)
            {
                Status = StepStatus.Passed;
                ResultText = $"All threads arrived at rendezvous '{RendezvousName}'";
            }
            else
            {
                Status = StepStatus.Failed;
                ResultText = $"Rendezvous '{RendezvousName}' was cancelled";
            }
        }
    }

    /// <summary>
    /// Step that sends a notification
    /// </summary>
    public class NotificationSendStep : TestStep
    {
        public string NotificationName { get; set; } = "DefaultNotification";
        public string NotificationData { get; set; } = "";

        public NotificationSendStep() { Name = "Send Notification"; }
        public NotificationSendStep(string name, string notificationName)
        {
            Name = name;
            NotificationName = notificationName;
        }

        public override Task ExecuteAsync(Context context)
        {
            var manager = SynchronizationManager.Instance;
            
            var notification = manager.GetNotification(NotificationName);
            if (notification == null)
            {
                manager.CreateNotification(NotificationName);
            }
            
            manager.SignalNotification(NotificationName, NotificationData);
            
            Status = StepStatus.Passed;
            ResultText = $"Sent notification '{NotificationName}'";
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Step that waits for a notification
    /// </summary>
    public class NotificationWaitStep : TestStep
    {
        public string NotificationName { get; set; } = "DefaultNotification";
        public int TimeoutMs { get; set; } = -1;
        public bool CreateIfNotExists { get; set; } = true;

        public NotificationWaitStep() { Name = "Wait for Notification"; }
        public NotificationWaitStep(string name, string notificationName, int timeoutMs = -1)
        {
            Name = name;
            NotificationName = notificationName;
            TimeoutMs = timeoutMs;
        }

        public override async Task ExecuteAsync(Context context)
        {
            var manager = SynchronizationManager.Instance;
            
            if (manager.GetNotification(NotificationName) == null && CreateIfNotExists)
            {
                manager.CreateNotification(NotificationName);
            }
            
            ResultText = $"Waiting for notification '{NotificationName}'...";
            var data = await manager.WaitForNotificationAsync(NotificationName, TimeoutMs, CancellationToken.None);
            
            if (data != null || TimeoutMs == -1)
            {
                Status = StepStatus.Passed;
                ResultText = $"Received notification '{NotificationName}'";
                if (data != null)
                {
                    context.SetValue($"{NotificationName}_Data", data);
                }
            }
            else
            {
                Status = StepStatus.Failed;
                ResultText = $"Timeout waiting for notification '{NotificationName}'";
            }
        }
    }

    /// <summary>
    /// Step that enqueues data to a named queue
    /// </summary>
    public class QueueEnqueueStep : TestStep
    {
        public string QueueName { get; set; } = "DefaultQueue";
        public string DataExpression { get; set; } = "";

        public QueueEnqueueStep() { Name = "Queue Enqueue"; }
        public QueueEnqueueStep(string name, string queueName, string dataExpression = "")
        {
            Name = name;
            QueueName = queueName;
            DataExpression = dataExpression;
        }

        public override Task ExecuteAsync(Context context)
        {
            var manager = SynchronizationManager.Instance;
            
            object data = DataExpression;
            if (!string.IsNullOrEmpty(DataExpression))
            {
                // Try to get from context
                var contextData = context.GetValue<object>(DataExpression);
                if (contextData != null)
                {
                    data = contextData;
                }
            }
            
            manager.Enqueue(QueueName, data);
            
            Status = StepStatus.Passed;
            ResultText = $"Enqueued data to queue '{QueueName}' (count: {manager.GetQueueCount(QueueName)})";
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Step that dequeues data from a named queue
    /// </summary>
    public class QueueDequeueStep : TestStep
    {
        public string QueueName { get; set; } = "DefaultQueue";
        public string OutputVariable { get; set; } = "";
        public bool FailIfEmpty { get; set; } = true;

        public QueueDequeueStep() { Name = "Queue Dequeue"; }
        public QueueDequeueStep(string name, string queueName, string outputVariable = "")
        {
            Name = name;
            QueueName = queueName;
            OutputVariable = outputVariable;
        }

        public override Task ExecuteAsync(Context context)
        {
            var manager = SynchronizationManager.Instance;
            
            if (manager.TryDequeue(QueueName, out var data))
            {
                if (!string.IsNullOrEmpty(OutputVariable) && data != null)
                {
                    context.SetValue(OutputVariable, data);
                }
                
                Status = StepStatus.Passed;
                ResultText = $"Dequeued data from queue '{QueueName}'";
            }
            else
            {
                if (FailIfEmpty)
                {
                    Status = StepStatus.Failed;
                    ResultText = $"Queue '{QueueName}' is empty";
                }
                else
                {
                    Status = StepStatus.Passed;
                    ResultText = $"Queue '{QueueName}' is empty (no data)";
                }
            }
            
            return Task.CompletedTask;
        }
    }
}
