// =============================================================================
// DataTypes.cs - Array, Container, and Object data types
// Part of TestStandClone.Core - TestStand-like test automation framework
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace TestStandClone.Core.DataTypes
{
    /// <summary>
    /// Base class for TestStand-like data types.
    /// </summary>
    public abstract class TestStandDataType
    {
        /// <summary>
        /// Gets the type name.
        /// </summary>
        public abstract string TypeName { get; }

        /// <summary>
        /// Converts the data type to JSON.
        /// </summary>
        public abstract string ToJson();

        /// <summary>
        /// Gets a string representation of the value.
        /// </summary>
        public abstract string GetValueString();
    }

    /// <summary>
    /// Array data type supporting multiple element types.
    /// Similar to TestStand's Array type.
    /// </summary>
    public class TestStandArray : TestStandDataType, IEnumerable<object?>
    {
        private readonly List<object?> _elements = new();

        /// <summary>
        /// Gets the array name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the element type.
        /// </summary>
        public Type ElementType { get; set; } = typeof(object);

        /// <summary>
        /// Gets the number of elements.
        /// </summary>
        public int Count => _elements.Count;

        /// <summary>
        /// Gets or sets an element at the specified index.
        /// </summary>
        public object? this[int index]
        {
            get => _elements[index];
            set => _elements[index] = value;
        }

        public override string TypeName => "Array";

        /// <summary>
        /// Creates a new empty array.
        /// </summary>
        public TestStandArray() { }

        /// <summary>
        /// Creates a new array with initial elements.
        /// </summary>
        public TestStandArray(string name, params object?[] elements)
        {
            Name = name;
            _elements.AddRange(elements);
        }

        /// <summary>
        /// Creates a new array with specified size.
        /// </summary>
        public TestStandArray(string name, int size, object? defaultValue = null)
        {
            Name = name;
            for (int i = 0; i < size; i++)
                _elements.Add(defaultValue);
        }

        /// <summary>
        /// Adds an element to the array.
        /// </summary>
        public void Add(object? element) => _elements.Add(element);

        /// <summary>
        /// Inserts an element at the specified index.
        /// </summary>
        public void Insert(int index, object? element) => _elements.Insert(index, element);

        /// <summary>
        /// Removes an element at the specified index.
        /// </summary>
        public void RemoveAt(int index) => _elements.RemoveAt(index);

        /// <summary>
        /// Clears all elements.
        /// </summary>
        public void Clear() => _elements.Clear();

        /// <summary>
        /// Resizes the array.
        /// </summary>
        public void Resize(int newSize, object? defaultValue = null)
        {
            while (_elements.Count < newSize)
                _elements.Add(defaultValue);
            while (_elements.Count > newSize)
                _elements.RemoveAt(_elements.Count - 1);
        }

        /// <summary>
        /// Gets a subset of elements.
        /// </summary>
        public TestStandArray Subset(int startIndex, int count)
        {
            var result = new TestStandArray($"{Name}_Subset");
            result._elements.AddRange(_elements.Skip(startIndex).Take(count));
            return result;
        }

        /// <summary>
        /// Finds the index of an element.
        /// </summary>
        public int IndexOf(object? element) => _elements.IndexOf(element);

        /// <summary>
        /// Checks if the array contains an element.
        /// </summary>
        public bool Contains(object? element) => _elements.Contains(element);

        /// <summary>
        /// Sorts the array.
        /// </summary>
        public void Sort()
        {
            _elements.Sort((a, b) => Comparer<object>.Default.Compare(a, b));
        }

        /// <summary>
        /// Reverses the array.
        /// </summary>
        public void Reverse() => _elements.Reverse();

        /// <summary>
        /// Converts to a typed array.
        /// </summary>
        public T[] ToTypedArray<T>()
        {
            return _elements.Cast<T>().ToArray();
        }

        public override string ToJson()
        {
            return JsonSerializer.Serialize(_elements);
        }

        public override string GetValueString()
        {
            return $"[{string.Join(", ", _elements.Take(5))}{(_elements.Count > 5 ? "..." : "")}]";
        }

        public IEnumerator<object?> GetEnumerator() => _elements.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Container data type for named properties.
    /// Similar to TestStand's Container type.
    /// </summary>
    public class TestStandContainer : TestStandDataType, IEnumerable<KeyValuePair<string, object?>>
    {
        private readonly Dictionary<string, object?> _properties = new();
        private readonly Dictionary<string, Type> _propertyTypes = new();

        /// <summary>
        /// Gets the container name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets the number of properties.
        /// </summary>
        public int Count => _properties.Count;

        /// <summary>
        /// Gets the property names.
        /// </summary>
        public IEnumerable<string> PropertyNames => _properties.Keys;

        /// <summary>
        /// Gets or sets a property value.
        /// </summary>
        public object? this[string propertyName]
        {
            get => _properties.TryGetValue(propertyName, out var value) ? value : null;
            set => _properties[propertyName] = value;
        }

        public override string TypeName => "Container";

        /// <summary>
        /// Creates a new empty container.
        /// </summary>
        public TestStandContainer() { }

        /// <summary>
        /// Creates a new container with a name.
        /// </summary>
        public TestStandContainer(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Sets a property with type information.
        /// </summary>
        public void SetProperty(string name, object? value, Type? type = null)
        {
            _properties[name] = value;
            _propertyTypes[name] = type ?? value?.GetType() ?? typeof(object);
        }

        /// <summary>
        /// Gets a typed property value.
        /// </summary>
        public T? GetProperty<T>(string name)
        {
            if (_properties.TryGetValue(name, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }

        /// <summary>
        /// Checks if a property exists.
        /// </summary>
        public bool HasProperty(string name) => _properties.ContainsKey(name);

        /// <summary>
        /// Removes a property.
        /// </summary>
        public bool RemoveProperty(string name)
        {
            _propertyTypes.Remove(name);
            return _properties.Remove(name);
        }

        /// <summary>
        /// Gets the type of a property.
        /// </summary>
        public Type? GetPropertyType(string name)
        {
            return _propertyTypes.TryGetValue(name, out var type) ? type : null;
        }

        /// <summary>
        /// Clears all properties.
        /// </summary>
        public void Clear()
        {
            _properties.Clear();
            _propertyTypes.Clear();
        }

        /// <summary>
        /// Merges another container into this one.
        /// </summary>
        public void Merge(TestStandContainer other)
        {
            foreach (var kvp in other._properties)
            {
                _properties[kvp.Key] = kvp.Value;
                if (other._propertyTypes.TryGetValue(kvp.Key, out var type))
                    _propertyTypes[kvp.Key] = type;
            }
        }

        /// <summary>
        /// Creates a shallow copy.
        /// </summary>
        public TestStandContainer Clone()
        {
            var clone = new TestStandContainer(Name);
            foreach (var kvp in _properties)
            {
                clone._properties[kvp.Key] = kvp.Value;
                if (_propertyTypes.TryGetValue(kvp.Key, out var type))
                    clone._propertyTypes[kvp.Key] = type;
            }
            return clone;
        }

        public override string ToJson()
        {
            return JsonSerializer.Serialize(_properties);
        }

        public override string GetValueString()
        {
            return $"{{{string.Join(", ", _properties.Take(3).Select(p => $"{p.Key}={p.Value}"))}}}";
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _properties.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Object reference data type.
    /// Similar to TestStand's Object Reference type.
    /// </summary>
    public class TestStandObjectReference : TestStandDataType
    {
        /// <summary>
        /// Gets or sets the referenced object.
        /// </summary>
        public object? Reference { get; set; }

        /// <summary>
        /// Gets or sets the reference name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reference type name.
        /// </summary>
        public string ReferenceTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Gets whether the reference is valid.
        /// </summary>
        public bool IsValid => Reference != null;

        public override string TypeName => "ObjectReference";

        /// <summary>
        /// Creates a new empty object reference.
        /// </summary>
        public TestStandObjectReference() { }

        /// <summary>
        /// Creates a new object reference.
        /// </summary>
        public TestStandObjectReference(string name, object? reference)
        {
            Name = name;
            Reference = reference;
            ReferenceTypeName = reference?.GetType().Name ?? "null";
        }

        /// <summary>
        /// Gets the referenced object as a typed value.
        /// </summary>
        public T? GetAs<T>() where T : class
        {
            return Reference as T;
        }

        /// <summary>
        /// Releases the reference.
        /// </summary>
        public void Release()
        {
            if (Reference is IDisposable disposable)
                disposable.Dispose();
            Reference = null;
        }

        public override string ToJson()
        {
            return JsonSerializer.Serialize(new { Name, ReferenceTypeName, IsValid });
        }

        public override string GetValueString()
        {
            return IsValid ? $"<{ReferenceTypeName}>" : "<null>";
        }
    }

    /// <summary>
    /// Numeric limit structure.
    /// Similar to TestStand's NumericLimitTest result structure.
    /// </summary>
    public class NumericLimitResult : TestStandDataType
    {
        public string Name { get; set; } = string.Empty;
        public double Measurement { get; set; }
        public double LowLimit { get; set; }
        public double HighLimit { get; set; }
        public string Units { get; set; } = string.Empty;
        public ComparisonType ComparisonType { get; set; } = ComparisonType.GELE;
        public bool Passed { get; set; }

        public override string TypeName => "NumericLimitResult";

        public NumericLimitResult() { }

        public NumericLimitResult(string name, double measurement, double lowLimit, double highLimit)
        {
            Name = name;
            Measurement = measurement;
            LowLimit = lowLimit;
            HighLimit = highLimit;
            Passed = measurement >= lowLimit && measurement <= highLimit;
        }

        public override string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public override string GetValueString()
        {
            return $"{Measurement} ({LowLimit} - {HighLimit}) {(Passed ? "PASS" : "FAIL")}";
        }
    }

    /// <summary>
    /// Comparison types for numeric limits.
    /// </summary>
    public enum ComparisonType
    {
        /// <summary>Equal</summary>
        EQ,
        /// <summary>Not Equal</summary>
        NE,
        /// <summary>Greater Than</summary>
        GT,
        /// <summary>Greater Than or Equal</summary>
        GE,
        /// <summary>Less Than</summary>
        LT,
        /// <summary>Less Than or Equal</summary>
        LE,
        /// <summary>Greater Than or Equal and Less Than or Equal (default)</summary>
        GELE,
        /// <summary>Greater Than and Less Than</summary>
        GTLT,
        /// <summary>Greater Than or Equal and Less Than</summary>
        GELT,
        /// <summary>Greater Than and Less Than or Equal</summary>
        GTLE,
        /// <summary>Log base 10</summary>
        LOG
    }

    /// <summary>
    /// String value result structure.
    /// Similar to TestStand's StringValueTest result structure.
    /// </summary>
    public class StringValueResult : TestStandDataType
    {
        public string Name { get; set; } = string.Empty;
        public string ActualValue { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public StringComparisonType ComparisonType { get; set; } = StringComparisonType.Equal;
        public bool CaseSensitive { get; set; } = false;
        public bool Passed { get; set; }

        public override string TypeName => "StringValueResult";

        public StringValueResult() { }

        public StringValueResult(string name, string actual, string expected, StringComparisonType comparisonType = StringComparisonType.Equal)
        {
            Name = name;
            ActualValue = actual;
            ExpectedValue = expected;
            ComparisonType = comparisonType;
            Evaluate();
        }

        public void Evaluate()
        {
            var comparison = CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            
            Passed = ComparisonType switch
            {
                StringComparisonType.Equal => ActualValue.Equals(ExpectedValue, comparison),
                StringComparisonType.NotEqual => !ActualValue.Equals(ExpectedValue, comparison),
                StringComparisonType.Contains => ActualValue.Contains(ExpectedValue, comparison),
                StringComparisonType.StartsWith => ActualValue.StartsWith(ExpectedValue, comparison),
                StringComparisonType.EndsWith => ActualValue.EndsWith(ExpectedValue, comparison),
                StringComparisonType.Regex => System.Text.RegularExpressions.Regex.IsMatch(ActualValue, ExpectedValue),
                _ => false
            };
        }

        public override string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public override string GetValueString()
        {
            return $"'{ActualValue}' {ComparisonType} '{ExpectedValue}' {(Passed ? "PASS" : "FAIL")}";
        }
    }

    /// <summary>
    /// String comparison types.
    /// </summary>
    public enum StringComparisonType
    {
        Equal,
        NotEqual,
        Contains,
        StartsWith,
        EndsWith,
        Regex
    }

    /// <summary>
    /// Waveform data type.
    /// Similar to TestStand's Waveform type.
    /// </summary>
    public class TestStandWaveform : TestStandDataType
    {
        /// <summary>
        /// Gets or sets the waveform name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data points.
        /// </summary>
        public double[] Data { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Gets or sets the sample rate in Hz.
        /// </summary>
        public double SampleRate { get; set; } = 1000.0;

        /// <summary>
        /// Gets or sets the start time in seconds.
        /// </summary>
        public double StartTime { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets the units.
        /// </summary>
        public string Units { get; set; } = "V";

        /// <summary>
        /// Gets the number of samples.
        /// </summary>
        public int SampleCount => Data.Length;

        /// <summary>
        /// Gets the duration in seconds.
        /// </summary>
        public double Duration => SampleRate > 0 ? Data.Length / SampleRate : 0;

        public override string TypeName => "Waveform";

        public TestStandWaveform() { }

        public TestStandWaveform(string name, double[] data, double sampleRate = 1000.0)
        {
            Name = name;
            Data = data;
            SampleRate = sampleRate;
        }

        /// <summary>
        /// Gets the minimum value.
        /// </summary>
        public double Min => Data.Length > 0 ? Data.Min() : 0;

        /// <summary>
        /// Gets the maximum value.
        /// </summary>
        public double Max => Data.Length > 0 ? Data.Max() : 0;

        /// <summary>
        /// Gets the average value.
        /// </summary>
        public double Average => Data.Length > 0 ? Data.Average() : 0;

        /// <summary>
        /// Gets the RMS value.
        /// </summary>
        public double RMS => Data.Length > 0 ? Math.Sqrt(Data.Sum(x => x * x) / Data.Length) : 0;

        public override string ToJson()
        {
            return JsonSerializer.Serialize(new { Name, SampleCount, SampleRate, StartTime, Units, Min, Max, Average });
        }

        public override string GetValueString()
        {
            return $"Waveform: {SampleCount} samples @ {SampleRate}Hz";
        }
    }
}
