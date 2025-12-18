using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace TestStandClone.Core.Limits
{
    /// <summary>
    /// Manages test limits from a database or file.
    /// Similar to TestStand's Limits Loader.
    /// </summary>
    public class LimitsDatabase : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _version = "1.0";

        /// <summary>
        /// Name of the limits database.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Version of the limits database.
        /// </summary>
        public string Version
        {
            get => _version;
            set
            {
                if (_version != value)
                {
                    _version = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Collection of limit entries.
        /// </summary>
        public ObservableCollection<LimitEntry> Limits { get; } = new ObservableCollection<LimitEntry>();

        /// <summary>
        /// Gets a limit entry by step name.
        /// </summary>
        public LimitEntry? GetLimit(string stepName)
        {
            return Limits.FirstOrDefault(l => l.StepName.Equals(stepName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a numeric limit by step name.
        /// </summary>
        public (double low, double high)? GetNumericLimits(string stepName)
        {
            var limit = GetLimit(stepName);
            if (limit?.LimitType == LimitType.Numeric && limit.LowLimit.HasValue && limit.HighLimit.HasValue)
            {
                return (limit.LowLimit.Value, limit.HighLimit.Value);
            }
            return null;
        }

        /// <summary>
        /// Adds or updates a limit entry.
        /// </summary>
        public void SetLimit(LimitEntry entry)
        {
            var existing = GetLimit(entry.StepName);
            if (existing != null)
            {
                Limits.Remove(existing);
            }
            Limits.Add(entry);
        }

        /// <summary>
        /// Loads limits from a JSON file.
        /// </summary>
        public async Task LoadFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var data = JsonSerializer.Deserialize<LimitsDatabaseDto>(json);

            if (data != null)
            {
                Name = data.Name ?? "";
                Version = data.Version ?? "1.0";
                Limits.Clear();
                
                foreach (var limitDto in data.Limits ?? new List<LimitEntryDto>())
                {
                    Limits.Add(new LimitEntry
                    {
                        StepName = limitDto.StepName ?? "",
                        LimitType = Enum.TryParse<LimitType>(limitDto.LimitType, out var lt) ? lt : LimitType.Numeric,
                        LowLimit = limitDto.LowLimit,
                        HighLimit = limitDto.HighLimit,
                        ExpectedValue = limitDto.ExpectedValue,
                        Units = limitDto.Units ?? "",
                        Description = limitDto.Description ?? ""
                    });
                }
            }
        }

        /// <summary>
        /// Saves limits to a JSON file.
        /// </summary>
        public async Task SaveToFileAsync(string filePath)
        {
            var data = new LimitsDatabaseDto
            {
                Name = Name,
                Version = Version,
                Limits = Limits.Select(l => new LimitEntryDto
                {
                    StepName = l.StepName,
                    LimitType = l.LimitType.ToString(),
                    LowLimit = l.LowLimit,
                    HighLimit = l.HighLimit,
                    ExpectedValue = l.ExpectedValue,
                    Units = l.Units,
                    Description = l.Description
                }).ToList()
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Applies limits from this database to a sequence.
        /// </summary>
        public void ApplyToSequence(Sequence sequence)
        {
            foreach (var step in sequence.Steps)
            {
                if (step is NumericLimitStep numericStep)
                {
                    var limits = GetNumericLimits(step.Name);
                    if (limits.HasValue)
                    {
                        numericStep.LowerLimit = limits.Value.low;
                        numericStep.UpperLimit = limits.Value.high;
                    }
                }
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// A single limit entry.
    /// </summary>
    public class LimitEntry
    {
        public string StepName { get; set; } = string.Empty;
        public LimitType LimitType { get; set; } = LimitType.Numeric;
        public double? LowLimit { get; set; }
        public double? HighLimit { get; set; }
        public string? ExpectedValue { get; set; }
        public string Units { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Type of limit.
    /// </summary>
    public enum LimitType
    {
        Numeric,
        String,
        Boolean
    }

    // DTOs for serialization
    internal class LimitsDatabaseDto
    {
        public string? Name { get; set; }
        public string? Version { get; set; }
        public List<LimitEntryDto>? Limits { get; set; }
    }

    internal class LimitEntryDto
    {
        public string? StepName { get; set; }
        public string? LimitType { get; set; }
        public double? LowLimit { get; set; }
        public double? HighLimit { get; set; }
        public string? ExpectedValue { get; set; }
        public string? Units { get; set; }
        public string? Description { get; set; }
    }
}
