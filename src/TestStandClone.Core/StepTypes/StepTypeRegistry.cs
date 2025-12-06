using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace TestStandClone.Core.StepTypes
{
    /// <summary>
    /// Attribute to mark a class as a custom step type
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class StepTypeAttribute : Attribute
    {
        public string Name { get; }
        public string Category { get; }
        public string Description { get; }
        public string IconPath { get; }

        public StepTypeAttribute(string name, string category = "Custom", string description = "", string iconPath = "")
        {
            Name = name;
            Category = category;
            Description = description;
            IconPath = iconPath;
        }
    }

    /// <summary>
    /// Attribute to mark a property as editable in the step properties panel
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class StepPropertyAttribute : Attribute
    {
        public string DisplayName { get; }
        public string Category { get; }
        public string Description { get; }
        public bool Required { get; }
        public int Order { get; }

        public StepPropertyAttribute(string displayName = "", string category = "General", string description = "", bool required = false, int order = 0)
        {
            DisplayName = displayName;
            Category = category;
            Description = description;
            Required = required;
            Order = order;
        }
    }

    /// <summary>
    /// Information about a step property for UI editing
    /// </summary>
    public class StepPropertyInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string Description { get; set; } = string.Empty;
        public Type PropertyType { get; set; } = typeof(string);
        public bool Required { get; set; }
        public int Order { get; set; }
        public PropertyInfo? PropertyInfo { get; set; }
    }

    /// <summary>
    /// Information about a registered step type
    /// </summary>
    public class StepTypeInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Custom";
        public string Description { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public Type StepType { get; set; } = typeof(TestStep);
        public List<StepPropertyInfo> Properties { get; set; } = new();
    }

    /// <summary>
    /// Registry for custom step types
    /// Similar to TestStand Step Type Palette
    /// </summary>
    public class StepTypeRegistry
    {
        private static readonly Lazy<StepTypeRegistry> _instance = new(() => new StepTypeRegistry());
        public static StepTypeRegistry Instance => _instance.Value;

        private readonly Dictionary<string, StepTypeInfo> _stepTypes = new();
        private readonly Dictionary<string, Func<TestStep>> _stepFactories = new();

        private StepTypeRegistry()
        {
            // Register built-in step types
            RegisterBuiltInTypes();
        }

        private void RegisterBuiltInTypes()
        {
            // Register the built-in step types
            RegisterStepType<DelayStep>("Delay", "Flow Control", "Wait for specified duration");
            RegisterStepType<NumericLimitStep>("Numeric Limit", "Measurement", "Compare a numeric value against limits");
            RegisterStepType<Steps.PassFailStep>("Pass/Fail", "Measurement", "Boolean pass/fail test");
            RegisterStepType<Steps.StringValueStep>("String Value", "Measurement", "String comparison test");
            RegisterStepType<Steps.ActionStep>("Action", "Flow Control", "Execute an action without pass/fail");
            RegisterStepType<Steps.MessagePopupStep>("Message Popup", "User Interface", "Display a message to the operator");
            RegisterStepType<Steps.LabelStep>("Label", "Flow Control", "Flow control label marker");
            RegisterStepType<Steps.GotoStep>("Goto", "Flow Control", "Jump to a label");
            RegisterStepType<Steps.LoopStep>("Loop", "Flow Control", "Loop iteration control");
            RegisterStepType<Steps.SequenceCallStep>("Sequence Call", "Flow Control", "Call a sub-sequence");
            RegisterStepType<Steps.CodeModuleStep>("Code Module", "Code", "Execute code from a DLL module");
            RegisterStepType<Steps.InstrumentStep>("Instrument Write", "Instrument I/O", "Send command to instrument");
            RegisterStepType<Steps.InstrumentMeasureStep>("Instrument Measure", "Instrument I/O", "Read measurement from instrument");
            RegisterStepType<Steps.InstrumentIdentifyStep>("Instrument Identify", "Instrument I/O", "Query instrument identification");
        }

        /// <summary>
        /// Register a step type with its factory
        /// </summary>
        public void RegisterStepType<T>(string name, string category = "Custom", string description = "") where T : TestStep, new()
        {
            var stepType = typeof(T);
            var info = new StepTypeInfo
            {
                Name = name,
                Category = category,
                Description = description,
                StepType = stepType
            };

            // Check for StepType attribute
            var attr = stepType.GetCustomAttribute<StepTypeAttribute>();
            if (attr != null)
            {
                info.Name = attr.Name;
                info.Category = attr.Category;
                info.Description = attr.Description;
                info.IconPath = attr.IconPath;
            }

            // Collect property information
            foreach (var prop in stepType.GetProperties())
            {
                var propAttr = prop.GetCustomAttribute<StepPropertyAttribute>();
                if (propAttr != null)
                {
                    info.Properties.Add(new StepPropertyInfo
                    {
                        Name = prop.Name,
                        DisplayName = string.IsNullOrEmpty(propAttr.DisplayName) ? prop.Name : propAttr.DisplayName,
                        Category = propAttr.Category,
                        Description = propAttr.Description,
                        PropertyType = prop.PropertyType,
                        Required = propAttr.Required,
                        Order = propAttr.Order,
                        PropertyInfo = prop
                    });
                }
            }

            // Sort properties by order
            info.Properties.Sort((a, b) => a.Order.CompareTo(b.Order));

            _stepTypes[name] = info;
            _stepFactories[name] = () => new T();
        }

        /// <summary>
        /// Register a step type with a custom factory
        /// </summary>
        public void RegisterStepType(string name, Type stepType, Func<TestStep> factory, string category = "Custom", string description = "")
        {
            var info = new StepTypeInfo
            {
                Name = name,
                Category = category,
                Description = description,
                StepType = stepType
            };

            _stepTypes[name] = info;
            _stepFactories[name] = factory;
        }

        /// <summary>
        /// Unregister a step type
        /// </summary>
        public bool UnregisterStepType(string name)
        {
            _stepFactories.Remove(name);
            return _stepTypes.Remove(name);
        }

        /// <summary>
        /// Create a new instance of a step type
        /// </summary>
        public TestStep? CreateStep(string typeName)
        {
            if (_stepFactories.TryGetValue(typeName, out var factory))
            {
                return factory();
            }
            return null;
        }

        /// <summary>
        /// Get information about a step type
        /// </summary>
        public StepTypeInfo? GetStepTypeInfo(string typeName)
        {
            _stepTypes.TryGetValue(typeName, out var info);
            return info;
        }

        /// <summary>
        /// Get all registered step types
        /// </summary>
        public IEnumerable<StepTypeInfo> GetAllStepTypes()
        {
            return _stepTypes.Values;
        }

        /// <summary>
        /// Get step types by category
        /// </summary>
        public IEnumerable<StepTypeInfo> GetStepTypesByCategory(string category)
        {
            foreach (var info in _stepTypes.Values)
            {
                if (info.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                {
                    yield return info;
                }
            }
        }

        /// <summary>
        /// Get all categories
        /// </summary>
        public IEnumerable<string> GetCategories()
        {
            var categories = new HashSet<string>();
            foreach (var info in _stepTypes.Values)
            {
                categories.Add(info.Category);
            }
            return categories;
        }

        /// <summary>
        /// Check if a step type is registered
        /// </summary>
        public bool IsRegistered(string typeName)
        {
            return _stepTypes.ContainsKey(typeName);
        }

        /// <summary>
        /// Load step types from an assembly
        /// </summary>
        public void LoadFromAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(TestStep).IsAssignableFrom(type)) continue;

                var attr = type.GetCustomAttribute<StepTypeAttribute>();
                if (attr != null)
                {
                    var constructor = type.GetConstructor(Type.EmptyTypes);
                    if (constructor != null)
                    {
                        _stepTypes[attr.Name] = new StepTypeInfo
                        {
                            Name = attr.Name,
                            Category = attr.Category,
                            Description = attr.Description,
                            IconPath = attr.IconPath,
                            StepType = type
                        };
                        _stepFactories[attr.Name] = () => (TestStep)Activator.CreateInstance(type)!;
                    }
                }
            }
        }

        /// <summary>
        /// Load step types from a DLL file
        /// </summary>
        public void LoadFromFile(string dllPath)
        {
            var assembly = Assembly.LoadFrom(dllPath);
            LoadFromAssembly(assembly);
        }
    }

    /// <summary>
    /// Interface for steps that support validation
    /// </summary>
    public interface IValidatableStep
    {
        /// <summary>
        /// Validate the step configuration
        /// </summary>
        ValidationResult Validate();
    }

    /// <summary>
    /// Result of step validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public void AddError(string message)
        {
            Errors.Add(message);
            IsValid = false;
        }

        public void AddWarning(string message)
        {
            Warnings.Add(message);
        }
    }

    /// <summary>
    /// Interface for steps that support async initialization
    /// </summary>
    public interface IInitializableStep
    {
        /// <summary>
        /// Initialize the step before execution
        /// </summary>
        Task InitializeAsync();
        
        /// <summary>
        /// Cleanup after execution
        /// </summary>
        Task CleanupAsync();
    }

    /// <summary>
    /// Interface for steps that support result customization
    /// </summary>
    public interface ICustomResultStep
    {
        /// <summary>
        /// Get custom result data
        /// </summary>
        Dictionary<string, object> GetCustomResults();
    }
}
