using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Server
{
    /// <summary>
    /// Zero-trust security implementation with continuous authentication and verification.
    /// </summary>
    public class ZeroTrustImplementation : IZeroTrustImplementation
    {
        private readonly Dictionary<string, SecurityPolicy> _policies = new();
        private readonly List<SecurityViolation> _violations = new();
        private readonly Dictionary<string, AuthenticationState> _authStates = new();
        private readonly object _lock = new();
        private long _totalVerifications;
        private long _successfulVerifications;
        private long _failedVerifications;
        private long _totalVerificationTimeMs;

        private class AuthenticationState
        {
            public string PrincipalId { get; set; }
            public DateTime LastAuthentication { get; set; }
            public bool MfaRequired { get; set; }
            public bool MfaCompleted { get; set; }
            public int ConsecutiveFailures { get; set; }
            public bool IsBlocked { get; set; }
            public DateTime? BlockedUntil { get; set; }
        }

        public ZeroTrustImplementation()
        {
            InitializeDefaultPolicies();
        }

        public Task<SecurityVerificationResult> VerifyRequestAsync(SecurityContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(context.PrincipalId))
                throw new ArgumentException("Principal ID cannot be empty", nameof(context));

            var sw = Stopwatch.StartNew();
            lock (_lock)
            {
                _totalVerifications++;

                // Check if principal is blocked
                if (_authStates.TryGetValue(context.PrincipalId, out var authState))
                {
                    if (authState.IsBlocked && authState.BlockedUntil > DateTime.UtcNow)
                    {
                        _failedVerifications++;
                        sw.Stop();
                        _totalVerificationTimeMs += sw.ElapsedMilliseconds;

                        return Task.FromResult(new SecurityVerificationResult
                        {
                            IsVerified = false,
                            VerificationId = Guid.NewGuid().ToString(),
                            RejectionReason = "Principal is temporarily blocked due to security violations",
                            VerifiedAt = DateTime.UtcNow,
                            TrustScore = 0
                        });
                    }
                }

                // Evaluate all policies
                var applicablePolicies = new List<string>();
                bool allowed = false;

                foreach (var policy in _policies.Values)
                {
                    if (!policy.IsActive)
                        continue;

                    // Check if policy applies
                    if (!PolicyApplies(policy, context))
                        continue;

                    applicablePolicies.Add(policy.PolicyId);

                    if (policy.Effect == "Allow")
                        allowed = true;
                    else if (policy.Effect == "Deny")
                    {
                        allowed = false;
                        break; // Deny takes precedence
                    }
                }

                // Calculate trust score based on verification factors
                double trustScore = 100.0;
                if (!string.IsNullOrEmpty(context.Token))
                    trustScore -= 10; // Token-based reduces inherent trust slightly
                if (context.RequestTime < DateTime.UtcNow.AddHours(-24))
                    trustScore -= 15; // Old requests are less trusted

                // Require MFA if multiple factors missing
                bool requiresMfa = authState?.MfaRequired ?? false;

                if (allowed && authState != null)
                {
                    authState.LastAuthentication = DateTime.UtcNow;
                    authState.ConsecutiveFailures = 0;
                    _successfulVerifications++;
                }
                else
                {
                    if (authState != null)
                        authState.ConsecutiveFailures++;

                    // Block after 5 consecutive failures
                    if (authState != null && authState.ConsecutiveFailures >= 5)
                    {
                        authState.IsBlocked = true;
                        authState.BlockedUntil = DateTime.UtcNow.AddMinutes(15);

                        RecordViolation(context, "ExcessiveFailedAttempts", 5);
                    }

                    _failedVerifications++;
                }

                sw.Stop();
                _totalVerificationTimeMs += sw.ElapsedMilliseconds;

                return Task.FromResult(new SecurityVerificationResult
                {
                    IsVerified = allowed,
                    VerificationId = Guid.NewGuid().ToString(),
                    RejectionReason = allowed ? null : "Request does not match any allow policy or matches deny policy",
                    VerifiedAt = DateTime.UtcNow,
                    AppliedPolicies = applicablePolicies,
                    TrustScore = trustScore,
                    RequiresMfa = requiresMfa
                });
            }
        }

        public Task<bool> ContinuousAuthenticationAsync(string principalId, string? token = null)
        {
            if (string.IsNullOrWhiteSpace(principalId))
                throw new ArgumentException("Principal ID cannot be empty", nameof(principalId));

            lock (_lock)
            {
                if (!_authStates.TryGetValue(principalId, out var authState))
                {
                    authState = new AuthenticationState
                    {
                        PrincipalId = principalId,
                        LastAuthentication = DateTime.UtcNow,
                        MfaRequired = false,
                        MfaCompleted = false,
                        ConsecutiveFailures = 0,
                        IsBlocked = false
                    };
                    _authStates[principalId] = authState;
                    return Task.FromResult(true);
                }

                // Check if authentication is still valid (less than 1 hour old)
                var timeSinceAuth = DateTime.UtcNow - authState.LastAuthentication;
                if (timeSinceAuth.TotalMinutes > 60)
                {
                    // Re-authentication required
                    return Task.FromResult(false);
                }

                // Token validation if provided
                if (!string.IsNullOrEmpty(token))
                {
                    if (!ValidateToken(token))
                        return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }
        }

        public Task<bool> ValidateResourceAccessAsync(string principalId, string resourceId, string action)
        {
            if (string.IsNullOrWhiteSpace(principalId))
                throw new ArgumentException("Principal ID cannot be empty", nameof(principalId));
            if (string.IsNullOrWhiteSpace(resourceId))
                throw new ArgumentException("Resource ID cannot be empty", nameof(resourceId));
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Action cannot be empty", nameof(action));

            lock (_lock)
            {
                var context = new SecurityContext
                {
                    PrincipalId = principalId,
                    ResourceId = resourceId,
                    Action = action,
                    RequestTime = DateTime.UtcNow
                };

                var result = VerifyRequestAsync(context).GetAwaiter().GetResult();
                return Task.FromResult(result.IsVerified);
            }
        }

        public Task<bool> RegisterPolicyAsync(SecurityPolicy policy)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            if (string.IsNullOrWhiteSpace(policy.PolicyId))
                throw new ArgumentException("Policy ID cannot be empty", nameof(policy));

            lock (_lock)
            {
                _policies[policy.PolicyId] = policy;
                return Task.FromResult(true);
            }
        }

        public Task<SecurityPolicy?> GetPolicyAsync(string policyId)
        {
            if (string.IsNullOrWhiteSpace(policyId))
                throw new ArgumentException("Policy ID cannot be empty", nameof(policyId));

            lock (_lock)
            {
                _policies.TryGetValue(policyId, out var policy);
                return Task.FromResult(policy);
            }
        }

        public Task<bool> RecordViolationAsync(SecurityViolation violation)
        {
            if (violation == null)
                throw new ArgumentNullException(nameof(violation));

            lock (_lock)
            {
                violation.ViolationId = Guid.NewGuid().ToString();
                violation.OccurredAt = DateTime.UtcNow;
                _violations.Add(violation);

                // Auto-block for critical violations
                if (violation.Severity >= 4)
                {
                    if (_authStates.TryGetValue(violation.PrincipalId, out var authState))
                    {
                        authState.IsBlocked = true;
                        authState.BlockedUntil = DateTime.UtcNow.AddMinutes(30);
                        violation.AutomaticallyBlocked = true;
                    }
                }

                return Task.FromResult(true);
            }
        }

        public Task<List<SecurityViolation>> GetRecentViolationsAsync(int count = 100)
        {
            lock (_lock)
            {
                return Task.FromResult(_violations.OrderByDescending(v => v.OccurredAt).Take(count).ToList());
            }
        }

        public Task<bool> EnforceMfaAsync(string principalId)
        {
            if (string.IsNullOrWhiteSpace(principalId))
                throw new ArgumentException("Principal ID cannot be empty", nameof(principalId));

            lock (_lock)
            {
                if (!_authStates.TryGetValue(principalId, out var authState))
                {
                    authState = new AuthenticationState
                    {
                        PrincipalId = principalId,
                        MfaRequired = true,
                        MfaCompleted = false
                    };
                    _authStates[principalId] = authState;
                }
                else
                {
                    authState.MfaRequired = true;
                }

                return Task.FromResult(true);
            }
        }

        public Task<bool> ValidateCredentialAsync(string principalId, string credentialType, string credentialValue)
        {
            if (string.IsNullOrWhiteSpace(principalId))
                throw new ArgumentException("Principal ID cannot be empty", nameof(principalId));
            if (string.IsNullOrWhiteSpace(credentialType))
                throw new ArgumentException("Credential type cannot be empty", nameof(credentialType));
            if (string.IsNullOrWhiteSpace(credentialValue))
                throw new ArgumentException("Credential value cannot be empty", nameof(credentialValue));

            lock (_lock)
            {
                // Validate based on credential type
                bool isValid = false;

                switch (credentialType.ToLower())
                {
                    case "password":
                        isValid = ValidatePassword(credentialValue);
                        break;
                    case "certificate":
                        isValid = ValidateCertificate(credentialValue);
                        break;
                    case "mfa":
                        isValid = ValidateMfaCode(credentialValue);
                        break;
                    case "apikey":
                        isValid = ValidateApiKey(credentialValue);
                        break;
                    default:
                        isValid = false;
                        break;
                }

                if (isValid && _authStates.TryGetValue(principalId, out var authState))
                {
                    if (credentialType.ToLower() == "mfa")
                        authState.MfaCompleted = true;
                }

                return Task.FromResult(isValid);
            }
        }

        public Task<ZeroTrustMetrics> GetMetricsAsync()
        {
            lock (_lock)
            {
                var criticalViolations = _violations.Count(v => v.Severity >= 4);
                var violationsByResource = _violations
                    .GroupBy(v => v.ResourceId)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToList();

                var violationsByPrincipal = _violations
                    .GroupBy(v => v.PrincipalId)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToList();

                return Task.FromResult(new ZeroTrustMetrics
                {
                    TotalVerificationAttempts = _totalVerifications,
                    SuccessfulVerifications = _successfulVerifications,
                    FailedVerifications = _failedVerifications,
                    TotalViolations = _violations.Count,
                    CriticalViolations = criticalViolations,
                    AverageVerificationTimeMs = _totalVerifications > 0 ? (double)_totalVerificationTimeMs / _totalVerifications : 0,
                    MostViolatedResources = violationsByResource,
                    MostViolatedPrincipals = violationsByPrincipal,
                    CollectedAt = DateTime.UtcNow
                });
            }
        }

        private bool PolicyApplies(SecurityPolicy policy, SecurityContext context)
        {
            // Check principals
            if (policy.Principals.Count > 0 && !policy.Principals.Contains(context.PrincipalId) && !policy.Principals.Contains("*"))
                return false;

            // Check resources
            if (policy.Resources.Count > 0 && !policy.Resources.Contains(context.ResourceId) && !policy.Resources.Contains("*"))
                return false;

            // Check actions
            if (policy.Actions.Count > 0 && !policy.Actions.Contains(context.Action) && !policy.Actions.Contains("*"))
                return false;

            // Check conditions
            if (policy.Conditions.Count > 0)
            {
                foreach (var condition in policy.Conditions)
                {
                    if (!EvaluateCondition(condition, context))
                        return false;
                }
            }

            return true;
        }

        private bool EvaluateCondition(PolicyCondition condition, SecurityContext context)
        {
            switch (condition.ConditionType.ToLower())
            {
                case "ipaddress":
                    if (string.IsNullOrEmpty(context.IpAddress))
                        return false;
                    return condition.Operator.ToLower() switch
                    {
                        "equals" => condition.Values.Contains(context.IpAddress),
                        "contains" => condition.Values.Any(v => context.IpAddress.Contains(v)),
                        "in" => condition.Values.Contains(context.IpAddress),
                        _ => false
                    };
                case "timerange":
                    var hour = DateTime.UtcNow.Hour;
                    return condition.Operator.ToLower() == "between" && condition.Values.Count >= 2 &&
                           int.TryParse(condition.Values[0], out var startHour) &&
                           int.TryParse(condition.Values[1], out var endHour) &&
                           hour >= startHour && hour < endHour;
                default:
                    return true;
            }
        }

        private void RecordViolation(SecurityContext context, string type, int severity)
        {
            _violations.Add(new SecurityViolation
            {
                ViolationId = Guid.NewGuid().ToString(),
                PrincipalId = context.PrincipalId,
                ViolationType = type,
                ResourceId = context.ResourceId,
                OccurredAt = DateTime.UtcNow,
                IpAddress = context.IpAddress,
                Severity = severity,
                AutomaticallyBlocked = false
            });
        }

        private bool ValidateToken(string token)
        {
            // Simple validation: Token should be 32+ characters
            return token.Length >= 32 && Regex.IsMatch(token, @"^[a-zA-Z0-9\-_\.]+$");
        }

        private bool ValidatePassword(string password)
        {
            // Password must be 8+ characters with mixed case and number
            return password.Length >= 8 &&
                   Regex.IsMatch(password, "[A-Z]") &&
                   Regex.IsMatch(password, "[a-z]") &&
                   Regex.IsMatch(password, "[0-9]");
        }

        private bool ValidateCertificate(string certificate)
        {
            // Simple validation: Certificate should contain BEGIN and END markers
            return certificate.Contains("BEGIN") && certificate.Contains("END");
        }

        private bool ValidateMfaCode(string code)
        {
            // MFA code should be 6 digits
            return code.Length == 6 && Regex.IsMatch(code, @"^[0-9]{6}$");
        }

        private bool ValidateApiKey(string apiKey)
        {
            // API key should be at least 32 characters
            return apiKey.Length >= 32;
        }

        private void InitializeDefaultPolicies()
        {
            lock (_lock)
            {
                // Default allow policy for authenticated admins
                _policies["default-admin"] = new SecurityPolicy
                {
                    PolicyId = "default-admin",
                    Name = "Default Admin Policy",
                    Effect = "Allow",
                    Principals = new List<string> { "admin" },
                    Resources = new List<string> { "*" },
                    Actions = new List<string> { "*" },
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Default deny all policy
                _policies["default-deny"] = new SecurityPolicy
                {
                    PolicyId = "default-deny",
                    Name = "Default Deny Policy",
                    Effect = "Deny",
                    Principals = new List<string> { "*" },
                    Resources = new List<string> { "*" },
                    Actions = new List<string> { "*" },
                    IsActive = false, // Disabled by default
                    CreatedAt = DateTime.UtcNow
                };
            }
        }
    }
}
