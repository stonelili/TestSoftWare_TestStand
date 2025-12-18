// =============================================================================
// ExpressionStep.cs - Multi-line expression execution step
// Part of TestStandClone.Core - TestStand-like test automation framework
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestStandClone.Core.Steps
{
    /// <summary>
    /// Step that evaluates one or more expressions.
    /// Similar to TestStand's Expression step type.
    /// </summary>
    public class ExpressionStep : TestStep
    {
        private readonly List<string> _expressions = new();

        /// <summary>
        /// Gets or sets the expressions to evaluate.
        /// </summary>
        public List<string> Expressions => _expressions;

        /// <summary>
        /// Gets or sets whether to stop on first error.
        /// </summary>
        public bool StopOnError { get; set; } = true;

        /// <summary>
        /// Gets the results of each expression evaluation.
        /// </summary>
        public List<ExpressionResult> Results { get; } = new();

        /// <summary>
        /// Creates a new ExpressionStep with the given name.
        /// </summary>
        public ExpressionStep(string name, params string[] expressions)
        {
            Name = name;
            if (expressions != null)
            {
                _expressions.AddRange(expressions);
            }
        }

        /// <summary>
        /// Executes all expressions in order.
        /// </summary>
        public override async Task ExecuteAsync(Context context)
        {
            Results.Clear();
            var evaluator = new Variables.ExpressionEvaluator(context);

            try
            {
                foreach (var expression in _expressions)
                {
                    if (string.IsNullOrWhiteSpace(expression))
                        continue;

                    var result = new ExpressionResult { Expression = expression };

                    try
                    {
                        // Try to evaluate the expression
                        result.Value = evaluator.Evaluate(expression);
                        result.Success = true;
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.Error = ex.Message;

                        if (StopOnError)
                        {
                            Results.Add(result);
                            Status = StepStatus.Failed;
                            ResultText = $"Expression error: {ex.Message}";
                            return;
                        }
                    }

                    Results.Add(result);
                }

                // Check if any expression failed
                bool allPassed = Results.TrueForAll(r => r.Success);
                Status = allPassed ? StepStatus.Passed : StepStatus.Failed;
                ResultText = allPassed 
                    ? $"Evaluated {Results.Count} expression(s)" 
                    : $"Some expressions failed";

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Status = StepStatus.Error;
                ResultText = $"Expression step error: {ex.Message}";
            }
        }

        /// <summary>
        /// Adds an expression to evaluate.
        /// </summary>
        public ExpressionStep AddExpression(string expression)
        {
            _expressions.Add(expression);
            return this;
        }
    }

    /// <summary>
    /// Result of a single expression evaluation.
    /// </summary>
    public class ExpressionResult
    {
        public string Expression { get; set; } = string.Empty;
        public object? Value { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}
