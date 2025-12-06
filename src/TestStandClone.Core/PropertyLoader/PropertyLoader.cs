using System.Text.Json;

namespace TestStandClone.Core.PropertyLoader
{
    /// <summary>
    /// Loads step properties from external files.
    /// Similar to TestStand's property loader for dynamic configuration.
    /// </summary>
    public class PropertyLoader
    {
        private readonly Dictionary<string, Dictionary<string, object?>> _propertyCache;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Creates a new PropertyLoader.
        /// </summary>
        public PropertyLoader()
        {
            _propertyCache = new Dictionary<string, Dictionary<string, object?>>();
        }

        /// <summary>
        /// Loads properties from a JSON file.
        /// </summary>
        /// <param name="filePath">Path to the property file.</param>
        /// <returns>Dictionary of property name-value pairs.</returns>
        public Dictionary<string, object?> LoadFromJson(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            lock (_lockObject)
            {
                if (_propertyCache.TryGetValue(filePath, out var cached))
                {
                    return new Dictionary<string, object?>(cached);
                }

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Property file not found: {filePath}");
                }

                var json = File.ReadAllText(filePath);
                var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                
                var result = new Dictionary<string, object?>();
                if (properties != null)
                {
                    foreach (var kvp in properties)
                    {
                        result[kvp.Key] = ConvertJsonElement(kvp.Value);
                    }
                }

                _propertyCache[filePath] = result;
                return new Dictionary<string, object?>(result);
            }
        }

        /// <summary>
        /// Converts a JsonElement to a .NET object.
        /// </summary>
        private static object? ConvertJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var longVal) ? longVal : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => ConvertJsonArray(element),
                JsonValueKind.Object => ConvertJsonObject(element),
                _ => element.ToString()
            };
        }

        /// <summary>
        /// Converts a JSON array to a .NET list.
        /// </summary>
        private static List<object?> ConvertJsonArray(JsonElement element)
        {
            var list = new List<object?>();
            foreach (var item in element.EnumerateArray())
            {
                list.Add(ConvertJsonElement(item));
            }
            return list;
        }

        /// <summary>
        /// Converts a JSON object to a .NET dictionary.
        /// </summary>
        private static Dictionary<string, object?> ConvertJsonObject(JsonElement element)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var prop in element.EnumerateObject())
            {
                dict[prop.Name] = ConvertJsonElement(prop.Value);
            }
            return dict;
        }

        /// <summary>
        /// Loads properties from a CSV file.
        /// </summary>
        /// <param name="filePath">Path to the CSV file.</param>
        /// <param name="delimiter">CSV delimiter character.</param>
        /// <returns>Dictionary of property name-value pairs.</returns>
        public Dictionary<string, object?> LoadFromCsv(string filePath, char delimiter = ',')
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            lock (_lockObject)
            {
                var cacheKey = $"{filePath}|csv|{delimiter}";
                if (_propertyCache.TryGetValue(cacheKey, out var cached))
                {
                    return new Dictionary<string, object?>(cached);
                }

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Property file not found: {filePath}");
                }

                var result = new Dictionary<string, object?>();
                var lines = File.ReadAllLines(filePath);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    {
                        continue;
                    }

                    var parts = line.Split(delimiter, 2);
                    if (parts.Length == 2)
                    {
                        var name = parts[0].Trim();
                        var value = parts[1].Trim();
                        result[name] = ParseValue(value);
                    }
                }

                _propertyCache[cacheKey] = result;
                return new Dictionary<string, object?>(result);
            }
        }

        /// <summary>
        /// Parses a string value to appropriate type.
        /// </summary>
        private static object? ParseValue(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (bool.TryParse(value, out var boolVal))
            {
                return boolVal;
            }

            if (int.TryParse(value, out var intVal))
            {
                return intVal;
            }

            if (double.TryParse(value, out var doubleVal))
            {
                return doubleVal;
            }

            return value;
        }

        /// <summary>
        /// Applies properties to a sequence.
        /// </summary>
        /// <param name="sequence">The sequence to apply properties to.</param>
        /// <param name="properties">The properties to apply.</param>
        public void ApplyToSequence(Sequence sequence, Dictionary<string, object?> properties)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }

            foreach (var step in sequence.Steps)
            {
                ApplyToStep(step, properties);
            }
        }

        /// <summary>
        /// Applies properties to a step.
        /// Property names should be in format "StepName.PropertyName" or just "PropertyName" for all steps.
        /// </summary>
        /// <param name="step">The step to apply properties to.</param>
        /// <param name="properties">The properties to apply.</param>
        public void ApplyToStep(TestStep step, Dictionary<string, object?> properties)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            var stepType = step.GetType();

            foreach (var kvp in properties)
            {
                var parts = kvp.Key.Split('.', 2);
                string propertyName;

                if (parts.Length == 2)
                {
                    // Format: StepName.PropertyName
                    if (!step.Name.Equals(parts[0], StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    propertyName = parts[1];
                }
                else
                {
                    // Format: PropertyName (applies to all steps)
                    propertyName = parts[0];
                }

                var property = stepType.GetProperty(propertyName);
                if (property != null && property.CanWrite && kvp.Value != null)
                {
                    try
                    {
                        var convertedValue = Convert.ChangeType(kvp.Value, property.PropertyType);
                        property.SetValue(step, convertedValue);
                    }
                    catch
                    {
                        // Skip properties that can't be converted
                    }
                }
            }
        }

        /// <summary>
        /// Clears the property cache.
        /// </summary>
        public void ClearCache()
        {
            lock (_lockObject)
            {
                _propertyCache.Clear();
            }
        }

        /// <summary>
        /// Saves properties to a JSON file.
        /// </summary>
        /// <param name="properties">Properties to save.</param>
        /// <param name="filePath">Path to save to.</param>
        public void SaveToJson(Dictionary<string, object?> properties, string filePath)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var json = JsonSerializer.Serialize(properties, new JsonSerializerOptions { WriteIndented = true });
            
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Extracts properties from a step.
        /// </summary>
        /// <param name="step">The step to extract properties from.</param>
        /// <returns>Dictionary of property name-value pairs.</returns>
        public Dictionary<string, object?> ExtractFromStep(TestStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            var result = new Dictionary<string, object?>();
            var stepType = step.GetType();

            foreach (var property in stepType.GetProperties())
            {
                if (property.CanRead && property.PropertyType.IsPublic)
                {
                    try
                    {
                        var value = property.GetValue(step);
                        if (IsSimpleType(property.PropertyType))
                        {
                            result[$"{step.Name}.{property.Name}"] = value;
                        }
                    }
                    catch
                    {
                        // Skip properties that can't be read
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if a type is a simple type that can be serialized.
        /// </summary>
        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive || 
                   type == typeof(string) || 
                   type == typeof(decimal) || 
                   type == typeof(DateTime) || 
                   type == typeof(Guid) ||
                   type.IsEnum;
        }
    }

    /// <summary>
    /// Configuration for step properties.
    /// </summary>
    public class StepPropertyConfiguration
    {
        /// <summary>
        /// Step name pattern (supports wildcards).
        /// </summary>
        public string StepPattern { get; set; } = "*";

        /// <summary>
        /// Properties to apply.
        /// </summary>
        public Dictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
    }

    /// <summary>
    /// Property file format.
    /// </summary>
    public enum PropertyFileFormat
    {
        Json,
        Csv,
        Ini
    }
}
