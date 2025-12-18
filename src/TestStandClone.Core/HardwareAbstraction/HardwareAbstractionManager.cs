using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestStandClone.Core.HardwareAbstraction
{
    #region Hardware Types

    /// <summary>
    /// Hardware device status
    /// </summary>
    public enum HardwareDeviceStatus
    {
        Unknown,
        Disconnected,
        Connecting,
        Connected,
        Ready,
        Busy,
        Error,
        Offline
    }

    /// <summary>
    /// Hardware device type
    /// </summary>
    public enum HardwareDeviceType
    {
        Generic,
        Multimeter,
        Oscilloscope,
        PowerSupply,
        FunctionGenerator,
        Switch,
        DAQ,
        PLC,
        Robot,
        Camera,
        Scanner,
        Printer,
        Custom
    }

    /// <summary>
    /// Hardware capability
    /// </summary>
    public class HardwareCapability
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public object? MinValue { get; set; }
        public object? MaxValue { get; set; }
        public string? Unit { get; set; }
        public bool IsSupported { get; set; } = true;
    }

    /// <summary>
    /// Abstract hardware device
    /// </summary>
    public abstract class HardwareDevice
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public HardwareDeviceType DeviceType { get; set; } = HardwareDeviceType.Generic;
        public HardwareDeviceStatus Status { get; protected set; } = HardwareDeviceStatus.Disconnected;
        public string ConnectionString { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
        public List<HardwareCapability> Capabilities { get; set; } = new();

        public event EventHandler<HardwareDeviceStatus>? StatusChanged;

        protected void SetStatus(HardwareDeviceStatus status)
        {
            if (Status != status)
            {
                Status = status;
                StatusChanged?.Invoke(this, status);
            }
        }

        public abstract Task<bool> ConnectAsync();
        public abstract Task DisconnectAsync();
        public abstract Task<bool> TestConnectionAsync();
        public abstract Task<string> IdentifyAsync();
        public abstract Task ResetAsync();
    }

    /// <summary>
    /// Simulated hardware device for testing
    /// </summary>
    public class SimulatedHardwareDevice : HardwareDevice
    {
        private readonly Random _random = new();

        public SimulatedHardwareDevice()
        {
            DeviceType = HardwareDeviceType.Generic;
            Manufacturer = "Simulated";
            Model = "SimDevice";
            FirmwareVersion = "1.0";
        }

        public override async Task<bool> ConnectAsync()
        {
            SetStatus(HardwareDeviceStatus.Connecting);
            await Task.Delay(100);
            SetStatus(HardwareDeviceStatus.Connected);
            await Task.Delay(50);
            SetStatus(HardwareDeviceStatus.Ready);
            return true;
        }

        public override async Task DisconnectAsync()
        {
            await Task.Delay(50);
            SetStatus(HardwareDeviceStatus.Disconnected);
        }

        public override async Task<bool> TestConnectionAsync()
        {
            await Task.Delay(50);
            return Status == HardwareDeviceStatus.Ready || Status == HardwareDeviceStatus.Connected;
        }

        public override async Task<string> IdentifyAsync()
        {
            await Task.Delay(50);
            return $"{Manufacturer},{Model},{SerialNumber},{FirmwareVersion}";
        }

        public override async Task ResetAsync()
        {
            SetStatus(HardwareDeviceStatus.Busy);
            await Task.Delay(200);
            SetStatus(HardwareDeviceStatus.Ready);
        }

        public double ReadMeasurement()
        {
            return _random.NextDouble() * 10.0;
        }
    }

    /// <summary>
    /// Hardware driver interface
    /// </summary>
    public interface IHardwareDriver
    {
        string DriverId { get; }
        string DriverName { get; }
        string DriverVersion { get; }
        HardwareDeviceType SupportedDeviceType { get; }

        Task<bool> InitializeAsync();
        Task ShutdownAsync();
        HardwareDevice CreateDevice(string connectionString);
        bool CanConnect(string connectionString);
    }

    /// <summary>
    /// Simulated hardware driver
    /// </summary>
    public class SimulatedHardwareDriver : IHardwareDriver
    {
        public string DriverId => "simulated";
        public string DriverName => "Simulated Driver";
        public string DriverVersion => "1.0";
        public HardwareDeviceType SupportedDeviceType => HardwareDeviceType.Generic;

        public Task<bool> InitializeAsync() => Task.FromResult(true);
        public Task ShutdownAsync() => Task.CompletedTask;

        public HardwareDevice CreateDevice(string connectionString)
        {
            return new SimulatedHardwareDevice
            {
                Name = "Simulated Device",
                ConnectionString = connectionString,
                SerialNumber = Guid.NewGuid().ToString("N")[..8].ToUpper()
            };
        }

        public bool CanConnect(string connectionString) => 
            connectionString.StartsWith("SIM:", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Hardware abstraction layer manager
    /// </summary>
    public class HardwareAbstractionManager
    {
        private static readonly Lazy<HardwareAbstractionManager> _instance = 
            new(() => new HardwareAbstractionManager());
        public static HardwareAbstractionManager Instance => _instance.Value;

        private readonly Dictionary<string, IHardwareDriver> _drivers = new();
        private readonly Dictionary<string, HardwareDevice> _devices = new();

        public event EventHandler<HardwareDevice>? DeviceConnected;
        public event EventHandler<HardwareDevice>? DeviceDisconnected;
        public event EventHandler<HardwareDevice>? DeviceStatusChanged;

        public IReadOnlyDictionary<string, HardwareDevice> Devices => _devices;
        public IReadOnlyDictionary<string, IHardwareDriver> Drivers => _drivers;

        public HardwareAbstractionManager()
        {
            // Register default simulated driver
            RegisterDriver(new SimulatedHardwareDriver());
        }

        public void RegisterDriver(IHardwareDriver driver)
        {
            _drivers[driver.DriverId] = driver;
        }

        public void UnregisterDriver(string driverId)
        {
            _drivers.Remove(driverId);
        }

        public async Task<HardwareDevice?> CreateAndConnectDeviceAsync(
            string driverId, string connectionString, string? deviceName = null)
        {
            if (!_drivers.TryGetValue(driverId, out var driver))
                return null;

            var device = driver.CreateDevice(connectionString);
            if (deviceName != null)
                device.Name = deviceName;

            device.StatusChanged += (s, status) => DeviceStatusChanged?.Invoke(this, device);

            if (await device.ConnectAsync())
            {
                _devices[device.Id] = device;
                DeviceConnected?.Invoke(this, device);
                return device;
            }

            return null;
        }

        public async Task DisconnectDeviceAsync(string deviceId)
        {
            if (_devices.TryGetValue(deviceId, out var device))
            {
                await device.DisconnectAsync();
                _devices.Remove(deviceId);
                DeviceDisconnected?.Invoke(this, device);
            }
        }

        public async Task DisconnectAllDevicesAsync()
        {
            var deviceIds = _devices.Keys.ToList();
            foreach (var deviceId in deviceIds)
            {
                await DisconnectDeviceAsync(deviceId);
            }
        }

        public HardwareDevice? GetDevice(string deviceId)
        {
            return _devices.TryGetValue(deviceId, out var device) ? device : null;
        }

        public List<HardwareDevice> GetDevicesByType(HardwareDeviceType type)
        {
            return _devices.Values.Where(d => d.DeviceType == type).ToList();
        }

        public List<HardwareDevice> GetDevicesByStatus(HardwareDeviceStatus status)
        {
            return _devices.Values.Where(d => d.Status == status).ToList();
        }

        public async Task<bool> TestAllConnectionsAsync()
        {
            foreach (var device in _devices.Values)
            {
                if (!await device.TestConnectionAsync())
                    return false;
            }
            return true;
        }
    }

    #endregion
}
