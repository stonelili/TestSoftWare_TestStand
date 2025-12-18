using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestStandClone.Core.BarcodeScanner
{
    /// <summary>
    /// Barcode type enumeration
    /// </summary>
    public enum BarcodeType
    {
        Code128,
        Code39,
        QRCode,
        DataMatrix,
        EAN13,
        UPC,
        Interleaved2of5,
        PDF417,
        Unknown
    }

    /// <summary>
    /// Scanner connection type
    /// </summary>
    public enum ScannerConnectionType
    {
        USB,
        Serial,
        Bluetooth,
        Network,
        Simulated
    }

    /// <summary>
    /// Represents a barcode scan result
    /// </summary>
    public class BarcodeScanResult
    {
        public string Data { get; set; } = string.Empty;
        public BarcodeType Type { get; set; }
        public DateTime ScanTime { get; set; } = DateTime.Now;
        public string ScannerId { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; } = string.Empty;
        public Dictionary<string, object> ParsedData { get; set; } = new();
    }

    /// <summary>
    /// Barcode validation rule
    /// </summary>
    public class BarcodeValidationRule
    {
        public string Name { get; set; } = string.Empty;
        public BarcodeType? ExpectedType { get; set; }
        public string Pattern { get; set; } = string.Empty;
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public Func<string, bool>? CustomValidator { get; set; }
    }

    /// <summary>
    /// Represents a barcode scanner device
    /// </summary>
    public class Scanner
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public ScannerConnectionType ConnectionType { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public bool IsEnabled { get; set; } = true;
        public List<BarcodeType> SupportedTypes { get; set; } = new();
    }

    /// <summary>
    /// Scanner interface for different implementations
    /// </summary>
    public interface IScannerDriver
    {
        string ScannerId { get; }
        bool IsConnected { get; }
        Task<bool> ConnectAsync();
        Task DisconnectAsync();
        Task<BarcodeScanResult?> ReadBarcodeAsync(TimeSpan? timeout = null);
        void CancelRead();
    }

    /// <summary>
    /// Simulated scanner driver for testing
    /// </summary>
    public class SimulatedScannerDriver : IScannerDriver
    {
        public string ScannerId { get; }
        public bool IsConnected { get; private set; }
        private readonly List<string> _simulatedBarcodes = new();
        private int _currentIndex;

        public SimulatedScannerDriver(string scannerId)
        {
            ScannerId = scannerId;
        }

        public void AddSimulatedBarcode(string barcode)
        {
            _simulatedBarcodes.Add(barcode);
        }

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

        public async Task<BarcodeScanResult?> ReadBarcodeAsync(TimeSpan? timeout = null)
        {
            await Task.Delay(100);

            if (_simulatedBarcodes.Count == 0)
            {
                return new BarcodeScanResult
                {
                    Data = $"SN{DateTime.Now:yyyyMMddHHmmss}",
                    Type = BarcodeType.Code128,
                    ScannerId = ScannerId,
                    IsValid = true
                };
            }

            var barcode = _simulatedBarcodes[_currentIndex % _simulatedBarcodes.Count];
            _currentIndex++;

            return new BarcodeScanResult
            {
                Data = barcode,
                Type = BarcodeType.Code128,
                ScannerId = ScannerId,
                IsValid = true
            };
        }

        public void CancelRead()
        {
        }
    }

    /// <summary>
    /// Barcode scanner manager
    /// </summary>
    public class BarcodeScannerManager
    {
        private static BarcodeScannerManager? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, Scanner> _scanners = new();
        private readonly Dictionary<string, IScannerDriver> _drivers = new();
        private readonly List<BarcodeValidationRule> _validationRules = new();
        private readonly List<BarcodeScanResult> _scanHistory = new();

        public static BarcodeScannerManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new BarcodeScannerManager();
                    }
                }
                return _instance;
            }
        }

        public event EventHandler<BarcodeScanResult>? BarcodeScanned;
        public event EventHandler<Scanner>? ScannerConnected;
        public event EventHandler<Scanner>? ScannerDisconnected;

        /// <summary>
        /// Register a scanner
        /// </summary>
        public void RegisterScanner(Scanner scanner, IScannerDriver driver)
        {
            lock (_lock)
            {
                _scanners[scanner.Id] = scanner;
                _drivers[scanner.Id] = driver;
            }
        }

        /// <summary>
        /// Connect to a scanner
        /// </summary>
        public async Task<bool> ConnectScannerAsync(string scannerId)
        {
            if (!_drivers.TryGetValue(scannerId, out var driver))
            {
                return false;
            }

            var connected = await driver.ConnectAsync();
            
            if (connected && _scanners.TryGetValue(scannerId, out var scanner))
            {
                scanner.IsConnected = true;
                ScannerConnected?.Invoke(this, scanner);
            }

            return connected;
        }

        /// <summary>
        /// Disconnect from a scanner
        /// </summary>
        public async Task DisconnectScannerAsync(string scannerId)
        {
            if (_drivers.TryGetValue(scannerId, out var driver))
            {
                await driver.DisconnectAsync();
                
                if (_scanners.TryGetValue(scannerId, out var scanner))
                {
                    scanner.IsConnected = false;
                    ScannerDisconnected?.Invoke(this, scanner);
                }
            }
        }

        /// <summary>
        /// Read a barcode from a specific scanner
        /// </summary>
        public async Task<BarcodeScanResult?> ReadBarcodeAsync(string scannerId, TimeSpan? timeout = null)
        {
            if (!_drivers.TryGetValue(scannerId, out var driver))
            {
                return null;
            }

            var result = await driver.ReadBarcodeAsync(timeout);
            
            if (result != null)
            {
                ValidateBarcode(result);
                
                lock (_lock)
                {
                    _scanHistory.Add(result);
                }
                
                BarcodeScanned?.Invoke(this, result);
            }

            return result;
        }

        /// <summary>
        /// Read a barcode from any available scanner
        /// </summary>
        public async Task<BarcodeScanResult?> ReadBarcodeFromAnyAsync(TimeSpan? timeout = null)
        {
            var connectedScanners = _scanners.Values.Where(s => s.IsConnected && s.IsEnabled).ToList();
            
            if (connectedScanners.Count == 0)
            {
                return null;
            }

            return await ReadBarcodeAsync(connectedScanners[0].Id, timeout);
        }

        /// <summary>
        /// Add a validation rule
        /// </summary>
        public void AddValidationRule(BarcodeValidationRule rule)
        {
            lock (_lock)
            {
                _validationRules.Add(rule);
            }
        }

        /// <summary>
        /// Validate a barcode against rules
        /// </summary>
        public void ValidateBarcode(BarcodeScanResult result)
        {
            result.IsValid = true;
            result.ValidationMessage = "Valid";

            foreach (var rule in _validationRules)
            {
                if (rule.ExpectedType.HasValue && result.Type != rule.ExpectedType.Value)
                {
                    result.IsValid = false;
                    result.ValidationMessage = $"Expected type {rule.ExpectedType.Value}, got {result.Type}";
                    return;
                }

                if (rule.MinLength.HasValue && result.Data.Length < rule.MinLength.Value)
                {
                    result.IsValid = false;
                    result.ValidationMessage = $"Barcode too short (min {rule.MinLength.Value})";
                    return;
                }

                if (rule.MaxLength.HasValue && result.Data.Length > rule.MaxLength.Value)
                {
                    result.IsValid = false;
                    result.ValidationMessage = $"Barcode too long (max {rule.MaxLength.Value})";
                    return;
                }

                if (!string.IsNullOrEmpty(rule.Pattern))
                {
                    var regex = new System.Text.RegularExpressions.Regex(rule.Pattern);
                    if (!regex.IsMatch(result.Data))
                    {
                        result.IsValid = false;
                        result.ValidationMessage = $"Barcode does not match pattern {rule.Pattern}";
                        return;
                    }
                }

                if (rule.CustomValidator != null && !rule.CustomValidator(result.Data))
                {
                    result.IsValid = false;
                    result.ValidationMessage = $"Custom validation failed for rule {rule.Name}";
                    return;
                }
            }
        }

        /// <summary>
        /// Parse barcode data
        /// </summary>
        public Dictionary<string, object> ParseBarcodeData(string data, string format)
        {
            var result = new Dictionary<string, object>();

            if (format == "SerialNumber")
            {
                result["SerialNumber"] = data;
            }
            else if (format == "ProductCode")
            {
                var parts = data.Split('-');
                if (parts.Length >= 1) result["ProductCode"] = parts[0];
                if (parts.Length >= 2) result["LotNumber"] = parts[1];
                if (parts.Length >= 3) result["SerialNumber"] = parts[2];
            }

            return result;
        }

        /// <summary>
        /// Get scan history
        /// </summary>
        public IReadOnlyList<BarcodeScanResult> GetScanHistory(DateTime? startDate = null, DateTime? endDate = null)
        {
            lock (_lock)
            {
                var query = _scanHistory.AsEnumerable();
                
                if (startDate.HasValue)
                    query = query.Where(r => r.ScanTime >= startDate.Value);
                
                if (endDate.HasValue)
                    query = query.Where(r => r.ScanTime <= endDate.Value);
                
                return query.ToList();
            }
        }

        /// <summary>
        /// Get all registered scanners
        /// </summary>
        public IReadOnlyList<Scanner> GetAllScanners()
        {
            lock (_lock)
            {
                return _scanners.Values.ToList();
            }
        }
    }
}
