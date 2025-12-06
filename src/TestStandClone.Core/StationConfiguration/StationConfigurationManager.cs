using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestStandClone.Core.StationConfiguration
{
    /// <summary>
    /// Station options for test execution
    /// </summary>
    public class StationOptions
    {
        /// <summary>Model options for the test station</summary>
        public ModelOptions Model { get; set; } = new();
        
        /// <summary>User options</summary>
        public UserOptions User { get; set; } = new();
        
        /// <summary>Execution options</summary>
        public ExecutionOptions Execution { get; set; } = new();
        
        /// <summary>Report options</summary>
        public ReportOptions Report { get; set; } = new();
        
        /// <summary>Database options</summary>
        public DatabaseOptions Database { get; set; } = new();
        
        /// <summary>Instrument options</summary>
        public InstrumentOptions Instruments { get; set; } = new();
        
        /// <summary>Serial number options</summary>
        public SerialNumberOptions SerialNumber { get; set; } = new();
        
        /// <summary>Custom station properties</summary>
        public Dictionary<string, object> CustomProperties { get; set; } = new();
    }

    /// <summary>
    /// Model-specific options
    /// </summary>
    public class ModelOptions
    {
        /// <summary>Default process model to use</summary>
        public string DefaultProcessModel { get; set; } = "Sequential";
        
        /// <summary>Number of test sockets for parallel execution</summary>
        public int NumberOfTestSockets { get; set; } = 1;
        
        /// <summary>Whether to use independent thread for each socket</summary>
        public bool UseIndependentThread { get; set; } = true;
        
        /// <summary>Batch size for batch model</summary>
        public int BatchSize { get; set; } = 4;
        
        /// <summary>Enable automatic resource allocation</summary>
        public bool AutoResourceAllocation { get; set; } = true;
    }

    /// <summary>
    /// User-related options
    /// </summary>
    public class UserOptions
    {
        /// <summary>Require user login before execution</summary>
        public bool RequireLogin { get; set; } = false;
        
        /// <summary>Default user if login not required</summary>
        public string DefaultUser { get; set; } = "Operator";
        
        /// <summary>Allow guest access</summary>
        public bool AllowGuestAccess { get; set; } = true;
        
        /// <summary>Session timeout in minutes (0 = no timeout)</summary>
        public int SessionTimeoutMinutes { get; set; } = 0;
        
        /// <summary>Audit user actions</summary>
        public bool AuditActions { get; set; } = false;
    }

    /// <summary>
    /// Execution options
    /// </summary>
    public class ExecutionOptions
    {
        /// <summary>Maximum execution time in seconds (0 = unlimited)</summary>
        public int MaxExecutionTimeSeconds { get; set; } = 0;
        
        /// <summary>Pause on step failure</summary>
        public bool PauseOnFailure { get; set; } = false;
        
        /// <summary>Enable step timing</summary>
        public bool EnableTiming { get; set; } = true;
        
        /// <summary>Disable interactive steps during automated execution</summary>
        public bool DisableInteractiveSteps { get; set; } = false;
        
        /// <summary>Run setup and cleanup steps</summary>
        public bool RunSetupCleanup { get; set; } = true;
        
        /// <summary>Continue on error in cleanup sequence</summary>
        public bool ContinueOnCleanupError { get; set; } = true;
        
        /// <summary>Enable tracing</summary>
        public bool EnableTracing { get; set; } = false;
        
        /// <summary>Trace file path</summary>
        public string TraceFilePath { get; set; } = "";
        
        /// <summary>Number of retries on failure</summary>
        public int RetryCount { get; set; } = 0;
    }

    /// <summary>
    /// Report options
    /// </summary>
    public class ReportOptions
    {
        /// <summary>Automatically generate report after execution</summary>
        public bool AutoGenerateReport { get; set; } = false;
        
        /// <summary>Default report format</summary>
        public string DefaultFormat { get; set; } = "HTML";
        
        /// <summary>Report output directory</summary>
        public string OutputDirectory { get; set; } = "./Reports";
        
        /// <summary>Include step details in report</summary>
        public bool IncludeStepDetails { get; set; } = true;
        
        /// <summary>Include timing information</summary>
        public bool IncludeTiming { get; set; } = true;
        
        /// <summary>Include limits in report</summary>
        public bool IncludeLimits { get; set; } = true;
        
        /// <summary>Report filename pattern</summary>
        public string FilenamePattern { get; set; } = "{SequenceName}_{DateTime}_{UUT}";
        
        /// <summary>Report template path</summary>
        public string TemplatePath { get; set; } = "";
    }

    /// <summary>
    /// Database options
    /// </summary>
    public class DatabaseOptions
    {
        /// <summary>Enable database logging</summary>
        public bool EnableLogging { get; set; } = true;
        
        /// <summary>Database type (JSON, SQLite, SQL)</summary>
        public string DatabaseType { get; set; } = "JSON";
        
        /// <summary>Database connection string or path</summary>
        public string ConnectionString { get; set; } = "./Results";
        
        /// <summary>Log passed steps</summary>
        public bool LogPassedSteps { get; set; } = true;
        
        /// <summary>Log failed steps</summary>
        public bool LogFailedSteps { get; set; } = true;
        
        /// <summary>Log skipped steps</summary>
        public bool LogSkippedSteps { get; set; } = false;
        
        /// <summary>Retention days (0 = forever)</summary>
        public int RetentionDays { get; set; } = 0;
    }

    /// <summary>
    /// Instrument options
    /// </summary>
    public class InstrumentOptions
    {
        /// <summary>Default VISA resource manager</summary>
        public string DefaultResourceManager { get; set; } = "Simulated";
        
        /// <summary>Timeout for instrument communication in ms</summary>
        public int DefaultTimeoutMs { get; set; } = 5000;
        
        /// <summary>Retry count for failed commands</summary>
        public int CommandRetryCount { get; set; } = 1;
        
        /// <summary>Delay between retries in ms</summary>
        public int RetryDelayMs { get; set; } = 100;
        
        /// <summary>Enable instrument simulation mode</summary>
        public bool SimulationMode { get; set; } = true;
        
        /// <summary>Instrument alias mappings</summary>
        public Dictionary<string, string> InstrumentAliases { get; set; } = new();
    }

    /// <summary>
    /// Serial number options
    /// </summary>
    public class SerialNumberOptions
    {
        /// <summary>Prompt for serial number before test</summary>
        public bool PromptForSerialNumber { get; set; } = true;
        
        /// <summary>Serial number format pattern</summary>
        public string FormatPattern { get; set; } = "";
        
        /// <summary>Validate serial number format</summary>
        public bool ValidateFormat { get; set; } = false;
        
        /// <summary>Auto-generate if not provided</summary>
        public bool AutoGenerate { get; set; } = false;
        
        /// <summary>Auto-generate prefix</summary>
        public string AutoGeneratePrefix { get; set; } = "UUT";
        
        /// <summary>Check for duplicate serial numbers</summary>
        public bool CheckDuplicates { get; set; } = false;
    }

    /// <summary>
    /// Manages station configuration for the test system
    /// Similar to TestStand Station Options
    /// </summary>
    public class StationConfigurationManager
    {
        private static readonly Lazy<StationConfigurationManager> _instance = new(() => new StationConfigurationManager());
        public static StationConfigurationManager Instance => _instance.Value;

        private StationOptions _options = new();
        private string _configFilePath = "";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private StationConfigurationManager() { }

        /// <summary>
        /// Current station options
        /// </summary>
        public StationOptions Options => _options;

        /// <summary>
        /// Load station configuration from file
        /// </summary>
        public void LoadConfiguration(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Station configuration file not found", filePath);
            }

            var json = File.ReadAllText(filePath);
            var options = JsonSerializer.Deserialize<StationOptions>(json, JsonOptions);
            if (options != null)
            {
                _options = options;
                _configFilePath = filePath;
            }
        }

        /// <summary>
        /// Save station configuration to file
        /// </summary>
        public void SaveConfiguration(string? filePath = null)
        {
            var path = filePath ?? _configFilePath;
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("No configuration file path specified");
            }

            var json = JsonSerializer.Serialize(_options, JsonOptions);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(path, json);
            _configFilePath = path;
        }

        /// <summary>
        /// Reset to default configuration
        /// </summary>
        public void ResetToDefaults()
        {
            _options = new StationOptions();
        }

        /// <summary>
        /// Get a custom property value
        /// </summary>
        public T? GetCustomProperty<T>(string key)
        {
            if (_options.CustomProperties.TryGetValue(key, out var value))
            {
                if (value is JsonElement element)
                {
                    return JsonSerializer.Deserialize<T>(element.GetRawText());
                }
                return (T)value;
            }
            return default;
        }

        /// <summary>
        /// Set a custom property value
        /// </summary>
        public void SetCustomProperty<T>(string key, T value)
        {
            _options.CustomProperties[key] = value!;
        }

        /// <summary>
        /// Create default configuration file
        /// </summary>
        public static void CreateDefaultConfiguration(string filePath)
        {
            var options = new StationOptions();
            var json = JsonSerializer.Serialize(options, JsonOptions);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(filePath, json);
        }
    }
}
