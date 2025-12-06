using System.Text;

namespace TestStandClone.Core.Reporting
{
    /// <summary>
    /// Generates test reports from sequence execution results.
    /// Similar to TestStand's Report Generation feature.
    /// </summary>
    public class ReportGenerator
    {
        /// <summary>
        /// Generates an HTML report for a sequence execution.
        /// </summary>
        /// <param name="sequence">The executed sequence.</param>
        /// <returns>HTML report content.</returns>
        public string GenerateHtmlReport(Sequence sequence)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine($"<title>Test Report - {sequence.Name}</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; margin: 20px; background: #f5f5f5; }");
            sb.AppendLine(".container { max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }");
            sb.AppendLine("h1 { color: #0078D4; border-bottom: 2px solid #0078D4; padding-bottom: 10px; }");
            sb.AppendLine("h2 { color: #333; margin-top: 30px; }");
            sb.AppendLine(".summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; margin: 20px 0; }");
            sb.AppendLine(".summary-card { background: #f8f8f8; padding: 15px; border-radius: 5px; border-left: 4px solid #0078D4; }");
            sb.AppendLine(".summary-card.passed { border-left-color: #4CAF50; }");
            sb.AppendLine(".summary-card.failed { border-left-color: #f44336; }");
            sb.AppendLine(".summary-card.error { border-left-color: #ff9800; }");
            sb.AppendLine(".summary-label { font-size: 12px; color: #666; text-transform: uppercase; }");
            sb.AppendLine(".summary-value { font-size: 24px; font-weight: bold; color: #333; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 10px; }");
            sb.AppendLine("th { background: #0078D4; color: white; padding: 12px 8px; text-align: left; }");
            sb.AppendLine("td { padding: 10px 8px; border-bottom: 1px solid #ddd; }");
            sb.AppendLine("tr:hover { background: #f5f5f5; }");
            sb.AppendLine(".status { padding: 4px 10px; border-radius: 3px; color: white; font-weight: bold; font-size: 11px; }");
            sb.AppendLine(".status.idle { background: #9E9E9E; }");
            sb.AppendLine(".status.running { background: #2196F3; }");
            sb.AppendLine(".status.passed { background: #4CAF50; }");
            sb.AppendLine(".status.failed { background: #f44336; }");
            sb.AppendLine(".status.error { background: #ff5722; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head><body>");
            sb.AppendLine("<div class='container'>");

            // Header
            sb.AppendLine($"<h1>Test Report: {EscapeHtml(sequence.Name)}</h1>");
            sb.AppendLine($"<p>{EscapeHtml(sequence.Description)}</p>");

            // Summary
            int totalSteps = sequence.Steps.Count;
            int passedSteps = sequence.Steps.Count(s => s.Status == StepStatus.Passed);
            int failedSteps = sequence.Steps.Count(s => s.Status == StepStatus.Failed);
            int errorSteps = sequence.Steps.Count(s => s.Status == StepStatus.Error);

            sb.AppendLine("<div class='summary'>");
            sb.AppendLine($"<div class='summary-card'><div class='summary-label'>Total Steps</div><div class='summary-value'>{totalSteps}</div></div>");
            sb.AppendLine($"<div class='summary-card passed'><div class='summary-label'>Passed</div><div class='summary-value'>{passedSteps}</div></div>");
            sb.AppendLine($"<div class='summary-card failed'><div class='summary-label'>Failed</div><div class='summary-value'>{failedSteps}</div></div>");
            sb.AppendLine($"<div class='summary-card error'><div class='summary-label'>Errors</div><div class='summary-value'>{errorSteps}</div></div>");
            sb.AppendLine($"<div class='summary-card'><div class='summary-label'>Sequence Status</div><div class='summary-value'>{sequence.Status}</div></div>");
            if (sequence.ExecutionTime.HasValue)
            {
                sb.AppendLine($"<div class='summary-card'><div class='summary-label'>Execution Time</div><div class='summary-value'>{sequence.ExecutionTime.Value:mm\\:ss\\.fff}</div></div>");
            }
            sb.AppendLine("</div>");

            // Execution Details
            sb.AppendLine("<h2>Execution Details</h2>");
            sb.AppendLine($"<p><strong>Start Time:</strong> {sequence.StartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}</p>");
            sb.AppendLine($"<p><strong>End Time:</strong> {sequence.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}</p>");

            // Steps Table
            sb.AppendLine("<h2>Step Results</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>#</th><th>Step Name</th><th>Type</th><th>Status</th><th>Time</th><th>Result</th></tr>");

            int stepNum = 1;
            foreach (var step in sequence.Steps)
            {
                string statusClass = step.Status.ToString().ToLower();
                string timeStr = step.ExecutionTime?.ToString(@"mm\:ss\.fff") ?? "-";
                
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{stepNum++}</td>");
                sb.AppendLine($"<td>{EscapeHtml(step.Name)}</td>");
                sb.AppendLine($"<td>{step.GetType().Name.Replace("Step", "")}</td>");
                sb.AppendLine($"<td><span class='status {statusClass}'>{step.Status}</span></td>");
                sb.AppendLine($"<td>{timeStr}</td>");
                sb.AppendLine($"<td>{EscapeHtml(step.ResultText)}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");

            // Footer
            sb.AppendLine($"<p style='margin-top: 30px; color: #666; font-size: 12px;'>Report generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} by TestStand Clone</p>");
            
            sb.AppendLine("</div></body></html>");
            
            return sb.ToString();
        }

        /// <summary>
        /// Generates a plain text report for a sequence execution.
        /// </summary>
        /// <param name="sequence">The executed sequence.</param>
        /// <returns>Plain text report content.</returns>
        public string GenerateTextReport(Sequence sequence)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            sb.AppendLine($"  TEST REPORT: {sequence.Name}");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            sb.AppendLine();
            
            sb.AppendLine("SUMMARY");
            sb.AppendLine("───────────────────────────────────────────────────────────────────");
            sb.AppendLine($"  Sequence Status: {sequence.Status}");
            sb.AppendLine($"  Start Time:      {sequence.StartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}");
            sb.AppendLine($"  End Time:        {sequence.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}");
            sb.AppendLine($"  Execution Time:  {sequence.ExecutionTime?.ToString(@"mm\:ss\.fff") ?? "N/A"}");
            sb.AppendLine();

            int totalSteps = sequence.Steps.Count;
            int passedSteps = sequence.Steps.Count(s => s.Status == StepStatus.Passed);
            int failedSteps = sequence.Steps.Count(s => s.Status == StepStatus.Failed);
            int errorSteps = sequence.Steps.Count(s => s.Status == StepStatus.Error);

            sb.AppendLine($"  Total Steps:     {totalSteps}");
            sb.AppendLine($"  Passed:          {passedSteps}");
            sb.AppendLine($"  Failed:          {failedSteps}");
            sb.AppendLine($"  Errors:          {errorSteps}");
            sb.AppendLine();

            sb.AppendLine("STEP DETAILS");
            sb.AppendLine("───────────────────────────────────────────────────────────────────");
            sb.AppendLine($"{"#",-4} {"Step Name",-25} {"Type",-15} {"Status",-10} {"Time",-12} Result");
            sb.AppendLine("───────────────────────────────────────────────────────────────────");

            int stepNum = 1;
            foreach (var step in sequence.Steps)
            {
                string typeName = step.GetType().Name.Replace("Step", "");
                string timeStr = step.ExecutionTime?.ToString(@"mm\:ss\.fff") ?? "-";
                sb.AppendLine($"{stepNum,-4} {Truncate(step.Name, 25),-25} {typeName,-15} {step.Status,-10} {timeStr,-12} {step.ResultText}");
                stepNum++;
            }

            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            sb.AppendLine($"  Report generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("═══════════════════════════════════════════════════════════════════");

            return sb.ToString();
        }

        /// <summary>
        /// Saves the HTML report to a file.
        /// </summary>
        public async Task SaveHtmlReportAsync(Sequence sequence, string filePath)
        {
            var content = GenerateHtmlReport(sequence);
            await File.WriteAllTextAsync(filePath, content);
        }

        /// <summary>
        /// Saves the text report to a file.
        /// </summary>
        public async Task SaveTextReportAsync(Sequence sequence, string filePath)
        {
            var content = GenerateTextReport(sequence);
            await File.WriteAllTextAsync(filePath, content);
        }

        private static string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength - 3) + "...";
        }
    }
}
