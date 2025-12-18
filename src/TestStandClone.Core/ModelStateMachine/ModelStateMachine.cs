using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.ModelStateMachine
{
    /// <summary>
    /// Represents a state in the process model
    /// </summary>
    public class ModelState
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public bool IsInitial { get; set; }
        public bool IsFinal { get; set; }
        public Action<StateContext>? OnEnter { get; set; }
        public Action<StateContext>? OnExit { get; set; }
        public Action<StateContext>? OnExecute { get; set; }
        public Dictionary<string, object?> Properties { get; set; } = new();
    }

    /// <summary>
    /// Represents a transition between states
    /// </summary>
    public class StateTransition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string SourceStateId { get; set; } = string.Empty;
        public string TargetStateId { get; set; } = string.Empty;
        public Func<StateContext, bool>? Guard { get; set; }
        public string? TriggerEvent { get; set; }
        public Action<StateContext>? OnTransition { get; set; }
        public int Priority { get; set; } = 0;
    }

    /// <summary>
    /// Context passed to state actions
    /// </summary>
    public class StateContext
    {
        public ModelState CurrentState { get; set; } = null!;
        public ModelState? PreviousState { get; set; }
        public StateTransition? TriggeringTransition { get; set; }
        public Dictionary<string, object?> Variables { get; set; } = new();
        public string? TriggerEvent { get; set; }
        public object? TriggerData { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// State machine for process model execution
    /// </summary>
    public class ModelStateMachine
    {
        private readonly Dictionary<string, ModelState> _states = new();
        private readonly List<StateTransition> _transitions = new();
        private ModelState? _currentState;
        private readonly List<StateHistoryEntry> _history = new();
        private bool _isRunning;

        public event EventHandler<StateChangedEventArgs>? StateChanged;
        public event EventHandler<TransitionEventArgs>? TransitionTriggered;

        public ModelState? CurrentState => _currentState;
        public bool IsRunning => _isRunning;
        public IReadOnlyList<StateHistoryEntry> History => _history.AsReadOnly();

        /// <summary>
        /// Adds a state to the machine
        /// </summary>
        public ModelState AddState(string name, bool isInitial = false, bool isFinal = false)
        {
            var state = new ModelState
            {
                Name = name,
                IsInitial = isInitial,
                IsFinal = isFinal
            };
            _states[state.Id] = state;

            if (isInitial && _currentState == null)
            {
                _currentState = state;
            }

            return state;
        }

        /// <summary>
        /// Adds a transition between states
        /// </summary>
        public StateTransition AddTransition(string sourceStateId, string targetStateId, string? triggerEvent = null, Func<StateContext, bool>? guard = null)
        {
            var transition = new StateTransition
            {
                SourceStateId = sourceStateId,
                TargetStateId = targetStateId,
                TriggerEvent = triggerEvent,
                Guard = guard
            };
            _transitions.Add(transition);
            return transition;
        }

        /// <summary>
        /// Adds a transition between states by name
        /// </summary>
        public StateTransition AddTransitionByName(string sourceStateName, string targetStateName, string? triggerEvent = null, Func<StateContext, bool>? guard = null)
        {
            var sourceState = _states.Values.FirstOrDefault(s => s.Name == sourceStateName);
            var targetState = _states.Values.FirstOrDefault(s => s.Name == targetStateName);

            if (sourceState == null) throw new ArgumentException($"Source state '{sourceStateName}' not found");
            if (targetState == null) throw new ArgumentException($"Target state '{targetStateName}' not found");

            return AddTransition(sourceState.Id, targetState.Id, triggerEvent, guard);
        }

        /// <summary>
        /// Gets a state by ID
        /// </summary>
        public ModelState? GetState(string stateId)
        {
            return _states.TryGetValue(stateId, out var state) ? state : null;
        }

        /// <summary>
        /// Gets a state by name
        /// </summary>
        public ModelState? GetStateByName(string name)
        {
            return _states.Values.FirstOrDefault(s => s.Name == name);
        }

        /// <summary>
        /// Starts the state machine
        /// </summary>
        public void Start(Dictionary<string, object?>? initialVariables = null)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("State machine is already running");
            }

            var initialState = _states.Values.FirstOrDefault(s => s.IsInitial);
            if (initialState == null)
            {
                throw new InvalidOperationException("No initial state defined");
            }

            _isRunning = true;
            _history.Clear();

            var context = new StateContext
            {
                CurrentState = initialState,
                Variables = initialVariables ?? new Dictionary<string, object?>()
            };

            EnterState(initialState, context);
        }

        /// <summary>
        /// Stops the state machine
        /// </summary>
        public void Stop()
        {
            if (_currentState != null && _isRunning)
            {
                var context = new StateContext { CurrentState = _currentState };
                _currentState.OnExit?.Invoke(context);
            }
            _isRunning = false;
        }

        /// <summary>
        /// Triggers an event to potentially cause a transition
        /// </summary>
        public bool TriggerEvent(string eventName, object? eventData = null, Dictionary<string, object?>? variables = null)
        {
            if (!_isRunning || _currentState == null)
            {
                return false;
            }

            var context = new StateContext
            {
                CurrentState = _currentState,
                TriggerEvent = eventName,
                TriggerData = eventData,
                Variables = variables ?? new Dictionary<string, object?>()
            };

            var possibleTransitions = _transitions
                .Where(t => t.SourceStateId == _currentState.Id && t.TriggerEvent == eventName)
                .OrderByDescending(t => t.Priority)
                .ToList();

            foreach (var transition in possibleTransitions)
            {
                if (transition.Guard == null || transition.Guard(context))
                {
                    return ExecuteTransition(transition, context);
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to find and execute an automatic transition
        /// </summary>
        public bool Step(Dictionary<string, object?>? variables = null)
        {
            if (!_isRunning || _currentState == null)
            {
                return false;
            }

            if (_currentState.IsFinal)
            {
                return false;
            }

            var context = new StateContext
            {
                CurrentState = _currentState,
                Variables = variables ?? new Dictionary<string, object?>()
            };

            // Execute current state
            _currentState.OnExecute?.Invoke(context);

            // Find automatic transitions (no trigger event)
            var possibleTransitions = _transitions
                .Where(t => t.SourceStateId == _currentState.Id && string.IsNullOrEmpty(t.TriggerEvent))
                .OrderByDescending(t => t.Priority)
                .ToList();

            foreach (var transition in possibleTransitions)
            {
                if (transition.Guard == null || transition.Guard(context))
                {
                    return ExecuteTransition(transition, context);
                }
            }

            return false;
        }

        /// <summary>
        /// Runs the state machine to completion
        /// </summary>
        public void Run(Dictionary<string, object?>? variables = null)
        {
            if (!_isRunning)
            {
                Start(variables);
            }

            while (_isRunning && _currentState != null && !_currentState.IsFinal)
            {
                if (!Step(variables))
                {
                    break;
                }
            }
        }

        private bool ExecuteTransition(StateTransition transition, StateContext context)
        {
            var targetState = _states.GetValueOrDefault(transition.TargetStateId);
            if (targetState == null)
            {
                return false;
            }

            context.TriggeringTransition = transition;

            // Exit current state
            _currentState?.OnExit?.Invoke(context);

            // Execute transition action
            transition.OnTransition?.Invoke(context);

            TransitionTriggered?.Invoke(this, new TransitionEventArgs(transition, _currentState!, targetState));

            // Record history
            _history.Add(new StateHistoryEntry
            {
                FromState = _currentState!,
                ToState = targetState,
                Transition = transition,
                Timestamp = DateTime.UtcNow
            });

            // Enter new state
            context.PreviousState = _currentState;
            EnterState(targetState, context);

            return true;
        }

        private void EnterState(ModelState state, StateContext context)
        {
            var previousState = _currentState;
            _currentState = state;
            context.CurrentState = state;

            state.OnEnter?.Invoke(context);

            StateChanged?.Invoke(this, new StateChangedEventArgs(previousState, state));
        }

        /// <summary>
        /// Resets the state machine
        /// </summary>
        public void Reset()
        {
            _isRunning = false;
            _currentState = _states.Values.FirstOrDefault(s => s.IsInitial);
            _history.Clear();
        }

        /// <summary>
        /// Gets all states
        /// </summary>
        public IEnumerable<ModelState> GetStates() => _states.Values;

        /// <summary>
        /// Gets all transitions
        /// </summary>
        public IEnumerable<StateTransition> GetTransitions() => _transitions;
    }

    /// <summary>
    /// History entry for state changes
    /// </summary>
    public class StateHistoryEntry
    {
        public ModelState FromState { get; set; } = null!;
        public ModelState ToState { get; set; } = null!;
        public StateTransition Transition { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Event args for state changes
    /// </summary>
    public class StateChangedEventArgs : EventArgs
    {
        public ModelState? PreviousState { get; }
        public ModelState NewState { get; }

        public StateChangedEventArgs(ModelState? previousState, ModelState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }

    /// <summary>
    /// Event args for transitions
    /// </summary>
    public class TransitionEventArgs : EventArgs
    {
        public StateTransition Transition { get; }
        public ModelState SourceState { get; }
        public ModelState TargetState { get; }

        public TransitionEventArgs(StateTransition transition, ModelState sourceState, ModelState targetState)
        {
            Transition = transition;
            SourceState = sourceState;
            TargetState = targetState;
        }
    }

    /// <summary>
    /// Pre-built process model state machines
    /// </summary>
    public static class ProcessModelTemplates
    {
        /// <summary>
        /// Creates a sequential process model state machine
        /// </summary>
        public static ModelStateMachine CreateSequentialModel()
        {
            var machine = new ModelStateMachine();

            var idle = machine.AddState("Idle", isInitial: true);
            var preUut = machine.AddState("PreUUT");
            var mainSequence = machine.AddState("MainSequence");
            var postUut = machine.AddState("PostUUT");
            var reportGen = machine.AddState("ReportGeneration");
            var complete = machine.AddState("Complete", isFinal: true);

            machine.AddTransitionByName("Idle", "PreUUT", "START");
            machine.AddTransitionByName("PreUUT", "MainSequence");
            machine.AddTransitionByName("MainSequence", "PostUUT");
            machine.AddTransitionByName("PostUUT", "ReportGeneration");
            machine.AddTransitionByName("ReportGeneration", "Idle", guard: ctx => 
                ctx.Variables.TryGetValue("Loop", out var loop) && loop is bool b && b);
            machine.AddTransitionByName("ReportGeneration", "Complete", guard: ctx => 
                !ctx.Variables.TryGetValue("Loop", out var loop) || loop is not bool b || !b);

            return machine;
        }

        /// <summary>
        /// Creates a batch process model state machine
        /// </summary>
        public static ModelStateMachine CreateBatchModel()
        {
            var machine = new ModelStateMachine();

            var idle = machine.AddState("Idle", isInitial: true);
            var preBatch = machine.AddState("PreBatch");
            var slotExecution = machine.AddState("SlotExecution");
            var syncPoint = machine.AddState("SyncPoint");
            var postBatch = machine.AddState("PostBatch");
            var complete = machine.AddState("Complete", isFinal: true);

            machine.AddTransitionByName("Idle", "PreBatch", "START");
            machine.AddTransitionByName("PreBatch", "SlotExecution");
            machine.AddTransitionByName("SlotExecution", "SyncPoint");
            machine.AddTransitionByName("SyncPoint", "PostBatch");
            machine.AddTransitionByName("PostBatch", "Idle", guard: ctx => 
                ctx.Variables.TryGetValue("ContinueBatch", out var cont) && cont is bool b && b);
            machine.AddTransitionByName("PostBatch", "Complete", guard: ctx => 
                !ctx.Variables.TryGetValue("ContinueBatch", out var cont) || cont is not bool b || !b);

            return machine;
        }
    }
}
