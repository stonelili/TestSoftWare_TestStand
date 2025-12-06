using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestStandClone.Core.Scripting
{
    /// <summary>
    /// Type of script language.
    /// </summary>
    public enum ScriptLanguage
    {
        Expression,
        CSharp,
        Python,
        JavaScript,
        Custom
    }

    /// <summary>
    /// Represents a script to be executed.
    /// </summary>
    public class Script
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public ScriptLanguage Language { get; set; } = ScriptLanguage.Expression;
        public string Code { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Result of script execution.
    /// </summary>
    public class ScriptResult
    {
        public bool Success { get; set; }
        public object? ReturnValue { get; set; }
        public string? Output { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public Dictionary<string, object> OutputVariables { get; set; } = new();
    }

    /// <summary>
    /// Context for script execution.
    /// </summary>
    public class ScriptContext
    {
        public Dictionary<string, object> Variables { get; set; } = new();
        public Dictionary<string, Func<object[], object>> Functions { get; set; } = new();
        public bool EnableDebug { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Interface for script executors.
    /// </summary>
    public interface IScriptExecutor
    {
        ScriptLanguage Language { get; }
        Task<ScriptResult> ExecuteAsync(Script script, ScriptContext context);
        bool Validate(string code, out List<string> errors);
    }

    /// <summary>
    /// Simple expression executor.
    /// </summary>
    public class ExpressionExecutor : IScriptExecutor
    {
        public ScriptLanguage Language => ScriptLanguage.Expression;

        public Task<ScriptResult> ExecuteAsync(Script script, ScriptContext context)
        {
            var startTime = DateTime.UtcNow;
            var result = new ScriptResult { Success = true };

            try
            {
                // Simple expression evaluation (placeholder)
                var code = script.Code.Trim();
                
                // Handle simple operations
                if (double.TryParse(code, out var numValue))
                {
                    result.ReturnValue = numValue;
                }
                else if (code.StartsWith("\"") && code.EndsWith("\""))
                {
                    result.ReturnValue = code.Trim('"');
                }
                else if (bool.TryParse(code, out var boolValue))
                {
                    result.ReturnValue = boolValue;
                }
                else
                {
                    // Try to evaluate as variable reference
                    if (context.Variables.TryGetValue(code, out var varValue))
                    {
                        result.ReturnValue = varValue;
                    }
                    else
                    {
                        result.ReturnValue = code;
                    }
                }

                result.ExecutionTime = DateTime.UtcNow - startTime;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return Task.FromResult(result);
        }

        public bool Validate(string code, out List<string> errors)
        {
            errors = new List<string>();
            if (string.IsNullOrWhiteSpace(code))
            {
                errors.Add("Expression cannot be empty");
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Manager for script execution.
    /// </summary>
    public class ScriptingManager
    {
        private static ScriptingManager? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<ScriptLanguage, IScriptExecutor> _executors = new();
        private readonly Dictionary<string, Script> _scripts = new();
        private readonly List<ScriptExecutionLog> _executionLogs = new();

        public static ScriptingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ScriptingManager();
                    }
                }
                return _instance;
            }
        }

        private ScriptingManager()
        {
            RegisterExecutor(new ExpressionExecutor());
        }

        /// <summary>
        /// Registers a script executor.
        /// </summary>
        public void RegisterExecutor(IScriptExecutor executor)
        {
            _executors[executor.Language] = executor;
        }

        /// <summary>
        /// Gets available script languages.
        /// </summary>
        public IEnumerable<ScriptLanguage> GetAvailableLanguages()
        {
            return _executors.Keys;
        }

        /// <summary>
        /// Saves a script.
        /// </summary>
        public void SaveScript(Script script)
        {
            script.ModifiedAt = DateTime.UtcNow;
            _scripts[script.Id] = script;
        }

        /// <summary>
        /// Gets a script by ID.
        /// </summary>
        public Script? GetScript(string scriptId)
        {
            return _scripts.TryGetValue(scriptId, out var script) ? script : null;
        }

        /// <summary>
        /// Gets all scripts.
        /// </summary>
        public IEnumerable<Script> GetAllScripts()
        {
            return _scripts.Values;
        }

        /// <summary>
        /// Deletes a script.
        /// </summary>
        public bool DeleteScript(string scriptId)
        {
            return _scripts.Remove(scriptId);
        }

        /// <summary>
        /// Executes a script.
        /// </summary>
        public async Task<ScriptResult> ExecuteScriptAsync(string scriptId, ScriptContext? context = null)
        {
            if (!_scripts.TryGetValue(scriptId, out var script))
            {
                return new ScriptResult
                {
                    Success = false,
                    ErrorMessage = "Script not found"
                };
            }

            return await ExecuteAsync(script, context ?? new ScriptContext());
        }

        /// <summary>
        /// Executes a script directly.
        /// </summary>
        public async Task<ScriptResult> ExecuteAsync(Script script, ScriptContext context)
        {
            if (!_executors.TryGetValue(script.Language, out var executor))
            {
                return new ScriptResult
                {
                    Success = false,
                    ErrorMessage = $"No executor found for language: {script.Language}"
                };
            }

            var result = await executor.ExecuteAsync(script, context);

            // Log execution
            _executionLogs.Add(new ScriptExecutionLog
            {
                ScriptId = script.Id,
                ScriptName = script.Name,
                Language = script.Language,
                Success = result.Success,
                ExecutionTime = result.ExecutionTime,
                ErrorMessage = result.ErrorMessage,
                ExecutedAt = DateTime.UtcNow
            });

            return result;
        }

        /// <summary>
        /// Validates a script.
        /// </summary>
        public bool ValidateScript(Script script, out List<string> errors)
        {
            if (!_executors.TryGetValue(script.Language, out var executor))
            {
                errors = new List<string> { $"No executor found for language: {script.Language}" };
                return false;
            }

            return executor.Validate(script.Code, out errors);
        }

        /// <summary>
        /// Gets execution logs.
        /// </summary>
        public IEnumerable<ScriptExecutionLog> GetExecutionLogs(string? scriptId = null)
        {
            return scriptId == null
                ? _executionLogs
                : _executionLogs.Where(l => l.ScriptId == scriptId);
        }
    }

    /// <summary>
    /// Log entry for script execution.
    /// </summary>
    public class ScriptExecutionLog
    {
        public string ScriptId { get; set; } = string.Empty;
        public string ScriptName { get; set; } = string.Empty;
        public ScriptLanguage Language { get; set; }
        public bool Success { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ExecutedAt { get; set; }
    }
}
