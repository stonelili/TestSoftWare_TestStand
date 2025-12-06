using System.Collections.ObjectModel;
using TestStandClone.Core.Steps;

namespace TestStandClone.Core
{
    /// <summary>
    /// Provides editing operations for sequences.
    /// Similar to TestStand's sequence editing capabilities.
    /// </summary>
    public class SequenceEditor
    {
        private readonly Sequence _sequence;
        private readonly Stack<EditorAction> _undoStack;
        private readonly Stack<EditorAction> _redoStack;

        /// <summary>
        /// Event raised when the sequence is modified.
        /// </summary>
        public event EventHandler? SequenceModified;

        /// <summary>
        /// Gets whether undo is available.
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// Gets whether redo is available.
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// Gets the sequence being edited.
        /// </summary>
        public Sequence Sequence => _sequence;

        /// <summary>
        /// Creates a new SequenceEditor.
        /// </summary>
        /// <param name="sequence">The sequence to edit.</param>
        public SequenceEditor(Sequence sequence)
        {
            _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            _undoStack = new Stack<EditorAction>();
            _redoStack = new Stack<EditorAction>();
        }

        /// <summary>
        /// Adds a step at the specified index.
        /// </summary>
        /// <param name="step">The step to add.</param>
        /// <param name="index">The index to insert at.</param>
        /// <param name="group">The step group.</param>
        public void AddStep(TestStep step, int index = -1, StepGroup group = StepGroup.Main)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            var steps = GetStepCollection(group);
            var actualIndex = index < 0 || index > steps.Count ? steps.Count : index;

            steps.Insert(actualIndex, step);
            _sequence.SyncStepsCollection();

            RecordAction(new AddStepAction(step, actualIndex, group));
            OnSequenceModified();
        }

        /// <summary>
        /// Removes a step.
        /// </summary>
        /// <param name="step">The step to remove.</param>
        public void RemoveStep(TestStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            var (group, index) = FindStep(step);
            if (index >= 0)
            {
                var steps = GetStepCollection(group);
                steps.RemoveAt(index);
                _sequence.SyncStepsCollection();

                RecordAction(new RemoveStepAction(step, index, group));
                OnSequenceModified();
            }
        }

        /// <summary>
        /// Moves a step up in the sequence.
        /// </summary>
        /// <param name="step">The step to move.</param>
        public void MoveStepUp(TestStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            var (group, index) = FindStep(step);
            if (index > 0)
            {
                var steps = GetStepCollection(group);
                steps.Move(index, index - 1);
                _sequence.SyncStepsCollection();

                RecordAction(new MoveStepAction(step, index, index - 1, group));
                OnSequenceModified();
            }
        }

        /// <summary>
        /// Moves a step down in the sequence.
        /// </summary>
        /// <param name="step">The step to move.</param>
        public void MoveStepDown(TestStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            var (group, index) = FindStep(step);
            var steps = GetStepCollection(group);
            if (index >= 0 && index < steps.Count - 1)
            {
                steps.Move(index, index + 1);
                _sequence.SyncStepsCollection();

                RecordAction(new MoveStepAction(step, index, index + 1, group));
                OnSequenceModified();
            }
        }

        /// <summary>
        /// Duplicates a step.
        /// </summary>
        /// <param name="step">The step to duplicate.</param>
        /// <returns>The duplicated step.</returns>
        public TestStep DuplicateStep(TestStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            var (group, index) = FindStep(step);
            if (index >= 0)
            {
                var clone = CloneStep(step);
                clone.Name = $"{step.Name}_Copy";
                
                var steps = GetStepCollection(group);
                steps.Insert(index + 1, clone);
                _sequence.SyncStepsCollection();

                RecordAction(new AddStepAction(clone, index + 1, group));
                OnSequenceModified();
                
                return clone;
            }

            return step;
        }

        /// <summary>
        /// Undoes the last action.
        /// </summary>
        public void Undo()
        {
            if (!CanUndo)
            {
                return;
            }

            var action = _undoStack.Pop();
            action.Undo(this);
            _redoStack.Push(action);
            OnSequenceModified();
        }

        /// <summary>
        /// Redoes the last undone action.
        /// </summary>
        public void Redo()
        {
            if (!CanRedo)
            {
                return;
            }

            var action = _redoStack.Pop();
            action.Redo(this);
            _undoStack.Push(action);
            OnSequenceModified();
        }

        /// <summary>
        /// Creates a step of the specified type.
        /// </summary>
        /// <param name="stepType">Type of step to create.</param>
        /// <param name="name">Step name.</param>
        /// <returns>The created step.</returns>
        public static TestStep CreateStep(Type stepType, string name)
        {
            if (stepType == null)
            {
                throw new ArgumentNullException(nameof(stepType));
            }

            if (!typeof(TestStep).IsAssignableFrom(stepType))
            {
                throw new ArgumentException($"Type must inherit from TestStep: {stepType.Name}");
            }

            // Try to find a constructor that takes just a name
            var constructor = stepType.GetConstructor(new[] { typeof(string) });
            if (constructor != null)
            {
                return (TestStep)constructor.Invoke(new object[] { name });
            }

            // Try parameterless constructor
            constructor = stepType.GetConstructor(Type.EmptyTypes);
            if (constructor != null)
            {
                var step = (TestStep)constructor.Invoke(null);
                step.Name = name;
                return step;
            }

            throw new InvalidOperationException($"Cannot create step of type {stepType.Name}");
        }

        /// <summary>
        /// Gets all available step types.
        /// </summary>
        /// <returns>List of step types.</returns>
        public static IReadOnlyList<Type> GetAvailableStepTypes()
        {
            return new List<Type>
            {
                typeof(DelayStep),
                typeof(NumericLimitStep),
                typeof(PassFailStep),
                typeof(StringValueStep),
                typeof(ActionStep),
                typeof(MessagePopupStep),
                typeof(LabelStep),
                typeof(GotoStep),
                typeof(LoopStep),
                typeof(SequenceCallStep),
                typeof(InstrumentStep),
                typeof(InstrumentMeasureStep),
                typeof(InstrumentIdentifyStep),
                typeof(CodeModuleStep)
            }.AsReadOnly();
        }

        /// <summary>
        /// Gets a friendly name for a step type.
        /// </summary>
        public static string GetStepTypeName(Type stepType)
        {
            return stepType.Name.Replace("Step", " Step");
        }

        /// <summary>
        /// Finds a step in the sequence.
        /// </summary>
        private (StepGroup Group, int Index) FindStep(TestStep step)
        {
            int index = _sequence.SetupSteps.IndexOf(step);
            if (index >= 0) return (StepGroup.Setup, index);

            index = _sequence.MainSteps.IndexOf(step);
            if (index >= 0) return (StepGroup.Main, index);

            index = _sequence.CleanupSteps.IndexOf(step);
            if (index >= 0) return (StepGroup.Cleanup, index);

            return (StepGroup.Main, -1);
        }

        /// <summary>
        /// Gets the step collection for a group.
        /// </summary>
        private ObservableCollection<TestStep> GetStepCollection(StepGroup group)
        {
            return group switch
            {
                StepGroup.Setup => _sequence.SetupSteps,
                StepGroup.Cleanup => _sequence.CleanupSteps,
                _ => _sequence.MainSteps
            };
        }

        /// <summary>
        /// Records an action for undo.
        /// </summary>
        private void RecordAction(EditorAction action)
        {
            _undoStack.Push(action);
            _redoStack.Clear();
        }

        /// <summary>
        /// Raises the SequenceModified event.
        /// </summary>
        private void OnSequenceModified()
        {
            SequenceModified?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Clones a step.
        /// </summary>
        private static TestStep CloneStep(TestStep step)
        {
            // Simple clone - create new instance and copy properties
            var stepType = step.GetType();
            var clone = CreateStep(stepType, step.Name);
            
            // Copy common properties
            clone.IsEnabled = step.IsEnabled;
            clone.HasBreakpoint = step.HasBreakpoint;
            clone.Precondition = step.Precondition;
            clone.Comment = step.Comment;
            
            return clone;
        }
    }

    /// <summary>
    /// Step group enumeration.
    /// </summary>
    public enum StepGroup
    {
        Setup,
        Main,
        Cleanup
    }

    /// <summary>
    /// Base class for editor actions.
    /// </summary>
    internal abstract class EditorAction
    {
        public abstract void Undo(SequenceEditor editor);
        public abstract void Redo(SequenceEditor editor);
    }

    /// <summary>
    /// Action for adding a step.
    /// </summary>
    internal class AddStepAction : EditorAction
    {
        private readonly TestStep _step;
        private readonly int _index;
        private readonly StepGroup _group;

        public AddStepAction(TestStep step, int index, StepGroup group)
        {
            _step = step;
            _index = index;
            _group = group;
        }

        public override void Undo(SequenceEditor editor)
        {
            editor.RemoveStep(_step);
        }

        public override void Redo(SequenceEditor editor)
        {
            editor.AddStep(_step, _index, _group);
        }
    }

    /// <summary>
    /// Action for removing a step.
    /// </summary>
    internal class RemoveStepAction : EditorAction
    {
        private readonly TestStep _step;
        private readonly int _index;
        private readonly StepGroup _group;

        public RemoveStepAction(TestStep step, int index, StepGroup group)
        {
            _step = step;
            _index = index;
            _group = group;
        }

        public override void Undo(SequenceEditor editor)
        {
            editor.AddStep(_step, _index, _group);
        }

        public override void Redo(SequenceEditor editor)
        {
            editor.RemoveStep(_step);
        }
    }

    /// <summary>
    /// Action for moving a step.
    /// </summary>
    internal class MoveStepAction : EditorAction
    {
        private readonly TestStep _step;
        private readonly int _fromIndex;
        private readonly int _toIndex;
        private readonly StepGroup _group;

        public MoveStepAction(TestStep step, int fromIndex, int toIndex, StepGroup group)
        {
            _step = step;
            _fromIndex = fromIndex;
            _toIndex = toIndex;
            _group = group;
        }

        public override void Undo(SequenceEditor editor)
        {
            if (_toIndex > _fromIndex)
            {
                editor.MoveStepUp(_step);
            }
            else
            {
                editor.MoveStepDown(_step);
            }
        }

        public override void Redo(SequenceEditor editor)
        {
            if (_toIndex > _fromIndex)
            {
                editor.MoveStepDown(_step);
            }
            else
            {
                editor.MoveStepUp(_step);
            }
        }
    }
}
