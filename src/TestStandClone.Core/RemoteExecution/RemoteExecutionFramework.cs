using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestStandClone.Core.RemoteExecution
{
    /// <summary>
    /// Status of a remote client.
    /// </summary>
    public enum RemoteClientStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Executing,
        Error
    }

    /// <summary>
    /// Type of remote command.
    /// </summary>
    public enum RemoteCommandType
    {
        ExecuteSequence,
        AbortExecution,
        PauseExecution,
        ResumeExecution,
        GetStatus,
        GetResults,
        SendFile,
        ReceiveFile,
        SetVariable,
        GetVariable
    }

    /// <summary>
    /// Represents a remote client connection.
    /// </summary>
    public class RemoteClient
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string HostAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 8080;
        public RemoteClientStatus Status { get; set; } = RemoteClientStatus.Disconnected;
        public DateTime? LastConnectionTime { get; set; }
        public DateTime? LastActivityTime { get; set; }
        public string? CurrentSequence { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// Represents a remote command to be executed.
    /// </summary>
    public class RemoteCommand
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public RemoteCommandType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Result of a remote command execution.
    /// </summary>
    public class RemoteCommandResult
    {
        public string CommandId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public object? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// Status information from a remote client.
    /// </summary>
    public class RemoteStatusInfo
    {
        public string ClientId { get; set; } = string.Empty;
        public RemoteClientStatus Status { get; set; }
        public string? CurrentSequence { get; set; }
        public string? CurrentStep { get; set; }
        public double Progress { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Interface for remote communication protocol.
    /// </summary>
    public interface IRemoteProtocol
    {
        Task<bool> ConnectAsync(string host, int port, CancellationToken cancellationToken = default);
        Task DisconnectAsync();
        bool IsConnected { get; }
        Task<RemoteCommandResult> SendCommandAsync(RemoteCommand command, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Simulated remote protocol for testing.
    /// </summary>
    public class SimulatedRemoteProtocol : IRemoteProtocol
    {
        public bool IsConnected { get; private set; }

        public Task<bool> ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            IsConnected = true;
            return Task.FromResult(true);
        }

        public Task DisconnectAsync()
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        public Task<RemoteCommandResult> SendCommandAsync(RemoteCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RemoteCommandResult
            {
                CommandId = command.Id,
                Success = true,
                Data = command.Type switch
                {
                    RemoteCommandType.GetStatus => new RemoteStatusInfo { Status = RemoteClientStatus.Connected },
                    _ => null
                }
            });
        }
    }

    /// <summary>
    /// Manager for remote execution.
    /// </summary>
    public class RemoteExecutionManager
    {
        private static RemoteExecutionManager? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, RemoteClient> _clients = new();
        private readonly Dictionary<string, IRemoteProtocol> _protocols = new();

        public static RemoteExecutionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new RemoteExecutionManager();
                    }
                }
                return _instance;
            }
        }

        private RemoteExecutionManager() { }

        /// <summary>
        /// Registers a remote client.
        /// </summary>
        public void RegisterClient(RemoteClient client)
        {
            _clients[client.Id] = client;
        }

        /// <summary>
        /// Gets a remote client by ID.
        /// </summary>
        public RemoteClient? GetClient(string clientId)
        {
            return _clients.TryGetValue(clientId, out var client) ? client : null;
        }

        /// <summary>
        /// Gets all registered clients.
        /// </summary>
        public IEnumerable<RemoteClient> GetAllClients()
        {
            return _clients.Values;
        }

        /// <summary>
        /// Removes a remote client.
        /// </summary>
        public bool RemoveClient(string clientId)
        {
            return _clients.Remove(clientId);
        }

        /// <summary>
        /// Connects to a remote client.
        /// </summary>
        public async Task<bool> ConnectAsync(string clientId, CancellationToken cancellationToken = default)
        {
            if (!_clients.TryGetValue(clientId, out var client))
                return false;

            client.Status = RemoteClientStatus.Connecting;

            var protocol = new SimulatedRemoteProtocol();
            var success = await protocol.ConnectAsync(client.HostAddress, client.Port, cancellationToken);

            if (success)
            {
                _protocols[clientId] = protocol;
                client.Status = RemoteClientStatus.Connected;
                client.LastConnectionTime = DateTime.UtcNow;
            }
            else
            {
                client.Status = RemoteClientStatus.Error;
            }

            return success;
        }

        /// <summary>
        /// Disconnects from a remote client.
        /// </summary>
        public async Task DisconnectAsync(string clientId)
        {
            if (_protocols.TryGetValue(clientId, out var protocol))
            {
                await protocol.DisconnectAsync();
                _protocols.Remove(clientId);
            }

            if (_clients.TryGetValue(clientId, out var client))
            {
                client.Status = RemoteClientStatus.Disconnected;
            }
        }

        /// <summary>
        /// Sends a command to a remote client.
        /// </summary>
        public async Task<RemoteCommandResult> SendCommandAsync(string clientId, RemoteCommand command, CancellationToken cancellationToken = default)
        {
            if (!_protocols.TryGetValue(clientId, out var protocol))
            {
                return new RemoteCommandResult
                {
                    CommandId = command.Id,
                    Success = false,
                    ErrorMessage = "Client not connected"
                };
            }

            if (_clients.TryGetValue(clientId, out var client))
            {
                client.LastActivityTime = DateTime.UtcNow;
            }

            return await protocol.SendCommandAsync(command, cancellationToken);
        }

        /// <summary>
        /// Executes a sequence on a remote client.
        /// </summary>
        public async Task<RemoteCommandResult> ExecuteSequenceRemotelyAsync(string clientId, string sequencePath, CancellationToken cancellationToken = default)
        {
            var command = new RemoteCommand
            {
                Type = RemoteCommandType.ExecuteSequence,
                Parameters = { ["SequencePath"] = sequencePath }
            };

            return await SendCommandAsync(clientId, command, cancellationToken);
        }

        /// <summary>
        /// Gets the status of a remote client.
        /// </summary>
        public async Task<RemoteStatusInfo?> GetRemoteStatusAsync(string clientId, CancellationToken cancellationToken = default)
        {
            var command = new RemoteCommand { Type = RemoteCommandType.GetStatus };
            var result = await SendCommandAsync(clientId, command, cancellationToken);

            return result.Success ? result.Data as RemoteStatusInfo : null;
        }
    }
}
