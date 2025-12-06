using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using TestStandClone.Core.Serialization;

namespace TestStandClone.Core.Deployment
{
    /// <summary>
    /// Information about a deployment package
    /// </summary>
    public class DeploymentPackage
    {
        public string PackageId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = Environment.UserName;
        
        public List<string> SequenceFiles { get; set; } = new();
        public List<string> CodeModules { get; set; } = new();
        public List<string> ResourceFiles { get; set; } = new();
        public List<string> ConfigurationFiles { get; set; } = new();
        
        public Dictionary<string, string> Dependencies { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        
        public string TargetFramework { get; set; } = "net8.0";
        public bool IncludeRuntime { get; set; } = false;
        public bool CreateExecutable { get; set; } = true;
    }

    /// <summary>
    /// Result of deployment operation
    /// </summary>
    public class DeploymentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public long PackageSize { get; set; }
        public int FileCount { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Deployment validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> MissingFiles { get; set; } = new();
        public List<string> MissingDependencies { get; set; } = new();
    }

    /// <summary>
    /// Manages deployment of sequence files and related resources
    /// Similar to TestStand Deployment Utility
    /// </summary>
    public class DeploymentManager
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Create a deployment package from sequences and resources
        /// </summary>
        public DeploymentResult CreatePackage(DeploymentPackage package, string outputPath)
        {
            var result = new DeploymentResult();
            
            try
            {
                // Validate the package first
                var validation = ValidatePackage(package);
                if (!validation.IsValid)
                {
                    result.Success = false;
                    result.Message = "Package validation failed";
                    result.Errors = validation.Errors;
                    return result;
                }

                result.Warnings.AddRange(validation.Warnings);

                // Create output directory if needed
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Create the package (ZIP file)
                var packagePath = outputPath;
                if (!packagePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    packagePath += ".zip";
                }

                // Remove existing file if present
                if (File.Exists(packagePath))
                {
                    File.Delete(packagePath);
                }

                using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create))
                {
                    int fileCount = 0;

                    // Add manifest
                    var manifestEntry = archive.CreateEntry("manifest.json");
                    using (var writer = new StreamWriter(manifestEntry.Open()))
                    {
                        var manifest = JsonSerializer.Serialize(package, JsonOptions);
                        writer.Write(manifest);
                    }
                    fileCount++;

                    // Add sequence files
                    foreach (var seqFile in package.SequenceFiles)
                    {
                        if (File.Exists(seqFile))
                        {
                            var entryName = Path.Combine("Sequences", Path.GetFileName(seqFile));
                            archive.CreateEntryFromFile(seqFile, entryName);
                            fileCount++;
                        }
                    }

                    // Add code modules
                    foreach (var module in package.CodeModules)
                    {
                        if (File.Exists(module))
                        {
                            var entryName = Path.Combine("Modules", Path.GetFileName(module));
                            archive.CreateEntryFromFile(module, entryName);
                            fileCount++;
                        }
                    }

                    // Add resource files
                    foreach (var resource in package.ResourceFiles)
                    {
                        if (File.Exists(resource))
                        {
                            var entryName = Path.Combine("Resources", Path.GetFileName(resource));
                            archive.CreateEntryFromFile(resource, entryName);
                            fileCount++;
                        }
                    }

                    // Add configuration files
                    foreach (var config in package.ConfigurationFiles)
                    {
                        if (File.Exists(config))
                        {
                            var entryName = Path.Combine("Config", Path.GetFileName(config));
                            archive.CreateEntryFromFile(config, entryName);
                            fileCount++;
                        }
                    }

                    result.FileCount = fileCount;
                }

                var fileInfo = new FileInfo(packagePath);
                result.Success = true;
                result.Message = "Package created successfully";
                result.OutputPath = packagePath;
                result.PackageSize = fileInfo.Length;

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Failed to create package: {ex.Message}";
                result.Errors.Add(ex.ToString());
                return result;
            }
        }

        /// <summary>
        /// Extract a deployment package to a target directory
        /// </summary>
        public DeploymentResult ExtractPackage(string packagePath, string targetDirectory)
        {
            var result = new DeploymentResult();

            try
            {
                if (!File.Exists(packagePath))
                {
                    result.Success = false;
                    result.Message = "Package file not found";
                    result.Errors.Add($"File not found: {packagePath}");
                    return result;
                }

                // Create target directory
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                // Extract the package
                ZipFile.ExtractToDirectory(packagePath, targetDirectory, true);

                // Count extracted files
                result.FileCount = Directory.GetFiles(targetDirectory, "*", SearchOption.AllDirectories).Length;

                result.Success = true;
                result.Message = "Package extracted successfully";
                result.OutputPath = targetDirectory;

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Failed to extract package: {ex.Message}";
                result.Errors.Add(ex.ToString());
                return result;
            }
        }

        /// <summary>
        /// Read package manifest without extracting
        /// </summary>
        public DeploymentPackage? ReadManifest(string packagePath)
        {
            try
            {
                using var archive = ZipFile.OpenRead(packagePath);
                var manifestEntry = archive.GetEntry("manifest.json");
                if (manifestEntry == null) return null;

                using var reader = new StreamReader(manifestEntry.Open());
                var json = reader.ReadToEnd();
                return JsonSerializer.Deserialize<DeploymentPackage>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Validate a deployment package
        /// </summary>
        public ValidationResult ValidatePackage(DeploymentPackage package)
        {
            var result = new ValidationResult { IsValid = true };

            // Check required fields
            if (string.IsNullOrEmpty(package.Name))
            {
                result.Errors.Add("Package name is required");
                result.IsValid = false;
            }

            // Check sequence files exist
            foreach (var seqFile in package.SequenceFiles)
            {
                if (!File.Exists(seqFile))
                {
                    result.MissingFiles.Add(seqFile);
                    result.Warnings.Add($"Sequence file not found: {seqFile}");
                }
            }

            // Check code modules exist
            foreach (var module in package.CodeModules)
            {
                if (!File.Exists(module))
                {
                    result.MissingFiles.Add(module);
                    result.Warnings.Add($"Code module not found: {module}");
                }
            }

            // Check resource files exist
            foreach (var resource in package.ResourceFiles)
            {
                if (!File.Exists(resource))
                {
                    result.MissingFiles.Add(resource);
                    result.Warnings.Add($"Resource file not found: {resource}");
                }
            }

            // Check if any sequence files are present
            if (package.SequenceFiles.Count == 0)
            {
                result.Warnings.Add("No sequence files included in package");
            }

            return result;
        }

        /// <summary>
        /// Analyze a sequence file for dependencies
        /// </summary>
        public async Task<List<string>> AnalyzeDependenciesAsync(string sequenceFilePath)
        {
            var dependencies = new List<string>();

            try
            {
                var serializer = new SequenceFileSerializer();
                var sequence = await serializer.LoadAsync(sequenceFilePath);
                if (sequence == null) return dependencies;

                // Look for code module references
                foreach (var step in sequence.SetupSteps.Concat(sequence.MainSteps).Concat(sequence.CleanupSteps))
                {
                    if (step is Steps.CodeModuleStep codeStep && !string.IsNullOrEmpty(codeStep.AssemblyPath))
                    {
                        if (!dependencies.Contains(codeStep.AssemblyPath))
                        {
                            dependencies.Add(codeStep.AssemblyPath);
                        }
                    }

                    if (step is Steps.SequenceCallStep seqCallStep && !string.IsNullOrEmpty(seqCallStep.SequenceFilePath))
                    {
                        if (!dependencies.Contains(seqCallStep.SequenceFilePath))
                        {
                            dependencies.Add(seqCallStep.SequenceFilePath);
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors during analysis
            }

            return dependencies;
        }

        /// <summary>
        /// Create a deployment package builder for fluent configuration
        /// </summary>
        public static DeploymentPackageBuilder CreateBuilder()
        {
            return new DeploymentPackageBuilder();
        }
    }

    /// <summary>
    /// Fluent builder for deployment packages
    /// </summary>
    public class DeploymentPackageBuilder
    {
        private readonly DeploymentPackage _package = new();

        public DeploymentPackageBuilder WithName(string name)
        {
            _package.Name = name;
            return this;
        }

        public DeploymentPackageBuilder WithVersion(string version)
        {
            _package.Version = version;
            return this;
        }

        public DeploymentPackageBuilder WithDescription(string description)
        {
            _package.Description = description;
            return this;
        }

        public DeploymentPackageBuilder AddSequence(string filePath)
        {
            _package.SequenceFiles.Add(filePath);
            return this;
        }

        public DeploymentPackageBuilder AddSequences(IEnumerable<string> filePaths)
        {
            _package.SequenceFiles.AddRange(filePaths);
            return this;
        }

        public DeploymentPackageBuilder AddCodeModule(string filePath)
        {
            _package.CodeModules.Add(filePath);
            return this;
        }

        public DeploymentPackageBuilder AddResource(string filePath)
        {
            _package.ResourceFiles.Add(filePath);
            return this;
        }

        public DeploymentPackageBuilder AddConfiguration(string filePath)
        {
            _package.ConfigurationFiles.Add(filePath);
            return this;
        }

        public DeploymentPackageBuilder WithMetadata(string key, object value)
        {
            _package.Metadata[key] = value;
            return this;
        }

        public DeploymentPackageBuilder IncludeRuntime(bool include = true)
        {
            _package.IncludeRuntime = include;
            return this;
        }

        public DeploymentPackageBuilder CreateExecutable(bool create = true)
        {
            _package.CreateExecutable = create;
            return this;
        }

        public DeploymentPackage Build()
        {
            return _package;
        }

        public DeploymentResult Deploy(string outputPath)
        {
            var manager = new DeploymentManager();
            return manager.CreatePackage(_package, outputPath);
        }
    }
}
