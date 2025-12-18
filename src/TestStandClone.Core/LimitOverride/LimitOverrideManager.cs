using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.LimitOverride
{
    /// <summary>
    /// Override scope
    /// </summary>
    public enum OverrideScope
    {
        Step,
        Sequence,
        Session,
        Global
    }

    /// <summary>
    /// Override status
    /// </summary>
    public enum OverrideStatus
    {
        Active,
        Expired,
        Disabled
    }

    /// <summary>
    /// Limit override definition
    /// </summary>
    public class LimitOverrideDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string StepName { get; set; } = string.Empty;
        public string ParameterName { get; set; } = string.Empty;
        public double? OriginalLowLimit { get; set; }
        public double? OriginalHighLimit { get; set; }
        public double? OverrideLowLimit { get; set; }
        public double? OverrideHighLimit { get; set; }
        public OverrideScope Scope { get; set; } = OverrideScope.Session;
        public OverrideStatus Status { get; set; } = OverrideStatus.Active;
        public string Reason { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public int? MaxUses { get; set; }
        public int UseCount { get; set; }
    }

    /// <summary>
    /// Override request for approval workflow
    /// </summary>
    public class OverrideRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public LimitOverrideDefinition Override { get; set; } = null!;
        public string RequestedBy { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
    }

    /// <summary>
    /// Limit values
    /// </summary>
    public class LimitValues
    {
        public double? LowLimit { get; set; }
        public double? HighLimit { get; set; }
        public bool IsOverridden { get; set; }
        public string? OverrideId { get; set; }
    }

    /// <summary>
    /// Override audit entry
    /// </summary>
    public class OverrideAuditEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string OverrideId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Limit override manager singleton
    /// </summary>
    public class LimitOverrideManager
    {
        private static readonly Lazy<LimitOverrideManager> _instance = new Lazy<LimitOverrideManager>(() => new LimitOverrideManager());
        public static LimitOverrideManager Instance => _instance.Value;

        private readonly List<LimitOverrideDefinition> _overrides = new List<LimitOverrideDefinition>();
        private readonly List<OverrideRequest> _pendingRequests = new List<OverrideRequest>();
        private readonly List<OverrideAuditEntry> _auditLog = new List<OverrideAuditEntry>();
        private readonly object _lock = new object();

        public bool RequireApproval { get; set; } = true;

        public event EventHandler<LimitOverrideDefinition>? OverrideApplied;
        public event EventHandler<LimitOverrideDefinition>? OverrideExpired;
        public event EventHandler<OverrideRequest>? ApprovalRequired;

        private LimitOverrideManager() { }

        public LimitOverrideDefinition CreateOverride(string stepName, string parameterName, 
            double? originalLow, double? originalHigh, double? overrideLow, double? overrideHigh,
            string reason, string createdBy, OverrideScope scope = OverrideScope.Session, TimeSpan? duration = null)
        {
            var limitOverride = new LimitOverrideDefinition
            {
                StepName = stepName,
                ParameterName = parameterName,
                OriginalLowLimit = originalLow,
                OriginalHighLimit = originalHigh,
                OverrideLowLimit = overrideLow,
                OverrideHighLimit = overrideHigh,
                Reason = reason,
                CreatedBy = createdBy,
                Scope = scope,
                ExpiresAt = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : null
            };

            if (RequireApproval)
            {
                var request = new OverrideRequest { Override = limitOverride, RequestedBy = createdBy };
                lock (_lock) { _pendingRequests.Add(request); }
                ApprovalRequired?.Invoke(this, request);
                return limitOverride;
            }

            ApplyOverride(limitOverride, createdBy);
            return limitOverride;
        }

        public void ApproveRequest(string requestId, string approvedBy)
        {
            lock (_lock)
            {
                var request = _pendingRequests.FirstOrDefault(r => r.Id == requestId);
                if (request == null) return;

                request.IsApproved = true;
                request.ApprovedBy = approvedBy;
                request.ApprovedAt = DateTime.UtcNow;
                request.Override.ApprovedBy = approvedBy;
                _pendingRequests.Remove(request);
                
                ApplyOverride(request.Override, approvedBy);
            }
        }

        public void RejectRequest(string requestId, string rejectedBy, string reason)
        {
            lock (_lock)
            {
                var request = _pendingRequests.FirstOrDefault(r => r.Id == requestId);
                if (request == null) return;

                request.IsApproved = false;
                request.RejectionReason = reason;
                _pendingRequests.Remove(request);

                AddAuditEntry(request.Override.Id, "Rejected", rejectedBy, new Dictionary<string, object> { { "Reason", reason } });
            }
        }

        private void ApplyOverride(LimitOverrideDefinition limitOverride, string appliedBy)
        {
            lock (_lock)
            {
                _overrides.Add(limitOverride);
                AddAuditEntry(limitOverride.Id, "Applied", appliedBy);
                OverrideApplied?.Invoke(this, limitOverride);
            }
        }

        public LimitValues GetEffectiveLimits(string stepName, string parameterName, double? defaultLow, double? defaultHigh)
        {
            lock (_lock)
            {
                CheckExpiredOverrides();

                var activeOverride = _overrides
                    .Where(o => o.StepName == stepName && o.ParameterName == parameterName && o.Status == OverrideStatus.Active)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefault();

                if (activeOverride != null)
                {
                    if (activeOverride.MaxUses.HasValue && activeOverride.UseCount >= activeOverride.MaxUses.Value)
                    {
                        activeOverride.Status = OverrideStatus.Expired;
                        return new LimitValues { LowLimit = defaultLow, HighLimit = defaultHigh };
                    }

                    activeOverride.UseCount++;
                    return new LimitValues
                    {
                        LowLimit = activeOverride.OverrideLowLimit,
                        HighLimit = activeOverride.OverrideHighLimit,
                        IsOverridden = true,
                        OverrideId = activeOverride.Id
                    };
                }

                return new LimitValues { LowLimit = defaultLow, HighLimit = defaultHigh };
            }
        }

        private void CheckExpiredOverrides()
        {
            var now = DateTime.UtcNow;
            foreach (var limitOverride in _overrides.Where(o => o.Status == OverrideStatus.Active && o.ExpiresAt.HasValue && o.ExpiresAt <= now))
            {
                limitOverride.Status = OverrideStatus.Expired;
                OverrideExpired?.Invoke(this, limitOverride);
            }
        }

        public void DisableOverride(string overrideId, string disabledBy)
        {
            lock (_lock)
            {
                var limitOverride = _overrides.FirstOrDefault(o => o.Id == overrideId);
                if (limitOverride != null)
                {
                    limitOverride.Status = OverrideStatus.Disabled;
                    AddAuditEntry(overrideId, "Disabled", disabledBy);
                }
            }
        }

        private void AddAuditEntry(string overrideId, string action, string performedBy, Dictionary<string, object>? details = null)
        {
            _auditLog.Add(new OverrideAuditEntry
            {
                OverrideId = overrideId,
                Action = action,
                PerformedBy = performedBy,
                Details = details ?? new Dictionary<string, object>()
            });
        }

        public List<LimitOverrideDefinition> GetActiveOverrides() { lock (_lock) { return _overrides.Where(o => o.Status == OverrideStatus.Active).ToList(); } }
        public List<OverrideRequest> GetPendingRequests() { lock (_lock) { return _pendingRequests.ToList(); } }
        public List<OverrideAuditEntry> GetAuditLog(string? overrideId = null) 
        { 
            lock (_lock) 
            { 
                return overrideId != null ? _auditLog.Where(a => a.OverrideId == overrideId).ToList() : _auditLog.ToList(); 
            } 
        }

        public void ClearSessionOverrides()
        {
            lock (_lock) { _overrides.RemoveAll(o => o.Scope == OverrideScope.Session); }
        }
    }
}
