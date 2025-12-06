using System.Reflection;

namespace TestStandClone.Core.CodeModules
{
    /// <summary>
    /// Interface for code modules that can be loaded and executed.
    /// </summary>
    public interface ICodeModule
    {
        /// <summary>
        /// Executes the code module.
        /// </summary>
        /// <param name="context">Execution context.</param>
        /// <returns>True if passed, false if failed.</returns>
        Task<bool> ExecuteAsync(Context context);
    }

    /// <summary>
    /// Result from executing a code module.
    /// </summary>
    public class CodeModuleResult
    {
        /// <summary>
        /// Whether the module execution passed.
        /// </summary>
        public bool Passed { get; set; }

        /// <summary>
        /// Result message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Measured numeric value (if applicable).
        /// </summary>
        public double? NumericValue { get; set; }

        /// <summary>
        /// String value (if applicable).
        /// </summary>
        public string? StringValue { get; set; }

        /// <summary>
        /// Error that occurred during execution.
        /// </summary>
        public Exception? Error { get; set; }

        /// <summary>
        /// Additional output data.
        /// </summary>
        public Dictionary<string, object> OutputData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Loader for .NET code modules (DLLs).
    /// Similar to TestStand's code module adapter for .NET.
    /// </summary>
    public class CodeModuleLoader : IDisposable
    {
        private readonly Dictionary<string, Assembly> _loadedAssemblies;
        private readonly Dictionary<string, Type> _cachedTypes;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Creates a new CodeModuleLoader.
        /// </summary>
        public CodeModuleLoader()
        {
            _loadedAssemblies = new Dictionary<string, Assembly>();
            _cachedTypes = new Dictionary<string, Type>();
        }

        /// <summary>
        /// Loads an assembly from the specified path.
        /// </summary>
        /// <param name="assemblyPath">Path to the assembly DLL.</param>
        /// <returns>The loaded assembly.</returns>
        public Assembly LoadAssembly(string assemblyPath)
        {
            if (string.IsNullOrEmpty(assemblyPath))
            {
                throw new ArgumentNullException(nameof(assemblyPath));
            }

            var fullPath = Path.GetFullPath(assemblyPath);
            
            lock (_lockObject)
            {
                if (_loadedAssemblies.TryGetValue(fullPath, out var existing))
                {
                    return existing;
                }

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"Assembly not found: {fullPath}");
                }

                var assembly = Assembly.LoadFrom(fullPath);
                _loadedAssemblies[fullPath] = assembly;
                return assembly;
            }
        }

        /// <summary>
        /// Gets a type from a loaded assembly.
        /// </summary>
        /// <param name="assemblyPath">Path to the assembly.</param>
        /// <param name="typeName">Full name of the type.</param>
        /// <returns>The type.</returns>
        public Type GetType(string assemblyPath, string typeName)
        {
            var cacheKey = $"{assemblyPath}|{typeName}";
            
            lock (_lockObject)
            {
                if (_cachedTypes.TryGetValue(cacheKey, out var cachedType))
                {
                    return cachedType;
                }

                var assembly = LoadAssembly(assemblyPath);
                var type = assembly.GetType(typeName) 
                    ?? throw new TypeLoadException($"Type '{typeName}' not found in assembly '{assemblyPath}'");

                _cachedTypes[cacheKey] = type;
                return type;
            }
        }

        /// <summary>
        /// Creates an instance of a type from an assembly.
        /// </summary>
        /// <param name="assemblyPath">Path to the assembly.</param>
        /// <param name="typeName">Full name of the type.</param>
        /// <param name="args">Constructor arguments.</param>
        /// <returns>The created instance.</returns>
        public object CreateInstance(string assemblyPath, string typeName, params object[] args)
        {
            var type = GetType(assemblyPath, typeName);
            var instance = Activator.CreateInstance(type, args)
                ?? throw new InvalidOperationException($"Failed to create instance of type '{typeName}'");
            return instance;
        }

        /// <summary>
        /// Invokes a static method on a type.
        /// </summary>
        /// <param name="assemblyPath">Path to the assembly.</param>
        /// <param name="typeName">Full name of the type.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="args">Method arguments.</param>
        /// <returns>The method result.</returns>
        public object? InvokeStaticMethod(string assemblyPath, string typeName, string methodName, params object[] args)
        {
            var type = GetType(assemblyPath, typeName);
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                ?? throw new MissingMethodException($"Static method '{methodName}' not found on type '{typeName}'");

            return method.Invoke(null, args);
        }

        /// <summary>
        /// Invokes an instance method.
        /// </summary>
        /// <param name="instance">The object instance.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="args">Method arguments.</param>
        /// <returns>The method result.</returns>
        public object? InvokeInstanceMethod(object instance, string methodName, params object[] args)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var type = instance.GetType();
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException($"Instance method '{methodName}' not found on type '{type.Name}'");

            return method.Invoke(instance, args);
        }

        /// <summary>
        /// Invokes an async method on an instance.
        /// </summary>
        public async Task<object?> InvokeAsyncMethod(object instance, string methodName, params object[] args)
        {
            var result = InvokeInstanceMethod(instance, methodName, args);
            
            if (result is Task task)
            {
                await task;
                
                // Check if it's a Task<T>
                var taskType = task.GetType();
                if (taskType.IsGenericType)
                {
                    var resultProperty = taskType.GetProperty("Result");
                    return resultProperty?.GetValue(task);
                }
                
                return null;
            }
            
            return result;
        }

        /// <summary>
        /// Executes a code module implementing ICodeModule.
        /// </summary>
        /// <param name="assemblyPath">Path to the assembly.</param>
        /// <param name="typeName">Full name of the ICodeModule type.</param>
        /// <param name="context">Execution context.</param>
        /// <returns>The code module result.</returns>
        public async Task<CodeModuleResult> ExecuteCodeModuleAsync(string assemblyPath, string typeName, Context context)
        {
            var result = new CodeModuleResult();

            try
            {
                var instance = CreateInstance(assemblyPath, typeName);
                
                if (instance is ICodeModule codeModule)
                {
                    result.Passed = await codeModule.ExecuteAsync(context);
                    result.Message = result.Passed ? "Passed" : "Failed";
                }
                else
                {
                    throw new InvalidOperationException($"Type '{typeName}' does not implement ICodeModule");
                }
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Error = ex;
                result.Message = $"Error: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Gets all ICodeModule types from an assembly.
        /// </summary>
        /// <param name="assemblyPath">Path to the assembly.</param>
        /// <returns>List of type names implementing ICodeModule.</returns>
        public IReadOnlyList<string> GetCodeModuleTypes(string assemblyPath)
        {
            var assembly = LoadAssembly(assemblyPath);
            var codeModuleInterface = typeof(ICodeModule);

            return assembly.GetTypes()
                .Where(t => codeModuleInterface.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(t => t.FullName ?? t.Name)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            lock (_lockObject)
            {
                _loadedAssemblies.Clear();
                _cachedTypes.Clear();
            }
            GC.SuppressFinalize(this);
        }
    }
}
