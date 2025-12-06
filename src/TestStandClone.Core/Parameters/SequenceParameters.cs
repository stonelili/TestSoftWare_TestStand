// =============================================================================
// SequenceParameters.cs - Input/Output parameters for sequences
// Part of TestStandClone.Core - TestStand-like test automation framework
// =============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;

namespace TestStandClone.Core.Parameters
{
    /// <summary>
    /// Represents a sequence parameter.
    /// Similar to TestStand's sequence parameters.
    /// </summary>
    public class SequenceParameter : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private object? _value;
        private ParameterDirection _direction;
        private string _description = string.Empty;
        private bool _required;

        /// <summary>
        /// Gets or sets the parameter name.
        /// </summary>
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        /// <summary>
        /// Gets or sets the parameter value.
        /// </summary>
        public object? Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(nameof(Value)); }
        }

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the parameter type.
        /// </summary>
        public Type ValueType { get; set; } = typeof(object);

        /// <summary>
        /// Gets or sets the parameter direction.
        /// </summary>
        public ParameterDirection Direction
        {
            get => _direction;
            set { _direction = value; OnPropertyChanged(nameof(Direction)); }
        }

        /// <summary>
        /// Gets or sets the parameter description.
        /// </summary>
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        /// <summary>
        /// Gets or sets whether the parameter is required.
        /// </summary>
        public bool Required
        {
            get => _required;
            set { _required = value; OnPropertyChanged(nameof(Required)); }
        }

        /// <summary>
        /// Gets or sets the valid values (for enumerated parameters).
        /// </summary>
        public List<object>? ValidValues { get; set; }

        /// <summary>
        /// Gets or sets the minimum value (for numeric parameters).
        /// </summary>
        public object? MinValue { get; set; }

        /// <summary>
        /// Gets or sets the maximum value (for numeric parameters).
        /// </summary>
        public object? MaxValue { get; set; }

        /// <summary>
        /// Creates a new parameter.
        /// </summary>
        public SequenceParameter() { }

        /// <summary>
        /// Creates a new parameter with initial values.
        /// </summary>
        public SequenceParameter(string name, object? defaultValue, ParameterDirection direction = ParameterDirection.Input)
        {
            Name = name;
            DefaultValue = defaultValue;
            Value = defaultValue;
            Direction = direction;
            ValueType = defaultValue?.GetType() ?? typeof(object);
        }

        /// <summary>
        /// Gets the typed value.
        /// </summary>
        public T? GetValue<T>()
        {
            if (Value is T typedValue)
                return typedValue;
            
            try
            {
                return (T?)Convert.ChangeType(Value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Validates the parameter value.
        /// </summary>
        public bool Validate(out string? errorMessage)
        {
            errorMessage = null;

            if (Required && Value == null)
            {
                errorMessage = $"Parameter '{Name}' is required";
                return false;
            }

            if (ValidValues != null && Value != null && !ValidValues.Contains(Value))
            {
                errorMessage = $"Parameter '{Name}' has invalid value";
                return false;
            }

            if (MinValue != null && Value is IComparable comparableMin && 
                comparableMin.CompareTo(MinValue) < 0)
            {
                errorMessage = $"Parameter '{Name}' is below minimum value";
                return false;
            }

            if (MaxValue != null && Value is IComparable comparableMax && 
                comparableMax.CompareTo(MaxValue) > 0)
            {
                errorMessage = $"Parameter '{Name}' is above maximum value";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Resets the parameter to its default value.
        /// </summary>
        public void Reset()
        {
            Value = DefaultValue;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Parameter direction.
    /// </summary>
    public enum ParameterDirection
    {
        /// <summary>Input parameter</summary>
        Input,
        /// <summary>Output parameter</summary>
        Output,
        /// <summary>Input/Output parameter</summary>
        InputOutput
    }

    /// <summary>
    /// Manager for sequence parameters.
    /// </summary>
    public class SequenceParameterManager
    {
        private readonly List<SequenceParameter> _parameters = new();

        /// <summary>
        /// Gets all parameters.
        /// </summary>
        public IReadOnlyList<SequenceParameter> Parameters => _parameters.AsReadOnly();

        /// <summary>
        /// Gets input parameters.
        /// </summary>
        public IEnumerable<SequenceParameter> InputParameters =>
            _parameters.Where(p => p.Direction == ParameterDirection.Input || 
                                   p.Direction == ParameterDirection.InputOutput);

        /// <summary>
        /// Gets output parameters.
        /// </summary>
        public IEnumerable<SequenceParameter> OutputParameters =>
            _parameters.Where(p => p.Direction == ParameterDirection.Output || 
                                   p.Direction == ParameterDirection.InputOutput);

        /// <summary>
        /// Adds a parameter.
        /// </summary>
        public void AddParameter(SequenceParameter parameter)
        {
            _parameters.Add(parameter);
        }

        /// <summary>
        /// Adds an input parameter.
        /// </summary>
        public SequenceParameter AddInput(string name, object? defaultValue, string description = "")
        {
            var param = new SequenceParameter(name, defaultValue, ParameterDirection.Input)
            {
                Description = description
            };
            _parameters.Add(param);
            return param;
        }

        /// <summary>
        /// Adds an output parameter.
        /// </summary>
        public SequenceParameter AddOutput(string name, object? defaultValue = null, string description = "")
        {
            var param = new SequenceParameter(name, defaultValue, ParameterDirection.Output)
            {
                Description = description
            };
            _parameters.Add(param);
            return param;
        }

        /// <summary>
        /// Adds an input/output parameter.
        /// </summary>
        public SequenceParameter AddInputOutput(string name, object? defaultValue, string description = "")
        {
            var param = new SequenceParameter(name, defaultValue, ParameterDirection.InputOutput)
            {
                Description = description
            };
            _parameters.Add(param);
            return param;
        }

        /// <summary>
        /// Gets a parameter by name.
        /// </summary>
        public SequenceParameter? GetParameter(string name)
        {
            return _parameters.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a parameter value.
        /// </summary>
        public T? GetValue<T>(string name)
        {
            var param = GetParameter(name);
            return param != null ? param.GetValue<T>() : default;
        }

        /// <summary>
        /// Sets a parameter value.
        /// </summary>
        public void SetValue(string name, object? value)
        {
            var param = GetParameter(name);
            if (param != null)
                param.Value = value;
        }

        /// <summary>
        /// Removes a parameter.
        /// </summary>
        public bool RemoveParameter(string name)
        {
            var param = GetParameter(name);
            return param != null && _parameters.Remove(param);
        }

        /// <summary>
        /// Validates all required parameters.
        /// </summary>
        public bool ValidateAll(out List<string> errors)
        {
            errors = new List<string>();
            
            foreach (var param in _parameters)
            {
                if (!param.Validate(out var error) && error != null)
                    errors.Add(error);
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// Resets all parameters to default values.
        /// </summary>
        public void ResetAll()
        {
            foreach (var param in _parameters)
                param.Reset();
        }

        /// <summary>
        /// Clears all parameters.
        /// </summary>
        public void Clear()
        {
            _parameters.Clear();
        }

        /// <summary>
        /// Applies input values from a dictionary.
        /// </summary>
        public void ApplyInputs(Dictionary<string, object?> inputs)
        {
            foreach (var kvp in inputs)
            {
                SetValue(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Collects output values to a dictionary.
        /// </summary>
        public Dictionary<string, object?> CollectOutputs()
        {
            return OutputParameters.ToDictionary(p => p.Name, p => p.Value);
        }

        /// <summary>
        /// Serializes parameters to JSON.
        /// </summary>
        public string ToJson()
        {
            var data = _parameters.Select(p => new
            {
                p.Name,
                p.Value,
                p.DefaultValue,
                Direction = p.Direction.ToString(),
                Type = p.ValueType.Name,
                p.Description,
                p.Required
            });
            return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Creates a clone of the parameter manager.
        /// </summary>
        public SequenceParameterManager Clone()
        {
            var clone = new SequenceParameterManager();
            foreach (var param in _parameters)
            {
                clone.AddParameter(new SequenceParameter(param.Name, param.DefaultValue, param.Direction)
                {
                    Value = param.Value,
                    Description = param.Description,
                    Required = param.Required,
                    ValueType = param.ValueType,
                    ValidValues = param.ValidValues,
                    MinValue = param.MinValue,
                    MaxValue = param.MaxValue
                });
            }
            return clone;
        }
    }
}
