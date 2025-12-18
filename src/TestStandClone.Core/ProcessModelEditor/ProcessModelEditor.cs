using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace TestStandClone.Core.ProcessModelEditor
{
    /// <summary>
    /// Represents a step in a process model flow
    /// </summary>
    public class ProcessModelStep
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public ProcessModelStepType StepType { get; set; } = ProcessModelStepType.Action;
        public string Description { get; set; } = string.Empty;
        public string ActionExpression { get; set; } = string.Empty;
        public string ConditionExpression { get; set; } = string.Empty;
        public string NextStepOnTrue { get; set; } = string.Empty;
        public string NextStepOnFalse { get; set; } = string.Empty;
        public string NextStep { get; set; } = string.Empty;
        public bool IsEntryPoint { get; set; }
        public bool IsExitPoint { get; set; }
        public Dictionary<string, object?> Properties { get; set; } = new();
        public int PositionX { get; set; }
        public int PositionY { get; set; }
    }

    /// <summary>
    /// Types of process model steps
    /// </summary>
    public enum ProcessModelStepType
    {
        Entry,
        Exit,
        Action,
        Decision,
        Callback,
        SequenceCall,
        Loop,
        Parallel,
        Wait,
        ErrorHandler
    }

    /// <summary>
    /// Represents a connection between two process model steps
    /// </summary>
    public class ProcessModelConnection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SourceStepId { get; set; } = string.Empty;
        public string TargetStepId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public ConnectionType Type { get; set; } = ConnectionType.Normal;
    }

    /// <summary>
    /// Types of connections
    /// </summary>
    public enum ConnectionType
    {
        Normal,
        TrueBranch,
        FalseBranch,
        Error,
        Loop
    }

    /// <summary>
    /// Represents a complete process model definition
    /// </summary>
    public class ProcessModelDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public ProcessModelType ModelType { get; set; } = ProcessModelType.Sequential;
        public List<ProcessModelStep> Steps { get; set; } = new();
        public List<ProcessModelConnection> Connections { get; set; } = new();
        public Dictionary<string, object?> Properties { get; set; } = new();
        public List<ProcessModelVariable> Variables { get; set; } = new();
    }

    /// <summary>
    /// Types of process models
    /// </summary>
    public enum ProcessModelType
    {
        Sequential,
        Parallel,
        Batch,
        Custom
    }

    /// <summary>
    /// Variable definition for process model
    /// </summary>
    public class ProcessModelVariable
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "String";
        public object? DefaultValue { get; set; }
        public string Description { get; set; } = string.Empty;
        public VariableScope Scope { get; set; } = VariableScope.Local;
    }

    /// <summary>
    /// Variable scope
    /// </summary>
    public enum VariableScope
    {
        Local,
        Model,
        Station
    }

    /// <summary>
    /// Result of process model validation
    /// </summary>
    public class ProcessModelValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<ProcessModelValidationError> Errors { get; set; } = new();
        public List<ProcessModelValidationWarning> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Validation error
    /// </summary>
    public class ProcessModelValidationError
    {
        public string StepId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validation warning
    /// </summary>
    public class ProcessModelValidationWarning
    {
        public string StepId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string WarningCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Editor for creating and modifying process models
    /// </summary>
    public class ProcessModelEditor
    {
        private ProcessModelDefinition _currentModel;
        private readonly Stack<ProcessModelDefinition> _undoStack = new();
        private readonly Stack<ProcessModelDefinition> _redoStack = new();

        public ProcessModelEditor()
        {
            _currentModel = new ProcessModelDefinition();
        }

        public ProcessModelDefinition CurrentModel => _currentModel;

        /// <summary>
        /// Creates a new empty process model
        /// </summary>
        public void NewModel(string name, ProcessModelType modelType = ProcessModelType.Sequential)
        {
            SaveUndo();
            _currentModel = new ProcessModelDefinition
            {
                Name = name,
                ModelType = modelType
            };
            _redoStack.Clear();
        }

        /// <summary>
        /// Loads a process model from JSON
        /// </summary>
        public void LoadFromJson(string json)
        {
            var model = JsonSerializer.Deserialize<ProcessModelDefinition>(json);
            if (model != null)
            {
                SaveUndo();
                _currentModel = model;
                _redoStack.Clear();
            }
        }

        /// <summary>
        /// Saves the process model to JSON
        /// </summary>
        public string SaveToJson()
        {
            _currentModel.ModifiedDate = DateTime.UtcNow;
            return JsonSerializer.Serialize(_currentModel, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Adds a step to the process model
        /// </summary>
        public ProcessModelStep AddStep(string name, ProcessModelStepType stepType)
        {
            SaveUndo();
            var step = new ProcessModelStep
            {
                Name = name,
                StepType = stepType
            };
            _currentModel.Steps.Add(step);
            _redoStack.Clear();
            return step;
        }

        /// <summary>
        /// Removes a step from the process model
        /// </summary>
        public bool RemoveStep(string stepId)
        {
            var step = _currentModel.Steps.FirstOrDefault(s => s.Id == stepId);
            if (step != null)
            {
                SaveUndo();
                _currentModel.Steps.Remove(step);
                _currentModel.Connections.RemoveAll(c => c.SourceStepId == stepId || c.TargetStepId == stepId);
                _redoStack.Clear();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Updates a step in the process model
        /// </summary>
        public bool UpdateStep(ProcessModelStep updatedStep)
        {
            var index = _currentModel.Steps.FindIndex(s => s.Id == updatedStep.Id);
            if (index >= 0)
            {
                SaveUndo();
                _currentModel.Steps[index] = updatedStep;
                _redoStack.Clear();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a connection between two steps
        /// </summary>
        public ProcessModelConnection AddConnection(string sourceStepId, string targetStepId, ConnectionType type = ConnectionType.Normal)
        {
            SaveUndo();
            var connection = new ProcessModelConnection
            {
                SourceStepId = sourceStepId,
                TargetStepId = targetStepId,
                Type = type
            };
            _currentModel.Connections.Add(connection);
            _redoStack.Clear();
            return connection;
        }

        /// <summary>
        /// Removes a connection
        /// </summary>
        public bool RemoveConnection(string connectionId)
        {
            var connection = _currentModel.Connections.FirstOrDefault(c => c.Id == connectionId);
            if (connection != null)
            {
                SaveUndo();
                _currentModel.Connections.Remove(connection);
                _redoStack.Clear();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a variable to the process model
        /// </summary>
        public ProcessModelVariable AddVariable(string name, string type, object? defaultValue = null)
        {
            SaveUndo();
            var variable = new ProcessModelVariable
            {
                Name = name,
                Type = type,
                DefaultValue = defaultValue
            };
            _currentModel.Variables.Add(variable);
            _redoStack.Clear();
            return variable;
        }

        /// <summary>
        /// Validates the process model
        /// </summary>
        public ProcessModelValidationResult Validate()
        {
            var result = new ProcessModelValidationResult();

            if (!_currentModel.Steps.Any(s => s.IsEntryPoint))
            {
                result.Errors.Add(new ProcessModelValidationError
                {
                    Message = "Process model must have at least one entry point",
                    ErrorCode = "PM001"
                });
            }

            if (!_currentModel.Steps.Any(s => s.IsExitPoint))
            {
                result.Errors.Add(new ProcessModelValidationError
                {
                    Message = "Process model must have at least one exit point",
                    ErrorCode = "PM002"
                });
            }

            foreach (var step in _currentModel.Steps)
            {
                if (!step.IsEntryPoint && !_currentModel.Connections.Any(c => c.TargetStepId == step.Id))
                {
                    result.Warnings.Add(new ProcessModelValidationWarning
                    {
                        StepId = step.Id,
                        Message = $"Step '{step.Name}' has no incoming connections",
                        WarningCode = "PMW001"
                    });
                }

                if (!step.IsExitPoint && !_currentModel.Connections.Any(c => c.SourceStepId == step.Id))
                {
                    result.Warnings.Add(new ProcessModelValidationWarning
                    {
                        StepId = step.Id,
                        Message = $"Step '{step.Name}' has no outgoing connections",
                        WarningCode = "PMW002"
                    });
                }
            }

            foreach (var step in _currentModel.Steps.Where(s => s.StepType == ProcessModelStepType.Decision))
            {
                var connections = _currentModel.Connections.Where(c => c.SourceStepId == step.Id).ToList();
                if (!connections.Any(c => c.Type == ConnectionType.TrueBranch))
                {
                    result.Errors.Add(new ProcessModelValidationError
                    {
                        StepId = step.Id,
                        Message = $"Decision step '{step.Name}' must have a True branch",
                        ErrorCode = "PM003"
                    });
                }
                if (!connections.Any(c => c.Type == ConnectionType.FalseBranch))
                {
                    result.Errors.Add(new ProcessModelValidationError
                    {
                        StepId = step.Id,
                        Message = $"Decision step '{step.Name}' must have a False branch",
                        ErrorCode = "PM004"
                    });
                }
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }

        /// <summary>
        /// Undoes the last change
        /// </summary>
        public bool Undo()
        {
            if (_undoStack.Count > 0)
            {
                _redoStack.Push(CloneModel(_currentModel));
                _currentModel = _undoStack.Pop();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Redoes the last undone change
        /// </summary>
        public bool Redo()
        {
            if (_redoStack.Count > 0)
            {
                _undoStack.Push(CloneModel(_currentModel));
                _currentModel = _redoStack.Pop();
                return true;
            }
            return false;
        }

        private void SaveUndo()
        {
            _undoStack.Push(CloneModel(_currentModel));
            if (_undoStack.Count > 50)
            {
                var list = _undoStack.ToList();
                list.RemoveAt(list.Count - 1);
                _undoStack.Clear();
                foreach (var item in list.AsEnumerable().Reverse())
                {
                    _undoStack.Push(item);
                }
            }
        }

        private ProcessModelDefinition CloneModel(ProcessModelDefinition model)
        {
            var json = JsonSerializer.Serialize(model);
            return JsonSerializer.Deserialize<ProcessModelDefinition>(json) ?? new ProcessModelDefinition();
        }

        /// <summary>
        /// Gets step by ID
        /// </summary>
        public ProcessModelStep? GetStep(string stepId)
        {
            return _currentModel.Steps.FirstOrDefault(s => s.Id == stepId);
        }

        /// <summary>
        /// Gets all connections for a step
        /// </summary>
        public List<ProcessModelConnection> GetStepConnections(string stepId)
        {
            return _currentModel.Connections
                .Where(c => c.SourceStepId == stepId || c.TargetStepId == stepId)
                .ToList();
        }

        /// <summary>
        /// Creates a default sequential model
        /// </summary>
        public void CreateDefaultSequentialModel()
        {
            NewModel("Sequential Model", ProcessModelType.Sequential);

            var entry = AddStep("Entry Point", ProcessModelStepType.Entry);
            entry.IsEntryPoint = true;

            var preUut = AddStep("Pre-UUT Callback", ProcessModelStepType.Callback);
            var mainSeq = AddStep("Main Sequence", ProcessModelStepType.SequenceCall);
            var postUut = AddStep("Post-UUT Callback", ProcessModelStepType.Callback);
            var decision = AddStep("Continue?", ProcessModelStepType.Decision);
            decision.ConditionExpression = "UUTLoop == true";

            var exit = AddStep("Exit Point", ProcessModelStepType.Exit);
            exit.IsExitPoint = true;

            AddConnection(entry.Id, preUut.Id);
            AddConnection(preUut.Id, mainSeq.Id);
            AddConnection(mainSeq.Id, postUut.Id);
            AddConnection(postUut.Id, decision.Id);
            AddConnection(decision.Id, preUut.Id, ConnectionType.TrueBranch);
            AddConnection(decision.Id, exit.Id, ConnectionType.FalseBranch);
        }

        /// <summary>
        /// Creates a default parallel model
        /// </summary>
        public void CreateDefaultParallelModel()
        {
            NewModel("Parallel Model", ProcessModelType.Parallel);

            var entry = AddStep("Entry Point", ProcessModelStepType.Entry);
            entry.IsEntryPoint = true;

            var parallel = AddStep("Parallel Execution", ProcessModelStepType.Parallel);
            var sync = AddStep("Synchronization", ProcessModelStepType.Wait);

            var exit = AddStep("Exit Point", ProcessModelStepType.Exit);
            exit.IsExitPoint = true;

            AddConnection(entry.Id, parallel.Id);
            AddConnection(parallel.Id, sync.Id);
            AddConnection(sync.Id, exit.Id);
        }
    }

    /// <summary>
    /// Manager for process model templates
    /// </summary>
    public class ProcessModelTemplateManager
    {
        private static readonly Lazy<ProcessModelTemplateManager> _instance = 
            new(() => new ProcessModelTemplateManager());
        
        public static ProcessModelTemplateManager Instance => _instance.Value;

        private readonly Dictionary<string, ProcessModelDefinition> _templates = new();

        private ProcessModelTemplateManager()
        {
            InitializeBuiltInTemplates();
        }

        private void InitializeBuiltInTemplates()
        {
            var editor = new ProcessModelEditor();
            editor.CreateDefaultSequentialModel();
            _templates["Sequential"] = editor.CurrentModel;

            editor = new ProcessModelEditor();
            editor.CreateDefaultParallelModel();
            _templates["Parallel"] = editor.CurrentModel;
        }

        /// <summary>
        /// Gets all available templates
        /// </summary>
        public IEnumerable<string> GetTemplateNames()
        {
            return _templates.Keys;
        }

        /// <summary>
        /// Gets a template by name
        /// </summary>
        public ProcessModelDefinition? GetTemplate(string name)
        {
            return _templates.TryGetValue(name, out var template) ? template : null;
        }

        /// <summary>
        /// Creates a new model from a template
        /// </summary>
        public ProcessModelDefinition? CreateFromTemplate(string templateName)
        {
            var template = GetTemplate(templateName);
            if (template != null)
            {
                var json = JsonSerializer.Serialize(template);
                var newModel = JsonSerializer.Deserialize<ProcessModelDefinition>(json);
                if (newModel != null)
                {
                    newModel.Id = Guid.NewGuid().ToString();
                    newModel.CreatedDate = DateTime.UtcNow;
                    newModel.ModifiedDate = DateTime.UtcNow;
                    return newModel;
                }
            }
            return null;
        }

        /// <summary>
        /// Adds a custom template
        /// </summary>
        public void AddTemplate(string name, ProcessModelDefinition model)
        {
            _templates[name] = model;
        }
    }
}
