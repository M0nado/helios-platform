using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Server
{
    /// <summary>
    /// Zero-trust security implementation interface with continuous verification and access validation.
    /// </summary>
    public interface IZeroTrustImplementation
    {
        /// <summary>
        /// Verifies a request with zero-trust principles. No implicit trust is granted.
        /// </summary>
        Task<SecurityVerificationResult> VerifyRequestAsync(SecurityContext context);

        /// <summary>
        /// Performs continuous authentication check.
        /// </summary>
        Task<bool> ContinuousAuthenticationAsync(string principalId, string? token = null);

        /// <summary>
        /// Validates access to a resource with proper authorization.
        /// </summary>
        Task<bool> ValidateResourceAccessAsync(string principalId, string resourceId, string action);

        /// <summary>
        /// Registers a security policy.
        /// </summary>
        Task<bool> RegisterPolicyAsync(SecurityPolicy policy);

        /// <summary>
        /// Gets a security policy by ID.
        /// </summary>
        Task<SecurityPolicy?> GetPolicyAsync(string policyId);

        /// <summary>
        /// Tracks a security violation event.
        /// </summary>
        Task<bool> RecordViolationAsync(SecurityViolation violation);

        /// <summary>
        /// Gets recent security violations.
        /// </summary>
        Task<List<SecurityViolation>> GetRecentViolationsAsync(int count = 100);

        /// <summary>
        /// Enforces multi-factor authentication for a principal.
        /// </summary>
        Task<bool> EnforceMfaAsync(string principalId);

        /// <summary>
        /// Validates certificate or credential.
        /// </summary>
        Task<bool> ValidateCredentialAsync(string principalId, string credentialType, string credentialValue);

        /// <summary>
        /// Gets security metrics and statistics.
        /// </summary>
        Task<ZeroTrustMetrics> GetMetricsAsync();
    }

    /// <summary>
    /// Security context for verification.
    /// </summary>
    public class SecurityContext
    {
        public string PrincipalId { get; set; }
        public string Action { get; set; }
        public string ResourceId { get; set; }
        public Dictionary<string, object> ContextData { get; set; } = new();
        public DateTime RequestTime { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Token { get; set; }
    }

    /// <summary>
    /// Result of security verification.
    /// </summary>
    public class SecurityVerificationResult
    {
        public bool IsVerified { get; set; }
        public string VerificationId { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime VerifiedAt { get; set; }
        public List<string> AppliedPolicies { get; set; } = new();
        public double TrustScore { get; set; } // 0-100
        public bool RequiresMfa { get; set; }
    }

    /// <summary>
    /// Security policy for access control.
    /// </summary>
    public class SecurityPolicy
    {
        public string PolicyId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Effect { get; set; } // "Allow" or "Deny"
        public List<string> Principals { get; set; } = new();
        public List<string> Resources { get; set; } = new();
        public List<string> Actions { get; set; } = new();
        public List<PolicyCondition> Conditions { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Condition in a security policy.
    /// </summary>
    public class PolicyCondition
    {
        public string ConditionType { get; set; } // "IpAddress", "TimeRange", "DeviceId", etc.
        public string Operator { get; set; } // "Equals", "Contains", "In", "Between", etc.
        public List<string> Values { get; set; } = new();
    }

    /// <summary>
    /// Security violation event.
    /// </summary>
    public class SecurityViolation
    {
        public string ViolationId { get; set; }
        public string PrincipalId { get; set; }
        public string ViolationType { get; set; } // "UnauthorizedAccess", "FailedAuth", "PolicyViolation", etc.
        public string ResourceId { get; set; }
        public string? Description { get; set; }
        public DateTime OccurredAt { get; set; }
        public string? IpAddress { get; set; }
        public int Severity { get; set; } // 1-5
        public bool AutomaticallyBlocked { get; set; }
    }

    /// <summary>
    /// Zero-trust security metrics.
    /// </summary>
    public class ZeroTrustMetrics
    {
        public long TotalVerificationAttempts { get; set; }
        public long SuccessfulVerifications { get; set; }
        public long FailedVerifications { get; set; }
        public long TotalViolations { get; set; }
        public long CriticalViolations { get; set; }
        public double AverageVerificationTimeMs { get; set; }
        public List<string> MostViolatedResources { get; set; } = new();
        public List<string> MostViolatedPrincipals { get; set; } = new();
        public DateTime CollectedAt { get; set; }
    }
}
