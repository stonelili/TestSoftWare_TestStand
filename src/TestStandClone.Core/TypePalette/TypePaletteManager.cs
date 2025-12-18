using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TestStandClone.Core.TypePalette
{
    /// <summary>
    /// Type category
    /// </summary>
    public enum TypeCategory
    {
        Numeric,
        String,
        Boolean,
        Array,
        Container,
        Reference,
        Custom
    }

    /// <summary>
    /// Custom type definition
    /// </summary>
    public class TypeDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TypeCategory Category { get; set; }
        public string? BaseType { get; set; }
        public bool IsBuiltIn { get; set; }
        public List<PropertyDefinition> Properties { get; set; } = new();
        public Dictionary<string, object> DefaultValues { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        public string? Author { get; set; }
        public string Version { get; set; } = "1.0.0";
    }

    /// <summary>
    /// Property definition within a type
    /// </summary>
    public class PropertyDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TypeName { get; set; } = "String";
        public bool IsRequired { get; set; }
        public bool IsReadOnly { get; set; }
        public object? DefaultValue { get; set; }
        public object? MinValue { get; set; }
        public object? MaxValue { get; set; }
        public List<object>? AllowedValues { get; set; }
        public string? ValidationExpression { get; set; }
        public int DisplayOrder { get; set; }
        public string? DisplayGroup { get; set; }
    }

    /// <summary>
    /// Type instance
    /// </summary>
    public class TypeInstance
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TypeId { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public Dictionary<string, object?> Values { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Type palette manager for custom types
    /// </summary>
    public class TypePaletteManager
    {
        private static readonly Lazy<TypePaletteManager> _instance = new(() => new TypePaletteManager());
        public static TypePaletteManager Instance => _instance.Value;

        private readonly Dictionary<string, TypeDefinition> _types = new();
        private readonly object _lock = new();
        private string _storageDirectory;

        public event EventHandler<TypeEventArgs>? TypeAdded;
        public event EventHandler<TypeEventArgs>? TypeRemoved;
        public event EventHandler<TypeEventArgs>? TypeModified;

        private TypePaletteManager()
        {
            _storageDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TestStandClone", "Types");
            Directory.CreateDirectory(_storageDirectory);

            // Register built-in types
            RegisterBuiltInTypes();
        }

        /// <summary>
        /// Storage directory
        /// </summary>
        public string StorageDirectory
        {
            get => _storageDirectory;
            set
            {
                _storageDirectory = value;
                Directory.CreateDirectory(_storageDirectory);
            }
        }

        /// <summary>
        /// Register built-in types
        /// </summary>
        private void RegisterBuiltInTypes()
        {
            // Numeric types
            RegisterType(new TypeDefinition
            {
                Name = "Number",
                Category = TypeCategory.Numeric,
                IsBuiltIn = true,
                Description = "A numeric value (double precision)"
            });

            RegisterType(new TypeDefinition
            {
                Name = "Integer",
                Category = TypeCategory.Numeric,
                IsBuiltIn = true,
                Description = "An integer value"
            });

            // String type
            RegisterType(new TypeDefinition
            {
                Name = "String",
                Category = TypeCategory.String,
                IsBuiltIn = true,
                Description = "A text string"
            });

            // Boolean type
            RegisterType(new TypeDefinition
            {
                Name = "Boolean",
                Category = TypeCategory.Boolean,
                IsBuiltIn = true,
                Description = "A true/false value"
            });

            // Array type
            RegisterType(new TypeDefinition
            {
                Name = "Array",
                Category = TypeCategory.Array,
                IsBuiltIn = true,
                Description = "An array of values",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition { Name = "ElementType", TypeName = "String", DefaultValue = "Number" },
                    new PropertyDefinition { Name = "Length", TypeName = "Integer", DefaultValue = 0 }
                }
            });

            // Container type
            RegisterType(new TypeDefinition
            {
                Name = "Container",
                Category = TypeCategory.Container,
                IsBuiltIn = true,
                Description = "A container with named properties"
            });

            // Reference type
            RegisterType(new TypeDefinition
            {
                Name = "Reference",
                Category = TypeCategory.Reference,
                IsBuiltIn = true,
                Description = "A reference to another object"
            });

            // Numeric Limit Result type
            RegisterType(new TypeDefinition
            {
                Name = "NumericLimitResult",
                Category = TypeCategory.Custom,
                IsBuiltIn = true,
                Description = "Result of a numeric limit test",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition { Name = "Value", TypeName = "Number", IsRequired = true },
                    new PropertyDefinition { Name = "LowLimit", TypeName = "Number" },
                    new PropertyDefinition { Name = "HighLimit", TypeName = "Number" },
                    new PropertyDefinition { Name = "Units", TypeName = "String" },
                    new PropertyDefinition { Name = "Status", TypeName = "String" }
                }
            });

            // String Value Result type
            RegisterType(new TypeDefinition
            {
                Name = "StringValueResult",
                Category = TypeCategory.Custom,
                IsBuiltIn = true,
                Description = "Result of a string value test",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition { Name = "Value", TypeName = "String", IsRequired = true },
                    new PropertyDefinition { Name = "ExpectedValue", TypeName = "String" },
                    new PropertyDefinition { Name = "ComparisonType", TypeName = "String" },
                    new PropertyDefinition { Name = "Status", TypeName = "String" }
                }
            });

            // Waveform type
            RegisterType(new TypeDefinition
            {
                Name = "Waveform",
                Category = TypeCategory.Custom,
                IsBuiltIn = true,
                Description = "Waveform data",
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition { Name = "Data", TypeName = "Array" },
                    new PropertyDefinition { Name = "SampleRate", TypeName = "Number" },
                    new PropertyDefinition { Name = "StartTime", TypeName = "Number" },
                    new PropertyDefinition { Name = "Units", TypeName = "String" }
                }
            });
        }

        /// <summary>
        /// Get all registered types
        /// </summary>
        public IEnumerable<TypeDefinition> Types
        {
            get
            {
                lock (_lock)
                {
                    return _types.Values.ToList();
                }
            }
        }

        /// <summary>
        /// Get types by category
        /// </summary>
        public IEnumerable<TypeDefinition> GetTypesByCategory(TypeCategory category)
        {
            lock (_lock)
            {
                return _types.Values.Where(t => t.Category == category).ToList();
            }
        }

        /// <summary>
        /// Get custom (non-built-in) types
        /// </summary>
        public IEnumerable<TypeDefinition> GetCustomTypes()
        {
            lock (_lock)
            {
                return _types.Values.Where(t => !t.IsBuiltIn).ToList();
            }
        }

        /// <summary>
        /// Register a type
        /// </summary>
        public void RegisterType(TypeDefinition type)
        {
            lock (_lock)
            {
                _types[type.Name] = type;
            }

            TypeAdded?.Invoke(this, new TypeEventArgs { Type = type });
        }

        /// <summary>
        /// Unregister a type
        /// </summary>
        public bool UnregisterType(string typeName)
        {
            TypeDefinition? type;
            lock (_lock)
            {
                if (!_types.TryGetValue(typeName, out type))
                {
                    return false;
                }

                if (type.IsBuiltIn)
                {
                    throw new InvalidOperationException("Cannot unregister built-in types");
                }

                _types.Remove(typeName);
            }

            TypeRemoved?.Invoke(this, new TypeEventArgs { Type = type });
            return true;
        }

        /// <summary>
        /// Get a type by name
        /// </summary>
        public TypeDefinition? GetType(string typeName)
        {
            lock (_lock)
            {
                return _types.TryGetValue(typeName, out var type) ? type : null;
            }
        }

        /// <summary>
        /// Check if a type exists
        /// </summary>
        public bool TypeExists(string typeName)
        {
            lock (_lock)
            {
                return _types.ContainsKey(typeName);
            }
        }

        /// <summary>
        /// Update a type definition
        /// </summary>
        public bool UpdateType(TypeDefinition type)
        {
            lock (_lock)
            {
                if (!_types.ContainsKey(type.Name))
                {
                    return false;
                }

                var existing = _types[type.Name];
                if (existing.IsBuiltIn)
                {
                    throw new InvalidOperationException("Cannot modify built-in types");
                }

                type.ModifiedAt = DateTime.UtcNow;
                _types[type.Name] = type;
            }

            TypeModified?.Invoke(this, new TypeEventArgs { Type = type });
            return true;
        }

        /// <summary>
        /// Create an instance of a type
        /// </summary>
        public TypeInstance CreateInstance(string typeName)
        {
            var type = GetType(typeName);
            if (type == null)
            {
                throw new ArgumentException($"Type '{typeName}' not found");
            }

            var instance = new TypeInstance
            {
                TypeId = type.Id,
                TypeName = typeName
            };

            // Initialize with default values
            foreach (var prop in type.Properties)
            {
                instance.Values[prop.Name] = prop.DefaultValue;
            }

            foreach (var kvp in type.DefaultValues)
            {
                instance.Values[kvp.Key] = kvp.Value;
            }

            return instance;
        }

        /// <summary>
        /// Validate an instance against its type
        /// </summary>
        public List<string> ValidateInstance(TypeInstance instance)
        {
            var errors = new List<string>();

            var type = GetType(instance.TypeName);
            if (type == null)
            {
                errors.Add($"Type '{instance.TypeName}' not found");
                return errors;
            }

            foreach (var prop in type.Properties)
            {
                if (prop.IsRequired && !instance.Values.ContainsKey(prop.Name))
                {
                    errors.Add($"Required property '{prop.Name}' is missing");
                }

                if (instance.Values.TryGetValue(prop.Name, out var value) && value != null)
                {
                    // Check min/max for numeric types
                    if (prop.TypeName == "Number" || prop.TypeName == "Integer")
                    {
                        if (prop.MinValue != null && value is IComparable comparable)
                        {
                            if (comparable.CompareTo(Convert.ChangeType(prop.MinValue, value.GetType())) < 0)
                            {
                                errors.Add($"Property '{prop.Name}' is below minimum value {prop.MinValue}");
                            }
                        }

                        if (prop.MaxValue != null && value is IComparable comparable2)
                        {
                            if (comparable2.CompareTo(Convert.ChangeType(prop.MaxValue, value.GetType())) > 0)
                            {
                                errors.Add($"Property '{prop.Name}' is above maximum value {prop.MaxValue}");
                            }
                        }
                    }

                    // Check allowed values
                    if (prop.AllowedValues != null && prop.AllowedValues.Count > 0)
                    {
                        if (!prop.AllowedValues.Contains(value))
                        {
                            errors.Add($"Property '{prop.Name}' value is not in allowed values");
                        }
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Save a type to file
        /// </summary>
        public void SaveType(string typeName, string? filePath = null)
        {
            var type = GetType(typeName);
            if (type == null)
            {
                throw new ArgumentException($"Type '{typeName}' not found");
            }

            filePath ??= Path.Combine(_storageDirectory, $"{typeName}.json");
            var json = JsonSerializer.Serialize(type, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Load a type from file
        /// </summary>
        public TypeDefinition? LoadType(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = File.ReadAllText(filePath);
            var type = JsonSerializer.Deserialize<TypeDefinition>(json);
            
            if (type != null)
            {
                type.IsBuiltIn = false; // Loaded types are never built-in
                RegisterType(type);
            }

            return type;
        }

        /// <summary>
        /// Load all types from directory
        /// </summary>
        public int LoadTypesFromDirectory(string? directory = null)
        {
            directory ??= _storageDirectory;
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            var loadedCount = 0;
            var files = Directory.GetFiles(directory, "*.json");

            foreach (var file in files)
            {
                try
                {
                    if (LoadType(file) != null)
                    {
                        loadedCount++;
                    }
                }
                catch
                {
                    // Skip invalid files
                }
            }

            return loadedCount;
        }

        /// <summary>
        /// Export all custom types
        /// </summary>
        public void ExportCustomTypes(string directory)
        {
            Directory.CreateDirectory(directory);

            foreach (var type in GetCustomTypes())
            {
                var filePath = Path.Combine(directory, $"{type.Name}.json");
                SaveType(type.Name, filePath);
            }
        }

        /// <summary>
        /// Clone a type with a new name
        /// </summary>
        public TypeDefinition CloneType(string sourceName, string newName)
        {
            var source = GetType(sourceName);
            if (source == null)
            {
                throw new ArgumentException($"Type '{sourceName}' not found");
            }

            var clone = new TypeDefinition
            {
                Name = newName,
                Description = $"Clone of {source.Name}",
                Category = source.Category,
                BaseType = source.Name,
                IsBuiltIn = false,
                Properties = source.Properties.Select(p => new PropertyDefinition
                {
                    Name = p.Name,
                    Description = p.Description,
                    TypeName = p.TypeName,
                    IsRequired = p.IsRequired,
                    IsReadOnly = p.IsReadOnly,
                    DefaultValue = p.DefaultValue,
                    MinValue = p.MinValue,
                    MaxValue = p.MaxValue,
                    AllowedValues = p.AllowedValues?.ToList(),
                    ValidationExpression = p.ValidationExpression,
                    DisplayOrder = p.DisplayOrder,
                    DisplayGroup = p.DisplayGroup
                }).ToList(),
                DefaultValues = new Dictionary<string, object>(source.DefaultValues)
            };

            RegisterType(clone);
            return clone;
        }
    }

    /// <summary>
    /// Type event arguments
    /// </summary>
    public class TypeEventArgs : EventArgs
    {
        public TypeDefinition Type { get; set; } = new();
    }
}
