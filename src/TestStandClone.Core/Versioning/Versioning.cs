// =============================================================================
// Versioning.cs - Version control and sequence versioning
// Part of TestStandClone.Core - TestStand-like test automation framework
// =============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TestStandClone.Core.Versioning
{
    /// <summary>
    /// Sequence version information.
    /// Similar to TestStand's sequence file versioning.
    /// </summary>
    public class SequenceVersion
    {
        /// <summary>
        /// Gets or sets the major version number.
        /// </summary>
        public int Major { get; set; } = 1;

        /// <summary>
        /// Gets or sets the minor version number.
        /// </summary>
        public int Minor { get; set; } = 0;

        /// <summary>
        /// Gets or sets the patch version number.
        /// </summary>
        public int Patch { get; set; } = 0;

        /// <summary>
        /// Gets or sets the build number.
        /// </summary>
        public int Build { get; set; } = 0;

        /// <summary>
        /// Gets the version string.
        /// </summary>
        public string VersionString => $"{Major}.{Minor}.{Patch}.{Build}";

        /// <summary>
        /// Creates a default version.
        /// </summary>
        public SequenceVersion() { }

        /// <summary>
        /// Creates a version with specified values.
        /// </summary>
        public SequenceVersion(int major, int minor, int patch = 0, int build = 0)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Build = build;
        }

        /// <summary>
        /// Parses a version string.
        /// </summary>
        public static SequenceVersion Parse(string versionString)
        {
            var parts = versionString.Split('.');
            return new SequenceVersion
            {
                Major = parts.Length > 0 ? int.TryParse(parts[0], out int major) ? major : 0 : 0,
                Minor = parts.Length > 1 ? int.TryParse(parts[1], out int minor) ? minor : 0 : 0,
                Patch = parts.Length > 2 ? int.TryParse(parts[2], out int patch) ? patch : 0 : 0,
                Build = parts.Length > 3 ? int.TryParse(parts[3], out int build) ? build : 0 : 0
            };
        }

        /// <summary>
        /// Increments the major version.
        /// </summary>
        public void IncrementMajor()
        {
            Major++;
            Minor = 0;
            Patch = 0;
            Build = 0;
        }

        /// <summary>
        /// Increments the minor version.
        /// </summary>
        public void IncrementMinor()
        {
            Minor++;
            Patch = 0;
            Build = 0;
        }

        /// <summary>
        /// Increments the patch version.
        /// </summary>
        public void IncrementPatch()
        {
            Patch++;
            Build = 0;
        }

        /// <summary>
        /// Increments the build number.
        /// </summary>
        public void IncrementBuild()
        {
            Build++;
        }

        /// <summary>
        /// Compares two versions.
        /// </summary>
        public int CompareTo(SequenceVersion other)
        {
            if (Major != other.Major) return Major.CompareTo(other.Major);
            if (Minor != other.Minor) return Minor.CompareTo(other.Minor);
            if (Patch != other.Patch) return Patch.CompareTo(other.Patch);
            return Build.CompareTo(other.Build);
        }

        public override string ToString() => VersionString;
    }

    /// <summary>
    /// Sequence file metadata.
    /// </summary>
    public class SequenceMetadata
    {
        /// <summary>
        /// Gets or sets the sequence name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sequence description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        public SequenceVersion Version { get; set; } = new();

        /// <summary>
        /// Gets or sets the author.
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the company.
        /// </summary>
        public string Company { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the last modified date.
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the created by user.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the modified by user.
        /// </summary>
        public string ModifiedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file format version.
        /// </summary>
        public string FileFormatVersion { get; set; } = "1.0";

        /// <summary>
        /// Gets or sets the minimum engine version required.
        /// </summary>
        public string MinEngineVersion { get; set; } = "1.0";

        /// <summary>
        /// Gets or sets custom properties.
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; } = new();

        /// <summary>
        /// Gets or sets the revision history.
        /// </summary>
        public List<RevisionEntry> RevisionHistory { get; set; } = new();

        /// <summary>
        /// Updates the modified info.
        /// </summary>
        public void UpdateModified(string userName)
        {
            ModifiedDate = DateTime.Now;
            ModifiedBy = userName;
        }

        /// <summary>
        /// Adds a revision entry.
        /// </summary>
        public void AddRevision(string description, string author)
        {
            RevisionHistory.Add(new RevisionEntry
            {
                Version = Version.VersionString,
                Date = DateTime.Now,
                Author = author,
                Description = description
            });
        }

        /// <summary>
        /// Serializes to JSON.
        /// </summary>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Deserializes from JSON.
        /// </summary>
        public static SequenceMetadata? FromJson(string json)
        {
            return JsonSerializer.Deserialize<SequenceMetadata>(json);
        }
    }

    /// <summary>
    /// Revision history entry.
    /// </summary>
    public class RevisionEntry
    {
        /// <summary>
        /// Gets or sets the version at this revision.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the revision date.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the author.
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the revision description.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Version compatibility checker.
    /// </summary>
    public class VersionCompatibility
    {
        /// <summary>
        /// Gets the current engine version.
        /// </summary>
        public static SequenceVersion CurrentEngineVersion { get; } = new(1, 0, 0, 0);

        /// <summary>
        /// Checks if a file version is compatible.
        /// </summary>
        public static CompatibilityResult CheckCompatibility(string fileFormatVersion, string minEngineVersion)
        {
            var result = new CompatibilityResult();
            
            var fileVersion = SequenceVersion.Parse(fileFormatVersion);
            var minVersion = SequenceVersion.Parse(minEngineVersion);

            // Check if engine meets minimum requirements
            if (CurrentEngineVersion.CompareTo(minVersion) < 0)
            {
                result.IsCompatible = false;
                result.Warnings.Add($"Sequence requires engine version {minEngineVersion} or later");
            }

            // Check file format version
            if (fileVersion.Major > 1)
            {
                result.IsCompatible = false;
                result.Warnings.Add($"Unsupported file format version {fileFormatVersion}");
            }

            return result;
        }
    }

    /// <summary>
    /// Result of a compatibility check.
    /// </summary>
    public class CompatibilityResult
    {
        /// <summary>
        /// Gets or sets whether the file is compatible.
        /// </summary>
        public bool IsCompatible { get; set; } = true;

        /// <summary>
        /// Gets compatibility warnings.
        /// </summary>
        public List<string> Warnings { get; } = new();

        /// <summary>
        /// Gets compatibility errors.
        /// </summary>
        public List<string> Errors { get; } = new();
    }

    /// <summary>
    /// Sequence change tracker.
    /// </summary>
    public class ChangeTracker
    {
        private readonly List<ChangeEntry> _changes = new();
        private int _currentIndex = -1;

        /// <summary>
        /// Gets whether there are unsaved changes.
        /// </summary>
        public bool HasUnsavedChanges => _currentIndex >= 0;

        /// <summary>
        /// Gets whether undo is available.
        /// </summary>
        public bool CanUndo => _currentIndex >= 0;

        /// <summary>
        /// Gets whether redo is available.
        /// </summary>
        public bool CanRedo => _currentIndex < _changes.Count - 1;

        /// <summary>
        /// Records a change.
        /// </summary>
        public void RecordChange(string changeType, string description, object? oldValue, object? newValue)
        {
            // Remove any changes after current index (invalidate redo history)
            while (_changes.Count > _currentIndex + 1)
            {
                _changes.RemoveAt(_changes.Count - 1);
            }

            _changes.Add(new ChangeEntry
            {
                ChangeType = changeType,
                Description = description,
                OldValue = oldValue,
                NewValue = newValue,
                Timestamp = DateTime.Now
            });
            _currentIndex++;
        }

        /// <summary>
        /// Gets the current change for undo.
        /// </summary>
        public ChangeEntry? GetUndoChange()
        {
            if (!CanUndo) return null;
            return _changes[_currentIndex--];
        }

        /// <summary>
        /// Gets the next change for redo.
        /// </summary>
        public ChangeEntry? GetRedoChange()
        {
            if (!CanRedo) return null;
            return _changes[++_currentIndex];
        }

        /// <summary>
        /// Marks changes as saved.
        /// </summary>
        public void MarkAsSaved()
        {
            _changes.Clear();
            _currentIndex = -1;
        }

        /// <summary>
        /// Gets all changes.
        /// </summary>
        public IReadOnlyList<ChangeEntry> GetAllChanges() => _changes.AsReadOnly();
    }

    /// <summary>
    /// Change entry.
    /// </summary>
    public class ChangeEntry
    {
        public string ChangeType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
