using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestStandClone.Core.ReportBuilder
{
    // Report section
    public class ReportSection
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public int Level { get; set; } // Heading level
        public List<ReportSection> Subsections { get; set; }
        public List<ReportTable> Tables { get; set; }
        public List<ReportChart> Charts { get; set; }

        public ReportSection()
        {
            Subsections = new List<ReportSection>();
            Tables = new List<ReportTable>();
            Charts = new List<ReportChart>();
        }
    }

    // Report table
    public class ReportTable
    {
        public string Title { get; set; }
        public List<string> Headers { get; set; }
        public List<List<string>> Rows { get; set; }
        public bool ShowBorders { get; set; }

        public ReportTable()
        {
            Headers = new List<string>();
            Rows = new List<List<string>>();
            ShowBorders = true;
        }
    }

    // Report chart
    public class ReportChart
    {
        public string Title { get; set; }
        public ChartType Type { get; set; }
        public List<ChartSeries> Series { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public ReportChart()
        {
            Series = new List<ChartSeries>();
            Width = 800;
            Height = 600;
        }
    }

    public enum ChartType { Line, Bar, Pie, Scatter }

    public class ChartSeries
    {
        public string Name { get; set; }
        public List<double> Data { get; set; }
        public List<string> Labels { get; set; }

        public ChartSeries()
        {
            Data = new List<double>();
            Labels = new List<string>();
        }
    }

    // Complete report
    public class TestReport
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<ReportSection> Sections { get; set; }
        public ReportMetadata Metadata { get; set; }
        public ReportStyle Style { get; set; }

        public TestReport()
        {
            Sections = new List<ReportSection>();
            Metadata = new ReportMetadata();
            Style = new ReportStyle();
            GeneratedDate = DateTime.Now;
        }
    }

    // Report metadata
    public class ReportMetadata
    {
        public string TestProgram { get; set; }
        public string TestStation { get; set; }
        public string Operator { get; set; }
        public string SerialNumber { get; set; }
        public Dictionary<string, string> CustomFields { get; set; }

        public ReportMetadata()
        {
            CustomFields = new Dictionary<string, string>();
        }
    }

    // Report style
    public class ReportStyle
    {
        public string FontFamily { get; set; }
        public int FontSize { get; set; }
        public string HeaderColor { get; set; }
        public string BackgroundColor { get; set; }
        public bool IncludeLogo { get; set; }
        public string LogoPath { get; set; }

        public ReportStyle()
        {
            FontFamily = "Arial";
            FontSize = 12;
            HeaderColor = "#333333";
            BackgroundColor = "#FFFFFF";
            IncludeLogo = false;
        }
    }

    // Report format
    public enum ReportFormat { HTML, PDF, Word, Excel, Text, Markdown }

    // Report Builder Manager
    public class ReportBuilderManager
    {
        private static ReportBuilderManager _instance;
        private static readonly object _lock = new object();
        private Dictionary<string, TestReport> _reports;

        private ReportBuilderManager()
        {
            _reports = new Dictionary<string, TestReport>();
        }

        public static ReportBuilderManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ReportBuilderManager();
                    }
                }
                return _instance;
            }
        }

        public TestReport CreateReport(string title)
        {
            var report = new TestReport { Title = title };
            _reports[title] = report;
            return report;
        }

        public TestReport GetReport(string title)
        {
            return _reports.TryGetValue(title, out var report) ? report : null;
        }

        public void AddSection(TestReport report, ReportSection section)
        {
            report.Sections.Add(section);
        }

        public string ExportToHTML(TestReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine($"<title>{report.Title}</title>");
            sb.AppendLine($"<style>body {{ font-family: {report.Style.FontFamily}; font-size: {report.Style.FontSize}px; }}</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine($"<h1>{report.Title}</h1>");
            sb.AppendLine($"<p>Generated: {report.GeneratedDate}</p>");

            foreach (var section in report.Sections)
            {
                ExportSectionToHTML(sb, section);
            }

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        private void ExportSectionToHTML(StringBuilder sb, ReportSection section)
        {
            sb.AppendLine($"<h{section.Level + 1}>{section.Title}</h{section.Level + 1}>");
            if (!string.IsNullOrEmpty(section.Content))
            {
                sb.AppendLine($"<p>{section.Content}</p>");
            }

            foreach (var table in section.Tables)
            {
                sb.AppendLine("<table border='1'>");
                sb.AppendLine("<tr>");
                foreach (var header in table.Headers)
                {
                    sb.AppendLine($"<th>{header}</th>");
                }
                sb.AppendLine("</tr>");
                foreach (var row in table.Rows)
                {
                    sb.AppendLine("<tr>");
                    foreach (var cell in row)
                    {
                        sb.AppendLine($"<td>{cell}</td>");
                    }
                    sb.AppendLine("</tr>");
                }
                sb.AppendLine("</table>");
            }

            foreach (var subsection in section.Subsections)
            {
                ExportSectionToHTML(sb, subsection);
            }
        }

        public string ExportToText(TestReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine(report.Title);
            sb.AppendLine(new string('=', report.Title.Length));
            sb.AppendLine($"Generated: {report.GeneratedDate}");
            sb.AppendLine();

            foreach (var section in report.Sections)
            {
                ExportSectionToText(sb, section);
            }

            return sb.ToString();
        }

        private void ExportSectionToText(StringBuilder sb, ReportSection section)
        {
            string indent = new string(' ', section.Level * 2);
            sb.AppendLine($"{indent}{section.Title}");
            sb.AppendLine($"{indent}{new string('-', section.Title.Length)}");
            
            if (!string.IsNullOrEmpty(section.Content))
            {
                sb.AppendLine($"{indent}{section.Content}");
                sb.AppendLine();
            }

            foreach (var subsection in section.Subsections)
            {
                ExportSectionToText(sb, subsection);
            }
        }
    }
}
