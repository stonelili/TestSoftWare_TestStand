using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TestStandClone.Core.SequenceValidation
{
    /// <summary>
    /// Validates sequences before execution
    /// </summary>
    public interface ISequenceValidator
    {
        string Name { get; }
        string Description { get; }
        ValidationCategory Category { get; }
        ValidationResult Validate(Sequence sequence);
    }

    /// <summary>
    /// Validation categories
    /// </summary>
    public enum ValidationCategory
    {
        Structure,
        Logic,
        Performance,
        BestPractices,
        Security,
        Compatibility
    }

    /// <summary>
    /// Severity levels for validation issues
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Result of validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => !Issues.Any(i => i.Severity == ValidationSeverity.Error || i.Severity == ValidationSeverity.Critical);
        public List<ValidationIssue> Issues { get; set; } = new();
        public string ValidatorName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// A single validation issue
    /// </summary>
    public class ValidationIssue
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Warning;
        public ValidationCategory Category { get; set; }
        public string? StepId { get; set; }
        public string? StepName { get; set; }
        public string? Recommendation { get; set; }
        public int? LineNumber { get; set; }
    }

    /// <summary>
    /// Validates sequence structure
    /// </summary>
    public class StructureValidator : ISequenceValidator
    {
        public string Name => "Structure Validator";
        public string Description => "Validates the structural integrity of the sequence";
        public ValidationCategory Category => ValidationCategory.Structure;

        public ValidationResult Validate(Sequence sequence)
        {
            var result = new ValidationResult { ValidatorName = Name };

            // Check for empty sequence
            if (sequence.Steps.Count == 0)
            {
                result.Issues.Add(new ValidationIssue
                {
                    Code = "STR001",
                    Message = "Sequence has no steps",
                    Severity = ValidationSeverity.Warning,
                    Category = Category,
                    Recommendation = "Add at least one step to the sequence"
                });
            }

            // Check for duplicate step IDs
            var duplicateIds = sequence.Steps.GroupBy(s => s.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var id in duplicateIds)
            {
                result.Issues.Add(new ValidationIssue
                {
                    Code = "STR002",
                    Message = $"Duplicate step ID: {id}",
                    Severity = ValidationSeverity.Error,
                    Category = Category,
                    StepId = id.ToString(),
                    Recommendation = "Ensure all steps have unique IDs"
                });
            }

            // Check for duplicate step names
            var duplicateNames = sequence.Steps.GroupBy(s => s.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var name in duplicateNames)
            {
                result.Issues.Add(new ValidationIssue
                {
                    Code = "STR003",
                    Message = $"Duplicate step name: {name}",
                    Severity = ValidationSeverity.Warning,
                    Category = Category,
                    StepName = name,
                    Recommendation = "Consider using unique step names for clarity"
                });
            }

            return result;
        }
    }

    /// <summary>
    /// Validates sequence logic
    /// </summary>
    public class LogicValidator : ISequenceValidator
    {
        public string Name => "Logic Validator";
        public string Description => "Validates the logical correctness of the sequence";
        public ValidationCategory Category => ValidationCategory.Logic;

        public ValidationResult Validate(Sequence sequence)
        {
            var result = new ValidationResult { ValidatorName = Name };

            // Check for unreachable steps after unconditional goto
            ValidateFlowControl(sequence, result);

            // Check loop configurations
            ValidateLoops(sequence, result);

            // Check preconditions
            ValidatePreconditions(sequence, result);

            return result;
        }

        private void ValidateFlowControl(Sequence sequence, ValidationResult result)
        {
            for (int i = 0; i < sequence.Steps.Count - 1; i++)
            {
                var step = sequence.Steps[i];
                var stepType = step.GetType().Name;

                // Check if step is an unconditional goto
                if (stepType == "GotoStep" && string.IsNullOrEmpty(step.Precondition))
                {
                    var nextStep = sequence.Steps[i + 1];
                    
                    // Check if next step is a label
                    if (nextStep.GetType().Name != "LabelStep")
                    {
                        result.Issues.Add(new ValidationIssue
                        {
                            Code = "LOG001",
                            Message = $"Step '{nextStep.Name}' after unconditional Goto may be unreachable",
                            Severity = ValidationSeverity.Warning,
                            Category = Category,
                            StepId = nextStep.Id.ToString(),
                            StepName = nextStep.Name,
                            Recommendation = "Review flow control logic"
                        });
                    }
                }
            }
        }

        private void ValidateLoops(Sequence sequence, ValidationResult result)
        {
            foreach (var step in sequence.Steps)
            {
                // Check for potential infinite loops
                if (step.LoopCount == 0 && step.GetType().Name.Contains("Loop"))
                {
                    result.Issues.Add(new ValidationIssue
                    {
                        Code = "LOG002",
                        Message = $"Loop step '{step.Name}' has loop count of 0",
                        Severity = ValidationSeverity.Warning,
                        Category = Category,
                        StepId = step.Id.ToString(),
                        StepName = step.Name,
                        Recommendation = "Set a positive loop count or use exit conditions"
                    });
                }

                // Check if loop body is defined
                if (step.GetType().Name == "LoopStep")
                {
                    var loopEndProperty = step.GetType().GetProperty("LoopEndStepName");
                    if (loopEndProperty != null)
                    {
                        var loopEnd = loopEndProperty.GetValue(step) as string;
                        if (string.IsNullOrEmpty(loopEnd))
                        {
                            result.Issues.Add(new ValidationIssue
                            {
                                Code = "LOG003",
                                Message = $"Loop step '{step.Name}' has no defined end step",
                                Severity = ValidationSeverity.Error,
                                Category = Category,
                                StepId = step.Id.ToString(),
                                StepName = step.Name,
                                Recommendation = "Define a loop end step"
                            });
                        }
                    }
                }
            }
        }

        private void ValidatePreconditions(Sequence sequence, ValidationResult result)
        {
            foreach (var step in sequence.Steps)
            {
                if (!string.IsNullOrEmpty(step.Precondition))
                {
                    // Check if precondition references existing steps
                    var stepNamePattern = @"Step\[""([^""]+)""\]";
                    var matches = Regex.Matches(step.Precondition, stepNamePattern);

                    foreach (Match match in matches)
                    {
                        var referencedStep = match.Groups[1].Value;
                        if (!sequence.Steps.Any(s => s.Name == referencedStep))
                        {
                            result.Issues.Add(new ValidationIssue
                            {
                                Code = "LOG004",
                                Message = $"Precondition in '{step.Name}' references non-existent step '{referencedStep}'",
                                Severity = ValidationSeverity.Error,
                                Category = Category,
                                StepId = step.Id.ToString(),
                                StepName = step.Name,
                                Recommendation = "Update precondition to reference an existing step"
                            });
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Validates naming conventions
    /// </summary>
    public class NamingConventionValidator : ISequenceValidator
    {
        public string Name => "Naming Convention Validator";
        public string Description => "Validates naming conventions for steps and variables";
        public ValidationCategory Category => ValidationCategory.BestPractices;

        private readonly Regex _validNamePattern = new(@"^[a-zA-Z][a-zA-Z0-9_]*$");

        public ValidationResult Validate(Sequence sequence)
        {
            var result = new ValidationResult { ValidatorName = Name };

            // Check sequence name
            if (string.IsNullOrWhiteSpace(sequence.Name))
            {
                result.Issues.Add(new ValidationIssue
                {
                    Code = "NAM001",
                    Message = "Sequence has no name",
                    Severity = ValidationSeverity.Warning,
                    Category = Category,
                    Recommendation = "Provide a descriptive name for the sequence"
                });
            }

            // Check step names
            foreach (var step in sequence.Steps)
            {
                if (string.IsNullOrWhiteSpace(step.Name))
                {
                    result.Issues.Add(new ValidationIssue
                    {
                        Code = "NAM002",
                        Message = "Step has no name",
                        Severity = ValidationSeverity.Warning,
                        Category = Category,
                        StepId = step.Id.ToString(),
                        Recommendation = "Provide a descriptive name for the step"
                    });
                }
                else if (!_validNamePattern.IsMatch(step.Name.Replace(" ", "_")))
                {
                    result.Issues.Add(new ValidationIssue
                    {
                        Code = "NAM003",
                        Message = $"Step name '{step.Name}' contains invalid characters",
                        Severity = ValidationSeverity.Info,
                        Category = Category,
                        StepId = step.Id.ToString(),
                        StepName = step.Name,
                        Recommendation = "Use alphanumeric characters and underscores only"
                    });
                }

                // Check for very long names
                if (step.Name.Length > 100)
                {
                    result.Issues.Add(new ValidationIssue
                    {
                        Code = "NAM004",
                        Message = $"Step name '{step.Name.Substring(0, 50)}...' is too long",
                        Severity = ValidationSeverity.Info,
                        Category = Category,
                        StepId = step.Id.ToString(),
                        StepName = step.Name,
                        Recommendation = "Consider using shorter, more concise names"
                    });
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Validates limits configuration
    /// </summary>
    public class LimitsValidator : ISequenceValidator
    {
        public string Name => "Limits Validator";
        public string Description => "Validates limit configurations for measurement steps";
        public ValidationCategory Category => ValidationCategory.Structure;

        public ValidationResult Validate(Sequence sequence)
        {
            var result = new ValidationResult { ValidatorName = Name };

            foreach (var step in sequence.Steps)
            {
                var typeName = step.GetType().Name;

                if (typeName == "NumericLimitStep")
                {
                    ValidateNumericLimits(step, result);
                }
                else if (typeName == "StringValueStep")
                {
                    ValidateStringLimits(step, result);
                }
            }

            return result;
        }

        private void ValidateNumericLimits(TestStep step, ValidationResult result)
        {
            var lowLimitProp = step.GetType().GetProperty("LowLimit");
            var highLimitProp = step.GetType().GetProperty("HighLimit");

            if (lowLimitProp != null && highLimitProp != null)
            {
                var lowLimit = lowLimitProp.GetValue(step) as double?;
                var highLimit = highLimitProp.GetValue(step) as double?;

                if (lowLimit.HasValue && highLimit.HasValue && lowLimit > highLimit)
                {
                    result.Issues.Add(new ValidationIssue
                    {
                        Code = "LIM001",
                        Message = $"Step '{step.Name}' has low limit ({lowLimit}) greater than high limit ({highLimit})",
                        Severity = ValidationSeverity.Error,
                        Category = Category,
                        StepId = step.Id.ToString(),
                        StepName = step.Name,
                        Recommendation = "Ensure low limit is less than or equal to high limit"
                    });
                }

                if (!lowLimit.HasValue && !highLimit.HasValue)
                {
                    result.Issues.Add(new ValidationIssue
                    {
                        Code = "LIM002",
                        Message = $"Step '{step.Name}' has no limits defined",
                        Severity = ValidationSeverity.Warning,
                        Category = Category,
                        StepId = step.Id.ToString(),
                        StepName = step.Name,
                        Recommendation = "Consider defining limits for measurement validation"
                    });
                }
            }
        }

        private void ValidateStringLimits(TestStep step, ValidationResult result)
        {
            var expectedValueProp = step.GetType().GetProperty("ExpectedValue");

            if (expectedValueProp != null)
            {
                var expectedValue = expectedValueProp.GetValue(step) as string;

                if (string.IsNullOrEmpty(expectedValue))
                {
                    result.Issues.Add(new ValidationIssue
                    {
                        Code = "LIM003",
                        Message = $"Step '{step.Name}' has no expected value defined",
                        Severity = ValidationSeverity.Warning,
                        Category = Category,
                        StepId = step.Id.ToString(),
                        StepName = step.Name,
                        Recommendation = "Define an expected value for comparison"
                    });
                }
            }
        }
    }

    /// <summary>
    /// Manager for sequence validation
    /// </summary>
    public class SequenceValidationManager
    {
        private static readonly Lazy<SequenceValidationManager> _instance = 
            new(() => new SequenceValidationManager());
        
        public static SequenceValidationManager Instance => _instance.Value;

        private readonly List<ISequenceValidator> _validators = new();

        private SequenceValidationManager()
        {
            // Register built-in validators
            RegisterValidator(new StructureValidator());
            RegisterValidator(new LogicValidator());
            RegisterValidator(new NamingConventionValidator());
            RegisterValidator(new LimitsValidator());
        }

        /// <summary>
        /// Registers a validator
        /// </summary>
        public void RegisterValidator(ISequenceValidator validator)
        {
            _validators.Add(validator);
        }

        /// <summary>
        /// Unregisters a validator
        /// </summary>
        public void UnregisterValidator(string validatorName)
        {
            _validators.RemoveAll(v => v.Name == validatorName);
        }

        /// <summary>
        /// Gets all registered validators
        /// </summary>
        public IEnumerable<ISequenceValidator> GetValidators()
        {
            return _validators.AsReadOnly();
        }

        /// <summary>
        /// Validates a sequence with all validators
        /// </summary>
        public SequenceValidationReport ValidateSequence(Sequence sequence)
        {
            var report = new SequenceValidationReport
            {
                SequenceName = sequence.Name,
                StepCount = sequence.Steps.Count
            };

            foreach (var validator in _validators)
            {
                try
                {
                    var result = validator.Validate(sequence);
                    report.Results.Add(result);
                }
                catch (Exception ex)
                {
                    report.Results.Add(new ValidationResult
                    {
                        ValidatorName = validator.Name,
                        Issues = new List<ValidationIssue>
                        {
                            new()
                            {
                                Code = "VAL999",
                                Message = $"Validator failed: {ex.Message}",
                                Severity = ValidationSeverity.Error,
                                Category = validator.Category
                            }
                        }
                    });
                }
            }

            return report;
        }

        /// <summary>
        /// Validates with specific validators
        /// </summary>
        public SequenceValidationReport ValidateSequence(Sequence sequence, params ValidationCategory[] categories)
        {
            var selectedValidators = _validators.Where(v => categories.Contains(v.Category));
            var report = new SequenceValidationReport
            {
                SequenceName = sequence.Name,
                StepCount = sequence.Steps.Count
            };

            foreach (var validator in selectedValidators)
            {
                var result = validator.Validate(sequence);
                report.Results.Add(result);
            }

            return report;
        }
    }

    /// <summary>
    /// Complete validation report for a sequence
    /// </summary>
    public class SequenceValidationReport
    {
        public string SequenceName { get; set; } = string.Empty;
        public int StepCount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public List<ValidationResult> Results { get; set; } = new();
        
        public bool IsValid => Results.All(r => r.IsValid);
        public int TotalIssues => Results.Sum(r => r.Issues.Count);
        public int ErrorCount => Results.Sum(r => r.Issues.Count(i => i.Severity == ValidationSeverity.Error || i.Severity == ValidationSeverity.Critical));
        public int WarningCount => Results.Sum(r => r.Issues.Count(i => i.Severity == ValidationSeverity.Warning));
        public int InfoCount => Results.Sum(r => r.Issues.Count(i => i.Severity == ValidationSeverity.Info));

        /// <summary>
        /// Gets all issues across all validators
        /// </summary>
        public IEnumerable<ValidationIssue> GetAllIssues()
        {
            return Results.SelectMany(r => r.Issues);
        }

        /// <summary>
        /// Gets issues by severity
        /// </summary>
        public IEnumerable<ValidationIssue> GetIssuesBySeverity(ValidationSeverity severity)
        {
            return GetAllIssues().Where(i => i.Severity == severity);
        }

        /// <summary>
        /// Generates a summary string
        /// </summary>
        public string GetSummary()
        {
            return $"Validation Report for '{SequenceName}': " +
                   $"{(IsValid ? "VALID" : "INVALID")} - " +
                   $"{ErrorCount} errors, {WarningCount} warnings, {InfoCount} info";
        }
    }
}
