using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestStandClone.Core.DataLinks
{
    /// <summary>
    /// Data link type enumeration
    /// </summary>
    public enum DataLinkType
    {
        SqlServer,
        MySql,
        PostgreSql,
        Oracle,
        SQLite,
        OleDb,
        Odbc,
        Custom
    }

    /// <summary>
    /// Data link state enumeration
    /// </summary>
    public enum DataLinkState
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    /// <summary>
    /// Interface for data link providers
    /// </summary>
    public interface IDataLinkProvider
    {
        /// <summary>
        /// Provider type
        /// </summary>
        DataLinkType Type { get; }

        /// <summary>
        /// Test the connection
        /// </summary>
        Task<bool> TestConnectionAsync(string connectionString);

        /// <summary>
        /// Create a data link
        /// </summary>
        IDataLink CreateDataLink(DataLinkConfiguration config);
    }

    /// <summary>
    /// Interface for data links
    /// </summary>
    public interface IDataLink : IDisposable
    {
        /// <summary>
        /// Data link name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Current state
        /// </summary>
        DataLinkState State { get; }

        /// <summary>
        /// Configuration
        /// </summary>
        DataLinkConfiguration Configuration { get; }

        /// <summary>
        /// Connect to the data source
        /// </summary>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Disconnect from the data source
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Execute a query
        /// </summary>
        Task<DataTable> ExecuteQueryAsync(string query, IDictionary<string, object>? parameters = null);

        /// <summary>
        /// Execute a non-query command
        /// </summary>
        Task<int> ExecuteNonQueryAsync(string command, IDictionary<string, object>? parameters = null);

        /// <summary>
        /// Execute a scalar query
        /// </summary>
        Task<object?> ExecuteScalarAsync(string query, IDictionary<string, object>? parameters = null);

        /// <summary>
        /// Begin a transaction
        /// </summary>
        Task BeginTransactionAsync();

        /// <summary>
        /// Commit the current transaction
        /// </summary>
        Task CommitTransactionAsync();

        /// <summary>
        /// Rollback the current transaction
        /// </summary>
        Task RollbackTransactionAsync();
    }

    /// <summary>
    /// Data link configuration
    /// </summary>
    public class DataLinkConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public DataLinkType Type { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Database { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IntegratedSecurity { get; set; }
        public int ConnectionTimeout { get; set; } = 30;
        public int CommandTimeout { get; set; } = 60;
        public bool PoolConnections { get; set; } = true;
        public int MinPoolSize { get; set; } = 1;
        public int MaxPoolSize { get; set; } = 100;
        public Dictionary<string, string> AdditionalOptions { get; set; } = new();

        /// <summary>
        /// Build the connection string
        /// </summary>
        public string BuildConnectionString()
        {
            if (!string.IsNullOrEmpty(ConnectionString))
            {
                return ConnectionString;
            }

            return Type switch
            {
                DataLinkType.SqlServer => BuildSqlServerConnectionString(),
                DataLinkType.MySql => BuildMySqlConnectionString(),
                DataLinkType.PostgreSql => BuildPostgreSqlConnectionString(),
                DataLinkType.SQLite => BuildSqliteConnectionString(),
                _ => ConnectionString
            };
        }

        private string BuildSqlServerConnectionString()
        {
            var parts = new List<string>
            {
                $"Server={Server}",
                $"Database={Database}",
                $"Connection Timeout={ConnectionTimeout}"
            };

            if (Port > 0)
            {
                parts[0] = $"Server={Server},{Port}";
            }

            if (IntegratedSecurity)
            {
                parts.Add("Integrated Security=True");
            }
            else
            {
                parts.Add($"User Id={Username}");
                parts.Add($"Password={Password}");
            }

            if (PoolConnections)
            {
                parts.Add($"Min Pool Size={MinPoolSize}");
                parts.Add($"Max Pool Size={MaxPoolSize}");
            }
            else
            {
                parts.Add("Pooling=False");
            }

            return string.Join(";", parts);
        }

        private string BuildMySqlConnectionString()
        {
            var parts = new List<string>
            {
                $"Server={Server}",
                $"Database={Database}",
                $"Uid={Username}",
                $"Pwd={Password}",
                $"Connection Timeout={ConnectionTimeout}"
            };

            if (Port > 0)
            {
                parts.Add($"Port={Port}");
            }

            return string.Join(";", parts);
        }

        private string BuildPostgreSqlConnectionString()
        {
            var parts = new List<string>
            {
                $"Host={Server}",
                $"Database={Database}",
                $"Username={Username}",
                $"Password={Password}",
                $"Timeout={ConnectionTimeout}"
            };

            if (Port > 0)
            {
                parts.Add($"Port={Port}");
            }

            return string.Join(";", parts);
        }

        private string BuildSqliteConnectionString()
        {
            return $"Data Source={Database}";
        }
    }

    /// <summary>
    /// Simulated data link for testing
    /// </summary>
    public class SimulatedDataLink : IDataLink
    {
        private DataLinkState _state = DataLinkState.Disconnected;
        private readonly DataTable _simulatedData = new();
        private bool _inTransaction;

        public string Name { get; }
        public DataLinkState State => _state;
        public DataLinkConfiguration Configuration { get; }

        public SimulatedDataLink(DataLinkConfiguration config)
        {
            Name = config.Name;
            Configuration = config;
            InitializeSimulatedData();
        }

        private void InitializeSimulatedData()
        {
            _simulatedData.Columns.Add("Id", typeof(int));
            _simulatedData.Columns.Add("Name", typeof(string));
            _simulatedData.Columns.Add("Value", typeof(double));
            _simulatedData.Columns.Add("Timestamp", typeof(DateTime));

            for (int i = 1; i <= 10; i++)
            {
                _simulatedData.Rows.Add(i, $"Item_{i}", i * 1.5, DateTime.UtcNow.AddMinutes(-i));
            }
        }

        public Task<bool> ConnectAsync()
        {
            _state = DataLinkState.Connected;
            return Task.FromResult(true);
        }

        public Task DisconnectAsync()
        {
            _state = DataLinkState.Disconnected;
            return Task.CompletedTask;
        }

        public Task<DataTable> ExecuteQueryAsync(string query, IDictionary<string, object>? parameters = null)
        {
            if (_state != DataLinkState.Connected)
            {
                throw new InvalidOperationException("Data link is not connected");
            }

            return Task.FromResult(_simulatedData.Copy());
        }

        public Task<int> ExecuteNonQueryAsync(string command, IDictionary<string, object>? parameters = null)
        {
            if (_state != DataLinkState.Connected)
            {
                throw new InvalidOperationException("Data link is not connected");
            }

            return Task.FromResult(1);
        }

        public Task<object?> ExecuteScalarAsync(string query, IDictionary<string, object>? parameters = null)
        {
            if (_state != DataLinkState.Connected)
            {
                throw new InvalidOperationException("Data link is not connected");
            }

            return Task.FromResult<object?>(_simulatedData.Rows.Count);
        }

        public Task BeginTransactionAsync()
        {
            _inTransaction = true;
            return Task.CompletedTask;
        }

        public Task CommitTransactionAsync()
        {
            _inTransaction = false;
            return Task.CompletedTask;
        }

        public Task RollbackTransactionAsync()
        {
            _inTransaction = false;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _state = DataLinkState.Disconnected;
        }
    }

    /// <summary>
    /// Simulated data link provider
    /// </summary>
    public class SimulatedDataLinkProvider : IDataLinkProvider
    {
        public DataLinkType Type => DataLinkType.Custom;

        public Task<bool> TestConnectionAsync(string connectionString)
        {
            return Task.FromResult(true);
        }

        public IDataLink CreateDataLink(DataLinkConfiguration config)
        {
            return new SimulatedDataLink(config);
        }
    }

    /// <summary>
    /// Data link manager singleton
    /// </summary>
    public class DataLinkManager
    {
        private static readonly Lazy<DataLinkManager> _instance = new(() => new DataLinkManager());
        public static DataLinkManager Instance => _instance.Value;

        private readonly Dictionary<string, IDataLink> _dataLinks = new();
        private readonly Dictionary<DataLinkType, IDataLinkProvider> _providers = new();
        private readonly object _lock = new();

        public event EventHandler<DataLinkEventArgs>? DataLinkConnected;
        public event EventHandler<DataLinkEventArgs>? DataLinkDisconnected;
        public event EventHandler<DataLinkErrorEventArgs>? DataLinkError;

        private DataLinkManager()
        {
            // Register default providers
            RegisterProvider(new SimulatedDataLinkProvider());
        }

        /// <summary>
        /// Register a data link provider
        /// </summary>
        public void RegisterProvider(IDataLinkProvider provider)
        {
            lock (_lock)
            {
                _providers[provider.Type] = provider;
            }
        }

        /// <summary>
        /// Create and add a data link
        /// </summary>
        public IDataLink CreateDataLink(DataLinkConfiguration config)
        {
            IDataLinkProvider? provider;
            lock (_lock)
            {
                if (!_providers.TryGetValue(config.Type, out provider))
                {
                    // Use simulated provider as fallback
                    provider = new SimulatedDataLinkProvider();
                }
            }

            var dataLink = provider.CreateDataLink(config);

            lock (_lock)
            {
                _dataLinks[config.Name] = dataLink;
            }

            return dataLink;
        }

        /// <summary>
        /// Get a data link by name
        /// </summary>
        public IDataLink? GetDataLink(string name)
        {
            lock (_lock)
            {
                return _dataLinks.TryGetValue(name, out var dataLink) ? dataLink : null;
            }
        }

        /// <summary>
        /// Get all data links
        /// </summary>
        public IEnumerable<IDataLink> GetAllDataLinks()
        {
            lock (_lock)
            {
                return _dataLinks.Values.ToList();
            }
        }

        /// <summary>
        /// Connect a data link
        /// </summary>
        public async Task<bool> ConnectAsync(string name)
        {
            var dataLink = GetDataLink(name);
            if (dataLink == null) return false;

            try
            {
                var result = await dataLink.ConnectAsync();
                if (result)
                {
                    DataLinkConnected?.Invoke(this, new DataLinkEventArgs { DataLink = dataLink });
                }
                return result;
            }
            catch (Exception ex)
            {
                DataLinkError?.Invoke(this, new DataLinkErrorEventArgs
                {
                    DataLink = dataLink,
                    Error = ex
                });
                return false;
            }
        }

        /// <summary>
        /// Disconnect a data link
        /// </summary>
        public async Task DisconnectAsync(string name)
        {
            var dataLink = GetDataLink(name);
            if (dataLink == null) return;

            try
            {
                await dataLink.DisconnectAsync();
                DataLinkDisconnected?.Invoke(this, new DataLinkEventArgs { DataLink = dataLink });
            }
            catch (Exception ex)
            {
                DataLinkError?.Invoke(this, new DataLinkErrorEventArgs
                {
                    DataLink = dataLink,
                    Error = ex
                });
            }
        }

        /// <summary>
        /// Remove a data link
        /// </summary>
        public async Task RemoveDataLinkAsync(string name)
        {
            IDataLink? dataLink;
            lock (_lock)
            {
                if (!_dataLinks.TryGetValue(name, out dataLink))
                {
                    return;
                }
                _dataLinks.Remove(name);
            }

            if (dataLink.State == DataLinkState.Connected)
            {
                await dataLink.DisconnectAsync();
            }

            dataLink.Dispose();
        }

        /// <summary>
        /// Test a connection
        /// </summary>
        public async Task<bool> TestConnectionAsync(DataLinkConfiguration config)
        {
            IDataLinkProvider? provider;
            lock (_lock)
            {
                if (!_providers.TryGetValue(config.Type, out provider))
                {
                    return false;
                }
            }

            return await provider.TestConnectionAsync(config.BuildConnectionString());
        }

        /// <summary>
        /// Execute a query on a data link
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string dataLinkName, string query, IDictionary<string, object>? parameters = null)
        {
            var dataLink = GetDataLink(dataLinkName);
            if (dataLink == null)
            {
                throw new InvalidOperationException($"Data link '{dataLinkName}' not found");
            }

            if (dataLink.State != DataLinkState.Connected)
            {
                await dataLink.ConnectAsync();
            }

            return await dataLink.ExecuteQueryAsync(query, parameters);
        }

        /// <summary>
        /// Disconnect all data links
        /// </summary>
        public async Task DisconnectAllAsync()
        {
            List<string> names;
            lock (_lock)
            {
                names = _dataLinks.Keys.ToList();
            }

            foreach (var name in names)
            {
                await DisconnectAsync(name);
            }
        }
    }

    /// <summary>
    /// Data link event arguments
    /// </summary>
    public class DataLinkEventArgs : EventArgs
    {
        public IDataLink DataLink { get; set; } = null!;
    }

    /// <summary>
    /// Data link error event arguments
    /// </summary>
    public class DataLinkErrorEventArgs : EventArgs
    {
        public IDataLink? DataLink { get; set; }
        public Exception? Error { get; set; }
    }
}
