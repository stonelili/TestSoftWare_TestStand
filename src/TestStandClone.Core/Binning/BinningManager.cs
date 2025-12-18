using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.Binning
{
    /// <summary>
    /// Bin category types
    /// </summary>
    public enum BinCategory
    {
        Pass,
        Fail,
        Retest,
        Abort,
        Skip,
        Custom
    }

    /// <summary>
    /// Bin definition
    /// </summary>
    public class BinDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int BinNumber { get; set; }
        public BinCategory Category { get; set; } = BinCategory.Custom;
        public string Color { get; set; } = "#808080";
        public bool IsEnabled { get; set; } = true;
        public int Priority { get; set; }
        public List<BinCondition> Conditions { get; set; } = new List<BinCondition>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Condition types for binning
    /// </summary>
    public enum BinConditionType
    {
        StepStatus,
        StepName,
        MeasuredValue,
        ErrorCode,
        Expression
    }

    /// <summary>
    /// Bin condition
    /// </summary>
    public class BinCondition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public BinConditionType Type { get; set; }
        public string Property { get; set; } = string.Empty;
        public string Operator { get; set; } = "==";
        public object Value { get; set; } = null!;
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Result of bin evaluation
    /// </summary>
    public class BinResult
    {
        public string UnitId { get; set; } = string.Empty;
        public BinDefinition AssignedBin { get; set; } = null!;
        public List<string> MatchedConditions { get; set; } = new List<string>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Bin statistics
    /// </summary>
    public class BinStatistics
    {
        public Dictionary<int, int> BinCounts { get; set; } = new Dictionary<int, int>();
        public int TotalUnits { get; set; }
        public double PassYield => TotalUnits > 0 ? (double)PassCount / TotalUnits * 100 : 0;
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public double GetBinPercentage(int binNumber)
        {
            if (TotalUnits == 0 || !BinCounts.ContainsKey(binNumber)) return 0;
            return (double)BinCounts[binNumber] / TotalUnits * 100;
        }
    }

    /// <summary>
    /// Binning manager singleton
    /// </summary>
    public class BinningManager
    {
        private static readonly Lazy<BinningManager> _instance = new Lazy<BinningManager>(() => new BinningManager());
        public static BinningManager Instance => _instance.Value;

        private readonly List<BinDefinition> _bins = new List<BinDefinition>();
        private readonly List<BinResult> _results = new List<BinResult>();
        private readonly object _lock = new object();

        public event EventHandler<BinResult>? UnitBinned;

        private BinningManager()
        {
            InitializeDefaultBins();
        }

        private void InitializeDefaultBins()
        {
            _bins.Add(new BinDefinition { Name = "Pass", BinNumber = 1, Category = BinCategory.Pass, Color = "#00FF00", Priority = 100 });
            _bins.Add(new BinDefinition { Name = "Fail", BinNumber = 2, Category = BinCategory.Fail, Color = "#FF0000", Priority = 1 });
            _bins.Add(new BinDefinition { Name = "Retest", BinNumber = 3, Category = BinCategory.Retest, Color = "#FFFF00", Priority = 50 });
            _bins.Add(new BinDefinition { Name = "Abort", BinNumber = 4, Category = BinCategory.Abort, Color = "#FF8000", Priority = 10 });
        }

        public void RegisterBin(BinDefinition bin)
        {
            lock (_lock)
            {
                _bins.RemoveAll(b => b.Id == bin.Id);
                _bins.Add(bin);
            }
        }

        public BinDefinition? GetBin(int binNumber)
        {
            lock (_lock)
            {
                return _bins.FirstOrDefault(b => b.BinNumber == binNumber);
            }
        }

        public List<BinDefinition> GetAllBins()
        {
            lock (_lock)
            {
                return _bins.ToList();
            }
        }

        public BinResult AssignBin(string unitId, Dictionary<string, object> context)
        {
            lock (_lock)
            {
                var enabledBins = _bins.Where(b => b.IsEnabled).OrderBy(b => b.Priority).ToList();
                
                foreach (var bin in enabledBins)
                {
                    if (EvaluateBinConditions(bin, context))
                    {
                        var result = new BinResult
                        {
                            UnitId = unitId,
                            AssignedBin = bin,
                            Context = context,
                            MatchedConditions = bin.Conditions.Select(c => c.Id).ToList()
                        };
                        _results.Add(result);
                        UnitBinned?.Invoke(this, result);
                        return result;
                    }
                }

                // Default to fail bin if no match
                var defaultBin = _bins.FirstOrDefault(b => b.Category == BinCategory.Fail) ?? _bins.First();
                var defaultResult = new BinResult
                {
                    UnitId = unitId,
                    AssignedBin = defaultBin,
                    Context = context
                };
                _results.Add(defaultResult);
                UnitBinned?.Invoke(this, defaultResult);
                return defaultResult;
            }
        }

        private bool EvaluateBinConditions(BinDefinition bin, Dictionary<string, object> context)
        {
            if (bin.Conditions.Count == 0 && bin.Category == BinCategory.Pass)
            {
                return context.TryGetValue("OverallStatus", out var status) && 
                       status?.ToString()?.Equals("Passed", StringComparison.OrdinalIgnoreCase) == true;
            }

            foreach (var condition in bin.Conditions.Where(c => c.IsEnabled))
            {
                if (!EvaluateCondition(condition, context))
                    return false;
            }
            return bin.Conditions.Any(c => c.IsEnabled);
        }

        private bool EvaluateCondition(BinCondition condition, Dictionary<string, object> context)
        {
            if (!context.TryGetValue(condition.Property, out var value))
                return false;

            return condition.Operator switch
            {
                "==" => Equals(value, condition.Value),
                "!=" => !Equals(value, condition.Value),
                ">" => Compare(value, condition.Value) > 0,
                "<" => Compare(value, condition.Value) < 0,
                ">=" => Compare(value, condition.Value) >= 0,
                "<=" => Compare(value, condition.Value) <= 0,
                "contains" => value?.ToString()?.Contains(condition.Value?.ToString() ?? "") == true,
                _ => false
            };
        }

        private int Compare(object a, object b)
        {
            if (a is IComparable ca && b is IComparable cb)
                return ca.CompareTo(cb);
            return 0;
        }

        public BinStatistics GetStatistics(DateTime? startTime = null, DateTime? endTime = null)
        {
            lock (_lock)
            {
                var filtered = _results.AsEnumerable();
                if (startTime.HasValue)
                    filtered = filtered.Where(r => r.Timestamp >= startTime.Value);
                if (endTime.HasValue)
                    filtered = filtered.Where(r => r.Timestamp <= endTime.Value);

                var list = filtered.ToList();
                var stats = new BinStatistics
                {
                    TotalUnits = list.Count,
                    StartTime = list.Any() ? list.Min(r => r.Timestamp) : DateTime.UtcNow,
                    EndTime = list.Any() ? list.Max(r => r.Timestamp) : DateTime.UtcNow
                };

                foreach (var result in list)
                {
                    var binNum = result.AssignedBin.BinNumber;
                    stats.BinCounts.TryGetValue(binNum, out var count);
                    stats.BinCounts[binNum] = count + 1;

                    if (result.AssignedBin.Category == BinCategory.Pass)
                        stats.PassCount++;
                    else if (result.AssignedBin.Category == BinCategory.Fail)
                        stats.FailCount++;
                }

                return stats;
            }
        }

        public void ClearResults() { lock (_lock) { _results.Clear(); } }
    }
}
