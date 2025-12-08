using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TestStandClone.Core.Workspace
{
    #region Workspace Classes

    /// <summary>
    /// Represents a workspace file reference
    /// </summary>
    public class WorkspaceFile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public WorkspaceFileType FileType { get; set; } = WorkspaceFileType.Sequence;
        public bool IsOpen { get; set; }
        public DateTime LastAccessed { get; set; } = DateTime.Now;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Types of files in a workspace
    /// </summary>
    public enum WorkspaceFileType
    {
        Sequence,
        Limits,
        ProcessModel,
        CodeModule,
        Report,
        Configuration,
        Other
    }

    /// <summary>
    /// Represents a folder in the workspace
    /// </summary>
    public class WorkspaceFolder
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public List<WorkspaceFolder> SubFolders { get; set; } = new();
        public List<WorkspaceFile> Files { get; set; } = new();
        public bool IsExpanded { get; set; } = true;
    }

    /// <summary>
    /// Represents a complete workspace
    /// </summary>
    public class WorkspaceDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
        public WorkspaceFolder RootFolder { get; set; } = new();
        public List<string> RecentFiles { get; set; } = new();
        public Dictionary<string, string> Settings { get; set; } = new();
        public List<string> SearchPaths { get; set; } = new();
    }

    /// <summary>
    /// Manages workspaces
    /// </summary>
    public class WorkspaceManager
    {
        private static readonly Lazy<WorkspaceManager> _instance = new(() => new WorkspaceManager());
        public static WorkspaceManager Instance => _instance.Value;

        private WorkspaceDefinition? _currentWorkspace;
        private readonly List<WorkspaceDefinition> _recentWorkspaces = new();
        private const int MaxRecentWorkspaces = 10;

        public event EventHandler<WorkspaceDefinition>? WorkspaceOpened;
        public event EventHandler? WorkspaceClosed;
        public event EventHandler<WorkspaceFile>? FileOpened;

        public WorkspaceDefinition? CurrentWorkspace => _currentWorkspace;
        public IReadOnlyList<WorkspaceDefinition> RecentWorkspaces => _recentWorkspaces.AsReadOnly();

        public WorkspaceDefinition CreateWorkspace(string name, string path)
        {
            var workspace = new WorkspaceDefinition
            {
                Name = name,
                FilePath = path,
                RootFolder = new WorkspaceFolder { Name = name }
            };

            _currentWorkspace = workspace;
            SaveWorkspace(workspace);
            WorkspaceOpened?.Invoke(this, workspace);

            return workspace;
        }

        public void OpenWorkspace(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Workspace file not found", path);

            var json = File.ReadAllText(path);
            var workspace = JsonSerializer.Deserialize<WorkspaceDefinition>(json);
            if (workspace != null)
            {
                _currentWorkspace = workspace;
                AddToRecentWorkspaces(workspace);
                WorkspaceOpened?.Invoke(this, workspace);
            }
        }

        public void SaveWorkspace(WorkspaceDefinition workspace)
        {
            workspace.ModifiedAt = DateTime.Now;
            var json = JsonSerializer.Serialize(workspace, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(workspace.FilePath, json);
        }

        public void CloseWorkspace()
        {
            if (_currentWorkspace != null)
            {
                SaveWorkspace(_currentWorkspace);
                _currentWorkspace = null;
                WorkspaceClosed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void AddFile(WorkspaceFile file, string? folderId = null)
        {
            if (_currentWorkspace == null) return;

            var folder = folderId != null 
                ? FindFolder(_currentWorkspace.RootFolder, folderId) 
                : _currentWorkspace.RootFolder;

            folder?.Files.Add(file);
        }

        public void AddFolder(WorkspaceFolder folder, string? parentFolderId = null)
        {
            if (_currentWorkspace == null) return;

            var parentFolder = parentFolderId != null 
                ? FindFolder(_currentWorkspace.RootFolder, parentFolderId) 
                : _currentWorkspace.RootFolder;

            parentFolder?.SubFolders.Add(folder);
        }

        public void OpenFile(string fileId)
        {
            if (_currentWorkspace == null) return;

            var file = FindFile(_currentWorkspace.RootFolder, fileId);
            if (file != null)
            {
                file.IsOpen = true;
                file.LastAccessed = DateTime.Now;
                
                if (!_currentWorkspace.RecentFiles.Contains(file.FilePath))
                {
                    _currentWorkspace.RecentFiles.Insert(0, file.FilePath);
                    if (_currentWorkspace.RecentFiles.Count > 20)
                        _currentWorkspace.RecentFiles.RemoveAt(20);
                }

                FileOpened?.Invoke(this, file);
            }
        }

        private WorkspaceFolder? FindFolder(WorkspaceFolder root, string folderId)
        {
            if (root.Id == folderId) return root;

            foreach (var subfolder in root.SubFolders)
            {
                var found = FindFolder(subfolder, folderId);
                if (found != null) return found;
            }

            return null;
        }

        private WorkspaceFile? FindFile(WorkspaceFolder root, string fileId)
        {
            var file = root.Files.FirstOrDefault(f => f.Id == fileId);
            if (file != null) return file;

            foreach (var subfolder in root.SubFolders)
            {
                var found = FindFile(subfolder, fileId);
                if (found != null) return found;
            }

            return null;
        }

        private void AddToRecentWorkspaces(WorkspaceDefinition workspace)
        {
            _recentWorkspaces.RemoveAll(w => w.FilePath == workspace.FilePath);
            _recentWorkspaces.Insert(0, workspace);
            if (_recentWorkspaces.Count > MaxRecentWorkspaces)
                _recentWorkspaces.RemoveAt(MaxRecentWorkspaces);
        }
    }

    #endregion
}
