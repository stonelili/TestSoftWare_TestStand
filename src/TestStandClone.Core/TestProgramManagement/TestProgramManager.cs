using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TestStandClone.Core.TestProgramManagement
{
    public enum TestProgramStatus { Draft, UnderReview, Approved, Released, Deprecated, Archived }
    public enum TestProgramType { Production, Engineering, Qualification, Debug, Calibration }

    public class TestProgramVersion
    {
        public string Version { get; set; } = "1.0.0";
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string ChangeLog { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;
        public DateTime? ApprovedAt { get; set; }
    }

    public class TestProgramFile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
    }

    public class TestProgram
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TestProgramType Type { get; set; } = TestProgramType.Production;
        public TestProgramStatus Status { get; set; } = TestProgramStatus.Draft;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductFamily { get; set; } = string.Empty;
        public TestProgramVersion CurrentVersion { get; set; } = new TestProgramVersion();
        public List<TestProgramVersion> VersionHistory { get; set; } = new List<TestProgramVersion>();
        public List<TestProgramFile> Files { get; set; } = new List<TestProgramFile>();
        public string MainSequenceFile { get; set; } = string.Empty;
        public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
    }

    public class TestProgramRelease
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ProgramId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ReleasedBy { get; set; } = string.Empty;
        public DateTime ReleasedAt { get; set; } = DateTime.Now;
        public string ReleaseNotes { get; set; } = string.Empty;
        public List<string> TargetStations { get; set; } = new List<string>();
        public bool IsActive { get; set; } = true;
    }

    public class TestProgramDeployment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ReleaseId { get; set; } = string.Empty;
        public string StationId { get; set; } = string.Empty;
        public DateTime DeployedAt { get; set; } = DateTime.Now;
        public string DeployedBy { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; }
        public string DeploymentLog { get; set; } = string.Empty;
    }

    public class TestProgramQuery
    {
        public string? NameContains { get; set; }
        public string? ProductCode { get; set; }
        public TestProgramType? Type { get; set; }
        public TestProgramStatus? Status { get; set; }
    }

    public class TestProgramManager
    {
        private static readonly Lazy<TestProgramManager> _instance = new Lazy<TestProgramManager>(() => new TestProgramManager());
        public static TestProgramManager Instance => _instance.Value;

        private readonly Dictionary<string, TestProgram> _programs = new Dictionary<string, TestProgram>();
        private readonly Dictionary<string, TestProgramRelease> _releases = new Dictionary<string, TestProgramRelease>();
        private readonly List<TestProgramDeployment> _deployments = new List<TestProgramDeployment>();
        private string _storageDirectory = "./TestPrograms";

        public event EventHandler<TestProgram>? ProgramCreated;
        public event EventHandler<TestProgramRelease>? ProgramReleased;
        public event EventHandler<TestProgramDeployment>? ProgramDeployed;

        private TestProgramManager() { }

        public void SetStorageDirectory(string dir) { _storageDirectory = dir; if (!Directory.Exists(dir)) Directory.CreateDirectory(dir); }

        public TestProgram CreateProgram(string name, TestProgramType type, string productCode)
        {
            var program = new TestProgram { Name = name, Type = type, ProductCode = productCode };
            _programs[program.Id] = program;
            ProgramCreated?.Invoke(this, program);
            return program;
        }

        public void UpdateProgram(TestProgram program) { program.ModifiedAt = DateTime.Now; _programs[program.Id] = program; }
        public TestProgram? GetProgram(string id) { _programs.TryGetValue(id, out var p); return p; }
        public IEnumerable<TestProgram> GetAllPrograms() => _programs.Values.ToList();

        public IEnumerable<TestProgram> QueryPrograms(TestProgramQuery query)
        {
            var results = _programs.Values.AsEnumerable();
            if (!string.IsNullOrEmpty(query.NameContains)) results = results.Where(p => p.Name.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(query.ProductCode)) results = results.Where(p => p.ProductCode == query.ProductCode);
            if (query.Type.HasValue) results = results.Where(p => p.Type == query.Type.Value);
            if (query.Status.HasValue) results = results.Where(p => p.Status == query.Status.Value);
            return results.ToList();
        }

        public TestProgramRelease ReleaseProgram(string programId, string releasedBy, string releaseNotes)
        {
            if (!_programs.TryGetValue(programId, out var program)) throw new InvalidOperationException($"Program {programId} not found");
            program.Status = TestProgramStatus.Released;
            var release = new TestProgramRelease { ProgramId = programId, Version = program.CurrentVersion.Version, ReleasedBy = releasedBy, ReleaseNotes = releaseNotes };
            _releases[release.Id] = release;
            ProgramReleased?.Invoke(this, release);
            return release;
        }

        public TestProgramDeployment DeployProgram(string releaseId, string stationId, string deployedBy)
        {
            if (!_releases.TryGetValue(releaseId, out var release)) throw new InvalidOperationException($"Release {releaseId} not found");
            var deployment = new TestProgramDeployment { ReleaseId = releaseId, StationId = stationId, DeployedBy = deployedBy, IsSuccessful = true, DeploymentLog = $"Deployed {release.Version} to {stationId}" };
            _deployments.Add(deployment);
            ProgramDeployed?.Invoke(this, deployment);
            return deployment;
        }

        public IEnumerable<TestProgramRelease> GetReleases(string programId) => _releases.Values.Where(r => r.ProgramId == programId).ToList();
        public IEnumerable<TestProgramDeployment> GetDeployments(string stationId) => _deployments.Where(d => d.StationId == stationId).ToList();

        public void SaveProgram(string programId)
        {
            if (!_programs.TryGetValue(programId, out var p)) return;
            var filePath = Path.Combine(_storageDirectory, $"{p.Id}.json");
            File.WriteAllText(filePath, JsonSerializer.Serialize(p, new JsonSerializerOptions { WriteIndented = true }));
        }

        public TestProgram? LoadProgram(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            var p = JsonSerializer.Deserialize<TestProgram>(File.ReadAllText(filePath));
            if (p != null) _programs[p.Id] = p;
            return p;
        }

        public void DeleteProgram(string id) => _programs.Remove(id);
    }
}
