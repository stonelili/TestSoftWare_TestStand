using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.Navigation
{
    /// <summary>
    /// Navigation item representing a location in the sequence hierarchy
    /// </summary>
    public class NavigationItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SequenceId { get; set; } = string.Empty;
        public string SequenceName { get; set; } = string.Empty;
        public string StepId { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public int StepIndex { get; set; }
        public NavigationItemType ItemType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();

        public string DisplayName => !string.IsNullOrEmpty(StepName) 
            ? $"{SequenceName}/{StepName}" 
            : SequenceName;
    }

    /// <summary>
    /// Types of navigation items
    /// </summary>
    public enum NavigationItemType
    {
        Sequence,
        Step,
        StepGroup,
        SubSequence,
        Bookmark
    }

    /// <summary>
    /// Bookmark for quick navigation
    /// </summary>
    public class Bookmark
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public NavigationItem Location { get; set; } = new NavigationItem();
        public DateTime Created { get; set; } = DateTime.Now;
        public string Color { get; set; } = "#0078D7";
        public bool IsTemporary { get; set; }
    }

    /// <summary>
    /// Navigation history entry
    /// </summary>
    public class NavigationHistoryEntry
    {
        public NavigationItem Item { get; set; } = new NavigationItem();
        public DateTime VisitTime { get; set; } = DateTime.Now;
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Sequence navigator for navigating through sequences and steps
    /// </summary>
    public class SequenceNavigator
    {
        private readonly List<NavigationHistoryEntry> _history = new List<NavigationHistoryEntry>();
        private int _currentIndex = -1;
        private NavigationItem? _currentLocation;
        private readonly List<Bookmark> _bookmarks = new List<Bookmark>();
        private readonly int _maxHistorySize;

        public event EventHandler<NavigationItem>? LocationChanged;
        public event EventHandler<Bookmark>? BookmarkAdded;
        public event EventHandler<Bookmark>? BookmarkRemoved;

        public NavigationItem? CurrentLocation => _currentLocation;
        public IReadOnlyList<NavigationHistoryEntry> History => _history.AsReadOnly();
        public IReadOnlyList<Bookmark> Bookmarks => _bookmarks.AsReadOnly();
        public bool CanGoBack => _currentIndex > 0;
        public bool CanGoForward => _currentIndex < _history.Count - 1;

        public SequenceNavigator(int maxHistorySize = 100)
        {
            _maxHistorySize = maxHistorySize;
        }

        /// <summary>
        /// Navigate to a specific location
        /// </summary>
        public void NavigateTo(NavigationItem item)
        {
            if (_currentLocation != null)
            {
                // Update duration of previous location
                if (_currentIndex >= 0 && _currentIndex < _history.Count)
                {
                    _history[_currentIndex].Duration = DateTime.Now - _history[_currentIndex].VisitTime;
                }
            }

            // Remove forward history if we're not at the end
            if (_currentIndex < _history.Count - 1)
            {
                _history.RemoveRange(_currentIndex + 1, _history.Count - _currentIndex - 1);
            }

            // Add new entry
            var entry = new NavigationHistoryEntry
            {
                Item = item,
                VisitTime = DateTime.Now
            };
            _history.Add(entry);

            // Trim history if needed
            while (_history.Count > _maxHistorySize)
            {
                _history.RemoveAt(0);
            }

            _currentIndex = _history.Count - 1;
            _currentLocation = item;

            LocationChanged?.Invoke(this, item);
        }

        /// <summary>
        /// Navigate to a sequence
        /// </summary>
        public void NavigateToSequence(string sequenceId, string sequenceName)
        {
            NavigateTo(new NavigationItem
            {
                SequenceId = sequenceId,
                SequenceName = sequenceName,
                ItemType = NavigationItemType.Sequence
            });
        }

        /// <summary>
        /// Navigate to a step
        /// </summary>
        public void NavigateToStep(string sequenceId, string sequenceName, string stepId, string stepName, int stepIndex)
        {
            NavigateTo(new NavigationItem
            {
                SequenceId = sequenceId,
                SequenceName = sequenceName,
                StepId = stepId,
                StepName = stepName,
                StepIndex = stepIndex,
                ItemType = NavigationItemType.Step
            });
        }

        /// <summary>
        /// Go back in history
        /// </summary>
        public NavigationItem? GoBack()
        {
            if (!CanGoBack) return null;

            _currentIndex--;
            _currentLocation = _history[_currentIndex].Item;
            LocationChanged?.Invoke(this, _currentLocation);
            return _currentLocation;
        }

        /// <summary>
        /// Go forward in history
        /// </summary>
        public NavigationItem? GoForward()
        {
            if (!CanGoForward) return null;

            _currentIndex++;
            _currentLocation = _history[_currentIndex].Item;
            LocationChanged?.Invoke(this, _currentLocation);
            return _currentLocation;
        }

        /// <summary>
        /// Add a bookmark at the current location
        /// </summary>
        public Bookmark? AddBookmark(string name, string description = "", string color = "#0078D7")
        {
            if (_currentLocation == null) return null;

            var bookmark = new Bookmark
            {
                Name = name,
                Description = description,
                Location = _currentLocation,
                Color = color
            };

            _bookmarks.Add(bookmark);
            BookmarkAdded?.Invoke(this, bookmark);
            return bookmark;
        }

        /// <summary>
        /// Remove a bookmark
        /// </summary>
        public bool RemoveBookmark(string bookmarkId)
        {
            var bookmark = _bookmarks.FirstOrDefault(b => b.Id == bookmarkId);
            if (bookmark == null) return false;

            _bookmarks.Remove(bookmark);
            BookmarkRemoved?.Invoke(this, bookmark);
            return true;
        }

        /// <summary>
        /// Navigate to a bookmark
        /// </summary>
        public void NavigateToBookmark(string bookmarkId)
        {
            var bookmark = _bookmarks.FirstOrDefault(b => b.Id == bookmarkId);
            if (bookmark != null)
            {
                NavigateTo(bookmark.Location);
            }
        }

        /// <summary>
        /// Get frequently visited locations
        /// </summary>
        public List<NavigationItem> GetFrequentLocations(int count = 10)
        {
            return _history
                .GroupBy(h => h.Item.Id)
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => g.First().Item)
                .ToList();
        }

        /// <summary>
        /// Get recent locations
        /// </summary>
        public List<NavigationItem> GetRecentLocations(int count = 10)
        {
            return _history
                .OrderByDescending(h => h.VisitTime)
                .Take(count)
                .Select(h => h.Item)
                .ToList();
        }

        /// <summary>
        /// Clear navigation history
        /// </summary>
        public void ClearHistory()
        {
            _history.Clear();
            _currentIndex = -1;
        }

        /// <summary>
        /// Clear all bookmarks
        /// </summary>
        public void ClearBookmarks()
        {
            _bookmarks.Clear();
        }
    }

    /// <summary>
    /// Navigation manager singleton for global navigation
    /// </summary>
    public class NavigationManager
    {
        private static readonly Lazy<NavigationManager> _instance = 
            new Lazy<NavigationManager>(() => new NavigationManager());
        
        public static NavigationManager Instance => _instance.Value;

        private readonly Dictionary<string, SequenceNavigator> _navigators = 
            new Dictionary<string, SequenceNavigator>();
        
        private SequenceNavigator _defaultNavigator = new SequenceNavigator();

        public SequenceNavigator DefaultNavigator => _defaultNavigator;

        private NavigationManager() { }

        /// <summary>
        /// Get or create a navigator for a specific context
        /// </summary>
        public SequenceNavigator GetNavigator(string contextId)
        {
            if (!_navigators.ContainsKey(contextId))
            {
                _navigators[contextId] = new SequenceNavigator();
            }
            return _navigators[contextId];
        }

        /// <summary>
        /// Remove a navigator
        /// </summary>
        public void RemoveNavigator(string contextId)
        {
            _navigators.Remove(contextId);
        }
    }
}
