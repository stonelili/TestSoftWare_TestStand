using System.Collections.ObjectModel;

namespace TestStandClone.Core.Callbacks
{
    /// <summary>
    /// Manages callbacks for test execution.
    /// </summary>
    public class CallbackManager
    {
        private readonly ObservableCollection<Callback> _callbacks = new ObservableCollection<Callback>();

        /// <summary>
        /// All registered callbacks.
        /// </summary>
        public ObservableCollection<Callback> Callbacks => _callbacks;

        /// <summary>
        /// Registers a new callback.
        /// </summary>
        public void Register(Callback callback)
        {
            _callbacks.Add(callback);
        }

        /// <summary>
        /// Registers a new callback with action.
        /// </summary>
        public void Register(string name, CallbackType type, Func<Context, Task> action, int priority = 0)
        {
            _callbacks.Add(new Callback(name, type, action) { Priority = priority });
        }

        /// <summary>
        /// Registers a new callback with sync action.
        /// </summary>
        public void Register(string name, CallbackType type, Action<Context> action, int priority = 0)
        {
            _callbacks.Add(new Callback(name, type, action) { Priority = priority });
        }

        /// <summary>
        /// Unregisters a callback by name.
        /// </summary>
        public bool Unregister(string name)
        {
            var callback = _callbacks.FirstOrDefault(c => c.Name == name);
            if (callback != null)
            {
                return _callbacks.Remove(callback);
            }
            return false;
        }

        /// <summary>
        /// Gets all callbacks of a specific type.
        /// </summary>
        public IEnumerable<Callback> GetCallbacks(CallbackType type)
        {
            return _callbacks
                .Where(c => c.Type == type && c.IsEnabled)
                .OrderBy(c => c.Priority);
        }

        /// <summary>
        /// Executes all callbacks of a specific type.
        /// </summary>
        public async Task ExecuteCallbacksAsync(CallbackType type, Context context)
        {
            foreach (var callback in GetCallbacks(type))
            {
                await callback.ExecuteAsync(context);
            }
        }

        /// <summary>
        /// Clears all callbacks.
        /// </summary>
        public void Clear()
        {
            _callbacks.Clear();
        }

        /// <summary>
        /// Enables or disables all callbacks of a type.
        /// </summary>
        public void SetEnabled(CallbackType type, bool enabled)
        {
            foreach (var callback in _callbacks.Where(c => c.Type == type))
            {
                callback.IsEnabled = enabled;
            }
        }
    }
}
