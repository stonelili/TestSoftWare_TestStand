using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;

namespace TestStandClone.Core.LimitsEditor
{
    /// <summary>
    /// Represents a single limit definition
    /// </summary>
    public class LimitDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string StepName { get; set; } = string.Empty;
        public string ParameterName { get; set; } = string.Empty;
        public LimitType Type { get; set; } = LimitType.NumericLimit;
        public double? LowLimit { get; set; }
        public double? HighLimit { get; set; }
        public string? StringValue { get; set; }
        public ComparisonType Comparison { get; set; } = ComparisonType.InRange;
        public string Units { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public Dictionary<string, object?> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Limit types
    /// </summary>
    public enum LimitType
    {
        NumericLimit,
        StringLimit,
        BooleanLimit
    }

    /// <summary>
    /// Comparison types for limits
    /// </summary>
    public enum ComparisonType
    {
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        InRange,
        OutOfRange,
        Contains,
        StartsWith,
        EndsWith,
        RegexMatch
    }

    /// <summary>
    /// Represents a limit file
    /// </summary>
    public class LimitFile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public string Author { get; set; } = string.Empty;
        public List<LimitDefinition> Limits { get; set; } = new();
        public Dictionary<string, object?> Properties { get; set; } = new();
    }

    /// <summary>
    /// Editor for creating and modifying limit files
    /// </summary>
    public class LimitsEditor
    {
        private LimitFile _currentFile;
        private readonly Stack<LimitFile> _undoStack = new();
        private readonly Stack<LimitFile> _redoStack = new();
        private bool _isDirty;

        public LimitsEditor()
        {
            _currentFile = new LimitFile();
        }

        public LimitFile CurrentFile => _currentFile;
        public bool IsDirty => _isDirty;

        /// <summary>
        /// Creates a new limit file
        /// </summary>
        public void NewFile(string name)
        {
            SaveUndo();
            _currentFile = new LimitFile { Name = name };
            _isDirty = false;
            _redoStack.Clear();
        }

        /// <summary>
        /// Loads a limit file from disk
        /// </summary>
        public void LoadFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var file = JsonSerializer.Deserialize<LimitFile>(json);
                if (file != null)
                {
                    SaveUndo();
                    _currentFile = file;
                    _currentFile.FilePath = filePath;
                    _isDirty = false;
                    _redoStack.Clear();
                }
            }
        }

        /// <summary>
        /// Saves the limit file to disk
        /// </summary>
        public void SaveToFile(string? filePath = null)
        {
            var path = filePath ?? _currentFile.FilePath;
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("No file path specified");
            }

            _currentFile.FilePath = path;
            _currentFile.ModifiedDate = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(_currentFile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            _isDirty = false;
        }

        /// <summary>
        /// Adds a limit definition
        /// </summary>
        public LimitDefinition AddLimit(string stepName, string parameterName, LimitType type)
        {
            SaveUndo();
            var limit = new LimitDefinition
            {
                StepName = stepName,
                ParameterName = parameterName,
                Type = type
            };
            _currentFile.Limits.Add(limit);
            _isDirty = true;
            _redoStack.Clear();
            return limit;
        }

        /// <summary>
        /// Adds a numeric limit
        /// </summary>
        public LimitDefinition AddNumericLimit(string stepName, string parameterName, double? low, double? high, string units = "")
        {
            var limit = AddLimit(stepName, parameterName, LimitType.NumericLimit);
            limit.LowLimit = low;
            limit.HighLimit = high;
            limit.Units = units;
            limit.Comparison = ComparisonType.InRange;
            return limit;
        }

        /// <summary>
        /// Adds a string limit
        /// </summary>
        public LimitDefinition AddStringLimit(string stepName, string parameterName, string expectedValue, ComparisonType comparison = ComparisonType.Equal)
        {
            var limit = AddLimit(stepName, parameterName, LimitType.StringLimit);
            limit.StringValue = expectedValue;
            limit.Comparison = comparison;
            return limit;
        }

        /// <summary>
        /// Updates a limit definition
        /// </summary>
        public bool UpdateLimit(LimitDefinition updatedLimit)
        {
            var index = _currentFile.Limits.FindIndex(l => l.Id == updatedLimit.Id);
            if (index >= 0)
            {
                SaveUndo();
                _currentFile.Limits[index] = updatedLimit;
                _isDirty = true;
                _redoStack.Clear();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a limit definition
        /// </summary>
        public bool RemoveLimit(string limitId)
        {
            var limit = _currentFile.Limits.FirstOrDefault(l => l.Id == limitId);
            if (limit != null)
            {
                SaveUndo();
                _currentFile.Limits.Remove(limit);
                _isDirty = true;
                _redoStack.Clear();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets limits for a specific step
        /// </summary>
        public List<LimitDefinition> GetLimitsForStep(string stepName)
        {
            return _currentFile.Limits.Where(l => l.StepName == stepName).ToList();
        }

        /// <summary>
        /// Gets limits by category
        /// </summary>
        public List<LimitDefinition> GetLimitsByCategory(string category)
        {
            return _currentFile.Limits.Where(l => l.Category == category).ToList();
        }

        /// <summary>
        /// Duplicates a limit
        /// </summary>
        public LimitDefinition DuplicateLimit(string limitId)
        {
            var original = _currentFile.Limits.FirstOrDefault(l => l.Id == limitId);
            if (original != null)
            {
                SaveUndo();
                var json = JsonSerializer.Serialize(original);
                var duplicate = JsonSerializer.Deserialize<LimitDefinition>(json)!;
                duplicate.Id = Guid.NewGuid().ToString();
                duplicate.StepName = original.StepName + "_Copy";
                _currentFile.Limits.Add(duplicate);
                _isDirty = true;
                _redoStack.Clear();
                return duplicate;
            }
            throw new ArgumentException($"Limit with ID {limitId} not found");
        }

        /// <summary>
        /// Imports limits from CSV
        /// </summary>
        public void ImportFromCsv(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("CSV file not found", filePath);
            }

            SaveUndo();
            var lines = File.ReadAllLines(filePath);
            
            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');
                if (values.Length >= 4)
                {
                    var limit = new LimitDefinition
                    {
                        StepName = values[0].Trim(),
                        ParameterName = values[1].Trim()
                    };

                    if (double.TryParse(values[2].Trim(), out var low))
                    {
                        limit.LowLimit = low;
                    }
                    if (double.TryParse(values[3].Trim(), out var high))
                    {
                        limit.HighLimit = high;
                    }
                    if (values.Length > 4)
                    {
                        limit.Units = values[4].Trim();
                    }

                    _currentFile.Limits.Add(limit);
                }
            }
            _isDirty = true;
            _redoStack.Clear();
        }

        /// <summary>
        /// Exports limits to CSV
        /// </summary>
        public void ExportToCsv(string filePath)
        {
            var lines = new List<string>
            {
                "StepName,ParameterName,LowLimit,HighLimit,Units,Type,Comparison"
            };

            foreach (var limit in _currentFile.Limits)
            {
                lines.Add($"{limit.StepName},{limit.ParameterName},{limit.LowLimit},{limit.HighLimit},{limit.Units},{limit.Type},{limit.Comparison}");
            }

            File.WriteAllLines(filePath, lines);
        }

        /// <summary>
        /// Validates a measurement against limits
        /// </summary>
        public LimitValidationResult ValidateMeasurement(string stepName, string parameterName, object value)
        {
            var limit = _currentFile.Limits.FirstOrDefault(l => 
                l.StepName == stepName && 
                l.ParameterName == parameterName && 
                l.IsEnabled);

            if (limit == null)
            {
                return new LimitValidationResult
                {
                    IsValid = true,
                    Message = "No limit defined"
                };
            }

            return ValidateAgainstLimit(limit, value);
        }

        private LimitValidationResult ValidateAgainstLimit(LimitDefinition limit, object value)
        {
            var result = new LimitValidationResult { Limit = limit };

            try
            {
                switch (limit.Type)
                {
                    case LimitType.NumericLimit:
                        if (value is double numValue || double.TryParse(value?.ToString(), out numValue))
                        {
                            result = ValidateNumericLimit(limit, numValue);
                        }
                        else
                        {
                            result.IsValid = false;
                            result.Message = "Value is not numeric";
                        }
                        break;

                    case LimitType.StringLimit:
                        result = ValidateStringLimit(limit, value?.ToString() ?? "");
                        break;

                    case LimitType.BooleanLimit:
                        if (value is bool boolValue)
                        {
                            result.IsValid = boolValue.ToString().Equals(limit.StringValue, StringComparison.OrdinalIgnoreCase);
                            result.Message = result.IsValid ? "Pass" : "Boolean value mismatch";
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"Validation error: {ex.Message}";
            }

            return result;
        }

        private LimitValidationResult ValidateNumericLimit(LimitDefinition limit, double value)
        {
            var result = new LimitValidationResult { Limit = limit, ActualValue = value };

            switch (limit.Comparison)
            {
                case ComparisonType.InRange:
                    result.IsValid = (!limit.LowLimit.HasValue || value >= limit.LowLimit.Value) &&
                                    (!limit.HighLimit.HasValue || value <= limit.HighLimit.Value);
                    break;
                case ComparisonType.OutOfRange:
                    result.IsValid = (limit.LowLimit.HasValue && value < limit.LowLimit.Value) ||
                                    (limit.HighLimit.HasValue && value > limit.HighLimit.Value);
                    break;
                case ComparisonType.Equal:
                    result.IsValid = limit.LowLimit.HasValue && Math.Abs(value - limit.LowLimit.Value) < 0.0001;
                    break;
                case ComparisonType.NotEqual:
                    result.IsValid = !limit.LowLimit.HasValue || Math.Abs(value - limit.LowLimit.Value) >= 0.0001;
                    break;
                case ComparisonType.LessThan:
                    result.IsValid = limit.HighLimit.HasValue && value < limit.HighLimit.Value;
                    break;
                case ComparisonType.LessThanOrEqual:
                    result.IsValid = limit.HighLimit.HasValue && value <= limit.HighLimit.Value;
                    break;
                case ComparisonType.GreaterThan:
                    result.IsValid = limit.LowLimit.HasValue && value > limit.LowLimit.Value;
                    break;
                case ComparisonType.GreaterThanOrEqual:
                    result.IsValid = limit.LowLimit.HasValue && value >= limit.LowLimit.Value;
                    break;
            }

            result.Message = result.IsValid ? "Pass" : $"Value {value} is out of limits [{limit.LowLimit}, {limit.HighLimit}]";
            return result;
        }

        private LimitValidationResult ValidateStringLimit(LimitDefinition limit, string value)
        {
            var result = new LimitValidationResult { Limit = limit, ActualValue = value };
            var expected = limit.StringValue ?? "";

            switch (limit.Comparison)
            {
                case ComparisonType.Equal:
                    result.IsValid = value.Equals(expected, StringComparison.OrdinalIgnoreCase);
                    break;
                case ComparisonType.NotEqual:
                    result.IsValid = !value.Equals(expected, StringComparison.OrdinalIgnoreCase);
                    break;
                case ComparisonType.Contains:
                    result.IsValid = value.Contains(expected, StringComparison.OrdinalIgnoreCase);
                    break;
                case ComparisonType.StartsWith:
                    result.IsValid = value.StartsWith(expected, StringComparison.OrdinalIgnoreCase);
                    break;
                case ComparisonType.EndsWith:
                    result.IsValid = value.EndsWith(expected, StringComparison.OrdinalIgnoreCase);
                    break;
                case ComparisonType.RegexMatch:
                    try
                    {
                        result.IsValid = System.Text.RegularExpressions.Regex.IsMatch(value, expected);
                    }
                    catch
                    {
                        result.IsValid = false;
                        result.Message = "Invalid regex pattern";
                        return result;
                    }
                    break;
            }

            result.Message = result.IsValid ? "Pass" : $"String '{value}' does not match expected '{expected}'";
            return result;
        }

        /// <summary>
        /// Undoes the last change
        /// </summary>
        public bool Undo()
        {
            if (_undoStack.Count > 0)
            {
                _redoStack.Push(CloneFile(_currentFile));
                _currentFile = _undoStack.Pop();
                _isDirty = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Redoes the last undone change
        /// </summary>
        public bool Redo()
        {
            if (_redoStack.Count > 0)
            {
                _undoStack.Push(CloneFile(_currentFile));
                _currentFile = _redoStack.Pop();
                _isDirty = true;
                return true;
            }
            return false;
        }

        private void SaveUndo()
        {
            _undoStack.Push(CloneFile(_currentFile));
            if (_undoStack.Count > 50)
            {
                var list = _undoStack.ToList();
                list.RemoveAt(list.Count - 1);
                _undoStack.Clear();
                foreach (var item in list.AsEnumerable().Reverse())
                {
                    _undoStack.Push(item);
                }
            }
        }

        private LimitFile CloneFile(LimitFile file)
        {
            var json = JsonSerializer.Serialize(file);
            return JsonSerializer.Deserialize<LimitFile>(json) ?? new LimitFile();
        }
    }

    /// <summary>
    /// Result of limit validation
    /// </summary>
    public class LimitValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public LimitDefinition? Limit { get; set; }
        public object? ActualValue { get; set; }
    }

    /// <summary>
    /// Manager for limit files
    /// </summary>
    public class LimitFileManager
    {
        private static readonly Lazy<LimitFileManager> _instance = 
            new(() => new LimitFileManager());
        
        public static LimitFileManager Instance => _instance.Value;

        private readonly Dictionary<string, LimitFile> _loadedFiles = new();
        private string _limitsDirectory = "Limits";

        private LimitFileManager() { }

        /// <summary>
        /// Sets the limits directory
        /// </summary>
        public void SetLimitsDirectory(string path)
        {
            _limitsDirectory = path;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Loads a limit file
        /// </summary>
        public LimitFile LoadFile(string name)
        {
            if (_loadedFiles.TryGetValue(name, out var file))
            {
                return file;
            }

            var path = Path.Combine(_limitsDirectory, $"{name}.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var loadedFile = JsonSerializer.Deserialize<LimitFile>(json);
                if (loadedFile != null)
                {
                    _loadedFiles[name] = loadedFile;
                    return loadedFile;
                }
            }

            throw new FileNotFoundException($"Limit file '{name}' not found");
        }

        /// <summary>
        /// Gets all available limit files
        /// </summary>
        public IEnumerable<string> GetAvailableFiles()
        {
            if (Directory.Exists(_limitsDirectory))
            {
                return Directory.GetFiles(_limitsDirectory, "*.json")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(n => n != null)
                    .Cast<string>();
            }
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Unloads a limit file
        /// </summary>
        public void UnloadFile(string name)
        {
            _loadedFiles.Remove(name);
        }

        /// <summary>
        /// Gets a loaded file
        /// </summary>
        public LimitFile? GetLoadedFile(string name)
        {
            return _loadedFiles.TryGetValue(name, out var file) ? file : null;
        }
    }
}
