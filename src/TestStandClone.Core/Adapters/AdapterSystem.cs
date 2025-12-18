// AdapterSystem.cs - Instrument and hardware adapter support
// Provides abstraction layer for instrument communication

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TestStandClone.Core.Adapters
{
    /// <summary>
    /// Represents the type of adapter
    /// </summary>
    public enum AdapterType
    {
        /// <summary>VISA adapter for SCPI instruments</summary>
        VISA,
        /// <summary>Serial port adapter</summary>
        Serial,
        /// <summary>TCP/IP socket adapter</summary>
        TCPIP,
        /// <summary>USB device adapter</summary>
        USB,
        /// <summary>GPIB adapter</summary>
        GPIB,
        /// <summary>DAQ hardware adapter</summary>
        DAQ,
        /// <summary>PXI module adapter</summary>
        PXI,
        /// <summary>Custom adapter type</summary>
        Custom
    }

    /// <summary>
    /// Represents the connection state of an adapter
    /// </summary>
    public enum AdapterConnectionState
    {
        /// <summary>Adapter is disconnected</summary>
        Disconnected,
        /// <summary>Adapter is connecting</summary>
        Connecting,
        /// <summary>Adapter is connected</summary>
        Connected,
        /// <summary>Adapter connection error</summary>
        Error
    }

    /// <summary>
    /// Interface for instrument adapters
    /// </summary>
    public interface IAdapter : IDisposable
    {
        /// <summary>Unique identifier for the adapter</summary>
        string Id { get; }
        
        /// <summary>Display name of the adapter</summary>
        string Name { get; set; }
        
        /// <summary>Type of adapter</summary>
        AdapterType Type { get; }
        
        /// <summary>Connection state</summary>
        AdapterConnectionState State { get; }
        
        /// <summary>Connection string or resource name</summary>
        string ResourceName { get; set; }
        
        /// <summary>Timeout in milliseconds</summary>
        int Timeout { get; set; }
        
        /// <summary>Connect to the instrument</summary>
        Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
        
        /// <summary>Disconnect from the instrument</summary>
        Task DisconnectAsync();
        
        /// <summary>Write data to the instrument</summary>
        Task WriteAsync(string data, CancellationToken cancellationToken = default);
        
        /// <summary>Read data from the instrument</summary>
        Task<string> ReadAsync(CancellationToken cancellationToken = default);
        
        /// <summary>Write and read data (query)</summary>
        Task<string> QueryAsync(string command, CancellationToken cancellationToken = default);
        
        /// <summary>Event raised when state changes</summary>
        event EventHandler<AdapterConnectionState>? StateChanged;
    }

    /// <summary>
    /// Base implementation of IAdapter
    /// </summary>
    public abstract class AdapterBase : IAdapter
    {
        public string Id { get; }
        public string Name { get; set; }
        public abstract AdapterType Type { get; }
        public AdapterConnectionState State { get; protected set; } = AdapterConnectionState.Disconnected;
        public string ResourceName { get; set; } = string.Empty;
        public int Timeout { get; set; } = 5000;
        
        public event EventHandler<AdapterConnectionState>? StateChanged;

        protected AdapterBase(string name)
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
        }

        protected void SetState(AdapterConnectionState newState)
        {
            if (State != newState)
            {
                State = newState;
                StateChanged?.Invoke(this, newState);
            }
        }

        public abstract Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
        public abstract Task DisconnectAsync();
        public abstract Task WriteAsync(string data, CancellationToken cancellationToken = default);
        public abstract Task<string> ReadAsync(CancellationToken cancellationToken = default);
        
        public virtual async Task<string> QueryAsync(string command, CancellationToken cancellationToken = default)
        {
            await WriteAsync(command, cancellationToken);
            await Task.Delay(10, cancellationToken); // Small delay for instrument processing
            return await ReadAsync(cancellationToken);
        }

        public abstract void Dispose();
    }

    /// <summary>
    /// Simulated adapter for testing
    /// </summary>
    public class SimulatedAdapter : AdapterBase
    {
        private readonly Dictionary<string, string> _responses = new();
        private string _lastCommand = string.Empty;

        public override AdapterType Type => AdapterType.Custom;

        public SimulatedAdapter(string name) : base(name)
        {
            // Default responses
            _responses["*IDN?"] = "Simulated Instrument, Model 1000, Serial 12345";
            _responses["*RST"] = string.Empty;
            _responses["*OPC?"] = "1";
        }

        /// <summary>
        /// Add a simulated response for a command
        /// </summary>
        public void AddResponse(string command, string response)
        {
            _responses[command.Trim()] = response;
        }

        public override Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            SetState(AdapterConnectionState.Connecting);
            SetState(AdapterConnectionState.Connected);
            return Task.FromResult(true);
        }

        public override Task DisconnectAsync()
        {
            SetState(AdapterConnectionState.Disconnected);
            return Task.CompletedTask;
        }

        public override Task WriteAsync(string data, CancellationToken cancellationToken = default)
        {
            _lastCommand = data.Trim();
            return Task.CompletedTask;
        }

        public override Task<string> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (_responses.TryGetValue(_lastCommand, out var response))
            {
                return Task.FromResult(response);
            }
            
            // Default: echo command
            return Task.FromResult($"Response to: {_lastCommand}");
        }

        public override void Dispose()
        {
            SetState(AdapterConnectionState.Disconnected);
        }
    }

    /// <summary>
    /// TCP/IP socket adapter
    /// </summary>
    public class TcpIpAdapter : AdapterBase
    {
        private System.Net.Sockets.TcpClient? _client;
        private System.Net.Sockets.NetworkStream? _stream;
        private readonly byte[] _readBuffer = new byte[4096];

        public override AdapterType Type => AdapterType.TCPIP;

        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5025;

        public TcpIpAdapter(string name) : base(name) { }

        public override async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                SetState(AdapterConnectionState.Connecting);
                _client = new System.Net.Sockets.TcpClient();
                
                // Parse resource name if provided
                if (!string.IsNullOrEmpty(ResourceName))
                {
                    var parts = ResourceName.Split(':');
                    Host = parts[0];
                    if (parts.Length > 1 && int.TryParse(parts[1], out var port))
                    {
                        Port = port;
                    }
                }

                await _client.ConnectAsync(Host, Port, cancellationToken);
                _stream = _client.GetStream();
                _stream.ReadTimeout = Timeout;
                _stream.WriteTimeout = Timeout;
                
                SetState(AdapterConnectionState.Connected);
                return true;
            }
            catch (Exception)
            {
                SetState(AdapterConnectionState.Error);
                return false;
            }
        }

        public override Task DisconnectAsync()
        {
            _stream?.Close();
            _client?.Close();
            _stream = null;
            _client = null;
            SetState(AdapterConnectionState.Disconnected);
            return Task.CompletedTask;
        }

        public override async Task WriteAsync(string data, CancellationToken cancellationToken = default)
        {
            if (_stream == null)
                throw new InvalidOperationException("Adapter not connected");

            var bytes = System.Text.Encoding.ASCII.GetBytes(data + "\n");
            await _stream.WriteAsync(bytes, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }

        public override async Task<string> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (_stream == null)
                throw new InvalidOperationException("Adapter not connected");

            var bytesRead = await _stream.ReadAsync(_readBuffer, cancellationToken);
            var response = System.Text.Encoding.ASCII.GetString(_readBuffer, 0, bytesRead);
            return response.TrimEnd('\n', '\r');
        }

        public override void Dispose()
        {
            DisconnectAsync().Wait();
        }
    }

    /// <summary>
    /// Adapter configuration
    /// </summary>
    public class AdapterConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AdapterType Type { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public int Timeout { get; set; } = 5000;
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// Factory for creating adapters
    /// </summary>
    public class AdapterFactory
    {
        private readonly Dictionary<AdapterType, Func<string, IAdapter>> _factories = new();

        public AdapterFactory()
        {
            // Register default factories
            _factories[AdapterType.Custom] = name => new SimulatedAdapter(name);
            _factories[AdapterType.TCPIP] = name => new TcpIpAdapter(name);
        }

        /// <summary>
        /// Register a custom adapter factory
        /// </summary>
        public void RegisterFactory(AdapterType type, Func<string, IAdapter> factory)
        {
            _factories[type] = factory;
        }

        /// <summary>
        /// Create an adapter from configuration
        /// </summary>
        public IAdapter CreateAdapter(AdapterConfiguration config)
        {
            if (!_factories.TryGetValue(config.Type, out var factory))
            {
                throw new ArgumentException($"No factory registered for adapter type: {config.Type}");
            }

            var adapter = factory(config.Name);
            adapter.ResourceName = config.ResourceName;
            adapter.Timeout = config.Timeout;
            
            return adapter;
        }
    }

    /// <summary>
    /// Manages all adapters in the system
    /// </summary>
    public class AdapterManager
    {
        private static AdapterManager? _instance;
        private static readonly object _lock = new();
        
        private readonly ConcurrentDictionary<string, IAdapter> _adapters = new();
        private readonly AdapterFactory _factory = new();

        public static AdapterManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new AdapterManager();
                    }
                }
                return _instance;
            }
        }

        private AdapterManager() { }

        /// <summary>
        /// Get the adapter factory
        /// </summary>
        public AdapterFactory Factory => _factory;

        /// <summary>
        /// Add an adapter
        /// </summary>
        public void AddAdapter(IAdapter adapter)
        {
            _adapters[adapter.Id] = adapter;
        }

        /// <summary>
        /// Get an adapter by ID
        /// </summary>
        public IAdapter? GetAdapter(string id)
        {
            _adapters.TryGetValue(id, out var adapter);
            return adapter;
        }

        /// <summary>
        /// Get an adapter by name
        /// </summary>
        public IAdapter? GetAdapterByName(string name)
        {
            return _adapters.Values.FirstOrDefault(a => a.Name == name);
        }

        /// <summary>
        /// Get all adapters
        /// </summary>
        public IEnumerable<IAdapter> GetAllAdapters()
        {
            return _adapters.Values;
        }

        /// <summary>
        /// Remove an adapter
        /// </summary>
        public bool RemoveAdapter(string id)
        {
            if (_adapters.TryRemove(id, out var adapter))
            {
                adapter.Dispose();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Connect all adapters
        /// </summary>
        public async Task ConnectAllAsync(CancellationToken cancellationToken = default)
        {
            var tasks = _adapters.Values
                .Where(a => a.State != AdapterConnectionState.Connected)
                .Select(a => a.ConnectAsync(cancellationToken));
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Disconnect all adapters
        /// </summary>
        public async Task DisconnectAllAsync()
        {
            var tasks = _adapters.Values
                .Where(a => a.State == AdapterConnectionState.Connected)
                .Select(a => a.DisconnectAsync());
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Clear all adapters
        /// </summary>
        public void Clear()
        {
            foreach (var adapter in _adapters.Values)
            {
                adapter.Dispose();
            }
            _adapters.Clear();
        }
    }
}
