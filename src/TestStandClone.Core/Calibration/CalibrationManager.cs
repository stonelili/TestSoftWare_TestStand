using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace TestStandClone.Core.Calibration
{
    /// <summary>
    /// Calibration status
    /// </summary>
    public enum CalibrationStatus
    {
        /// <summary>Not calibrated</summary>
        NotCalibrated,
        /// <summary>Calibration is valid</summary>
        Valid,
        /// <summary>Calibration is expired</summary>
        Expired,
        /// <summary>Calibration due soon</summary>
        DueSoon,
        /// <summary>Calibration failed</summary>
        Failed
    }

    /// <summary>
    /// Represents a calibration record for an instrument or equipment
    /// </summary>
    public class CalibrationRecord
    {
        /// <summary>Unique identifier</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>Equipment identifier</summary>
        public string EquipmentId { get; set; } = string.Empty;
        /// <summary>Equipment name</summary>
        public string EquipmentName { get; set; } = string.Empty;
        /// <summary>Equipment type</summary>
        public string EquipmentType { get; set; } = string.Empty;
        /// <summary>Serial number</summary>
        public string SerialNumber { get; set; } = string.Empty;
        /// <summary>Manufacturer</summary>
        public string Manufacturer { get; set; } = string.Empty;
        /// <summary>Model number</summary>
        public string Model { get; set; } = string.Empty;
        /// <summary>Last calibration date</summary>
        public DateTime LastCalibrationDate { get; set; } = DateTime.MinValue;
        /// <summary>Next calibration due date</summary>
        public DateTime NextCalibrationDue { get; set; } = DateTime.MinValue;
        /// <summary>Calibration interval in days</summary>
        public int CalibrationIntervalDays { get; set; } = 365;
        /// <summary>Calibration laboratory</summary>
        public string CalibrationLab { get; set; } = string.Empty;
        /// <summary>Certificate number</summary>
        public string CertificateNumber { get; set; } = string.Empty;
        /// <summary>Technician who performed calibration</summary>
        public string Technician { get; set; } = string.Empty;
        /// <summary>Calibration procedure used</summary>
        public string Procedure { get; set; } = string.Empty;
        /// <summary>Notes about the calibration</summary>
        public string Notes { get; set; } = string.Empty;
        /// <summary>Whether the calibration passed</summary>
        public bool CalibrationPassed { get; set; } = true;
        /// <summary>Calibration data points</summary>
        public List<CalibrationDataPoint> DataPoints { get; set; } = new();
        /// <summary>Adjustment factors</summary>
        public Dictionary<string, double> AdjustmentFactors { get; set; } = new();
        /// <summary>Associated file paths (certificates, reports)</summary>
        public List<string> AttachedFiles { get; set; } = new();

        /// <summary>Gets the calibration status</summary>
        public CalibrationStatus Status
        {
            get
            {
                if (LastCalibrationDate == DateTime.MinValue) return CalibrationStatus.NotCalibrated;
                if (!CalibrationPassed) return CalibrationStatus.Failed;
                if (DateTime.Now > NextCalibrationDue) return CalibrationStatus.Expired;
                if ((NextCalibrationDue - DateTime.Now).TotalDays <= 30) return CalibrationStatus.DueSoon;
                return CalibrationStatus.Valid;
            }
        }

        /// <summary>Gets days until calibration is due</summary>
        public int DaysUntilDue => Math.Max(0, (NextCalibrationDue - DateTime.Now).Days);

        /// <summary>Gets days since last calibration</summary>
        public int DaysSinceLastCalibration => (DateTime.Now - LastCalibrationDate).Days;
    }

    /// <summary>
    /// Calibration data point
    /// </summary>
    public class CalibrationDataPoint
    {
        /// <summary>Parameter name</summary>
        public string Parameter { get; set; } = string.Empty;
        /// <summary>Nominal value</summary>
        public double NominalValue { get; set; }
        /// <summary>Measured value</summary>
        public double MeasuredValue { get; set; }
        /// <summary>Tolerance</summary>
        public double Tolerance { get; set; }
        /// <summary>Unit of measurement</summary>
        public string Unit { get; set; } = string.Empty;
        /// <summary>Whether the point passed</summary>
        public bool Passed => Math.Abs(MeasuredValue - NominalValue) <= Tolerance;
        /// <summary>Error value</summary>
        public double Error => MeasuredValue - NominalValue;
        /// <summary>Error percentage</summary>
        public double ErrorPercent => NominalValue != 0 ? (Error / NominalValue) * 100 : 0;
    }

    /// <summary>
    /// Calibration schedule entry
    /// </summary>
    public class CalibrationScheduleEntry
    {
        /// <summary>Equipment ID</summary>
        public string EquipmentId { get; set; } = string.Empty;
        /// <summary>Equipment name</summary>
        public string EquipmentName { get; set; } = string.Empty;
        /// <summary>Due date</summary>
        public DateTime DueDate { get; set; }
        /// <summary>Priority (1=highest)</summary>
        public int Priority { get; set; } = 3;
        /// <summary>Assigned technician</summary>
        public string AssignedTo { get; set; } = string.Empty;
        /// <summary>Status</summary>
        public CalibrationStatus Status { get; set; }
        /// <summary>Notes</summary>
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Interface for calibration data store
    /// </summary>
    public interface ICalibrationStore
    {
        /// <summary>Saves a calibration record</summary>
        void Save(CalibrationRecord record);
        /// <summary>Loads a calibration record by ID</summary>
        CalibrationRecord? Load(string id);
        /// <summary>Loads all calibration records</summary>
        List<CalibrationRecord> LoadAll();
        /// <summary>Deletes a calibration record</summary>
        bool Delete(string id);
    }

    /// <summary>
    /// JSON file-based calibration store
    /// </summary>
    public class JsonCalibrationStore : ICalibrationStore
    {
        private readonly string _directory;

        /// <summary>Creates a new JSON calibration store</summary>
        public JsonCalibrationStore(string directory)
        {
            _directory = directory;
            if (!Directory.Exists(_directory))
            {
                Directory.CreateDirectory(_directory);
            }
        }

        /// <inheritdoc/>
        public void Save(CalibrationRecord record)
        {
            string path = Path.Combine(_directory, $"{record.Id}.json");
            string json = JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        /// <inheritdoc/>
        public CalibrationRecord? Load(string id)
        {
            string path = Path.Combine(_directory, $"{id}.json");
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<CalibrationRecord>(json);
        }

        /// <inheritdoc/>
        public List<CalibrationRecord> LoadAll()
        {
            var records = new List<CalibrationRecord>();
            if (!Directory.Exists(_directory)) return records;

            foreach (string file in Directory.GetFiles(_directory, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var record = JsonSerializer.Deserialize<CalibrationRecord>(json);
                    if (record != null) records.Add(record);
                }
                catch { /* Skip invalid files */ }
            }
            return records;
        }

        /// <inheritdoc/>
        public bool Delete(string id)
        {
            string path = Path.Combine(_directory, $"{id}.json");
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Calibration manager singleton
    /// </summary>
    public sealed class CalibrationManager
    {
        private static readonly Lazy<CalibrationManager> _instance = 
            new Lazy<CalibrationManager>(() => new CalibrationManager());
        
        /// <summary>Gets the singleton instance</summary>
        public static CalibrationManager Instance => _instance.Value;

        private ICalibrationStore _store;
        private readonly List<CalibrationRecord> _records = new();
        private readonly object _lockObject = new object();

        /// <summary>Event raised when a record is added or updated</summary>
        public event EventHandler<CalibrationRecord>? RecordUpdated;

        /// <summary>Event raised when calibration is due</summary>
        public event EventHandler<CalibrationRecord>? CalibrationDue;

        private CalibrationManager()
        {
            _store = new JsonCalibrationStore("calibration");
            LoadAll();
        }

        /// <summary>Sets the calibration store</summary>
        public void SetStore(ICalibrationStore store)
        {
            _store = store;
            LoadAll();
        }

        /// <summary>Gets all calibration records</summary>
        public IReadOnlyList<CalibrationRecord> Records => _records.AsReadOnly();

        /// <summary>Adds or updates a calibration record</summary>
        public void SaveRecord(CalibrationRecord record)
        {
            lock (_lockObject)
            {
                _store.Save(record);
                var existing = _records.FirstOrDefault(r => r.Id == record.Id);
                if (existing != null)
                {
                    _records.Remove(existing);
                }
                _records.Add(record);
                RecordUpdated?.Invoke(this, record);
            }
        }

        /// <summary>Gets a calibration record by ID</summary>
        public CalibrationRecord? GetRecord(string id)
        {
            lock (_lockObject)
            {
                return _records.FirstOrDefault(r => r.Id == id);
            }
        }

        /// <summary>Gets calibration record by equipment ID</summary>
        public CalibrationRecord? GetRecordByEquipmentId(string equipmentId)
        {
            lock (_lockObject)
            {
                return _records.FirstOrDefault(r => r.EquipmentId == equipmentId);
            }
        }

        /// <summary>Deletes a calibration record</summary>
        public bool DeleteRecord(string id)
        {
            lock (_lockObject)
            {
                if (_store.Delete(id))
                {
                    var record = _records.FirstOrDefault(r => r.Id == id);
                    if (record != null)
                    {
                        _records.Remove(record);
                    }
                    return true;
                }
                return false;
            }
        }

        /// <summary>Loads all records from store</summary>
        private void LoadAll()
        {
            lock (_lockObject)
            {
                _records.Clear();
                _records.AddRange(_store.LoadAll());
            }
        }

        /// <summary>Gets records due for calibration</summary>
        public List<CalibrationRecord> GetDueRecords(int withinDays = 30)
        {
            lock (_lockObject)
            {
                return _records.Where(r => 
                    r.DaysUntilDue <= withinDays || 
                    r.Status == CalibrationStatus.Expired ||
                    r.Status == CalibrationStatus.DueSoon)
                    .OrderBy(r => r.NextCalibrationDue)
                    .ToList();
            }
        }

        /// <summary>Gets expired calibration records</summary>
        public List<CalibrationRecord> GetExpiredRecords()
        {
            lock (_lockObject)
            {
                return _records.Where(r => r.Status == CalibrationStatus.Expired)
                    .OrderBy(r => r.NextCalibrationDue)
                    .ToList();
            }
        }

        /// <summary>Gets the calibration schedule</summary>
        public List<CalibrationScheduleEntry> GetSchedule(DateTime startDate, DateTime endDate)
        {
            lock (_lockObject)
            {
                return _records
                    .Where(r => r.NextCalibrationDue >= startDate && r.NextCalibrationDue <= endDate)
                    .Select(r => new CalibrationScheduleEntry
                    {
                        EquipmentId = r.EquipmentId,
                        EquipmentName = r.EquipmentName,
                        DueDate = r.NextCalibrationDue,
                        Status = r.Status
                    })
                    .OrderBy(e => e.DueDate)
                    .ToList();
            }
        }

        /// <summary>Checks all calibrations and raises events for due items</summary>
        public void CheckCalibrations()
        {
            lock (_lockObject)
            {
                foreach (var record in _records)
                {
                    if (record.Status == CalibrationStatus.DueSoon || 
                        record.Status == CalibrationStatus.Expired)
                    {
                        CalibrationDue?.Invoke(this, record);
                    }
                }
            }
        }

        /// <summary>Exports calibration records to CSV</summary>
        public void ExportToCsv(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Id,EquipmentId,EquipmentName,Type,SerialNumber,Manufacturer,Model,LastCalibration,NextDue,Status,Lab,Certificate");
            
            lock (_lockObject)
            {
                foreach (var r in _records)
                {
                    sb.AppendLine($"\"{r.Id}\",\"{r.EquipmentId}\",\"{r.EquipmentName}\",\"{r.EquipmentType}\",\"{r.SerialNumber}\",\"{r.Manufacturer}\",\"{r.Model}\",\"{r.LastCalibrationDate:yyyy-MM-dd}\",\"{r.NextCalibrationDue:yyyy-MM-dd}\",\"{r.Status}\",\"{r.CalibrationLab}\",\"{r.CertificateNumber}\"");
                }
            }
            
            File.WriteAllText(filePath, sb.ToString());
        }

        /// <summary>Gets calibration statistics</summary>
        public Dictionary<string, int> GetStatistics()
        {
            lock (_lockObject)
            {
                return new Dictionary<string, int>
                {
                    ["Total"] = _records.Count,
                    ["Valid"] = _records.Count(r => r.Status == CalibrationStatus.Valid),
                    ["Expired"] = _records.Count(r => r.Status == CalibrationStatus.Expired),
                    ["DueSoon"] = _records.Count(r => r.Status == CalibrationStatus.DueSoon),
                    ["NotCalibrated"] = _records.Count(r => r.Status == CalibrationStatus.NotCalibrated),
                    ["Failed"] = _records.Count(r => r.Status == CalibrationStatus.Failed)
                };
            }
        }
    }
}
