// Copyright (c) TestStand Clone. All rights reserved.
// Sequence import/export functionality

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace TestStandClone.Core.SequenceImportExport
{
    /// <summary>
    /// Supported sequence file formats
    /// </summary>
    public enum SequenceFileFormat
    {
        Json,
        Xml,
        Csv,
        Text
    }

    /// <summary>
    /// Import/export options
    /// </summary>
    public class ImportExportOptions
    {
        public SequenceFileFormat Format { get; set; } = SequenceFileFormat.Json;
        public bool IncludeStepDetails { get; set; } = true;
        public bool IndentOutput { get; set; } = true;
        public Encoding Encoding { get; set; } = Encoding.UTF8;
    }

    /// <summary>
    /// Result of import operation
    /// </summary>
    public class ImportResult
    {
        public bool Success { get; set; }
        public Sequence? Sequence { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Warnings { get; } = new List<string>();
        public int StepsImported { get; set; }

        public static ImportResult Succeeded(Sequence sequence, int stepsImported)
        {
            return new ImportResult { Success = true, Sequence = sequence, StepsImported = stepsImported };
        }

        public static ImportResult Failed(string error)
        {
            return new ImportResult { Success = false, ErrorMessage = error };
        }
    }

    /// <summary>
    /// Sequence data transfer object for serialization
    /// </summary>
    public class SequenceDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public List<StepDto> Steps { get; set; } = new List<StepDto>();
    }

    /// <summary>
    /// Step data transfer object for serialization
    /// </summary>
    public class StepDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool HasBreakpoint { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Manages sequence import/export operations
    /// </summary>
    public sealed class SequenceImportExportManager
    {
        private static readonly Lazy<SequenceImportExportManager> _instance = 
            new Lazy<SequenceImportExportManager>(() => new SequenceImportExportManager());

        public static SequenceImportExportManager Instance => _instance.Value;

        private SequenceImportExportManager() { }

        /// <summary>
        /// Export a sequence to JSON string
        /// </summary>
        public string ExportToJson(Sequence sequence, ImportExportOptions? options = null)
        {
            options ??= new ImportExportOptions { Format = SequenceFileFormat.Json };
            var dto = ConvertToDto(sequence);
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = options.IndentOutput,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(dto, jsonOptions);
        }

        /// <summary>
        /// Export a sequence to XML string
        /// </summary>
        public string ExportToXml(Sequence sequence, ImportExportOptions? options = null)
        {
            options ??= new ImportExportOptions { Format = SequenceFileFormat.Xml };
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings { Indent = options.IndentOutput, Encoding = options.Encoding };
            
            using (var writer = XmlWriter.Create(sb, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Sequence");
                writer.WriteAttributeString("name", sequence.Name);
                writer.WriteStartElement("Description");
                writer.WriteCData(sequence.Description ?? string.Empty);
                writer.WriteEndElement();
                writer.WriteStartElement("Steps");
                foreach (var step in sequence.Steps)
                {
                    writer.WriteStartElement("Step");
                    writer.WriteAttributeString("id", step.Id.ToString());
                    writer.WriteAttributeString("name", step.Name);
                    writer.WriteAttributeString("type", step.GetType().Name);
                    writer.WriteAttributeString("hasBreakpoint", step.HasBreakpoint.ToString().ToLower());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
            return sb.ToString();
        }

        /// <summary>
        /// Export a sequence to CSV string
        /// </summary>
        public string ExportToCsv(Sequence sequence)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Index,Name,Type,Status,HasBreakpoint");
            int index = 1;
            foreach (var step in sequence.Steps)
            {
                sb.AppendLine(string.Format("{0},{1},{2},{3},{4}", 
                    index, EscapeCsv(step.Name), step.GetType().Name, step.Status, step.HasBreakpoint));
                index++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Export a sequence to text string
        /// </summary>
        public string ExportToText(Sequence sequence)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("Sequence: {0}", sequence.Name));
            sb.AppendLine(string.Format("Description: {0}", sequence.Description ?? "N/A"));
            sb.AppendLine(string.Format("Total Steps: {0}", sequence.Steps.Count));
            sb.AppendLine(new string('=', 60));
            sb.AppendLine();
            
            int index = 1;
            foreach (var step in sequence.Steps)
            {
                var bp = step.HasBreakpoint ? " [BP]" : "";
                sb.AppendLine(string.Format("  {0}. {1}{2}", index, step.Name, bp));
                sb.AppendLine(string.Format("      Type: {0}", step.GetType().Name));
                sb.AppendLine(string.Format("      Status: {0}", step.Status));
                index++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Export a sequence to file
        /// </summary>
        public void ExportToFile(Sequence sequence, string filePath, SequenceFileFormat format)
        {
            var options = new ImportExportOptions { Format = format };
            string content = format switch
            {
                SequenceFileFormat.Json => ExportToJson(sequence, options),
                SequenceFileFormat.Xml => ExportToXml(sequence, options),
                SequenceFileFormat.Csv => ExportToCsv(sequence),
                SequenceFileFormat.Text => ExportToText(sequence),
                _ => ExportToJson(sequence, options)
            };
            File.WriteAllText(filePath, content, options.Encoding);
        }

        /// <summary>
        /// Import a sequence from JSON string
        /// </summary>
        public ImportResult ImportFromJson(string content)
        {
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };
                var dto = JsonSerializer.Deserialize<SequenceDto>(content, jsonOptions);
                if (dto == null) return ImportResult.Failed("Failed to parse JSON content");
                var sequence = ConvertFromDto(dto);
                return ImportResult.Succeeded(sequence, dto.Steps.Count);
            }
            catch (Exception ex)
            {
                return ImportResult.Failed(string.Format("JSON import error: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Import a sequence from file
        /// </summary>
        public ImportResult ImportFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return ImportResult.Failed(string.Format("File not found: {0}", filePath));
            
            var extension = Path.GetExtension(filePath).ToLower();
            var content = File.ReadAllText(filePath, Encoding.UTF8);
            
            return extension switch
            {
                ".json" => ImportFromJson(content),
                ".xml" => ImportFromXml(content),
                _ => ImportResult.Failed(string.Format("Unsupported file format: {0}", extension))
            };
        }

        /// <summary>
        /// Import a sequence from XML string
        /// </summary>
        public ImportResult ImportFromXml(string content)
        {
            try
            {
                var sequence = new Sequence();
                var doc = new XmlDocument();
                doc.LoadXml(content);
                
                var seqNode = doc.SelectSingleNode("//Sequence");
                if (seqNode?.Attributes?["name"] != null)
                    sequence.Name = seqNode.Attributes["name"]!.Value;
                
                var descNode = doc.SelectSingleNode("//Description");
                if (descNode != null)
                    sequence.Description = descNode.InnerText;
                
                var stepNodes = doc.SelectNodes("//Step");
                if (stepNodes != null)
                {
                    foreach (XmlNode stepNode in stepNodes)
                    {
                        var name = stepNode.Attributes?["name"]?.Value ?? "Step";
                        var type = stepNode.Attributes?["type"]?.Value ?? "DelayStep";
                        
                        TestStep step = type switch
                        {
                            "NumericLimitStep" => new NumericLimitStep(name, 0, 100),
                            _ => new DelayStep(name, 1000)
                        };
                        
                        if (stepNode.Attributes?["hasBreakpoint"]?.Value == "true")
                            step.HasBreakpoint = true;
                        
                        sequence.Steps.Add(step);
                    }
                }
                
                return ImportResult.Succeeded(sequence, sequence.Steps.Count);
            }
            catch (Exception ex)
            {
                return ImportResult.Failed(string.Format("XML import error: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Get file filter string for dialogs
        /// </summary>
        public string GetFileFilter()
        {
            return "JSON Files (*.json)|*.json|XML Files (*.xml)|*.xml|CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
        }

        private SequenceDto ConvertToDto(Sequence sequence)
        {
            var dto = new SequenceDto { Name = sequence.Name, Description = sequence.Description };
            foreach (var step in sequence.Steps)
            {
                dto.Steps.Add(new StepDto
                {
                    Id = step.Id.ToString(),
                    Name = step.Name,
                    Type = step.GetType().Name,
                    HasBreakpoint = step.HasBreakpoint
                });
            }
            return dto;
        }

        private Sequence ConvertFromDto(SequenceDto dto)
        {
            var sequence = new Sequence { Name = dto.Name, Description = dto.Description };
            foreach (var stepDto in dto.Steps)
            {
                TestStep step;
                switch (stepDto.Type)
                {
                    case "DelayStep":
                        int delayMs = 1000;
                        if (stepDto.Properties.TryGetValue("DelayMilliseconds", out var ms))
                        {
                            if (ms is JsonElement je) delayMs = je.GetInt32();
                            else if (ms is int intMs) delayMs = intMs;
                        }
                        step = new DelayStep(stepDto.Name, delayMs);
                        break;
                    case "NumericLimitStep":
                        double low = 0, high = 100;
                        if (stepDto.Properties.TryGetValue("LowLimit", out var lowVal))
                        {
                            if (lowVal is JsonElement jeLow) low = jeLow.GetDouble();
                            else if (lowVal is double dLow) low = dLow;
                        }
                        if (stepDto.Properties.TryGetValue("HighLimit", out var highVal))
                        {
                            if (highVal is JsonElement jeHigh) high = jeHigh.GetDouble();
                            else if (highVal is double dHigh) high = dHigh;
                        }
                        step = new NumericLimitStep(stepDto.Name, low, high);
                        break;
                    default:
                        step = new DelayStep(stepDto.Name, 100);
                        break;
                }
                step.HasBreakpoint = stepDto.HasBreakpoint;
                sequence.Steps.Add(step);
            }
            return sequence;
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return string.Format("\"{0}\"", value.Replace("\"", "\"\""));
            return value;
        }
    }
}
