// =============================================================================
// FlowControlSteps.cs - If/Then/Else and Switch/Case flow control steps
// Part of TestStandClone.Core - TestStand-like test automation framework
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestStandClone.Core.Steps
{
    /// <summary>
    /// Step for conditional execution (If/Then/Else).
    /// Similar to TestStand's If step type.
    /// </summary>
    public class IfStep : TestStep
    {
        /// <summary>
        /// Gets or sets the condition expression.
        /// </summary>
        public string Condition { get; set; } = string.Empty;

        /// <summary>
        /// Gets the steps to execute if condition is true.
        /// </summary>
        public List<TestStep> ThenSteps { get; } = new();

        /// <summary>
        /// Gets the steps to execute if condition is false.
        /// </summary>
        public List<TestStep> ElseSteps { get; } = new();

        /// <summary>
        /// Gets or sets whether the condition was true.
        /// </summary>
        public bool ConditionResult { get; private set; }

        /// <summary>
        /// Creates a new IfStep with the given name.
        /// </summary>
        public IfStep(string name, string condition)
        {
            Name = name;
            Condition = condition;
        }

        /// <summary>
        /// Evaluates condition and executes appropriate branch.
        /// </summary>
        public override async Task ExecuteAsync(Context context)
        {
            try
            {
                // Evaluate condition
                ConditionResult = EvaluateCondition(context);
                
                var stepsToExecute = ConditionResult ? ThenSteps : ElseSteps;
                
                foreach (var step in stepsToExecute)
                {
                    step.Status = StepStatus.Running;
                    await step.ExecuteAsync(context);
                    
                    if (step.Status == StepStatus.Failed || step.Status == StepStatus.Error)
                    {
                        Status = step.Status;
                        ResultText = $"Branch step failed: {step.Name}";
                        return;
                    }
                }

                Status = StepStatus.Passed;
                ResultText = ConditionResult 
                    ? $"Then branch executed ({ThenSteps.Count} steps)" 
                    : $"Else branch executed ({ElseSteps.Count} steps)";
            }
            catch (Exception ex)
            {
                Status = StepStatus.Error;
                ResultText = $"If step error: {ex.Message}";
            }
        }

        private bool EvaluateCondition(Context context)
        {
            // Simple condition evaluation
            // Supports: ==, !=, <, >, <=, >=, contains, startswith, endswith
            
            string condition = Condition.Trim();
            
            // Handle boolean literals
            if (condition.Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;
            if (condition.Equals("false", StringComparison.OrdinalIgnoreCase))
                return false;

            // Handle variable reference
            if (context.Data.TryGetValue(condition, out var value))
            {
                if (value is bool b) return b;
                if (value is int i) return i != 0;
                if (value is double d) return d != 0;
                if (value is string s) return !string.IsNullOrEmpty(s);
                return value != null;
            }

            // Handle comparison operators
            foreach (var op in new[] { "==", "!=", "<=", ">=", "<", ">" })
            {
                int idx = condition.IndexOf(op, StringComparison.Ordinal);
                if (idx > 0)
                {
                    string left = ResolveValue(condition[..idx].Trim(), context);
                    string right = ResolveValue(condition[(idx + op.Length)..].Trim(), context);
                    
                    return op switch
                    {
                        "==" => left.Equals(right, StringComparison.OrdinalIgnoreCase),
                        "!=" => !left.Equals(right, StringComparison.OrdinalIgnoreCase),
                        "<" => CompareNumeric(left, right) < 0,
                        ">" => CompareNumeric(left, right) > 0,
                        "<=" => CompareNumeric(left, right) <= 0,
                        ">=" => CompareNumeric(left, right) >= 0,
                        _ => false
                    };
                }
            }

            // Handle string operations
            if (condition.Contains(".contains(", StringComparison.OrdinalIgnoreCase))
            {
                var parts = condition.Split(new[] { ".contains(" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string left = ResolveValue(parts[0].Trim(), context);
                    string right = ResolveValue(parts[1].TrimEnd(')').Trim(' ', '"', '\''), context);
                    return left.Contains(right, StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        private string ResolveValue(string value, Context context)
        {
            value = value.Trim(' ', '"', '\'');
            if (context.Data.TryGetValue(value, out var resolved))
                return resolved?.ToString() ?? string.Empty;
            return value;
        }

        private static int CompareNumeric(string left, string right)
        {
            if (double.TryParse(left, out double l) && double.TryParse(right, out double r))
                return l.CompareTo(r);
            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Adds a step to the Then branch.
        /// </summary>
        public IfStep AddThenStep(TestStep step)
        {
            ThenSteps.Add(step);
            return this;
        }

        /// <summary>
        /// Adds a step to the Else branch.
        /// </summary>
        public IfStep AddElseStep(TestStep step)
        {
            ElseSteps.Add(step);
            return this;
        }
    }

    /// <summary>
    /// Step for switch/case selection.
    /// Similar to TestStand's Select step type.
    /// </summary>
    public class SwitchStep : TestStep
    {
        /// <summary>
        /// Gets or sets the expression to evaluate.
        /// </summary>
        public string Expression { get; set; } = string.Empty;

        /// <summary>
        /// Gets the cases and their steps.
        /// </summary>
        public Dictionary<string, List<TestStep>> Cases { get; } = new();

        /// <summary>
        /// Gets the default case steps.
        /// </summary>
        public List<TestStep> DefaultCase { get; } = new();

        /// <summary>
        /// Gets the matched case value.
        /// </summary>
        public string? MatchedCase { get; private set; }

        /// <summary>
        /// Creates a new SwitchStep with the given name.
        /// </summary>
        public SwitchStep(string name, string expression)
        {
            Name = name;
            Expression = expression;
        }

        /// <summary>
        /// Evaluates expression and executes matching case.
        /// </summary>
        public override async Task ExecuteAsync(Context context)
        {
            try
            {
                // Get expression value
                string value = ResolveValue(Expression, context);
                List<TestStep>? stepsToExecute = null;

                // Find matching case
                foreach (var kvp in Cases)
                {
                    string caseValue = ResolveValue(kvp.Key, context);
                    if (value.Equals(caseValue, StringComparison.OrdinalIgnoreCase))
                    {
                        MatchedCase = kvp.Key;
                        stepsToExecute = kvp.Value;
                        break;
                    }
                }

                // Use default if no match
                if (stepsToExecute == null)
                {
                    MatchedCase = "default";
                    stepsToExecute = DefaultCase;
                }

                // Execute matched case
                foreach (var step in stepsToExecute)
                {
                    step.Status = StepStatus.Running;
                    await step.ExecuteAsync(context);
                    
                    if (step.Status == StepStatus.Failed || step.Status == StepStatus.Error)
                    {
                        Status = step.Status;
                        ResultText = $"Case step failed: {step.Name}";
                        return;
                    }
                }

                Status = StepStatus.Passed;
                ResultText = $"Case '{MatchedCase}' executed ({stepsToExecute.Count} steps)";
            }
            catch (Exception ex)
            {
                Status = StepStatus.Error;
                ResultText = $"Switch step error: {ex.Message}";
            }
        }

        private string ResolveValue(string value, Context context)
        {
            value = value.Trim();
            if (context.Data.TryGetValue(value, out var resolved))
                return resolved?.ToString() ?? string.Empty;
            return value;
        }

        /// <summary>
        /// Adds a case with steps.
        /// </summary>
        public SwitchStep AddCase(string caseValue, params TestStep[] steps)
        {
            if (!Cases.ContainsKey(caseValue))
                Cases[caseValue] = new List<TestStep>();
            Cases[caseValue].AddRange(steps);
            return this;
        }

        /// <summary>
        /// Adds a step to the default case.
        /// </summary>
        public SwitchStep AddDefaultStep(TestStep step)
        {
            DefaultCase.Add(step);
            return this;
        }
    }

    /// <summary>
    /// Step for For loop execution.
    /// Similar to TestStand's For loop.
    /// </summary>
    public class ForLoopStep : TestStep
    {
        /// <summary>
        /// Gets or sets the loop variable name.
        /// </summary>
        public string VariableName { get; set; } = "i";

        /// <summary>
        /// Gets or sets the start value.
        /// </summary>
        public int StartValue { get; set; } = 0;

        /// <summary>
        /// Gets or sets the end value (exclusive).
        /// </summary>
        public int EndValue { get; set; } = 10;

        /// <summary>
        /// Gets or sets the increment value.
        /// </summary>
        public int Increment { get; set; } = 1;

        /// <summary>
        /// Gets the steps to execute in each iteration.
        /// </summary>
        public List<TestStep> LoopSteps { get; } = new();

        /// <summary>
        /// Gets the current iteration count.
        /// </summary>
        public int CurrentIteration { get; private set; }

        /// <summary>
        /// Creates a new ForLoopStep with the given name.
        /// </summary>
        public ForLoopStep(string name, int startValue = 0, int endValue = 10, int increment = 1)
        {
            Name = name;
            StartValue = startValue;
            EndValue = endValue;
            Increment = increment;
        }

        /// <summary>
        /// Executes the loop.
        /// </summary>
        public override async Task ExecuteAsync(Context context)
        {
            try
            {
                CurrentIteration = 0;
                
                for (int i = StartValue; i < EndValue; i += Increment)
                {
                    CurrentIteration++;
                    context.Data[VariableName] = i;
                    
                    foreach (var step in LoopSteps)
                    {
                        step.Status = StepStatus.Running;
                        await step.ExecuteAsync(context);
                        
                        if (step.Status == StepStatus.Failed || step.Status == StepStatus.Error)
                        {
                            Status = step.Status;
                            ResultText = $"Loop step failed at iteration {CurrentIteration}: {step.Name}";
                            return;
                        }
                    }
                }

                Status = StepStatus.Passed;
                ResultText = $"Loop completed: {CurrentIteration} iterations";
            }
            catch (Exception ex)
            {
                Status = StepStatus.Error;
                ResultText = $"For loop error: {ex.Message}";
            }
        }

        /// <summary>
        /// Adds a step to the loop body.
        /// </summary>
        public ForLoopStep AddLoopStep(TestStep step)
        {
            LoopSteps.Add(step);
            return this;
        }
    }

    /// <summary>
    /// Step for While loop execution.
    /// Similar to TestStand's While loop.
    /// </summary>
    public class WhileLoopStep : TestStep
    {
        /// <summary>
        /// Gets or sets the condition expression.
        /// </summary>
        public string Condition { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum iterations (safety limit).
        /// </summary>
        public int MaxIterations { get; set; } = 1000;

        /// <summary>
        /// Gets the steps to execute in each iteration.
        /// </summary>
        public List<TestStep> LoopSteps { get; } = new();

        /// <summary>
        /// Gets the current iteration count.
        /// </summary>
        public int CurrentIteration { get; private set; }

        /// <summary>
        /// Creates a new WhileLoopStep with the given name.
        /// </summary>
        public WhileLoopStep(string name, string condition)
        {
            Name = name;
            Condition = condition;
        }

        /// <summary>
        /// Executes the while loop.
        /// </summary>
        public override async Task ExecuteAsync(Context context)
        {
            try
            {
                CurrentIteration = 0;
                
                while (EvaluateCondition(context) && CurrentIteration < MaxIterations)
                {
                    CurrentIteration++;
                    context.Data["WhileIteration"] = CurrentIteration;
                    
                    foreach (var step in LoopSteps)
                    {
                        step.Status = StepStatus.Running;
                        await step.ExecuteAsync(context);
                        
                        if (step.Status == StepStatus.Failed || step.Status == StepStatus.Error)
                        {
                            Status = step.Status;
                            ResultText = $"Loop step failed at iteration {CurrentIteration}: {step.Name}";
                            return;
                        }
                    }
                }

                if (CurrentIteration >= MaxIterations)
                {
                    Status = StepStatus.Failed;
                    ResultText = $"Loop exceeded maximum iterations ({MaxIterations})";
                }
                else
                {
                    Status = StepStatus.Passed;
                    ResultText = $"While loop completed: {CurrentIteration} iterations";
                }
            }
            catch (Exception ex)
            {
                Status = StepStatus.Error;
                ResultText = $"While loop error: {ex.Message}";
            }
        }

        private bool EvaluateCondition(Context context)
        {
            // Simple condition evaluation (same as IfStep)
            string condition = Condition.Trim();
            
            if (condition.Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;
            if (condition.Equals("false", StringComparison.OrdinalIgnoreCase))
                return false;

            if (context.Data.TryGetValue(condition, out var value))
            {
                if (value is bool b) return b;
                if (value is int i) return i != 0;
                if (value is double d) return d != 0;
                return value != null;
            }

            return false;
        }

        /// <summary>
        /// Adds a step to the loop body.
        /// </summary>
        public WhileLoopStep AddLoopStep(TestStep step)
        {
            LoopSteps.Add(step);
            return this;
        }
    }

    /// <summary>
    /// Step for Do-While loop execution.
    /// </summary>
    public class DoWhileLoopStep : TestStep
    {
        /// <summary>
        /// Gets or sets the condition expression (evaluated after each iteration).
        /// </summary>
        public string Condition { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum iterations (safety limit).
        /// </summary>
        public int MaxIterations { get; set; } = 1000;

        /// <summary>
        /// Gets the steps to execute in each iteration.
        /// </summary>
        public List<TestStep> LoopSteps { get; } = new();

        /// <summary>
        /// Gets the current iteration count.
        /// </summary>
        public int CurrentIteration { get; private set; }

        /// <summary>
        /// Creates a new DoWhileLoopStep with the given name.
        /// </summary>
        public DoWhileLoopStep(string name, string condition)
        {
            Name = name;
            Condition = condition;
        }

        /// <summary>
        /// Executes the do-while loop.
        /// </summary>
        public override async Task ExecuteAsync(Context context)
        {
            try
            {
                CurrentIteration = 0;
                
                do
                {
                    CurrentIteration++;
                    context.Data["DoWhileIteration"] = CurrentIteration;
                    
                    foreach (var step in LoopSteps)
                    {
                        step.Status = StepStatus.Running;
                        await step.ExecuteAsync(context);
                        
                        if (step.Status == StepStatus.Failed || step.Status == StepStatus.Error)
                        {
                            Status = step.Status;
                            ResultText = $"Loop step failed at iteration {CurrentIteration}: {step.Name}";
                            return;
                        }
                    }
                } while (EvaluateCondition(context) && CurrentIteration < MaxIterations);

                if (CurrentIteration >= MaxIterations)
                {
                    Status = StepStatus.Failed;
                    ResultText = $"Loop exceeded maximum iterations ({MaxIterations})";
                }
                else
                {
                    Status = StepStatus.Passed;
                    ResultText = $"Do-While loop completed: {CurrentIteration} iterations";
                }
            }
            catch (Exception ex)
            {
                Status = StepStatus.Error;
                ResultText = $"Do-While loop error: {ex.Message}";
            }
        }

        private bool EvaluateCondition(Context context)
        {
            string condition = Condition.Trim();
            
            if (condition.Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;
            if (condition.Equals("false", StringComparison.OrdinalIgnoreCase))
                return false;

            if (context.Data.TryGetValue(condition, out var value))
            {
                if (value is bool b) return b;
                if (value is int i) return i != 0;
                if (value is double d) return d != 0;
                return value != null;
            }

            return false;
        }

        /// <summary>
        /// Adds a step to the loop body.
        /// </summary>
        public DoWhileLoopStep AddLoopStep(TestStep step)
        {
            LoopSteps.Add(step);
            return this;
        }
    }

    /// <summary>
    /// Step for ForEach loop execution.
    /// </summary>
    public class ForEachStep : TestStep
    {
        /// <summary>
        /// Gets or sets the variable name for current item.
        /// </summary>
        public string ItemVariable { get; set; } = "item";

        /// <summary>
        /// Gets or sets the collection variable name.
        /// </summary>
        public string CollectionVariable { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the index variable name.
        /// </summary>
        public string IndexVariable { get; set; } = "index";

        /// <summary>
        /// Gets the steps to execute for each item.
        /// </summary>
        public List<TestStep> LoopSteps { get; } = new();

        /// <summary>
        /// Gets the current iteration count.
        /// </summary>
        public int CurrentIteration { get; private set; }

        /// <summary>
        /// Creates a new ForEachStep with the given name.
        /// </summary>
        public ForEachStep(string name, string collectionVariable)
        {
            Name = name;
            CollectionVariable = collectionVariable;
        }

        /// <summary>
        /// Executes the foreach loop.
        /// </summary>
        public override async Task ExecuteAsync(Context context)
        {
            try
            {
                CurrentIteration = 0;
                
                if (!context.Data.TryGetValue(CollectionVariable, out var collection))
                {
                    Status = StepStatus.Failed;
                    ResultText = $"Collection not found: {CollectionVariable}";
                    return;
                }

                IEnumerable<object> items;
                if (collection is System.Collections.IEnumerable enumerable)
                    items = enumerable.Cast<object>();
                else
                {
                    Status = StepStatus.Failed;
                    ResultText = $"Variable is not a collection: {CollectionVariable}";
                    return;
                }

                foreach (var item in items)
                {
                    context.Data[ItemVariable] = item;
                    context.Data[IndexVariable] = CurrentIteration;
                    
                    foreach (var step in LoopSteps)
                    {
                        step.Status = StepStatus.Running;
                        await step.ExecuteAsync(context);
                        
                        if (step.Status == StepStatus.Failed || step.Status == StepStatus.Error)
                        {
                            Status = step.Status;
                            ResultText = $"Loop step failed at index {CurrentIteration}: {step.Name}";
                            return;
                        }
                    }
                    
                    CurrentIteration++;
                }

                Status = StepStatus.Passed;
                ResultText = $"ForEach loop completed: {CurrentIteration} items";
            }
            catch (Exception ex)
            {
                Status = StepStatus.Error;
                ResultText = $"ForEach loop error: {ex.Message}";
            }
        }

        /// <summary>
        /// Adds a step to the loop body.
        /// </summary>
        public ForEachStep AddLoopStep(TestStep step)
        {
            LoopSteps.Add(step);
            return this;
        }
    }
}
