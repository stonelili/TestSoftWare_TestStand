using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

namespace TestStandClone.Core.Clipboard
{
    /// <summary>
    /// Clipboard content type
    /// </summary>
    public enum ClipboardContentType
    {
        Step,
        StepGroup,
        Sequence,
        Variable,
        Parameter,
        Expression,
        Mixed
    }

    /// <summary>
    /// Clipboard operation type
    /// </summary>
    public enum ClipboardOperation
    {
        Copy,
        Cut
    }

    /// <summary>
    /// Clipboard content
    /// </summary>
    public class ClipboardContent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ClipboardContentType Type { get; set; }
        public ClipboardOperation Operation { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string SourceSequenceId { get; set; } = string.Empty;
        public string SourceSequenceName { get; set; } = string.Empty;
        public List<ClipboardItem> Items { get; set; } = new List<ClipboardItem>();
        public string? SerializedData { get; set; }
    }

    /// <summary>
    /// Individual clipboard item
    /// </summary>
    public class ClipboardItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string OriginalId { get; set; } = string.Empty;
        public ClipboardContentType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public string? SerializedData { get; set; }
    }

    /// <summary>
    /// Step clipboard data
    /// </summary>
    public class StepClipboardData
    {
        public string Name { get; set; } = string.Empty;
        public string StepType { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public string? Precondition { get; set; }
        public string? PostActionOnPass { get; set; }
        public string? PostActionOnFail { get; set; }
        public string? Comment { get; set; }
    }

    /// <summary>
    /// Variable clipboard data
    /// </summary>
    public class VariableClipboardData
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public object? DefaultValue { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Clipboard history entry
    /// </summary>
    public class ClipboardHistoryEntry
    {
        public ClipboardContent Content { get; set; } = new ClipboardContent();
        public DateTime AccessTime { get; set; } = DateTime.Now;
        public int AccessCount { get; set; } = 1;
    }

    /// <summary>
    /// Clipboard manager for copy/cut/paste operations
    /// </summary>
    public class ClipboardManager
    {
        private ClipboardContent? _currentContent;
        private readonly List<ClipboardHistoryEntry> _history = new List<ClipboardHistoryEntry>();
        private readonly int _maxHistorySize;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public event EventHandler<ClipboardContent>? ContentChanged;

        public ClipboardContent? CurrentContent => _currentContent;
        public IReadOnlyList<ClipboardHistoryEntry> History => _history.AsReadOnly();
        public bool HasContent => _currentContent != null && _currentContent.Items.Count > 0;

        public ClipboardManager(int maxHistorySize = 20)
        {
            _maxHistorySize = maxHistorySize;
        }

        /// <summary>
        /// Copy steps to clipboard
        /// </summary>
        public void CopySteps(IEnumerable<StepClipboardData> steps, 
            string sourceSequenceId, string sourceSequenceName)
        {
            var content = new ClipboardContent
            {
                Type = ClipboardContentType.Step,
                Operation = ClipboardOperation.Copy,
                SourceSequenceId = sourceSequenceId,
                SourceSequenceName = sourceSequenceName
            };

            foreach (var step in steps)
            {
                content.Items.Add(new ClipboardItem
                {
                    Type = ClipboardContentType.Step,
                    Name = step.Name,
                    SerializedData = JsonSerializer.Serialize(step, _jsonOptions)
                });
            }

            SetContent(content);
        }

        /// <summary>
        /// Cut steps to clipboard
        /// </summary>
        public void CutSteps(IEnumerable<StepClipboardData> steps,
            string sourceSequenceId, string sourceSequenceName)
        {
            var content = new ClipboardContent
            {
                Type = ClipboardContentType.Step,
                Operation = ClipboardOperation.Cut,
                SourceSequenceId = sourceSequenceId,
                SourceSequenceName = sourceSequenceName
            };

            foreach (var step in steps)
            {
                content.Items.Add(new ClipboardItem
                {
                    Type = ClipboardContentType.Step,
                    Name = step.Name,
                    SerializedData = JsonSerializer.Serialize(step, _jsonOptions)
                });
            }

            SetContent(content);
        }

        /// <summary>
        /// Copy variables to clipboard
        /// </summary>
        public void CopyVariables(IEnumerable<VariableClipboardData> variables,
            string sourceSequenceId, string sourceSequenceName)
        {
            var content = new ClipboardContent
            {
                Type = ClipboardContentType.Variable,
                Operation = ClipboardOperation.Copy,
                SourceSequenceId = sourceSequenceId,
                SourceSequenceName = sourceSequenceName
            };

            foreach (var variable in variables)
            {
                content.Items.Add(new ClipboardItem
                {
                    Type = ClipboardContentType.Variable,
                    Name = variable.Name,
                    SerializedData = JsonSerializer.Serialize(variable, _jsonOptions)
                });
            }

            SetContent(content);
        }

        /// <summary>
        /// Get steps from clipboard
        /// </summary>
        public List<StepClipboardData> GetSteps()
        {
            if (_currentContent == null || _currentContent.Type != ClipboardContentType.Step)
                return new List<StepClipboardData>();

            var steps = new List<StepClipboardData>();
            foreach (var item in _currentContent.Items)
            {
                if (item.SerializedData != null)
                {
                    var step = JsonSerializer.Deserialize<StepClipboardData>(item.SerializedData, _jsonOptions);
                    if (step != null)
                    {
                        // Generate new name for paste
                        step.Name = GenerateNewName(step.Name);
                        steps.Add(step);
                    }
                }
            }
            return steps;
        }

        /// <summary>
        /// Get variables from clipboard
        /// </summary>
        public List<VariableClipboardData> GetVariables()
        {
            if (_currentContent == null || _currentContent.Type != ClipboardContentType.Variable)
                return new List<VariableClipboardData>();

            var variables = new List<VariableClipboardData>();
            foreach (var item in _currentContent.Items)
            {
                if (item.SerializedData != null)
                {
                    var variable = JsonSerializer.Deserialize<VariableClipboardData>(item.SerializedData, _jsonOptions);
                    if (variable != null)
                    {
                        variable.Name = GenerateNewName(variable.Name);
                        variables.Add(variable);
                    }
                }
            }
            return variables;
        }

        /// <summary>
        /// Check if clipboard can paste to target
        /// </summary>
        public bool CanPaste(ClipboardContentType targetType)
        {
            if (_currentContent == null) return false;
            return _currentContent.Type == targetType;
        }

        /// <summary>
        /// Clear clipboard
        /// </summary>
        public void Clear()
        {
            _currentContent = null;
            ContentChanged?.Invoke(this, null!);
        }

        /// <summary>
        /// Restore from history
        /// </summary>
        public bool RestoreFromHistory(int index)
        {
            if (index < 0 || index >= _history.Count) return false;

            var entry = _history[index];
            _currentContent = entry.Content;
            entry.AccessTime = DateTime.Now;
            entry.AccessCount++;

            ContentChanged?.Invoke(this, _currentContent);
            return true;
        }

        /// <summary>
        /// Clear history
        /// </summary>
        public void ClearHistory()
        {
            _history.Clear();
        }

        /// <summary>
        /// Export clipboard content to text
        /// </summary>
        public string ExportToText()
        {
            if (_currentContent == null) return string.Empty;
            return JsonSerializer.Serialize(_currentContent, _jsonOptions);
        }

        /// <summary>
        /// Import clipboard content from text
        /// </summary>
        public bool ImportFromText(string text)
        {
            try
            {
                var content = JsonSerializer.Deserialize<ClipboardContent>(text, _jsonOptions);
                if (content != null)
                {
                    SetContent(content);
                    return true;
                }
            }
            catch
            {
                // Invalid format
            }
            return false;
        }

        private void SetContent(ClipboardContent content)
        {
            _currentContent = content;

            // Add to history
            _history.Insert(0, new ClipboardHistoryEntry { Content = content });

            // Trim history
            while (_history.Count > _maxHistorySize)
            {
                _history.RemoveAt(_history.Count - 1);
            }

            ContentChanged?.Invoke(this, content);
        }

        private static string GenerateNewName(string originalName)
        {
            // Check if name already has a copy suffix
            var pattern = @"(.+)_Copy(\d*)$";
            var match = System.Text.RegularExpressions.Regex.Match(originalName, pattern);

            if (match.Success)
            {
                var baseName = match.Groups[1].Value;
                var countStr = match.Groups[2].Value;
                var count = string.IsNullOrEmpty(countStr) ? 1 : int.Parse(countStr);
                return $"{baseName}_Copy{count + 1}";
            }

            return $"{originalName}_Copy";
        }
    }

    /// <summary>
    /// Clipboard manager singleton
    /// </summary>
    public class GlobalClipboardManager
    {
        private static readonly Lazy<GlobalClipboardManager> _instance = 
            new Lazy<GlobalClipboardManager>(() => new GlobalClipboardManager());
        
        public static GlobalClipboardManager Instance => _instance.Value;

        private readonly ClipboardManager _manager = new ClipboardManager();

        private GlobalClipboardManager() { }

        public ClipboardManager Manager => _manager;

        /// <summary>
        /// Quick copy steps
        /// </summary>
        public void CopySteps(IEnumerable<StepClipboardData> steps,
            string sourceSequenceId = "", string sourceSequenceName = "")
        {
            _manager.CopySteps(steps, sourceSequenceId, sourceSequenceName);
        }

        /// <summary>
        /// Quick cut steps
        /// </summary>
        public void CutSteps(IEnumerable<StepClipboardData> steps,
            string sourceSequenceId = "", string sourceSequenceName = "")
        {
            _manager.CutSteps(steps, sourceSequenceId, sourceSequenceName);
        }

        /// <summary>
        /// Quick paste steps
        /// </summary>
        public List<StepClipboardData> PasteSteps()
        {
            return _manager.GetSteps();
        }

        /// <summary>
        /// Check if can paste
        /// </summary>
        public bool CanPaste => _manager.HasContent;
    }
}
