using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TestStandClone.Core.Variables
{
    /// <summary>
    /// Represents a variable with a name, type, and value.
    /// Similar to TestStand's variable system.
    /// </summary>
    public class Variable : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private object? _value;
        private VariableType _type = VariableType.String;
        private string _description = string.Empty;

        /// <summary>
        /// Name of the variable.
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
        /// Value of the variable.
        /// </summary>
        public object? Value
        {
            get => _value;
            set
            {
                if (!Equals(_value, value))
                {
                    _value = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StringValue));
                }
            }
        }

        /// <summary>
        /// Type of the variable.
        /// </summary>
        public VariableType Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Description of the variable.
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// String representation of the value.
        /// </summary>
        public string StringValue => Value?.ToString() ?? string.Empty;

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        public Variable()
        {
        }

        /// <summary>
        /// Creates a new variable with name and value.
        /// </summary>
        public Variable(string name, object? value, VariableType type = VariableType.String)
        {
            Name = name;
            Value = value;
            Type = type;
        }

        /// <summary>
        /// Gets the value as a specific type.
        /// </summary>
        public T? GetValue<T>()
        {
            if (Value is T typedValue)
            {
                return typedValue;
            }

            try
            {
                var converted = Convert.ChangeType(Value, typeof(T));
                return converted != null ? (T)converted : default;
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Sets the value with type conversion.
        /// </summary>
        public void SetValue<T>(T value)
        {
            Value = value;
            Type = GetVariableType(typeof(T));
        }

        private static VariableType GetVariableType(Type type)
        {
            if (type == typeof(bool)) return VariableType.Boolean;
            if (type == typeof(int) || type == typeof(long)) return VariableType.Integer;
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return VariableType.Number;
            if (type == typeof(string)) return VariableType.String;
            if (type.IsArray) return VariableType.Array;
            return VariableType.Object;
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
    /// Type of variable.
    /// </summary>
    public enum VariableType
    {
        String,
        Number,
        Integer,
        Boolean,
        Array,
        Object
    }
}
