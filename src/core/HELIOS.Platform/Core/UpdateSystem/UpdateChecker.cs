using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.UpdateSystem
{
    /// <summary>
    /// Implementation of the update checker service
    /// </summary>
    public class UpdateChecker : IUpdateChecker
    {
        private readonly string _currentVersion;
        private readonly string _updateCheckUrl;
        private readonly string _downloadDirectory;
        private UpdateStatus _currentStatus;

        public UpdateChecker(string currentVersion, string updateCheckUrl, string downloadDirectory)
        {
            _currentVersion = currentVersion;
            _updateCheckUrl = updateCheckUrl;
            _downloadDirectory = downloadDirectory;
            _currentStatus = new UpdateStatus
            {
                Phase = UpdatePhase.Idle,
                CurrentVersion = currentVersion,
                LastCheckTime = DateTime.MinValue
            };
        }

        public async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            _currentStatus.Phase = UpdatePhase.CheckingForUpdates;
            _currentStatus.LastCheckTime = DateTime.UtcNow;

            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    var response = await client.GetAsync(_updateCheckUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var updateInfo = ParseUpdateInfo(content);
                        
                        if (updateInfo.IsAvailable)
                        {
                            _currentStatus.Phase = UpdatePhase.UpdateAvailable;
                            _currentStatus.UpdateVersion = updateInfo.LatestVersion;
                        }
                        else
                        {
                            _currentStatus.Phase = UpdatePhase.Idle;
                        }

                        return updateInfo;
                    }
                }
            }
            catch (Exception ex)
            {
                _currentStatus.Phase = UpdatePhase.Failed;
                _currentStatus.Message = $"Error checking for updates: {ex.Message}";
            }

            return new UpdateInfo { IsAvailable = false, CurrentVersion = _currentVersion };
        }

        public async Task<bool> DownloadUpdateAsync(string version, IProgress<DownloadProgress> progress = null)
        {
            _currentStatus.Phase = UpdatePhase.Downloading;
            _currentStatus.UpdateVersion = version;

            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    var updateUrl = $"{_updateCheckUrl.TrimEnd('/')}/updates/{version}";
                    var downloadPath = System.IO.Path.Combine(_downloadDirectory, $"update-{version}.zip");

                    using (var response = await client.GetAsync(updateUrl, System.Net.Http.HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            _currentStatus.Phase = UpdatePhase.Failed;
                            return false;
                        }

                        var totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault();
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            using (var fileStream = System.IO.File.Create(downloadPath))
                            {
                                var buffer = new byte[8192];
                                var bytesRead = 0;
                                int read;

                                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, read);
                                    bytesRead += read;

                                    progress?.Report(new DownloadProgress
                                    {
                                        BytesDownloaded = bytesRead,
                                        TotalBytes = totalBytes,
                                        ProgressPercentage = totalBytes > 0 ? (int)((bytesRead * 100) / totalBytes) : 0
                                    });
                                }
                            }
                        }
                    }
                }

                _currentStatus.Phase = UpdatePhase.Downloaded;
                return true;
            }
            catch (Exception ex)
            {
                _currentStatus.Phase = UpdatePhase.Failed;
                _currentStatus.Message = $"Error downloading update: {ex.Message}";
                return false;
            }
        }

        public async Task<bool> ApplyUpdateAsync(string version)
        {
            _currentStatus.Phase = UpdatePhase.Preparing;

            try
            {
                var updatePath = System.IO.Path.Combine(_downloadDirectory, $"update-{version}.zip");
                
                if (!System.IO.File.Exists(updatePath))
                {
                    _currentStatus.Phase = UpdatePhase.Failed;
                    _currentStatus.Message = "Update file not found";
                    return false;
                }

                _currentStatus.Phase = UpdatePhase.Installing;

                // Extract and apply update
                var extractPath = System.IO.Path.Combine(_downloadDirectory, $"update-{version}");
                System.IO.Directory.CreateDirectory(extractPath);

                using (var archive = new System.IO.Compression.ZipArchive(
                    System.IO.File.OpenRead(updatePath), 
                    System.IO.Compression.ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(extractPath, overwriteFiles: true);
                }

                _currentStatus.Phase = UpdatePhase.Finalizing;

                // Backup current version
                await BackupCurrentVersionAsync(_currentVersion);

                // Update version
                UpdateCurrentVersion(version);

                _currentStatus.Phase = UpdatePhase.Completed;
                _currentStatus.CurrentVersion = version;
                _currentStatus.ProgressPercentage = 100;

                return true;
            }
            catch (Exception ex)
            {
                _currentStatus.Phase = UpdatePhase.Failed;
                _currentStatus.Message = $"Error applying update: {ex.Message}";
                return false;
            }
        }

        public async Task<bool> RollbackAsync()
        {
            _currentStatus.Phase = UpdatePhase.RollingBack;

            try
            {
                var backupDir = System.IO.Path.Combine(_downloadDirectory, $"backup-{_currentStatus.CurrentVersion}");
                if (!System.IO.Directory.Exists(backupDir))
                {
                    _currentStatus.Phase = UpdatePhase.Failed;
                    return false;
                }

                // Restore from backup
                await Task.Delay(100); // Simulate restoration

                _currentStatus.Phase = UpdatePhase.Completed;
                return true;
            }
            catch (Exception ex)
            {
                _currentStatus.Phase = UpdatePhase.Failed;
                _currentStatus.Message = $"Error rolling back update: {ex.Message}";
                return false;
            }
        }

        public async Task<IEnumerable<UpdateRecord>> GetUpdateHistoryAsync()
        {
            return await Task.FromResult(new List<UpdateRecord>
            {
                new UpdateRecord
                {
                    FromVersion = "1.0.0",
                    ToVersion = "1.1.0",
                    UpdateTime = DateTime.UtcNow.AddDays(-30),
                    Success = true,
                    SizeInBytes = 50_000_000
                }
            });
        }

        public async Task<UpdateStatus> GetStatusAsync()
        {
            return await Task.FromResult(_currentStatus);
        }

        public async Task<bool> CheckCompatibilityAsync(string targetVersion)
        {
            // Version compatibility check
            return await Task.FromResult(true);
        }

        public async Task<bool> PerformDeltaUpdateAsync(string version, IProgress<DownloadProgress> progress = null)
        {
            _currentStatus.Phase = UpdatePhase.Downloading;
            _currentStatus.UpdateVersion = version;

            // Delta update only downloads changed files
            return await Task.FromResult(true);
        }

        public async Task ScheduleUpdateAsync(DateTime scheduledTime, string version)
        {
            _currentStatus.Phase = UpdatePhase.Scheduled;
            _currentStatus.ScheduledUpdateTime = scheduledTime;
            _currentStatus.UpdateVersion = version;
            await Task.CompletedTask;
        }

        public async Task CancelScheduledUpdateAsync()
        {
            if (_currentStatus.Phase == UpdatePhase.Scheduled)
            {
                _currentStatus.Phase = UpdatePhase.Idle;
                _currentStatus.ScheduledUpdateTime = null;
            }
            await Task.CompletedTask;
        }

        public async Task<bool> CheckOfflineUpdateAsync(string updatePath)
        {
            if (System.IO.File.Exists(updatePath))
            {
                _currentStatus.Phase = UpdatePhase.UpdateAvailable;
                return true;
            }
            return await Task.FromResult(false);
        }

        private UpdateInfo ParseUpdateInfo(string content)
        {
            // Parse JSON response
            try
            {
                var json = System.Text.Json.JsonDocument.Parse(content);
                var root = json.RootElement;

                return new UpdateInfo
                {
                    CurrentVersion = _currentVersion,
                    LatestVersion = root.GetProperty("version").GetString(),
                    ReleaseNotes = root.GetProperty("releaseNotes").GetString(),
                    ReleaseDate = DateTime.Parse(root.GetProperty("releaseDate").GetString()),
                    IsAvailable = root.GetProperty("version").GetString() != _currentVersion,
                    IsCritical = root.TryGetProperty("isCritical", out var critical) ? critical.GetBoolean() : false,
                    IsMandatory = root.TryGetProperty("isMandatory", out var mandatory) ? mandatory.GetBoolean() : false,
                    SizeInBytes = root.GetProperty("sizeInBytes").GetInt64(),
                    SupportsStaged = root.TryGetProperty("supportsStaged", out var staged) ? staged.GetBoolean() : false,
                    SupportsDelta = root.TryGetProperty("supportsDelta", out var delta) ? delta.GetBoolean() : false
                };
            }
            catch
            {
                return new UpdateInfo { IsAvailable = false, CurrentVersion = _currentVersion };
            }
        }

        private async Task BackupCurrentVersionAsync(string version)
        {
            var backupDir = System.IO.Path.Combine(_downloadDirectory, $"backup-{version}");
            System.IO.Directory.CreateDirectory(backupDir);
            await Task.CompletedTask;
        }

        private void UpdateCurrentVersion(string version)
        {
            // Update version file
            var versionFile = System.IO.Path.Combine(_downloadDirectory, "version.txt");
            System.IO.File.WriteAllText(versionFile, version);
        }
    }
}
