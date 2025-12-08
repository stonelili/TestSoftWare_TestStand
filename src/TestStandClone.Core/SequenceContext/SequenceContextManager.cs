using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.SequenceContext
{
    #region Sequence Context Classes

    /// <summary>
    /// Execution context scope
    /// </summary>
    public enum ContextScope
    {
        Step,
        Sequence,
        File,
        Station,
        Global
    }

    /// <summary>
    /// Context property with metadata
    /// </summary>
    public class ContextProperty
    {
        public string Name { get; set; } = string.Empty;
        public object? Value { get; set; }
        public Type ValueType { get; set; } = typeof(object);
        public ContextScope Scope { get; set; } = ContextScope.Sequence;
        public bool IsReadOnly { get; set; }
        public bool IsPersistent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Context change event args
    /// </summary>
    public class ContextChangeEventArgs : EventArgs
    {
        public string PropertyName { get; set; } = string.Empty;
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public ContextScope Scope { get; set; }
    }

    /// <summary>
    /// Extended context with hierarchical property management
    /// </summary>
    public class ExecutionContext
    {
        private readonly Dictionary<string, ContextProperty> _properties = new();
        private readonly Dictionary<string, object> _stepResults = new();
        private readonly Stack<string> _callStack = new();
        private ExecutionContext? _parentContext;

        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public ContextScope Scope { get; set; } = ContextScope.Sequence;
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }
        public bool IsActive { get; set; } = true;

        public event EventHandler<ContextChangeEventArgs>? PropertyChanged;
        public event EventHandler<string>? CallStackChanged;

        public IReadOnlyDictionary<string, ContextProperty> Properties => _properties;
        public IReadOnlyDictionary<string, object> StepResults => _stepResults;
        public IReadOnlyCollection<string> CallStack => _callStack;

        public ExecutionContext() { }

        public ExecutionContext(ExecutionContext parent)
        {
            _parentContext = parent;
        }

        public void SetProperty(string name, object? value, ContextScope scope = ContextScope.Sequence)
        {
            var oldValue = GetPropertyValue(name);
            
            if (_properties.TryGetValue(name, out var existing))
            {
                if (existing.IsReadOnly)
                    throw new InvalidOperationException($"Property '{name}' is read-only");
                
                existing.Value = value;
                existing.ModifiedAt = DateTime.Now;
            }
            else
            {
                _properties[name] = new ContextProperty
                {
                    Name = name,
                    Value = value,
                    ValueType = value?.GetType() ?? typeof(object),
                    Scope = scope
                };
            }

            PropertyChanged?.Invoke(this, new ContextChangeEventArgs
            {
                PropertyName = name,
                OldValue = oldValue,
                NewValue = value,
                Scope = scope
            });
        }

        public object? GetPropertyValue(string name)
        {
            if (_properties.TryGetValue(name, out var prop))
                return prop.Value;

            return _parentContext?.GetPropertyValue(name);
        }

        public T? GetPropertyValue<T>(string name, T? defaultValue = default)
        {
            var value = GetPropertyValue(name);
            if (value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        public bool HasProperty(string name)
        {
            return _properties.ContainsKey(name) || (_parentContext?.HasProperty(name) ?? false);
        }

        public void RemoveProperty(string name)
        {
            if (_properties.TryGetValue(name, out var prop) && !prop.IsReadOnly)
            {
                _properties.Remove(name);
            }
        }

        public void SetStepResult(string stepName, object result)
        {
            _stepResults[$"Step.{stepName}.Result"] = result;
        }

        public void SetStepStatus(string stepName, string status)
        {
            _stepResults[$"Step.{stepName}.Status"] = status;
        }

        public object? GetStepResult(string stepName)
        {
            return _stepResults.TryGetValue($"Step.{stepName}.Result", out var result) ? result : null;
        }

        public string? GetStepStatus(string stepName)
        {
            return _stepResults.TryGetValue($"Step.{stepName}.Status", out var status) ? status as string : null;
        }

        public void PushCallStack(string sequenceName)
        {
            _callStack.Push(sequenceName);
            CallStackChanged?.Invoke(this, sequenceName);
        }

        public string? PopCallStack()
        {
            if (_callStack.Count > 0)
            {
                var popped = _callStack.Pop();
                CallStackChanged?.Invoke(this, popped);
                return popped;
            }
            return null;
        }

        public string? PeekCallStack()
        {
            return _callStack.Count > 0 ? _callStack.Peek() : null;
        }

        public int GetCallStackDepth()
        {
            return _callStack.Count;
        }

        public void ClearCallStack()
        {
            _callStack.Clear();
        }

        public ExecutionContext CreateChildContext(string name, ContextScope scope = ContextScope.Sequence)
        {
            return new ExecutionContext(this)
            {
                Name = name,
                Scope = scope
            };
        }

        public Dictionary<string, object?> GetAllProperties()
        {
            var result = new Dictionary<string, object?>();
            
            // Get parent properties first
            if (_parentContext != null)
            {
                foreach (var prop in _parentContext.GetAllProperties())
                {
                    result[prop.Key] = prop.Value;
                }
            }

            // Override with local properties
            foreach (var prop in _properties)
            {
                result[prop.Key] = prop.Value.Value;
            }

            return result;
        }

        public void End()
        {
            IsActive = false;
            EndTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Manages execution contexts
    /// </summary>
    public class SequenceContextManager
    {
        private static readonly Lazy<SequenceContextManager> _instance = 
            new(() => new SequenceContextManager());
        public static SequenceContextManager Instance => _instance.Value;

        private readonly Dictionary<string, ExecutionContext> _contexts = new();
        private ExecutionContext? _currentContext;
        private readonly Stack<ExecutionContext> _contextStack = new();

        public event EventHandler<ExecutionContext>? ContextCreated;
        public event EventHandler<ExecutionContext>? ContextActivated;
        public event EventHandler<ExecutionContext>? ContextDeactivated;

        public ExecutionContext? CurrentContext => _currentContext;
        public IReadOnlyDictionary<string, ExecutionContext> Contexts => _contexts;

        public ExecutionContext CreateContext(string name, ContextScope scope = ContextScope.Sequence)
        {
            var context = new ExecutionContext
            {
                Name = name,
                Scope = scope
            };

            _contexts[context.Id] = context;
            ContextCreated?.Invoke(this, context);

            return context;
        }

        public void ActivateContext(string contextId)
        {
            if (_contexts.TryGetValue(contextId, out var context))
            {
                if (_currentContext != null)
                {
                    _contextStack.Push(_currentContext);
                    ContextDeactivated?.Invoke(this, _currentContext);
                }

                _currentContext = context;
                ContextActivated?.Invoke(this, context);
            }
        }

        public void DeactivateCurrentContext()
        {
            if (_currentContext != null)
            {
                _currentContext.End();
                ContextDeactivated?.Invoke(this, _currentContext);

                _currentContext = _contextStack.Count > 0 ? _contextStack.Pop() : null;
                if (_currentContext != null)
                {
                    ContextActivated?.Invoke(this, _currentContext);
                }
            }
        }

        public void RemoveContext(string contextId)
        {
            if (_contexts.TryGetValue(contextId, out var context))
            {
                if (_currentContext == context)
                {
                    DeactivateCurrentContext();
                }
                _contexts.Remove(contextId);
            }
        }

        public void ClearAllContexts()
        {
            foreach (var context in _contexts.Values)
            {
                context.End();
            }
            _contexts.Clear();
            _contextStack.Clear();
            _currentContext = null;
        }

        public List<ExecutionContext> GetActiveContexts()
        {
            return _contexts.Values.Where(c => c.IsActive).ToList();
        }
    }

    #endregion
}
