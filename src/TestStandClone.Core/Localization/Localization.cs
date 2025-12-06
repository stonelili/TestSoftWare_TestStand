// =============================================================================
// Localization.cs - Multi-language string resources
// Part of TestStandClone.Core - TestStand-like test automation framework
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace TestStandClone.Core.Localization
{
    /// <summary>
    /// Localization manager for multi-language support.
    /// Similar to TestStand's localization capabilities.
    /// </summary>
    public class LocalizationManager
    {
        private static LocalizationManager? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, Dictionary<string, string>> _resources = new();
        private string _currentLanguage = "en-US";

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static LocalizationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new LocalizationManager();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Gets or sets the current language.
        /// </summary>
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    LanguageChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// Gets the available languages.
        /// </summary>
        public IEnumerable<string> AvailableLanguages => _resources.Keys;

        /// <summary>
        /// Event raised when language changes.
        /// </summary>
        public event EventHandler<string>? LanguageChanged;

        private LocalizationManager()
        {
            // Initialize default English strings
            InitializeDefaultStrings();
        }

        private void InitializeDefaultStrings()
        {
            var english = new Dictionary<string, string>
            {
                // General UI
                ["App.Title"] = "TestStand Clone",
                ["App.Ready"] = "Ready",
                ["App.Running"] = "Running",
                ["App.Paused"] = "Paused",
                ["App.Completed"] = "Completed",
                ["App.Aborted"] = "Aborted",
                ["App.Error"] = "Error",

                // File Menu
                ["Menu.File"] = "File",
                ["Menu.File.New"] = "New",
                ["Menu.File.Open"] = "Open...",
                ["Menu.File.Save"] = "Save",
                ["Menu.File.SaveAs"] = "Save As...",
                ["Menu.File.Exit"] = "Exit",

                // Edit Menu
                ["Menu.Edit"] = "Edit",
                ["Menu.Edit.Undo"] = "Undo",
                ["Menu.Edit.Redo"] = "Redo",
                ["Menu.Edit.Cut"] = "Cut",
                ["Menu.Edit.Copy"] = "Copy",
                ["Menu.Edit.Paste"] = "Paste",
                ["Menu.Edit.Delete"] = "Delete",

                // Execute Menu
                ["Menu.Execute"] = "Execute",
                ["Menu.Execute.Run"] = "Run",
                ["Menu.Execute.Pause"] = "Pause",
                ["Menu.Execute.Resume"] = "Resume",
                ["Menu.Execute.Abort"] = "Abort",
                ["Menu.Execute.SingleStep"] = "Single Step",
                ["Menu.Execute.Reset"] = "Reset",

                // View Menu
                ["Menu.View"] = "View",
                ["Menu.View.Properties"] = "Properties",
                ["Menu.View.Variables"] = "Variables",
                ["Menu.View.Watch"] = "Watch Window",
                ["Menu.View.Output"] = "Output",

                // Toolbar
                ["Toolbar.New"] = "New",
                ["Toolbar.Open"] = "Open",
                ["Toolbar.Save"] = "Save",
                ["Toolbar.Run"] = "Run",
                ["Toolbar.Pause"] = "Pause",
                ["Toolbar.Stop"] = "Stop",
                ["Toolbar.Step"] = "Step",

                // Steps
                ["Step.Status.Idle"] = "Idle",
                ["Step.Status.Running"] = "Running",
                ["Step.Status.Passed"] = "Passed",
                ["Step.Status.Failed"] = "Failed",
                ["Step.Status.Error"] = "Error",
                ["Step.Status.Skipped"] = "Skipped",

                // Step Types
                ["StepType.Delay"] = "Delay",
                ["StepType.NumericLimit"] = "Numeric Limit Test",
                ["StepType.StringValue"] = "String Value Test",
                ["StepType.PassFail"] = "Pass/Fail Test",
                ["StepType.Action"] = "Action",
                ["StepType.SequenceCall"] = "Sequence Call",
                ["StepType.MessagePopup"] = "Message Popup",
                ["StepType.Label"] = "Label",
                ["StepType.Goto"] = "Goto",
                ["StepType.Loop"] = "Loop",
                ["StepType.If"] = "If/Then/Else",
                ["StepType.Switch"] = "Switch/Case",
                ["StepType.FileIO"] = "File I/O",

                // Properties Panel
                ["Properties.Title"] = "Properties",
                ["Properties.Name"] = "Name",
                ["Properties.Type"] = "Type",
                ["Properties.Status"] = "Status",
                ["Properties.Result"] = "Result",
                ["Properties.Description"] = "Description",

                // Dialogs
                ["Dialog.Confirm"] = "Confirm",
                ["Dialog.Warning"] = "Warning",
                ["Dialog.Error"] = "Error",
                ["Dialog.Info"] = "Information",
                ["Dialog.OK"] = "OK",
                ["Dialog.Cancel"] = "Cancel",
                ["Dialog.Yes"] = "Yes",
                ["Dialog.No"] = "No",
                ["Dialog.SaveChanges"] = "Do you want to save changes?",
                ["Dialog.ConfirmAbort"] = "Are you sure you want to abort execution?",

                // Reports
                ["Report.Title"] = "Test Report",
                ["Report.Sequence"] = "Sequence",
                ["Report.StartTime"] = "Start Time",
                ["Report.EndTime"] = "End Time",
                ["Report.Duration"] = "Duration",
                ["Report.Result"] = "Result",
                ["Report.Passed"] = "PASSED",
                ["Report.Failed"] = "FAILED",

                // Errors
                ["Error.FileNotFound"] = "File not found",
                ["Error.LoadFailed"] = "Failed to load file",
                ["Error.SaveFailed"] = "Failed to save file",
                ["Error.ExecutionFailed"] = "Execution failed",
                ["Error.InvalidStep"] = "Invalid step configuration"
            };
            _resources["en-US"] = english;

            // Chinese (Simplified)
            var chinese = new Dictionary<string, string>
            {
                // General UI
                ["App.Title"] = "TestStand 克隆版",
                ["App.Ready"] = "就绪",
                ["App.Running"] = "运行中",
                ["App.Paused"] = "已暂停",
                ["App.Completed"] = "已完成",
                ["App.Aborted"] = "已中止",
                ["App.Error"] = "错误",

                // File Menu
                ["Menu.File"] = "文件",
                ["Menu.File.New"] = "新建",
                ["Menu.File.Open"] = "打开...",
                ["Menu.File.Save"] = "保存",
                ["Menu.File.SaveAs"] = "另存为...",
                ["Menu.File.Exit"] = "退出",

                // Edit Menu
                ["Menu.Edit"] = "编辑",
                ["Menu.Edit.Undo"] = "撤销",
                ["Menu.Edit.Redo"] = "重做",
                ["Menu.Edit.Cut"] = "剪切",
                ["Menu.Edit.Copy"] = "复制",
                ["Menu.Edit.Paste"] = "粘贴",
                ["Menu.Edit.Delete"] = "删除",

                // Execute Menu
                ["Menu.Execute"] = "执行",
                ["Menu.Execute.Run"] = "运行",
                ["Menu.Execute.Pause"] = "暂停",
                ["Menu.Execute.Resume"] = "继续",
                ["Menu.Execute.Abort"] = "中止",
                ["Menu.Execute.SingleStep"] = "单步执行",
                ["Menu.Execute.Reset"] = "重置",

                // View Menu
                ["Menu.View"] = "视图",
                ["Menu.View.Properties"] = "属性",
                ["Menu.View.Variables"] = "变量",
                ["Menu.View.Watch"] = "监视窗口",
                ["Menu.View.Output"] = "输出",

                // Steps
                ["Step.Status.Idle"] = "空闲",
                ["Step.Status.Running"] = "运行中",
                ["Step.Status.Passed"] = "通过",
                ["Step.Status.Failed"] = "失败",
                ["Step.Status.Error"] = "错误",
                ["Step.Status.Skipped"] = "已跳过",

                // Step Types
                ["StepType.Delay"] = "延迟",
                ["StepType.NumericLimit"] = "数值限制测试",
                ["StepType.StringValue"] = "字符串值测试",
                ["StepType.PassFail"] = "通过/失败测试",
                ["StepType.Action"] = "动作",
                ["StepType.SequenceCall"] = "序列调用",
                ["StepType.MessagePopup"] = "消息弹窗",

                // Properties Panel
                ["Properties.Title"] = "属性",
                ["Properties.Name"] = "名称",
                ["Properties.Type"] = "类型",
                ["Properties.Status"] = "状态",
                ["Properties.Result"] = "结果",
                ["Properties.Description"] = "描述",

                // Dialogs
                ["Dialog.Confirm"] = "确认",
                ["Dialog.Warning"] = "警告",
                ["Dialog.Error"] = "错误",
                ["Dialog.Info"] = "信息",
                ["Dialog.OK"] = "确定",
                ["Dialog.Cancel"] = "取消",
                ["Dialog.Yes"] = "是",
                ["Dialog.No"] = "否",
                ["Dialog.SaveChanges"] = "是否保存更改？",
                ["Dialog.ConfirmAbort"] = "确定要中止执行吗？",

                // Reports
                ["Report.Title"] = "测试报告",
                ["Report.Sequence"] = "序列",
                ["Report.StartTime"] = "开始时间",
                ["Report.EndTime"] = "结束时间",
                ["Report.Duration"] = "持续时间",
                ["Report.Result"] = "结果",
                ["Report.Passed"] = "通过",
                ["Report.Failed"] = "失败"
            };
            _resources["zh-CN"] = chinese;
        }

        /// <summary>
        /// Gets a localized string.
        /// </summary>
        public string GetString(string key)
        {
            if (_resources.TryGetValue(_currentLanguage, out var langResources) &&
                langResources.TryGetValue(key, out var value))
            {
                return value;
            }

            // Fallback to English
            if (_resources.TryGetValue("en-US", out var enResources) &&
                enResources.TryGetValue(key, out var enValue))
            {
                return enValue;
            }

            return key;
        }

        /// <summary>
        /// Gets a localized string with format arguments.
        /// </summary>
        public string GetString(string key, params object[] args)
        {
            return string.Format(GetString(key), args);
        }

        /// <summary>
        /// Loads resources from a JSON file.
        /// </summary>
        public void LoadFromFile(string filePath, string languageCode)
        {
            if (!File.Exists(filePath))
                return;

            try
            {
                string json = File.ReadAllText(filePath);
                var resources = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (resources != null)
                {
                    _resources[languageCode] = resources;
                }
            }
            catch (Exception)
            {
                // Ignore load errors
            }
        }

        /// <summary>
        /// Saves resources to a JSON file.
        /// </summary>
        public void SaveToFile(string filePath, string languageCode)
        {
            if (_resources.TryGetValue(languageCode, out var resources))
            {
                string json = JsonSerializer.Serialize(resources, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
        }

        /// <summary>
        /// Adds or updates a string resource.
        /// </summary>
        public void SetString(string key, string value, string? languageCode = null)
        {
            languageCode ??= _currentLanguage;
            
            if (!_resources.ContainsKey(languageCode))
                _resources[languageCode] = new Dictionary<string, string>();

            _resources[languageCode][key] = value;
        }

        /// <summary>
        /// Sets the language from system culture.
        /// </summary>
        public void SetLanguageFromSystem()
        {
            var culture = CultureInfo.CurrentCulture;
            
            if (_resources.ContainsKey(culture.Name))
            {
                CurrentLanguage = culture.Name;
            }
            else if (_resources.ContainsKey(culture.TwoLetterISOLanguageName))
            {
                CurrentLanguage = culture.TwoLetterISOLanguageName;
            }
            else
            {
                CurrentLanguage = "en-US";
            }
        }

        /// <summary>
        /// Gets all string keys for a language.
        /// </summary>
        public IEnumerable<string> GetAllKeys(string? languageCode = null)
        {
            languageCode ??= _currentLanguage;
            
            if (_resources.TryGetValue(languageCode, out var resources))
                return resources.Keys;
            
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Extension methods for localization.
    /// </summary>
    public static class LocalizationExtensions
    {
        /// <summary>
        /// Gets a localized string for a key.
        /// </summary>
        public static string Localize(this string key)
        {
            return LocalizationManager.Instance.GetString(key);
        }

        /// <summary>
        /// Gets a localized string with format arguments.
        /// </summary>
        public static string Localize(this string key, params object[] args)
        {
            return LocalizationManager.Instance.GetString(key, args);
        }
    }
}
