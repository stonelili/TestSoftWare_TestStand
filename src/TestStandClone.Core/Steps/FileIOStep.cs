// =============================================================================
// FileIOStep.cs - File I/O operations step
// Part of TestStandClone.Core - TestStand-like test automation framework
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestStandClone.Core.Steps
{
    /// <summary>
    /// Step for file I/O operations.
    /// Similar to TestStand's File I/O functionality.
    /// </summary>
    public class FileIOStep : TestStep
    {
        /// <summary>
        /// Gets or sets the file operation to perform.
        /// </summary>
        public FileOperation Operation { get; set; } = FileOperation.Read;

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data to write (for write operations).
        /// </summary>
        public string Data { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether to append (for write operations).
        /// </summary>
        public bool Append { get; set; } = false;

        /// <summary>
        /// Gets or sets the encoding to use.
        /// </summary>
        public string Encoding { get; set; } = "UTF-8";

        /// <summary>
        /// Gets or sets the context variable to store result.
        /// </summary>
        public string ResultVariable { get; set; } = string.Empty;

        /// <summary>
        /// Gets the result of the file operation.
        /// </summary>
        public object? Result { get; private set; }

        /// <summary>
        /// Creates a new FileIOStep with the given name.
        /// </summary>
        public FileIOStep(string name, FileOperation operation = FileOperation.Read)
        {
            Name = name;
            Operation = operation;
        }

        /// <summary>
        /// Executes the file operation.
        /// </summary>
        public override async Task ExecuteAsync(Context context)
        {
            try
            {
                // Resolve file path from context if needed
                string resolvedPath = ResolveValue(FilePath, context);
                var encoding = GetEncoding();

                switch (Operation)
                {
                    case FileOperation.Read:
                        await ExecuteReadAsync(resolvedPath, encoding, context);
                        break;
                    case FileOperation.Write:
                        await ExecuteWriteAsync(resolvedPath, encoding, context);
                        break;
                    case FileOperation.ReadLines:
                        await ExecuteReadLinesAsync(resolvedPath, encoding, context);
                        break;
                    case FileOperation.WriteLines:
                        await ExecuteWriteLinesAsync(resolvedPath, encoding, context);
                        break;
                    case FileOperation.ReadJson:
                        await ExecuteReadJsonAsync(resolvedPath, context);
                        break;
                    case FileOperation.WriteJson:
                        await ExecuteWriteJsonAsync(resolvedPath, context);
                        break;
                    case FileOperation.Exists:
                        ExecuteExists(resolvedPath, context);
                        break;
                    case FileOperation.Delete:
                        ExecuteDelete(resolvedPath);
                        break;
                    case FileOperation.Copy:
                        ExecuteCopy(resolvedPath, context);
                        break;
                    case FileOperation.Move:
                        ExecuteMove(resolvedPath, context);
                        break;
                    case FileOperation.GetInfo:
                        ExecuteGetInfo(resolvedPath, context);
                        break;
                    case FileOperation.CreateDirectory:
                        ExecuteCreateDirectory(resolvedPath);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown operation: {Operation}");
                }
            }
            catch (Exception ex)
            {
                Status = StepStatus.Error;
                ResultText = $"File I/O error: {ex.Message}";
            }
        }

        private async Task ExecuteReadAsync(string path, Encoding encoding, Context context)
        {
            if (!File.Exists(path))
            {
                Status = StepStatus.Failed;
                ResultText = $"File not found: {path}";
                return;
            }

            Result = await File.ReadAllTextAsync(path, encoding);
            StoreResult(context);
            Status = StepStatus.Passed;
            ResultText = $"Read {((string)Result).Length} characters";
        }

        private async Task ExecuteWriteAsync(string path, Encoding encoding, Context context)
        {
            string data = ResolveValue(Data, context);
            
            if (Append)
                await File.AppendAllTextAsync(path, data, encoding);
            else
                await File.WriteAllTextAsync(path, data, encoding);

            Status = StepStatus.Passed;
            ResultText = Append ? $"Appended {data.Length} characters" : $"Wrote {data.Length} characters";
        }

        private async Task ExecuteReadLinesAsync(string path, Encoding encoding, Context context)
        {
            if (!File.Exists(path))
            {
                Status = StepStatus.Failed;
                ResultText = $"File not found: {path}";
                return;
            }

            Result = await File.ReadAllLinesAsync(path, encoding);
            StoreResult(context);
            Status = StepStatus.Passed;
            ResultText = $"Read {((string[])Result).Length} lines";
        }

        private async Task ExecuteWriteLinesAsync(string path, Encoding encoding, Context context)
        {
            IEnumerable<string> lines;
            
            if (context.Data.TryGetValue(Data, out var value) && value is IEnumerable<string> enumerable)
                lines = enumerable;
            else
                lines = Data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            if (Append)
                await File.AppendAllLinesAsync(path, lines, encoding);
            else
                await File.WriteAllLinesAsync(path, lines, encoding);

            Status = StepStatus.Passed;
            ResultText = "Lines written";
        }

        private async Task ExecuteReadJsonAsync(string path, Context context)
        {
            if (!File.Exists(path))
            {
                Status = StepStatus.Failed;
                ResultText = $"File not found: {path}";
                return;
            }

            string json = await File.ReadAllTextAsync(path);
            Result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            StoreResult(context);
            Status = StepStatus.Passed;
            ResultText = "JSON loaded";
        }

        private async Task ExecuteWriteJsonAsync(string path, Context context)
        {
            object? data;
            
            if (context.Data.TryGetValue(Data, out var value))
                data = value;
            else
                data = context.Data;

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
            Status = StepStatus.Passed;
            ResultText = "JSON saved";
        }

        private void ExecuteExists(string path, Context context)
        {
            bool exists = File.Exists(path) || Directory.Exists(path);
            Result = exists;
            StoreResult(context);
            Status = StepStatus.Passed;
            ResultText = exists ? "File/Directory exists" : "File/Directory not found";
        }

        private void ExecuteDelete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Status = StepStatus.Passed;
                ResultText = "File deleted";
            }
            else if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                Status = StepStatus.Passed;
                ResultText = "Directory deleted";
            }
            else
            {
                Status = StepStatus.Failed;
                ResultText = "File/Directory not found";
            }
        }

        private void ExecuteCopy(string sourcePath, Context context)
        {
            string destPath = ResolveValue(Data, context);
            
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, destPath, overwrite: true);
                Status = StepStatus.Passed;
                ResultText = $"File copied to {destPath}";
            }
            else
            {
                Status = StepStatus.Failed;
                ResultText = "Source file not found";
            }
        }

        private void ExecuteMove(string sourcePath, Context context)
        {
            string destPath = ResolveValue(Data, context);
            
            if (File.Exists(sourcePath))
            {
                File.Move(sourcePath, destPath, overwrite: true);
                Status = StepStatus.Passed;
                ResultText = $"File moved to {destPath}";
            }
            else
            {
                Status = StepStatus.Failed;
                ResultText = "Source file not found";
            }
        }

        private void ExecuteGetInfo(string path, Context context)
        {
            if (File.Exists(path))
            {
                var info = new FileInfo(path);
                Result = new Dictionary<string, object>
                {
                    ["Name"] = info.Name,
                    ["FullName"] = info.FullName,
                    ["Length"] = info.Length,
                    ["CreationTime"] = info.CreationTime,
                    ["LastWriteTime"] = info.LastWriteTime,
                    ["Extension"] = info.Extension,
                    ["IsReadOnly"] = info.IsReadOnly
                };
                StoreResult(context);
                Status = StepStatus.Passed;
                ResultText = $"File info: {info.Name}, {info.Length} bytes";
            }
            else
            {
                Status = StepStatus.Failed;
                ResultText = "File not found";
            }
        }

        private void ExecuteCreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
            Status = StepStatus.Passed;
            ResultText = $"Directory created: {path}";
        }

        private void StoreResult(Context context)
        {
            if (!string.IsNullOrEmpty(ResultVariable))
            {
                context.Data[ResultVariable] = Result;
            }
        }

        private string ResolveValue(string value, Context context)
        {
            if (value.StartsWith("${") && value.EndsWith("}"))
            {
                string varName = value[2..^1];
                if (context.Data.TryGetValue(varName, out var resolved))
                    return resolved?.ToString() ?? string.Empty;
            }
            return value;
        }

        private Encoding GetEncoding()
        {
            return Encoding.ToUpperInvariant() switch
            {
                "UTF-8" or "UTF8" => System.Text.Encoding.UTF8,
                "UTF-16" or "UNICODE" => System.Text.Encoding.Unicode,
                "ASCII" => System.Text.Encoding.ASCII,
                "UTF-32" => System.Text.Encoding.UTF32,
                _ => System.Text.Encoding.UTF8
            };
        }
    }

    /// <summary>
    /// File operations supported by FileIOStep.
    /// </summary>
    public enum FileOperation
    {
        /// <summary>Read entire file as text</summary>
        Read,
        /// <summary>Write text to file</summary>
        Write,
        /// <summary>Read file as array of lines</summary>
        ReadLines,
        /// <summary>Write array of lines to file</summary>
        WriteLines,
        /// <summary>Read and parse JSON file</summary>
        ReadJson,
        /// <summary>Write data as JSON file</summary>
        WriteJson,
        /// <summary>Check if file/directory exists</summary>
        Exists,
        /// <summary>Delete file or directory</summary>
        Delete,
        /// <summary>Copy file</summary>
        Copy,
        /// <summary>Move file</summary>
        Move,
        /// <summary>Get file information</summary>
        GetInfo,
        /// <summary>Create directory</summary>
        CreateDirectory
    }
}
