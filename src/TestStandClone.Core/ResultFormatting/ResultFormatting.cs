using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Globalization;

namespace TestStandClone.Core.ResultFormatting
{
    /// <summary>
    /// Interface for result formatters
    /// </summary>
    public interface IResultFormatter
    {
        string Name { get; }
        string Format(object result, FormatOptions? options = null);
    }

    /// <summary>
    /// Format options
    /// </summary>
    public class FormatOptions
    {
        public int DecimalPlaces { get; set; } = 3;
        public string NumberFormat { get; set; } = "G";
        public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        public string TimeSpanFormat { get; set; } = @"hh\:mm\:ss\.fff";
        public bool IncludeUnits { get; set; } = true;
        public string UnitsSeparator { get; set; } = " ";
        public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;
        public bool UseScientificNotation { get; set; } = false;
        public double ScientificNotationThreshold { get; set; } = 1e6;
        public bool ShowPassFail { get; set; } = true;
        public string PassText { get; set; } = "PASS";
        public string FailText { get; set; } = "FAIL";
    }

    /// <summary>
    /// Formats numeric results
    /// </summary>
    public class NumericResultFormatter : IResultFormatter
    {
        public string Name => "Numeric";

        public string Format(object result, FormatOptions? options = null)
        {
            options ??= new FormatOptions();

            if (result is double d)
            {
                return FormatDouble(d, options);
            }
            if (result is float f)
            {
                return FormatDouble(f, options);
            }
            if (result is decimal dec)
            {
                return FormatDouble((double)dec, options);
            }
            if (result is int i)
            {
                return i.ToString(options.Culture);
            }
            if (result is long l)
            {
                return l.ToString(options.Culture);
            }

            return result?.ToString() ?? "";
        }

        private string FormatDouble(double value, FormatOptions options)
        {
            if (options.UseScientificNotation && Math.Abs(value) >= options.ScientificNotationThreshold)
            {
                return value.ToString($"E{options.DecimalPlaces}", options.Culture);
            }

            return Math.Round(value, options.DecimalPlaces).ToString($"F{options.DecimalPlaces}", options.Culture);
        }
    }

    /// <summary>
    /// Formats measurement results with limits
    /// </summary>
    public class MeasurementResultFormatter : IResultFormatter
    {
        public string Name => "Measurement";

        public string Format(object result, FormatOptions? options = null)
        {
            options ??= new FormatOptions();

            if (result is MeasurementData measurement)
            {
                return FormatMeasurement(measurement, options);
            }

            return result?.ToString() ?? "";
        }

        private string FormatMeasurement(MeasurementData measurement, FormatOptions options)
        {
            var sb = new StringBuilder();

            // Format value
            var valueFormatter = new NumericResultFormatter();
            var formattedValue = valueFormatter.Format(measurement.Value, options);
            sb.Append(formattedValue);

            // Add units
            if (options.IncludeUnits && !string.IsNullOrEmpty(measurement.Units))
            {
                sb.Append(options.UnitsSeparator);
                sb.Append(measurement.Units);
            }

            // Add limits
            if (measurement.HasLimits)
            {
                sb.Append($" [{measurement.LowLimit} - {measurement.HighLimit}]");
            }

            // Add pass/fail
            if (options.ShowPassFail)
            {
                sb.Append($" {(measurement.Passed ? options.PassText : options.FailText)}");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Measurement data structure
    /// </summary>
    public class MeasurementData
    {
        public double Value { get; set; }
        public string Units { get; set; } = string.Empty;
        public double? LowLimit { get; set; }
        public double? HighLimit { get; set; }
        public bool Passed { get; set; }
        public bool HasLimits => LowLimit.HasValue || HighLimit.HasValue;
    }

    /// <summary>
    /// Formats step results
    /// </summary>
    public class StepResultFormatter : IResultFormatter
    {
        public string Name => "StepResult";

        public string Format(object result, FormatOptions? options = null)
        {
            options ??= new FormatOptions();

            if (result is StepResultData stepResult)
            {
                return FormatStepResult(stepResult, options);
            }

            return result?.ToString() ?? "";
        }

        private string FormatStepResult(StepResultData stepResult, FormatOptions options)
        {
            var sb = new StringBuilder();
            sb.Append($"[{stepResult.Status}] {stepResult.StepName}");

            if (!string.IsNullOrEmpty(stepResult.ResultText))
            {
                sb.Append($": {stepResult.ResultText}");
            }

            if (stepResult.Duration.TotalMilliseconds > 0)
            {
                sb.Append($" ({stepResult.Duration.ToString(options.TimeSpanFormat)})");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Step result data structure
    /// </summary>
    public class StepResultData
    {
        public string StepName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ResultText { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public object? Value { get; set; }
    }

    /// <summary>
    /// Formats sequence results as table
    /// </summary>
    public class TableResultFormatter : IResultFormatter
    {
        public string Name => "Table";

        public string Format(object result, FormatOptions? options = null)
        {
            options ??= new FormatOptions();

            if (result is IEnumerable<StepResultData> stepResults)
            {
                return FormatTable(stepResults.ToList(), options);
            }

            return result?.ToString() ?? "";
        }

        private string FormatTable(List<StepResultData> results, FormatOptions options)
        {
            if (results.Count == 0) return "No results";

            var sb = new StringBuilder();

            // Calculate column widths
            var nameWidth = Math.Max(10, results.Max(r => r.StepName.Length) + 2);
            var statusWidth = Math.Max(8, results.Max(r => r.Status.Length) + 2);
            var resultWidth = Math.Max(12, results.Max(r => r.ResultText.Length) + 2);

            // Header
            sb.AppendLine(new string('-', nameWidth + statusWidth + resultWidth + 20));
            sb.AppendLine($"{"Step Name".PadRight(nameWidth)}{"Status".PadRight(statusWidth)}{"Result".PadRight(resultWidth)}Duration");
            sb.AppendLine(new string('-', nameWidth + statusWidth + resultWidth + 20));

            // Rows
            foreach (var result in results)
            {
                sb.AppendLine($"{result.StepName.PadRight(nameWidth)}{result.Status.PadRight(statusWidth)}{result.ResultText.PadRight(resultWidth)}{result.Duration.ToString(options.TimeSpanFormat)}");
            }

            sb.AppendLine(new string('-', nameWidth + statusWidth + resultWidth + 20));

            return sb.ToString();
        }
    }

    /// <summary>
    /// Formats results as JSON
    /// </summary>
    public class JsonResultFormatter : IResultFormatter
    {
        public string Name => "JSON";

        public string Format(object result, FormatOptions? options = null)
        {
            return JsonSerializer.Serialize(result, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
    }

    /// <summary>
    /// Formats results as CSV
    /// </summary>
    public class CsvResultFormatter : IResultFormatter
    {
        public string Name => "CSV";

        public string Format(object result, FormatOptions? options = null)
        {
            options ??= new FormatOptions();

            if (result is IEnumerable<StepResultData> stepResults)
            {
                return FormatCsv(stepResults.ToList(), options);
            }

            return result?.ToString() ?? "";
        }

        private string FormatCsv(List<StepResultData> results, FormatOptions options)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("StepName,Status,Result,Duration");

            // Rows
            foreach (var result in results)
            {
                var duration = result.Duration.ToString(options.TimeSpanFormat);
                sb.AppendLine($"\"{result.StepName}\",\"{result.Status}\",\"{result.ResultText}\",\"{duration}\"");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Formats results as XML
    /// </summary>
    public class XmlResultFormatter : IResultFormatter
    {
        public string Name => "XML";

        public string Format(object result, FormatOptions? options = null)
        {
            options ??= new FormatOptions();

            if (result is IEnumerable<StepResultData> stepResults)
            {
                return FormatXml(stepResults.ToList(), options);
            }

            return result?.ToString() ?? "";
        }

        private string FormatXml(List<StepResultData> results, FormatOptions options)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<Results>");

            foreach (var result in results)
            {
                sb.AppendLine("  <Step>");
                sb.AppendLine($"    <Name>{EscapeXml(result.StepName)}</Name>");
                sb.AppendLine($"    <Status>{EscapeXml(result.Status)}</Status>");
                sb.AppendLine($"    <Result>{EscapeXml(result.ResultText)}</Result>");
                sb.AppendLine($"    <Duration>{result.Duration.ToString(options.TimeSpanFormat)}</Duration>");
                sb.AppendLine("  </Step>");
            }

            sb.AppendLine("</Results>");
            return sb.ToString();
        }

        private string EscapeXml(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }

    /// <summary>
    /// Manager for result formatting
    /// </summary>
    public class ResultFormattingManager
    {
        private static readonly Lazy<ResultFormattingManager> _instance = 
            new(() => new ResultFormattingManager());
        
        public static ResultFormattingManager Instance => _instance.Value;

        private readonly Dictionary<string, IResultFormatter> _formatters = new();
        private FormatOptions _defaultOptions = new();

        private ResultFormattingManager()
        {
            // Register built-in formatters
            RegisterFormatter(new NumericResultFormatter());
            RegisterFormatter(new MeasurementResultFormatter());
            RegisterFormatter(new StepResultFormatter());
            RegisterFormatter(new TableResultFormatter());
            RegisterFormatter(new JsonResultFormatter());
            RegisterFormatter(new CsvResultFormatter());
            RegisterFormatter(new XmlResultFormatter());
        }

        /// <summary>
        /// Sets default format options
        /// </summary>
        public void SetDefaultOptions(FormatOptions options)
        {
            _defaultOptions = options;
        }

        /// <summary>
        /// Gets default format options
        /// </summary>
        public FormatOptions GetDefaultOptions()
        {
            return _defaultOptions;
        }

        /// <summary>
        /// Registers a formatter
        /// </summary>
        public void RegisterFormatter(IResultFormatter formatter)
        {
            _formatters[formatter.Name] = formatter;
        }

        /// <summary>
        /// Gets a formatter by name
        /// </summary>
        public IResultFormatter? GetFormatter(string name)
        {
            return _formatters.TryGetValue(name, out var formatter) ? formatter : null;
        }

        /// <summary>
        /// Gets all registered formatters
        /// </summary>
        public IEnumerable<IResultFormatter> GetFormatters()
        {
            return _formatters.Values;
        }

        /// <summary>
        /// Formats a result using the specified formatter
        /// </summary>
        public string Format(object result, string formatterName, FormatOptions? options = null)
        {
            var formatter = GetFormatter(formatterName);
            if (formatter == null)
            {
                throw new ArgumentException($"Formatter '{formatterName}' not found");
            }

            return formatter.Format(result, options ?? _defaultOptions);
        }

        /// <summary>
        /// Formats a numeric value
        /// </summary>
        public string FormatNumeric(double value, FormatOptions? options = null)
        {
            return Format(value, "Numeric", options);
        }

        /// <summary>
        /// Formats a measurement
        /// </summary>
        public string FormatMeasurement(double value, string units, double? lowLimit = null, double? highLimit = null, FormatOptions? options = null)
        {
            var measurement = new MeasurementData
            {
                Value = value,
                Units = units,
                LowLimit = lowLimit,
                HighLimit = highLimit,
                Passed = (!lowLimit.HasValue || value >= lowLimit) && (!highLimit.HasValue || value <= highLimit)
            };

            return Format(measurement, "Measurement", options);
        }

        /// <summary>
        /// Formats results as table
        /// </summary>
        public string FormatAsTable(IEnumerable<StepResultData> results, FormatOptions? options = null)
        {
            return Format(results, "Table", options);
        }

        /// <summary>
        /// Formats results as JSON
        /// </summary>
        public string FormatAsJson(object result)
        {
            return Format(result, "JSON");
        }

        /// <summary>
        /// Formats results as CSV
        /// </summary>
        public string FormatAsCsv(IEnumerable<StepResultData> results, FormatOptions? options = null)
        {
            return Format(results, "CSV", options);
        }

        /// <summary>
        /// Formats results as XML
        /// </summary>
        public string FormatAsXml(IEnumerable<StepResultData> results, FormatOptions? options = null)
        {
            return Format(results, "XML", options);
        }
    }
}
