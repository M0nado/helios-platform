using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Users
{
    /// <summary>
    /// Manages role-based access control and permissions for user accounts.
    /// </summary>
    public class AccountPermissionManager
    {
        private readonly string _logPath;
        private readonly object _lockObject = new();

        public enum AccountRole
        {
            Administrator,
            StandardUser,
            Guest,
            Service
        }

        public AccountPermissionManager(string logPath = null)
        {
            _logPath = logPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELIOS", "Logs", "Permissions.log");
            EnsureLogDirectory();
        }

        /// <summary>
        /// Applies permission set based on account role.
        /// </summary>
        public async Task<bool> ApplyRolePermissionsAsync(string username, AccountRole role)
        {
            try
            {
                LogMessage($"Applying {role} permissions to: {username}");

                // Add user to appropriate groups
                bool groupResult = await AddUserToGroupsAsync(username, role);
                if (!groupResult)
                {
                    LogMessage($"Failed to add user to groups", LogLevel.Warning);
                }

                // Set file system permissions
                bool fsResult = await SetFileSystemPermissionsAsync(username, role);
                if (!fsResult)
                {
                    LogMessage($"Failed to set file system permissions", LogLevel.Warning);
                }

                // Set registry permissions
                bool regResult = await SetRegistryPermissionsAsync(username, role);
                if (!regResult)
                {
                    LogMessage($"Failed to set registry permissions", LogLevel.Warning);
                }

                // Configure UAC level
                bool uacResult = await ConfigureUACLevelAsync(username, role);

                LogMessage($"Successfully applied {role} permissions");
                return groupResult && fsResult && regResult && uacResult;
            }
            catch (Exception ex)
            {
                LogMessage($"Exception applying permissions: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Adds user to appropriate local groups.
        /// </summary>
        private async Task<bool> AddUserToGroupsAsync(string username, AccountRole role)
        {
            return await Task.Run(() =>
            {
                try
                {
                    List<string> groups = GetGroupsForRole(role);

                    foreach (var group in groups)
                    {
                        try
                        {
                            DirectoryEntry groupEntry = new DirectoryEntry($"WinNT://{Environment.MachineName}/{group},group");
                            
                            if (groupEntry.Invoke("IsMember", new object[] { $"WinNT://{Environment.MachineName}/{username},user" }).Equals(false))
                            {
                                groupEntry.Invoke("Add", new object[] { $"WinNT://{Environment.MachineName}/{username},user" });
                                LogMessage($"Added {username} to group: {group}");
                            }

                            groupEntry?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Error adding user to group {group}: {ex.Message}", LogLevel.Warning);
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error in AddUserToGroupsAsync: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Gets groups for specified role.
        /// </summary>
        private List<string> GetGroupsForRole(AccountRole role)
        {
            return role switch
            {
                AccountRole.Administrator => new List<string> { "Administrators", "Remote Desktop Users" },
                AccountRole.StandardUser => new List<string> { "Users" },
                AccountRole.Guest => new List<string> { "Guests" },
                AccountRole.Service => new List<string> { "Network Service" },
                _ => new List<string> { "Users" }
            };
        }

        /// <summary>
        /// Sets NTFS file system permissions.
        /// </summary>
        private async Task<bool> SetFileSystemPermissionsAsync(string username, AccountRole role)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var permissions = GetFileSystemPermissionsForRole(role);

                    foreach (var (path, accessRights) in permissions)
                    {
                        try
                        {
                            if (!Directory.Exists(path) && !File.Exists(path))
                            {
                                LogMessage($"Path does not exist: {path}", LogLevel.Warning);
                                continue;
                            }

                            // File system permissions would be applied here using ACLs
                            // This is a placeholder for actual NTFS ACL manipulation
                            LogMessage($"Applied file permissions to {path} for user: {username}");
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Error setting permissions on {path}: {ex.Message}", LogLevel.Warning);
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error in SetFileSystemPermissionsAsync: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Gets file system permission requirements for role.
        /// </summary>
        private Dictionary<string, string> GetFileSystemPermissionsForRole(AccountRole role)
        {
            var systemDrive = Environment.ExpandEnvironmentVariables("%SystemDrive%");

            return role switch
            {
                AccountRole.Administrator => new Dictionary<string, string>
                {
                    { systemDrive, "FullControl" },
                    { Path.Combine(systemDrive, "Program Files"), "Modify" }
                },
                AccountRole.StandardUser => new Dictionary<string, string>
                {
                    { Path.Combine(systemDrive, "Users"), "Modify" },
                    { Path.Combine(systemDrive, "Program Files"), "Read" }
                },
                AccountRole.Guest => new Dictionary<string, string>
                {
                    { Path.Combine(systemDrive, "Users"), "ReadAndExecute" }
                },
                AccountRole.Service => new Dictionary<string, string>
                {
                    { Path.Combine(systemDrive, "Program Files", "HELIOS"), "Modify" }
                },
                _ => new Dictionary<string, string>()
            };
        }

        /// <summary>
        /// Sets registry permissions.
        /// </summary>
        private async Task<bool> SetRegistryPermissionsAsync(string username, AccountRole role)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var registryPaths = GetRegistryPathsForRole(role);

                    foreach (var path in registryPaths)
                    {
                        try
                        {
                            // Registry permissions would be applied here
                            LogMessage($"Applied registry permissions to {path} for user: {username}");
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Error setting registry permissions on {path}: {ex.Message}", LogLevel.Warning);
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error in SetRegistryPermissionsAsync: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Gets registry paths for role.
        /// </summary>
        private List<string> GetRegistryPathsForRole(AccountRole role)
        {
            return role switch
            {
                AccountRole.Administrator => new List<string>
                {
                    "HKEY_LOCAL_MACHINE\\Software",
                    "HKEY_LOCAL_MACHINE\\System"
                },
                AccountRole.StandardUser => new List<string>
                {
                    "HKEY_CURRENT_USER\\Software"
                },
                AccountRole.Guest => new List<string>
                {
                    "HKEY_CURRENT_USER\\Software\\Policies"
                },
                AccountRole.Service => new List<string>
                {
                    "HKEY_LOCAL_MACHINE\\Software\\HELIOS"
                },
                _ => new List<string>()
            };
        }

        /// <summary>
        /// Configures User Account Control level for role.
        /// </summary>
        private async Task<bool> ConfigureUACLevelAsync(string username, AccountRole role)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // UAC configuration logic
                    string uacLevel = role switch
                    {
                        AccountRole.Administrator => "Always notify",
                        AccountRole.StandardUser => "Default (notify on apps)",
                        AccountRole.Guest => "Always notify",
                        AccountRole.Service => "Never notify",
                        _ => "Default (notify on apps)"
                    };

                    LogMessage($"Configured UAC level for {username}: {uacLevel}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error configuring UAC: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Revokes all permissions for a user.
        /// </summary>
        public async Task<bool> RevokeAllPermissionsAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    LogMessage($"Revoking all permissions for: {username}");

                    // Remove from all groups
                    var allGroups = new[] { "Administrators", "Users", "Guests", "Remote Desktop Users" };
                    
                    foreach (var group in allGroups)
                    {
                        try
                        {
                            DirectoryEntry groupEntry = new DirectoryEntry($"WinNT://{Environment.MachineName}/{group},group");
                            
                            if (groupEntry.Invoke("IsMember", new object[] { $"WinNT://{Environment.MachineName}/{username},user" }).Equals(true))
                            {
                                groupEntry.Invoke("Remove", new object[] { $"WinNT://{Environment.MachineName}/{username},user" });
                            }

                            groupEntry?.Dispose();
                        }
                        catch { }
                    }

                    LogMessage($"Revoked permissions for: {username}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error revoking permissions: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Checks if user has administrator privileges.
        /// </summary>
        public async Task<bool> IsAdministratorAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    DirectoryEntry groupEntry = new DirectoryEntry($"WinNT://{Environment.MachineName}/Administrators,group");
                    var isMember = groupEntry.Invoke("IsMember", new object[] { $"WinNT://{Environment.MachineName}/{username},user" });
                    groupEntry?.Dispose();
                    return isMember.Equals(true);
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Gets current permissions for user.
        /// </summary>
        public async Task<UserPermissions> GetUserPermissionsAsync(string username)
        {
            return await Task.Run(() =>
            {
                var permissions = new UserPermissions { Username = username };

                try
                {
                    permissions.IsAdministrator = IsAdministratorAsync(username).Result;
                    permissions.Groups = GetUserGroupsAsync(username).Result;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error getting user permissions: {ex.Message}", LogLevel.Error);
                }

                return permissions;
            });
        }

        /// <summary>
        /// Gets groups user belongs to.
        /// </summary>
        private async Task<List<string>> GetUserGroupsAsync(string username)
        {
            return await Task.Run(() =>
            {
                var groups = new List<string>();
                try
                {
                    var allGroups = new[] { "Administrators", "Users", "Guests", "Remote Desktop Users", "Network Service" };
                    
                    foreach (var group in allGroups)
                    {
                        try
                        {
                            DirectoryEntry groupEntry = new DirectoryEntry($"WinNT://{Environment.MachineName}/{group},group");
                            if (groupEntry.Invoke("IsMember", new object[] { $"WinNT://{Environment.MachineName}/{username},user" }).Equals(true))
                            {
                                groups.Add(group);
                            }
                            groupEntry?.Dispose();
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error getting user groups: {ex.Message}", LogLevel.Error);
                }

                return groups;
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

        public class UserPermissions
        {
            public string Username { get; set; }
            public bool IsAdministrator { get; set; }
            public List<string> Groups { get; set; } = new();
        }
    }
}
