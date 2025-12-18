// =============================================================================
// PropertyStep.cs - Get/Set property values step
// Part of TestStandClone.Core - TestStand-like test automation framework
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestStandClone.Core.Steps
{
    /// <summary>
    /// Step that gets or sets property values.
    /// Similar to TestStand's Statement step type for property operations.
    /// </summary>
    public class PropertyStep : TestStep
    {
        /// <summary>
        /// Gets or sets the operation to perform.
        /// </summary>
        public PropertyOperation Operation { get; set; } = PropertyOperation.Get;

        /// <summary>
        /// Gets or sets the source property path.
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the destination property path (for Set/Copy operations).
        /// </summary>
        public string DestinationPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value to set (for Set operation with literal value).
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Gets the result value (for Get operation).
        /// </summary>
        public object? ResultValue { get; private set; }

        /// <summary>
        /// Creates a new PropertyStep with the given name.
        /// </summary>
        public PropertyStep(string name, PropertyOperation operation = PropertyOperation.Get)
        {
            Name = name;
            Operation = operation;
        }

        /// <summary>
        /// Executes the property operation.
        /// </summary>
        public override async Task ExecuteAsync(Context context)
        {
            try
            {
                switch (Operation)
                {
                    case PropertyOperation.Get:
                        ExecuteGet(context);
                        break;
                    case PropertyOperation.Set:
                        ExecuteSet(context);
                        break;
                    case PropertyOperation.Copy:
                        ExecuteCopy(context);
                        break;
                    case PropertyOperation.Delete:
                        ExecuteDelete(context);
                        break;
                    case PropertyOperation.Exists:
                        ExecuteExists(context);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown operation: {Operation}");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Status = StepStatus.Error;
                ResultText = $"Property operation error: {ex.Message}";
            }
        }

        private void ExecuteGet(Context context)
        {
            if (context.Data.TryGetValue(SourcePath, out var value))
            {
                ResultValue = value;
                Status = StepStatus.Passed;
                ResultText = $"Got: {value}";
            }
            else
            {
                Status = StepStatus.Failed;
                ResultText = $"Property not found: {SourcePath}";
            }
        }

        private void ExecuteSet(Context context)
        {
            context.Data[DestinationPath] = Value;
            Status = StepStatus.Passed;
            ResultText = $"Set {DestinationPath} = {Value}";
        }

        private void ExecuteCopy(Context context)
        {
            if (context.Data.TryGetValue(SourcePath, out var value))
            {
                context.Data[DestinationPath] = value;
                Status = StepStatus.Passed;
                ResultText = $"Copied {SourcePath} to {DestinationPath}";
            }
            else
            {
                Status = StepStatus.Failed;
                ResultText = $"Source property not found: {SourcePath}";
            }
        }

        private void ExecuteDelete(Context context)
        {
            if (context.Data.Remove(SourcePath))
            {
                Status = StepStatus.Passed;
                ResultText = $"Deleted: {SourcePath}";
            }
            else
            {
                Status = StepStatus.Failed;
                ResultText = $"Property not found: {SourcePath}";
            }
        }

        private void ExecuteExists(Context context)
        {
            bool exists = context.Data.ContainsKey(SourcePath);
            ResultValue = exists;
            Status = StepStatus.Passed;
            ResultText = exists ? $"Property exists: {SourcePath}" : $"Property not found: {SourcePath}";
        }
    }

    /// <summary>
    /// Property operations supported by PropertyStep.
    /// </summary>
    public enum PropertyOperation
    {
        /// <summary>Get property value</summary>
        Get,
        /// <summary>Set property value</summary>
        Set,
        /// <summary>Copy property to another location</summary>
        Copy,
        /// <summary>Delete property</summary>
        Delete,
        /// <summary>Check if property exists</summary>
        Exists
    }
}
