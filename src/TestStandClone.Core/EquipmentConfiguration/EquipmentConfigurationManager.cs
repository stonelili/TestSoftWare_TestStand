using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestStandClone.Core.EquipmentConfiguration
{
    /// <summary>
    /// Equipment type enumeration
    /// </summary>
    public enum EquipmentType
    {
        Instrument,
        Fixture,
        Controller,
        PowerSupply,
        DAQ,
        Switch,
        Communication,
        Custom
    }

    /// <summary>
    /// Equipment status
    /// </summary>
    public enum EquipmentStatus
    {
        Online,
        Offline,
        Error,
        Maintenance,
        Calibrating,
        Unknown
    }

    /// <summary>
    /// Connection type
    /// </summary>
    public enum EquipmentConnectionType
    {
        GPIB,
        USB,
        Ethernet,
        Serial,
        PXI,
        VXI,
        Simulated
    }

    /// <summary>
    /// Represents an equipment configuration
    /// </summary>
    public class EquipmentConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public EquipmentType Type { get; set; }
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public EquipmentConnectionType ConnectionType { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        public int Timeout { get; set; } = 10000;
        public bool AutoConnect { get; set; }
        public bool SimulationMode { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
        public Dictionary<string, string> Settings { get; set; } = new();
        public List<string> Channels { get; set; } = new();
        public DateTime? LastCalibrationDate { get; set; }
        public DateTime? NextCalibrationDate { get; set; }
    }

    /// <summary>
    /// Equipment instance with runtime state
    /// </summary>
    public class EquipmentInstance
    {
        public EquipmentConfig Config { get; set; } = new();
        public EquipmentStatus Status { get; set; } = EquipmentStatus.Offline;
        public bool IsConnected { get; set; }
        public string? IdentityString { get; set; }
        public DateTime? LastAccessTime { get; set; }
        public string LastError { get; set; } = string.Empty;
        public Dictionary<string, object> RuntimeState { get; set; } = new();
    }

    /// <summary>
    /// Equipment configuration set
    /// </summary>
    public class EquipmentConfigurationSet
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
        public string Version { get; set; } = "1.0";
        public List<EquipmentConfig> Equipment { get; set; } = new();
        public Dictionary<string, string> GlobalSettings { get; set; } = new();
    }

    /// <summary>
    /// Equipment configuration validation result
    /// </summary>
    public class ConfigValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Equipment configuration manager
    /// </summary>
    public class EquipmentConfigurationManager
    {
        private static EquipmentConfigurationManager? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, EquipmentConfig> _configurations = new();
        private readonly Dictionary<string, EquipmentInstance> _instances = new();
        private readonly Dictionary<string, EquipmentConfigurationSet> _configSets = new();
        private string _activeConfigSetId = string.Empty;
        private string _storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TestStandClone", "Equipment");

        public static EquipmentConfigurationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new EquipmentConfigurationManager();
                    }
                }
                return _instance;
            }
        }

        public event EventHandler<EquipmentInstance>? EquipmentStatusChanged;
        public event EventHandler<EquipmentConfig>? EquipmentConfigChanged;

        public EquipmentConfigurationManager()
        {
            Directory.CreateDirectory(_storagePath);
        }

        /// <summary>
        /// Add equipment configuration
        /// </summary>
        public void AddEquipment(EquipmentConfig config)
        {
            lock (_lock)
            {
                _configurations[config.Id] = config;
                _instances[config.Id] = new EquipmentInstance { Config = config };
            }
        }

        /// <summary>
        /// Remove equipment configuration
        /// </summary>
        public bool RemoveEquipment(string equipmentId)
        {
            lock (_lock)
            {
                _configurations.Remove(equipmentId);
                return _instances.Remove(equipmentId);
            }
        }

        /// <summary>
        /// Get equipment configuration by ID
        /// </summary>
        public EquipmentConfig? GetEquipment(string equipmentId)
        {
            _configurations.TryGetValue(equipmentId, out var config);
            return config;
        }

        /// <summary>
        /// Get equipment by alias
        /// </summary>
        public EquipmentConfig? GetEquipmentByAlias(string alias)
        {
            lock (_lock)
            {
                return _configurations.Values.FirstOrDefault(c => c.Alias == alias);
            }
        }

        /// <summary>
        /// Get all equipment configurations
        /// </summary>
        public IReadOnlyList<EquipmentConfig> GetAllEquipment()
        {
            lock (_lock)
            {
                return _configurations.Values.ToList();
            }
        }

        /// <summary>
        /// Get equipment by type
        /// </summary>
        public IReadOnlyList<EquipmentConfig> GetEquipmentByType(EquipmentType type)
        {
            lock (_lock)
            {
                return _configurations.Values.Where(c => c.Type == type).ToList();
            }
        }

        /// <summary>
        /// Get equipment instance
        /// </summary>
        public EquipmentInstance? GetInstance(string equipmentId)
        {
            _instances.TryGetValue(equipmentId, out var instance);
            return instance;
        }

        /// <summary>
        /// Update equipment status
        /// </summary>
        public void UpdateStatus(string equipmentId, EquipmentStatus status, bool isConnected = false, string? error = null)
        {
            if (_instances.TryGetValue(equipmentId, out var instance))
            {
                instance.Status = status;
                instance.IsConnected = isConnected;
                instance.LastAccessTime = DateTime.Now;
                if (error != null)
                {
                    instance.LastError = error;
                }
                EquipmentStatusChanged?.Invoke(this, instance);
            }
        }

        /// <summary>
        /// Update equipment configuration
        /// </summary>
        public void UpdateEquipment(EquipmentConfig config)
        {
            lock (_lock)
            {
                _configurations[config.Id] = config;
                if (_instances.TryGetValue(config.Id, out var instance))
                {
                    instance.Config = config;
                }
            }
            EquipmentConfigChanged?.Invoke(this, config);
        }

        /// <summary>
        /// Validate equipment configuration
        /// </summary>
        public ConfigValidationResult ValidateConfiguration(EquipmentConfig config)
        {
            var result = new ConfigValidationResult { IsValid = true };

            if (string.IsNullOrEmpty(config.Name))
            {
                result.Errors.Add("Equipment name is required");
                result.IsValid = false;
            }

            if (string.IsNullOrEmpty(config.ConnectionString) && !config.SimulationMode)
            {
                result.Errors.Add("Connection string is required for non-simulated equipment");
                result.IsValid = false;
            }

            if (config.Timeout <= 0)
            {
                result.Warnings.Add("Timeout should be a positive value");
            }

            if (config.NextCalibrationDate.HasValue && config.NextCalibrationDate.Value < DateTime.Now)
            {
                result.Warnings.Add("Equipment is due for calibration");
            }

            lock (_lock)
            {
                var duplicate = _configurations.Values.FirstOrDefault(c => 
                    c.Id != config.Id && 
                    (c.Alias == config.Alias || c.ConnectionString == config.ConnectionString));
                
                if (duplicate != null)
                {
                    if (!string.IsNullOrEmpty(config.Alias) && duplicate.Alias == config.Alias)
                    {
                        result.Errors.Add($"Alias '{config.Alias}' is already in use by '{duplicate.Name}'");
                        result.IsValid = false;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Create a configuration set
        /// </summary>
        public EquipmentConfigurationSet CreateConfigurationSet(string name, string description = "")
        {
            var configSet = new EquipmentConfigurationSet
            {
                Name = name,
                Description = description,
                Equipment = GetAllEquipment().ToList()
            };

            lock (_lock)
            {
                _configSets[configSet.Id] = configSet;
            }

            return configSet;
        }

        /// <summary>
        /// Load a configuration set
        /// </summary>
        public void LoadConfigurationSet(string configSetId)
        {
            if (!_configSets.TryGetValue(configSetId, out var configSet))
            {
                throw new KeyNotFoundException($"Configuration set {configSetId} not found");
            }

            lock (_lock)
            {
                _configurations.Clear();
                _instances.Clear();

                foreach (var config in configSet.Equipment)
                {
                    _configurations[config.Id] = config;
                    _instances[config.Id] = new EquipmentInstance { Config = config };
                }

                _activeConfigSetId = configSetId;
            }
        }

        /// <summary>
        /// Get all configuration sets
        /// </summary>
        public IReadOnlyList<EquipmentConfigurationSet> GetAllConfigurationSets()
        {
            lock (_lock)
            {
                return _configSets.Values.ToList();
            }
        }

        /// <summary>
        /// Save equipment configurations to file
        /// </summary>
        public async Task SaveAsync(string? fileName = null)
        {
            var configSet = new EquipmentConfigurationSet
            {
                Name = "Default",
                Equipment = GetAllEquipment().ToList()
            };

            var filePath = Path.Combine(_storagePath, fileName ?? "equipment_config.json");
            var json = JsonSerializer.Serialize(configSet, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Load equipment configurations from file
        /// </summary>
        public async Task LoadAsync(string? fileName = null)
        {
            var filePath = Path.Combine(_storagePath, fileName ?? "equipment_config.json");
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                var configSet = JsonSerializer.Deserialize<EquipmentConfigurationSet>(json);
                if (configSet != null)
                {
                    lock (_lock)
                    {
                        _configurations.Clear();
                        _instances.Clear();

                        foreach (var config in configSet.Equipment)
                        {
                            _configurations[config.Id] = config;
                            _instances[config.Id] = new EquipmentInstance { Config = config };
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Export configuration to file
        /// </summary>
        public async Task ExportAsync(string filePath, IEnumerable<string>? equipmentIds = null)
        {
            List<EquipmentConfig> equipment;
            lock (_lock)
            {
                equipment = equipmentIds != null
                    ? _configurations.Values.Where(c => equipmentIds.Contains(c.Id)).ToList()
                    : _configurations.Values.ToList();
            }

            var configSet = new EquipmentConfigurationSet
            {
                Name = "Export",
                Equipment = equipment
            };

            var json = JsonSerializer.Serialize(configSet, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Import configuration from file
        /// </summary>
        public async Task ImportAsync(string filePath, bool replaceExisting = false)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Configuration file not found", filePath);
            }

            var json = await File.ReadAllTextAsync(filePath);
            var configSet = JsonSerializer.Deserialize<EquipmentConfigurationSet>(json);
            
            if (configSet != null)
            {
                lock (_lock)
                {
                    foreach (var config in configSet.Equipment)
                    {
                        if (replaceExisting || !_configurations.ContainsKey(config.Id))
                        {
                            _configurations[config.Id] = config;
                            _instances[config.Id] = new EquipmentInstance { Config = config };
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check equipment requiring calibration
        /// </summary>
        public IReadOnlyList<EquipmentConfig> GetEquipmentNeedingCalibration(int warningDays = 30)
        {
            var warningDate = DateTime.Now.AddDays(warningDays);
            lock (_lock)
            {
                return _configurations.Values
                    .Where(c => c.NextCalibrationDate.HasValue && c.NextCalibrationDate.Value <= warningDate)
                    .OrderBy(c => c.NextCalibrationDate)
                    .ToList();
            }
        }
    }
}
