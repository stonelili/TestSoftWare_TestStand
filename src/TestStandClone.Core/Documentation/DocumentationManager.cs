using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace TestStandClone.Core.Documentation
{
    /// <summary>
    /// Documentation element type
    /// </summary>
    public enum DocElementType
    {
        /// <summary>Sequence documentation</summary>
        Sequence,
        /// <summary>Step documentation</summary>
        Step,
        /// <summary>Variable documentation</summary>
        Variable,
        /// <summary>Parameter documentation</summary>
        Parameter,
        /// <summary>Step type documentation</summary>
        StepType,
        /// <summary>General note</summary>
        Note,
        /// <summary>Warning</summary>
        Warning,
        /// <summary>Example</summary>
        Example
    }

    /// <summary>
    /// Documentation format
    /// </summary>
    public enum DocFormat
    {
        /// <summary>Plain text</summary>
        Text,
        /// <summary>Markdown</summary>
        Markdown,
        /// <summary>HTML</summary>
        Html,
        /// <summary>PDF (requires external library)</summary>
        Pdf
    }

    /// <summary>
    /// Individual documentation entry
    /// </summary>
    public class DocEntry
    {
        /// <summary>Unique identifier</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>Element type</summary>
        public DocElementType Type { get; set; }
        /// <summary>Target element ID</summary>
        public string TargetId { get; set; } = string.Empty;
        /// <summary>Target element name</summary>
        public string TargetName { get; set; } = string.Empty;
        /// <summary>Title</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Description</summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>Author</summary>
        public string Author { get; set; } = string.Empty;
        /// <summary>Created date</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        /// <summary>Last modified date</summary>
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
        /// <summary>Tags for categorization</summary>
        public List<string> Tags { get; set; } = new();
        /// <summary>Related entry IDs</summary>
        public List<string> RelatedEntries { get; set; } = new();
        /// <summary>Code examples</summary>
        public List<string> Examples { get; set; } = new();
        /// <summary>Warnings or notes</summary>
        public List<string> Notes { get; set; } = new();
    }

    /// <summary>
    /// Documentation section in a document
    /// </summary>
    public class DocSection
    {
        /// <summary>Section title</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Section level (1-6)</summary>
        public int Level { get; set; } = 1;
        /// <summary>Section content</summary>
        public string Content { get; set; } = string.Empty;
        /// <summary>Child sections</summary>
        public List<DocSection> SubSections { get; set; } = new();
        /// <summary>Entries in this section</summary>
        public List<DocEntry> Entries { get; set; } = new();
    }

    /// <summary>
    /// Complete documentation document
    /// </summary>
    public class DocDocument
    {
        /// <summary>Document ID</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>Document title</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Document subtitle</summary>
        public string Subtitle { get; set; } = string.Empty;
        /// <summary>Author</summary>
        public string Author { get; set; } = string.Empty;
        /// <summary>Version</summary>
        public string Version { get; set; } = "1.0";
        /// <summary>Created date</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        /// <summary>Last modified date</summary>
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
        /// <summary>Abstract/Summary</summary>
        public string Abstract { get; set; } = string.Empty;
        /// <summary>Table of contents</summary>
        public List<string> TableOfContents { get; set; } = new();
        /// <summary>Document sections</summary>
        public List<DocSection> Sections { get; set; } = new();
        /// <summary>All entries</summary>
        public List<DocEntry> Entries { get; set; } = new();
        /// <summary>Keywords</summary>
        public List<string> Keywords { get; set; } = new();
    }

    /// <summary>
    /// Interface for documentation generators
    /// </summary>
    public interface IDocGenerator
    {
        /// <summary>Gets the output format</summary>
        DocFormat Format { get; }
        /// <summary>Generates documentation for a sequence</summary>
        string GenerateSequenceDoc(Sequence sequence, DocDocument? template = null);
        /// <summary>Generates documentation from a document</summary>
        string GenerateFromDocument(DocDocument document);
        /// <summary>Gets file extension for this format</summary>
        string FileExtension { get; }
    }

    /// <summary>
    /// Markdown documentation generator
    /// </summary>
    public class MarkdownDocGenerator : IDocGenerator
    {
        /// <inheritdoc/>
        public DocFormat Format => DocFormat.Markdown;
        /// <inheritdoc/>
        public string FileExtension => ".md";

        /// <inheritdoc/>
        public string GenerateSequenceDoc(Sequence sequence, DocDocument? template = null)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"# {sequence.Name}");
            sb.AppendLine();
            sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            
            sb.AppendLine("## Overview");
            sb.AppendLine();
            sb.AppendLine($"- **Name:** {sequence.Name}");
            sb.AppendLine($"- **Steps:** {sequence.Steps.Count}");
            sb.AppendLine();
            
            sb.AppendLine("## Steps");
            sb.AppendLine();
            sb.AppendLine("| # | Name | Type | Status |");
            sb.AppendLine("|---|------|------|--------|");
            
            int index = 1;
            foreach (var step in sequence.Steps)
            {
                sb.AppendLine($"| {index} | {step.Name} | {step.GetType().Name} | {step.Status} |");
                index++;
            }
            
            sb.AppendLine();
            sb.AppendLine("## Step Details");
            sb.AppendLine();
            
            index = 1;
            foreach (var step in sequence.Steps)
            {
                sb.AppendLine($"### {index}. {step.Name}");
                sb.AppendLine();
                sb.AppendLine($"- **Type:** `{step.GetType().Name}`");
                sb.AppendLine($"- **ID:** `{step.Id}`");
                sb.AppendLine();
                index++;
            }
            
            return sb.ToString();
        }

        /// <inheritdoc/>
        public string GenerateFromDocument(DocDocument document)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"# {document.Title}");
            if (!string.IsNullOrEmpty(document.Subtitle))
            {
                sb.AppendLine($"## {document.Subtitle}");
            }
            sb.AppendLine();
            sb.AppendLine($"**Author:** {document.Author}");
            sb.AppendLine($"**Version:** {document.Version}");
            sb.AppendLine($"**Date:** {document.ModifiedAt:yyyy-MM-dd}");
            sb.AppendLine();
            
            if (!string.IsNullOrEmpty(document.Abstract))
            {
                sb.AppendLine("## Abstract");
                sb.AppendLine();
                sb.AppendLine(document.Abstract);
                sb.AppendLine();
            }
            
            if (document.TableOfContents.Count > 0)
            {
                sb.AppendLine("## Table of Contents");
                sb.AppendLine();
                foreach (var item in document.TableOfContents)
                {
                    sb.AppendLine($"- {item}");
                }
                sb.AppendLine();
            }
            
            foreach (var section in document.Sections)
            {
                GenerateSection(sb, section);
            }
            
            return sb.ToString();
        }

        private void GenerateSection(StringBuilder sb, DocSection section)
        {
            string header = new string('#', Math.Min(section.Level, 6));
            sb.AppendLine($"{header} {section.Title}");
            sb.AppendLine();
            
            if (!string.IsNullOrEmpty(section.Content))
            {
                sb.AppendLine(section.Content);
                sb.AppendLine();
            }
            
            foreach (var entry in section.Entries)
            {
                GenerateEntry(sb, entry);
            }
            
            foreach (var subSection in section.SubSections)
            {
                GenerateSection(sb, subSection);
            }
        }

        private void GenerateEntry(StringBuilder sb, DocEntry entry)
        {
            sb.AppendLine($"**{entry.Title}**");
            sb.AppendLine();
            sb.AppendLine(entry.Description);
            sb.AppendLine();
            
            if (entry.Examples.Count > 0)
            {
                sb.AppendLine("*Examples:*");
                foreach (var example in entry.Examples)
                {
                    sb.AppendLine("```");
                    sb.AppendLine(example);
                    sb.AppendLine("```");
                }
                sb.AppendLine();
            }
            
            if (entry.Notes.Count > 0)
            {
                foreach (var note in entry.Notes)
                {
                    sb.AppendLine($"> **Note:** {note}");
                }
                sb.AppendLine();
            }
        }
    }

    /// <summary>
    /// HTML documentation generator
    /// </summary>
    public class HtmlDocGenerator : IDocGenerator
    {
        /// <inheritdoc/>
        public DocFormat Format => DocFormat.Html;
        /// <inheritdoc/>
        public string FileExtension => ".html";

        /// <inheritdoc/>
        public string GenerateSequenceDoc(Sequence sequence, DocDocument? template = null)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine($"<title>{EscapeHtml(sequence.Name)}</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            sb.AppendLine("h1 { color: #333; }");
            sb.AppendLine("table { border-collapse: collapse; width: 100%; }");
            sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            sb.AppendLine("th { background-color: #4CAF50; color: white; }");
            sb.AppendLine("tr:nth-child(even) { background-color: #f2f2f2; }");
            sb.AppendLine(".passed { color: green; }");
            sb.AppendLine(".failed { color: red; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            
            sb.AppendLine($"<h1>{EscapeHtml(sequence.Name)}</h1>");
            sb.AppendLine($"<p><strong>Generated:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            
            sb.AppendLine("<h2>Steps</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>#</th><th>Name</th><th>Type</th><th>Status</th></tr>");
            
            int index = 1;
            foreach (var step in sequence.Steps)
            {
                string statusClass = step.Status == StepStatus.Passed ? "passed" : 
                                    step.Status == StepStatus.Failed ? "failed" : "";
                sb.AppendLine($"<tr><td>{index}</td><td>{EscapeHtml(step.Name)}</td><td>{step.GetType().Name}</td><td class=\"{statusClass}\">{step.Status}</td></tr>");
                index++;
            }
            
            sb.AppendLine("</table>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            return sb.ToString();
        }

        /// <inheritdoc/>
        public string GenerateFromDocument(DocDocument document)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine($"<title>{EscapeHtml(document.Title)}</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; max-width: 900px; }");
            sb.AppendLine("h1, h2, h3, h4 { color: #333; }");
            sb.AppendLine("code { background-color: #f4f4f4; padding: 2px 5px; }");
            sb.AppendLine("pre { background-color: #f4f4f4; padding: 10px; overflow-x: auto; }");
            sb.AppendLine(".note { background-color: #e7f3fe; padding: 10px; border-left: 4px solid #2196F3; }");
            sb.AppendLine(".warning { background-color: #fffae6; padding: 10px; border-left: 4px solid #ff9800; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            
            sb.AppendLine($"<h1>{EscapeHtml(document.Title)}</h1>");
            if (!string.IsNullOrEmpty(document.Subtitle))
            {
                sb.AppendLine($"<h2>{EscapeHtml(document.Subtitle)}</h2>");
            }
            
            sb.AppendLine($"<p><strong>Author:</strong> {EscapeHtml(document.Author)}</p>");
            sb.AppendLine($"<p><strong>Version:</strong> {document.Version}</p>");
            sb.AppendLine($"<p><strong>Date:</strong> {document.ModifiedAt:yyyy-MM-dd}</p>");
            
            if (!string.IsNullOrEmpty(document.Abstract))
            {
                sb.AppendLine("<h2>Abstract</h2>");
                sb.AppendLine($"<p>{EscapeHtml(document.Abstract)}</p>");
            }
            
            foreach (var section in document.Sections)
            {
                GenerateSection(sb, section);
            }
            
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            return sb.ToString();
        }

        private void GenerateSection(StringBuilder sb, DocSection section)
        {
            string tag = $"h{Math.Min(section.Level, 6)}";
            sb.AppendLine($"<{tag}>{EscapeHtml(section.Title)}</{tag}>");
            
            if (!string.IsNullOrEmpty(section.Content))
            {
                sb.AppendLine($"<p>{EscapeHtml(section.Content)}</p>");
            }
            
            foreach (var entry in section.Entries)
            {
                GenerateEntry(sb, entry);
            }
            
            foreach (var subSection in section.SubSections)
            {
                GenerateSection(sb, subSection);
            }
        }

        private void GenerateEntry(StringBuilder sb, DocEntry entry)
        {
            sb.AppendLine($"<h4>{EscapeHtml(entry.Title)}</h4>");
            sb.AppendLine($"<p>{EscapeHtml(entry.Description)}</p>");
            
            if (entry.Examples.Count > 0)
            {
                sb.AppendLine("<h5>Examples</h5>");
                foreach (var example in entry.Examples)
                {
                    sb.AppendLine("<pre>");
                    sb.AppendLine(EscapeHtml(example));
                    sb.AppendLine("</pre>");
                }
            }
            
            foreach (var note in entry.Notes)
            {
                sb.AppendLine($"<div class=\"note\"><strong>Note:</strong> {EscapeHtml(note)}</div>");
            }
        }

        private static string EscapeHtml(string text)
        {
            return System.Net.WebUtility.HtmlEncode(text);
        }
    }

    /// <summary>
    /// Documentation manager singleton
    /// </summary>
    public sealed class DocumentationManager
    {
        private static readonly Lazy<DocumentationManager> _instance = 
            new Lazy<DocumentationManager>(() => new DocumentationManager());
        
        /// <summary>Gets the singleton instance</summary>
        public static DocumentationManager Instance => _instance.Value;

        private readonly Dictionary<DocFormat, IDocGenerator> _generators = new();
        private readonly List<DocDocument> _documents = new();
        private readonly Dictionary<string, DocEntry> _entries = new();
        private readonly object _lockObject = new object();

        private DocumentationManager()
        {
            // Register default generators
            RegisterGenerator(new MarkdownDocGenerator());
            RegisterGenerator(new HtmlDocGenerator());
        }

        /// <summary>Registers a documentation generator</summary>
        public void RegisterGenerator(IDocGenerator generator)
        {
            lock (_lockObject)
            {
                _generators[generator.Format] = generator;
            }
        }

        /// <summary>Gets all registered generators</summary>
        public IReadOnlyDictionary<DocFormat, IDocGenerator> Generators => _generators;

        /// <summary>Gets all documents</summary>
        public IReadOnlyList<DocDocument> Documents => _documents.AsReadOnly();

        /// <summary>Creates a new document</summary>
        public DocDocument CreateDocument(string title, string author = "")
        {
            var doc = new DocDocument
            {
                Title = title,
                Author = author
            };

            lock (_lockObject)
            {
                _documents.Add(doc);
            }

            return doc;
        }

        /// <summary>Adds a documentation entry</summary>
        public void AddEntry(DocEntry entry)
        {
            lock (_lockObject)
            {
                _entries[entry.Id] = entry;
            }
        }

        /// <summary>Gets an entry by ID</summary>
        public DocEntry? GetEntry(string id)
        {
            lock (_lockObject)
            {
                return _entries.TryGetValue(id, out var entry) ? entry : null;
            }
        }

        /// <summary>Gets entries for a target</summary>
        public List<DocEntry> GetEntriesForTarget(string targetId)
        {
            lock (_lockObject)
            {
                return _entries.Values.Where(e => e.TargetId == targetId).ToList();
            }
        }

        /// <summary>Generates documentation for a sequence</summary>
        public string GenerateSequenceDoc(Sequence sequence, DocFormat format = DocFormat.Markdown)
        {
            lock (_lockObject)
            {
                if (_generators.TryGetValue(format, out var generator))
                {
                    return generator.GenerateSequenceDoc(sequence);
                }
                return string.Empty;
            }
        }

        /// <summary>Generates documentation from a document</summary>
        public string GenerateDocument(DocDocument document, DocFormat format = DocFormat.Markdown)
        {
            lock (_lockObject)
            {
                if (_generators.TryGetValue(format, out var generator))
                {
                    return generator.GenerateFromDocument(document);
                }
                return string.Empty;
            }
        }

        /// <summary>Exports documentation to file</summary>
        public void ExportToFile(Sequence sequence, string filePath, DocFormat format = DocFormat.Markdown)
        {
            string content = GenerateSequenceDoc(sequence, format);
            File.WriteAllText(filePath, content);
        }

        /// <summary>Exports a document to file</summary>
        public void ExportDocumentToFile(DocDocument document, string filePath, DocFormat format = DocFormat.Markdown)
        {
            string content = GenerateDocument(document, format);
            File.WriteAllText(filePath, content);
        }

        /// <summary>Saves a document to JSON</summary>
        public void SaveDocument(DocDocument document, string filePath)
        {
            string json = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        /// <summary>Loads a document from JSON</summary>
        public DocDocument? LoadDocument(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            string json = File.ReadAllText(filePath);
            var doc = JsonSerializer.Deserialize<DocDocument>(json);
            
            if (doc != null)
            {
                lock (_lockObject)
                {
                    _documents.Add(doc);
                }
            }
            
            return doc;
        }

        /// <summary>Searches entries by keyword</summary>
        public List<DocEntry> Search(string keyword)
        {
            lock (_lockObject)
            {
                string lower = keyword.ToLower();
                return _entries.Values.Where(e =>
                    e.Title.ToLower().Contains(lower) ||
                    e.Description.ToLower().Contains(lower) ||
                    e.Tags.Any(t => t.ToLower().Contains(lower))
                ).ToList();
            }
        }

        /// <summary>Gets documentation statistics</summary>
        public Dictionary<string, int> GetStatistics()
        {
            lock (_lockObject)
            {
                return new Dictionary<string, int>
                {
                    ["Documents"] = _documents.Count,
                    ["Entries"] = _entries.Count,
                    ["Generators"] = _generators.Count
                };
            }
        }
    }
}
