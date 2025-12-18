using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestStandClone.Core.ReportTemplates
{
    /// <summary>
    /// Types of report templates
    /// </summary>
    public enum ReportTemplateType
    {
        Html,
        Text,
        Xml,
        Csv,
        Custom
    }

    /// <summary>
    /// Report template definition
    /// </summary>
    public class ReportTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ReportTemplateType Type { get; set; } = ReportTemplateType.Html;
        public string TemplateContent { get; set; } = string.Empty;
        public string FileExtension { get; set; } = ".html";
        public Dictionary<string, string> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Data model for report generation
    /// </summary>
    public class ReportData
    {
        public string SequenceName { get; set; } = string.Empty;
        public string SequenceFilePath { get; set; } = string.Empty;
        public string UutSerialNumber { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public StepStatus OverallStatus { get; set; }
        public string OperatorName { get; set; } = string.Empty;
        public string StationName { get; set; } = Environment.MachineName;
        
        public List<StepReportData> Steps { get; set; } = new();
        public Dictionary<string, object> CustomData { get; set; } = new();
        
        public int TotalSteps => Steps.Count;
        public int PassedSteps => Steps.Count(s => s.Status == StepStatus.Passed);
        public int FailedSteps => Steps.Count(s => s.Status == StepStatus.Failed);
        public int ErrorSteps => Steps.Count(s => s.Status == StepStatus.Error);
        public double PassRate => TotalSteps > 0 ? (double)PassedSteps / TotalSteps * 100 : 0;
    }

    /// <summary>
    /// Step data for reports
    /// </summary>
    public class StepReportData
    {
        public int StepNumber { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string StepType { get; set; } = string.Empty;
        public string Group { get; set; } = "Main";
        public StepStatus Status { get; set; }
        public string ResultText { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        
        public double? NumericValue { get; set; }
        public double? LowLimit { get; set; }
        public double? HighLimit { get; set; }
        public string? Units { get; set; }
        public string? StringValue { get; set; }
        public string? ExpectedString { get; set; }
        
        public Dictionary<string, object> CustomResults { get; set; } = new();
    }

    /// <summary>
    /// Manages report templates and generation
    /// Similar to TestStand Report Generation
    /// </summary>
    public class ReportTemplateManager
    {
        private static readonly Lazy<ReportTemplateManager> _instance = new(() => new ReportTemplateManager());
        public static ReportTemplateManager Instance => _instance.Value;

        private readonly Dictionary<string, ReportTemplate> _templates = new();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private ReportTemplateManager()
        {
            LoadDefaultTemplates();
        }

        private void LoadDefaultTemplates()
        {
            // HTML Template
            _templates["HTML"] = new ReportTemplate
            {
                Name = "HTML",
                Description = "Standard HTML report",
                Type = ReportTemplateType.Html,
                FileExtension = ".html",
                TemplateContent = GetDefaultHtmlTemplate()
            };

            // Text Template
            _templates["Text"] = new ReportTemplate
            {
                Name = "Text",
                Description = "Plain text report",
                Type = ReportTemplateType.Text,
                FileExtension = ".txt",
                TemplateContent = GetDefaultTextTemplate()
            };

            // CSV Template
            _templates["CSV"] = new ReportTemplate
            {
                Name = "CSV",
                Description = "Comma-separated values report",
                Type = ReportTemplateType.Csv,
                FileExtension = ".csv",
                TemplateContent = ""
            };

            // XML Template
            _templates["XML"] = new ReportTemplate
            {
                Name = "XML",
                Description = "XML report",
                Type = ReportTemplateType.Xml,
                FileExtension = ".xml",
                TemplateContent = ""
            };
        }

        private static string GetDefaultHtmlTemplate()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Test Report - {{SequenceName}}</title>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 20px; background-color: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 20px; box-shadow: 0 2px 5px rgba(0,0,0,0.1); }
        h1 { color: #333; border-bottom: 2px solid #007acc; padding-bottom: 10px; }
        h2 { color: #555; margin-top: 30px; }
        .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; margin: 20px 0; }
        .summary-item { padding: 15px; background: #f8f8f8; border-radius: 5px; }
        .summary-item label { font-weight: bold; color: #666; display: block; margin-bottom: 5px; }
        .summary-item span { font-size: 1.2em; }
        .status-pass { color: green; }
        .status-fail { color: red; }
        .status-error { color: orange; }
        table { width: 100%; border-collapse: collapse; margin-top: 20px; }
        th, td { padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }
        th { background: #f0f0f0; font-weight: bold; }
        tr:hover { background: #f9f9f9; }
        .pass { background-color: #d4edda; }
        .fail { background-color: #f8d7da; }
        .error { background-color: #fff3cd; }
        .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 0.9em; }
    </style>
</head>
<body>
    <div class=""container"">
        <h1>Test Report</h1>
        
        <div class=""summary"">
            <div class=""summary-item"">
                <label>Sequence Name</label>
                <span>{{SequenceName}}</span>
            </div>
            <div class=""summary-item"">
                <label>UUT Serial Number</label>
                <span>{{UutSerialNumber}}</span>
            </div>
            <div class=""summary-item"">
                <label>Overall Status</label>
                <span class=""status-{{OverallStatusClass}}"">{{OverallStatus}}</span>
            </div>
            <div class=""summary-item"">
                <label>Start Time</label>
                <span>{{StartTime}}</span>
            </div>
            <div class=""summary-item"">
                <label>Duration</label>
                <span>{{Duration}}</span>
            </div>
            <div class=""summary-item"">
                <label>Pass Rate</label>
                <span>{{PassRate}}%</span>
            </div>
            <div class=""summary-item"">
                <label>Operator</label>
                <span>{{OperatorName}}</span>
            </div>
            <div class=""summary-item"">
                <label>Station</label>
                <span>{{StationName}}</span>
            </div>
        </div>

        <h2>Test Steps</h2>
        <table>
            <thead>
                <tr>
                    <th>#</th>
                    <th>Step Name</th>
                    <th>Type</th>
                    <th>Status</th>
                    <th>Result</th>
                    <th>Duration</th>
                </tr>
            </thead>
            <tbody>
                {{StepRows}}
            </tbody>
        </table>

        <div class=""footer"">
            <p>Report generated by TestStand Clone on {{GeneratedAt}}</p>
        </div>
    </div>
</body>
</html>";
        }

        private static string GetDefaultTextTemplate()
        {
            return @"=================================================================
                    TEST REPORT
=================================================================

Sequence Name:    {{SequenceName}}
UUT Serial:       {{UutSerialNumber}}
Overall Status:   {{OverallStatus}}
Start Time:       {{StartTime}}
Duration:         {{Duration}}
Pass Rate:        {{PassRate}}%
Operator:         {{OperatorName}}
Station:          {{StationName}}

=================================================================
                    TEST STEPS
=================================================================

{{StepDetails}}

=================================================================
Report generated by TestStand Clone on {{GeneratedAt}}
=================================================================";
        }

        /// <summary>
        /// Generate a report from data using a template
        /// </summary>
        public string GenerateReport(ReportData data, string templateName)
        {
            if (!_templates.TryGetValue(templateName, out var template))
            {
                throw new ArgumentException($"Template '{templateName}' not found");
            }

            return GenerateReport(data, template);
        }

        /// <summary>
        /// Generate a report from data using a template
        /// </summary>
        public string GenerateReport(ReportData data, ReportTemplate template)
        {
            return template.Type switch
            {
                ReportTemplateType.Html => GenerateHtmlReport(data, template),
                ReportTemplateType.Text => GenerateTextReport(data, template),
                ReportTemplateType.Csv => GenerateCsvReport(data),
                ReportTemplateType.Xml => GenerateXmlReport(data),
                _ => GenerateHtmlReport(data, template)
            };
        }

        private string GenerateHtmlReport(ReportData data, ReportTemplate template)
        {
            var html = template.TemplateContent;

            // Replace placeholders
            html = html.Replace("{{SequenceName}}", System.Web.HttpUtility.HtmlEncode(data.SequenceName));
            html = html.Replace("{{UutSerialNumber}}", System.Web.HttpUtility.HtmlEncode(data.UutSerialNumber));
            html = html.Replace("{{OverallStatus}}", data.OverallStatus.ToString());
            html = html.Replace("{{OverallStatusClass}}", data.OverallStatus.ToString().ToLower());
            html = html.Replace("{{StartTime}}", data.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            html = html.Replace("{{Duration}}", data.Duration.ToString(@"hh\:mm\:ss\.fff"));
            html = html.Replace("{{PassRate}}", data.PassRate.ToString("F1"));
            html = html.Replace("{{OperatorName}}", System.Web.HttpUtility.HtmlEncode(data.OperatorName));
            html = html.Replace("{{StationName}}", System.Web.HttpUtility.HtmlEncode(data.StationName));
            html = html.Replace("{{GeneratedAt}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // Generate step rows
            var stepRows = new StringBuilder();
            foreach (var step in data.Steps)
            {
                var statusClass = step.Status switch
                {
                    StepStatus.Passed => "pass",
                    StepStatus.Failed => "fail",
                    StepStatus.Error => "error",
                    _ => ""
                };

                stepRows.AppendLine($@"<tr class=""{statusClass}"">
                    <td>{step.StepNumber}</td>
                    <td>{System.Web.HttpUtility.HtmlEncode(step.StepName)}</td>
                    <td>{System.Web.HttpUtility.HtmlEncode(step.StepType)}</td>
                    <td>{step.Status}</td>
                    <td>{System.Web.HttpUtility.HtmlEncode(step.ResultText)}</td>
                    <td>{step.Duration.TotalMilliseconds:F2} ms</td>
                </tr>");
            }
            html = html.Replace("{{StepRows}}", stepRows.ToString());

            return html;
        }

        private string GenerateTextReport(ReportData data, ReportTemplate template)
        {
            var text = template.TemplateContent;

            text = text.Replace("{{SequenceName}}", data.SequenceName);
            text = text.Replace("{{UutSerialNumber}}", data.UutSerialNumber);
            text = text.Replace("{{OverallStatus}}", data.OverallStatus.ToString());
            text = text.Replace("{{StartTime}}", data.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            text = text.Replace("{{Duration}}", data.Duration.ToString(@"hh\:mm\:ss\.fff"));
            text = text.Replace("{{PassRate}}", data.PassRate.ToString("F1"));
            text = text.Replace("{{OperatorName}}", data.OperatorName);
            text = text.Replace("{{StationName}}", data.StationName);
            text = text.Replace("{{GeneratedAt}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            var stepDetails = new StringBuilder();
            foreach (var step in data.Steps)
            {
                stepDetails.AppendLine($"Step {step.StepNumber}: {step.StepName}");
                stepDetails.AppendLine($"  Type: {step.StepType}");
                stepDetails.AppendLine($"  Status: {step.Status}");
                stepDetails.AppendLine($"  Result: {step.ResultText}");
                stepDetails.AppendLine($"  Duration: {step.Duration.TotalMilliseconds:F2} ms");
                stepDetails.AppendLine();
            }
            text = text.Replace("{{StepDetails}}", stepDetails.ToString());

            return text;
        }

        private static string GenerateCsvReport(ReportData data)
        {
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine("StepNumber,StepName,StepType,Group,Status,ResultText,StartTime,EndTime,DurationMs,NumericValue,LowLimit,HighLimit,Units");
            
            // Data rows
            foreach (var step in data.Steps)
            {
                csv.AppendLine($"{step.StepNumber},{EscapeCsv(step.StepName)},{EscapeCsv(step.StepType)},{step.Group},{step.Status},{EscapeCsv(step.ResultText)},{step.StartTime:O},{step.EndTime:O},{step.Duration.TotalMilliseconds:F2},{step.NumericValue},{step.LowLimit},{step.HighLimit},{step.Units}");
            }

            return csv.ToString();
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        private static string GenerateXmlReport(ReportData data)
        {
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            xml.AppendLine("<TestReport>");
            xml.AppendLine($"  <SequenceName>{System.Security.SecurityElement.Escape(data.SequenceName)}</SequenceName>");
            xml.AppendLine($"  <UutSerialNumber>{System.Security.SecurityElement.Escape(data.UutSerialNumber)}</UutSerialNumber>");
            xml.AppendLine($"  <OverallStatus>{data.OverallStatus}</OverallStatus>");
            xml.AppendLine($"  <StartTime>{data.StartTime:O}</StartTime>");
            xml.AppendLine($"  <EndTime>{data.EndTime:O}</EndTime>");
            xml.AppendLine($"  <DurationMs>{data.Duration.TotalMilliseconds:F2}</DurationMs>");
            xml.AppendLine($"  <PassRate>{data.PassRate:F1}</PassRate>");
            xml.AppendLine($"  <OperatorName>{System.Security.SecurityElement.Escape(data.OperatorName)}</OperatorName>");
            xml.AppendLine($"  <StationName>{System.Security.SecurityElement.Escape(data.StationName)}</StationName>");
            
            xml.AppendLine("  <Steps>");
            foreach (var step in data.Steps)
            {
                xml.AppendLine("    <Step>");
                xml.AppendLine($"      <StepNumber>{step.StepNumber}</StepNumber>");
                xml.AppendLine($"      <StepName>{System.Security.SecurityElement.Escape(step.StepName)}</StepName>");
                xml.AppendLine($"      <StepType>{System.Security.SecurityElement.Escape(step.StepType)}</StepType>");
                xml.AppendLine($"      <Status>{step.Status}</Status>");
                xml.AppendLine($"      <ResultText>{System.Security.SecurityElement.Escape(step.ResultText)}</ResultText>");
                xml.AppendLine($"      <DurationMs>{step.Duration.TotalMilliseconds:F2}</DurationMs>");
                if (step.NumericValue.HasValue)
                {
                    xml.AppendLine($"      <NumericValue>{step.NumericValue}</NumericValue>");
                    xml.AppendLine($"      <LowLimit>{step.LowLimit}</LowLimit>");
                    xml.AppendLine($"      <HighLimit>{step.HighLimit}</HighLimit>");
                    xml.AppendLine($"      <Units>{step.Units}</Units>");
                }
                xml.AppendLine("    </Step>");
            }
            xml.AppendLine("  </Steps>");
            xml.AppendLine("</TestReport>");

            return xml.ToString();
        }

        /// <summary>
        /// Save report to file
        /// </summary>
        public void SaveReport(string report, string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(filePath, report);
        }

        /// <summary>
        /// Generate and save report
        /// </summary>
        public string GenerateAndSave(ReportData data, string templateName, string outputPath)
        {
            var report = GenerateReport(data, templateName);
            
            if (!_templates.TryGetValue(templateName, out var template))
            {
                template = _templates["HTML"];
            }

            var filePath = outputPath;
            if (!filePath.EndsWith(template.FileExtension, StringComparison.OrdinalIgnoreCase))
            {
                filePath += template.FileExtension;
            }

            SaveReport(report, filePath);
            return filePath;
        }

        /// <summary>
        /// Register a custom template
        /// </summary>
        public void RegisterTemplate(ReportTemplate template)
        {
            _templates[template.Name] = template;
        }

        /// <summary>
        /// Load template from file
        /// </summary>
        public void LoadTemplateFromFile(string name, string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Template file not found", filePath);
            }

            var content = File.ReadAllText(filePath);
            var extension = Path.GetExtension(filePath).ToLower();
            var type = extension switch
            {
                ".html" or ".htm" => ReportTemplateType.Html,
                ".txt" => ReportTemplateType.Text,
                ".xml" => ReportTemplateType.Xml,
                ".csv" => ReportTemplateType.Csv,
                _ => ReportTemplateType.Custom
            };

            _templates[name] = new ReportTemplate
            {
                Name = name,
                Type = type,
                FileExtension = extension,
                TemplateContent = content
            };
        }

        /// <summary>
        /// Get all template names
        /// </summary>
        public IEnumerable<string> GetTemplateNames()
        {
            return _templates.Keys;
        }

        /// <summary>
        /// Get a template by name
        /// </summary>
        public ReportTemplate? GetTemplate(string name)
        {
            _templates.TryGetValue(name, out var template);
            return template;
        }
    }
}
