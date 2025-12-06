using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TestStandClone.Core.Callbacks
{
    /// <summary>
    /// Types of callback events.
    /// </summary>
    public enum CallbackType
    {
        /// <summary>
        /// Called before UUT test starts.
        /// </summary>
        PreUUT,

        /// <summary>
        /// Called after UUT test completes.
        /// </summary>
        PostUUT,

        /// <summary>
        /// Called before a step executes.
        /// </summary>
        PreStep,

        /// <summary>
        /// Called after a step executes.
        /// </summary>
        PostStep,

        /// <summary>
        /// Called when an error occurs.
        /// </summary>
        OnError,

        /// <summary>
        /// Called when sequence starts.
        /// </summary>
        SequenceStart,

        /// <summary>
        /// Called when sequence ends.
        /// </summary>
        SequenceEnd,

        /// <summary>
        /// Called for report generation.
        /// </summary>
        ReportGeneration,

        /// <summary>
        /// Custom user-defined callback.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Represents a callback that can be invoked during test execution.
    /// </summary>
    public class Callback : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private CallbackType _type = CallbackType.Custom;
        private bool _isEnabled = true;
        private int _priority;
        private Sequence? _callbackSequence;
        private Func<Context, Task>? _asyncAction;
        private Action<Context>? _syncAction;

        /// <summary>
        /// Name of the callback.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Type of the callback.
        /// </summary>
        public CallbackType Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Whether this callback is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Execution priority (lower values execute first).
        /// </summary>
        public int Priority
        {
            get => _priority;
            set
            {
                if (_priority != value)
                {
                    _priority = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Sequence to execute as callback.
        /// </summary>
        public Sequence? CallbackSequence
        {
            get => _callbackSequence;
            set
            {
                if (_callbackSequence != value)
                {
                    _callbackSequence = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Async action to execute as callback.
        /// </summary>
        public Func<Context, Task>? AsyncAction
        {
            get => _asyncAction;
            set
            {
                if (_asyncAction != value)
                {
                    _asyncAction = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Sync action to execute as callback.
        /// </summary>
        public Action<Context>? SyncAction
        {
            get => _syncAction;
            set
            {
                if (_syncAction != value)
                {
                    _syncAction = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Creates a new callback.
        /// </summary>
        public Callback()
        {
        }

        /// <summary>
        /// Creates a new callback with name and type.
        /// </summary>
        public Callback(string name, CallbackType type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Creates a callback with an async action.
        /// </summary>
        public Callback(string name, CallbackType type, Func<Context, Task> asyncAction) : this(name, type)
        {
            AsyncAction = asyncAction;
        }

        /// <summary>
        /// Creates a callback with a sync action.
        /// </summary>
        public Callback(string name, CallbackType type, Action<Context> syncAction) : this(name, type)
        {
            SyncAction = syncAction;
        }

        /// <summary>
        /// Executes the callback.
        /// </summary>
        public async Task ExecuteAsync(Context context)
        {
            await ExecuteAsync(context, null);
        }

        /// <summary>
        /// Executes the callback with an optional engine.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <param name="engineFactory">Optional factory to create engine for sequence callbacks.</param>
        public async Task ExecuteAsync(Context context, Func<Engine>? engineFactory)
        {
            if (!IsEnabled)
            {
                return;
            }

            // Execute callback sequence if defined
            if (CallbackSequence != null)
            {
                var engine = engineFactory?.Invoke() ?? new Engine();
                await engine.ExecuteSequenceAsync(CallbackSequence);
            }
            // Or execute async action
            else if (AsyncAction != null)
            {
                await AsyncAction(context);
            }
            // Or execute sync action
            else if (SyncAction != null)
            {
                await Task.Run(() => SyncAction(context));
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
