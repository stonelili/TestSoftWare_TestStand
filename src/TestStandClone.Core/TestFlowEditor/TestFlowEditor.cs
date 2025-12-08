using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.TestFlowEditor
{
    /// <summary>
    /// Flow node types
    /// </summary>
    public enum FlowNodeType
    {
        Start,
        End,
        Step,
        Decision,
        Loop,
        Parallel,
        Merge,
        SubFlow
    }

    /// <summary>
    /// Connection types
    /// </summary>
    public enum ConnectionType
    {
        Normal,
        OnPass,
        OnFail,
        OnError,
        Conditional
    }

    /// <summary>
    /// Flow node
    /// </summary>
    public class FlowNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public FlowNodeType Type { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 120;
        public double Height { get; set; } = 60;
        public string? StepId { get; set; }
        public string? Condition { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public bool IsSelected { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Flow connection
    /// </summary>
    public class FlowConnection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SourceNodeId { get; set; } = string.Empty;
        public string TargetNodeId { get; set; } = string.Empty;
        public ConnectionType Type { get; set; } = ConnectionType.Normal;
        public string? Label { get; set; }
        public string? Condition { get; set; }
        public int Priority { get; set; }
    }

    /// <summary>
    /// Test flow definition
    /// </summary>
    public class TestFlowDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<FlowNode> Nodes { get; set; } = new List<FlowNode>();
        public List<FlowConnection> Connections { get; set; } = new List<FlowConnection>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        public string Version { get; set; } = "1.0";
    }

    /// <summary>
    /// Edit action for undo/redo
    /// </summary>
    public abstract class FlowEditAction
    {
        public abstract void Execute(TestFlowDefinition flow);
        public abstract void Undo(TestFlowDefinition flow);
    }

    /// <summary>
    /// Add node action
    /// </summary>
    public class AddNodeAction : FlowEditAction
    {
        private readonly FlowNode _node;
        public AddNodeAction(FlowNode node) { _node = node; }
        public override void Execute(TestFlowDefinition flow) => flow.Nodes.Add(_node);
        public override void Undo(TestFlowDefinition flow) => flow.Nodes.RemoveAll(n => n.Id == _node.Id);
    }

    /// <summary>
    /// Remove node action
    /// </summary>
    public class RemoveNodeAction : FlowEditAction
    {
        private readonly FlowNode _node;
        private readonly List<FlowConnection> _removedConnections = new List<FlowConnection>();
        
        public RemoveNodeAction(FlowNode node) { _node = node; }
        
        public override void Execute(TestFlowDefinition flow)
        {
            _removedConnections.Clear();
            _removedConnections.AddRange(flow.Connections.Where(c => c.SourceNodeId == _node.Id || c.TargetNodeId == _node.Id));
            flow.Connections.RemoveAll(c => c.SourceNodeId == _node.Id || c.TargetNodeId == _node.Id);
            flow.Nodes.RemoveAll(n => n.Id == _node.Id);
        }
        
        public override void Undo(TestFlowDefinition flow)
        {
            flow.Nodes.Add(_node);
            flow.Connections.AddRange(_removedConnections);
        }
    }

    /// <summary>
    /// Add connection action
    /// </summary>
    public class AddConnectionAction : FlowEditAction
    {
        private readonly FlowConnection _connection;
        public AddConnectionAction(FlowConnection connection) { _connection = connection; }
        public override void Execute(TestFlowDefinition flow) => flow.Connections.Add(_connection);
        public override void Undo(TestFlowDefinition flow) => flow.Connections.RemoveAll(c => c.Id == _connection.Id);
    }

    /// <summary>
    /// Flow validation result
    /// </summary>
    public class FlowValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// Test flow editor
    /// </summary>
    public class TestFlowEditorManager
    {
        private static readonly Lazy<TestFlowEditorManager> _instance = new Lazy<TestFlowEditorManager>(() => new TestFlowEditorManager());
        public static TestFlowEditorManager Instance => _instance.Value;

        private readonly Stack<FlowEditAction> _undoStack = new Stack<FlowEditAction>();
        private readonly Stack<FlowEditAction> _redoStack = new Stack<FlowEditAction>();
        private TestFlowDefinition? _currentFlow;
        private readonly object _lock = new object();

        public event EventHandler<TestFlowDefinition>? FlowModified;

        private TestFlowEditorManager() { }

        public TestFlowDefinition CreateNewFlow(string name)
        {
            lock (_lock)
            {
                _currentFlow = new TestFlowDefinition
                {
                    Name = name,
                    Nodes = new List<FlowNode>
                    {
                        new FlowNode { Type = FlowNodeType.Start, Name = "Start", X = 100, Y = 100 },
                        new FlowNode { Type = FlowNodeType.End, Name = "End", X = 500, Y = 100 }
                    }
                };
                _undoStack.Clear();
                _redoStack.Clear();
                return _currentFlow;
            }
        }

        public void LoadFlow(TestFlowDefinition flow)
        {
            lock (_lock)
            {
                _currentFlow = flow;
                _undoStack.Clear();
                _redoStack.Clear();
            }
        }

        public void ExecuteAction(FlowEditAction action)
        {
            lock (_lock)
            {
                if (_currentFlow == null) return;
                action.Execute(_currentFlow);
                _undoStack.Push(action);
                _redoStack.Clear();
                _currentFlow.ModifiedAt = DateTime.UtcNow;
                FlowModified?.Invoke(this, _currentFlow);
            }
        }

        public void Undo()
        {
            lock (_lock)
            {
                if (_currentFlow == null || _undoStack.Count == 0) return;
                var action = _undoStack.Pop();
                action.Undo(_currentFlow);
                _redoStack.Push(action);
                FlowModified?.Invoke(this, _currentFlow);
            }
        }

        public void Redo()
        {
            lock (_lock)
            {
                if (_currentFlow == null || _redoStack.Count == 0) return;
                var action = _redoStack.Pop();
                action.Execute(_currentFlow);
                _undoStack.Push(action);
                FlowModified?.Invoke(this, _currentFlow);
            }
        }

        public FlowNode AddNode(FlowNodeType type, string name, double x, double y)
        {
            var node = new FlowNode { Type = type, Name = name, X = x, Y = y };
            ExecuteAction(new AddNodeAction(node));
            return node;
        }

        public void RemoveNode(string nodeId)
        {
            lock (_lock)
            {
                var node = _currentFlow?.Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node != null)
                    ExecuteAction(new RemoveNodeAction(node));
            }
        }

        public FlowConnection AddConnection(string sourceId, string targetId, ConnectionType type = ConnectionType.Normal)
        {
            var connection = new FlowConnection { SourceNodeId = sourceId, TargetNodeId = targetId, Type = type };
            ExecuteAction(new AddConnectionAction(connection));
            return connection;
        }

        public FlowValidationResult ValidateFlow()
        {
            var result = new FlowValidationResult { IsValid = true };
            lock (_lock)
            {
                if (_currentFlow == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("No flow loaded");
                    return result;
                }

                var startNodes = _currentFlow.Nodes.Where(n => n.Type == FlowNodeType.Start).ToList();
                var endNodes = _currentFlow.Nodes.Where(n => n.Type == FlowNodeType.End).ToList();

                if (startNodes.Count == 0) { result.IsValid = false; result.Errors.Add("No start node"); }
                if (startNodes.Count > 1) { result.Warnings.Add("Multiple start nodes"); }
                if (endNodes.Count == 0) { result.IsValid = false; result.Errors.Add("No end node"); }

                foreach (var node in _currentFlow.Nodes.Where(n => n.Type == FlowNodeType.Step))
                {
                    var hasIncoming = _currentFlow.Connections.Any(c => c.TargetNodeId == node.Id);
                    var hasOutgoing = _currentFlow.Connections.Any(c => c.SourceNodeId == node.Id);
                    if (!hasIncoming) result.Warnings.Add($"Node '{node.Name}' has no incoming connection");
                    if (!hasOutgoing) result.Warnings.Add($"Node '{node.Name}' has no outgoing connection");
                }
            }
            return result;
        }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public TestFlowDefinition? CurrentFlow => _currentFlow;
    }
}
