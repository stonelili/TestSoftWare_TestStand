// RuntimeVariables.cs - Variables that persist during runtime
// Provides runtime variable management with persistence options

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TestStandClone.Core.RuntimeVariables
{
    /// <summary>
    /// Runtime variable scope
    /// </summary>
    public enum RuntimeVariableScope
    {
        /// <summary>Step-level variable (lifetime of step execution)</summary>
        Step,
        /// <summary>Sequence-level variable (lifetime of sequence execution)</summary>
        Sequence,
        /// <summary>Execution-level variable (lifetime of test execution)</summary>
        Execution,
        /// <summary>Session-level variable (lifetime of test session)</summary>
        Session,
        /// <summary>Station-level variable (persists across sessions)</summary>
        Station,
        /// <summary>Global variable (persists across restarts)</summary>
        Global
    }

    /// <summary>
    /// Runtime variable persistence mode
    /// </summary>
    public enum VariablePersistence
    {
        /// <summary>Variable is not persisted</summary>
        None,
        /// <summary>Variable is persisted to file</summary>
        File,
        /// <summary>Variable is persisted to database</summary>
        Database,
        /// <summary>Variable is persisted to registry (Windows only)</summary>
        Registry
    }

    /// <summary>
    /// Represents a runtime variable
    /// </summary>
    public class RuntimeVariable : INotifyPropertyChanged
    {
        private object? _value;
        private bool _isLocked;

        /// <summary>Variable name</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Variable value</summary>
        public object? Value
        {
            get => _value;
            set
            {
                if (_isLocked)
                    throw new InvalidOperationException($"Variable '{Name}' is locked and cannot be modified");

                if (!Equals(_value, value))
                {
                    var oldValue = _value;
                    _value = value;
                    LastModified = DateTime.Now;
                    ModificationCount++;
                    OnPropertyChanged();
                    ValueChanged?.Invoke(this, new VariableChangedEventArgs(Name, oldValue, value));
                }
            }
        }

        /// <summary>Variable type name</summary>
        public string TypeName { get; set; } = "System.Object";

        /// <summary>Variable scope</summary>
        public RuntimeVariableScope Scope { get; set; } = RuntimeVariableScope.Execution;

        /// <summary>Persistence mode</summary>
        public VariablePersistence Persistence { get; set; } = VariablePersistence.None;

        /// <summary>Whether the variable is read-only</summary>
        public bool IsReadOnly { get; set; }

        /// <summary>Whether the variable is locked</summary>
        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (_isLocked != value)
                {
                    _isLocked = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Description</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Default value</summary>
        public object? DefaultValue { get; set; }

        /// <summary>Creation timestamp</summary>
        public DateTime Created { get; set; } = DateTime.Now;

        /// <summary>Last modification timestamp</summary>
        public DateTime LastModified { get; set; } = DateTime.Now;

        /// <summary>Number of times the variable was modified</summary>
        public int ModificationCount { get; set; }

        /// <summary>Tags for categorization</summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>Event raised when value changes</summary>
        public event EventHandler<VariableChangedEventArgs>? ValueChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Reset to default value
        /// </summary>
        public void Reset()
        {
            Value = DefaultValue;
        }

        /// <summary>
        /// Get value as specific type
        /// </summary>
        public T? GetValue<T>()
        {
            if (Value == null)
                return default;

            if (Value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(Value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Event args for variable changes
    /// </summary>
    public class VariableChangedEventArgs : EventArgs
    {
        public string VariableName { get; }
        public object? OldValue { get; }
        public object? NewValue { get; }

        public VariableChangedEventArgs(string name, object? oldValue, object? newValue)
        {
            VariableName = name;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    /// <summary>
    /// Variable collection for a specific scope
    /// </summary>
    public class VariableCollection
    {
        private readonly ConcurrentDictionary<string, RuntimeVariable> _variables = new();

        /// <summary>Scope of this collection</summary>
        public RuntimeVariableScope Scope { get; }

        /// <summary>Event raised when a variable changes</summary>
        public event EventHandler<VariableChangedEventArgs>? VariableChanged;

        public VariableCollection(RuntimeVariableScope scope)
        {
            Scope = scope;
        }

        /// <summary>
        /// Add or update a variable
        /// </summary>
        public RuntimeVariable Set(string name, object? value, string? description = null)
        {
            if (_variables.TryGetValue(name, out var existing))
            {
                existing.Value = value;
                return existing;
            }

            var variable = new RuntimeVariable
            {
                Name = name,
                Value = value,
                DefaultValue = value,
                Scope = Scope,
                Description = description ?? string.Empty
            };

            variable.ValueChanged += (s, e) => VariableChanged?.Invoke(s, e);
            _variables[name] = variable;

            return variable;
        }

        /// <summary>
        /// Get a variable
        /// </summary>
        public RuntimeVariable? Get(string name)
        {
            _variables.TryGetValue(name, out var variable);
            return variable;
        }

        /// <summary>
        /// Get a variable value
        /// </summary>
        public object? GetValue(string name)
        {
            return Get(name)?.Value;
        }

        /// <summary>
        /// Get a variable value as specific type
        /// </summary>
        public object? GetValueTyped(string name, Type targetType)
        {
            var variable = Get(name);
            if (variable == null || variable.Value == null)
                return null;
            
            try
            {
                return Convert.ChangeType(variable.Value, targetType);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Check if variable exists
        /// </summary>
        public bool Contains(string name)
        {
            return _variables.ContainsKey(name);
        }

        /// <summary>
        /// Remove a variable
        /// </summary>
        public bool Remove(string name)
        {
            return _variables.TryRemove(name, out _);
        }

        /// <summary>
        /// Get all variables
        /// </summary>
        public IEnumerable<RuntimeVariable> GetAll()
        {
            return _variables.Values;
        }

        /// <summary>
        /// Get all variable names
        /// </summary>
        public IEnumerable<string> GetNames()
        {
            return _variables.Keys;
        }

        /// <summary>
        /// Clear all variables
        /// </summary>
        public void Clear()
        {
            _variables.Clear();
        }

        /// <summary>
        /// Reset all variables to default values
        /// </summary>
        public void ResetAll()
        {
            foreach (var variable in _variables.Values)
            {
                variable.Reset();
            }
        }

        /// <summary>
        /// Variable count
        /// </summary>
        public int Count => _variables.Count;
    }

    /// <summary>
    /// Runtime variable manager
    /// </summary>
    public class RuntimeVariableManager
    {
        private static RuntimeVariableManager? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<RuntimeVariableScope, VariableCollection> _collections = new();
        private string _persistenceDirectory = string.Empty;

        public static RuntimeVariableManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new RuntimeVariableManager();
                    }
                }
                return _instance;
            }
        }

        /// <summary>Event raised when any variable changes</summary>
        public event EventHandler<VariableChangedEventArgs>? VariableChanged;

        private RuntimeVariableManager()
        {
            // Initialize collections for each scope
            foreach (RuntimeVariableScope scope in Enum.GetValues(typeof(RuntimeVariableScope)))
            {
                var collection = new VariableCollection(scope);
                collection.VariableChanged += (s, e) => VariableChanged?.Invoke(s, e);
                _collections[scope] = collection;
            }
        }

        /// <summary>
        /// Set persistence directory
        /// </summary>
        public void SetPersistenceDirectory(string directory)
        {
            _persistenceDirectory = directory;
            if (!Directory.Exists(_persistenceDirectory))
            {
                Directory.CreateDirectory(_persistenceDirectory);
            }
        }

        /// <summary>
        /// Get collection for scope
        /// </summary>
        public VariableCollection GetCollection(RuntimeVariableScope scope)
        {
            return _collections[scope];
        }

        /// <summary>
        /// Set a variable
        /// </summary>
        public RuntimeVariable Set(string name, object? value, RuntimeVariableScope scope = RuntimeVariableScope.Execution, string? description = null)
        {
            return _collections[scope].Set(name, value, description);
        }

        /// <summary>
        /// Get a variable value
        /// </summary>
        public object? GetValue(string name, RuntimeVariableScope? scope = null)
        {
            if (scope.HasValue)
            {
                return _collections[scope.Value].GetValue(name);
            }

            // Search all scopes from most specific to least
            foreach (var s in new[] { RuntimeVariableScope.Step, RuntimeVariableScope.Sequence, RuntimeVariableScope.Execution, RuntimeVariableScope.Session, RuntimeVariableScope.Station, RuntimeVariableScope.Global })
            {
                var value = _collections[s].GetValue(name);
                if (value != null)
                    return value;
            }

            return null;
        }

        /// <summary>
        /// Get a variable value as specific type
        /// </summary>
        public T? GetValue<T>(string name, RuntimeVariableScope? scope = null)
        {
            var value = GetValue(name, scope);
            if (value == null)
                return default;

            if (value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Get a variable
        /// </summary>
        public RuntimeVariable? Get(string name, RuntimeVariableScope? scope = null)
        {
            if (scope.HasValue)
            {
                return _collections[scope.Value].Get(name);
            }

            foreach (var s in Enum.GetValues(typeof(RuntimeVariableScope)).Cast<RuntimeVariableScope>())
            {
                var variable = _collections[s].Get(name);
                if (variable != null)
                    return variable;
            }

            return null;
        }

        /// <summary>
        /// Check if variable exists
        /// </summary>
        public bool Contains(string name, RuntimeVariableScope? scope = null)
        {
            if (scope.HasValue)
            {
                return _collections[scope.Value].Contains(name);
            }

            return _collections.Values.Any(c => c.Contains(name));
        }

        /// <summary>
        /// Remove a variable
        /// </summary>
        public bool Remove(string name, RuntimeVariableScope scope)
        {
            return _collections[scope].Remove(name);
        }

        /// <summary>
        /// Clear variables for a scope
        /// </summary>
        public void ClearScope(RuntimeVariableScope scope)
        {
            _collections[scope].Clear();
        }

        /// <summary>
        /// Clear all variables
        /// </summary>
        public void ClearAll()
        {
            foreach (var collection in _collections.Values)
            {
                collection.Clear();
            }
        }

        /// <summary>
        /// Get all variables across all scopes
        /// </summary>
        public IEnumerable<RuntimeVariable> GetAllVariables()
        {
            return _collections.Values.SelectMany(c => c.GetAll());
        }

        /// <summary>
        /// Save persistent variables to file
        /// </summary>
        public async Task SavePersistentVariablesAsync()
        {
            if (string.IsNullOrEmpty(_persistenceDirectory))
            {
                _persistenceDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TestStandClone", "Variables");
                if (!Directory.Exists(_persistenceDirectory))
                {
                    Directory.CreateDirectory(_persistenceDirectory);
                }
            }

            var persistentVariables = GetAllVariables()
                .Where(v => v.Persistence == VariablePersistence.File)
                .Select(v => new PersistedVariable
                {
                    Name = v.Name,
                    Value = v.Value?.ToString(),
                    TypeName = v.TypeName,
                    Scope = v.Scope,
                    Description = v.Description
                })
                .ToList();

            var filePath = Path.Combine(_persistenceDirectory, "variables.json");
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var json = JsonSerializer.Serialize(persistentVariables, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Load persistent variables from file
        /// </summary>
        public async Task LoadPersistentVariablesAsync()
        {
            if (string.IsNullOrEmpty(_persistenceDirectory))
            {
                _persistenceDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TestStandClone", "Variables");
            }

            var filePath = Path.Combine(_persistenceDirectory, "variables.json");
            if (!File.Exists(filePath))
                return;

            try
            {
                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() }
                };

                var json = await File.ReadAllTextAsync(filePath);
                var persistedVariables = JsonSerializer.Deserialize<List<PersistedVariable>>(json, options);

                if (persistedVariables != null)
                {
                    foreach (var pv in persistedVariables)
                    {
                        var variable = Set(pv.Name, pv.Value, pv.Scope, pv.Description);
                        variable.TypeName = pv.TypeName;
                        variable.Persistence = VariablePersistence.File;
                    }
                }
            }
            catch
            {
                // Skip if file is invalid
            }
        }

        /// <summary>
        /// DTO for persisted variables
        /// </summary>
        private class PersistedVariable
        {
            public string Name { get; set; } = string.Empty;
            public string? Value { get; set; }
            public string TypeName { get; set; } = "System.String";
            public RuntimeVariableScope Scope { get; set; }
            public string Description { get; set; } = string.Empty;
        }
    }
}
