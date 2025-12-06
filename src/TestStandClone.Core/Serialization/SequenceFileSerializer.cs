using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestStandClone.Core.Serialization
{
    /// <summary>
    /// Serializes and deserializes sequence files.
    /// Supports JSON format similar to TestStand's sequence file format.
    /// </summary>
    public class SequenceFileSerializer
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Saves a sequence to a JSON file.
        /// </summary>
        public async Task SaveAsync(Sequence sequence, string filePath)
        {
            var dto = SequenceToDto(sequence);
            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Loads a sequence from a JSON file.
        /// </summary>
        public async Task<Sequence?> LoadAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var dto = JsonSerializer.Deserialize<SequenceFileDto>(json, _jsonOptions);
            
            return dto != null ? DtoToSequence(dto) : null;
        }

        /// <summary>
        /// Serializes a sequence to JSON string.
        /// </summary>
        public string SerializeToJson(Sequence sequence)
        {
            var dto = SequenceToDto(sequence);
            return JsonSerializer.Serialize(dto, _jsonOptions);
        }

        /// <summary>
        /// Deserializes a sequence from JSON string.
        /// </summary>
        public Sequence? DeserializeFromJson(string json)
        {
            var dto = JsonSerializer.Deserialize<SequenceFileDto>(json, _jsonOptions);
            return dto != null ? DtoToSequence(dto) : null;
        }

        private static SequenceFileDto SequenceToDto(Sequence sequence)
        {
            return new SequenceFileDto
            {
                Name = sequence.Name,
                Description = sequence.Description,
                Version = "1.0",
                SetupSteps = sequence.SetupSteps.Select(StepToDto).ToList(),
                MainSteps = sequence.MainSteps.Select(StepToDto).ToList(),
                CleanupSteps = sequence.CleanupSteps.Select(StepToDto).ToList()
            };
        }

        private static Sequence DtoToSequence(SequenceFileDto dto)
        {
            var sequence = new Sequence
            {
                Name = dto.Name ?? "Unnamed",
                Description = dto.Description ?? ""
            };

            foreach (var stepDto in dto.SetupSteps ?? new List<StepDto>())
            {
                var step = DtoToStep(stepDto);
                if (step != null)
                {
                    sequence.SetupSteps.Add(step);
                }
            }

            foreach (var stepDto in dto.MainSteps ?? new List<StepDto>())
            {
                var step = DtoToStep(stepDto);
                if (step != null)
                {
                    sequence.MainSteps.Add(step);
                }
            }

            foreach (var stepDto in dto.CleanupSteps ?? new List<StepDto>())
            {
                var step = DtoToStep(stepDto);
                if (step != null)
                {
                    sequence.CleanupSteps.Add(step);
                }
            }

            sequence.SyncStepsCollection();
            return sequence;
        }

        private static StepDto StepToDto(TestStep step)
        {
            var dto = new StepDto
            {
                Name = step.Name,
                Type = step.GetType().Name,
                Description = step.Description,
                IsEnabled = step.IsEnabled,
                HasBreakpoint = step.HasBreakpoint
            };

            // Add type-specific properties
            switch (step)
            {
                case DelayStep delay:
                    dto.Properties["delayMilliseconds"] = delay.DelayMilliseconds;
                    break;
                case NumericLimitStep numeric:
                    dto.Properties["lowerLimit"] = numeric.LowerLimit;
                    dto.Properties["upperLimit"] = numeric.UpperLimit;
                    dto.Properties["minGeneratedValue"] = numeric.MinGeneratedValue;
                    dto.Properties["maxGeneratedValue"] = numeric.MaxGeneratedValue;
                    break;
                case Steps.PassFailStep passFail:
                    dto.Properties["expectedResult"] = passFail.ExpectedResult;
                    break;
                case Steps.StringValueStep stringValue:
                    dto.Properties["expectedValue"] = stringValue.ExpectedValue;
                    dto.Properties["actualValue"] = stringValue.ActualValue;
                    break;
                case Steps.LabelStep label:
                    dto.Properties["labelId"] = label.LabelId;
                    break;
                case Steps.GotoStep gotoStep:
                    dto.Properties["targetLabelId"] = gotoStep.TargetLabelId;
                    break;
                case Steps.LoopStep loop:
                    dto.Properties["loopCount"] = loop.LoopCount;
                    dto.Properties["loopType"] = loop.Type.ToString();
                    break;
                case Steps.MessagePopupStep popup:
                    dto.Properties["title"] = popup.Title;
                    dto.Properties["message"] = popup.Message;
                    dto.Properties["buttons"] = popup.Buttons.ToString();
                    break;
            }

            return dto;
        }

        private static TestStep? DtoToStep(StepDto dto)
        {
            TestStep? step = dto.Type switch
            {
                "DelayStep" => new DelayStep
                {
                    DelayMilliseconds = GetIntProperty(dto.Properties, "delayMilliseconds", 1000)
                },
                "NumericLimitStep" => new NumericLimitStep
                {
                    LowerLimit = GetDoubleProperty(dto.Properties, "lowerLimit", 0),
                    UpperLimit = GetDoubleProperty(dto.Properties, "upperLimit", 100),
                    MinGeneratedValue = GetDoubleProperty(dto.Properties, "minGeneratedValue", 0),
                    MaxGeneratedValue = GetDoubleProperty(dto.Properties, "maxGeneratedValue", 100)
                },
                "PassFailStep" => new Steps.PassFailStep
                {
                    ExpectedResult = GetBoolProperty(dto.Properties, "expectedResult", true)
                },
                "StringValueStep" => new Steps.StringValueStep
                {
                    ExpectedValue = GetStringProperty(dto.Properties, "expectedValue", ""),
                    ActualValue = GetStringProperty(dto.Properties, "actualValue", "")
                },
                "ActionStep" => new Steps.ActionStep(),
                "LabelStep" => new Steps.LabelStep
                {
                    LabelId = GetStringProperty(dto.Properties, "labelId", "")
                },
                "GotoStep" => new Steps.GotoStep
                {
                    TargetLabelId = GetStringProperty(dto.Properties, "targetLabelId", "")
                },
                "LoopStep" => new Steps.LoopStep
                {
                    LoopCount = GetIntProperty(dto.Properties, "loopCount", 1),
                    Type = Enum.TryParse<Steps.LoopType>(GetStringProperty(dto.Properties, "loopType", "Begin"), out var lt) 
                           ? lt : Steps.LoopType.Begin
                },
                "MessagePopupStep" => new Steps.MessagePopupStep
                {
                    Title = GetStringProperty(dto.Properties, "title", "Message"),
                    Message = GetStringProperty(dto.Properties, "message", ""),
                    Buttons = Enum.TryParse<Steps.MessagePopupButtons>(GetStringProperty(dto.Properties, "buttons", "Ok"), out var btn) 
                              ? btn : Steps.MessagePopupButtons.Ok
                },
                _ => null
            };

            if (step != null)
            {
                step.Name = dto.Name ?? "";
                step.Description = dto.Description ?? "";
                step.IsEnabled = dto.IsEnabled;
                step.HasBreakpoint = dto.HasBreakpoint;
            }

            return step;
        }

        private static int GetIntProperty(Dictionary<string, object> props, string key, int defaultValue)
        {
            if (props.TryGetValue(key, out var value))
            {
                if (value is JsonElement element)
                {
                    return element.TryGetInt32(out int result) ? result : defaultValue;
                }
                if (value is int intValue) return intValue;
                if (int.TryParse(value?.ToString(), out int parsed)) return parsed;
            }
            return defaultValue;
        }

        private static double GetDoubleProperty(Dictionary<string, object> props, string key, double defaultValue)
        {
            if (props.TryGetValue(key, out var value))
            {
                if (value is JsonElement element)
                {
                    return element.TryGetDouble(out double result) ? result : defaultValue;
                }
                if (value is double doubleValue) return doubleValue;
                if (double.TryParse(value?.ToString(), out double parsed)) return parsed;
            }
            return defaultValue;
        }

        private static bool GetBoolProperty(Dictionary<string, object> props, string key, bool defaultValue)
        {
            if (props.TryGetValue(key, out var value))
            {
                if (value is JsonElement element && element.ValueKind == JsonValueKind.True || 
                    value is JsonElement element2 && element2.ValueKind == JsonValueKind.False)
                {
                    return ((JsonElement)value).GetBoolean();
                }
                if (value is bool boolValue) return boolValue;
                if (bool.TryParse(value?.ToString(), out bool parsed)) return parsed;
            }
            return defaultValue;
        }

        private static string GetStringProperty(Dictionary<string, object> props, string key, string defaultValue)
        {
            if (props.TryGetValue(key, out var value))
            {
                if (value is JsonElement element)
                {
                    return element.GetString() ?? defaultValue;
                }
                return value?.ToString() ?? defaultValue;
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// Data transfer object for sequence file.
    /// </summary>
    public class SequenceFileDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Version { get; set; }
        public List<StepDto>? SetupSteps { get; set; }
        public List<StepDto>? MainSteps { get; set; }
        public List<StepDto>? CleanupSteps { get; set; }
    }

    /// <summary>
    /// Data transfer object for step.
    /// </summary>
    public class StepDto
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool HasBreakpoint { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
