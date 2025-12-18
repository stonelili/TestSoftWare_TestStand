// =============================================================================
// StepGroups.cs - Custom step groups management
// Part of TestStandClone.Core - TestStand-like test automation framework
// =============================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace TestStandClone.Core.StepGroups
{
    /// <summary>
    /// Represents a step group.
    /// Similar to TestStand's step groups (Setup, Main, Cleanup).
    /// </summary>
    public class StepGroup : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private StepGroupType _type;
        private bool _enabled = true;
        private string _description = string.Empty;

        /// <summary>
        /// Gets or sets the group name.
        /// </summary>
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        /// <summary>
        /// Gets or sets the group type.
        /// </summary>
        public StepGroupType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(nameof(Type)); }
        }

        /// <summary>
        /// Gets or sets whether the group is enabled.
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set { _enabled = value; OnPropertyChanged(nameof(Enabled)); }
        }

        /// <summary>
        /// Gets or sets the group description.
        /// </summary>
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        /// <summary>
        /// Gets the steps in this group.
        /// </summary>
        public ObservableCollection<TestStep> Steps { get; } = new();

        /// <summary>
        /// Gets or sets execution options for the group.
        /// </summary>
        public StepGroupExecutionOptions ExecutionOptions { get; set; } = new();

        /// <summary>
        /// Gets the number of steps in the group.
        /// </summary>
        public int StepCount => Steps.Count;

        /// <summary>
        /// Gets the number of passed steps.
        /// </summary>
        public int PassedCount => Steps.Count(s => s.Status == StepStatus.Passed);

        /// <summary>
        /// Gets the number of failed steps.
        /// </summary>
        public int FailedCount => Steps.Count(s => s.Status == StepStatus.Failed);

        /// <summary>
        /// Creates a new step group.
        /// </summary>
        public StepGroup() { }

        /// <summary>
        /// Creates a new step group with the given name and type.
        /// </summary>
        public StepGroup(string name, StepGroupType type = StepGroupType.Main)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Adds a step to the group.
        /// </summary>
        public void AddStep(TestStep step)
        {
            Steps.Add(step);
        }

        /// <summary>
        /// Inserts a step at the specified index.
        /// </summary>
        public void InsertStep(int index, TestStep step)
        {
            Steps.Insert(index, step);
        }

        /// <summary>
        /// Removes a step from the group.
        /// </summary>
        public bool RemoveStep(TestStep step)
        {
            return Steps.Remove(step);
        }

        /// <summary>
        /// Clears all steps from the group.
        /// </summary>
        public void ClearSteps()
        {
            Steps.Clear();
        }

        /// <summary>
        /// Resets all step statuses in the group.
        /// </summary>
        public void ResetSteps()
        {
            foreach (var step in Steps)
            {
                step.Status = StepStatus.Idle;
                step.ResultText = string.Empty;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Types of step groups.
    /// </summary>
    public enum StepGroupType
    {
        /// <summary>Setup group - runs before Main</summary>
        Setup,
        /// <summary>Main group - primary test steps</summary>
        Main,
        /// <summary>Cleanup group - runs after Main (always)</summary>
        Cleanup,
        /// <summary>Custom group</summary>
        Custom
    }

    /// <summary>
    /// Execution options for step groups.
    /// </summary>
    public class StepGroupExecutionOptions
    {
        /// <summary>
        /// Gets or sets whether to run even if previous groups failed.
        /// </summary>
        public bool RunOnFailure { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to stop on first failure.
        /// </summary>
        public bool StopOnFailure { get; set; } = false;

        /// <summary>
        /// Gets or sets the timeout in milliseconds (0 = no timeout).
        /// </summary>
        public int TimeoutMs { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of retries on failure.
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the delay between retries in milliseconds.
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to record results for this group.
        /// </summary>
        public bool RecordResults { get; set; } = true;

        /// <summary>
        /// Gets or sets the precondition expression.
        /// </summary>
        public string Precondition { get; set; } = string.Empty;
    }

    /// <summary>
    /// Manager for step groups within a sequence.
    /// </summary>
    public class StepGroupManager
    {
        private readonly Dictionary<string, StepGroup> _groups = new();
        private readonly List<string> _executionOrder = new();

        /// <summary>
        /// Gets all groups.
        /// </summary>
        public IReadOnlyDictionary<string, StepGroup> Groups => _groups;

        /// <summary>
        /// Gets the execution order of groups.
        /// </summary>
        public IReadOnlyList<string> ExecutionOrder => _executionOrder.AsReadOnly();

        /// <summary>
        /// Gets the setup group.
        /// </summary>
        public StepGroup? Setup => GetGroup("Setup");

        /// <summary>
        /// Gets the main group.
        /// </summary>
        public StepGroup? Main => GetGroup("Main");

        /// <summary>
        /// Gets the cleanup group.
        /// </summary>
        public StepGroup? Cleanup => GetGroup("Cleanup");

        /// <summary>
        /// Creates a new manager with default groups.
        /// </summary>
        public StepGroupManager()
        {
            // Create default groups
            AddGroup(new StepGroup("Setup", StepGroupType.Setup));
            AddGroup(new StepGroup("Main", StepGroupType.Main));
            AddGroup(new StepGroup("Cleanup", StepGroupType.Cleanup)
            {
                ExecutionOptions = new StepGroupExecutionOptions { RunOnFailure = true }
            });
        }

        /// <summary>
        /// Adds a step group.
        /// </summary>
        public void AddGroup(StepGroup group)
        {
            _groups[group.Name] = group;
            if (!_executionOrder.Contains(group.Name))
            {
                // Insert based on type
                int insertIndex = group.Type switch
                {
                    StepGroupType.Setup => 0,
                    StepGroupType.Cleanup => _executionOrder.Count,
                    _ => _executionOrder.IndexOf("Cleanup") >= 0 
                        ? _executionOrder.IndexOf("Cleanup") 
                        : _executionOrder.Count
                };
                _executionOrder.Insert(insertIndex, group.Name);
            }
        }

        /// <summary>
        /// Gets a step group by name.
        /// </summary>
        public StepGroup? GetGroup(string name)
        {
            return _groups.TryGetValue(name, out var group) ? group : null;
        }

        /// <summary>
        /// Removes a step group.
        /// </summary>
        public bool RemoveGroup(string name)
        {
            // Don't allow removing default groups
            if (name == "Setup" || name == "Main" || name == "Cleanup")
                return false;

            _executionOrder.Remove(name);
            return _groups.Remove(name);
        }

        /// <summary>
        /// Gets groups in execution order.
        /// </summary>
        public IEnumerable<StepGroup> GetGroupsInOrder()
        {
            foreach (var name in _executionOrder)
            {
                if (_groups.TryGetValue(name, out var group) && group.Enabled)
                    yield return group;
            }
        }

        /// <summary>
        /// Sets the execution order.
        /// </summary>
        public void SetExecutionOrder(params string[] order)
        {
            _executionOrder.Clear();
            _executionOrder.AddRange(order);
        }

        /// <summary>
        /// Moves a group in the execution order.
        /// </summary>
        public void MoveGroup(string name, int newIndex)
        {
            if (!_executionOrder.Contains(name))
                return;

            _executionOrder.Remove(name);
            _executionOrder.Insert(Math.Clamp(newIndex, 0, _executionOrder.Count), name);
        }

        /// <summary>
        /// Gets all steps from all groups in execution order.
        /// </summary>
        public IEnumerable<TestStep> GetAllSteps()
        {
            return GetGroupsInOrder().SelectMany(g => g.Steps);
        }

        /// <summary>
        /// Gets the total step count.
        /// </summary>
        public int TotalStepCount => _groups.Values.Sum(g => g.StepCount);

        /// <summary>
        /// Resets all groups.
        /// </summary>
        public void ResetAll()
        {
            foreach (var group in _groups.Values)
            {
                group.ResetSteps();
            }
        }

        /// <summary>
        /// Adds a step to the specified group.
        /// </summary>
        public void AddStepToGroup(TestStep step, string groupName = "Main")
        {
            var group = GetGroup(groupName);
            group?.AddStep(step);
        }
    }
}
