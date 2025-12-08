using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestStandClone.Core.MESIntegration
{
    /// <summary>
    /// MES message type
    /// </summary>
    public enum MESMessageType
    {
        StartOperation,
        EndOperation,
        DefectReport,
        MaterialConsumption,
        LotStart,
        LotEnd,
        StatusUpdate,
        DataCollection,
        Custom
    }

    /// <summary>
    /// MES connection status
    /// </summary>
    public enum MESConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error,
        Offline
    }

    /// <summary>
    /// MES operation result
    /// </summary>
    public enum MESOperationResult
    {
        Success,
        Failure,
        Warning,
        Pending,
        Timeout
    }

    /// <summary>
    /// MES configuration
    /// </summary>
    public class MESConfiguration
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string ServerUrl { get; set; } = string.Empty;
        public int Port { get; set; } = 8080;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
        public bool AutoReconnect { get; set; } = true;
        public Dictionary<string, string> CustomSettings { get; set; } = new Dictionary<string, string>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// MES message
    /// </summary>
    public class MESMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public MESMessageType Type { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string OperationCode { get; set; } = string.Empty;
        public string WorkOrderId { get; set; } = string.Empty;
        public string StationId { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// MES response
    /// </summary>
    public class MESResponse
    {
        public string MessageId { get; set; } = string.Empty;
        public MESOperationResult Result { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string ResultMessage { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Work order information
    /// </summary>
    public class WorkOrder
    {
        public string Id { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int TargetQuantity { get; set; }
        public int CompletedQuantity { get; set; }
        public int PassedQuantity { get; set; }
        public int FailedQuantity { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// MES connection interface
    /// </summary>
    public interface IMESConnection
    {
        MESConnectionStatus Status { get; }
        Task<bool> ConnectAsync(MESConfiguration config);
        Task DisconnectAsync();
        Task<MESResponse> SendMessageAsync(MESMessage message);
        Task<WorkOrder?> GetWorkOrderAsync(string workOrderId);
        Task<bool> ValidateSerialNumberAsync(string serialNumber, string workOrderId);
    }

    /// <summary>
    /// Simulated MES connection for testing
    /// </summary>
    public class SimulatedMESConnection : IMESConnection
    {
        public MESConnectionStatus Status { get; private set; } = MESConnectionStatus.Disconnected;
        private readonly Dictionary<string, WorkOrder> _workOrders = new Dictionary<string, WorkOrder>();

        public Task<bool> ConnectAsync(MESConfiguration config)
        {
            Status = MESConnectionStatus.Connected;
            var wo = new WorkOrder
            {
                Id = "WO-001",
                ProductCode = "PROD-001",
                ProductName = "Test Product",
                TargetQuantity = 100,
                CompletedQuantity = 0,
                StartTime = DateTime.Now,
                Status = "Active"
            };
            _workOrders[wo.Id] = wo;
            return Task.FromResult(true);
        }

        public Task DisconnectAsync()
        {
            Status = MESConnectionStatus.Disconnected;
            return Task.CompletedTask;
        }

        public Task<MESResponse> SendMessageAsync(MESMessage message)
        {
            var response = new MESResponse
            {
                MessageId = message.Id,
                Result = MESOperationResult.Success,
                ResultCode = "OK",
                ResultMessage = "Operation completed successfully"
            };
            return Task.FromResult(response);
        }

        public Task<WorkOrder?> GetWorkOrderAsync(string workOrderId)
        {
            _workOrders.TryGetValue(workOrderId, out var wo);
            return Task.FromResult(wo);
        }

        public Task<bool> ValidateSerialNumberAsync(string serialNumber, string workOrderId)
        {
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// MES integration manager singleton
    /// </summary>
    public class MESIntegrationManager
    {
        private static readonly Lazy<MESIntegrationManager> _instance = new Lazy<MESIntegrationManager>(() => new MESIntegrationManager());
        public static MESIntegrationManager Instance => _instance.Value;

        private readonly Dictionary<string, MESConfiguration> _configurations = new Dictionary<string, MESConfiguration>();
        private readonly Dictionary<string, IMESConnection> _connections = new Dictionary<string, IMESConnection>();
        private readonly List<MESMessage> _messageHistory = new List<MESMessage>();
        private string? _activeConfigId;

        public event EventHandler<MESConnectionStatus>? ConnectionStatusChanged;
        public event EventHandler<MESMessage>? MessageSent;
        public event EventHandler<MESResponse>? ResponseReceived;

        private MESIntegrationManager() { }

        public void AddConfiguration(MESConfiguration config) => _configurations[config.Id] = config;
        public MESConfiguration? GetConfiguration(string id) { _configurations.TryGetValue(id, out var config); return config; }
        public IEnumerable<MESConfiguration> GetAllConfigurations() => _configurations.Values.ToList();

        public async Task<bool> ConnectAsync(string configId)
        {
            if (!_configurations.TryGetValue(configId, out var config)) return false;
            var connection = new SimulatedMESConnection();
            var result = await connection.ConnectAsync(config);
            if (result) { _connections[configId] = connection; _activeConfigId = configId; ConnectionStatusChanged?.Invoke(this, connection.Status); }
            return result;
        }

        public async Task DisconnectAsync(string configId)
        {
            if (_connections.TryGetValue(configId, out var connection))
            {
                await connection.DisconnectAsync();
                _connections.Remove(configId);
                if (_activeConfigId == configId) _activeConfigId = null;
                ConnectionStatusChanged?.Invoke(this, MESConnectionStatus.Disconnected);
            }
        }

        public async Task<MESResponse?> SendMessageAsync(MESMessage message)
        {
            if (_activeConfigId == null || !_connections.TryGetValue(_activeConfigId, out var connection)) return null;
            _messageHistory.Add(message);
            MessageSent?.Invoke(this, message);
            var response = await connection.SendMessageAsync(message);
            ResponseReceived?.Invoke(this, response);
            return response;
        }

        public async Task<MESResponse?> ReportStartOperationAsync(string serialNumber, string operationCode, string workOrderId)
        {
            return await SendMessageAsync(new MESMessage { Type = MESMessageType.StartOperation, SerialNumber = serialNumber, OperationCode = operationCode, WorkOrderId = workOrderId });
        }

        public async Task<MESResponse?> ReportEndOperationAsync(string serialNumber, string operationCode, bool passed, Dictionary<string, object>? testData = null)
        {
            var message = new MESMessage { Type = MESMessageType.EndOperation, SerialNumber = serialNumber, OperationCode = operationCode, Data = testData ?? new Dictionary<string, object>() };
            message.Data["Passed"] = passed;
            return await SendMessageAsync(message);
        }

        public async Task<WorkOrder?> GetWorkOrderAsync(string workOrderId)
        {
            if (_activeConfigId == null || !_connections.TryGetValue(_activeConfigId, out var connection)) return null;
            return await connection.GetWorkOrderAsync(workOrderId);
        }

        public IEnumerable<MESMessage> GetMessageHistory(int count = 100) => _messageHistory.TakeLast(count).ToList();
        public void ClearMessageHistory() => _messageHistory.Clear();
    }
}
