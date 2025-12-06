// Copyright (c) TestStand Clone. All rights reserved.
// Expression builder for creating expressions interactively

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestStandClone.Core.ExpressionBuilder
{
    /// <summary>
    /// Expression node types
    /// </summary>
    public enum ExpressionNodeType
    {
        Literal,
        Variable,
        Property,
        Operator,
        Function,
        Group
    }

    /// <summary>
    /// Expression operators
    /// </summary>
    public enum ExpressionOperator
    {
        // Arithmetic
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        Power,
        
        // Comparison
        Equal,
        NotEqual,
        LessThan,
        LessOrEqual,
        GreaterThan,
        GreaterOrEqual,
        
        // Logical
        And,
        Or,
        Not,
        
        // String
        Concat,
        Contains,
        StartsWith,
        EndsWith
    }

    /// <summary>
    /// Represents a node in an expression tree
    /// </summary>
    public class ExpressionNode
    {
        /// <summary>
        /// Type of node
        /// </summary>
        public ExpressionNodeType NodeType { get; set; }

        /// <summary>
        /// Value for literals
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Variable name for variable nodes
        /// </summary>
        public string? VariableName { get; set; }

        /// <summary>
        /// Property path for property nodes
        /// </summary>
        public string? PropertyPath { get; set; }

        /// <summary>
        /// Operator for operator nodes
        /// </summary>
        public ExpressionOperator? Operator { get; set; }

        /// <summary>
        /// Function name for function nodes
        /// </summary>
        public string? FunctionName { get; set; }

        /// <summary>
        /// Child nodes
        /// </summary>
        public List<ExpressionNode> Children { get; } = new List<ExpressionNode>();

        /// <summary>
        /// Create a literal node
        /// </summary>
        public static ExpressionNode Literal(object value)
        {
            return new ExpressionNode
            {
                NodeType = ExpressionNodeType.Literal,
                Value = value
            };
        }

        /// <summary>
        /// Create a variable node
        /// </summary>
        public static ExpressionNode Variable(string name)
        {
            return new ExpressionNode
            {
                NodeType = ExpressionNodeType.Variable,
                VariableName = name
            };
        }

        /// <summary>
        /// Create a property node
        /// </summary>
        public static ExpressionNode Property(string path)
        {
            return new ExpressionNode
            {
                NodeType = ExpressionNodeType.Property,
                PropertyPath = path
            };
        }

        /// <summary>
        /// Create an operator node
        /// </summary>
        public static ExpressionNode Op(ExpressionOperator op, params ExpressionNode[] operands)
        {
            var node = new ExpressionNode
            {
                NodeType = ExpressionNodeType.Operator,
                Operator = op
            };
            node.Children.AddRange(operands);
            return node;
        }

        /// <summary>
        /// Create a function node
        /// </summary>
        public static ExpressionNode Function(string name, params ExpressionNode[] args)
        {
            var node = new ExpressionNode
            {
                NodeType = ExpressionNodeType.Function,
                FunctionName = name
            };
            node.Children.AddRange(args);
            return node;
        }

        /// <summary>
        /// Create a group node (parentheses)
        /// </summary>
        public static ExpressionNode Group(ExpressionNode inner)
        {
            var node = new ExpressionNode
            {
                NodeType = ExpressionNodeType.Group
            };
            node.Children.Add(inner);
            return node;
        }
    }

    /// <summary>
    /// Available expression functions
    /// </summary>
    public class ExpressionFunction
    {
        /// <summary>
        /// Function name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the function
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Category for grouping
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Parameter names
        /// </summary>
        public List<string> Parameters { get; } = new List<string>();

        /// <summary>
        /// Return type
        /// </summary>
        public string ReturnType { get; set; } = "Object";

        /// <summary>
        /// Example usage
        /// </summary>
        public string Example { get; set; } = string.Empty;
    }

    /// <summary>
    /// Available variable or property for expression building
    /// </summary>
    public class ExpressionItem
    {
        /// <summary>
        /// Name of the item
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Full path (for nested properties)
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Data type
        /// </summary>
        public string DataType { get; set; } = "Object";

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Category
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Whether this item has children (nested properties)
        /// </summary>
        public bool HasChildren { get; set; }

        /// <summary>
        /// Child items
        /// </summary>
        public List<ExpressionItem> Children { get; } = new List<ExpressionItem>();
    }

    /// <summary>
    /// Provides helpers for building expressions
    /// </summary>
    public sealed class ExpressionBuilderHelper
    {
        private static readonly Lazy<ExpressionBuilderHelper> _instance = 
            new Lazy<ExpressionBuilderHelper>(() => new ExpressionBuilderHelper());

        private readonly List<ExpressionFunction> _functions = new List<ExpressionFunction>();
        private readonly List<ExpressionItem> _commonVariables = new List<ExpressionItem>();

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static ExpressionBuilderHelper Instance => _instance.Value;

        /// <summary>
        /// Available functions
        /// </summary>
        public IReadOnlyList<ExpressionFunction> Functions => _functions.AsReadOnly();

        /// <summary>
        /// Common variables
        /// </summary>
        public IReadOnlyList<ExpressionItem> CommonVariables => _commonVariables.AsReadOnly();

        private ExpressionBuilderHelper()
        {
            InitializeFunctions();
            InitializeCommonVariables();
        }

        private void InitializeFunctions()
        {
            // Math functions
            _functions.Add(new ExpressionFunction
            {
                Name = "Abs",
                Category = "Math",
                Description = "Returns the absolute value",
                Parameters = { "value" },
                ReturnType = "Number",
                Example = "Abs(-5) = 5"
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "Round",
                Category = "Math",
                Description = "Rounds to specified decimal places",
                Parameters = { "value", "decimals" },
                ReturnType = "Number",
                Example = "Round(3.14159, 2) = 3.14"
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "Floor",
                Category = "Math",
                Description = "Rounds down to nearest integer",
                Parameters = { "value" },
                ReturnType = "Number",
                Example = "Floor(3.7) = 3"
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "Ceiling",
                Category = "Math",
                Description = "Rounds up to nearest integer",
                Parameters = { "value" },
                ReturnType = "Number",
                Example = "Ceiling(3.2) = 4"
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "Sqrt",
                Category = "Math",
                Description = "Returns the square root",
                Parameters = { "value" },
                ReturnType = "Number",
                Example = "Sqrt(16) = 4"
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "Min",
                Category = "Math",
                Description = "Returns the minimum value",
                Parameters = { "a", "b" },
                ReturnType = "Number",
                Example = "Min(5, 3) = 3"
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "Max",
                Category = "Math",
                Description = "Returns the maximum value",
                Parameters = { "a", "b" },
                ReturnType = "Number",
                Example = "Max(5, 3) = 5"
            });

            // String functions
            _functions.Add(new ExpressionFunction
            {
                Name = "Length",
                Category = "String",
                Description = "Returns the string length",
                Parameters = { "text" },
                ReturnType = "Number",
                Example = "Length(\"Hello\") = 5"
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "ToUpper",
                Category = "String",
                Description = "Converts to uppercase",
                Parameters = { "text" },
                ReturnType = "String",
                Example = "ToUpper(\"hello\") = \"HELLO\""
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "ToLower",
                Category = "String",
                Description = "Converts to lowercase",
                Parameters = { "text" },
                ReturnType = "String",
                Example = "ToLower(\"HELLO\") = \"hello\""
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "Substring",
                Category = "String",
                Description = "Extracts a portion of the string",
                Parameters = { "text", "startIndex", "length" },
                ReturnType = "String",
                Example = "Substring(\"Hello\", 0, 2) = \"He\""
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "Trim",
                Category = "String",
                Description = "Removes leading and trailing whitespace",
                Parameters = { "text" },
                ReturnType = "String",
                Example = "Trim(\" Hello \") = \"Hello\""
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "Replace",
                Category = "String",
                Description = "Replaces occurrences of a substring",
                Parameters = { "text", "oldValue", "newValue" },
                ReturnType = "String",
                Example = "Replace(\"Hello\", \"l\", \"L\") = \"HeLLo\""
            });

            // Logical functions
            _functions.Add(new ExpressionFunction
            {
                Name = "If",
                Category = "Logical",
                Description = "Returns value based on condition",
                Parameters = { "condition", "trueValue", "falseValue" },
                ReturnType = "Object",
                Example = "If(x > 5, \"High\", \"Low\")"
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "IsNull",
                Category = "Logical",
                Description = "Checks if value is null",
                Parameters = { "value" },
                ReturnType = "Boolean",
                Example = "IsNull(x)"
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "Coalesce",
                Category = "Logical",
                Description = "Returns first non-null value",
                Parameters = { "value1", "value2" },
                ReturnType = "Object",
                Example = "Coalesce(x, 0)"
            });

            // Date/Time functions
            _functions.Add(new ExpressionFunction
            {
                Name = "Now",
                Category = "DateTime",
                Description = "Returns current date and time",
                Parameters = { },
                ReturnType = "DateTime",
                Example = "Now()"
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "Today",
                Category = "DateTime",
                Description = "Returns current date",
                Parameters = { },
                ReturnType = "DateTime",
                Example = "Today()"
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "DateDiff",
                Category = "DateTime",
                Description = "Returns difference between dates",
                Parameters = { "date1", "date2", "unit" },
                ReturnType = "Number",
                Example = "DateDiff(StartTime, EndTime, \"seconds\")"
            });

            // Conversion functions
            _functions.Add(new ExpressionFunction
            {
                Name = "ToNumber",
                Category = "Conversion",
                Description = "Converts to number",
                Parameters = { "value" },
                ReturnType = "Number",
                Example = "ToNumber(\"123\") = 123"
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "ToString",
                Category = "Conversion",
                Description = "Converts to string",
                Parameters = { "value" },
                ReturnType = "String",
                Example = "ToString(123) = \"123\""
            });
            
            _functions.Add(new ExpressionFunction
            {
                Name = "ToBool",
                Category = "Conversion",
                Description = "Converts to boolean",
                Parameters = { "value" },
                ReturnType = "Boolean",
                Example = "ToBool(1) = true"
            });
        }

        private void InitializeCommonVariables()
        {
            // Step properties
            var stepProps = new ExpressionItem
            {
                Name = "Step",
                Path = "Step",
                Category = "Step",
                Description = "Current step properties",
                HasChildren = true
            };
            stepProps.Children.Add(new ExpressionItem { Name = "Name", Path = "Step.Name", DataType = "String" });
            stepProps.Children.Add(new ExpressionItem { Name = "Status", Path = "Step.Status", DataType = "StepStatus" });
            stepProps.Children.Add(new ExpressionItem { Name = "ResultText", Path = "Step.ResultText", DataType = "String" });
            stepProps.Children.Add(new ExpressionItem { Name = "ExecutionTime", Path = "Step.ExecutionTime", DataType = "TimeSpan" });
            _commonVariables.Add(stepProps);

            // Sequence properties
            var seqProps = new ExpressionItem
            {
                Name = "Sequence",
                Path = "Sequence",
                Category = "Sequence",
                Description = "Current sequence properties",
                HasChildren = true
            };
            seqProps.Children.Add(new ExpressionItem { Name = "Name", Path = "Sequence.Name", DataType = "String" });
            seqProps.Children.Add(new ExpressionItem { Name = "StepCount", Path = "Sequence.Steps.Count", DataType = "Number" });
            _commonVariables.Add(seqProps);

            // RunState properties
            var runState = new ExpressionItem
            {
                Name = "RunState",
                Path = "RunState",
                Category = "Execution",
                Description = "Current run state",
                HasChildren = true
            };
            runState.Children.Add(new ExpressionItem { Name = "SerialNumber", Path = "RunState.SerialNumber", DataType = "String" });
            runState.Children.Add(new ExpressionItem { Name = "TestSocketIndex", Path = "RunState.TestSocketIndex", DataType = "Number" });
            runState.Children.Add(new ExpressionItem { Name = "LoopIndex", Path = "RunState.LoopIndex", DataType = "Number" });
            _commonVariables.Add(runState);
        }

        /// <summary>
        /// Convert an expression node to string
        /// </summary>
        public string NodeToString(ExpressionNode node)
        {
            return node.NodeType switch
            {
                ExpressionNodeType.Literal => FormatLiteral(node.Value),
                ExpressionNodeType.Variable => node.VariableName ?? "",
                ExpressionNodeType.Property => node.PropertyPath ?? "",
                ExpressionNodeType.Operator => FormatOperator(node),
                ExpressionNodeType.Function => FormatFunction(node),
                ExpressionNodeType.Group => $"({NodeToString(node.Children.FirstOrDefault()!)})",
                _ => ""
            };
        }

        private string FormatLiteral(object? value)
        {
            if (value == null) return "null";
            if (value is string s) return $"\"{s}\"";
            if (value is bool b) return b ? "true" : "false";
            return value.ToString() ?? "";
        }

        private string FormatOperator(ExpressionNode node)
        {
            var op = node.Operator;
            var children = node.Children;
            
            if (children.Count == 1 && op == ExpressionOperator.Not)
                return $"!{NodeToString(children[0])}";
            
            if (children.Count != 2)
                return "";
            
            var left = NodeToString(children[0]);
            var right = NodeToString(children[1]);
            
            var opStr = op switch
            {
                ExpressionOperator.Add => "+",
                ExpressionOperator.Subtract => "-",
                ExpressionOperator.Multiply => "*",
                ExpressionOperator.Divide => "/",
                ExpressionOperator.Modulo => "%",
                ExpressionOperator.Power => "^",
                ExpressionOperator.Equal => "==",
                ExpressionOperator.NotEqual => "!=",
                ExpressionOperator.LessThan => "<",
                ExpressionOperator.LessOrEqual => "<=",
                ExpressionOperator.GreaterThan => ">",
                ExpressionOperator.GreaterOrEqual => ">=",
                ExpressionOperator.And => "&&",
                ExpressionOperator.Or => "||",
                ExpressionOperator.Concat => "+",
                ExpressionOperator.Contains => ".Contains",
                ExpressionOperator.StartsWith => ".StartsWith",
                ExpressionOperator.EndsWith => ".EndsWith",
                _ => "?"
            };
            
            if (op == ExpressionOperator.Contains || 
                op == ExpressionOperator.StartsWith || 
                op == ExpressionOperator.EndsWith)
            {
                return $"{left}{opStr}({right})";
            }
            
            return $"{left} {opStr} {right}";
        }

        private string FormatFunction(ExpressionNode node)
        {
            var name = node.FunctionName ?? "";
            var args = string.Join(", ", node.Children.Select(NodeToString));
            return $"{name}({args})";
        }

        /// <summary>
        /// Get operator string representation
        /// </summary>
        public string GetOperatorString(ExpressionOperator op)
        {
            return op switch
            {
                ExpressionOperator.Add => "+",
                ExpressionOperator.Subtract => "-",
                ExpressionOperator.Multiply => "*",
                ExpressionOperator.Divide => "/",
                ExpressionOperator.Modulo => "%",
                ExpressionOperator.Power => "^",
                ExpressionOperator.Equal => "==",
                ExpressionOperator.NotEqual => "!=",
                ExpressionOperator.LessThan => "<",
                ExpressionOperator.LessOrEqual => "<=",
                ExpressionOperator.GreaterThan => ">",
                ExpressionOperator.GreaterOrEqual => ">=",
                ExpressionOperator.And => "&&",
                ExpressionOperator.Or => "||",
                ExpressionOperator.Not => "!",
                ExpressionOperator.Concat => "+",
                ExpressionOperator.Contains => "Contains",
                ExpressionOperator.StartsWith => "StartsWith",
                ExpressionOperator.EndsWith => "EndsWith",
                _ => ""
            };
        }

        /// <summary>
        /// Get functions by category
        /// </summary>
        public IEnumerable<ExpressionFunction> GetFunctionsByCategory(string category)
        {
            return _functions.Where(f => f.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get all function categories
        /// </summary>
        public IEnumerable<string> GetFunctionCategories()
        {
            return _functions.Select(f => f.Category).Distinct().OrderBy(c => c);
        }

        /// <summary>
        /// Validate an expression
        /// </summary>
        public (bool isValid, string? error) ValidateExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return (false, "Expression cannot be empty");
            
            // Basic validation
            int parens = 0;
            int brackets = 0;
            bool inString = false;
            
            foreach (char c in expression)
            {
                if (c == '"' && !inString) inString = true;
                else if (c == '"' && inString) inString = false;
                else if (!inString)
                {
                    if (c == '(') parens++;
                    else if (c == ')') parens--;
                    else if (c == '[') brackets++;
                    else if (c == ']') brackets--;
                }
            }
            
            if (inString)
                return (false, "Unclosed string literal");
            if (parens != 0)
                return (false, "Unbalanced parentheses");
            if (brackets != 0)
                return (false, "Unbalanced brackets");
            
            return (true, null);
        }

        /// <summary>
        /// Build a simple comparison expression
        /// </summary>
        public string BuildComparison(string left, ExpressionOperator op, object right)
        {
            var node = ExpressionNode.Op(op, 
                ExpressionNode.Property(left), 
                ExpressionNode.Literal(right));
            return NodeToString(node);
        }

        /// <summary>
        /// Build a limit check expression
        /// </summary>
        public string BuildLimitCheck(string variable, double lowLimit, double highLimit)
        {
            var varNode = ExpressionNode.Property(variable);
            var lowCheck = ExpressionNode.Op(ExpressionOperator.GreaterOrEqual, varNode, ExpressionNode.Literal(lowLimit));
            var highCheck = ExpressionNode.Op(ExpressionOperator.LessOrEqual, varNode, ExpressionNode.Literal(highLimit));
            var combined = ExpressionNode.Op(ExpressionOperator.And, lowCheck, highCheck);
            return NodeToString(combined);
        }
    }
}
