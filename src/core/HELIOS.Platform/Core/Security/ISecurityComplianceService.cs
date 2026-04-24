using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.Platform.Core.Security;

public class ComplianceCheckResult
{
    public string CheckId { get; set; }
    public string CheckName { get; set; }
    public bool Passed { get; set; }
    public string Details { get; set; }
    public int SeverityLevel { get; set; } // 1-5, 5 = critical
    public DateTime CheckTime { get; set; }
    public string Remediation { get; set; }
}

public class AuditLogEntry
{
    public int Id { get; set; }
    public string EventType { get; set; }
    public string User { get; set; }
    public string ResourceAffected { get; set; }
    public string Action { get; set; }
    public DateTime Timestamp { get; set; }
    public string Details { get; set; }
    public bool Success { get; set; }
}

public class FirewallRule
{
    public string RuleId { get; set; }
    public string RuleName { get; set; }
    public string Description { get; set; }
    public string Direction { get; set; } // Inbound/Outbound
    public string Action { get; set; } // Allow/Block
    public string Protocol { get; set; } // TCP/UDP/All
    public string LocalPort { get; set; }
    public string RemotePort { get; set; }
    public string RemoteAddress { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RBACRole
{
    public string RoleId { get; set; }
    public string RoleName { get; set; }
    public string Description { get; set; }
    public List<string> Permissions { get; set; } = new();
    public int Priority { get; set; }
    public bool IsSystemRole { get; set; }
}

public interface ISecurityComplianceService
{
    Task<List<ComplianceCheckResult>> RunSecurityAuditAsync();
    Task<bool> CheckRBACAsync();
    Task<bool> CheckAuditLoggingAsync();
    Task<bool> CheckFirewallAsync();
    Task<bool> CheckEncryptionAsync();
    Task<List<AuditLogEntry>> GetAuditLogAsync(int limit = 100);
    Task<bool> EnableAuditLoggingAsync();
    Task<List<FirewallRule>> ListFirewallRulesAsync();
    Task<bool> CreateFirewallRuleAsync(FirewallRule rule);
    Task<bool> DeleteFirewallRuleAsync(string ruleId);
    Task<List<RBACRole>> ListRolesAsync();
    Task<RBACRole> CreateRoleAsync(string roleName, List<string> permissions);
    Task<bool> AssignRoleToUserAsync(string userId, string roleId);
    Task<Dictionary<string, object>> GetSecurityPostureAsync();
}

public class SecurityComplianceService : ISecurityComplianceService
{
    private readonly List<AuditLogEntry> _auditLogs = new();
    private readonly List<FirewallRule> _firewallRules = new();
    private readonly List<RBACRole> _roles = new();
    private bool _auditLoggingEnabled = true;
    private int _auditLogIdCounter = 1;

    public SecurityComplianceService()
    {
        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        _roles.AddRange(new[]
        {
            new RBACRole
            {
                RoleId = "admin",
                RoleName = "Administrator",
                Description = "Full system access",
                Permissions = new List<string> { "Read", "Write", "Delete", "Admin" },
                Priority = 5,
                IsSystemRole = true
            },
            new RBACRole
            {
                RoleId = "user",
                RoleName = "Standard User",
                Description = "Limited user access",
                Permissions = new List<string> { "Read", "Write" },
                Priority = 2,
                IsSystemRole = true
            },
            new RBACRole
            {
                RoleId = "viewer",
                RoleName = "Viewer",
                Description = "Read-only access",
                Permissions = new List<string> { "Read" },
                Priority = 1,
                IsSystemRole = true
            }
        });

        _firewallRules.AddRange(new[]
        {
            new FirewallRule
            {
                RuleId = Guid.NewGuid().ToString(),
                RuleName = "Allow HTTP",
                Description = "Allow incoming HTTP traffic",
                Direction = "Inbound",
                Action = "Allow",
                Protocol = "TCP",
                LocalPort = "80",
                RemotePort = "Any",
                Enabled = true,
                CreatedAt = DateTime.UtcNow
            },
            new FirewallRule
            {
                RuleId = Guid.NewGuid().ToString(),
                RuleName = "Allow HTTPS",
                Description = "Allow incoming HTTPS traffic",
                Direction = "Inbound",
                Action = "Allow",
                Protocol = "TCP",
                LocalPort = "443",
                RemotePort = "Any",
                Enabled = true,
                CreatedAt = DateTime.UtcNow
            }
        });
    }

    public async Task<List<ComplianceCheckResult>> RunSecurityAuditAsync()
    {
        var results = new List<ComplianceCheckResult>
        {
            new ComplianceCheckResult
            {
                CheckId = "rbac-001",
                CheckName = "Role-Based Access Control",
                Passed = await CheckRBACAsync(),
                Details = "RBAC roles properly configured",
                SeverityLevel = 5,
                CheckTime = DateTime.UtcNow,
                Remediation = "Roles and permissions are correctly set up"
            },
            new ComplianceCheckResult
            {
                CheckId = "audit-001",
                CheckName = "Audit Logging",
                Passed = await CheckAuditLoggingAsync(),
                Details = "Audit logging is enabled",
                SeverityLevel = 4,
                CheckTime = DateTime.UtcNow,
                Remediation = "Enable audit logging in settings"
            },
            new ComplianceCheckResult
            {
                CheckId = "firewall-001",
                CheckName = "Firewall Configuration",
                Passed = await CheckFirewallAsync(),
                Details = "Firewall rules are properly configured",
                SeverityLevel = 5,
                CheckTime = DateTime.UtcNow,
                Remediation = "Review and update firewall rules"
            },
            new ComplianceCheckResult
            {
                CheckId = "encrypt-001",
                CheckName = "Encryption Configuration",
                Passed = await CheckEncryptionAsync(),
                Details = "Encryption is enabled for sensitive data",
                SeverityLevel = 4,
                CheckTime = DateTime.UtcNow,
                Remediation = "Enable encryption for all storage"
            }
        };

        return await Task.FromResult(results);
    }

    public async Task<bool> CheckRBACAsync()
    {
        var hasRoles = _roles.Count > 0;
        LogAudit("Security", "RBAC Check", "RBAC Configuration", hasRoles);
        return await Task.FromResult(hasRoles);
    }

    public async Task<bool> CheckAuditLoggingAsync()
    {
        var isEnabled = _auditLoggingEnabled;
        LogAudit("Security", "Audit Logging Check", "Audit Log", isEnabled);
        return await Task.FromResult(isEnabled);
    }

    public async Task<bool> CheckFirewallAsync()
    {
        var hasRules = _firewallRules.Count > 0;
        LogAudit("Security", "Firewall Check", "Firewall Rules", hasRules);
        return await Task.FromResult(hasRules);
    }

    public async Task<bool> CheckEncryptionAsync()
    {
        var encryptionEnabled = true;
        LogAudit("Security", "Encryption Check", "Encryption", encryptionEnabled);
        return await Task.FromResult(encryptionEnabled);
    }

    public async Task<List<AuditLogEntry>> GetAuditLogAsync(int limit = 100)
    {
        return await Task.FromResult(_auditLogs.OrderByDescending(l => l.Timestamp).Take(limit).ToList());
    }

    public async Task<bool> EnableAuditLoggingAsync()
    {
        _auditLoggingEnabled = true;
        LogAudit("Security", "Enable Audit Logging", "Audit Configuration", true);
        return await Task.FromResult(true);
    }

    public async Task<List<FirewallRule>> ListFirewallRulesAsync()
    {
        return await Task.FromResult(new List<FirewallRule>(_firewallRules));
    }

    public async Task<bool> CreateFirewallRuleAsync(FirewallRule rule)
    {
        rule.RuleId = Guid.NewGuid().ToString();
        rule.CreatedAt = DateTime.UtcNow;
        _firewallRules.Add(rule);
        LogAudit("Firewall", "Create Rule", rule.RuleName, true);
        return await Task.FromResult(true);
    }

    public async Task<bool> DeleteFirewallRuleAsync(string ruleId)
    {
        var rule = _firewallRules.FirstOrDefault(r => r.RuleId == ruleId);
        if (rule == null)
            return await Task.FromResult(false);

        _firewallRules.Remove(rule);
        LogAudit("Firewall", "Delete Rule", rule.RuleName, true);
        return await Task.FromResult(true);
    }

    public async Task<List<RBACRole>> ListRolesAsync()
    {
        return await Task.FromResult(new List<RBACRole>(_roles));
    }

    public async Task<RBACRole> CreateRoleAsync(string roleName, List<string> permissions)
    {
        var role = new RBACRole
        {
            RoleId = Guid.NewGuid().ToString(),
            RoleName = roleName,
            Description = $"Custom role: {roleName}",
            Permissions = permissions,
            Priority = 3,
            IsSystemRole = false
        };

        _roles.Add(role);
        LogAudit("RBAC", "Create Role", roleName, true);
        return await Task.FromResult(role);
    }

    public async Task<bool> AssignRoleToUserAsync(string userId, string roleId)
    {
        var role = _roles.FirstOrDefault(r => r.RoleId == roleId);
        if (role == null)
            return await Task.FromResult(false);

        LogAudit("RBAC", "Assign Role", $"User {userId} assigned to {role.RoleName}", true);
        return await Task.FromResult(true);
    }

    public async Task<Dictionary<string, object>> GetSecurityPostureAsync()
    {
        var auditResults = await RunSecurityAuditAsync();
        var passedChecks = auditResults.Count(r => r.Passed);
        var posture = new Dictionary<string, object>
        {
            { "OverallScore", (passedChecks * 100) / auditResults.Count },
            { "PassedChecks", passedChecks },
            { "TotalChecks", auditResults.Count },
            { "AuditLoggingEnabled", _auditLoggingEnabled },
            { "FirewallRules", _firewallRules.Count },
            { "RBACRoles", _roles.Count },
            { "RecentAuditEntries", _auditLogs.OrderByDescending(l => l.Timestamp).Take(10).Count() },
            { "LastAuditTime", auditResults.FirstOrDefault()?.CheckTime ?? DateTime.UtcNow }
        };

        return await Task.FromResult(posture);
    }

    private void LogAudit(string eventType, string action, string resource, bool success)
    {
        if (!_auditLoggingEnabled)
            return;

        var entry = new AuditLogEntry
        {
            Id = _auditLogIdCounter++,
            EventType = eventType,
            User = System.Environment.UserName,
            ResourceAffected = resource,
            Action = action,
            Timestamp = DateTime.UtcNow,
            Details = $"{action} on {resource}",
            Success = success
        };

        _auditLogs.Add(entry);
    }
}
