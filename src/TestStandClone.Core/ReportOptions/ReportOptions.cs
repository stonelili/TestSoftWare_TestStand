// Copyright (c) TestStand Clone. All rights reserved.
// Report configuration options

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TestStandClone.Core.ReportOptions
{
    /// <summary>
    /// Report format types
    /// </summary>
    public enum ReportFormat
    {
        Html,
        Text,
        Xml,
        Csv,
        Pdf,
        Custom
    }

    /// <summary>
    /// Report detail level
    /// </summary>
    public enum ReportDetailLevel
    {
        Summary,
        Standard,
        Detailed,
        Full
    }

    /// <summary>
    /// Step inclusion filter
    /// </summary>
    public enum StepInclusionFilter
    {
        AllSteps,
        PassedOnly,
        FailedOnly,
        PassedAndFailed,
        ErrorOnly
    }

    /// <summary>
    /// Report options configuration
    /// </summary>
    public class ReportConfiguration : INotifyPropertyChanged
    {
        private string _outputDirectory = string.Empty;
        private string _fileNamePattern = "{SequenceName}_{Date}_{Time}";
        private ReportFormat _format = ReportFormat.Html;
        private ReportDetailLevel _detailLevel = ReportDetailLevel.Standard;
        private StepInclusionFilter _stepFilter = StepInclusionFilter.AllSteps;
        private bool _includePassedSteps = true;
        private bool _includeFailedSteps = true;
        private bool _includeSkippedSteps = false;
        private bool _includeStepTiming = true;
        private bool _includeVariables = true;
        private bool _includeLimits = true;
        private bool _includeOperatorComments = true;
        private bool _includeUUTInfo = true;
        private bool _includeStationInfo = true;
        private bool _openAfterGeneration = false;
        private bool _autoGenerateOnComplete = true;
        private string _customHeader = string.Empty;
        private string _customFooter = string.Empty;
        private string _logoPath = string.Empty;
        private string _companyName = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Output directory for reports
        /// </summary>
        public string OutputDirectory
        {
            get => _outputDirectory;
            set { _outputDirectory = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// File name pattern (supports placeholders)
        /// </summary>
        public string FileNamePattern
        {
            get => _fileNamePattern;
            set { _fileNamePattern = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Report format
        /// </summary>
        public ReportFormat Format
        {
            get => _format;
            set { _format = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Detail level
        /// </summary>
        public ReportDetailLevel DetailLevel
        {
            get => _detailLevel;
            set { _detailLevel = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Step inclusion filter
        /// </summary>
        public StepInclusionFilter StepFilter
        {
            get => _stepFilter;
            set { _stepFilter = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Include passed steps
        /// </summary>
        public bool IncludePassedSteps
        {
            get => _includePassedSteps;
            set { _includePassedSteps = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Include failed steps
        /// </summary>
        public bool IncludeFailedSteps
        {
            get => _includeFailedSteps;
            set { _includeFailedSteps = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Include skipped steps
        /// </summary>
        public bool IncludeSkippedSteps
        {
            get => _includeSkippedSteps;
            set { _includeSkippedSteps = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Include step timing information
        /// </summary>
        public bool IncludeStepTiming
        {
            get => _includeStepTiming;
            set { _includeStepTiming = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Include variables in report
        /// </summary>
        public bool IncludeVariables
        {
            get => _includeVariables;
            set { _includeVariables = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Include limit information
        /// </summary>
        public bool IncludeLimits
        {
            get => _includeLimits;
            set { _includeLimits = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Include operator comments
        /// </summary>
        public bool IncludeOperatorComments
        {
            get => _includeOperatorComments;
            set { _includeOperatorComments = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Include UUT information
        /// </summary>
        public bool IncludeUUTInfo
        {
            get => _includeUUTInfo;
            set { _includeUUTInfo = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Include station information
        /// </summary>
        public bool IncludeStationInfo
        {
            get => _includeStationInfo;
            set { _includeStationInfo = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Open report after generation
        /// </summary>
        public bool OpenAfterGeneration
        {
            get => _openAfterGeneration;
            set { _openAfterGeneration = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Auto-generate report on completion
        /// </summary>
        public bool AutoGenerateOnComplete
        {
            get => _autoGenerateOnComplete;
            set { _autoGenerateOnComplete = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Custom header text
        /// </summary>
        public string CustomHeader
        {
            get => _customHeader;
            set { _customHeader = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Custom footer text
        /// </summary>
        public string CustomFooter
        {
            get => _customFooter;
            set { _customFooter = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Path to company logo
        /// </summary>
        public string LogoPath
        {
            get => _logoPath;
            set { _logoPath = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Company name for report header
        /// </summary>
        public string CompanyName
        {
            get => _companyName;
            set { _companyName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Additional custom options
        /// </summary>
        public Dictionary<string, object> CustomOptions { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Generate the file name based on pattern
        /// </summary>
        public string GenerateFileName(string sequenceName, DateTime timestamp)
        {
            var name = FileNamePattern
                .Replace("{SequenceName}", sequenceName)
                .Replace("{Date}", timestamp.ToString("yyyy-MM-dd"))
                .Replace("{Time}", timestamp.ToString("HHmmss"))
                .Replace("{DateTime}", timestamp.ToString("yyyy-MM-dd_HHmmss"))
                .Replace("{Timestamp}", timestamp.Ticks.ToString());
            
            // Sanitize file name
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c.ToString(), "_");
            }
            
            return name + GetFileExtension();
        }

        /// <summary>
        /// Get file extension for the format
        /// </summary>
        public string GetFileExtension()
        {
            return Format switch
            {
                ReportFormat.Html => ".html",
                ReportFormat.Text => ".txt",
                ReportFormat.Xml => ".xml",
                ReportFormat.Csv => ".csv",
                ReportFormat.Pdf => ".pdf",
                _ => ".txt"
            };
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Manages report options and generation
    /// </summary>
    public sealed class ReportOptionsManager
    {
        private static readonly Lazy<ReportOptionsManager> _instance = 
            new Lazy<ReportOptionsManager>(() => new ReportOptionsManager());

        private ReportConfiguration _currentConfiguration = new ReportConfiguration();
        private readonly Dictionary<string, ReportConfiguration> _savedConfigurations = 
            new Dictionary<string, ReportConfiguration>();

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static ReportOptionsManager Instance => _instance.Value;

        /// <summary>
        /// Current report configuration
        /// </summary>
        public ReportConfiguration CurrentConfiguration
        {
            get => _currentConfiguration;
            set => _currentConfiguration = value;
        }

        /// <summary>
        /// Saved configurations
        /// </summary>
        public IReadOnlyDictionary<string, ReportConfiguration> SavedConfigurations => _savedConfigurations;

        private ReportOptionsManager()
        {
            // Initialize with defaults
            _savedConfigurations["Default"] = new ReportConfiguration();
            _savedConfigurations["Summary"] = CreateSummaryConfiguration();
            _savedConfigurations["Detailed"] = CreateDetailedConfiguration();
            _savedConfigurations["FailedOnly"] = CreateFailedOnlyConfiguration();
        }

        /// <summary>
        /// Save current configuration
        /// </summary>
        public void SaveConfiguration(string name)
        {
            _savedConfigurations[name] = CloneConfiguration(_currentConfiguration);
        }

        /// <summary>
        /// Load a saved configuration
        /// </summary>
        public bool LoadConfiguration(string name)
        {
            if (_savedConfigurations.TryGetValue(name, out var config))
            {
                _currentConfiguration = CloneConfiguration(config);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Delete a saved configuration
        /// </summary>
        public bool DeleteConfiguration(string name)
        {
            if (name == "Default")
                return false;
            
            return _savedConfigurations.Remove(name);
        }

        /// <summary>
        /// Reset to default configuration
        /// </summary>
        public void ResetToDefault()
        {
            _currentConfiguration = new ReportConfiguration();
        }

        private ReportConfiguration CreateSummaryConfiguration()
        {
            return new ReportConfiguration
            {
                DetailLevel = ReportDetailLevel.Summary,
                IncludeVariables = false,
                IncludeStepTiming = false,
                IncludeLimits = false
            };
        }

        private ReportConfiguration CreateDetailedConfiguration()
        {
            return new ReportConfiguration
            {
                DetailLevel = ReportDetailLevel.Full,
                IncludeVariables = true,
                IncludeStepTiming = true,
                IncludeLimits = true,
                IncludeSkippedSteps = true
            };
        }

        private ReportConfiguration CreateFailedOnlyConfiguration()
        {
            return new ReportConfiguration
            {
                StepFilter = StepInclusionFilter.FailedOnly,
                IncludePassedSteps = false,
                IncludeSkippedSteps = false
            };
        }

        private ReportConfiguration CloneConfiguration(ReportConfiguration source)
        {
            return new ReportConfiguration
            {
                OutputDirectory = source.OutputDirectory,
                FileNamePattern = source.FileNamePattern,
                Format = source.Format,
                DetailLevel = source.DetailLevel,
                StepFilter = source.StepFilter,
                IncludePassedSteps = source.IncludePassedSteps,
                IncludeFailedSteps = source.IncludeFailedSteps,
                IncludeSkippedSteps = source.IncludeSkippedSteps,
                IncludeStepTiming = source.IncludeStepTiming,
                IncludeVariables = source.IncludeVariables,
                IncludeLimits = source.IncludeLimits,
                IncludeOperatorComments = source.IncludeOperatorComments,
                IncludeUUTInfo = source.IncludeUUTInfo,
                IncludeStationInfo = source.IncludeStationInfo,
                OpenAfterGeneration = source.OpenAfterGeneration,
                AutoGenerateOnComplete = source.AutoGenerateOnComplete,
                CustomHeader = source.CustomHeader,
                CustomFooter = source.CustomFooter,
                LogoPath = source.LogoPath,
                CompanyName = source.CompanyName
            };
        }
    }
}
