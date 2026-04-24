using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Users.Interfaces
{
    /// <summary>
    /// Integration interface for all user account management services.
    /// </summary>
    public interface IUserAccountManagementService
    {
        /// <summary>
        /// Initializes all user account management services.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Creates all required system accounts (Admin, Primary User, Guest, Service).
        /// </summary>
        Task<bool> SetupSystemAccountsAsync();

        /// <summary>
        /// Provisions a new user account.
        /// </summary>
        Task<bool> ProvisionUserAsync(string username, string fullName, string accountType, string password);

        /// <summary>
        /// Switches to a different user profile.
        /// </summary>
        Task<bool> SwitchUserProfileAsync(string username);

        /// <summary>
        /// Gets current user security configuration.
        /// </summary>
        Task<SecurityConfiguration> GetUserSecurityConfigAsync(string username);

        /// <summary>
        /// Applies security policies to user account.
        /// </summary>
        Task<bool> ApplySecurityPoliciesAsync(string username);

        /// <summary>
        /// Gets user activity report.
        /// </summary>
        Task<UserActivityReport> GetActivityReportAsync(string username, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Validates account integrity and permissions.
        /// </summary>
        Task<AccountValidationResult> ValidateAccountAsync(string username);

        /// <summary>
        /// Removes/disables a user account.
        /// </summary>
        Task<bool> RemoveAccountAsync(string username);

        /// <summary>
        /// Gets system status and configuration.
        /// </summary>
        Task<SystemStatus> GetSystemStatusAsync();
    }

    /// <summary>
    /// Security configuration for user account.
    /// </summary>
    public class SecurityConfiguration
    {
        public string Username { get; set; }
        public bool PasswordRequired { get; set; }
        public int PasswordMinLength { get; set; }
        public int PasswordHistoryCount { get; set; }
        public int LockoutThreshold { get; set; }
        public int LockoutDurationMinutes { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool AuditLoggingEnabled { get; set; }
        public List<string> GroupMemberships { get; set; } = new();
    }

    /// <summary>
    /// User activity report.
    /// </summary>
    public class UserActivityReport
    {
        public string Username { get; set; }
        public DateTime ReportDate { get; set; }
        public int TotalLoginAttempts { get; set; }
        public int SuccessfulLogins { get; set; }
        public int FailedLoginAttempts { get; set; }
        public List<ActivityEvent> Activities { get; set; } = new();
        public int PrivilegeEscalationCount { get; set; }
        public string RiskLevel { get; set; }
    }

    /// <summary>
    /// Individual activity event.
    /// </summary>
    public class ActivityEvent
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; }
        public string Description { get; set; }
        public bool IsAnomalous { get; set; }
    }

    /// <summary>
    /// Account validation result.
    /// </summary>
    public class AccountValidationResult
    {
        public string Username { get; set; }
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public PermissionStatus PermissionStatus { get; set; }
    }

    /// <summary>
    /// Permission status for account.
    /// </summary>
    public class PermissionStatus
    {
        public bool IsAdministrator { get; set; }
        public bool CanModifySystem { get; set; }
        public bool CanAccessVault { get; set; }
        public bool CanAccessSandbox { get; set; }
        public bool CanAccessQuarantine { get; set; }
        public bool CanModifyOtherUsers { get; set; }
    }

    /// <summary>
    /// System status information.
    /// </summary>
    public class SystemStatus
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public List<string> ConfiguredAccounts { get; set; } = new();
        public bool IsFullyConfigured { get; set; }
        public List<string> PendingTasks { get; set; } = new();
    }
}
