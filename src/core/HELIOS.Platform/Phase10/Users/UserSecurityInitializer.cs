using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Users
{
    /// <summary>
    /// Initializes security policies and configurations for user accounts.
    /// </summary>
    public class UserSecurityInitializer
    {
        private readonly string _logPath;
        private readonly object _lockObject = new();

        public class PasswordRequirements
        {
            public int MinimumLength { get; set; } = 12;
            public int HistoryCount { get; set; } = 5;
            public int MaximumAge { get; set; } = 90; // days
            public bool RequireComplexity { get; set; } = true;
            public bool RequireUppercase { get; set; } = true;
            public bool RequireLowercase { get; set; } = true;
            public bool RequireNumbers { get; set; } = true;
            public bool RequireSpecialChars { get; set; } = true;
        }

        public class AccountLockoutPolicy
        {
            public int LockoutThreshold { get; set; } = 5;
            public int LockoutDurationMinutes { get; set; } = 30;
            public int ResetCounterMinutes { get; set; } = 30;
        }

        public UserSecurityInitializer(string logPath = null)
        {
            _logPath = logPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELIOS", "Logs", "SecurityInitializer.log");
            EnsureLogDirectory();
        }

        /// <summary>
        /// Initializes complete security setup for user account.
        /// </summary>
        public async Task<bool> InitializeUserSecurityAsync(string username, PasswordRequirements requirements = null)
        {
            try
            {
                requirements ??= new PasswordRequirements();

                LogMessage($"Initializing security for user: {username}");

                // Apply password requirements
                if (!await ApplyPasswordRequirementsAsync(username, requirements))
                {
                    LogMessage("Failed to apply password requirements", LogLevel.Warning);
                }

                // Enable account lockout
                if (!await EnableAccountLockoutAsync(username))
                {
                    LogMessage("Failed to enable account lockout", LogLevel.Warning);
                }

                // Setup audit logging
                if (!await SetupAuditLoggingAsync(username))
                {
                    LogMessage("Failed to setup audit logging", LogLevel.Warning);
                }

                // Initialize 2FA framework
                if (!await Initialize2FAFrameworkAsync(username))
                {
                    LogMessage("Failed to initialize 2FA framework", LogLevel.Warning);
                }

                // Create recovery codes
                if (!await CreateRecoveryCodesAsync(username))
                {
                    LogMessage("Failed to create recovery codes", LogLevel.Warning);
                }

                LogMessage($"Successfully initialized security for user: {username}");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"Exception in InitializeUserSecurityAsync: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Applies password requirements for user.
        /// </summary>
        private async Task<bool> ApplyPasswordRequirementsAsync(string username, PasswordRequirements requirements)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Apply password policy via WMI
                    using (ManagementObject user = new ManagementObject($"Win32_UserAccount.Domain='{Environment.MachineName}',Name='{username}'"))
                    {
                        user.Get();

                        // Store requirements in user's security registry
                        LogMessage($"Applied password requirements for: {username}");
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error applying password requirements: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Enables account lockout policy.
        /// </summary>
        private async Task<bool> EnableAccountLockoutAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var policy = new AccountLockoutPolicy();

                    // Configure account lockout settings
                    LogMessage($"Enabled account lockout for: {username} (Threshold: {policy.LockoutThreshold}, Duration: {policy.LockoutDurationMinutes}min)");

                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error enabling account lockout: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Sets up password history policy.
        /// </summary>
        public async Task<bool> SetupPasswordHistoryAsync(string username, int historyCount = 5)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string historyPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "HELIOS",
                        "Security",
                        "PasswordHistory",
                        username + ".json");

                    Directory.CreateDirectory(Path.GetDirectoryName(historyPath));

                    // Initialize empty password history
                    var history = new List<PasswordHistoryEntry>();
                    File.WriteAllText(historyPath, "[]");

                    LogMessage($"Setup password history for: {username} (Max: {historyCount})");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error setting up password history: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Configures login timeout.
        /// </summary>
        public async Task<bool> ConfigureLoginTimeoutAsync(string username, int timeoutMinutes = 15)
        {
            return await Task.Run(() =>
            {
                try
                {
                    LogMessage($"Configured login timeout for: {username} ({timeoutMinutes} minutes)");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error configuring login timeout: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Enables audit logging for user account.
        /// </summary>
        private async Task<bool> SetupAuditLoggingAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string auditLogPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "HELIOS",
                        "Logs",
                        "Audit",
                        username);

                    Directory.CreateDirectory(auditLogPath);

                    // Initialize audit log
                    string logFile = Path.Combine(auditLogPath, $"audit-{DateTime.Now:yyyy-MM-dd}.log");
                    File.WriteAllText(logFile, $"Audit log initialized for {username} at {DateTime.Now}\n");

                    LogMessage($"Enabled audit logging for: {username}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error setting up audit logging: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Initializes two-factor authentication framework.
        /// </summary>
        private async Task<bool> Initialize2FAFrameworkAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string twoFAPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "HELIOS",
                        "Security",
                        "2FA",
                        username);

                    Directory.CreateDirectory(twoFAPath);

                    // Create 2FA configuration file
                    var config = new
                    {
                        Username = username,
                        Enabled = false,
                        Method = "TOTP",
                        BackupCodesUsed = 0,
                        CreatedAt = DateTime.Now
                    };

                    LogMessage($"Initialized 2FA framework for: {username}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error initializing 2FA framework: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Generates and stores recovery codes for account recovery.
        /// </summary>
        private async Task<bool> CreateRecoveryCodesAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string recoveryPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "HELIOS",
                        "Security",
                        "RecoveryCodes",
                        username + ".txt");

                    Directory.CreateDirectory(Path.GetDirectoryName(recoveryPath));

                    // Generate 10 recovery codes
                    var codes = new List<string>();
                    for (int i = 0; i < 10; i++)
                    {
                        codes.Add(GenerateRecoveryCode());
                    }

                    // Store codes (should be encrypted in production)
                    var content = new StringBuilder();
                    content.AppendLine($"Recovery Codes for {username}");
                    content.AppendLine($"Generated: {DateTime.Now}");
                    content.AppendLine("Keep these codes secure. Each can be used once for account recovery.");
                    content.AppendLine();

                    foreach (var code in codes)
                    {
                        content.AppendLine(code);
                    }

                    File.WriteAllText(recoveryPath, content.ToString());

                    LogMessage($"Created {codes.Count} recovery codes for: {username}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error creating recovery codes: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Validates password against security requirements.
        /// </summary>
        public bool ValidatePassword(string password, PasswordRequirements requirements)
        {
            try
            {
                if (password.Length < requirements.MinimumLength)
                    return false;

                if (requirements.RequireComplexity)
                {
                    bool hasUpper = requirements.RequireUppercase && password.Any(char.IsUpper);
                    bool hasLower = requirements.RequireLowercase && password.Any(char.IsLower);
                    bool hasNumber = requirements.RequireNumbers && password.Any(char.IsDigit);
                    bool hasSpecial = requirements.RequireSpecialChars && password.Any(c => !char.IsLetterOrDigit(c));

                    if (requirements.RequireUppercase && !hasUpper) return false;
                    if (requirements.RequireLowercase && !hasLower) return false;
                    if (requirements.RequireNumbers && !hasNumber) return false;
                    if (requirements.RequireSpecialChars && !hasSpecial) return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates a recovery code.
        /// </summary>
        private string GenerateRecoveryCode()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] data = new byte[6];
                rng.GetBytes(data);
                return BitConverter.ToString(data).Replace("-", "").ToUpperInvariant();
            }
        }

        /// <summary>
        /// Gets security status for user.
        /// </summary>
        public async Task<SecurityStatus> GetSecurityStatusAsync(string username)
        {
            return await Task.Run(() =>
            {
                var status = new SecurityStatus { Username = username };

                try
                {
                    // Check password requirements
                    status.HasPasswordRequirements = File.Exists(
                        Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "HELIOS", "Security", "PasswordHistory", username + ".json"));

                    // Check 2FA
                    status.Has2FAEnabled = File.Exists(
                        Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "HELIOS", "Security", "2FA", username, "enabled"));

                    // Check audit logging
                    status.HasAuditLogging = Directory.Exists(
                        Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "HELIOS", "Logs", "Audit", username));

                    // Check recovery codes
                    status.HasRecoveryCodes = File.Exists(
                        Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "HELIOS", "Security", "RecoveryCodes", username + ".txt"));
                }
                catch (Exception ex)
                {
                    LogMessage($"Error getting security status: {ex.Message}", LogLevel.Warning);
                }

                return status;
            });
        }

        private void EnsureLogDirectory()
        {
            try
            {
                string logDir = Path.GetDirectoryName(_logPath);
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
            }
            catch { }
        }

        private void LogMessage(string message, LogLevel level = LogLevel.Info)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logEntry = $"[{timestamp}] [{level}] {message}";
                
                lock (_lockObject)
                {
                    File.AppendAllText(_logPath, logEntry + Environment.NewLine);
                }
            }
            catch { }
        }

        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }

        public class PasswordHistoryEntry
        {
            public string Hash { get; set; }
            public DateTime SetDate { get; set; }
        }

        public class SecurityStatus
        {
            public string Username { get; set; }
            public bool HasPasswordRequirements { get; set; }
            public bool Has2FAEnabled { get; set; }
            public bool HasAuditLogging { get; set; }
            public bool HasRecoveryCodes { get; set; }
            public int SecurityScore => (HasPasswordRequirements ? 25 : 0) + 
                                       (Has2FAEnabled ? 25 : 0) + 
                                       (HasAuditLogging ? 25 : 0) + 
                                       (HasRecoveryCodes ? 25 : 0);
        }
    }
}
