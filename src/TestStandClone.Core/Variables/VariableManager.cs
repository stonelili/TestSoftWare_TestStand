using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TestStandClone.Core.Variables
{
    /// <summary>
    /// Manages variables at different scopes: Local, FileGlobal, StationGlobal.
    /// Similar to TestStand's variable management system.
    /// </summary>
    public class VariableManager : INotifyPropertyChanged
    {
        private static VariableManager? _stationInstance;

        /// <summary>
        /// Station global variables (persist across all sequences).
        /// </summary>
        public static VariableManager StationGlobals
        {
            get
            {
                _stationInstance ??= new VariableManager(VariableScope.StationGlobal);
                return _stationInstance;
            }
        }

        /// <summary>
        /// Scope of this variable manager.
        /// </summary>
        public VariableScope Scope { get; }

        /// <summary>
        /// Collection of variables.
        /// </summary>
        public ObservableCollection<Variable> Variables { get; } = new ObservableCollection<Variable>();

        /// <summary>
        /// Creates a new variable manager with specified scope.
        /// </summary>
        public VariableManager(VariableScope scope = VariableScope.Local)
        {
            Scope = scope;
        }

        /// <summary>
        /// Gets a variable by name.
        /// </summary>
        public Variable? GetVariable(string name)
        {
            return Variables.FirstOrDefault(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a variable value by name.
        /// </summary>
        public T? GetValue<T>(string name)
        {
            var variable = GetVariable(name);
            return variable != null ? variable.GetValue<T>() : default;
        }

        /// <summary>
        /// Sets a variable value, creating if it doesn't exist.
        /// </summary>
        public void SetValue<T>(string name, T value)
        {
            var variable = GetVariable(name);
            if (variable == null)
            {
                variable = new Variable(name, value);
                Variables.Add(variable);
            }
            else
            {
                variable.SetValue(value);
            }
        }

        /// <summary>
        /// Adds a new variable.
        /// </summary>
        public void AddVariable(Variable variable)
        {
            if (GetVariable(variable.Name) == null)
            {
                Variables.Add(variable);
            }
        }

        /// <summary>
        /// Removes a variable by name.
        /// </summary>
        public bool RemoveVariable(string name)
        {
            var variable = GetVariable(name);
            if (variable != null)
            {
                return Variables.Remove(variable);
            }
            return false;
        }

        /// <summary>
        /// Checks if a variable exists.
        /// </summary>
        public bool HasVariable(string name)
        {
            return GetVariable(name) != null;
        }

        /// <summary>
        /// Clears all variables.
        /// </summary>
        public void Clear()
        {
            Variables.Clear();
        }

        /// <summary>
        /// Creates a dictionary of all variables.
        /// </summary>
        public Dictionary<string, object?> ToDictionary()
        {
            return Variables.ToDictionary(v => v.Name, v => v.Value);
        }

        /// <summary>
        /// Loads variables from a dictionary.
        /// </summary>
        public void FromDictionary(Dictionary<string, object?> values)
        {
            Clear();
            foreach (var kvp in values)
            {
                Variables.Add(new Variable(kvp.Key, kvp.Value));
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
    /// Scope of variables.
    /// </summary>
    public enum VariableScope
    {
        /// <summary>
        /// Variables local to the current sequence execution.
        /// </summary>
        Local,

        /// <summary>
        /// Variables shared across all sequences in a file.
        /// </summary>
        FileGlobal,

        /// <summary>
        /// Variables that persist across all executions (station-wide).
        /// </summary>
        StationGlobal
    }
}
