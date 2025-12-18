using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TestStandClone.Core.ProductConfiguration
{
    public enum ProductConfigStatus { Draft, Active, Inactive, Obsolete }
    public enum ProductVariantType { Standard, Custom, OEM, Special }

    public class ProductOption
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string OptionCode { get; set; } = string.Empty;
        public List<string> PossibleValues { get; set; } = new List<string>();
        public string DefaultValue { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public Dictionary<string, string> TestParameters { get; set; } = new Dictionary<string, string>();
    }

    public class ProductConfiguration
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProductFamily { get; set; } = string.Empty;
        public ProductConfigStatus Status { get; set; } = ProductConfigStatus.Draft;
        public ProductVariantType VariantType { get; set; } = ProductVariantType.Standard;
        public string Version { get; set; } = "1.0";
        public List<ProductOption> Options { get; set; } = new List<ProductOption>();
        public string DefaultTestProgramId { get; set; } = string.Empty;
        public List<string> CompatibleTestStations { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
    }

    public class ProductInstance
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ConfigurationId { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public Dictionary<string, string> OptionValues { get; set; } = new Dictionary<string, string>();
        public string WorkOrderId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class ProductFamily
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> ProductCodes { get; set; } = new List<string>();
    }

    public class ProductConfigurationManager
    {
        private static readonly Lazy<ProductConfigurationManager> _instance = new Lazy<ProductConfigurationManager>(() => new ProductConfigurationManager());
        public static ProductConfigurationManager Instance => _instance.Value;

        private readonly Dictionary<string, ProductConfiguration> _configurations = new Dictionary<string, ProductConfiguration>();
        private readonly Dictionary<string, ProductFamily> _families = new Dictionary<string, ProductFamily>();
        private readonly Dictionary<string, ProductInstance> _instances = new Dictionary<string, ProductInstance>();
        private string _storageDirectory = "./ProductConfigurations";

        public event EventHandler<ProductConfiguration>? ConfigurationCreated;
        public event EventHandler<ProductInstance>? InstanceCreated;

        private ProductConfigurationManager() { }

        public void SetStorageDirectory(string dir) { _storageDirectory = dir; if (!Directory.Exists(dir)) Directory.CreateDirectory(dir); }

        public ProductConfiguration CreateConfiguration(string productCode, string productName, string family)
        {
            var config = new ProductConfiguration { ProductCode = productCode, ProductName = productName, ProductFamily = family };
            _configurations[config.Id] = config;
            ConfigurationCreated?.Invoke(this, config);
            return config;
        }

        public void UpdateConfiguration(ProductConfiguration config) { config.ModifiedAt = DateTime.Now; _configurations[config.Id] = config; }
        public ProductConfiguration? GetConfiguration(string id) { _configurations.TryGetValue(id, out var c); return c; }
        public ProductConfiguration? GetConfigurationByProductCode(string productCode) => _configurations.Values.FirstOrDefault(c => c.ProductCode == productCode && c.Status == ProductConfigStatus.Active);
        public IEnumerable<ProductConfiguration> GetAllConfigurations() => _configurations.Values.ToList();
        public IEnumerable<ProductConfiguration> GetActiveConfigurations() => _configurations.Values.Where(c => c.Status == ProductConfigStatus.Active).ToList();
        public void ActivateConfiguration(string id) { if (_configurations.TryGetValue(id, out var c)) { c.Status = ProductConfigStatus.Active; c.ModifiedAt = DateTime.Now; } }
        public void DeactivateConfiguration(string id) { if (_configurations.TryGetValue(id, out var c)) { c.Status = ProductConfigStatus.Inactive; c.ModifiedAt = DateTime.Now; } }
        public void AddOptionToConfiguration(string configId, ProductOption option) { if (_configurations.TryGetValue(configId, out var c)) { c.Options.Add(option); c.ModifiedAt = DateTime.Now; } }

        public ProductInstance CreateInstance(string configId, string serialNumber, Dictionary<string, string> optionValues)
        {
            var instance = new ProductInstance { ConfigurationId = configId, SerialNumber = serialNumber, OptionValues = optionValues };
            _instances[instance.Id] = instance;
            InstanceCreated?.Invoke(this, instance);
            return instance;
        }

        public ProductInstance? GetInstance(string id) { _instances.TryGetValue(id, out var i); return i; }
        public ProductInstance? GetInstanceBySerialNumber(string serialNumber) => _instances.Values.FirstOrDefault(i => i.SerialNumber == serialNumber);

        public ProductFamily CreateFamily(string name, string description)
        {
            var family = new ProductFamily { Name = name, Description = description };
            _families[family.Id] = family;
            return family;
        }

        public ProductFamily? GetFamily(string id) { _families.TryGetValue(id, out var f); return f; }
        public IEnumerable<ProductFamily> GetAllFamilies() => _families.Values.ToList();

        public Dictionary<string, string> GetTestParametersForInstance(string instanceId)
        {
            if (!_instances.TryGetValue(instanceId, out var instance) || !_configurations.TryGetValue(instance.ConfigurationId, out var config)) return new Dictionary<string, string>();
            var parameters = new Dictionary<string, string>();
            foreach (var option in config.Options)
                if (instance.OptionValues.TryGetValue(option.OptionCode, out var value))
                    foreach (var param in option.TestParameters)
                        parameters[param.Key] = param.Value.Replace("{VALUE}", value);
            return parameters;
        }

        public void SaveConfiguration(string configId)
        {
            if (!_configurations.TryGetValue(configId, out var c)) return;
            File.WriteAllText(Path.Combine(_storageDirectory, $"{c.ProductCode}.json"), JsonSerializer.Serialize(c, new JsonSerializerOptions { WriteIndented = true }));
        }

        public ProductConfiguration? LoadConfiguration(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            var c = JsonSerializer.Deserialize<ProductConfiguration>(File.ReadAllText(filePath));
            if (c != null) _configurations[c.Id] = c;
            return c;
        }

        public void DeleteConfiguration(string id) => _configurations.Remove(id);
    }
}
