using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestStandClone.Core.LabelPrinting
{
    /// <summary>
    /// Label type enumeration
    /// </summary>
    public enum LabelType
    {
        ProductLabel,
        SerialNumberLabel,
        QualityLabel,
        ShippingLabel,
        WarningLabel,
        Custom
    }

    /// <summary>
    /// Print status enumeration
    /// </summary>
    public enum PrintStatus
    {
        Pending,
        Printing,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Represents a label template
    /// </summary>
    public class LabelTemplate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public LabelType Type { get; set; }
        public string TemplateContent { get; set; } = string.Empty;
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public List<string> RequiredFields { get; set; } = new();
        public Dictionary<string, string> DefaultValues { get; set; } = new();
    }

    /// <summary>
    /// Represents a label to be printed
    /// </summary>
    public class Label
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TemplateId { get; set; } = string.Empty;
        public Dictionary<string, string> FieldValues { get; set; } = new();
        public int Copies { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Print job
    /// </summary>
    public class PrintJob
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PrinterId { get; set; } = string.Empty;
        public List<Label> Labels { get; set; } = new();
        public PrintStatus Status { get; set; } = PrintStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int PrintedCount { get; set; }
        public int TotalCount => Labels.Sum(l => l.Copies);
    }

    /// <summary>
    /// Represents a label printer
    /// </summary>
    public class LabelPrinter
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public bool IsOnline { get; set; }
        public string CurrentStatus { get; set; } = "Ready";
        public int QueuedJobs { get; set; }
    }

    /// <summary>
    /// Printer driver interface
    /// </summary>
    public interface ILabelPrinterDriver
    {
        string PrinterId { get; }
        bool IsConnected { get; }
        Task<bool> ConnectAsync();
        Task DisconnectAsync();
        Task<bool> PrintLabelAsync(string templateContent, Dictionary<string, string> fieldValues);
        Task<string> GetStatusAsync();
    }

    /// <summary>
    /// Simulated printer driver for testing
    /// </summary>
    public class SimulatedLabelPrinterDriver : ILabelPrinterDriver
    {
        public string PrinterId { get; }
        public bool IsConnected { get; private set; }

        public SimulatedLabelPrinterDriver(string printerId)
        {
            PrinterId = printerId;
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

        public async Task<bool> PrintLabelAsync(string templateContent, Dictionary<string, string> fieldValues)
        {
            await Task.Delay(500);
            Console.WriteLine($"[Simulated Print] Template: {templateContent.Substring(0, Math.Min(50, templateContent.Length))}...");
            foreach (var field in fieldValues)
            {
                Console.WriteLine($"  {field.Key}: {field.Value}");
            }
            return true;
        }

        public Task<string> GetStatusAsync()
        {
            return Task.FromResult("Ready");
        }
    }

    /// <summary>
    /// Label printing manager
    /// </summary>
    public class LabelPrintingManager
    {
        private static LabelPrintingManager? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, LabelTemplate> _templates = new();
        private readonly Dictionary<string, LabelPrinter> _printers = new();
        private readonly Dictionary<string, ILabelPrinterDriver> _drivers = new();
        private readonly List<PrintJob> _printJobs = new();
        private readonly Queue<PrintJob> _printQueue = new();

        public static LabelPrintingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new LabelPrintingManager();
                    }
                }
                return _instance;
            }
        }

        public event EventHandler<PrintJob>? PrintJobStarted;
        public event EventHandler<PrintJob>? PrintJobCompleted;
        public event EventHandler<PrintJob>? PrintJobFailed;

        /// <summary>
        /// Register a label template
        /// </summary>
        public void RegisterTemplate(LabelTemplate template)
        {
            lock (_lock)
            {
                _templates[template.Id] = template;
            }
        }

        /// <summary>
        /// Get a template by ID
        /// </summary>
        public LabelTemplate? GetTemplate(string templateId)
        {
            _templates.TryGetValue(templateId, out var template);
            return template;
        }

        /// <summary>
        /// Get all templates
        /// </summary>
        public IReadOnlyList<LabelTemplate> GetAllTemplates()
        {
            lock (_lock)
            {
                return _templates.Values.ToList();
            }
        }

        /// <summary>
        /// Register a printer
        /// </summary>
        public void RegisterPrinter(LabelPrinter printer, ILabelPrinterDriver driver)
        {
            lock (_lock)
            {
                _printers[printer.Id] = printer;
                _drivers[printer.Id] = driver;
            }
        }

        /// <summary>
        /// Connect to a printer
        /// </summary>
        public async Task<bool> ConnectPrinterAsync(string printerId)
        {
            if (!_drivers.TryGetValue(printerId, out var driver))
            {
                return false;
            }

            var connected = await driver.ConnectAsync();
            
            if (connected && _printers.TryGetValue(printerId, out var printer))
            {
                printer.IsConnected = true;
                printer.IsOnline = true;
            }

            return connected;
        }

        /// <summary>
        /// Create a label from template
        /// </summary>
        public Label CreateLabel(string templateId, Dictionary<string, string> fieldValues, int copies = 1)
        {
            var template = GetTemplate(templateId);
            if (template == null)
            {
                throw new ArgumentException($"Template {templateId} not found");
            }

            var mergedValues = new Dictionary<string, string>(template.DefaultValues);
            foreach (var field in fieldValues)
            {
                mergedValues[field.Key] = field.Value;
            }

            foreach (var required in template.RequiredFields)
            {
                if (!mergedValues.ContainsKey(required) || string.IsNullOrEmpty(mergedValues[required]))
                {
                    throw new ArgumentException($"Required field '{required}' is missing");
                }
            }

            return new Label
            {
                TemplateId = templateId,
                FieldValues = mergedValues,
                Copies = copies
            };
        }

        /// <summary>
        /// Create a print job
        /// </summary>
        public PrintJob CreatePrintJob(string printerId, IEnumerable<Label> labels)
        {
            var job = new PrintJob
            {
                PrinterId = printerId,
                Labels = labels.ToList()
            };

            lock (_lock)
            {
                _printJobs.Add(job);
            }

            return job;
        }

        /// <summary>
        /// Submit a print job to the queue
        /// </summary>
        public void SubmitPrintJob(PrintJob job)
        {
            lock (_lock)
            {
                _printQueue.Enqueue(job);
                
                if (_printers.TryGetValue(job.PrinterId, out var printer))
                {
                    printer.QueuedJobs++;
                }
            }
        }

        /// <summary>
        /// Process the print queue
        /// </summary>
        public async Task ProcessPrintQueueAsync()
        {
            while (true)
            {
                PrintJob? job = null;
                
                lock (_lock)
                {
                    if (_printQueue.Count > 0)
                    {
                        job = _printQueue.Dequeue();
                    }
                }

                if (job == null)
                {
                    break;
                }

                await ExecutePrintJobAsync(job);
            }
        }

        /// <summary>
        /// Execute a print job
        /// </summary>
        public async Task ExecutePrintJobAsync(PrintJob job)
        {
            if (!_drivers.TryGetValue(job.PrinterId, out var driver))
            {
                job.Status = PrintStatus.Failed;
                job.ErrorMessage = "Printer not found";
                PrintJobFailed?.Invoke(this, job);
                return;
            }

            job.Status = PrintStatus.Printing;
            job.StartedAt = DateTime.Now;
            PrintJobStarted?.Invoke(this, job);

            try
            {
                foreach (var label in job.Labels)
                {
                    var template = GetTemplate(label.TemplateId);
                    if (template == null) continue;

                    for (int i = 0; i < label.Copies; i++)
                    {
                        var success = await driver.PrintLabelAsync(template.TemplateContent, label.FieldValues);
                        if (success)
                        {
                            job.PrintedCount++;
                        }
                        else
                        {
                            throw new Exception("Print failed");
                        }
                    }
                }

                job.Status = PrintStatus.Completed;
                job.CompletedAt = DateTime.Now;
                PrintJobCompleted?.Invoke(this, job);
            }
            catch (Exception ex)
            {
                job.Status = PrintStatus.Failed;
                job.ErrorMessage = ex.Message;
                job.CompletedAt = DateTime.Now;
                PrintJobFailed?.Invoke(this, job);
            }
            finally
            {
                if (_printers.TryGetValue(job.PrinterId, out var printer))
                {
                    printer.QueuedJobs = Math.Max(0, printer.QueuedJobs - 1);
                }
            }
        }

        /// <summary>
        /// Print a single label immediately
        /// </summary>
        public async Task<bool> PrintLabelAsync(string printerId, string templateId, Dictionary<string, string> fieldValues)
        {
            var label = CreateLabel(templateId, fieldValues);
            var job = CreatePrintJob(printerId, new[] { label });
            await ExecutePrintJobAsync(job);
            return job.Status == PrintStatus.Completed;
        }

        /// <summary>
        /// Get print job history
        /// </summary>
        public IReadOnlyList<PrintJob> GetPrintJobHistory(DateTime? startDate = null, DateTime? endDate = null)
        {
            lock (_lock)
            {
                var query = _printJobs.AsEnumerable();
                
                if (startDate.HasValue)
                    query = query.Where(j => j.CreatedAt >= startDate.Value);
                
                if (endDate.HasValue)
                    query = query.Where(j => j.CreatedAt <= endDate.Value);
                
                return query.ToList();
            }
        }

        /// <summary>
        /// Get all printers
        /// </summary>
        public IReadOnlyList<LabelPrinter> GetAllPrinters()
        {
            lock (_lock)
            {
                return _printers.Values.ToList();
            }
        }
    }
}
