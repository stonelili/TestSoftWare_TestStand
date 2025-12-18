using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.StepGenerator
{
    /// <summary>
    /// Step generation specification
    /// </summary>
    public class StepSpec
    {
        public string Name { get; set; } = string.Empty;
        public string StepType { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public StepGroupType Group { get; set; } = StepGroupType.Main;
        public string? Precondition { get; set; }
        public string? PostActionOnPass { get; set; }
        public string? PostActionOnFail { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Step group type for generation
    /// </summary>
    public enum StepGroupType
    {
        Setup,
        Main,
        Cleanup
    }

    /// <summary>
    /// Numeric limit test specification
    /// </summary>
    public class NumericLimitSpec
    {
        public string Name { get; set; } = string.Empty;
        public double LowLimit { get; set; }
        public double HighLimit { get; set; }
        public string Unit { get; set; } = string.Empty;
        public ComparisonType Comparison { get; set; } = ComparisonType.InRange;
        public string? DataExpression { get; set; }
    }

    /// <summary>
    /// Comparison type for limits
    /// </summary>
    public enum ComparisonType
    {
        InRange,
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual
    }

    /// <summary>
    /// String value test specification
    /// </summary>
    public class StringValueSpec
    {
        public string Name { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public StringComparison ComparisonType { get; set; } = StringComparison.InvariantCulture;
        public bool CaseSensitive { get; set; } = true;
        public string? DataExpression { get; set; }
    }

    /// <summary>
    /// Measurement step specification
    /// </summary>
    public class MeasurementSpec
    {
        public string Name { get; set; } = string.Empty;
        public string MeasurementType { get; set; } = string.Empty;
        public string InstrumentAddress { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public double? LowLimit { get; set; }
        public double? HighLimit { get; set; }
        public string Unit { get; set; } = string.Empty;
        public int DelayBeforeMeasure { get; set; }
        public int DelayAfterMeasure { get; set; }
    }

    /// <summary>
    /// Sequence template specification
    /// </summary>
    public class SequenceSpec
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<StepSpec> SetupSteps { get; set; } = new List<StepSpec>();
        public List<StepSpec> MainSteps { get; set; } = new List<StepSpec>();
        public List<StepSpec> CleanupSteps { get; set; } = new List<StepSpec>();
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> LocalVariables { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Generated step result
    /// </summary>
    public class GeneratedStep
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public StepSpec Specification { get; set; } = new StepSpec();
        public string GeneratedCode { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Step generator interface
    /// </summary>
    public interface IStepGenerator
    {
        string StepType { get; }
        GeneratedStep Generate(StepSpec spec);
        bool Validate(StepSpec spec, out List<string> errors);
    }

    /// <summary>
    /// Numeric limit step generator
    /// </summary>
    public class NumericLimitStepGenerator : IStepGenerator
    {
        public string StepType => "NumericLimit";

        public GeneratedStep Generate(StepSpec spec)
        {
            var result = new GeneratedStep { Specification = spec };

            if (!Validate(spec, out var errors))
            {
                result.IsValid = false;
                result.ValidationErrors = errors;
                return result;
            }

            // Generate the step code/configuration
            var lowLimit = spec.Properties.TryGetValue("LowLimit", out var low) ? low : 0.0;
            var highLimit = spec.Properties.TryGetValue("HighLimit", out var high) ? high : 100.0;

            result.GeneratedCode = $@"
{{
    ""Type"": ""NumericLimitStep"",
    ""Name"": ""{spec.Name}"",
    ""LowLimit"": {lowLimit},
    ""HighLimit"": {highLimit}
}}";
            result.IsValid = true;
            return result;
        }

        public bool Validate(StepSpec spec, out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrEmpty(spec.Name))
                errors.Add("Step name is required");

            if (spec.Properties.TryGetValue("LowLimit", out var low) &&
                spec.Properties.TryGetValue("HighLimit", out var high))
            {
                if (Convert.ToDouble(low) > Convert.ToDouble(high))
                    errors.Add("Low limit cannot be greater than high limit");
            }

            return errors.Count == 0;
        }
    }

    /// <summary>
    /// Delay step generator
    /// </summary>
    public class DelayStepGenerator : IStepGenerator
    {
        public string StepType => "Delay";

        public GeneratedStep Generate(StepSpec spec)
        {
            var result = new GeneratedStep { Specification = spec };

            if (!Validate(spec, out var errors))
            {
                result.IsValid = false;
                result.ValidationErrors = errors;
                return result;
            }

            var delayMs = spec.Properties.TryGetValue("DelayMs", out var delay) ? delay : 1000;

            result.GeneratedCode = $@"
{{
    ""Type"": ""DelayStep"",
    ""Name"": ""{spec.Name}"",
    ""DelayMs"": {delayMs}
}}";
            result.IsValid = true;
            return result;
        }

        public bool Validate(StepSpec spec, out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrEmpty(spec.Name))
                errors.Add("Step name is required");

            if (spec.Properties.TryGetValue("DelayMs", out var delay))
            {
                if (Convert.ToInt32(delay) < 0)
                    errors.Add("Delay cannot be negative");
            }

            return errors.Count == 0;
        }
    }

    /// <summary>
    /// Message popup step generator
    /// </summary>
    public class MessagePopupStepGenerator : IStepGenerator
    {
        public string StepType => "MessagePopup";

        public GeneratedStep Generate(StepSpec spec)
        {
            var result = new GeneratedStep { Specification = spec };

            if (!Validate(spec, out var errors))
            {
                result.IsValid = false;
                result.ValidationErrors = errors;
                return result;
            }

            var message = spec.Properties.TryGetValue("Message", out var msg) ? msg : "";
            var title = spec.Properties.TryGetValue("Title", out var t) ? t : "Message";

            result.GeneratedCode = $@"
{{
    ""Type"": ""MessagePopupStep"",
    ""Name"": ""{spec.Name}"",
    ""Message"": ""{message}"",
    ""Title"": ""{title}""
}}";
            result.IsValid = true;
            return result;
        }

        public bool Validate(StepSpec spec, out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrEmpty(spec.Name))
                errors.Add("Step name is required");

            return errors.Count == 0;
        }
    }

    /// <summary>
    /// Batch step generator for generating multiple steps at once
    /// </summary>
    public class BatchStepGenerator
    {
        private readonly Dictionary<string, IStepGenerator> _generators = 
            new Dictionary<string, IStepGenerator>();

        public BatchStepGenerator()
        {
            RegisterGenerator(new NumericLimitStepGenerator());
            RegisterGenerator(new DelayStepGenerator());
            RegisterGenerator(new MessagePopupStepGenerator());
        }

        public void RegisterGenerator(IStepGenerator generator)
        {
            _generators[generator.StepType] = generator;
        }

        /// <summary>
        /// Generate steps from a list of specifications
        /// </summary>
        public List<GeneratedStep> GenerateSteps(IEnumerable<StepSpec> specs)
        {
            var results = new List<GeneratedStep>();

            foreach (var spec in specs)
            {
                if (_generators.TryGetValue(spec.StepType, out var generator))
                {
                    results.Add(generator.Generate(spec));
                }
                else
                {
                    results.Add(new GeneratedStep
                    {
                        Specification = spec,
                        IsValid = false,
                        ValidationErrors = new List<string> { $"Unknown step type: {spec.StepType}" }
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Generate numeric limit tests from specifications
        /// </summary>
        public List<GeneratedStep> GenerateNumericLimitTests(IEnumerable<NumericLimitSpec> specs)
        {
            var stepSpecs = specs.Select(s => new StepSpec
            {
                Name = s.Name,
                StepType = "NumericLimit",
                Properties = new Dictionary<string, object>
                {
                    ["LowLimit"] = s.LowLimit,
                    ["HighLimit"] = s.HighLimit,
                    ["Unit"] = s.Unit,
                    ["Comparison"] = s.Comparison.ToString()
                }
            });

            return GenerateSteps(stepSpecs);
        }

        /// <summary>
        /// Generate a sequence from specification
        /// </summary>
        public GeneratedSequence GenerateSequence(SequenceSpec spec)
        {
            var result = new GeneratedSequence
            {
                Name = spec.Name,
                Description = spec.Description
            };

            result.SetupSteps = GenerateSteps(spec.SetupSteps);
            result.MainSteps = GenerateSteps(spec.MainSteps);
            result.CleanupSteps = GenerateSteps(spec.CleanupSteps);

            result.IsValid = result.SetupSteps.All(s => s.IsValid) &&
                            result.MainSteps.All(s => s.IsValid) &&
                            result.CleanupSteps.All(s => s.IsValid);

            return result;
        }
    }

    /// <summary>
    /// Generated sequence result
    /// </summary>
    public class GeneratedSequence
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<GeneratedStep> SetupSteps { get; set; } = new List<GeneratedStep>();
        public List<GeneratedStep> MainSteps { get; set; } = new List<GeneratedStep>();
        public List<GeneratedStep> CleanupSteps { get; set; } = new List<GeneratedStep>();
        public bool IsValid { get; set; }
    }

    /// <summary>
    /// Step generator manager singleton
    /// </summary>
    public class StepGeneratorManager
    {
        private static readonly Lazy<StepGeneratorManager> _instance = 
            new Lazy<StepGeneratorManager>(() => new StepGeneratorManager());
        
        public static StepGeneratorManager Instance => _instance.Value;

        private readonly BatchStepGenerator _batchGenerator = new BatchStepGenerator();

        private StepGeneratorManager() { }

        public BatchStepGenerator BatchGenerator => _batchGenerator;

        /// <summary>
        /// Register a custom generator
        /// </summary>
        public void RegisterGenerator(IStepGenerator generator)
        {
            _batchGenerator.RegisterGenerator(generator);
        }

        /// <summary>
        /// Generate steps from specifications
        /// </summary>
        public List<GeneratedStep> GenerateSteps(IEnumerable<StepSpec> specs)
        {
            return _batchGenerator.GenerateSteps(specs);
        }

        /// <summary>
        /// Generate a sequence from specification
        /// </summary>
        public GeneratedSequence GenerateSequence(SequenceSpec spec)
        {
            return _batchGenerator.GenerateSequence(spec);
        }
    }
}
