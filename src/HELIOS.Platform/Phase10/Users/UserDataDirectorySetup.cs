using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Users
{
    /// <summary>
    /// Sets up user data directory structures and folder organization.
    /// </summary>
    public class UserDataDirectorySetup
    {
        private readonly string _logPath;
        private readonly object _lockObject = new();

        public UserDataDirectorySetup(string logPath = null)
        {
            _logPath = logPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELIOS", "Logs", "DirectorySetup.log");
            EnsureLogDirectory();
        }

        /// <summary>
        /// Sets up complete user directory structure.
        /// </summary>
        public async Task<bool> SetupUserDirectoriesAsync(string username)
        {
            try
            {
                LogMessage($"Setting up directories for user: {username}");

                string userProfile = GetUserProfilePath(username);

                // Create standard directories
                var standardDirs = GetStandardDirectories(userProfile);
                foreach (var dir in standardDirs)
                {
                    if (!await CreateDirectoryAsync(dir))
                    {
                        LogMessage($"Failed to create directory: {dir}", LogLevel.Warning);
                    }
                }

                // Setup AppData structure
                if (!await SetupAppDataStructureAsync(username))
                {
                    LogMessage("Failed to setup AppData structure", LogLevel.Warning);
                }

                // Setup hidden system folders
                if (!await SetupSystemFoldersAsync(username))
                {
                    LogMessage("Failed to setup system folders", LogLevel.Warning);
                }

                LogMessage($"Successfully setup directories for user: {username}");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"Exception in SetupUserDirectoriesAsync: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Gets user profile path.
        /// </summary>
        public string GetUserProfilePath(string username)
        {
            return Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%"), "Users", username);
        }

        /// <summary>
        /// Gets list of standard directories to create.
        /// </summary>
        private List<string> GetStandardDirectories(string userProfile)
        {
            return new List<string>
            {
                Path.Combine(userProfile, "Desktop"),
                Path.Combine(userProfile, "Documents"),
                Path.Combine(userProfile, "Downloads"),
                Path.Combine(userProfile, "Music"),
                Path.Combine(userProfile, "Pictures"),
                Path.Combine(userProfile, "Videos"),
                Path.Combine(userProfile, "Favorites"),
                Path.Combine(userProfile, "Links"),
                Path.Combine(userProfile, "Contacts"),
                Path.Combine(userProfile, "Searches"),
                Path.Combine(userProfile, "Recent")
            };
        }

        /// <summary>
        /// Creates a directory asynchronously.
        /// </summary>
        private async Task<bool> CreateDirectoryAsync(string path)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                        LogMessage($"Created directory: {path}");
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error creating directory {path}: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Sets up AppData directory structure with Local, Roaming, and LocalLow.
        /// </summary>
        private async Task<bool> SetupAppDataStructureAsync(string username)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    string userProfile = GetUserProfilePath(username);
                    string appDataBase = Path.Combine(userProfile, "AppData");

                    var appDataDirs = new[]
                    {
                        Path.Combine(appDataBase, "Local"),
                        Path.Combine(appDataBase, "LocalLow"),
                        Path.Combine(appDataBase, "Roaming"),
                        Path.Combine(appDataBase, "Local", "Temp"),
                        Path.Combine(appDataBase, "Local", "History"),
                        Path.Combine(appDataBase, "Local", "Cache"),
                        Path.Combine(appDataBase, "Roaming", "Microsoft"),
                        Path.Combine(appDataBase, "Roaming", "Microsoft", "Windows"),
                        Path.Combine(appDataBase, "Roaming", "Microsoft", "Windows", "Recent"),
                    };

                    bool allSuccess = true;
                    foreach (var dir in appDataDirs)
                    {
                        if (!await CreateDirectoryAsync(dir))
                        {
                            allSuccess = false;
                        }
                    }

                    return allSuccess;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error in SetupAppDataStructureAsync: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Sets up hidden system folders.
        /// </summary>
        private async Task<bool> SetupSystemFoldersAsync(string username)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    string userProfile = GetUserProfilePath(username);

                    var systemFolders = new[]
                    {
                        Path.Combine(userProfile, ".windows"),
                        Path.Combine(userProfile, ".config"),
                        Path.Combine(userProfile, ".local")
                    };

                    bool allSuccess = true;
                    foreach (var folder in systemFolders)
                    {
                        if (!await CreateDirectoryAsync(folder))
                        {
                            allSuccess = false;
                        }
                        else
                        {
                            // Set folder as hidden
                            try
                            {
                                FileInfo fi = new FileInfo(folder);
                                fi.Attributes = FileAttributes.Hidden;
                            }
                            catch { }
                        }
                    }

                    return allSuccess;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error in SetupSystemFoldersAsync: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Creates document folders with categorization.
        /// </summary>
        public async Task<bool> CreateDocumentFoldersAsync(string username)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    string documentsPath = Path.Combine(GetUserProfilePath(username), "Documents");

                    var subfolders = new[]
                    {
                        "Work",
                        "Personal",
                        "Projects",
                        "Archive",
                        "Templates"
                    };

                    bool allSuccess = true;
                    foreach (var folder in subfolders)
                    {
                        if (!await CreateDirectoryAsync(Path.Combine(documentsPath, folder)))
                        {
                            allSuccess = false;
                        }
                    }

                    return allSuccess;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error creating document folders: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Creates media folders with organization.
        /// </summary>
        public async Task<bool> CreateMediaFoldersAsync(string username)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    string userProfile = GetUserProfilePath(username);

                    var mediaFolders = new Dictionary<string, string[]>
                    {
                        { Path.Combine(userProfile, "Pictures"), new[] { "Screenshots", "Wallpapers", "Screenshots", "Saved" } },
                        { Path.Combine(userProfile, "Videos"), new[] { "Recordings", "Movies", "TV Shows", "Edited" } },
                        { Path.Combine(userProfile, "Music"), new[] { "Artists", "Albums", "Playlists", "Downloaded" } }
                    };

                    bool allSuccess = true;
                    foreach (var (mediaDir, subfolders) in mediaFolders)
                    {
                        foreach (var subfolder in subfolders)
                        {
                            if (!await CreateDirectoryAsync(Path.Combine(mediaDir, subfolder)))
                            {
                                allSuccess = false;
                            }
                        }
                    }

                    return allSuccess;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error creating media folders: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Sets up HELIOS-specific folders.
        /// </summary>
        public async Task<bool> SetupHELIOSFoldersAsync(string username)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    string userProfile = GetUserProfilePath(username);
                    string heliosBase = Path.Combine(userProfile, ".helios");

                    var heliosFolders = new[]
                    {
                        Path.Combine(heliosBase, "config"),
                        Path.Combine(heliosBase, "data"),
                        Path.Combine(heliosBase, "cache"),
                        Path.Combine(heliosBase, "vault"),
                        Path.Combine(heliosBase, "logs"),
                        Path.Combine(heliosBase, "temp")
                    };

                    bool allSuccess = true;
                    foreach (var folder in heliosFolders)
                    {
                        if (!await CreateDirectoryAsync(folder))
                        {
                            allSuccess = false;
                        }
                    }

                    return allSuccess;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error setting up HELIOS folders: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Sets up OneDrive sync folders if applicable.
        /// </summary>
        public async Task<bool> SetupOneDriveFoldersAsync(string username, string oneDrivePath = null)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    if (string.IsNullOrEmpty(oneDrivePath))
                    {
                        oneDrivePath = Path.Combine(GetUserProfilePath(username), "OneDrive");
                    }

                    if (!Directory.Exists(oneDrivePath))
                    {
                        Directory.CreateDirectory(oneDrivePath);
                    }

                    var oneDriveFolders = new[]
                    {
                        Path.Combine(oneDrivePath, "Documents"),
                        Path.Combine(oneDrivePath, "Pictures"),
                        Path.Combine(oneDrivePath, "Desktop"),
                        Path.Combine(oneDrivePath, "Shared")
                    };

                    bool allSuccess = true;
                    foreach (var folder in oneDriveFolders)
                    {
                        if (!await CreateDirectoryAsync(folder))
                        {
                            allSuccess = false;
                        }
                    }

                    LogMessage($"Setup OneDrive folders for user: {username}");
                    return allSuccess;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error setting up OneDrive folders: {ex.Message}", LogLevel.Warning);
                    return false;
                }
            });
        }

        /// <summary>
        /// Cleans up user directories (removes folder contents).
        /// </summary>
        public async Task<bool> CleanupUserDirectoriesAsync(string username, bool preserveDocuments = true)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string userProfile = GetUserProfilePath(username);

                    var foldersToClean = new List<string>
                    {
                        Path.Combine(userProfile, "AppData", "Local", "Temp"),
                        Path.Combine(userProfile, "AppData", "Local", "Cache")
                    };

                    foreach (var folder in foldersToClean)
                    {
                        try
                        {
                            if (Directory.Exists(folder))
                            {
                                var dir = new DirectoryInfo(folder);
                                foreach (FileInfo file in dir.GetFiles())
                                {
                                    file.Delete();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Error cleaning folder {folder}: {ex.Message}", LogLevel.Warning);
                        }
                    }

                    LogMessage($"Cleaned up directories for user: {username}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error in CleanupUserDirectoriesAsync: {ex.Message}", LogLevel.Error);
                    return false;
                }
            });
        }

        /// <summary>
        /// Gets total directory size for user.
        /// </summary>
        public async Task<long> GetUserDirectorySizeAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string userProfile = GetUserProfilePath(username);
                    
                    if (!Directory.Exists(userProfile))
                    {
                        return 0;
                    }

                    var dirInfo = new DirectoryInfo(userProfile);
                    return GetDirectorySizeRecursive(dirInfo);
                }
                catch (Exception ex)
                {
                    LogMessage($"Error getting directory size: {ex.Message}", LogLevel.Error);
                    return 0;
                }
            });
        }

        /// <summary>
        /// Recursively gets directory size.
        /// </summary>
        private long GetDirectorySizeRecursive(DirectoryInfo dir)
        {
            try
            {
                long size = 0;

                try
                {
                    foreach (var file in dir.GetFiles())
                    {
                        size += file.Length;
                    }
                }
                catch { }

                foreach (var subDir in dir.GetDirectories())
                {
                    try
                    {
                        size += GetDirectorySizeRecursive(subDir);
                    }
                    catch { }
                }

                return size;
            }
            catch
            {
                return 0;
            }
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
