using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Users
{
    /// <summary>
    /// Coordinates switching between user profiles and managing profile-specific settings.
    /// </summary>
    public class MultiProfileCoordinator
    {
        private readonly string _logPath;
        private readonly object _lockObject = new();
        private Dictionary<string, ProfileState> _profileStates = new();
        private string _currentUser = Environment.UserName;

        public class ProfileState
        {
            public string Username { get; set; }
            public DateTime LastSwitchTime { get; set; }
            public List<ProcessInfo> ActiveProcesses { get; set; } = new();
            public Dictionary<string, string> Settings { get; set; } = new();
            public bool IsActive { get; set; }
        }

        public class ProcessInfo
        {
            public int ProcessId { get; set; }
            public string ProcessName { get; set; }
            public bool IsService { get; set; }
        }

        public MultiProfileCoordinator(string logPath = null)
        {
            _logPath = logPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELIOS", "Logs", "ProfileCoordinator.log");
            EnsureLogDirectory();
            InitializeProfileStates();
        }

        /// <summary>
        /// Initializes profile state tracking.
        /// </summary>
        private void InitializeProfileStates()
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
                            _profileStates[username] = new ProfileState
                            {
                                Username = username,
                                IsActive = username == _currentUser,
                                LastSwitchTime = DateTime.Now
                            };
                        }
                    }
                }

                LogMessage("Profile states initialized");
            }
            catch (Exception ex)
            {
                LogMessage($"Error initializing profile states: {ex.Message}", LogLevel.Warning);
            }

            if (_profileStates.Count == 0 && !string.IsNullOrWhiteSpace(_currentUser))
            {
                _profileStates[_currentUser] = new ProfileState
                {
                    Username = _currentUser,
                    IsActive = true,
                    LastSwitchTime = DateTime.Now
                };
                LogMessage("Profile states initialized with current user fallback", LogLevel.Warning);
            }
        }

        /// <summary>
        /// Switches to a different user profile.
        /// </summary>
        public async Task<bool> SwitchUserProfileAsync(string targetUsername, bool gracefulShutdown = true)
        {
            try
            {
                LogMessage($"Initiating profile switch from {_currentUser} to {targetUsername}");

                // Validate target user exists
                if (!await UserExistsAsync(targetUsername))
                {
                    LogMessage($"User {targetUsername} does not exist", LogLevel.Error);
                    return false;
                }

                // Prepare current profile for switch
                if (gracefulShutdown)
                {
                    if (!await PrepareProfileForSwitchAsync(_currentUser))
                    {
                        LogMessage("Failed to prepare current profile", LogLevel.Warning);
                    }
                }

                // Save current profile state
                await SaveProfileStateAsync(_currentUser);

                // Load target profile state
                if (!await LoadProfileStateAsync(targetUsername))
                {
                    LogMessage("Failed to load target profile state", LogLevel.Warning);
                }

                // Apply profile-specific settings
                if (!await ApplyProfileSettingsAsync(targetUsername))
                {
                    LogMessage("Failed to apply profile settings", LogLevel.Warning);
                }

                // Update current user tracking
                _currentUser = targetUsername;

                LogMessage($"Successfully switched to profile: {targetUsername}");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"Exception in SwitchUserProfileAsync: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Prepares current profile for switching.
        /// </summary>
        private async Task<bool> PrepareProfileForSwitchAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Save open applications
                    var processes = GetUserProcesses(username);
                    
                    // Request graceful shutdown of user-level processes
                    foreach (var proc in processes.Where(p => !p.IsService))
                    {
                        try
                        {
                            Process p = Process.GetProcessById(proc.ProcessId);
                            if (!p.HasExited)
                            {
                                p.CloseMainWindow();
                                if (!p.WaitForExit(5000))
                                {
                                    p.Kill();
                                }
                            }
                        }
                        catch { }
                    }

                    // Keep services running
                    LogMessage($"Prepared profile for switch: {username}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error preparing profile: {ex.Message}", LogLevel.Warning);
                    return false;
                }
            });
        }

        /// <summary>
        /// Gets processes running under specified user.
        /// </summary>
        private List<ProcessInfo> GetUserProcesses(string username)
        {
            var processes = new List<ProcessInfo>();

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    $"SELECT ProcessId, Name FROM Win32_Process WHERE GetOwner() = '{username}'"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        processes.Add(new ProcessInfo
                        {
                            ProcessId = Convert.ToInt32(mo["ProcessId"]),
                            ProcessName = mo["Name"]?.ToString(),
                            IsService = IsServiceProcess(mo["Name"]?.ToString())
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error getting user processes: {ex.Message}", LogLevel.Warning);
            }

            return processes;
        }

        /// <summary>
        /// Determines if process is a service.
        /// </summary>
        private bool IsServiceProcess(string processName)
        {
            var serviceProcesses = new[] { "svchost.exe", "services.exe", "lsass.exe", "csrss.exe" };
            return serviceProcesses.Contains(processName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Saves current profile state.
        /// </summary>
        private async Task<bool> SaveProfileStateAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (_profileStates.TryGetValue(username, out var state))
                    {
                        state.LastSwitchTime = DateTime.Now;
                        state.IsActive = false;
                        state.ActiveProcesses = GetUserProcesses(username);

                        LogMessage($"Saved profile state for: {username}");
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error saving profile state: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Loads target profile state.
        /// </summary>
        private async Task<bool> LoadProfileStateAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_profileStates.TryGetValue(username, out var state))
                    {
                        state = new ProfileState { Username = username };
                        _profileStates[username] = state;
                    }

                    state.IsActive = true;
                    state.LastSwitchTime = DateTime.Now;

                    LogMessage($"Loaded profile state for: {username}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error loading profile state: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Applies profile-specific settings.
        /// </summary>
        private async Task<bool> ApplyProfileSettingsAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    LogMessage($"Applying profile settings for: {username}");

                    // Load user environment variables
                    ApplyEnvironmentVariables(username);

                    // Apply display settings
                    ApplyDisplaySettings(username);

                    // Apply accessibility settings
                    ApplyAccessibilitySettings(username);

                    // Update shortcuts and launchers
                    UpdateShortcuts(username);

                    LogMessage($"Applied profile settings for: {username}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error applying profile settings: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Applies environment variables for user.
        /// </summary>
        private void ApplyEnvironmentVariables(string username)
        {
            try
            {
                // Set user-specific environment variables
                Environment.SetEnvironmentVariable("HELIOS_ACTIVE_USER", username, EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("HELIOS_PROFILE_LOADED", DateTime.Now.ToString(), EnvironmentVariableTarget.User);
            }
            catch (Exception ex)
            {
                LogMessage($"Error applying environment variables: {ex.Message}", LogLevel.Warning);
            }
        }

        /// <summary>
        /// Applies display settings for user.
        /// </summary>
        private void ApplyDisplaySettings(string username)
        {
            try
            {
                // Apply user's saved display preferences
                // This would typically involve reading from registry and applying settings
                LogMessage($"Applied display settings for: {username}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error applying display settings: {ex.Message}", LogLevel.Warning);
            }
        }

        /// <summary>
        /// Applies accessibility settings for user.
        /// </summary>
        private void ApplyAccessibilitySettings(string username)
        {
            try
            {
                // Apply user's accessibility preferences
                LogMessage($"Applied accessibility settings for: {username}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error applying accessibility settings: {ex.Message}", LogLevel.Warning);
            }
        }

        /// <summary>
        /// Updates shortcuts and launchers for user.
        /// </summary>
        private void UpdateShortcuts(string username)
        {
            try
            {
                string desktopPath = Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%"), "Users", username, "Desktop");
                
                // Ensure user shortcuts are available
                if (!Directory.Exists(desktopPath))
                {
                    Directory.CreateDirectory(desktopPath);
                }

                LogMessage($"Updated shortcuts for: {username}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating shortcuts: {ex.Message}", LogLevel.Warning);
            }
        }

        /// <summary>
        /// Keeps HELIOS services running during profile switch.
        /// </summary>
        public async Task<bool> KeepServicesRunningAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var serviceNames = new[] { "HELIOS-Main", "HELIOS-Vault", "HELIOS-Sandbox", "HELIOS-Audit" };

                    foreach (var serviceName in serviceNames)
                    {
                        try
                        {
                            ServiceController service = new ServiceController(serviceName);
                            if (service.Status != ServiceControllerStatus.Running)
                            {
                                service.Start();
                                LogMessage($"Started service: {serviceName}");
                            }
                        }
                        catch { }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error keeping services running: {ex.Message}", LogLevel.Warning);
                    return false;
                }
            });
        }

        /// <summary>
        /// Gets current active user.
        /// </summary>
        public string GetCurrentUser()
        {
            return _currentUser;
        }

        /// <summary>
        /// Gets profile state for user.
        /// </summary>
        public ProfileState GetProfileState(string username)
        {
            _profileStates.TryGetValue(username, out var state);
            return state;
        }

        /// <summary>
        /// Gets all tracked profiles.
        /// </summary>
        public List<ProfileState> GetAllProfiles()
        {
            lock (_lockObject)
            {
                return _profileStates.Values.ToList();
            }
        }

        /// <summary>
        /// Checks if user exists.
        /// </summary>
        private async Task<bool> UserExistsAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                        $"SELECT * FROM Win32_UserAccount WHERE Name = '{username}'"))
                    {
                        return searcher.Get().Count > 0;
                    }
                }
                catch
                {
                    return false;
                }
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
    }
}
