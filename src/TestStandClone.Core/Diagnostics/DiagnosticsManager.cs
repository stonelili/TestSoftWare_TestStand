using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TestStandClone.Core.Diagnostics
{
    /// <summary>
    /// Diagnostic category
    /// </summary>
    public enum DiagnosticCategory
    {
        /// <summary>System information</summary>
        System,
        /// <summary>Performance metrics</summary>
        Performance,
        /// <summary>Memory usage</summary>
        Memory,
        /// <summary>Configuration issues</summary>
        Configuration,
        /// <summary>Execution issues</summary>
        Execution,
        /// <summary>Communication issues</summary>
        Communication,
        /// <summary>File system issues</summary>
        FileSystem,
        /// <summary>License issues</summary>
        License
    }

    /// <summary>
    /// Diagnostic result status
    /// </summary>
    public enum DiagnosticStatus
    {
        /// <summary>Check passed</summary>
        Passed,
        /// <summary>Warning found</summary>
        Warning,
        /// <summary>Error found</summary>
        Failed,
        /// <summary>Check not applicable</summary>
        NotApplicable,
        /// <summary>Check not run</summary>
        NotRun
    }

    /// <summary>
    /// Individual diagnostic check result
    /// </summary>
    public class DiagnosticResult
    {
        /// <summary>Check name</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Category</summary>
        public DiagnosticCategory Category { get; set; }
        /// <summary>Status</summary>
        public DiagnosticStatus Status { get; set; } = DiagnosticStatus.NotRun;
        /// <summary>Message</summary>
        public string Message { get; set; } = string.Empty;
        /// <summary>Details</summary>
        public string Details { get; set; } = string.Empty;
        /// <summary>Value (if applicable)</summary>
        public object? Value { get; set; }
        /// <summary>Expected value (if applicable)</summary>
        public object? ExpectedValue { get; set; }
        /// <summary>Suggestion for fixing issues</summary>
        public string Suggestion { get; set; } = string.Empty;
        /// <summary>Check duration</summary>
        public TimeSpan Duration { get; set; }
        /// <summary>Timestamp</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Diagnostic report
    /// </summary>
    public class DiagnosticReport
    {
        /// <summary>Report ID</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>Generated at</summary>
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        /// <summary>System name</summary>
        public string SystemName { get; set; } = Environment.MachineName;
        /// <summary>Overall status</summary>
        public DiagnosticStatus OverallStatus { get; set; }
        /// <summary>All results</summary>
        public List<DiagnosticResult> Results { get; set; } = new();
        /// <summary>Summary statistics</summary>
        public Dictionary<string, int> Statistics { get; set; } = new();
        /// <summary>Total duration</summary>
        public TimeSpan TotalDuration { get; set; }
    }

    /// <summary>
    /// Interface for diagnostic checks
    /// </summary>
    public interface IDiagnosticCheck
    {
        /// <summary>Gets the check name</summary>
        string Name { get; }
        /// <summary>Gets the category</summary>
        DiagnosticCategory Category { get; }
        /// <summary>Gets the description</summary>
        string Description { get; }
        /// <summary>Runs the diagnostic check</summary>
        Task<DiagnosticResult> RunAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// System info check
    /// </summary>
    public class SystemInfoCheck : IDiagnosticCheck
    {
        /// <inheritdoc/>
        public string Name => "System Information";
        /// <inheritdoc/>
        public DiagnosticCategory Category => DiagnosticCategory.System;
        /// <inheritdoc/>
        public string Description => "Collects system information";

        /// <inheritdoc/>
        public Task<DiagnosticResult> RunAsync(CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            
            var info = new StringBuilder();
            info.AppendLine($"OS: {Environment.OSVersion}");
            info.AppendLine($"Machine: {Environment.MachineName}");
            info.AppendLine($"User: {Environment.UserName}");
            info.AppendLine($"Processors: {Environment.ProcessorCount}");
            info.AppendLine($".NET Version: {Environment.Version}");
            info.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
            info.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");

            sw.Stop();

            return Task.FromResult(new DiagnosticResult
            {
                Name = Name,
                Category = Category,
                Status = DiagnosticStatus.Passed,
                Message = "System information collected",
                Details = info.ToString(),
                Duration = sw.Elapsed
            });
        }
    }

    /// <summary>
    /// Memory check
    /// </summary>
    public class MemoryCheck : IDiagnosticCheck
    {
        /// <inheritdoc/>
        public string Name => "Memory Usage";
        /// <inheritdoc/>
        public DiagnosticCategory Category => DiagnosticCategory.Memory;
        /// <inheritdoc/>
        public string Description => "Checks memory usage and availability";

        private readonly long _warningThresholdMB;
        private readonly long _errorThresholdMB;

        /// <summary>Creates a new memory check</summary>
        public MemoryCheck(long warningThresholdMB = 500, long errorThresholdMB = 100)
        {
            _warningThresholdMB = warningThresholdMB;
            _errorThresholdMB = errorThresholdMB;
        }

        /// <inheritdoc/>
        public Task<DiagnosticResult> RunAsync(CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            
            var process = Process.GetCurrentProcess();
            long workingSet = process.WorkingSet64;
            long privateBytes = process.PrivateMemorySize64;
            
            long workingSetMB = workingSet / (1024 * 1024);
            long privateBytesMB = privateBytes / (1024 * 1024);

            sw.Stop();

            DiagnosticStatus status;
            string message;

            if (workingSetMB > _errorThresholdMB * 10)
            {
                status = DiagnosticStatus.Failed;
                message = $"High memory usage: {workingSetMB} MB";
            }
            else if (workingSetMB > _warningThresholdMB)
            {
                status = DiagnosticStatus.Warning;
                message = $"Elevated memory usage: {workingSetMB} MB";
            }
            else
            {
                status = DiagnosticStatus.Passed;
                message = $"Memory usage normal: {workingSetMB} MB";
            }

            return Task.FromResult(new DiagnosticResult
            {
                Name = Name,
                Category = Category,
                Status = status,
                Message = message,
                Details = $"Working Set: {workingSetMB} MB\nPrivate Bytes: {privateBytesMB} MB",
                Value = workingSetMB,
                Duration = sw.Elapsed,
                Suggestion = status != DiagnosticStatus.Passed ? "Consider closing unused sequences or restarting the application" : string.Empty
            });
        }
    }

    /// <summary>
    /// Disk space check
    /// </summary>
    public class DiskSpaceCheck : IDiagnosticCheck
    {
        /// <inheritdoc/>
        public string Name => "Disk Space";
        /// <inheritdoc/>
        public DiagnosticCategory Category => DiagnosticCategory.FileSystem;
        /// <inheritdoc/>
        public string Description => "Checks available disk space";

        private readonly string _path;
        private readonly long _warningThresholdGB;
        private readonly long _errorThresholdGB;

        /// <summary>Creates a new disk space check</summary>
        public DiskSpaceCheck(string path = ".", long warningThresholdGB = 5, long errorThresholdGB = 1)
        {
            _path = path;
            _warningThresholdGB = warningThresholdGB;
            _errorThresholdGB = errorThresholdGB;
        }

        /// <inheritdoc/>
        public Task<DiagnosticResult> RunAsync(CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            
            try
            {
                string fullPath = Path.GetFullPath(_path);
                string root = Path.GetPathRoot(fullPath) ?? _path;
                var driveInfo = new DriveInfo(root);
                
                long freeGB = driveInfo.AvailableFreeSpace / (1024 * 1024 * 1024);
                long totalGB = driveInfo.TotalSize / (1024 * 1024 * 1024);

                sw.Stop();

                DiagnosticStatus status;
                string message;

                if (freeGB < _errorThresholdGB)
                {
                    status = DiagnosticStatus.Failed;
                    message = $"Critical: Only {freeGB} GB free on {root}";
                }
                else if (freeGB < _warningThresholdGB)
                {
                    status = DiagnosticStatus.Warning;
                    message = $"Low disk space: {freeGB} GB free on {root}";
                }
                else
                {
                    status = DiagnosticStatus.Passed;
                    message = $"Adequate disk space: {freeGB} GB free on {root}";
                }

                return Task.FromResult(new DiagnosticResult
                {
                    Name = Name,
                    Category = Category,
                    Status = status,
                    Message = message,
                    Details = $"Drive: {root}\nFree: {freeGB} GB\nTotal: {totalGB} GB",
                    Value = freeGB,
                    Duration = sw.Elapsed,
                    Suggestion = status != DiagnosticStatus.Passed ? "Free up disk space by removing old files or logs" : string.Empty
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Task.FromResult(new DiagnosticResult
                {
                    Name = Name,
                    Category = Category,
                    Status = DiagnosticStatus.Failed,
                    Message = $"Failed to check disk space: {ex.Message}",
                    Duration = sw.Elapsed
                });
            }
        }
    }

    /// <summary>
    /// Configuration check
    /// </summary>
    public class ConfigurationCheck : IDiagnosticCheck
    {
        /// <inheritdoc/>
        public string Name => "Configuration";
        /// <inheritdoc/>
        public DiagnosticCategory Category => DiagnosticCategory.Configuration;
        /// <inheritdoc/>
        public string Description => "Validates configuration files";

        private readonly string _configPath;

        /// <summary>Creates a new configuration check</summary>
        public ConfigurationCheck(string configPath = "config.json")
        {
            _configPath = configPath;
        }

        /// <inheritdoc/>
        public Task<DiagnosticResult> RunAsync(CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            
            try
            {
                if (!File.Exists(_configPath))
                {
                    sw.Stop();
                    return Task.FromResult(new DiagnosticResult
                    {
                        Name = Name,
                        Category = Category,
                        Status = DiagnosticStatus.Warning,
                        Message = "Configuration file not found",
                        Details = $"Expected at: {_configPath}",
                        Duration = sw.Elapsed,
                        Suggestion = "Create a configuration file or use default settings"
                    });
                }

                string content = File.ReadAllText(_configPath);
                JsonDocument.Parse(content); // Validate JSON

                sw.Stop();
                return Task.FromResult(new DiagnosticResult
                {
                    Name = Name,
                    Category = Category,
                    Status = DiagnosticStatus.Passed,
                    Message = "Configuration file is valid",
                    Details = $"Path: {_configPath}\nSize: {new FileInfo(_configPath).Length} bytes",
                    Duration = sw.Elapsed
                });
            }
            catch (JsonException ex)
            {
                sw.Stop();
                return Task.FromResult(new DiagnosticResult
                {
                    Name = Name,
                    Category = Category,
                    Status = DiagnosticStatus.Failed,
                    Message = "Invalid configuration file",
                    Details = ex.Message,
                    Duration = sw.Elapsed,
                    Suggestion = "Fix JSON syntax errors in the configuration file"
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Task.FromResult(new DiagnosticResult
                {
                    Name = Name,
                    Category = Category,
                    Status = DiagnosticStatus.Failed,
                    Message = $"Configuration check failed: {ex.Message}",
                    Duration = sw.Elapsed
                });
            }
        }
    }

    /// <summary>
    /// Performance metrics collector
    /// </summary>
    public class PerformanceMetrics
    {
        /// <summary>CPU usage percentage</summary>
        public double CpuUsage { get; set; }
        /// <summary>Memory usage in MB</summary>
        public long MemoryUsageMB { get; set; }
        /// <summary>Thread count</summary>
        public int ThreadCount { get; set; }
        /// <summary>Handle count</summary>
        public int HandleCount { get; set; }
        /// <summary>GC collection counts</summary>
        public int[] GCCollections { get; set; } = new int[3];
        /// <summary>Timestamp</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Diagnostics manager singleton
    /// </summary>
    public sealed class DiagnosticsManager
    {
        private static readonly Lazy<DiagnosticsManager> _instance = 
            new Lazy<DiagnosticsManager>(() => new DiagnosticsManager());
        
        /// <summary>Gets the singleton instance</summary>
        public static DiagnosticsManager Instance => _instance.Value;

        private readonly List<IDiagnosticCheck> _checks = new();
        private readonly List<DiagnosticReport> _reports = new();
        private readonly List<PerformanceMetrics> _metricsHistory = new();
        private readonly object _lockObject = new object();
        private CancellationTokenSource? _monitorCts;
        private Task? _monitorTask;

        /// <summary>Event raised when diagnostics complete</summary>
        public event EventHandler<DiagnosticReport>? DiagnosticsCompleted;

        /// <summary>Event raised when new metrics are collected</summary>
        public event EventHandler<PerformanceMetrics>? MetricsCollected;

        private DiagnosticsManager()
        {
            // Register default checks
            RegisterCheck(new SystemInfoCheck());
            RegisterCheck(new MemoryCheck());
            RegisterCheck(new DiskSpaceCheck());
        }

        /// <summary>Registers a diagnostic check</summary>
        public void RegisterCheck(IDiagnosticCheck check)
        {
            lock (_lockObject)
            {
                _checks.Add(check);
            }
        }

        /// <summary>Gets all registered checks</summary>
        public IReadOnlyList<IDiagnosticCheck> Checks => _checks.AsReadOnly();

        /// <summary>Gets all reports</summary>
        public IReadOnlyList<DiagnosticReport> Reports => _reports.AsReadOnly();

        /// <summary>Gets metrics history</summary>
        public IReadOnlyList<PerformanceMetrics> MetricsHistory => _metricsHistory.AsReadOnly();

        /// <summary>Runs all diagnostic checks</summary>
        public async Task<DiagnosticReport> RunAllChecksAsync(CancellationToken ct = default)
        {
            var report = new DiagnosticReport();
            var sw = Stopwatch.StartNew();

            List<IDiagnosticCheck> checks;
            lock (_lockObject)
            {
                checks = _checks.ToList();
            }

            foreach (var check in checks)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var result = await check.RunAsync(ct);
                    report.Results.Add(result);
                }
                catch (Exception ex)
                {
                    report.Results.Add(new DiagnosticResult
                    {
                        Name = check.Name,
                        Category = check.Category,
                        Status = DiagnosticStatus.Failed,
                        Message = $"Check failed: {ex.Message}"
                    });
                }
            }

            sw.Stop();
            report.TotalDuration = sw.Elapsed;

            // Calculate statistics
            report.Statistics["Total"] = report.Results.Count;
            report.Statistics["Passed"] = report.Results.Count(r => r.Status == DiagnosticStatus.Passed);
            report.Statistics["Warning"] = report.Results.Count(r => r.Status == DiagnosticStatus.Warning);
            report.Statistics["Failed"] = report.Results.Count(r => r.Status == DiagnosticStatus.Failed);

            // Determine overall status
            if (report.Results.Any(r => r.Status == DiagnosticStatus.Failed))
                report.OverallStatus = DiagnosticStatus.Failed;
            else if (report.Results.Any(r => r.Status == DiagnosticStatus.Warning))
                report.OverallStatus = DiagnosticStatus.Warning;
            else
                report.OverallStatus = DiagnosticStatus.Passed;

            lock (_lockObject)
            {
                _reports.Add(report);
            }

            DiagnosticsCompleted?.Invoke(this, report);
            return report;
        }

        /// <summary>Runs a specific check by name</summary>
        public async Task<DiagnosticResult?> RunCheckAsync(string checkName, CancellationToken ct = default)
        {
            IDiagnosticCheck? check;
            lock (_lockObject)
            {
                check = _checks.FirstOrDefault(c => c.Name == checkName);
            }

            if (check == null) return null;

            return await check.RunAsync(ct);
        }

        /// <summary>Collects current performance metrics</summary>
        public PerformanceMetrics CollectMetrics()
        {
            var process = Process.GetCurrentProcess();
            
            var metrics = new PerformanceMetrics
            {
                MemoryUsageMB = process.WorkingSet64 / (1024 * 1024),
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                GCCollections = new[]
                {
                    GC.CollectionCount(0),
                    GC.CollectionCount(1),
                    GC.CollectionCount(2)
                }
            };

            lock (_lockObject)
            {
                _metricsHistory.Add(metrics);
                
                // Keep only last 1000 entries
                while (_metricsHistory.Count > 1000)
                {
                    _metricsHistory.RemoveAt(0);
                }
            }

            MetricsCollected?.Invoke(this, metrics);
            return metrics;
        }

        /// <summary>Starts continuous metrics monitoring</summary>
        public void StartMonitoring(int intervalMs = 1000)
        {
            if (_monitorTask != null) return;

            _monitorCts = new CancellationTokenSource();
            _monitorTask = MonitorLoopAsync(_monitorCts.Token, intervalMs);
        }

        /// <summary>Stops metrics monitoring</summary>
        public void StopMonitoring()
        {
            _monitorCts?.Cancel();
            _monitorTask?.Wait(5000);
            _monitorTask = null;
        }

        private async Task MonitorLoopAsync(CancellationToken ct, int intervalMs)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    CollectMetrics();
                    await Task.Delay(intervalMs, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Continue on errors
                }
            }
        }

        /// <summary>Exports a report to file</summary>
        public void ExportReport(string reportId, string filePath, string format = "json")
        {
            DiagnosticReport? report;
            lock (_lockObject)
            {
                report = _reports.FirstOrDefault(r => r.Id == reportId);
            }

            if (report == null) return;

            string content;
            if (format.ToLower() == "json")
            {
                content = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Diagnostic Report - {report.Id}");
                sb.AppendLine($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"System: {report.SystemName}");
                sb.AppendLine($"Overall Status: {report.OverallStatus}");
                sb.AppendLine($"Duration: {report.TotalDuration.TotalMilliseconds:F0} ms");
                sb.AppendLine();
                sb.AppendLine("Results:");
                foreach (var result in report.Results)
                {
                    sb.AppendLine($"  [{result.Status}] {result.Name}: {result.Message}");
                    if (!string.IsNullOrEmpty(result.Details))
                    {
                        sb.AppendLine($"    Details: {result.Details.Replace("\n", "\n    ")}");
                    }
                }
                content = sb.ToString();
            }

            File.WriteAllText(filePath, content);
        }

        /// <summary>Clears diagnostic history</summary>
        public void ClearHistory()
        {
            lock (_lockObject)
            {
                _reports.Clear();
                _metricsHistory.Clear();
            }
        }
    }
}
