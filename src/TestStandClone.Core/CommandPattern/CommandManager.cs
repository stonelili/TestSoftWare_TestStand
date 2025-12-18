using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.CommandPattern
{
    #region Command Pattern Classes

    /// <summary>
    /// Command execution result
    /// </summary>
    public class CommandResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
        public Exception? Error { get; set; }

        public static CommandResult Ok(string? message = null, object? data = null) =>
            new() { Success = true, Message = message, Data = data };

        public static CommandResult Fail(string message, Exception? error = null) =>
            new() { Success = false, Message = message, Error = error };
    }

    /// <summary>
    /// Command interface
    /// </summary>
    public interface ICommand
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        bool CanExecute { get; }
        bool CanUndo { get; }
        CommandResult Execute();
        CommandResult Undo();
    }

    /// <summary>
    /// Base command implementation
    /// </summary>
    public abstract class CommandBase : ICommand
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public abstract string Name { get; }
        public virtual string Description => Name;
        public virtual bool CanExecute => true;
        public virtual bool CanUndo => false;

        public abstract CommandResult Execute();
        public virtual CommandResult Undo() => CommandResult.Fail("Undo not supported");
    }

    /// <summary>
    /// Composite command for executing multiple commands
    /// </summary>
    public class CompositeCommand : CommandBase
    {
        private readonly List<ICommand> _commands = new();
        private readonly List<ICommand> _executedCommands = new();

        public override string Name => "Composite Command";
        public override bool CanUndo => _executedCommands.All(c => c.CanUndo);

        public void AddCommand(ICommand command)
        {
            _commands.Add(command);
        }

        public void RemoveCommand(string commandId)
        {
            _commands.RemoveAll(c => c.Id == commandId);
        }

        public override CommandResult Execute()
        {
            _executedCommands.Clear();

            foreach (var command in _commands)
            {
                if (!command.CanExecute)
                    continue;

                var result = command.Execute();
                if (!result.Success)
                {
                    // Rollback executed commands
                    foreach (var executed in _executedCommands.AsEnumerable().Reverse())
                    {
                        if (executed.CanUndo)
                            executed.Undo();
                    }
                    return result;
                }

                _executedCommands.Add(command);
            }

            return CommandResult.Ok($"Executed {_executedCommands.Count} commands");
        }

        public override CommandResult Undo()
        {
            foreach (var command in _executedCommands.AsEnumerable().Reverse())
            {
                if (command.CanUndo)
                {
                    var result = command.Undo();
                    if (!result.Success)
                        return result;
                }
            }

            _executedCommands.Clear();
            return CommandResult.Ok("Undone all commands");
        }
    }

    /// <summary>
    /// Action-based command
    /// </summary>
    public class ActionCommand : CommandBase
    {
        private readonly Action _execute;
        private readonly Action? _undo;
        private readonly Func<bool>? _canExecute;

        public override string Name { get; }
        public override bool CanExecute => _canExecute?.Invoke() ?? true;
        public override bool CanUndo => _undo != null;

        public ActionCommand(string name, Action execute, Action? undo = null, Func<bool>? canExecute = null)
        {
            Name = name;
            _execute = execute;
            _undo = undo;
            _canExecute = canExecute;
        }

        public override CommandResult Execute()
        {
            try
            {
                _execute();
                return CommandResult.Ok();
            }
            catch (Exception ex)
            {
                return CommandResult.Fail(ex.Message, ex);
            }
        }

        public override CommandResult Undo()
        {
            if (_undo == null)
                return CommandResult.Fail("Undo not supported");

            try
            {
                _undo();
                return CommandResult.Ok();
            }
            catch (Exception ex)
            {
                return CommandResult.Fail(ex.Message, ex);
            }
        }
    }

    /// <summary>
    /// Command history entry
    /// </summary>
    public class CommandHistoryEntry
    {
        public ICommand Command { get; set; } = null!;
        public DateTime ExecutedAt { get; set; } = DateTime.Now;
        public CommandResult Result { get; set; } = null!;
        public bool WasUndone { get; set; }
    }

    /// <summary>
    /// Command manager with undo/redo support
    /// </summary>
    public class CommandManager
    {
        private static readonly Lazy<CommandManager> _instance = new(() => new CommandManager());
        public static CommandManager Instance => _instance.Value;

        private readonly Stack<CommandHistoryEntry> _undoStack = new();
        private readonly Stack<CommandHistoryEntry> _redoStack = new();
        private readonly List<CommandHistoryEntry> _history = new();
        private readonly Dictionary<string, ICommand> _registeredCommands = new();
        private const int MaxHistorySize = 100;

        public event EventHandler<CommandHistoryEntry>? CommandExecuted;
        public event EventHandler<CommandHistoryEntry>? CommandUndone;
        public event EventHandler<CommandHistoryEntry>? CommandRedone;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public IReadOnlyList<CommandHistoryEntry> History => _history.AsReadOnly();

        public void RegisterCommand(string key, ICommand command)
        {
            _registeredCommands[key] = command;
        }

        public void UnregisterCommand(string key)
        {
            _registeredCommands.Remove(key);
        }

        public ICommand? GetCommand(string key)
        {
            return _registeredCommands.TryGetValue(key, out var command) ? command : null;
        }

        public CommandResult Execute(ICommand command)
        {
            if (!command.CanExecute)
                return CommandResult.Fail("Command cannot be executed");

            var result = command.Execute();
            
            var entry = new CommandHistoryEntry
            {
                Command = command,
                Result = result
            };

            if (result.Success && command.CanUndo)
            {
                _undoStack.Push(entry);
                _redoStack.Clear();
            }

            AddToHistory(entry);
            CommandExecuted?.Invoke(this, entry);

            return result;
        }

        public CommandResult ExecuteRegistered(string key)
        {
            var command = GetCommand(key);
            if (command == null)
                return CommandResult.Fail($"Command '{key}' not found");

            return Execute(command);
        }

        public CommandResult Undo()
        {
            if (!CanUndo)
                return CommandResult.Fail("Nothing to undo");

            var entry = _undoStack.Pop();
            var result = entry.Command.Undo();

            if (result.Success)
            {
                entry.WasUndone = true;
                _redoStack.Push(entry);
                CommandUndone?.Invoke(this, entry);
            }
            else
            {
                _undoStack.Push(entry);
            }

            return result;
        }

        public CommandResult Redo()
        {
            if (!CanRedo)
                return CommandResult.Fail("Nothing to redo");

            var entry = _redoStack.Pop();
            var result = entry.Command.Execute();

            if (result.Success)
            {
                entry.WasUndone = false;
                _undoStack.Push(entry);
                CommandRedone?.Invoke(this, entry);
            }
            else
            {
                _redoStack.Push(entry);
            }

            return result;
        }

        public void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _history.Clear();
        }

        private void AddToHistory(CommandHistoryEntry entry)
        {
            _history.Add(entry);
            if (_history.Count > MaxHistorySize)
                _history.RemoveAt(0);
        }
    }

    #endregion
}
