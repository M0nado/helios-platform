using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Users
{
    /// <summary>
    /// Monitors and logs user account activity for audit and security purposes.
    /// </summary>
    public class AccountActivityMonitor
    {
        private readonly string _logPath;
        private readonly string _auditPath;
        private readonly object _lockObject = new();
        private Dictionary<string, UserActivityLog> _activityLogs = new();

        public class ActivityEntry
        {
            public DateTime Timestamp { get; set; }
            public string EventType { get; set; }
            public string Description { get; set; }
            public string IpAddress { get; set; }
            public bool IsAnomalous { get; set; }
            public string Severity { get; set; } // Low, Medium, High, Critical
        }

        public class UserActivityLog
        {
            public string Username { get; set; }
            public DateTime CreatedDate { get; set; }
            public List<ActivityEntry> Entries { get; set; } = new();
            public int FailedLoginAttempts { get; set; }
            public int SuccessfulLogins { get; set; }
            public DateTime LastLoginTime { get; set; }
            public int PrivilegeEscalationCount { get; set; }
        }

        public AccountActivityMonitor(string logPath = null, string auditPath = null)
        {
            _logPath = logPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELIOS", "Logs", "ActivityMonitor.log");
            _auditPath = auditPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELIOS", "Audit");
            EnsureDirectories();
            LoadExistingLogs();
        }

        /// <summary>
        /// Logs account creation event.
        /// </summary>
        public async Task<bool> LogAccountCreationAsync(string username, string fullName, string accountType)
        {
            return await Task.Run(() =>
            {
                try
                {
                    GetOrCreateActivityLog(username);

                    var entry = new ActivityEntry
                    {
                        Timestamp = DateTime.Now,
                        EventType = "AccountCreated",
                        Description = $"Account created: {fullName} ({accountType})",
                        Severity = "Medium"
                    };

                    _activityLogs[username].Entries.Add(entry);

                    LogMessage($"Account creation logged for: {username}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error logging account creation: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Logs login attempt.
        /// </summary>
        public async Task<bool> LogLoginAttemptAsync(string username, bool successful, string ipAddress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var log = GetOrCreateActivityLog(username);

                    if (successful)
                    {
                        log.SuccessfulLogins++;
                        log.LastLoginTime = DateTime.Now;
                        log.FailedLoginAttempts = 0; // Reset failed counter

                        var entry = new ActivityEntry
                        {
                            Timestamp = DateTime.Now,
                            EventType = "LoginSuccessful",
                            Description = $"Successful login",
                            IpAddress = ipAddress,
                            Severity = "Low"
                        };

                        log.Entries.Add(entry);
                        LogMessage($"Successful login for: {username}");
                    }
                    else
                    {
                        log.FailedLoginAttempts++;
                        bool isAnomalous = log.FailedLoginAttempts > 3;

                        var entry = new ActivityEntry
                        {
                            Timestamp = DateTime.Now,
                            EventType = "LoginFailed",
                            Description = $"Failed login attempt (Attempt #{log.FailedLoginAttempts})",
                            IpAddress = ipAddress,
                            IsAnomalous = isAnomalous,
                            Severity = isAnomalous ? "High" : "Medium"
                        };

                        log.Entries.Add(entry);
                        LogMessage($"Failed login attempt for: {username} (Attempt #{log.FailedLoginAttempts})", LogLevel.Warning);

                        if (isAnomalous)
                        {
                            LogMessage($"Anomalous login activity detected for: {username}", LogLevel.Warning);
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error logging login attempt: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Logs logout event.
        /// </summary>
        public async Task<bool> LogLogoutAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var log = GetOrCreateActivityLog(username);

                    var entry = new ActivityEntry
                    {
                        Timestamp = DateTime.Now,
                        EventType = "Logout",
                        Description = "User logged out",
                        Severity = "Low"
                    };

                    log.Entries.Add(entry);
                    LogMessage($"Logout logged for: {username}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error logging logout: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Logs privilege escalation event.
        /// </summary>
        public async Task<bool> LogPrivilegeEscalationAsync(string username, string action, string targetResource)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var log = GetOrCreateActivityLog(username);
                    log.PrivilegeEscalationCount++;

                    var entry = new ActivityEntry
                    {
                        Timestamp = DateTime.Now,
                        EventType = "PrivilegeEscalation",
                        Description = $"Privilege escalation: {action} on {targetResource}",
                        IsAnomalous = true,
                        Severity = "High"
                    };

                    log.Entries.Add(entry);
                    LogMessage($"Privilege escalation logged for: {username}", LogLevel.Warning);
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error logging privilege escalation: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Logs file access event (if enabled).
        /// </summary>
        public async Task<bool> LogFileAccessAsync(string username, string filePath, string accessType)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var log = GetOrCreateActivityLog(username);

                    var entry = new ActivityEntry
                    {
                        Timestamp = DateTime.Now,
                        EventType = "FileAccess",
                        Description = $"File {accessType}: {filePath}",
                        Severity = "Low"
                    };

                    log.Entries.Add(entry);
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error logging file access: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Logs policy change event.
        /// </summary>
        public async Task<bool> LogPolicyChangeAsync(string username, string policyName, string oldValue, string newValue)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var log = GetOrCreateActivityLog(username);

                    var entry = new ActivityEntry
                    {
                        Timestamp = DateTime.Now,
                        EventType = "PolicyChange",
                        Description = $"Policy {policyName} changed from '{oldValue}' to '{newValue}'",
                        Severity = "Medium"
                    };

                    log.Entries.Add(entry);
                    LogMessage($"Policy change logged for: {username}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error logging policy change: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Generates activity report for user.
        /// </summary>
        public async Task<ActivityReport> GenerateActivityReportAsync(string username, DateTime startDate, DateTime endDate)
        {
            return await Task.Run(() =>
            {
                var report = new ActivityReport
                {
                    Username = username,
                    ReportDate = DateTime.Now,
                    StartDate = startDate,
                    EndDate = endDate
                };

                try
                {
                    if (!_activityLogs.TryGetValue(username, out var log))
                    {
                        return report;
                    }

                    var filteredEntries = log.Entries
                        .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                        .ToList();

                    report.Activities = filteredEntries;
                    report.TotalLoginAttempts = filteredEntries.Count(e => e.EventType.Contains("Login"));
                    report.SuccessfulLogins = log.SuccessfulLogins;
                    report.FailedLoginAttempts = log.FailedLoginAttempts;
                    report.PrivilegeEscalationCount = filteredEntries.Count(e => e.EventType == "PrivilegeEscalation");
                    report.AnomalousActivityCount = filteredEntries.Count(e => e.IsAnomalous);
                    
                    // Calculate risk level
                    report.RiskLevel = CalculateRiskLevel(report);

                    LogMessage($"Generated activity report for: {username}");
                    return report;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error generating activity report: {ex.Message}", LogLevel.Error);
                    return report;
                }
            });
        }

        /// <summary>
        /// Alerts on suspicious activity.
        /// </summary>
        public async Task<bool> CheckForSuspiciousActivityAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_activityLogs.TryGetValue(username, out var log))
                    {
                        return false;
                    }

                    var recentEntries = log.Entries
                        .Where(e => e.Timestamp > DateTime.Now.AddHours(-1))
                        .ToList();

                    var anomalies = recentEntries.Where(e => e.IsAnomalous).ToList();

                    if (anomalies.Count > 5)
                    {
                        LogMessage($"ALERT: Suspicious activity detected for user {username}. Found {anomalies.Count} anomalies in last hour.", LogLevel.Warning);
                        return true;
                    }

                    if (log.FailedLoginAttempts > 5)
                    {
                        LogMessage($"ALERT: Multiple failed login attempts for {username} ({log.FailedLoginAttempts} attempts)", LogLevel.Warning);
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error checking for suspicious activity: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Archives old logs.
        /// </summary>
        public async Task<bool> ArchiveOldLogsAsync(int daysToKeep = 90)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string archivePath = Path.Combine(_auditPath, "Archive");
                    Directory.CreateDirectory(archivePath);

                    var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                    foreach (var username in _activityLogs.Keys.ToList())
                    {
                        var log = _activityLogs[username];
                        var oldEntries = log.Entries.Where(e => e.Timestamp < cutoffDate).ToList();

                        if (oldEntries.Count > 0)
                        {
                            // Archive old entries
                            string archiveFile = Path.Combine(archivePath, $"{username}-{DateTime.Now:yyyy-MM-dd}.log");
                            var sb = new StringBuilder();

                            foreach (var entry in oldEntries)
                            {
                                sb.AppendLine($"{entry.Timestamp:yyyy-MM-dd HH:mm:ss} [{entry.EventType}] {entry.Description}");
                            }

                            File.AppendAllText(archiveFile, sb.ToString());

                            // Remove archived entries
                            log.Entries.RemoveAll(e => e.Timestamp < cutoffDate);

                            LogMessage($"Archived {oldEntries.Count} entries for: {username}");
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error archiving logs: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Gets activity log for user.
        /// </summary>
        public UserActivityLog GetActivityLog(string username)
        {
            _activityLogs.TryGetValue(username, out var log);
            return log;
        }

        /// <summary>
        /// Gets all high-severity events.
        /// </summary>
        public List<ActivityEntry> GetHighSeverityEvents()
        {
            var events = new List<ActivityEntry>();

            foreach (var log in _activityLogs.Values)
            {
                events.AddRange(log.Entries.Where(e => e.Severity == "High" || e.Severity == "Critical"));
            }

            return events.OrderByDescending(e => e.Timestamp).ToList();
        }

        private UserActivityLog GetOrCreateActivityLog(string username)
        {
            if (!_activityLogs.TryGetValue(username, out var log))
            {
                log = new UserActivityLog
                {
                    Username = username,
                    CreatedDate = DateTime.Now
                };
                _activityLogs[username] = log;
            }

            return log;
        }

        private string CalculateRiskLevel(ActivityReport report)
        {
            int riskScore = 0;

            if (report.FailedLoginAttempts > 10) riskScore += 30;
            else if (report.FailedLoginAttempts > 5) riskScore += 20;
            else if (report.FailedLoginAttempts > 0) riskScore += 10;

            if (report.PrivilegeEscalationCount > 5) riskScore += 30;
            else if (report.PrivilegeEscalationCount > 0) riskScore += 20;

            if (report.AnomalousActivityCount > 10) riskScore += 30;
            else if (report.AnomalousActivityCount > 5) riskScore += 20;
            else if (report.AnomalousActivityCount > 0) riskScore += 10;

            return riskScore switch
            {
                >= 70 => "Critical",
                >= 50 => "High",
                >= 30 => "Medium",
                >= 10 => "Low",
                _ => "Minimal"
            };
        }

        private void LoadExistingLogs()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserAccount"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        string username = mo["Name"]?.ToString();
                        if (!string.IsNullOrEmpty(username))
                        {
                            GetOrCreateActivityLog(username);
                        }
                    }
                }
            }
            catch { }
        }

        private void EnsureDirectories()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_logPath));
                Directory.CreateDirectory(_auditPath);
                Directory.CreateDirectory(Path.Combine(_auditPath, "Archive"));
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

        public class ActivityReport
        {
            public string Username { get; set; }
            public DateTime ReportDate { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int TotalLoginAttempts { get; set; }
            public int SuccessfulLogins { get; set; }
            public int FailedLoginAttempts { get; set; }
            public int PrivilegeEscalationCount { get; set; }
            public int AnomalousActivityCount { get; set; }
            public List<ActivityEntry> Activities { get; set; } = new();
            public string RiskLevel { get; set; }
        }
    }
}
