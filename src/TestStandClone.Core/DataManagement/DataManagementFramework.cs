using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestStandClone.Core.DataManagement
{
    /// <summary>
    /// Type of data source.
    /// </summary>
    public enum DataSourceType
    {
        File,
        Database,
        Memory,
        Remote,
        Custom
    }

    /// <summary>
    /// Data format type.
    /// </summary>
    public enum DataFormat
    {
        Json,
        Xml,
        Csv,
        Binary,
        Custom
    }

    /// <summary>
    /// Represents a data source configuration.
    /// </summary>
    public class DataSourceConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public DataSourceType Type { get; set; } = DataSourceType.File;
        public DataFormat Format { get; set; } = DataFormat.Json;
        public string ConnectionString { get; set; } = string.Empty;
        public Dictionary<string, string> Options { get; set; } = new();
    }

    /// <summary>
    /// Represents a data record.
    /// </summary>
    public class DataRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Category { get; set; } = string.Empty;
        public Dictionary<string, object> Fields { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Query for data records.
    /// </summary>
    public class DataQuery
    {
        public string? Category { get; set; }
        public Dictionary<string, object>? Filters { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; }
    }

    /// <summary>
    /// Interface for data providers.
    /// </summary>
    public interface IDataProvider
    {
        string Name { get; }
        Task<bool> ConnectAsync();
        Task DisconnectAsync();
        bool IsConnected { get; }
        Task<DataRecord?> GetAsync(string id);
        Task<IEnumerable<DataRecord>> QueryAsync(DataQuery query);
        Task<string> SaveAsync(DataRecord record);
        Task<bool> DeleteAsync(string id);
    }

    /// <summary>
    /// In-memory data provider.
    /// </summary>
    public class MemoryDataProvider : IDataProvider
    {
        private readonly Dictionary<string, DataRecord> _records = new();

        public string Name => "Memory";
        public bool IsConnected { get; private set; }

        public Task<bool> ConnectAsync()
        {
            IsConnected = true;
            return Task.FromResult(true);
        }

        public Task DisconnectAsync()
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        public Task<DataRecord?> GetAsync(string id)
        {
            return Task.FromResult(_records.TryGetValue(id, out var record) ? record : null);
        }

        public Task<IEnumerable<DataRecord>> QueryAsync(DataQuery query)
        {
            var results = _records.Values.AsEnumerable();

            if (!string.IsNullOrEmpty(query.Category))
                results = results.Where(r => r.Category == query.Category);

            if (query.Skip.HasValue)
                results = results.Skip(query.Skip.Value);

            if (query.Take.HasValue)
                results = results.Take(query.Take.Value);

            return Task.FromResult(results);
        }

        public Task<string> SaveAsync(DataRecord record)
        {
            if (_records.ContainsKey(record.Id))
            {
                record.ModifiedAt = DateTime.UtcNow;
            }
            _records[record.Id] = record;
            return Task.FromResult(record.Id);
        }

        public Task<bool> DeleteAsync(string id)
        {
            return Task.FromResult(_records.Remove(id));
        }
    }

    /// <summary>
    /// JSON file data provider.
    /// </summary>
    public class JsonFileDataProvider : IDataProvider
    {
        private readonly string _basePath;
        private readonly Dictionary<string, DataRecord> _cache = new();

        public string Name => "JsonFile";
        public bool IsConnected { get; private set; }

        public JsonFileDataProvider(string basePath)
        {
            _basePath = basePath;
        }

        public Task<bool> ConnectAsync()
        {
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
            IsConnected = true;
            return Task.FromResult(true);
        }

        public Task DisconnectAsync()
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        public async Task<DataRecord?> GetAsync(string id)
        {
            if (_cache.TryGetValue(id, out var cached))
                return cached;

            var filePath = Path.Combine(_basePath, $"{id}.json");
            if (!File.Exists(filePath))
                return null;

            var json = await File.ReadAllTextAsync(filePath);
            var record = JsonSerializer.Deserialize<DataRecord>(json);
            if (record != null)
            {
                _cache[id] = record;
            }
            return record;
        }

        public async Task<IEnumerable<DataRecord>> QueryAsync(DataQuery query)
        {
            var results = new List<DataRecord>();
            var files = Directory.GetFiles(_basePath, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var record = JsonSerializer.Deserialize<DataRecord>(json);
                    if (record != null)
                    {
                        if (string.IsNullOrEmpty(query.Category) || record.Category == query.Category)
                        {
                            results.Add(record);
                        }
                    }
                }
                catch
                {
                    // Skip invalid files
                }
            }

            if (query.Skip.HasValue)
                results = results.Skip(query.Skip.Value).ToList();

            if (query.Take.HasValue)
                results = results.Take(query.Take.Value).ToList();

            return results;
        }

        public async Task<string> SaveAsync(DataRecord record)
        {
            var filePath = Path.Combine(_basePath, $"{record.Id}.json");
            var json = JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
            _cache[record.Id] = record;
            return record.Id;
        }

        public Task<bool> DeleteAsync(string id)
        {
            var filePath = Path.Combine(_basePath, $"{id}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _cache.Remove(id);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Manager for data management.
    /// </summary>
    public class DataManagementManager
    {
        private static DataManagementManager? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, IDataProvider> _providers = new();
        private readonly Dictionary<string, DataSourceConfig> _configs = new();

        public static DataManagementManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new DataManagementManager();
                    }
                }
                return _instance;
            }
        }

        private DataManagementManager()
        {
            // Register default memory provider
            RegisterProvider("memory", new MemoryDataProvider());
        }

        /// <summary>
        /// Registers a data provider.
        /// </summary>
        public void RegisterProvider(string name, IDataProvider provider)
        {
            _providers[name] = provider;
        }

        /// <summary>
        /// Gets a data provider by name.
        /// </summary>
        public IDataProvider? GetProvider(string name)
        {
            return _providers.TryGetValue(name, out var provider) ? provider : null;
        }

        /// <summary>
        /// Adds a data source configuration.
        /// </summary>
        public void AddDataSource(DataSourceConfig config)
        {
            _configs[config.Id] = config;
        }

        /// <summary>
        /// Creates a provider from configuration.
        /// </summary>
        public IDataProvider? CreateProviderFromConfig(DataSourceConfig config)
        {
            return config.Type switch
            {
                DataSourceType.Memory => new MemoryDataProvider(),
                DataSourceType.File => new JsonFileDataProvider(config.ConnectionString),
                _ => null
            };
        }

        /// <summary>
        /// Gets all registered providers.
        /// </summary>
        public IEnumerable<string> GetProviderNames()
        {
            return _providers.Keys;
        }

        /// <summary>
        /// Gets all data source configurations.
        /// </summary>
        public IEnumerable<DataSourceConfig> GetDataSources()
        {
            return _configs.Values;
        }
    }
}
