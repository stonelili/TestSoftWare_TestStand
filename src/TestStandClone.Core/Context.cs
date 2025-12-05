namespace TestStandClone.Core
{
    /// <summary>
    /// Context class for passing data between test steps.
    /// </summary>
    public class Context
    {
        /// <summary>
        /// Dictionary to store and share data between steps.
        /// </summary>
        public Dictionary<string, object> Data { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets a value from the context.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key to look up.</param>
        /// <returns>The value if found, or default value if not found.</returns>
        public T? GetValue<T>(string key)
        {
            if (Data.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        /// <summary>
        /// Sets a value in the context.
        /// </summary>
        /// <param name="key">The key to store the value under.</param>
        /// <param name="value">The value to store.</param>
        public void SetValue(string key, object value)
        {
            Data[key] = value;
        }
    }
}
