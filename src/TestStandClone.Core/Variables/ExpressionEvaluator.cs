using System.Text.RegularExpressions;

namespace TestStandClone.Core.Variables
{
    /// <summary>
    /// Evaluates expressions containing variable references and calculations.
    /// Similar to TestStand's expression evaluation system.
    /// </summary>
    public class ExpressionEvaluator
    {
        private readonly Context _context;
        private readonly VariableManager? _locals;
        private readonly VariableManager? _fileGlobals;

        /// <summary>
        /// Creates a new expression evaluator.
        /// </summary>
        public ExpressionEvaluator(Context context, VariableManager? locals = null, VariableManager? fileGlobals = null)
        {
            _context = context;
            _locals = locals;
            _fileGlobals = fileGlobals;
        }

        /// <summary>
        /// Evaluates an expression string and returns the result.
        /// Supports variable references like Locals.MyVar, FileGlobals.MyVar, StationGlobals.MyVar
        /// </summary>
        public object? Evaluate(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return null;
            }

            // Handle simple variable references
            var variablePattern = new Regex(@"(Locals|FileGlobals|StationGlobals|Step)\.(\w+)");
            
            string resolved = variablePattern.Replace(expression, match =>
            {
                string scope = match.Groups[1].Value;
                string varName = match.Groups[2].Value;
                
                object? value = ResolveVariable(scope, varName);
                return value?.ToString() ?? "null";
            });

            // Try to evaluate as a simple numeric expression
            if (TryEvaluateNumeric(resolved, out double numericResult))
            {
                return numericResult;
            }

            // Try to evaluate as a boolean expression
            if (TryEvaluateBoolean(resolved, out bool boolResult))
            {
                return boolResult;
            }

            // Return as string if no other evaluation worked
            return resolved;
        }

        /// <summary>
        /// Evaluates an expression and returns it as a specific type.
        /// </summary>
        public T? Evaluate<T>(string expression)
        {
            object? result = Evaluate(expression);
            
            if (result == null)
            {
                return default;
            }

            if (result is T typedResult)
            {
                return typedResult;
            }

            try
            {
                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Resolves a variable reference from the appropriate scope.
        /// </summary>
        private object? ResolveVariable(string scope, string varName)
        {
            return scope switch
            {
                "Locals" => _locals?.GetVariable(varName)?.Value ?? _context.GetValue<object>(varName),
                "FileGlobals" => _fileGlobals?.GetVariable(varName)?.Value,
                "StationGlobals" => VariableManager.StationGlobals.GetVariable(varName)?.Value,
                "Step" => _context.GetValue<object>(varName),
                _ => null
            };
        }

        /// <summary>
        /// Tries to evaluate a string as a numeric expression.
        /// Supports basic arithmetic: +, -, *, /
        /// </summary>
        private static bool TryEvaluateNumeric(string expression, out double result)
        {
            result = 0;
            
            // Trim whitespace but preserve inner spaces for string comparisons
            expression = expression.Trim();
            
            // Check if it's a simple number
            if (double.TryParse(expression, out result))
            {
                return true;
            }

            // For numeric expressions, remove spaces around operators
            string numericExpr = expression;
            
            // Try to evaluate simple arithmetic expressions
            try
            {
                // Handle addition
                if (numericExpr.Contains('+') && !numericExpr.StartsWith('+'))
                {
                    var parts = numericExpr.Split('+');
                    if (parts.Length == 2 && 
                        double.TryParse(parts[0].Trim(), out double left) && 
                        double.TryParse(parts[1].Trim(), out double right))
                    {
                         result = left + right;
                        return true;
                    }
                }

                // Handle subtraction (but not negative numbers)
                var subMatch = Regex.Match(numericExpr, @"^(.+)-(.+)$");
                if (subMatch.Success &&
                    double.TryParse(subMatch.Groups[1].Value.Trim(), out double subLeft) &&
                    double.TryParse(subMatch.Groups[2].Value.Trim(), out double subRight))
                {
                    result = subLeft - subRight;
                    return true;
                }

                // Handle multiplication
                if (numericExpr.Contains('*'))
                {
                    var parts = numericExpr.Split('*');
                    if (parts.Length == 2 &&
                        double.TryParse(parts[0].Trim(), out double mulLeft) &&
                        double.TryParse(parts[1].Trim(), out double mulRight))
                    {
                        result = mulLeft * mulRight;
                        return true;
                    }
                }

                // Handle division
                if (numericExpr.Contains('/'))
                {
                    var parts = numericExpr.Split('/');
                    if (parts.Length == 2 &&
                        double.TryParse(parts[0].Trim(), out double divLeft) &&
                        double.TryParse(parts[1].Trim(), out double divRight) &&
                        divRight != 0)
                    {
                        result = divLeft / divRight;
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to evaluate a string as a boolean expression.
        /// </summary>
        private static bool TryEvaluateBoolean(string expression, out bool result)
        {
            result = false;
            expression = expression.Trim().ToLower();

            if (expression == "true")
            {
                result = true;
                return true;
            }
            if (expression == "false")
            {
                result = false;
                return true;
            }

            // Handle comparison operators
            if (expression.Contains("=="))
            {
                var parts = expression.Split(new[] { "==" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    result = parts[0].Trim() == parts[1].Trim();
                    return true;
                }
            }

            if (expression.Contains("!="))
            {
                var parts = expression.Split(new[] { "!=" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    result = parts[0].Trim() != parts[1].Trim();
                    return true;
                }
            }

            if (expression.Contains(">="))
            {
                var parts = expression.Split(new[] { ">=" }, StringSplitOptions.None);
                if (parts.Length == 2 &&
                    double.TryParse(parts[0].Trim(), out double left) &&
                    double.TryParse(parts[1].Trim(), out double right))
                {
                    result = left >= right;
                    return true;
                }
            }

            if (expression.Contains("<="))
            {
                var parts = expression.Split(new[] { "<=" }, StringSplitOptions.None);
                if (parts.Length == 2 &&
                    double.TryParse(parts[0].Trim(), out double left) &&
                    double.TryParse(parts[1].Trim(), out double right))
                {
                    result = left <= right;
                    return true;
                }
            }

            if (expression.Contains(">") && !expression.Contains(">="))
            {
                var parts = expression.Split('>');
                if (parts.Length == 2 &&
                    double.TryParse(parts[0].Trim(), out double left) &&
                    double.TryParse(parts[1].Trim(), out double right))
                {
                    result = left > right;
                    return true;
                }
            }

            if (expression.Contains("<") && !expression.Contains("<="))
            {
                var parts = expression.Split('<');
                if (parts.Length == 2 &&
                    double.TryParse(parts[0].Trim(), out double left) &&
                    double.TryParse(parts[1].Trim(), out double right))
                {
                    result = left < right;
                    return true;
                }
            }

            return false;
        }
    }
}
