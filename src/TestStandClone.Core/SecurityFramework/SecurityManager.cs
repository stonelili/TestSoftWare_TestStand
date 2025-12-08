using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TestStandClone.Core.SecurityFramework
{
    public enum SecurityLevel { Public, Internal, Confidential, Secret }
    public enum AccessType { Read, Write, Execute, Delete, Admin }

    public class SecurityToken
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public List<AccessType> Permissions { get; set; } = new();
        public DateTime IssuedAt { get; set; } = DateTime.Now;
        public DateTime ExpiresAt { get; set; } = DateTime.Now.AddHours(8);
        public string IpAddress { get; set; } = string.Empty;
        public bool IsValid => DateTime.Now < ExpiresAt;
    }

    public class SecurityPolicy
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public List<AccessType> RequiredPermissions { get; set; } = new();
        public List<string> AllowedRoles { get; set; } = new();
        public SecurityLevel MinimumLevel { get; set; } = SecurityLevel.Internal;
        public bool RequiresMFA { get; set; } = false;
    }

    public class SecurityAuditEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public bool Allowed { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
    }

    public class SecurityManager
    {
        private static readonly Lazy<SecurityManager> _instance = new(() => new SecurityManager());
        public static SecurityManager Instance => _instance.Value;
        private readonly Dictionary<string, SecurityToken> _tokens = new();
        private readonly Dictionary<string, SecurityPolicy> _policies = new();
        private readonly List<SecurityAuditEntry> _auditLog = new();
        private readonly object _lock = new();

        public event EventHandler<SecurityAuditEntry>? SecurityEvent;

        private SecurityManager()
        {
            RegisterDefaultPolicies();
        }

        private void RegisterDefaultPolicies()
        {
            RegisterPolicy(new SecurityPolicy
            {
                Name = "ExecuteSequence",
                Resource = "sequences/*",
                RequiredPermissions = new List<AccessType> { AccessType.Execute },
                AllowedRoles = new List<string> { "Operator", "Technician", "Developer", "Administrator" }
            });

            RegisterPolicy(new SecurityPolicy
            {
                Name = "EditSequence",
                Resource = "sequences/*",
                RequiredPermissions = new List<AccessType> { AccessType.Write },
                AllowedRoles = new List<string> { "Developer", "Administrator" }
            });

            RegisterPolicy(new SecurityPolicy
            {
                Name = "AdminAccess",
                Resource = "admin/*",
                RequiredPermissions = new List<AccessType> { AccessType.Admin },
                AllowedRoles = new List<string> { "Administrator" },
                RequiresMFA = true
            });
        }

        public SecurityToken CreateToken(string userId, string username, List<string> roles)
        {
            var permissions = DerivePermissionsFromRoles(roles);
            var token = new SecurityToken
            {
                UserId = userId,
                Username = username,
                Roles = roles,
                Permissions = permissions
            };

            lock (_lock) { _tokens[token.Id] = token; }
            return token;
        }

        private static List<AccessType> DerivePermissionsFromRoles(List<string> roles)
        {
            var permissions = new HashSet<AccessType>();
            foreach (var role in roles)
            {
                switch (role)
                {
                    case "Operator":
                        permissions.Add(AccessType.Read);
                        permissions.Add(AccessType.Execute);
                        break;
                    case "Technician":
                        permissions.Add(AccessType.Read);
                        permissions.Add(AccessType.Write);
                        permissions.Add(AccessType.Execute);
                        break;
                    case "Developer":
                        permissions.Add(AccessType.Read);
                        permissions.Add(AccessType.Write);
                        permissions.Add(AccessType.Execute);
                        permissions.Add(AccessType.Delete);
                        break;
                    case "Administrator":
                        permissions.Add(AccessType.Read);
                        permissions.Add(AccessType.Write);
                        permissions.Add(AccessType.Execute);
                        permissions.Add(AccessType.Delete);
                        permissions.Add(AccessType.Admin);
                        break;
                }
            }
            return permissions.ToList();
        }

        public bool ValidateToken(string tokenId)
        {
            lock (_lock)
            {
                return _tokens.TryGetValue(tokenId, out var token) && token.IsValid;
            }
        }

        public void RegisterPolicy(SecurityPolicy policy)
        {
            lock (_lock) { _policies[policy.Id] = policy; }
        }

        public bool CheckAccess(string tokenId, string resource, AccessType accessType)
        {
            lock (_lock)
            {
                if (!_tokens.TryGetValue(tokenId, out var token) || !token.IsValid)
                {
                    LogAudit(string.Empty, $"{accessType} on {resource}", resource, false, "Invalid token");
                    return false;
                }

                var applicablePolicy = _policies.Values.FirstOrDefault(p => 
                    resource.StartsWith(p.Resource.Replace("*", "")));

                bool allowed = token.Permissions.Contains(accessType);
                if (applicablePolicy != null)
                {
                    allowed = allowed && 
                              applicablePolicy.RequiredPermissions.All(rp => token.Permissions.Contains(rp)) &&
                              applicablePolicy.AllowedRoles.Any(r => token.Roles.Contains(r));
                }

                LogAudit(token.UserId, $"{accessType} on {resource}", resource, allowed, allowed ? "Granted" : "Denied");
                SecurityEvent?.Invoke(this, _auditLog.Last());
                return allowed;
            }
        }

        private void LogAudit(string userId, string action, string resource, bool allowed, string reason)
        {
            _auditLog.Add(new SecurityAuditEntry
            {
                UserId = userId,
                Action = action,
                Resource = resource,
                Allowed = allowed,
                Reason = reason
            });
        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public List<SecurityAuditEntry> GetAuditLog(int count = 100)
        {
            lock (_lock) { return _auditLog.TakeLast(count).ToList(); }
        }

        public void RevokeToken(string tokenId)
        {
            lock (_lock) { _tokens.Remove(tokenId); }
        }
    }
}
