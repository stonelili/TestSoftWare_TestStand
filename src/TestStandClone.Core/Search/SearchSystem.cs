using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TestStandClone.Core.Search
{
    /// <summary>
    /// Search scope
    /// </summary>
    [Flags]
    public enum SearchScope
    {
        None = 0,
        StepNames = 1,
        StepTypes = 2,
        StepProperties = 4,
        Variables = 8,
        Parameters = 16,
        Comments = 32,
        All = StepNames | StepTypes | StepProperties | Variables | Parameters | Comments
    }

    /// <summary>
    /// Search options
    /// </summary>
    public class SearchOptions
    {
        public string Query { get; set; } = string.Empty;
        public SearchScope Scope { get; set; } = SearchScope.All;
        public bool CaseSensitive { get; set; }
        public bool WholeWord { get; set; }
        public bool UseRegex { get; set; }
        public int MaxResults { get; set; } = 100;
        public string? InSequence { get; set; }
    }

    /// <summary>
    /// Search result item
    /// </summary>
    public class SearchResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public SearchResultType Type { get; set; }
        public string SequenceId { get; set; } = string.Empty;
        public string SequenceName { get; set; } = string.Empty;
        public string? StepId { get; set; }
        public string? StepName { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string MatchedText { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public int CharacterPosition { get; set; }
        public double Relevance { get; set; } = 1.0;
    }

    /// <summary>
    /// Search result type
    /// </summary>
    public enum SearchResultType
    {
        StepName,
        StepType,
        StepProperty,
        Variable,
        Parameter,
        Comment
    }

    /// <summary>
    /// Searchable item representing data to be searched
    /// </summary>
    public class SearchableItem
    {
        public string Id { get; set; } = string.Empty;
        public SearchResultType Type { get; set; }
        public string SequenceId { get; set; } = string.Empty;
        public string SequenceName { get; set; } = string.Empty;
        public string? StepId { get; set; }
        public string? StepName { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// Search engine for finding items in sequences
    /// </summary>
    public class SearchEngine
    {
        private readonly List<SearchableItem> _index = new List<SearchableItem>();

        /// <summary>
        /// Add an item to the search index
        /// </summary>
        public void AddToIndex(SearchableItem item)
        {
            _index.Add(item);
        }

        /// <summary>
        /// Clear the search index
        /// </summary>
        public void ClearIndex()
        {
            _index.Clear();
        }

        /// <summary>
        /// Search the index
        /// </summary>
        public List<SearchResult> Search(SearchOptions options)
        {
            var results = new List<SearchResult>();
            var pattern = BuildSearchPattern(options);

            if (pattern == null) return results;

            foreach (var item in _index)
            {
                // Check scope
                if (!IsScopeMatch(item.Type, options.Scope)) continue;

                // Check sequence filter
                if (!string.IsNullOrEmpty(options.InSequence) && 
                    !item.SequenceName.Equals(options.InSequence, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Search for matches
                var matches = pattern.Matches(item.Text);
                foreach (Match match in matches)
                {
                    results.Add(new SearchResult
                    {
                        Type = item.Type,
                        SequenceId = item.SequenceId,
                        SequenceName = item.SequenceName,
                        StepId = item.StepId,
                        StepName = item.StepName,
                        PropertyName = item.PropertyName,
                        MatchedText = match.Value,
                        Context = GetContext(item.Text, match.Index, 50),
                        CharacterPosition = match.Index
                    });

                    if (results.Count >= options.MaxResults) break;
                }

                if (results.Count >= options.MaxResults) break;
            }

            // Sort by relevance
            return results.OrderByDescending(r => r.Relevance).ToList();
        }

        /// <summary>
        /// Find and replace
        /// </summary>
        public List<ReplaceResult> FindAndReplace(
            SearchOptions searchOptions, 
            string replacement, 
            bool replaceAll = false)
        {
            var results = new List<ReplaceResult>();
            var pattern = BuildSearchPattern(searchOptions);

            if (pattern == null) return results;

            foreach (var item in _index)
            {
                if (!IsScopeMatch(item.Type, searchOptions.Scope)) continue;

                var matches = pattern.Matches(item.Text);
                if (matches.Count == 0) continue;

                var newText = replaceAll 
                    ? pattern.Replace(item.Text, replacement)
                    : pattern.Replace(item.Text, replacement, 1);

                results.Add(new ReplaceResult
                {
                    Item = item,
                    OriginalText = item.Text,
                    NewText = newText,
                    ReplacementCount = replaceAll ? matches.Count : 1
                });

                if (!replaceAll) break;
            }

            return results;
        }

        /// <summary>
        /// Get search suggestions based on index
        /// </summary>
        public List<string> GetSuggestions(string partial, int maxSuggestions = 10)
        {
            if (string.IsNullOrEmpty(partial)) return new List<string>();

            return _index
                .SelectMany(item => ExtractWords(item.Text))
                .Where(word => word.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(maxSuggestions)
                .ToList();
        }

        /// <summary>
        /// Get recent searches
        /// </summary>
        public List<string> RecentSearches { get; } = new List<string>();

        /// <summary>
        /// Add to recent searches
        /// </summary>
        public void AddToRecentSearches(string query)
        {
            RecentSearches.Remove(query);
            RecentSearches.Insert(0, query);
            if (RecentSearches.Count > 20)
            {
                RecentSearches.RemoveAt(RecentSearches.Count - 1);
            }
        }

        private Regex? BuildSearchPattern(SearchOptions options)
        {
            if (string.IsNullOrEmpty(options.Query)) return null;

            var pattern = options.Query;

            if (!options.UseRegex)
            {
                pattern = Regex.Escape(pattern);
            }

            if (options.WholeWord)
            {
                pattern = $@"\b{pattern}\b";
            }

            var regexOptions = RegexOptions.Compiled;
            if (!options.CaseSensitive)
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }

            try
            {
                return new Regex(pattern, regexOptions);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsScopeMatch(SearchResultType type, SearchScope scope)
        {
            return type switch
            {
                SearchResultType.StepName => scope.HasFlag(SearchScope.StepNames),
                SearchResultType.StepType => scope.HasFlag(SearchScope.StepTypes),
                SearchResultType.StepProperty => scope.HasFlag(SearchScope.StepProperties),
                SearchResultType.Variable => scope.HasFlag(SearchScope.Variables),
                SearchResultType.Parameter => scope.HasFlag(SearchScope.Parameters),
                SearchResultType.Comment => scope.HasFlag(SearchScope.Comments),
                _ => false
            };
        }

        private static string GetContext(string text, int position, int contextLength)
        {
            var start = Math.Max(0, position - contextLength);
            var end = Math.Min(text.Length, position + contextLength);
            var context = text.Substring(start, end - start);
            
            if (start > 0) context = "..." + context;
            if (end < text.Length) context = context + "...";
            
            return context;
        }

        private static IEnumerable<string> ExtractWords(string text)
        {
            return Regex.Matches(text, @"\b\w+\b")
                .Cast<Match>()
                .Select(m => m.Value)
                .Distinct();
        }
    }

    /// <summary>
    /// Replace result
    /// </summary>
    public class ReplaceResult
    {
        public SearchableItem Item { get; set; } = new SearchableItem();
        public string OriginalText { get; set; } = string.Empty;
        public string NewText { get; set; } = string.Empty;
        public int ReplacementCount { get; set; }
    }

    /// <summary>
    /// Search manager singleton
    /// </summary>
    public class SearchManager
    {
        private static readonly Lazy<SearchManager> _instance = 
            new Lazy<SearchManager>(() => new SearchManager());
        
        public static SearchManager Instance => _instance.Value;

        private readonly SearchEngine _engine = new SearchEngine();

        private SearchManager() { }

        public SearchEngine Engine => _engine;

        /// <summary>
        /// Quick search in sequences
        /// </summary>
        public List<SearchResult> QuickSearch(string query)
        {
            return _engine.Search(new SearchOptions
            {
                Query = query,
                Scope = SearchScope.All,
                MaxResults = 50
            });
        }

        /// <summary>
        /// Advanced search with options
        /// </summary>
        public List<SearchResult> AdvancedSearch(SearchOptions options)
        {
            _engine.AddToRecentSearches(options.Query);
            return _engine.Search(options);
        }

        /// <summary>
        /// Find and replace
        /// </summary>
        public List<ReplaceResult> FindReplace(
            string searchQuery, 
            string replacement,
            SearchScope scope = SearchScope.All,
            bool replaceAll = false)
        {
            return _engine.FindAndReplace(
                new SearchOptions { Query = searchQuery, Scope = scope },
                replacement,
                replaceAll);
        }
    }
}
