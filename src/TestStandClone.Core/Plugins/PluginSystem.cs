using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TestStandClone.Core.Plugins
{
    /// <summary>
    /// Plugin state enumeration
    /// </summary>
    public enum PluginState
    {
        Unloaded,
        Loaded,
        Initialized,
        Running,
        Stopped,
        Error
    }

    /// <summary>
    /// Plugin type enumeration
    /// </summary>
    public enum PluginType
    {
        StepType,
        Adapter,
        ReportGenerator,
        DataLink,
        ProcessModel,
        UIExtension,
        General
    }

    /// <summary>
    /// Interface for TestStand plugins
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Plugin unique identifier
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Plugin name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Plugin version
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Plugin description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Plugin author
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Plugin type
        /// </summary>
        PluginType Type { get; }

        /// <summary>
        /// Plugin dependencies (other plugin IDs)
        /// </summary>
        IEnumerable<string> Dependencies { get; }

        /// <summary>
        /// Initialize the plugin
        /// </summary>
        Task InitializeAsync(IPluginContext context);

        /// <summary>
        /// Start the plugin
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Stop the plugin
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Shutdown and cleanup the plugin
        /// </summary>
        Task ShutdownAsync();
    }

    /// <summary>
    /// Context provided to plugins during initialization
    /// </summary>
    public interface IPluginContext
    {
        /// <summary>
        /// Get a service by type
        /// </summary>
        T? GetService<T>() where T : class;

        /// <summary>
        /// Register a service
        /// </summary>
        void RegisterService<T>(T service) where T : class;

        /// <summary>
        /// Get plugin configuration
        /// </summary>
        IDictionary<string, object> Configuration { get; }

        /// <summary>
        /// Plugin data directory
        /// </summary>
        string DataDirectory { get; }

        /// <summary>
        /// Log a message
        /// </summary>
        void Log(string message, PluginLogLevel level = PluginLogLevel.Info);
    }

    /// <summary>
    /// Log level for plugins
    /// </summary>
    public enum PluginLogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Plugin information metadata
    /// </summary>
    public class PluginInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public PluginType Type { get; set; }
        public PluginState State { get; set; }
        public string AssemblyPath { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new();
        public DateTime LoadedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Default plugin context implementation
    /// </summary>
    public class PluginContext : IPluginContext
    {
        private readonly Dictionary<Type, object> _services = new();
        
        public IDictionary<string, object> Configuration { get; } = new Dictionary<string, object>();
        public string DataDirectory { get; set; } = string.Empty;

        public T? GetService<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var service) ? (T)service : null;
        }

        public void RegisterService<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }

        public void Log(string message, PluginLogLevel level = PluginLogLevel.Info)
        {
            Console.WriteLine($"[Plugin][{level}] {message}");
        }
    }

    /// <summary>
    /// Base class for plugins
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public virtual string Version => "1.0.0";
        public virtual string Description => string.Empty;
        public virtual string Author => string.Empty;
        public abstract PluginType Type { get; }
        public virtual IEnumerable<string> Dependencies => Enumerable.Empty<string>();

        protected IPluginContext? Context { get; private set; }

        public virtual Task InitializeAsync(IPluginContext context)
        {
            Context = context;
            return Task.CompletedTask;
        }

        public virtual Task StartAsync() => Task.CompletedTask;
        public virtual Task StopAsync() => Task.CompletedTask;
        public virtual Task ShutdownAsync() => Task.CompletedTask;
    }

    /// <summary>
    /// Plugin manager for loading and managing plugins
    /// </summary>
    public class PluginManager
    {
        private static readonly Lazy<PluginManager> _instance = new(() => new PluginManager());
        public static PluginManager Instance => _instance.Value;

        private readonly Dictionary<string, IPlugin> _plugins = new();
        private readonly Dictionary<string, PluginInfo> _pluginInfos = new();
        private readonly PluginContext _context = new();
        private readonly object _lock = new();

        public event EventHandler<PluginEventArgs>? PluginLoaded;
        public event EventHandler<PluginEventArgs>? PluginUnloaded;
        public event EventHandler<PluginEventArgs>? PluginStateChanged;
        public event EventHandler<PluginErrorEventArgs>? PluginError;

        private PluginManager()
        {
            _context.DataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TestStandClone", "Plugins");
            Directory.CreateDirectory(_context.DataDirectory);
        }

        /// <summary>
        /// Get all loaded plugins
        /// </summary>
        public IEnumerable<PluginInfo> Plugins
        {
            get
            {
                lock (_lock)
                {
                    return _pluginInfos.Values.ToList();
                }
            }
        }

        /// <summary>
        /// Get plugins by type
        /// </summary>
        public IEnumerable<PluginInfo> GetPluginsByType(PluginType type)
        {
            lock (_lock)
            {
                return _pluginInfos.Values.Where(p => p.Type == type).ToList();
            }
        }

        /// <summary>
        /// Load plugins from a directory
        /// </summary>
        public async Task<int> LoadPluginsFromDirectoryAsync(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            var loadedCount = 0;
            var dllFiles = Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories);

            foreach (var dllFile in dllFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dllFile);
                    var pluginTypes = assembly.GetTypes()
                        .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var pluginType in pluginTypes)
                    {
                        if (await LoadPluginAsync(pluginType, dllFile))
                        {
                            loadedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    PluginError?.Invoke(this, new PluginErrorEventArgs
                    {
                        AssemblyPath = dllFile,
                        Error = ex
                    });
                }
            }

            return loadedCount;
        }

        /// <summary>
        /// Load a plugin from a type
        /// </summary>
        public async Task<bool> LoadPluginAsync(Type pluginType, string assemblyPath = "")
        {
            try
            {
                if (Activator.CreateInstance(pluginType) is not IPlugin plugin)
                {
                    return false;
                }

                lock (_lock)
                {
                    if (_plugins.ContainsKey(plugin.Id))
                    {
                        return false;
                    }
                }

                // Check dependencies
                foreach (var depId in plugin.Dependencies)
                {
                    if (!_plugins.ContainsKey(depId))
                    {
                        throw new InvalidOperationException($"Missing dependency: {depId}");
                    }
                }

                var info = new PluginInfo
                {
                    Id = plugin.Id,
                    Name = plugin.Name,
                    Version = plugin.Version,
                    Description = plugin.Description,
                    Author = plugin.Author,
                    Type = plugin.Type,
                    State = PluginState.Loaded,
                    AssemblyPath = assemblyPath,
                    Dependencies = plugin.Dependencies.ToList(),
                    LoadedAt = DateTime.UtcNow
                };

                lock (_lock)
                {
                    _plugins[plugin.Id] = plugin;
                    _pluginInfos[plugin.Id] = info;
                }

                PluginLoaded?.Invoke(this, new PluginEventArgs { PluginInfo = info });

                return true;
            }
            catch (Exception ex)
            {
                PluginError?.Invoke(this, new PluginErrorEventArgs
                {
                    AssemblyPath = assemblyPath,
                    Error = ex
                });
                return false;
            }
        }

        /// <summary>
        /// Initialize a plugin
        /// </summary>
        public async Task<bool> InitializePluginAsync(string pluginId)
        {
            IPlugin? plugin;
            PluginInfo? info;

            lock (_lock)
            {
                if (!_plugins.TryGetValue(pluginId, out plugin) ||
                    !_pluginInfos.TryGetValue(pluginId, out info))
                {
                    return false;
                }
            }

            try
            {
                await plugin.InitializeAsync(_context);
                info.State = PluginState.Initialized;
                PluginStateChanged?.Invoke(this, new PluginEventArgs { PluginInfo = info });
                return true;
            }
            catch (Exception ex)
            {
                info.State = PluginState.Error;
                info.ErrorMessage = ex.Message;
                PluginError?.Invoke(this, new PluginErrorEventArgs
                {
                    PluginInfo = info,
                    Error = ex
                });
                return false;
            }
        }

        /// <summary>
        /// Start a plugin
        /// </summary>
        public async Task<bool> StartPluginAsync(string pluginId)
        {
            IPlugin? plugin;
            PluginInfo? info;

            lock (_lock)
            {
                if (!_plugins.TryGetValue(pluginId, out plugin) ||
                    !_pluginInfos.TryGetValue(pluginId, out info))
                {
                    return false;
                }
            }

            if (info.State != PluginState.Initialized && info.State != PluginState.Stopped)
            {
                return false;
            }

            try
            {
                await plugin.StartAsync();
                info.State = PluginState.Running;
                PluginStateChanged?.Invoke(this, new PluginEventArgs { PluginInfo = info });
                return true;
            }
            catch (Exception ex)
            {
                info.State = PluginState.Error;
                info.ErrorMessage = ex.Message;
                PluginError?.Invoke(this, new PluginErrorEventArgs
                {
                    PluginInfo = info,
                    Error = ex
                });
                return false;
            }
        }

        /// <summary>
        /// Stop a plugin
        /// </summary>
        public async Task<bool> StopPluginAsync(string pluginId)
        {
            IPlugin? plugin;
            PluginInfo? info;

            lock (_lock)
            {
                if (!_plugins.TryGetValue(pluginId, out plugin) ||
                    !_pluginInfos.TryGetValue(pluginId, out info))
                {
                    return false;
                }
            }

            if (info.State != PluginState.Running)
            {
                return false;
            }

            try
            {
                await plugin.StopAsync();
                info.State = PluginState.Stopped;
                PluginStateChanged?.Invoke(this, new PluginEventArgs { PluginInfo = info });
                return true;
            }
            catch (Exception ex)
            {
                info.State = PluginState.Error;
                info.ErrorMessage = ex.Message;
                PluginError?.Invoke(this, new PluginErrorEventArgs
                {
                    PluginInfo = info,
                    Error = ex
                });
                return false;
            }
        }

        /// <summary>
        /// Unload a plugin
        /// </summary>
        public async Task<bool> UnloadPluginAsync(string pluginId)
        {
            IPlugin? plugin;
            PluginInfo? info;

            lock (_lock)
            {
                if (!_plugins.TryGetValue(pluginId, out plugin) ||
                    !_pluginInfos.TryGetValue(pluginId, out info))
                {
                    return false;
                }
            }

            try
            {
                if (info.State == PluginState.Running)
                {
                    await plugin.StopAsync();
                }

                await plugin.ShutdownAsync();

                lock (_lock)
                {
                    _plugins.Remove(pluginId);
                    _pluginInfos.Remove(pluginId);
                }

                info.State = PluginState.Unloaded;
                PluginUnloaded?.Invoke(this, new PluginEventArgs { PluginInfo = info });
                return true;
            }
            catch (Exception ex)
            {
                PluginError?.Invoke(this, new PluginErrorEventArgs
                {
                    PluginInfo = info,
                    Error = ex
                });
                return false;
            }
        }

        /// <summary>
        /// Get a plugin instance
        /// </summary>
        public T? GetPlugin<T>(string pluginId) where T : class, IPlugin
        {
            lock (_lock)
            {
                return _plugins.TryGetValue(pluginId, out var plugin) ? plugin as T : null;
            }
        }

        /// <summary>
        /// Register a service in the context
        /// </summary>
        public void RegisterService<T>(T service) where T : class
        {
            _context.RegisterService(service);
        }

        /// <summary>
        /// Initialize all loaded plugins
        /// </summary>
        public async Task InitializeAllAsync()
        {
            List<string> pluginIds;
            lock (_lock)
            {
                pluginIds = _plugins.Keys.ToList();
            }

            foreach (var id in pluginIds)
            {
                await InitializePluginAsync(id);
            }
        }

        /// <summary>
        /// Start all initialized plugins
        /// </summary>
        public async Task StartAllAsync()
        {
            List<PluginInfo> infos;
            lock (_lock)
            {
                infos = _pluginInfos.Values
                    .Where(p => p.State == PluginState.Initialized)
                    .ToList();
            }

            foreach (var info in infos)
            {
                await StartPluginAsync(info.Id);
            }
        }

        /// <summary>
        /// Stop all running plugins
        /// </summary>
        public async Task StopAllAsync()
        {
            List<PluginInfo> infos;
            lock (_lock)
            {
                infos = _pluginInfos.Values
                    .Where(p => p.State == PluginState.Running)
                    .ToList();
            }

            foreach (var info in infos)
            {
                await StopPluginAsync(info.Id);
            }
        }

        /// <summary>
        /// Unload all plugins
        /// </summary>
        public async Task UnloadAllAsync()
        {
            await StopAllAsync();

            List<string> pluginIds;
            lock (_lock)
            {
                pluginIds = _plugins.Keys.ToList();
            }

            foreach (var id in pluginIds)
            {
                await UnloadPluginAsync(id);
            }
        }
    }

    /// <summary>
    /// Plugin event arguments
    /// </summary>
    public class PluginEventArgs : EventArgs
    {
        public PluginInfo PluginInfo { get; set; } = new();
    }

    /// <summary>
    /// Plugin error event arguments
    /// </summary>
    public class PluginErrorEventArgs : EventArgs
    {
        public PluginInfo? PluginInfo { get; set; }
        public string AssemblyPath { get; set; } = string.Empty;
        public Exception? Error { get; set; }
    }
}
