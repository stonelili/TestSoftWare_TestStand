using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace TestStandClone.Core
{
    /// <summary>
    /// Represents a node in the sequence hierarchy tree
    /// </summary>
    public class SequenceHierarchyNode : INotifyPropertyChanged
    {
        private bool _isExpanded = true;
        private bool _isSelected;
        private StepStatus _status = StepStatus.Idle;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string NodeType { get; set; } = string.Empty; // "Sequence", "Step", "Group"
        public string Description { get; set; } = string.Empty;
        public int Depth { get; set; }
        
        public StepStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public TestStep? Step { get; set; }
        public Sequence? Sequence { get; set; }
        public SequenceHierarchyNode? Parent { get; set; }
        public ObservableCollection<SequenceHierarchyNode> Children { get; } = new();

        public bool HasChildren => Children.Count > 0;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Add a child node
        /// </summary>
        public void AddChild(SequenceHierarchyNode child)
        {
            child.Parent = this;
            child.Depth = Depth + 1;
            Children.Add(child);
        }

        /// <summary>
        /// Remove a child node
        /// </summary>
        public bool RemoveChild(SequenceHierarchyNode child)
        {
            return Children.Remove(child);
        }

        /// <summary>
        /// Get all descendants
        /// </summary>
        public IEnumerable<SequenceHierarchyNode> GetAllDescendants()
        {
            foreach (var child in Children)
            {
                yield return child;
                foreach (var descendant in child.GetAllDescendants())
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        /// Get the path from root to this node
        /// </summary>
        public string GetPath()
        {
            var parts = new List<string>();
            var current = this;
            while (current != null)
            {
                parts.Insert(0, current.Name);
                current = current.Parent;
            }
            return string.Join(" / ", parts);
        }
    }

    /// <summary>
    /// Manages the hierarchy visualization for sequences
    /// Similar to TestStand Sequence Hierarchy View
    /// </summary>
    public class SequenceHierarchyBuilder
    {
        /// <summary>
        /// Build hierarchy tree from a sequence
        /// </summary>
        public SequenceHierarchyNode BuildHierarchy(Sequence sequence)
        {
            var root = new SequenceHierarchyNode
            {
                Id = sequence.Name,
                Name = sequence.Name,
                NodeType = "Sequence",
                Description = sequence.Description,
                Sequence = sequence,
                Depth = 0
            };

            // Add Setup group
            if (sequence.SetupSteps.Count > 0)
            {
                var setupGroup = new SequenceHierarchyNode
                {
                    Name = "Setup",
                    NodeType = "Group",
                    Description = "Setup steps run before main sequence"
                };
                root.AddChild(setupGroup);

                foreach (var step in sequence.SetupSteps)
                {
                    AddStepNode(setupGroup, step);
                }
            }

            // Add Main group
            if (sequence.MainSteps.Count > 0)
            {
                var mainGroup = new SequenceHierarchyNode
                {
                    Name = "Main",
                    NodeType = "Group",
                    Description = "Main sequence steps"
                };
                root.AddChild(mainGroup);

                foreach (var step in sequence.MainSteps)
                {
                    AddStepNode(mainGroup, step);
                }
            }

            // Add Cleanup group
            if (sequence.CleanupSteps.Count > 0)
            {
                var cleanupGroup = new SequenceHierarchyNode
                {
                    Name = "Cleanup",
                    NodeType = "Group",
                    Description = "Cleanup steps run after main sequence"
                };
                root.AddChild(cleanupGroup);

                foreach (var step in sequence.CleanupSteps)
                {
                    AddStepNode(cleanupGroup, step);
                }
            }

            // If no groups, add steps directly (for backward compatibility)
            if (sequence.SetupSteps.Count == 0 && sequence.MainSteps.Count == 0 && sequence.CleanupSteps.Count == 0)
            {
                foreach (var step in sequence.Steps)
                {
                    AddStepNode(root, step);
                }
            }

            return root;
        }

        private void AddStepNode(SequenceHierarchyNode parent, TestStep step)
        {
            var stepNode = new SequenceHierarchyNode
            {
                Id = step.Id.ToString(),
                Name = step.Name,
                NodeType = "Step",
                Description = step.GetType().Name,
                Step = step,
                Status = step.Status
            };
            parent.AddChild(stepNode);

            // If this is a sequence call step, add the sub-sequence
            if (step is Steps.SequenceCallStep callStep && callStep.TargetSequence != null)
            {
                var subHierarchy = BuildHierarchy(callStep.TargetSequence);
                subHierarchy.Parent = stepNode;
                subHierarchy.Depth = stepNode.Depth + 1;
                stepNode.Children.Add(subHierarchy);
            }
        }

        /// <summary>
        /// Update step statuses in the hierarchy from the actual sequence
        /// </summary>
        public void UpdateStatuses(SequenceHierarchyNode root)
        {
            if (root.Sequence != null)
            {
                UpdateSequenceStatuses(root, root.Sequence);
            }
        }

        private void UpdateSequenceStatuses(SequenceHierarchyNode node, Sequence sequence)
        {
            foreach (var child in node.Children)
            {
                if (child.Step != null)
                {
                    child.Status = child.Step.Status;
                }
                
                // Recursively update children
                if (child.Children.Count > 0)
                {
                    if (child.Sequence != null)
                    {
                        UpdateSequenceStatuses(child, child.Sequence);
                    }
                    else
                    {
                        foreach (var grandChild in child.Children)
                        {
                            if (grandChild.Step != null)
                            {
                                grandChild.Status = grandChild.Step.Status;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Export hierarchy to text format
        /// </summary>
        public string ExportToText(SequenceHierarchyNode root, bool includeStatus = true)
        {
            var sb = new StringBuilder();
            ExportNodeToText(sb, root, 0, includeStatus);
            return sb.ToString();
        }

        private void ExportNodeToText(StringBuilder sb, SequenceHierarchyNode node, int indent, bool includeStatus)
        {
            var indentStr = new string(' ', indent * 2);
            var statusStr = includeStatus ? $" [{node.Status}]" : "";
            var typeStr = node.NodeType == "Step" ? $" ({node.Description})" : "";
            
            sb.AppendLine($"{indentStr}+ {node.Name}{typeStr}{statusStr}");
            
            foreach (var child in node.Children)
            {
                ExportNodeToText(sb, child, indent + 1, includeStatus);
            }
        }

        /// <summary>
        /// Find a node by step ID
        /// </summary>
        public SequenceHierarchyNode? FindNodeByStepId(SequenceHierarchyNode root, string stepId)
        {
            if (root.Id == stepId)
            {
                return root;
            }

            foreach (var child in root.Children)
            {
                var found = FindNodeByStepId(child, stepId);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Flatten hierarchy to a list
        /// </summary>
        public List<SequenceHierarchyNode> Flatten(SequenceHierarchyNode root)
        {
            var list = new List<SequenceHierarchyNode> { root };
            foreach (var child in root.Children)
            {
                list.AddRange(Flatten(child));
            }
            return list;
        }

        /// <summary>
        /// Get statistics about the hierarchy
        /// </summary>
        public HierarchyStatistics GetStatistics(SequenceHierarchyNode root)
        {
            var stats = new HierarchyStatistics();
            CollectStatistics(root, stats);
            return stats;
        }

        private void CollectStatistics(SequenceHierarchyNode node, HierarchyStatistics stats)
        {
            switch (node.NodeType)
            {
                case "Sequence":
                    stats.TotalSequences++;
                    break;
                case "Step":
                    stats.TotalSteps++;
                    switch (node.Status)
                    {
                        case StepStatus.Passed:
                            stats.PassedSteps++;
                            break;
                        case StepStatus.Failed:
                            stats.FailedSteps++;
                            break;
                        case StepStatus.Error:
                            stats.ErrorSteps++;
                            break;
                        case StepStatus.Running:
                            stats.RunningSteps++;
                            break;
                    }
                    break;
            }

            stats.MaxDepth = Math.Max(stats.MaxDepth, node.Depth);

            foreach (var child in node.Children)
            {
                CollectStatistics(child, stats);
            }
        }
    }

    /// <summary>
    /// Statistics about a sequence hierarchy
    /// </summary>
    public class HierarchyStatistics
    {
        public int TotalSequences { get; set; }
        public int TotalSteps { get; set; }
        public int PassedSteps { get; set; }
        public int FailedSteps { get; set; }
        public int ErrorSteps { get; set; }
        public int RunningSteps { get; set; }
        public int MaxDepth { get; set; }
        
        public double PassRate => TotalSteps > 0 ? (double)PassedSteps / TotalSteps * 100 : 0;
    }
}
