// SequenceFileTypes.cs - Support for different sequence file formats
// Provides file type handlers for sequences

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TestStandClone.Core.SequenceFileTypes
{
    /// <summary>
    /// Sequence file format types
    /// </summary>
    public enum SequenceFileFormat
    {
        /// <summary>JSON format (.seq.json)</summary>
        Json,
        /// <summary>XML format (.seq.xml)</summary>
        Xml,
        /// <summary>Binary format (.seq)</summary>
        Binary,
        /// <summary>Compressed format (.seq.gz)</summary>
        Compressed,
        /// <summary>Legacy TestStand format (read only)</summary>
        LegacyTestStand
    }

    /// <summary>
    /// Sequence file metadata
    /// </summary>
    public class SequenceFileInfo
    {
        /// <summary>File path</summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>File format</summary>
        public SequenceFileFormat Format { get; set; }

        /// <summary>File size in bytes</summary>
        public long FileSize { get; set; }

        /// <summary>Creation date</summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>Last modified date</summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>Sequence name</summary>
        public string SequenceName { get; set; } = string.Empty;

        /// <summary>Sequence version</summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>Author</summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>Description</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Number of steps</summary>
        public int StepCount { get; set; }

        /// <summary>Is read only</summary>
        public bool IsReadOnly { get; set; }
    }

    /// <summary>
    /// Interface for sequence file handlers
    /// </summary>
    public interface ISequenceFileHandler
    {
        /// <summary>Supported format</summary>
        SequenceFileFormat Format { get; }

        /// <summary>File extension</summary>
        string Extension { get; }

        /// <summary>File filter for dialogs</summary>
        string FileFilter { get; }

        /// <summary>Can read files</summary>
        bool CanRead { get; }

        /// <summary>Can write files</summary>
        bool CanWrite { get; }

        /// <summary>Save a sequence to file</summary>
        Task SaveAsync(Sequence sequence, string filePath);

        /// <summary>Load a sequence from file</summary>
        Task<Sequence> LoadAsync(string filePath);

        /// <summary>Get file info without fully loading</summary>
        Task<SequenceFileInfo> GetInfoAsync(string filePath);
    }

    /// <summary>
    /// JSON sequence file handler
    /// </summary>
    public class JsonSequenceFileHandler : ISequenceFileHandler
    {
        public SequenceFileFormat Format => SequenceFileFormat.Json;
        public string Extension => ".seq.json";
        public string FileFilter => "JSON Sequence Files (*.seq.json)|*.seq.json";
        public bool CanRead => true;
        public bool CanWrite => true;

        public async Task SaveAsync(Sequence sequence, string filePath)
        {
            var dto = SequenceToDto(sequence);
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var json = JsonSerializer.Serialize(dto, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<Sequence> LoadAsync(string filePath)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            var json = await File.ReadAllTextAsync(filePath);
            var dto = JsonSerializer.Deserialize<SequenceDto>(json, options);

            if (dto == null)
                throw new InvalidOperationException("Failed to deserialize sequence");

            return DtoToSequence(dto);
        }

        public async Task<SequenceFileInfo> GetInfoAsync(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            
            var info = new SequenceFileInfo
            {
                FilePath = filePath,
                Format = SequenceFileFormat.Json,
                FileSize = fileInfo.Length,
                CreatedDate = fileInfo.CreationTime,
                ModifiedDate = fileInfo.LastWriteTime,
                IsReadOnly = fileInfo.IsReadOnly
            };

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                using var doc = JsonDocument.Parse(json);
                
                var root = doc.RootElement;
                info.SequenceName = root.TryGetProperty("Name", out var name) ? name.GetString() ?? "" : "";
                info.Version = root.TryGetProperty("Version", out var version) ? version.GetString() ?? "" : "";
                info.Author = root.TryGetProperty("Author", out var author) ? author.GetString() ?? "" : "";
                info.Description = root.TryGetProperty("Description", out var desc) ? desc.GetString() ?? "" : "";
                
                if (root.TryGetProperty("Steps", out var steps) && steps.ValueKind == JsonValueKind.Array)
                {
                    info.StepCount = steps.GetArrayLength();
                }
            }
            catch
            {
                // Return basic info if parsing fails
            }

            return info;
        }

        private SequenceDto SequenceToDto(Sequence sequence)
        {
            return new SequenceDto
            {
                Name = sequence.Name,
                Version = "1.0.0",
                Steps = sequence.Steps.Select(StepToDto).ToList()
            };
        }

        private StepDto StepToDto(TestStep step)
        {
            return new StepDto
            {
                Id = step.Id.ToString(),
                Name = step.Name,
                TypeName = step.GetType().Name,
                Status = step.Status.ToString()
            };
        }

        private Sequence DtoToSequence(SequenceDto dto)
        {
            var sequence = new Sequence { Name = dto.Name };
            
            foreach (var stepDto in dto.Steps)
            {
                var step = CreateStep(stepDto);
                if (step != null)
                {
                    sequence.Steps.Add(step);
                }
            }

            return sequence;
        }

        private TestStep? CreateStep(StepDto dto)
        {
            // Basic step creation (would need step factory in real implementation)
            return dto.TypeName switch
            {
                "DelayStep" => new DelayStep(dto.Name, 1000),
                "NumericLimitStep" => new NumericLimitStep(dto.Name, 0, 100),
                _ => null
            };
        }
    }

    /// <summary>
    /// XML sequence file handler
    /// </summary>
    public class XmlSequenceFileHandler : ISequenceFileHandler
    {
        public SequenceFileFormat Format => SequenceFileFormat.Xml;
        public string Extension => ".seq.xml";
        public string FileFilter => "XML Sequence Files (*.seq.xml)|*.seq.xml";
        public bool CanRead => true;
        public bool CanWrite => true;

        public async Task SaveAsync(Sequence sequence, string filePath)
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("Sequence",
                    new XAttribute("Name", sequence.Name),
                    new XAttribute("Version", "1.0.0"),
                    new XElement("Steps",
                        sequence.Steps.Select(step => new XElement("Step",
                            new XAttribute("Id", step.Id),
                            new XAttribute("Name", step.Name),
                            new XAttribute("Type", step.GetType().Name)
                        ))
                    )
                )
            );

            await Task.Run(() => doc.Save(filePath));
        }

        public async Task<Sequence> LoadAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                var doc = XDocument.Load(filePath);
                var root = doc.Root;

                if (root == null)
                    throw new InvalidOperationException("Invalid XML structure");

                var sequence = new Sequence
                {
                    Name = root.Attribute("Name")?.Value ?? "Unnamed"
                };

                var stepsElement = root.Element("Steps");
                if (stepsElement != null)
                {
                    foreach (var stepElement in stepsElement.Elements("Step"))
                    {
                        var name = stepElement.Attribute("Name")?.Value ?? "Step";
                        var typeName = stepElement.Attribute("Type")?.Value;

                        var step = typeName switch
                        {
                            "DelayStep" => (TestStep)new DelayStep(name, 1000),
                            "NumericLimitStep" => new NumericLimitStep(name, 0, 100),
                            _ => null
                        };

                        if (step != null)
                        {
                            sequence.Steps.Add(step);
                        }
                    }
                }

                return sequence;
            });
        }

        public async Task<SequenceFileInfo> GetInfoAsync(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            
            var info = new SequenceFileInfo
            {
                FilePath = filePath,
                Format = SequenceFileFormat.Xml,
                FileSize = fileInfo.Length,
                CreatedDate = fileInfo.CreationTime,
                ModifiedDate = fileInfo.LastWriteTime,
                IsReadOnly = fileInfo.IsReadOnly
            };

            try
            {
                await Task.Run(() =>
                {
                    var doc = XDocument.Load(filePath);
                    var root = doc.Root;

                    if (root != null)
                    {
                        info.SequenceName = root.Attribute("Name")?.Value ?? "";
                        info.Version = root.Attribute("Version")?.Value ?? "";

                        var steps = root.Element("Steps");
                        if (steps != null)
                        {
                            info.StepCount = steps.Elements("Step").Count();
                        }
                    }
                });
            }
            catch
            {
                // Return basic info if parsing fails
            }

            return info;
        }
    }

    /// <summary>
    /// DTO for sequence serialization
    /// </summary>
    public class SequenceDto
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<StepDto> Steps { get; set; } = new();
    }

    /// <summary>
    /// DTO for step serialization
    /// </summary>
    public class StepDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object?> Properties { get; set; } = new();
    }

    /// <summary>
    /// Sequence file type manager
    /// </summary>
    public class SequenceFileTypeManager
    {
        private static SequenceFileTypeManager? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<SequenceFileFormat, ISequenceFileHandler> _handlers = new();

        public static SequenceFileTypeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new SequenceFileTypeManager();
                    }
                }
                return _instance;
            }
        }

        private SequenceFileTypeManager()
        {
            // Register default handlers
            RegisterHandler(new JsonSequenceFileHandler());
            RegisterHandler(new XmlSequenceFileHandler());
        }

        /// <summary>
        /// Register a file handler
        /// </summary>
        public void RegisterHandler(ISequenceFileHandler handler)
        {
            _handlers[handler.Format] = handler;
        }

        /// <summary>
        /// Get a handler by format
        /// </summary>
        public ISequenceFileHandler? GetHandler(SequenceFileFormat format)
        {
            _handlers.TryGetValue(format, out var handler);
            return handler;
        }

        /// <summary>
        /// Get handler for file extension
        /// </summary>
        public ISequenceFileHandler? GetHandlerForExtension(string extension)
        {
            return _handlers.Values.FirstOrDefault(h => 
                extension.EndsWith(h.Extension, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get all file filters for dialogs
        /// </summary>
        public string GetAllFileFilters()
        {
            var filters = _handlers.Values
                .Where(h => h.CanRead)
                .Select(h => h.FileFilter);
            
            var allFiles = string.Join(";", _handlers.Values
                .Where(h => h.CanRead)
                .Select(h => "*" + h.Extension));

            return $"All Sequence Files|{allFiles}|" + string.Join("|", filters);
        }

        /// <summary>
        /// Get save file filters
        /// </summary>
        public string GetSaveFileFilters()
        {
            var filters = _handlers.Values
                .Where(h => h.CanWrite)
                .Select(h => h.FileFilter);

            return string.Join("|", filters);
        }

        /// <summary>
        /// Determine format from file path
        /// </summary>
        public SequenceFileFormat? DetermineFormat(string filePath)
        {
            var handler = GetHandlerForExtension(filePath);
            return handler?.Format;
        }

        /// <summary>
        /// Save sequence to file
        /// </summary>
        public async Task SaveAsync(Sequence sequence, string filePath, SequenceFileFormat? format = null)
        {
            var handler = format.HasValue 
                ? GetHandler(format.Value) 
                : GetHandlerForExtension(filePath);

            if (handler == null)
                throw new InvalidOperationException($"No handler found for format: {format ?? DetermineFormat(filePath)}");

            if (!handler.CanWrite)
                throw new InvalidOperationException($"Handler for format {handler.Format} does not support writing");

            await handler.SaveAsync(sequence, filePath);
        }

        /// <summary>
        /// Load sequence from file
        /// </summary>
        public async Task<Sequence> LoadAsync(string filePath)
        {
            var handler = GetHandlerForExtension(filePath);

            if (handler == null)
                throw new InvalidOperationException($"No handler found for file: {filePath}");

            if (!handler.CanRead)
                throw new InvalidOperationException($"Handler for format {handler.Format} does not support reading");

            return await handler.LoadAsync(filePath);
        }

        /// <summary>
        /// Get file info
        /// </summary>
        public async Task<SequenceFileInfo> GetInfoAsync(string filePath)
        {
            var handler = GetHandlerForExtension(filePath);

            if (handler == null)
            {
                // Return basic file info
                var fileInfo = new FileInfo(filePath);
                return new SequenceFileInfo
                {
                    FilePath = filePath,
                    FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                    CreatedDate = fileInfo.Exists ? fileInfo.CreationTime : DateTime.MinValue,
                    ModifiedDate = fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.MinValue
                };
            }

            return await handler.GetInfoAsync(filePath);
        }

        /// <summary>
        /// Get all registered handlers
        /// </summary>
        public IEnumerable<ISequenceFileHandler> GetAllHandlers()
        {
            return _handlers.Values;
        }
    }
}
