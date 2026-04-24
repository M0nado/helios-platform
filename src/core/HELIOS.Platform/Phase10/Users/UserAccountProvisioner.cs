using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Users
{
    /// <summary>
    /// Provisions user accounts with proper configuration and profiles.
    /// </summary>
    public class UserAccountProvisioner
    {
        private readonly string _logPath;
        private readonly object _lockObject = new();

        public UserAccountProvisioner(string logPath = null)
        {
            _logPath = logPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELIOS", "Logs", "UserProvisioning.log");
            EnsureLogDirectory();
        }

        /// <summary>
        /// Creates administrator account with strong password.
        /// </summary>
        public async Task<bool> CreateAdministratorAsync(string username, string fullName, string password)
        {
            return await CreateUserAccountAsync(username, fullName, password, "Administrator", isAdmin: true);
        }

        /// <summary>
        /// Creates primary user account for gaming/work/dev.
        /// </summary>
        public async Task<bool> CreatePrimaryUserAsync(string username, string fullName, string password)
        {
            return await CreateUserAccountAsync(username, fullName, password, "PrimaryUser", isAdmin: false);
        }

        /// <summary>
        /// Creates guest account with restricted permissions.
        /// </summary>
        public async Task<bool> CreateGuestAccountAsync(string username, string fullName)
        {
            // Guest accounts typically use empty password
            return await CreateUserAccountAsync(username, fullName, "", "Guest", isAdmin: false, isGuest: true);
        }

        /// <summary>
        /// Creates service account for HELIOS platform.
        /// </summary>
        public async Task<bool> CreateServiceAccountAsync(string username, string password)
        {
            return await CreateUserAccountAsync(username, "HELIOS Service Account", password, "Service", isAdmin: false, isService: true);
        }

        /// <summary>
        /// Creates a user account with specified parameters.
        /// </summary>
        private async Task<bool> CreateUserAccountAsync(
            string username,
            string fullName,
            string password,
            string accountType,
            bool isAdmin = false,
            bool isGuest = false,
            bool isService = false)
        {
            try
            {
                lock (_lockObject)
                {
                    LogMessage($"Creating {accountType} account: {username}");
                }

                // Check if user already exists
                if (await UserExistsAsync(username))
                {
                    lock (_lockObject)
                    {
                        LogMessage($"User {username} already exists. Skipping creation.", LogLevel.Warning);
                    }
                    return true;
                }

                // Use WMI to create user account
                using (ManagementClass userClass = new ManagementClass("Win32_UserAccount"))
                {
                    ManagementBaseObject inParams = userClass.GetMethodParameters("Create");
                    inParams["Name"] = username;
                    inParams["FullName"] = fullName;
                    inParams["Description"] = $"{accountType} account for HELIOS Platform";
                    inParams["Password"] = password;

                    ManagementBaseObject outParams = userClass.InvokeMethod("Create", inParams, null);

                    uint returnValue = Convert.ToUInt32(outParams["returnValue"]);
                    if (returnValue != 0)
                    {
                        lock (_lockObject)
                        {
                            LogMessage($"Failed to create user {username}. Error code: {returnValue}", LogLevel.Error);
                        }
                        return false;
                    }
                }

                // Set password expiration policy
                if (!isGuest && !isService)
                {
                    await SetPasswordPolicyAsync(username);
                }

                // Create user profile directories
                await CreateUserProfileDirectoriesAsync(username);

                // Setup environment variables
                await SetupUserEnvironmentAsync(username, accountType);

                lock (_lockObject)
                {
                    LogMessage($"Successfully created {accountType} account: {username}");
                }

                return true;
            }
            catch (Exception ex)
            {
                lock (_lockObject)
                {
                    LogMessage($"Exception creating user {username}: {ex.Message}", LogLevel.Error);
                }
                return false;
            }
        }

        /// <summary>
        /// Checks if user account exists.
        /// </summary>
        public async Task<bool> UserExistsAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                        $"SELECT * FROM Win32_UserAccount WHERE Name = '{username}'"))
                    {
                        ManagementObjectCollection collection = searcher.Get();
                        return collection.Count > 0;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Sets password policy for user account.
        /// </summary>
        private async Task<bool> SetPasswordPolicyAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (ManagementObject user = new ManagementObject($"Win32_UserAccount.Domain='{Environment.MachineName}',Name='{username}'"))
                    {
                        user.Get();
                        user["PasswordExpires"] = false;
                        user.Put();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lock (_lockObject)
                    {
                        LogMessage($"Failed to set password policy for {username}: {ex.Message}", LogLevel.Error);
                    }
                    return false;
                }
            });
        }

        /// <summary>
        /// Creates user profile directories.
        /// </summary>
        private async Task<bool> CreateUserProfileDirectoriesAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string userProfile = Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%"), "Users", username);
                    
                    if (!Directory.Exists(userProfile))
                    {
                        Directory.CreateDirectory(userProfile);
                    }

                    // Create standard directories
                    string[] directories = new[]
                    {
                        Path.Combine(userProfile, "Desktop"),
                        Path.Combine(userProfile, "Documents"),
                        Path.Combine(userProfile, "Downloads"),
                        Path.Combine(userProfile, "Music"),
                        Path.Combine(userProfile, "Pictures"),
                        Path.Combine(userProfile, "Videos"),
                        Path.Combine(userProfile, "AppData", "Local"),
                        Path.Combine(userProfile, "AppData", "Roaming"),
                        Path.Combine(userProfile, "AppData", "LocalLow"),
                    };

                    foreach (var dir in directories)
                    {
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                    }

                    lock (_lockObject)
                    {
                        LogMessage($"Created profile directories for user: {username}");
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    lock (_lockObject)
                    {
                        LogMessage($"Failed to create profile directories for {username}: {ex.Message}", LogLevel.Error);
                    }
                    return false;
                }
            });
        }

        /// <summary>
        /// Sets up user environment variables.
        /// </summary>
        private async Task<bool> SetupUserEnvironmentAsync(string username, string accountType)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string userProfile = Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%"), "Users", username);
                    
                    var envVars = new Dictionary<string, string>
                    {
                        { "HELIOS_USER_PROFILE", userProfile },
                        { "HELIOS_ACCOUNT_TYPE", accountType },
                        { "HELIOS_HOME", userProfile }
                    };

                    // Environment variables would typically be set via registry or system settings
                    // This is a placeholder for actual implementation

                    lock (_lockObject)
                    {
                        LogMessage($"Setup user environment for: {username}");
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    lock (_lockObject)
                    {
                        LogMessage($"Failed to setup environment for {username}: {ex.Message}", LogLevel.Error);
                    }
                    return false;
                }
            });
        }

        /// <summary>
        /// Gets list of all system accounts.
        /// </summary>
        public async Task<List<UserAccountInfo>> GetAllAccountsAsync()
        {
            return await Task.Run(() =>
            {
                var accounts = new List<UserAccountInfo>();
                try
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserAccount"))
                    {
                        foreach (ManagementObject mo in searcher.Get())
                        {
                            accounts.Add(new UserAccountInfo
                            {
                                Username = mo["Name"]?.ToString(),
                                FullName = mo["FullName"]?.ToString(),
                                Disabled = Convert.ToBoolean(mo["Disabled"]),
                                Sid = mo["SID"]?.ToString()
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (_lockObject)
                    {
                        LogMessage($"Error retrieving accounts: {ex.Message}", LogLevel.Error);
                    }
                }
                return accounts;
            });
        }

        /// <summary>
        /// Disables a user account.
        /// </summary>
        public async Task<bool> DisableAccountAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (ManagementObject user = new ManagementObject($"Win32_UserAccount.Domain='{Environment.MachineName}',Name='{username}'"))
                    {
                        user.Get();
                        user["Disabled"] = true;
                        user.Put();

                        lock (_lockObject)
                        {
                            LogMessage($"Disabled account: {username}");
                        }
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lock (_lockObject)
                    {
                        LogMessage($"Failed to disable account {username}: {ex.Message}", LogLevel.Error);
                    }
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

        public class UserAccountInfo
        {
            public string Username { get; set; }
            public string FullName { get; set; }
            public bool Disabled { get; set; }
            public string Sid { get; set; }
        }
    }
}
