using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TestStandClone.Core.Licensing
{
    /// <summary>
    /// License types available for the application
    /// </summary>
    public enum LicenseType
    {
        /// <summary>Trial license with limited time</summary>
        Trial,
        /// <summary>Base edition with limited features</summary>
        Base,
        /// <summary>Standard edition with most features</summary>
        Standard,
        /// <summary>Full edition with all features</summary>
        Full,
        /// <summary>Development edition for testing</summary>
        Development
    }

    /// <summary>
    /// Feature flags for license-based feature control
    /// </summary>
    [Flags]
    public enum LicenseFeatures
    {
        /// <summary>No features</summary>
        None = 0,
        /// <summary>Execute sequences</summary>
        Execute = 1,
        /// <summary>Edit sequences</summary>
        Edit = 2,
        /// <summary>Generate reports</summary>
        Reports = 4,
        /// <summary>Use code modules</summary>
        CodeModules = 8,
        /// <summary>Use instrument I/O</summary>
        Instruments = 16,
        /// <summary>Parallel execution</summary>
        Parallel = 32,
        /// <summary>Database logging</summary>
        Database = 64,
        /// <summary>Deployment system</summary>
        Deployment = 128,
        /// <summary>Plugin support</summary>
        Plugins = 256,
        /// <summary>All features</summary>
        All = Execute | Edit | Reports | CodeModules | Instruments | Parallel | Database | Deployment | Plugins
    }

    /// <summary>
    /// Represents a software license
    /// </summary>
    public class License
    {
        /// <summary>Unique license identifier</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>License key</summary>
        public string Key { get; set; } = string.Empty;
        /// <summary>Type of license</summary>
        public LicenseType Type { get; set; } = LicenseType.Trial;
        /// <summary>Licensed features</summary>
        public LicenseFeatures Features { get; set; } = LicenseFeatures.Execute;
        /// <summary>Licensee name</summary>
        public string LicenseeName { get; set; } = string.Empty;
        /// <summary>Licensee organization</summary>
        public string Organization { get; set; } = string.Empty;
        /// <summary>Issue date</summary>
        public DateTime IssuedDate { get; set; } = DateTime.Now;
        /// <summary>Expiration date</summary>
        public DateTime ExpirationDate { get; set; } = DateTime.Now.AddDays(30);
        /// <summary>Maximum number of test sockets</summary>
        public int MaxTestSockets { get; set; } = 1;
        /// <summary>Machine identifier this license is bound to</summary>
        public string MachineId { get; set; } = string.Empty;
        /// <summary>Digital signature for validation</summary>
        public string Signature { get; set; } = string.Empty;
        /// <summary>Whether the license is currently active</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Checks if the license is valid
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (!IsActive) return false;
                if (DateTime.Now > ExpirationDate) return false;
                if (!string.IsNullOrEmpty(MachineId) && MachineId != LicenseManager.Instance.GetMachineId())
                    return false;
                return true;
            }
        }

        /// <summary>
        /// Checks if a specific feature is licensed
        /// </summary>
        public bool HasFeature(LicenseFeatures feature)
        {
            return IsValid && Features.HasFlag(feature);
        }

        /// <summary>
        /// Gets remaining days until expiration
        /// </summary>
        public int DaysRemaining => Math.Max(0, (ExpirationDate - DateTime.Now).Days);
    }

    /// <summary>
    /// License activation request
    /// </summary>
    public class ActivationRequest
    {
        /// <summary>License key to activate</summary>
        public string LicenseKey { get; set; } = string.Empty;
        /// <summary>Machine identifier</summary>
        public string MachineId { get; set; } = string.Empty;
        /// <summary>User name</summary>
        public string UserName { get; set; } = string.Empty;
        /// <summary>Organization name</summary>
        public string Organization { get; set; } = string.Empty;
        /// <summary>Request timestamp</summary>
        public DateTime RequestTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// License activation response
    /// </summary>
    public class ActivationResponse
    {
        /// <summary>Whether activation was successful</summary>
        public bool Success { get; set; }
        /// <summary>Error message if activation failed</summary>
        public string ErrorMessage { get; set; } = string.Empty;
        /// <summary>Activated license</summary>
        public License? License { get; set; }
    }

    /// <summary>
    /// License validator interface
    /// </summary>
    public interface ILicenseValidator
    {
        /// <summary>Validates a license</summary>
        bool Validate(License license);
        /// <summary>Gets validation error message</summary>
        string GetValidationError(License license);
    }

    /// <summary>
    /// Default license validator
    /// </summary>
    public class DefaultLicenseValidator : ILicenseValidator
    {
        /// <inheritdoc/>
        public bool Validate(License license)
        {
            if (license == null) return false;
            if (!license.IsActive) return false;
            if (DateTime.Now > license.ExpirationDate) return false;
            if (!string.IsNullOrEmpty(license.MachineId) && 
                license.MachineId != LicenseManager.Instance.GetMachineId())
                return false;
            return true;
        }

        /// <inheritdoc/>
        public string GetValidationError(License license)
        {
            if (license == null) return "License is null";
            if (!license.IsActive) return "License is not active";
            if (DateTime.Now > license.ExpirationDate) return "License has expired";
            if (!string.IsNullOrEmpty(license.MachineId) && 
                license.MachineId != LicenseManager.Instance.GetMachineId())
                return "License is not bound to this machine";
            return string.Empty;
        }
    }

    /// <summary>
    /// License manager singleton for managing software licenses
    /// </summary>
    public sealed class LicenseManager
    {
        private static readonly Lazy<LicenseManager> _instance = 
            new Lazy<LicenseManager>(() => new LicenseManager());
        
        /// <summary>Gets the singleton instance</summary>
        public static LicenseManager Instance => _instance.Value;

        private License? _currentLicense;
        private ILicenseValidator _validator;
        private string _licenseFilePath = "license.dat";
        private readonly object _lockObject = new object();

        /// <summary>Event raised when license changes</summary>
        public event EventHandler<License?>? LicenseChanged;

        /// <summary>Event raised when license validation fails</summary>
        public event EventHandler<string>? ValidationFailed;

        private LicenseManager()
        {
            _validator = new DefaultLicenseValidator();
            LoadLicense();
        }

        /// <summary>Gets the current license</summary>
        public License? CurrentLicense => _currentLicense;

        /// <summary>Gets whether a valid license is present</summary>
        public bool HasValidLicense => _currentLicense?.IsValid ?? false;

        /// <summary>Gets the license type</summary>
        public LicenseType LicenseType => _currentLicense?.Type ?? LicenseType.Trial;

        /// <summary>Gets licensed features</summary>
        public LicenseFeatures LicensedFeatures => _currentLicense?.Features ?? LicenseFeatures.Execute;

        /// <summary>Sets the license file path</summary>
        public void SetLicenseFilePath(string path)
        {
            _licenseFilePath = path;
        }

        /// <summary>Sets a custom license validator</summary>
        public void SetValidator(ILicenseValidator validator)
        {
            _validator = validator;
        }

        /// <summary>Gets the machine identifier</summary>
        public string GetMachineId()
        {
            try
            {
                string computerName = Environment.MachineName;
                string userName = Environment.UserName;
                string combined = $"{computerName}:{userName}";
                using var sha256 = SHA256.Create();
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                return Convert.ToBase64String(hash).Substring(0, 16);
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        /// <summary>Activates a license with the given key</summary>
        public ActivationResponse ActivateLicense(string licenseKey)
        {
            lock (_lockObject)
            {
                var request = new ActivationRequest
                {
                    LicenseKey = licenseKey,
                    MachineId = GetMachineId(),
                    UserName = Environment.UserName,
                    RequestTime = DateTime.Now
                };

                // Simulate license activation (in production, this would call a license server)
                var license = GenerateLicenseFromKey(licenseKey);
                if (license == null)
                {
                    return new ActivationResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid license key"
                    };
                }

                license.MachineId = request.MachineId;
                
                if (!_validator.Validate(license))
                {
                    return new ActivationResponse
                    {
                        Success = false,
                        ErrorMessage = _validator.GetValidationError(license)
                    };
                }

                _currentLicense = license;
                SaveLicense();
                LicenseChanged?.Invoke(this, license);

                return new ActivationResponse
                {
                    Success = true,
                    License = license
                };
            }
        }

        /// <summary>Deactivates the current license</summary>
        public void DeactivateLicense()
        {
            lock (_lockObject)
            {
                _currentLicense = null;
                if (File.Exists(_licenseFilePath))
                {
                    File.Delete(_licenseFilePath);
                }
                LicenseChanged?.Invoke(this, null);
            }
        }

        /// <summary>Checks if a feature is licensed</summary>
        public bool IsFeatureLicensed(LicenseFeatures feature)
        {
            return _currentLicense?.HasFeature(feature) ?? false;
        }

        /// <summary>Validates the current license</summary>
        public bool ValidateLicense()
        {
            if (_currentLicense == null)
            {
                ValidationFailed?.Invoke(this, "No license installed");
                return false;
            }

            if (!_validator.Validate(_currentLicense))
            {
                string error = _validator.GetValidationError(_currentLicense);
                ValidationFailed?.Invoke(this, error);
                return false;
            }

            return true;
        }

        /// <summary>Loads license from file</summary>
        private void LoadLicense()
        {
            try
            {
                if (File.Exists(_licenseFilePath))
                {
                    string json = File.ReadAllText(_licenseFilePath);
                    _currentLicense = JsonSerializer.Deserialize<License>(json);
                }
            }
            catch
            {
                _currentLicense = null;
            }
        }

        /// <summary>Saves license to file</summary>
        private void SaveLicense()
        {
            try
            {
                if (_currentLicense != null)
                {
                    string json = JsonSerializer.Serialize(_currentLicense, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_licenseFilePath, json);
                }
            }
            catch
            {
                // Ignore save errors
            }
        }

        /// <summary>Generates a license from a license key (simulation)</summary>
        private License? GenerateLicenseFromKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length < 16) return null;

            // Simulate different license types based on key prefix
            LicenseType type;
            LicenseFeatures features;
            int maxSockets;
            int daysValid;

            if (key.StartsWith("TRIAL-"))
            {
                type = LicenseType.Trial;
                features = LicenseFeatures.Execute;
                maxSockets = 1;
                daysValid = 30;
            }
            else if (key.StartsWith("BASE-"))
            {
                type = LicenseType.Base;
                features = LicenseFeatures.Execute | LicenseFeatures.Edit | LicenseFeatures.Reports;
                maxSockets = 2;
                daysValid = 365;
            }
            else if (key.StartsWith("STD-"))
            {
                type = LicenseType.Standard;
                features = LicenseFeatures.Execute | LicenseFeatures.Edit | LicenseFeatures.Reports | 
                          LicenseFeatures.CodeModules | LicenseFeatures.Instruments | LicenseFeatures.Database;
                maxSockets = 4;
                daysValid = 365;
            }
            else if (key.StartsWith("FULL-"))
            {
                type = LicenseType.Full;
                features = LicenseFeatures.All;
                maxSockets = 16;
                daysValid = 365;
            }
            else if (key.StartsWith("DEV-"))
            {
                type = LicenseType.Development;
                features = LicenseFeatures.All;
                maxSockets = 16;
                daysValid = 30;
            }
            else
            {
                return null;
            }

            return new License
            {
                Key = key,
                Type = type,
                Features = features,
                MaxTestSockets = maxSockets,
                IssuedDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddDays(daysValid),
                IsActive = true
            };
        }

        /// <summary>Gets license status information</summary>
        public string GetLicenseStatus()
        {
            if (_currentLicense == null)
            {
                return "No license installed";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"License Type: {_currentLicense.Type}");
            sb.AppendLine($"License Key: {_currentLicense.Key}");
            sb.AppendLine($"Licensee: {_currentLicense.LicenseeName}");
            sb.AppendLine($"Organization: {_currentLicense.Organization}");
            sb.AppendLine($"Valid: {_currentLicense.IsValid}");
            sb.AppendLine($"Expires: {_currentLicense.ExpirationDate:yyyy-MM-dd}");
            sb.AppendLine($"Days Remaining: {_currentLicense.DaysRemaining}");
            sb.AppendLine($"Max Test Sockets: {_currentLicense.MaxTestSockets}");
            sb.AppendLine($"Features: {_currentLicense.Features}");
            return sb.ToString();
        }
    }
}
