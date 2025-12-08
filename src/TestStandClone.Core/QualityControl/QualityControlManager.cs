using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.QualityControl
{
    public enum QualityStatus { Unknown, Conforming, NonConforming, UnderReview, Quarantine, Rework, Scrap }
    public enum DefectSeverity { Minor, Major, Critical }
    public enum QualityRuleType { PassRate, ConsecutiveFails, YieldThreshold, CpkThreshold, DefectCount, Custom }

    public class QualityAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string RuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DefectSeverity Severity { get; set; }
        public DateTime TriggeredAt { get; set; } = DateTime.Now;
        public bool IsAcknowledged { get; set; }
        public string? AcknowledgedBy { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
    }

    public class QualityRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public QualityRuleType Type { get; set; }
        public double Threshold { get; set; }
        public DefectSeverity AlertSeverity { get; set; } = DefectSeverity.Major;
        public bool IsEnabled { get; set; } = true;
    }

    public class DefectRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SerialNumber { get; set; } = string.Empty;
        public string DefectCode { get; set; } = string.Empty;
        public string DefectDescription { get; set; } = string.Empty;
        public DefectSeverity Severity { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string ReportedBy { get; set; } = string.Empty;
        public DateTime ReportedAt { get; set; } = DateTime.Now;
        public QualityStatus DispositionStatus { get; set; } = QualityStatus.UnderReview;
        public string? DispositionNotes { get; set; }
        public string? DispositionBy { get; set; }
        public DateTime? DispositionAt { get; set; }
    }

    public class QualityMetrics
    {
        public DateTime SnapshotTime { get; set; } = DateTime.Now;
        public int TotalUnits { get; set; }
        public int PassedUnits { get; set; }
        public int FailedUnits { get; set; }
        public double FirstPassYield { get; set; }
        public int DefectCount { get; set; }
        public double DefectsPerUnit { get; set; }
        public int ConsecutiveFailures { get; set; }
    }

    public class SPCDataPoint
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string ParameterName { get; set; } = string.Empty;
        public double Value { get; set; }
        public double LowerControlLimit { get; set; }
        public double UpperControlLimit { get; set; }
        public double Mean { get; set; }
        public bool IsInControl { get; set; }
    }

    public class SPCControlChart
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ParameterName { get; set; } = string.Empty;
        public double LowerControlLimit { get; set; }
        public double CenterLine { get; set; }
        public double UpperControlLimit { get; set; }
        public double LowerSpecLimit { get; set; }
        public double UpperSpecLimit { get; set; }
        public List<SPCDataPoint> DataPoints { get; set; } = new List<SPCDataPoint>();
        public double Cp { get; set; }
        public double Cpk { get; set; }
    }

    public class QualityControlManager
    {
        private static readonly Lazy<QualityControlManager> _instance = new Lazy<QualityControlManager>(() => new QualityControlManager());
        public static QualityControlManager Instance => _instance.Value;

        private readonly Dictionary<string, QualityRule> _rules = new Dictionary<string, QualityRule>();
        private readonly List<QualityAlert> _alerts = new List<QualityAlert>();
        private readonly List<DefectRecord> _defects = new List<DefectRecord>();
        private readonly Dictionary<string, SPCControlChart> _controlCharts = new Dictionary<string, SPCControlChart>();
        private int _consecutiveFailures = 0, _totalUnits = 0, _passedUnits = 0;

        public event EventHandler<QualityAlert>? AlertTriggered;
        public event EventHandler<DefectRecord>? DefectReported;

        private QualityControlManager() { InitializeDefaultRules(); }

        private void InitializeDefaultRules()
        {
            AddRule(new QualityRule { Name = "Consecutive Failures", Type = QualityRuleType.ConsecutiveFails, Threshold = 3, AlertSeverity = DefectSeverity.Critical });
            AddRule(new QualityRule { Name = "Low Yield", Type = QualityRuleType.YieldThreshold, Threshold = 90, AlertSeverity = DefectSeverity.Major });
        }

        public void AddRule(QualityRule rule) => _rules[rule.Id] = rule;
        public void RemoveRule(string ruleId) => _rules.Remove(ruleId);
        public IEnumerable<QualityRule> GetAllRules() => _rules.Values.ToList();

        public void RecordTestResult(string serialNumber, bool passed)
        {
            _totalUnits++;
            if (passed) { _passedUnits++; _consecutiveFailures = 0; }
            else { _consecutiveFailures++; CheckRules(); }
        }

        private void CheckRules()
        {
            foreach (var rule in _rules.Values.Where(r => r.IsEnabled))
            {
                bool triggered = rule.Type switch
                {
                    QualityRuleType.ConsecutiveFails => _consecutiveFailures >= rule.Threshold,
                    QualityRuleType.YieldThreshold => _totalUnits > 0 && (double)_passedUnits / _totalUnits * 100 < rule.Threshold && _totalUnits >= 10,
                    _ => false
                };
                if (triggered) TriggerAlert(rule);
            }
        }

        private void TriggerAlert(QualityRule rule)
        {
            var alert = new QualityAlert { RuleId = rule.Id, RuleName = rule.Name, Severity = rule.AlertSeverity, Message = $"Quality rule '{rule.Name}' triggered" };
            _alerts.Add(alert);
            AlertTriggered?.Invoke(this, alert);
        }

        public void ReportDefect(DefectRecord defect) { _defects.Add(defect); DefectReported?.Invoke(this, defect); }
        public void SetDefectDisposition(string defectId, QualityStatus status, string notes, string by) { var d = _defects.FirstOrDefault(x => x.Id == defectId); if (d != null) { d.DispositionStatus = status; d.DispositionNotes = notes; d.DispositionBy = by; d.DispositionAt = DateTime.Now; } }
        public IEnumerable<DefectRecord> GetDefects(DateTime? startDate = null, DateTime? endDate = null) { var q = _defects.AsEnumerable(); if (startDate.HasValue) q = q.Where(d => d.ReportedAt >= startDate.Value); if (endDate.HasValue) q = q.Where(d => d.ReportedAt <= endDate.Value); return q.ToList(); }
        public IEnumerable<QualityAlert> GetAlerts(bool unacknowledgedOnly = false) { var q = _alerts.AsEnumerable(); if (unacknowledgedOnly) q = q.Where(a => !a.IsAcknowledged); return q.OrderByDescending(a => a.TriggeredAt).ToList(); }
        public void AcknowledgeAlert(string alertId, string by) { var a = _alerts.FirstOrDefault(x => x.Id == alertId); if (a != null) { a.IsAcknowledged = true; a.AcknowledgedBy = by; a.AcknowledgedAt = DateTime.Now; } }
        public QualityMetrics GetCurrentMetrics() => new QualityMetrics { TotalUnits = _totalUnits, PassedUnits = _passedUnits, FailedUnits = _totalUnits - _passedUnits, FirstPassYield = _totalUnits > 0 ? (double)_passedUnits / _totalUnits * 100 : 0, ConsecutiveFailures = _consecutiveFailures, DefectCount = _defects.Count, DefectsPerUnit = _totalUnits > 0 ? (double)_defects.Count / _totalUnits : 0 };

        public void CreateControlChart(string parameterName, double lcl, double centerLine, double ucl, double lsl, double usl)
        {
            _controlCharts[parameterName] = new SPCControlChart { ParameterName = parameterName, LowerControlLimit = lcl, CenterLine = centerLine, UpperControlLimit = ucl, LowerSpecLimit = lsl, UpperSpecLimit = usl };
        }

        public void AddSPCDataPoint(string parameterName, double value)
        {
            if (!_controlCharts.TryGetValue(parameterName, out var chart)) return;
            chart.DataPoints.Add(new SPCDataPoint { ParameterName = parameterName, Value = value, LowerControlLimit = chart.LowerControlLimit, UpperControlLimit = chart.UpperControlLimit, Mean = chart.CenterLine, IsInControl = value >= chart.LowerControlLimit && value <= chart.UpperControlLimit });
            if (chart.DataPoints.Count >= 2)
            {
                var values = chart.DataPoints.Select(p => p.Value).ToList();
                var mean = values.Average();
                var stdDev = Math.Sqrt(values.Average(v => Math.Pow(v - mean, 2)));
                if (stdDev > 0) { chart.Cp = (chart.UpperSpecLimit - chart.LowerSpecLimit) / (6 * stdDev); chart.Cpk = Math.Min((chart.UpperSpecLimit - mean) / (3 * stdDev), (mean - chart.LowerSpecLimit) / (3 * stdDev)); }
            }
        }

        public SPCControlChart? GetControlChart(string parameterName) { _controlCharts.TryGetValue(parameterName, out var c); return c; }
        public IEnumerable<SPCControlChart> GetAllControlCharts() => _controlCharts.Values.ToList();
        public void ResetStatistics() { _consecutiveFailures = 0; _totalUnits = 0; _passedUnits = 0; }
    }
}
