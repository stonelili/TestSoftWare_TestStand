// StepTemplates.cs - Reusable step templates
// Provides template management for step configurations

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TestStandClone.Core.StepTemplates
{
    /// <summary>
    /// Template category
    /// </summary>
    public enum TemplateCategory
    {
        /// <summary>General purpose template</summary>
        General,
        /// <summary>Measurement template</summary>
        Measurement,
        /// <summary>Flow control template</summary>
        FlowControl,
        /// <summary>Instrument I/O template</summary>
        InstrumentIO,
        /// <summary>Synchronization template</summary>
        Synchronization,
        /// <summary>Messaging template</summary>
        Messaging,
        /// <summary>File I/O template</summary>
        FileIO,
        /// <summary>Custom template</summary>
        Custom
    }

    /// <summary>
    /// Represents a step template property
    /// </summary>
    public class TemplateProperty
    {
        /// <summary>Property name</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Property value</summary>
        public object? Value { get; set; }

        /// <summary>Property type name</summary>
        public string TypeName { get; set; } = "System.String";

        /// <summary>Whether the property is editable when using template</summary>
        public bool IsEditable { get; set; } = true;

        /// <summary>Description of the property</summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a step template
    /// </summary>
    public class StepTemplate
    {
        /// <summary>Unique identifier</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Template name</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Template description</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Template category</summary>
        public TemplateCategory Category { get; set; } = TemplateCategory.General;

        /// <summary>Step type to create</summary>
        public string StepTypeName { get; set; } = string.Empty;

        /// <summary>Default step name format (e.g., "Voltage Check {0}")</summary>
        public string DefaultNameFormat { get; set; } = string.Empty;

        /// <summary>Template properties</summary>
        public List<TemplateProperty> Properties { get; set; } = new();

        /// <summary>Tags for searching</summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>Author of the template</summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>Version of the template</summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>Date template was created</summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>Date template was last modified</summary>
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        /// <summary>Whether this is a built-in template</summary>
        public bool IsBuiltIn { get; set; } = false;

        /// <summary>Icon for UI display</summary>
        public string? IconPath { get; set; }
    }

    /// <summary>
    /// Step template library
    /// </summary>
    public class TemplateLibrary
    {
        /// <summary>Library name</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Library description</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Library version</summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>Templates in the library</summary>
        public List<StepTemplate> Templates { get; set; } = new();
    }

    /// <summary>
    /// Template search options
    /// </summary>
    public class TemplateSearchOptions
    {
        /// <summary>Search text</summary>
        public string? SearchText { get; set; }

        /// <summary>Category filter</summary>
        public TemplateCategory? Category { get; set; }

        /// <summary>Tags filter</summary>
        public List<string>? Tags { get; set; }

        /// <summary>Step type filter</summary>
        public string? StepTypeName { get; set; }

        /// <summary>Include built-in templates</summary>
        public bool IncludeBuiltIn { get; set; } = true;

        /// <summary>Maximum results</summary>
        public int? MaxResults { get; set; }
    }

    /// <summary>
    /// Manages step templates
    /// </summary>
    public class TemplateManager
    {
        private static TemplateManager? _instance;
        private static readonly object _lock = new();

        private readonly ConcurrentDictionary<string, StepTemplate> _templates = new();
        private readonly List<TemplateLibrary> _libraries = new();
        private string _storageDirectory = string.Empty;

        public static TemplateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new TemplateManager();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Event raised when a template is added
        /// </summary>
        public event EventHandler<StepTemplate>? TemplateAdded;

        /// <summary>
        /// Event raised when a template is removed
        /// </summary>
        public event EventHandler<string>? TemplateRemoved;

        private TemplateManager()
        {
            InitializeBuiltInTemplates();
        }

        /// <summary>
        /// Set the storage directory for templates
        /// </summary>
        public void SetStorageDirectory(string directory)
        {
            _storageDirectory = directory;
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }
        }

        /// <summary>
        /// Initialize built-in templates
        /// </summary>
        private void InitializeBuiltInTemplates()
        {
            // Numeric Limit template
            AddTemplate(new StepTemplate
            {
                Name = "Numeric Limit Test",
                Description = "Test a numeric value against low and high limits",
                Category = TemplateCategory.Measurement,
                StepTypeName = "NumericLimitStep",
                DefaultNameFormat = "Numeric Test {0}",
                IsBuiltIn = true,
                Properties = new List<TemplateProperty>
                {
                    new TemplateProperty { Name = "LowLimit", Value = 0.0, TypeName = "System.Double", Description = "Low limit" },
                    new TemplateProperty { Name = "HighLimit", Value = 100.0, TypeName = "System.Double", Description = "High limit" }
                },
                Tags = new List<string> { "measurement", "limit", "numeric" }
            });

            // Delay step template
            AddTemplate(new StepTemplate
            {
                Name = "Delay Step",
                Description = "Wait for a specified duration",
                Category = TemplateCategory.FlowControl,
                StepTypeName = "DelayStep",
                DefaultNameFormat = "Delay {0}",
                IsBuiltIn = true,
                Properties = new List<TemplateProperty>
                {
                    new TemplateProperty { Name = "DelayMilliseconds", Value = 1000, TypeName = "System.Int32", Description = "Delay in milliseconds" }
                },
                Tags = new List<string> { "delay", "wait", "timing" }
            });

            // Message popup template
            AddTemplate(new StepTemplate
            {
                Name = "Message Popup",
                Description = "Display a message to the operator",
                Category = TemplateCategory.Messaging,
                StepTypeName = "MessagePopupStep",
                DefaultNameFormat = "Message {0}",
                IsBuiltIn = true,
                Properties = new List<TemplateProperty>
                {
                    new TemplateProperty { Name = "Message", Value = "", TypeName = "System.String", Description = "Message text" },
                    new TemplateProperty { Name = "Title", Value = "Information", TypeName = "System.String", Description = "Popup title" }
                },
                Tags = new List<string> { "message", "popup", "operator" }
            });

            // Pass/Fail step template
            AddTemplate(new StepTemplate
            {
                Name = "Pass/Fail Test",
                Description = "Boolean pass/fail evaluation",
                Category = TemplateCategory.Measurement,
                StepTypeName = "PassFailStep",
                DefaultNameFormat = "Check {0}",
                IsBuiltIn = true,
                Properties = new List<TemplateProperty>
                {
                    new TemplateProperty { Name = "ExpectedResult", Value = true, TypeName = "System.Boolean", Description = "Expected result (pass if match)" }
                },
                Tags = new List<string> { "pass", "fail", "boolean", "check" }
            });

            // String value test template
            AddTemplate(new StepTemplate
            {
                Name = "String Value Test",
                Description = "Compare string value against expected",
                Category = TemplateCategory.Measurement,
                StepTypeName = "StringValueStep",
                DefaultNameFormat = "String Check {0}",
                IsBuiltIn = true,
                Properties = new List<TemplateProperty>
                {
                    new TemplateProperty { Name = "ExpectedValue", Value = "", TypeName = "System.String", Description = "Expected string value" },
                    new TemplateProperty { Name = "CaseSensitive", Value = false, TypeName = "System.Boolean", Description = "Case sensitive comparison" }
                },
                Tags = new List<string> { "string", "text", "comparison" }
            });

            // Instrument query template
            AddTemplate(new StepTemplate
            {
                Name = "Instrument Query",
                Description = "Send SCPI command and read response",
                Category = TemplateCategory.InstrumentIO,
                StepTypeName = "InstrumentStep",
                DefaultNameFormat = "Query {0}",
                IsBuiltIn = true,
                Properties = new List<TemplateProperty>
                {
                    new TemplateProperty { Name = "Command", Value = "*IDN?", TypeName = "System.String", Description = "SCPI command" },
                    new TemplateProperty { Name = "Timeout", Value = 5000, TypeName = "System.Int32", Description = "Timeout in ms" }
                },
                Tags = new List<string> { "instrument", "scpi", "query" }
            });

            // File read template
            AddTemplate(new StepTemplate
            {
                Name = "Read File",
                Description = "Read contents from a file",
                Category = TemplateCategory.FileIO,
                StepTypeName = "FileIOStep",
                DefaultNameFormat = "Read File {0}",
                IsBuiltIn = true,
                Properties = new List<TemplateProperty>
                {
                    new TemplateProperty { Name = "FilePath", Value = "", TypeName = "System.String", Description = "Path to file" },
                    new TemplateProperty { Name = "Operation", Value = "Read", TypeName = "System.String", Description = "File operation" }
                },
                Tags = new List<string> { "file", "read", "io" }
            });
        }

        /// <summary>
        /// Add a template
        /// </summary>
        public void AddTemplate(StepTemplate template)
        {
            template.ModifiedDate = DateTime.Now;
            _templates[template.Id] = template;
            TemplateAdded?.Invoke(this, template);
        }

        /// <summary>
        /// Get a template by ID
        /// </summary>
        public StepTemplate? GetTemplate(string id)
        {
            _templates.TryGetValue(id, out var template);
            return template;
        }

        /// <summary>
        /// Get template by name
        /// </summary>
        public StepTemplate? GetTemplateByName(string name)
        {
            return _templates.Values.FirstOrDefault(t => t.Name == name);
        }

        /// <summary>
        /// Get all templates
        /// </summary>
        public IEnumerable<StepTemplate> GetAllTemplates()
        {
            return _templates.Values;
        }

        /// <summary>
        /// Search templates
        /// </summary>
        public IEnumerable<StepTemplate> SearchTemplates(TemplateSearchOptions options)
        {
            IEnumerable<StepTemplate> query = _templates.Values;

            if (!options.IncludeBuiltIn)
            {
                query = query.Where(t => !t.IsBuiltIn);
            }

            if (options.Category.HasValue)
            {
                query = query.Where(t => t.Category == options.Category.Value);
            }

            if (!string.IsNullOrEmpty(options.StepTypeName))
            {
                query = query.Where(t => t.StepTypeName == options.StepTypeName);
            }

            if (!string.IsNullOrEmpty(options.SearchText))
            {
                var searchLower = options.SearchText.ToLower();
                query = query.Where(t =>
                    t.Name.ToLower().Contains(searchLower) ||
                    t.Description.ToLower().Contains(searchLower) ||
                    t.Tags.Any(tag => tag.ToLower().Contains(searchLower)));
            }

            if (options.Tags != null && options.Tags.Count > 0)
            {
                query = query.Where(t => options.Tags.Any(tag => t.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
            }

            if (options.MaxResults.HasValue)
            {
                query = query.Take(options.MaxResults.Value);
            }

            return query.OrderBy(t => t.Name).ToList();
        }

        /// <summary>
        /// Get templates by category
        /// </summary>
        public IEnumerable<StepTemplate> GetTemplatesByCategory(TemplateCategory category)
        {
            return _templates.Values.Where(t => t.Category == category).OrderBy(t => t.Name);
        }

        /// <summary>
        /// Remove a template
        /// </summary>
        public bool RemoveTemplate(string id)
        {
            if (_templates.TryRemove(id, out var template))
            {
                // Don't allow removing built-in templates
                if (template.IsBuiltIn)
                {
                    _templates[id] = template;
                    return false;
                }

                TemplateRemoved?.Invoke(this, id);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Create a step from a template
        /// </summary>
        public TestStep? CreateStepFromTemplate(string templateId, string? stepName = null)
        {
            if (!_templates.TryGetValue(templateId, out var template))
                return null;

            // Create step instance (basic implementation)
            TestStep? step = template.StepTypeName switch
            {
                "DelayStep" => new DelayStep(stepName ?? string.Format(template.DefaultNameFormat, _stepCounter++), 1000),
                "NumericLimitStep" => new NumericLimitStep(stepName ?? string.Format(template.DefaultNameFormat, _stepCounter++), 0, 100),
                _ => null
            };

            if (step == null)
                return null;

            // Apply template properties
            var stepType = step.GetType();
            foreach (var prop in template.Properties)
            {
                var propInfo = stepType.GetProperty(prop.Name);
                if (propInfo != null && prop.Value != null)
                {
                    try
                    {
                        var value = Convert.ChangeType(prop.Value, propInfo.PropertyType);
                        propInfo.SetValue(step, value);
                    }
                    catch
                    {
                        // Skip if property can't be set
                    }
                }
            }

            return step;
        }

        private int _stepCounter = 1;

        /// <summary>
        /// Save templates to file
        /// </summary>
        public async Task SaveTemplatesAsync()
        {
            if (string.IsNullOrEmpty(_storageDirectory))
            {
                _storageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TestStandClone", "Templates");
                if (!Directory.Exists(_storageDirectory))
                {
                    Directory.CreateDirectory(_storageDirectory);
                }
            }

            var filePath = Path.Combine(_storageDirectory, "templates.json");
            var templates = _templates.Values.Where(t => !t.IsBuiltIn).ToList();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var json = JsonSerializer.Serialize(templates, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Load templates from file
        /// </summary>
        public async Task LoadTemplatesAsync()
        {
            if (string.IsNullOrEmpty(_storageDirectory))
            {
                _storageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TestStandClone", "Templates");
            }

            var filePath = Path.Combine(_storageDirectory, "templates.json");
            if (!File.Exists(filePath))
                return;

            try
            {
                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() }
                };

                var json = await File.ReadAllTextAsync(filePath);
                var templates = JsonSerializer.Deserialize<List<StepTemplate>>(json, options);

                if (templates != null)
                {
                    foreach (var template in templates)
                    {
                        _templates[template.Id] = template;
                    }
                }
            }
            catch
            {
                // Skip if file is invalid
            }
        }

        /// <summary>
        /// Export a template library
        /// </summary>
        public async Task ExportLibraryAsync(string filePath, IEnumerable<string> templateIds, string libraryName)
        {
            var library = new TemplateLibrary
            {
                Name = libraryName,
                Description = $"Exported library containing {templateIds.Count()} templates",
                Templates = templateIds
                    .Select(id => _templates.TryGetValue(id, out var t) ? t : null)
                    .Where(t => t != null)
                    .Cast<StepTemplate>()
                    .ToList()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var json = JsonSerializer.Serialize(library, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Import a template library
        /// </summary>
        public async Task ImportLibraryAsync(string filePath)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            var json = await File.ReadAllTextAsync(filePath);
            var library = JsonSerializer.Deserialize<TemplateLibrary>(json, options);

            if (library != null)
            {
                _libraries.Add(library);

                foreach (var template in library.Templates)
                {
                    template.IsBuiltIn = false;
                    template.Id = Guid.NewGuid().ToString(); // Assign new ID to avoid conflicts
                    _templates[template.Id] = template;
                }
            }
        }
    }
}
