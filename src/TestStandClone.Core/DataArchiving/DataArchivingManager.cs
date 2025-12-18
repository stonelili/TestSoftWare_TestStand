using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestStandClone.Core.DataArchiving
{
    /// <summary>
    /// Archive type enumeration
    /// </summary>
    public enum ArchiveType
    {
        TestResults,
        Sequences,
        Reports,
        Logs,
        Configuration,
        Mixed
    }

    /// <summary>
    /// Archive status enumeration
    /// </summary>
    public enum ArchiveStatus
    {
        Active,
        Archived,
        Restored,
        Deleted
    }

    /// <summary>
    /// Compression type
    /// </summary>
    public enum CompressionType
    {
        None,
        GZip,
        Zip
    }

    /// <summary>
    /// Represents an archived item
    /// </summary>
    public class ArchiveItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string OriginalPath { get; set; } = string.Empty;
        public string ArchivePath { get; set; } = string.Empty;
        public ArchiveType Type { get; set; }
        public ArchiveStatus Status { get; set; } = ArchiveStatus.Active;
        public DateTime CreatedAt { get; set; }
        public DateTime ArchivedAt { get; set; }
        public long OriginalSize { get; set; }
        public long ArchivedSize { get; set; }
        public CompressionType Compression { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new();
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Archive policy for automatic archiving
    /// </summary>
    public class ArchivePolicy
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public ArchiveType TargetType { get; set; }
        public int RetentionDays { get; set; } = 30;
        public int ArchiveAfterDays { get; set; } = 7;
        public bool AutoDelete { get; set; }
        public int DeleteAfterDays { get; set; } = 365;
        public CompressionType Compression { get; set; } = CompressionType.GZip;
        public string SourcePath { get; set; } = string.Empty;
        public string ArchivePath { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Archive query for searching
    /// </summary>
    public class ArchiveQuery
    {
        public ArchiveType? Type { get; set; }
        public ArchiveStatus? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? NamePattern { get; set; }
        public List<string>? Tags { get; set; }
    }

    /// <summary>
    /// Archive statistics
    /// </summary>
    public class ArchiveStatistics
    {
        public int TotalItems { get; set; }
        public long TotalOriginalSize { get; set; }
        public long TotalArchivedSize { get; set; }
        public double CompressionRatio => TotalOriginalSize > 0 ? (double)TotalArchivedSize / TotalOriginalSize : 1;
        public Dictionary<ArchiveType, int> ItemsByType { get; set; } = new();
        public Dictionary<ArchiveStatus, int> ItemsByStatus { get; set; } = new();
    }

    /// <summary>
    /// Data archiving manager
    /// </summary>
    public class DataArchivingManager
    {
        private static DataArchivingManager? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, ArchiveItem> _items = new();
        private readonly Dictionary<string, ArchivePolicy> _policies = new();
        private string _archiveBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TestStandClone", "Archives");

        public static DataArchivingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new DataArchivingManager();
                    }
                }
                return _instance;
            }
        }

        public event EventHandler<ArchiveItem>? ItemArchived;
        public event EventHandler<ArchiveItem>? ItemRestored;
        public event EventHandler<ArchiveItem>? ItemDeleted;

        public DataArchivingManager()
        {
            Directory.CreateDirectory(_archiveBasePath);
        }

        /// <summary>
        /// Set the base path for archives
        /// </summary>
        public void SetArchiveBasePath(string path)
        {
            _archiveBasePath = path;
            Directory.CreateDirectory(_archiveBasePath);
        }

        /// <summary>
        /// Archive a file
        /// </summary>
        public async Task<ArchiveItem> ArchiveFileAsync(string sourcePath, ArchiveType type, CompressionType compression = CompressionType.GZip, Dictionary<string, string>? metadata = null)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Source file not found", sourcePath);
            }

            var fileInfo = new FileInfo(sourcePath);
            var item = new ArchiveItem
            {
                Name = fileInfo.Name,
                OriginalPath = sourcePath,
                Type = type,
                CreatedAt = fileInfo.CreationTime,
                ArchivedAt = DateTime.Now,
                OriginalSize = fileInfo.Length,
                Compression = compression,
                Metadata = metadata ?? new Dictionary<string, string>()
            };

            var archiveDir = Path.Combine(_archiveBasePath, type.ToString(), DateTime.Now.ToString("yyyy-MM"));
            Directory.CreateDirectory(archiveDir);

            var archiveFileName = $"{item.Id}_{fileInfo.Name}";
            if (compression != CompressionType.None)
            {
                archiveFileName += ".gz";
            }

            item.ArchivePath = Path.Combine(archiveDir, archiveFileName);

            await CompressFileAsync(sourcePath, item.ArchivePath, compression);

            item.ArchivedSize = new FileInfo(item.ArchivePath).Length;
            item.Checksum = await ComputeChecksumAsync(item.ArchivePath);
            item.Status = ArchiveStatus.Archived;

            lock (_lock)
            {
                _items[item.Id] = item;
            }

            ItemArchived?.Invoke(this, item);
            return item;
        }

        /// <summary>
        /// Restore an archived item
        /// </summary>
        public async Task<string> RestoreItemAsync(string itemId, string? targetPath = null)
        {
            if (!_items.TryGetValue(itemId, out var item))
            {
                throw new KeyNotFoundException($"Archive item {itemId} not found");
            }

            if (!File.Exists(item.ArchivePath))
            {
                throw new FileNotFoundException("Archive file not found", item.ArchivePath);
            }

            var restorePath = targetPath ?? item.OriginalPath;
            var restoreDir = Path.GetDirectoryName(restorePath);
            if (!string.IsNullOrEmpty(restoreDir))
            {
                Directory.CreateDirectory(restoreDir);
            }

            await DecompressFileAsync(item.ArchivePath, restorePath, item.Compression);

            item.Status = ArchiveStatus.Restored;
            ItemRestored?.Invoke(this, item);

            return restorePath;
        }

        /// <summary>
        /// Delete an archived item
        /// </summary>
        public bool DeleteItem(string itemId, bool deleteFile = true)
        {
            if (!_items.TryGetValue(itemId, out var item))
            {
                return false;
            }

            if (deleteFile && File.Exists(item.ArchivePath))
            {
                File.Delete(item.ArchivePath);
            }

            item.Status = ArchiveStatus.Deleted;
            ItemDeleted?.Invoke(this, item);

            lock (_lock)
            {
                _items.Remove(itemId);
            }

            return true;
        }

        /// <summary>
        /// Add an archive policy
        /// </summary>
        public void AddPolicy(ArchivePolicy policy)
        {
            lock (_lock)
            {
                _policies[policy.Id] = policy;
            }
        }

        /// <summary>
        /// Apply archive policies
        /// </summary>
        public async Task ApplyPoliciesAsync()
        {
            foreach (var policy in _policies.Values.Where(p => p.IsEnabled))
            {
                await ApplyPolicyAsync(policy);
            }
        }

        private async Task ApplyPolicyAsync(ArchivePolicy policy)
        {
            if (!Directory.Exists(policy.SourcePath))
            {
                return;
            }

            var cutoffDate = DateTime.Now.AddDays(-policy.ArchiveAfterDays);
            var files = Directory.GetFiles(policy.SourcePath);

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTime < cutoffDate)
                {
                    await ArchiveFileAsync(file, policy.TargetType, policy.Compression);

                    if (policy.AutoDelete)
                    {
                        File.Delete(file);
                    }
                }
            }

            if (policy.AutoDelete)
            {
                var deleteCutoff = DateTime.Now.AddDays(-policy.DeleteAfterDays);
                var itemsToDelete = _items.Values
                    .Where(i => i.Type == policy.TargetType && i.ArchivedAt < deleteCutoff)
                    .ToList();

                foreach (var item in itemsToDelete)
                {
                    DeleteItem(item.Id);
                }
            }
        }

        /// <summary>
        /// Query archived items
        /// </summary>
        public IReadOnlyList<ArchiveItem> QueryItems(ArchiveQuery query)
        {
            lock (_lock)
            {
                var result = _items.Values.AsEnumerable();

                if (query.Type.HasValue)
                    result = result.Where(i => i.Type == query.Type.Value);

                if (query.Status.HasValue)
                    result = result.Where(i => i.Status == query.Status.Value);

                if (query.StartDate.HasValue)
                    result = result.Where(i => i.ArchivedAt >= query.StartDate.Value);

                if (query.EndDate.HasValue)
                    result = result.Where(i => i.ArchivedAt <= query.EndDate.Value);

                if (!string.IsNullOrEmpty(query.NamePattern))
                    result = result.Where(i => i.Name.Contains(query.NamePattern, StringComparison.OrdinalIgnoreCase));

                if (query.Tags != null && query.Tags.Count > 0)
                    result = result.Where(i => query.Tags.All(t => i.Tags.Contains(t)));

                return result.ToList();
            }
        }

        /// <summary>
        /// Get archive statistics
        /// </summary>
        public ArchiveStatistics GetStatistics()
        {
            lock (_lock)
            {
                return new ArchiveStatistics
                {
                    TotalItems = _items.Count,
                    TotalOriginalSize = _items.Values.Sum(i => i.OriginalSize),
                    TotalArchivedSize = _items.Values.Sum(i => i.ArchivedSize),
                    ItemsByType = _items.Values.GroupBy(i => i.Type).ToDictionary(g => g.Key, g => g.Count()),
                    ItemsByStatus = _items.Values.GroupBy(i => i.Status).ToDictionary(g => g.Key, g => g.Count())
                };
            }
        }

        private async Task CompressFileAsync(string sourcePath, string targetPath, CompressionType compression)
        {
            switch (compression)
            {
                case CompressionType.GZip:
                    await using (var sourceStream = File.OpenRead(sourcePath))
                    await using (var targetStream = File.Create(targetPath))
                    await using (var gzipStream = new GZipStream(targetStream, CompressionLevel.Optimal))
                    {
                        await sourceStream.CopyToAsync(gzipStream);
                    }
                    break;

                case CompressionType.Zip:
                    using (var archive = ZipFile.Open(targetPath, ZipArchiveMode.Create))
                    {
                        archive.CreateEntryFromFile(sourcePath, Path.GetFileName(sourcePath));
                    }
                    break;

                default:
                    File.Copy(sourcePath, targetPath);
                    break;
            }
        }

        private async Task DecompressFileAsync(string sourcePath, string targetPath, CompressionType compression)
        {
            switch (compression)
            {
                case CompressionType.GZip:
                    await using (var sourceStream = File.OpenRead(sourcePath))
                    await using (var gzipStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                    await using (var targetStream = File.Create(targetPath))
                    {
                        await gzipStream.CopyToAsync(targetStream);
                    }
                    break;

                case CompressionType.Zip:
                    using (var archive = ZipFile.OpenRead(sourcePath))
                    {
                        var entry = archive.Entries.FirstOrDefault();
                        entry?.ExtractToFile(targetPath, true);
                    }
                    break;

                default:
                    File.Copy(sourcePath, targetPath, true);
                    break;
            }
        }

        private async Task<string> ComputeChecksumAsync(string filePath)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            await using var stream = File.OpenRead(filePath);
            var hash = await sha256.ComputeHashAsync(stream);
            return Convert.ToHexString(hash);
        }

        /// <summary>
        /// Save archive index to file
        /// </summary>
        public async Task SaveIndexAsync()
        {
            var indexPath = Path.Combine(_archiveBasePath, "archive_index.json");
            var json = JsonSerializer.Serialize(_items.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(indexPath, json);
        }

        /// <summary>
        /// Load archive index from file
        /// </summary>
        public async Task LoadIndexAsync()
        {
            var indexPath = Path.Combine(_archiveBasePath, "archive_index.json");
            if (File.Exists(indexPath))
            {
                var json = await File.ReadAllTextAsync(indexPath);
                var items = JsonSerializer.Deserialize<List<ArchiveItem>>(json);
                if (items != null)
                {
                    lock (_lock)
                    {
                        _items.Clear();
                        foreach (var item in items)
                        {
                            _items[item.Id] = item;
                        }
                    }
                }
            }
        }
    }
}
