using TestStandClone.Core.CodeModules;

namespace TestStandClone.Core.Steps
{
    /// <summary>
    /// A step that executes a .NET code module (DLL).
    /// Similar to TestStand's .NET Adapter step type.
    /// </summary>
    public class CodeModuleStep : TestStep
    {
        private readonly CodeModuleLoader _loader;

        /// <summary>
        /// Path to the code module assembly.
        /// </summary>
        public string AssemblyPath { get; set; } = string.Empty;

        /// <summary>
        /// Full type name of the code module class.
        /// </summary>
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// Method name to invoke (if not using ICodeModule interface).
        /// </summary>
        public string MethodName { get; set; } = string.Empty;

        /// <summary>
        /// Whether to use ICodeModule interface or direct method invocation.
        /// </summary>
        public bool UseCodeModuleInterface { get; set; } = true;

        /// <summary>
        /// Parameters to pass to the method.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Last execution result.
        /// </summary>
        public CodeModuleResult? LastResult { get; private set; }

        /// <summary>
        /// Creates a new CodeModuleStep with default values.
        /// </summary>
        public CodeModuleStep()
        {
            _loader = new CodeModuleLoader();
        }

        /// <summary>
        /// Creates a new CodeModuleStep.
        /// </summary>
        public CodeModuleStep(string name, string assemblyPath, string typeName)
        {
            Name = name;
            AssemblyPath = assemblyPath;
            TypeName = typeName;
            _loader = new CodeModuleLoader();
        }

        /// <summary>
        /// Creates a new CodeModuleStep with method invocation.
        /// </summary>
        public CodeModuleStep(string name, string assemblyPath, string typeName, string methodName) : this(name, assemblyPath, typeName)
        {
            MethodName = methodName;
            UseCodeModuleInterface = false;
        }

        /// <inheritdoc/>
        public override async Task ExecuteAsync(Context context)
        {
            try
            {
                if (UseCodeModuleInterface)
                {
                    LastResult = await _loader.ExecuteCodeModuleAsync(AssemblyPath, TypeName, context);
                    Status = LastResult.Passed ? StepStatus.Passed : StepStatus.Failed;
                    ResultText = LastResult.Message;
                }
                else
                {
                    await ExecuteMethodAsync(context);
                }
            }
            catch (Exception ex)
            {
                Status = StepStatus.Error;
                ResultText = $"Error: {ex.Message}";
                LastResult = new CodeModuleResult
                {
                    Passed = false,
                    Error = ex,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Executes a specific method on the type.
        /// </summary>
        private async Task ExecuteMethodAsync(Context context)
        {
            var instance = _loader.CreateInstance(AssemblyPath, TypeName);
            
            // Build method arguments from parameters and context
            var args = new List<object>();
            foreach (var param in Parameters)
            {
                if (param.Value is string strValue && strValue.StartsWith("$"))
                {
                    // Variable reference
                    var varName = strValue.Substring(1);
                    var varValue = context.GetValue<object>(varName);
                    args.Add(varValue ?? param.Value);
                }
                else
                {
                    args.Add(param.Value);
                }
            }

            // Add context as last argument if method accepts it
            try
            {
                var result = await _loader.InvokeAsyncMethod(instance, MethodName, args.ToArray());
                
                if (result is bool boolResult)
                {
                    Status = boolResult ? StepStatus.Passed : StepStatus.Failed;
                    ResultText = boolResult ? "Passed" : "Failed";
                }
                else if (result is CodeModuleResult moduleResult)
                {
                    Status = moduleResult.Passed ? StepStatus.Passed : StepStatus.Failed;
                    ResultText = moduleResult.Message;
                    LastResult = moduleResult;
                }
                else
                {
                    Status = StepStatus.Passed;
                    ResultText = result?.ToString() ?? "Completed";
                    
                    // Store result in context
                    if (result != null)
                    {
                        context.SetValue($"{Name}.Result", result);
                    }
                }
            }
            catch (Exception ex)
            {
                Status = StepStatus.Error;
                ResultText = $"Error: {ex.Message}";
            }
        }
    }
}
