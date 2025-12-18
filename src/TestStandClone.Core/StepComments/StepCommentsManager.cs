using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.StepComments
{
    /// <summary>
    /// Type of comment.
    /// </summary>
    public enum CommentType
    {
        Note,
        Warning,
        Issue,
        Todo,
        Review,
        Documentation
    }

    /// <summary>
    /// Priority level for comments.
    /// </summary>
    public enum CommentPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Status of a comment.
    /// </summary>
    public enum CommentStatus
    {
        Open,
        Acknowledged,
        InProgress,
        Resolved,
        WontFix
    }

    /// <summary>
    /// Represents a comment attached to a step.
    /// </summary>
    public class StepComment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string StepId { get; set; } = string.Empty;
        public string SequenceId { get; set; } = string.Empty;
        public CommentType Type { get; set; } = CommentType.Note;
        public CommentPriority Priority { get; set; } = CommentPriority.Medium;
        public CommentStatus Status { get; set; } = CommentStatus.Open;
        public string Content { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
        public string? AssignedTo { get; set; }
        public List<CommentReply> Replies { get; set; } = new();
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Represents a reply to a comment.
    /// </summary>
    public class CommentReply
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Query options for searching comments.
    /// </summary>
    public class CommentQuery
    {
        public string? SequenceId { get; set; }
        public string? StepId { get; set; }
        public CommentType? Type { get; set; }
        public CommentPriority? Priority { get; set; }
        public CommentStatus? Status { get; set; }
        public string? Author { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<string>? Tags { get; set; }
    }

    /// <summary>
    /// Manager for step comments.
    /// </summary>
    public class StepCommentsManager
    {
        private static StepCommentsManager? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<string, StepComment> _comments = new();

        public static StepCommentsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new StepCommentsManager();
                    }
                }
                return _instance;
            }
        }

        private StepCommentsManager() { }

        /// <summary>
        /// Adds a comment to a step.
        /// </summary>
        public void AddComment(StepComment comment)
        {
            _comments[comment.Id] = comment;
        }

        /// <summary>
        /// Gets a comment by ID.
        /// </summary>
        public StepComment? GetComment(string commentId)
        {
            return _comments.TryGetValue(commentId, out var comment) ? comment : null;
        }

        /// <summary>
        /// Gets comments for a step.
        /// </summary>
        public IEnumerable<StepComment> GetStepComments(string stepId)
        {
            return _comments.Values.Where(c => c.StepId == stepId);
        }

        /// <summary>
        /// Gets comments for a sequence.
        /// </summary>
        public IEnumerable<StepComment> GetSequenceComments(string sequenceId)
        {
            return _comments.Values.Where(c => c.SequenceId == sequenceId);
        }

        /// <summary>
        /// Updates a comment.
        /// </summary>
        public void UpdateComment(StepComment comment)
        {
            if (_comments.ContainsKey(comment.Id))
            {
                comment.ModifiedAt = DateTime.UtcNow;
                _comments[comment.Id] = comment;
            }
        }

        /// <summary>
        /// Removes a comment.
        /// </summary>
        public bool RemoveComment(string commentId)
        {
            return _comments.Remove(commentId);
        }

        /// <summary>
        /// Adds a reply to a comment.
        /// </summary>
        public void AddReply(string commentId, CommentReply reply)
        {
            if (_comments.TryGetValue(commentId, out var comment))
            {
                comment.Replies.Add(reply);
                comment.ModifiedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Changes the status of a comment.
        /// </summary>
        public void ChangeStatus(string commentId, CommentStatus status)
        {
            if (_comments.TryGetValue(commentId, out var comment))
            {
                comment.Status = status;
                comment.ModifiedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Assigns a comment to a user.
        /// </summary>
        public void AssignComment(string commentId, string assignee)
        {
            if (_comments.TryGetValue(commentId, out var comment))
            {
                comment.AssignedTo = assignee;
                comment.ModifiedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Queries comments based on criteria.
        /// </summary>
        public IEnumerable<StepComment> QueryComments(CommentQuery query)
        {
            var results = _comments.Values.AsEnumerable();

            if (!string.IsNullOrEmpty(query.SequenceId))
                results = results.Where(c => c.SequenceId == query.SequenceId);

            if (!string.IsNullOrEmpty(query.StepId))
                results = results.Where(c => c.StepId == query.StepId);

            if (query.Type.HasValue)
                results = results.Where(c => c.Type == query.Type.Value);

            if (query.Priority.HasValue)
                results = results.Where(c => c.Priority == query.Priority.Value);

            if (query.Status.HasValue)
                results = results.Where(c => c.Status == query.Status.Value);

            if (!string.IsNullOrEmpty(query.Author))
                results = results.Where(c => c.Author == query.Author);

            if (!string.IsNullOrEmpty(query.AssignedTo))
                results = results.Where(c => c.AssignedTo == query.AssignedTo);

            if (query.FromDate.HasValue)
                results = results.Where(c => c.CreatedAt >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                results = results.Where(c => c.CreatedAt <= query.ToDate.Value);

            if (query.Tags != null && query.Tags.Count > 0)
                results = results.Where(c => c.Tags.Any(t => query.Tags.Contains(t)));

            return results;
        }

        /// <summary>
        /// Gets comment statistics.
        /// </summary>
        public CommentStatistics GetStatistics(string? sequenceId = null)
        {
            var comments = sequenceId == null
                ? _comments.Values
                : _comments.Values.Where(c => c.SequenceId == sequenceId);

            return new CommentStatistics
            {
                TotalCount = comments.Count(),
                OpenCount = comments.Count(c => c.Status == CommentStatus.Open),
                ResolvedCount = comments.Count(c => c.Status == CommentStatus.Resolved),
                HighPriorityCount = comments.Count(c => c.Priority == CommentPriority.High || c.Priority == CommentPriority.Critical),
                TodoCount = comments.Count(c => c.Type == CommentType.Todo),
                IssueCount = comments.Count(c => c.Type == CommentType.Issue)
            };
        }
    }

    /// <summary>
    /// Statistics for comments.
    /// </summary>
    public class CommentStatistics
    {
        public int TotalCount { get; set; }
        public int OpenCount { get; set; }
        public int ResolvedCount { get; set; }
        public int HighPriorityCount { get; set; }
        public int TodoCount { get; set; }
        public int IssueCount { get; set; }
    }
}
