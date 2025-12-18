// Copyright (c) TestStand Clone. All rights reserved.
// Step settings and configuration

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TestStandClone.Core.StepSettings
{
    /// <summary>
    /// Step run mode
    /// </summary>
    public enum StepRunMode
    {
        Normal,
        Skip,
        ForcePass,
        ForceFail
    }

    /// <summary>
    /// Step timing mode
    /// </summary>
    public enum StepTimingMode
    {
        None,
        PerStep,
        Accumulated
    }

    /// <summary>
    /// Step failure action
    /// </summary>
    public enum StepFailureAction
    {
        Continue,
        AbortSequence,
        AbortAll,
        Retry,
        GotoStep,
        CallCleanup
    }

    /// <summary>
    /// Step post action
    /// </summary>
    public enum StepPostAction
    {
        Continue,
        Goto,
        Terminate,
        TerminateWithFail,
        CallCallback
    }

    /// <summary>
    /// Comprehensive step settings
    /// </summary>
    public class StepSettings : INotifyPropertyChanged
    {
        private StepRunMode _runMode = StepRunMode.Normal;
        private StepFailureAction _failureAction = StepFailureAction.Continue;
        private int _maxRetryCount = 0;
        private int _retryDelayMs = 0;
        private int _timeoutMs = 0;
        private bool _recordResult = true;
        private bool _ignoreFailure = false;
        private string _precondition = string.Empty;
        private StepPostAction _onPassAction = StepPostAction.Continue;
        private StepPostAction _onFailAction = StepPostAction.Continue;
        private string _onPassGotoStep = string.Empty;
        private string _onFailGotoStep = string.Empty;
        private int _loopCount = 1;
        private bool _loopOnPass = false;
        private bool _loopOnFail = false;
        private int _loopDelayMs = 0;
        private string _loopCondition = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Run mode for the step
        /// </summary>
        public StepRunMode RunMode
        {
            get => _runMode;
            set { _runMode = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Action on failure
        /// </summary>
        public StepFailureAction FailureAction
        {
            get => _failureAction;
            set { _failureAction = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Maximum retry count
        /// </summary>
        public int MaxRetryCount
        {
            get => _maxRetryCount;
            set { _maxRetryCount = Math.Max(0, value); OnPropertyChanged(); }
        }

        /// <summary>
        /// Delay between retries in milliseconds
        /// </summary>
        public int RetryDelayMs
        {
            get => _retryDelayMs;
            set { _retryDelayMs = Math.Max(0, value); OnPropertyChanged(); }
        }

        /// <summary>
        /// Step timeout in milliseconds (0 = no timeout)
        /// </summary>
        public int TimeoutMs
        {
            get => _timeoutMs;
            set { _timeoutMs = Math.Max(0, value); OnPropertyChanged(); }
        }

        /// <summary>
        /// Whether to record the result
        /// </summary>
        public bool RecordResult
        {
            get => _recordResult;
            set { _recordResult = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Whether to ignore failure
        /// </summary>
        public bool IgnoreFailure
        {
            get => _ignoreFailure;
            set { _ignoreFailure = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Precondition expression
        /// </summary>
        public string Precondition
        {
            get => _precondition;
            set { _precondition = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Action on pass
        /// </summary>
        public StepPostAction OnPassAction
        {
            get => _onPassAction;
            set { _onPassAction = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Action on fail
        /// </summary>
        public StepPostAction OnFailAction
        {
            get => _onFailAction;
            set { _onFailAction = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Step to go to on pass
        /// </summary>
        public string OnPassGotoStep
        {
            get => _onPassGotoStep;
            set { _onPassGotoStep = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Step to go to on fail
        /// </summary>
        public string OnFailGotoStep
        {
            get => _onFailGotoStep;
            set { _onFailGotoStep = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Number of times to loop
        /// </summary>
        public int LoopCount
        {
            get => _loopCount;
            set { _loopCount = Math.Max(1, value); OnPropertyChanged(); }
        }

        /// <summary>
        /// Loop while passing
        /// </summary>
        public bool LoopOnPass
        {
            get => _loopOnPass;
            set { _loopOnPass = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Loop while failing
        /// </summary>
        public bool LoopOnFail
        {
            get => _loopOnFail;
            set { _loopOnFail = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Delay between loops in milliseconds
        /// </summary>
        public int LoopDelayMs
        {
            get => _loopDelayMs;
            set { _loopDelayMs = Math.Max(0, value); OnPropertyChanged(); }
        }

        /// <summary>
        /// Loop condition expression
        /// </summary>
        public string LoopCondition
        {
            get => _loopCondition;
            set { _loopCondition = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Additional custom settings
        /// </summary>
        public Dictionary<string, object> CustomSettings { get; } = new Dictionary<string, object>();

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Clone the settings
        /// </summary>
        public StepSettings Clone()
        {
            var clone = new StepSettings
            {
                RunMode = RunMode,
                FailureAction = FailureAction,
                MaxRetryCount = MaxRetryCount,
                RetryDelayMs = RetryDelayMs,
                TimeoutMs = TimeoutMs,
                RecordResult = RecordResult,
                IgnoreFailure = IgnoreFailure,
                Precondition = Precondition,
                OnPassAction = OnPassAction,
                OnFailAction = OnFailAction,
                OnPassGotoStep = OnPassGotoStep,
                OnFailGotoStep = OnFailGotoStep,
                LoopCount = LoopCount,
                LoopOnPass = LoopOnPass,
                LoopOnFail = LoopOnFail,
                LoopDelayMs = LoopDelayMs,
                LoopCondition = LoopCondition
            };
            
            foreach (var kv in CustomSettings)
            {
                clone.CustomSettings[kv.Key] = kv.Value;
            }
            
            return clone;
        }
    }

    /// <summary>
    /// Limit comparison type
    /// </summary>
    public enum LimitComparisonType
    {
        EQ,     // Equal
        NE,     // Not Equal
        GT,     // Greater Than
        GE,     // Greater or Equal
        LT,     // Less Than
        LE,     // Less or Equal
        GTLT,   // Between (exclusive)
        GELT,   // Between (inclusive low)
        GTLE,   // Between (inclusive high)
        GELE,   // Between (inclusive both)
        LTGT,   // Outside (exclusive)
        LEGT,   // Outside (inclusive low)
        LTGE,   // Outside (inclusive high)
        LEGE    // Outside (inclusive both)
    }

    /// <summary>
    /// Numeric limit settings
    /// </summary>
    public class NumericLimitSettings : INotifyPropertyChanged
    {
        private double _lowLimit;
        private double _highLimit;
        private LimitComparisonType _comparisonType = LimitComparisonType.GELE;
        private string _units = string.Empty;
        private bool _useLimits = true;
        private double _nominalValue;
        private double _tolerance;
        private bool _usePercentTolerance;
        private int _precision = 6;
        private string _format = "F6";

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Low limit
        /// </summary>
        public double LowLimit
        {
            get => _lowLimit;
            set { _lowLimit = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// High limit
        /// </summary>
        public double HighLimit
        {
            get => _highLimit;
            set { _highLimit = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Comparison type
        /// </summary>
        public LimitComparisonType ComparisonType
        {
            get => _comparisonType;
            set { _comparisonType = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Units of measurement
        /// </summary>
        public string Units
        {
            get => _units;
            set { _units = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Whether to use limits
        /// </summary>
        public bool UseLimits
        {
            get => _useLimits;
            set { _useLimits = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Nominal value
        /// </summary>
        public double NominalValue
        {
            get => _nominalValue;
            set { _nominalValue = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Tolerance value
        /// </summary>
        public double Tolerance
        {
            get => _tolerance;
            set { _tolerance = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Whether tolerance is percentage
        /// </summary>
        public bool UsePercentTolerance
        {
            get => _usePercentTolerance;
            set { _usePercentTolerance = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Decimal precision for display
        /// </summary>
        public int Precision
        {
            get => _precision;
            set { _precision = Math.Max(0, Math.Min(15, value)); OnPropertyChanged(); }
        }

        /// <summary>
        /// Format string for display
        /// </summary>
        public string Format
        {
            get => _format;
            set { _format = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Calculate limits from nominal and tolerance
        /// </summary>
        public void CalculateLimitsFromTolerance()
        {
            if (UsePercentTolerance)
            {
                var delta = Math.Abs(NominalValue * Tolerance / 100.0);
                LowLimit = NominalValue - delta;
                HighLimit = NominalValue + delta;
            }
            else
            {
                LowLimit = NominalValue - Tolerance;
                HighLimit = NominalValue + Tolerance;
            }
        }

        /// <summary>
        /// Check if a value passes the limit
        /// </summary>
        public bool CheckLimit(double value)
        {
            if (!UseLimits)
                return true;

            return ComparisonType switch
            {
                LimitComparisonType.EQ => Math.Abs(value - LowLimit) < double.Epsilon,
                LimitComparisonType.NE => Math.Abs(value - LowLimit) >= double.Epsilon,
                LimitComparisonType.GT => value > LowLimit,
                LimitComparisonType.GE => value >= LowLimit,
                LimitComparisonType.LT => value < HighLimit,
                LimitComparisonType.LE => value <= HighLimit,
                LimitComparisonType.GTLT => value > LowLimit && value < HighLimit,
                LimitComparisonType.GELT => value >= LowLimit && value < HighLimit,
                LimitComparisonType.GTLE => value > LowLimit && value <= HighLimit,
                LimitComparisonType.GELE => value >= LowLimit && value <= HighLimit,
                LimitComparisonType.LTGT => value < LowLimit || value > HighLimit,
                LimitComparisonType.LEGT => value <= LowLimit || value > HighLimit,
                LimitComparisonType.LTGE => value < LowLimit || value >= HighLimit,
                LimitComparisonType.LEGE => value <= LowLimit || value >= HighLimit,
                _ => true
            };
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// String comparison type
    /// </summary>
    public enum StringComparisonType
    {
        Equal,
        NotEqual,
        Contains,
        NotContains,
        StartsWith,
        EndsWith,
        Regex,
        Empty,
        NotEmpty
    }

    /// <summary>
    /// String limit settings
    /// </summary>
    public class StringLimitSettings : INotifyPropertyChanged
    {
        private string _expectedValue = string.Empty;
        private StringComparisonType _comparisonType = StringComparisonType.Equal;
        private bool _caseSensitive = false;
        private bool _trimWhitespace = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Expected string value
        /// </summary>
        public string ExpectedValue
        {
            get => _expectedValue;
            set { _expectedValue = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Comparison type
        /// </summary>
        public StringComparisonType ComparisonType
        {
            get => _comparisonType;
            set { _comparisonType = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Whether comparison is case sensitive
        /// </summary>
        public bool CaseSensitive
        {
            get => _caseSensitive;
            set { _caseSensitive = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Whether to trim whitespace before comparison
        /// </summary>
        public bool TrimWhitespace
        {
            get => _trimWhitespace;
            set { _trimWhitespace = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Check if a value passes the limit
        /// </summary>
        public bool CheckLimit(string value)
        {
            var testValue = TrimWhitespace ? value?.Trim() ?? "" : value ?? "";
            var expectedValue = TrimWhitespace ? ExpectedValue?.Trim() ?? "" : ExpectedValue ?? "";
            
            var comparison = CaseSensitive 
                ? StringComparison.Ordinal 
                : StringComparison.OrdinalIgnoreCase;

            return ComparisonType switch
            {
                StringComparisonType.Equal => testValue.Equals(expectedValue, comparison),
                StringComparisonType.NotEqual => !testValue.Equals(expectedValue, comparison),
                StringComparisonType.Contains => testValue.Contains(expectedValue, comparison),
                StringComparisonType.NotContains => !testValue.Contains(expectedValue, comparison),
                StringComparisonType.StartsWith => testValue.StartsWith(expectedValue, comparison),
                StringComparisonType.EndsWith => testValue.EndsWith(expectedValue, comparison),
                StringComparisonType.Regex => System.Text.RegularExpressions.Regex.IsMatch(testValue, expectedValue),
                StringComparisonType.Empty => string.IsNullOrEmpty(testValue),
                StringComparisonType.NotEmpty => !string.IsNullOrEmpty(testValue),
                _ => true
            };
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Data source for step values
    /// </summary>
    public class DataSource : INotifyPropertyChanged
    {
        private string _sourceType = "Literal";
        private string _expression = string.Empty;
        private string _variableName = string.Empty;
        private string _filePath = string.Empty;
        private string _propertyPath = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Type of data source (Literal, Expression, Variable, File, Property)
        /// </summary>
        public string SourceType
        {
            get => _sourceType;
            set { _sourceType = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Expression for calculated values
        /// </summary>
        public string Expression
        {
            get => _expression;
            set { _expression = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Variable name for variable source
        /// </summary>
        public string VariableName
        {
            get => _variableName;
            set { _variableName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// File path for file source
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Property path for property source
        /// </summary>
        public string PropertyPath
        {
            get => _propertyPath;
            set { _propertyPath = value; OnPropertyChanged(); }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
