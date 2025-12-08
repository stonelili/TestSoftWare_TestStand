using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TestStandClone.Core.ResourceManagement
{
    /// <summary>
    /// Resource type enumeration
    /// </summary>
    public enum ResourceType
    {
        Instrument,
        Fixture,
        Software,
        Hardware,
        Network,
        Database,
        File,
        Custom
    }

    /// <summary>
    /// Resource status enumeration
    /// </summary>
    public enum ResourceStatus
    {
        Available,
        InUse,
        Reserved,
        Maintenance,
        Error,
        Offline
    }

    /// <summary>
    /// Represents a shared resource
    /// </summary>
    public class Resource
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ResourceType Type { get; set; }
        public ResourceStatus Status { get; set; } = ResourceStatus.Available;
        public string CurrentOwner { get; set; } = string.Empty;
        public DateTime? AcquiredAt { get; set; }
        public TimeSpan? MaxHoldTime { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public int MaxConcurrentUsers { get; set; } = 1;
        public int CurrentUsers { get; set; } = 0;
        public bool RequiresExclusiveAccess { get; set; } = true;
    }

    /// <summary>
    /// Resource reservation
    /// </summary>
    public class ResourceReservation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ResourceId { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public bool IsActive => DateTime.Now >= StartTime && DateTime.Now <= EndTime;
    }

    /// <summary>
    /// Resource acquisition result
    /// </summary>
    public class ResourceAcquisitionResult
    {
        public bool Success { get; set; }
        public string ResourceId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime AcquiredAt { get; set; }
        public string AcquisitionToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resource pool for grouping resources
    /// </summary>
    public class ResourcePool
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public List<string> ResourceIds { get; set; } = new();
        public bool AllowAnyFromPool { get; set; } = true;
    }

    /// <summary>
    /// Resource manager for managing shared resources
    /// </summary>
    public class ResourceManager
    {
        private static ResourceManager? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, Resource> _resources = new();
        private readonly Dictionary<string, ResourceReservation> _reservations = new();
        private readonly Dictionary<string, ResourcePool> _pools = new();
        private readonly Dictionary<string, SemaphoreSlim> _resourceLocks = new();
        private readonly List<ResourceAcquisitionResult> _acquisitionHistory = new();

        public static ResourceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ResourceManager();
                    }
                }
                return _instance;
            }
        }

        public event EventHandler<Resource>? ResourceAcquired;
        public event EventHandler<Resource>? ResourceReleased;
        public event EventHandler<Resource>? ResourceStatusChanged;

        /// <summary>
        /// Register a new resource
        /// </summary>
        public void RegisterResource(Resource resource)
        {
            lock (_lock)
            {
                _resources[resource.Id] = resource;
                _resourceLocks[resource.Id] = new SemaphoreSlim(resource.MaxConcurrentUsers, resource.MaxConcurrentUsers);
            }
        }

        /// <summary>
        /// Unregister a resource
        /// </summary>
        public bool UnregisterResource(string resourceId)
        {
            lock (_lock)
            {
                if (_resources.ContainsKey(resourceId))
                {
                    var resource = _resources[resourceId];
                    if (resource.Status == ResourceStatus.InUse)
                    {
                        return false;
                    }
                    _resources.Remove(resourceId);
                    _resourceLocks.Remove(resourceId);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Acquire a resource
        /// </summary>
        public async Task<ResourceAcquisitionResult> AcquireResourceAsync(string resourceId, string ownerId, TimeSpan? timeout = null)
        {
            if (!_resources.TryGetValue(resourceId, out var resource))
            {
                return new ResourceAcquisitionResult
                {
                    Success = false,
                    ResourceId = resourceId,
                    Message = "Resource not found"
                };
            }

            if (resource.Status == ResourceStatus.Maintenance || resource.Status == ResourceStatus.Offline)
            {
                return new ResourceAcquisitionResult
                {
                    Success = false,
                    ResourceId = resourceId,
                    Message = $"Resource is {resource.Status}"
                };
            }

            var semaphore = _resourceLocks[resourceId];
            var acquired = await semaphore.WaitAsync(timeout ?? TimeSpan.FromSeconds(30));

            if (!acquired)
            {
                return new ResourceAcquisitionResult
                {
                    Success = false,
                    ResourceId = resourceId,
                    Message = "Timeout waiting for resource"
                };
            }

            lock (_lock)
            {
                resource.CurrentUsers++;
                resource.CurrentOwner = ownerId;
                resource.AcquiredAt = DateTime.Now;
                resource.Status = ResourceStatus.InUse;
            }

            var result = new ResourceAcquisitionResult
            {
                Success = true,
                ResourceId = resourceId,
                AcquiredAt = DateTime.Now,
                AcquisitionToken = Guid.NewGuid().ToString(),
                Message = "Resource acquired successfully"
            };

            _acquisitionHistory.Add(result);
            ResourceAcquired?.Invoke(this, resource);

            return result;
        }

        /// <summary>
        /// Release a resource
        /// </summary>
        public bool ReleaseResource(string resourceId, string ownerId)
        {
            if (!_resources.TryGetValue(resourceId, out var resource))
            {
                return false;
            }

            lock (_lock)
            {
                if (resource.CurrentOwner != ownerId && resource.RequiresExclusiveAccess)
                {
                    return false;
                }

                resource.CurrentUsers--;
                if (resource.CurrentUsers <= 0)
                {
                    resource.CurrentUsers = 0;
                    resource.CurrentOwner = string.Empty;
                    resource.AcquiredAt = null;
                    resource.Status = ResourceStatus.Available;
                }
            }

            _resourceLocks[resourceId].Release();
            ResourceReleased?.Invoke(this, resource);

            return true;
        }

        /// <summary>
        /// Get all resources
        /// </summary>
        public IReadOnlyList<Resource> GetAllResources()
        {
            lock (_lock)
            {
                return _resources.Values.ToList();
            }
        }

        /// <summary>
        /// Get available resources of a specific type
        /// </summary>
        public IReadOnlyList<Resource> GetAvailableResources(ResourceType type)
        {
            lock (_lock)
            {
                return _resources.Values
                    .Where(r => r.Type == type && r.Status == ResourceStatus.Available)
                    .ToList();
            }
        }

        /// <summary>
        /// Create a resource reservation
        /// </summary>
        public ResourceReservation CreateReservation(string resourceId, string ownerId, DateTime startTime, DateTime endTime, string purpose)
        {
            var reservation = new ResourceReservation
            {
                ResourceId = resourceId,
                OwnerId = ownerId,
                StartTime = startTime,
                EndTime = endTime,
                Purpose = purpose
            };

            lock (_lock)
            {
                _reservations[reservation.Id] = reservation;
            }

            return reservation;
        }

        /// <summary>
        /// Create a resource pool
        /// </summary>
        public ResourcePool CreatePool(string name, IEnumerable<string> resourceIds)
        {
            var pool = new ResourcePool
            {
                Name = name,
                ResourceIds = resourceIds.ToList()
            };

            lock (_lock)
            {
                _pools[pool.Id] = pool;
            }

            return pool;
        }

        /// <summary>
        /// Acquire any available resource from a pool
        /// </summary>
        public async Task<ResourceAcquisitionResult> AcquireFromPoolAsync(string poolId, string ownerId, TimeSpan? timeout = null)
        {
            if (!_pools.TryGetValue(poolId, out var pool))
            {
                return new ResourceAcquisitionResult
                {
                    Success = false,
                    Message = "Pool not found"
                };
            }

            foreach (var resourceId in pool.ResourceIds)
            {
                if (_resources.TryGetValue(resourceId, out var resource) && resource.Status == ResourceStatus.Available)
                {
                    return await AcquireResourceAsync(resourceId, ownerId, timeout);
                }
            }

            return new ResourceAcquisitionResult
            {
                Success = false,
                Message = "No available resources in pool"
            };
        }

        /// <summary>
        /// Set resource status
        /// </summary>
        public void SetResourceStatus(string resourceId, ResourceStatus status)
        {
            if (_resources.TryGetValue(resourceId, out var resource))
            {
                lock (_lock)
                {
                    resource.Status = status;
                }
                ResourceStatusChanged?.Invoke(this, resource);
            }
        }
    }
}
