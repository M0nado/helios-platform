using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.CloudSync.Engine
{
    /// <summary>
    /// Core synchronization engine managing bi-directional sync
    /// </summary>
    public class SyncEngine : ISyncEngine
    {
        private readonly ICloudStorageProvider _provider;
        private readonly ISyncStateStore _stateStore;
        private readonly string _localSyncPath;
        private SyncConflictResolution _conflictResolution;
        private bool _isRunning;

        public event EventHandler<SyncEventArgs> SyncProgress;
        public event EventHandler<SyncEventArgs> SyncError;

        public bool IsRunning => _isRunning;
        public bool IsOfflineMode { get; private set; }

        public SyncEngine(ICloudStorageProvider provider, ISyncStateStore stateStore, string localSyncPath)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            _localSyncPath = localSyncPath ?? throw new ArgumentNullException(nameof(localSyncPath));
            _conflictResolution = SyncConflictResolution.LastWriteWins;
            IsOfflineMode = false;

            // Create local sync path if it doesn't exist
            if (!Directory.Exists(_localSyncPath))
                Directory.CreateDirectory(_localSyncPath);
        }

        public async Task<SyncResult> SyncAsync(CancellationToken ct = default)
        {
            var startTime = DateTime.UtcNow;
            var result = new SyncResult { Success = true };

            try
            {
                _isRunning = true;
                OnSyncProgress(new SyncEventArgs { EventType = SyncEventType.SyncStarted });

                // Check connectivity
                if (!IsOfflineMode && !await _provider.IsConnectedAsync(ct))
                {
                    IsOfflineMode = true;
                    OnSyncProgress(new SyncEventArgs { EventType = SyncEventType.OfflineModeEnabled });
                }

                // Get current local state
                var localState = await _stateStore.GetLocalStateAsync(ct);

                // If offline, only sync local changes when reconnected
                if (IsOfflineMode)
                {
                    result.Message = "Offline mode: Local changes queued for sync";
                    return result;
                }

                // Get remote state
                var remoteState = await GetRemoteStateAsync(ct);

                // Perform sync
                await SyncDownloadAsync(localState, remoteState, result, ct);
                await SyncUploadAsync(localState, remoteState, result, ct);
                await ResolveConflictsAsync(result, ct);

                result.Duration = DateTime.UtcNow - startTime;
                OnSyncProgress(new SyncEventArgs { EventType = SyncEventType.SyncCompleted });
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Sync failed: {ex.Message}";
                OnSyncError(new SyncEventArgs { EventType = SyncEventType.SyncFailed, Exception = ex });
            }
            finally
            {
                _isRunning = false;
            }

            return result;
        }

        private async Task<Dictionary<string, CloudFileMetadata>> GetRemoteStateAsync(CancellationToken ct)
        {
            var remoteState = new Dictionary<string, CloudFileMetadata>();

            try
            {
                var files = await _provider.ListFilesAsync("", ct);
                foreach (var file in files)
                    remoteState[file.Path] = file;
            }
            catch (Exception ex)
            {
                OnSyncError(new SyncEventArgs { EventType = SyncEventType.SyncFailed, Exception = ex });
            }

            return remoteState;
        }

        private async Task SyncDownloadAsync(
            Dictionary<string, CloudFileMetadata> localState,
            Dictionary<string, CloudFileMetadata> remoteState,
            SyncResult result,
            CancellationToken ct)
        {
            foreach (var remoteFile in remoteState.Values)
            {
                try
                {
                    var localPath = Path.Combine(_localSyncPath, remoteFile.Name);
                    
                    if (!localState.ContainsKey(remoteFile.Path))
                    {
                        // New file - download it
                        await _provider.DownloadFileAsync(remoteFile.Path, localPath, ct);
                        await _stateStore.UpdateLocalStateAsync(remoteFile.Path, remoteFile, ct);
                        result.FilesAdded++;
                        result.SyncedFiles.Add(remoteFile.Path);
                    }
                    else if (localState[remoteFile.Path].Modified < remoteFile.Modified)
                    {
                        // Remote is newer - download it
                        await _provider.DownloadFileAsync(remoteFile.Path, localPath, ct);
                        await _stateStore.UpdateLocalStateAsync(remoteFile.Path, remoteFile, ct);
                        result.FilesModified++;
                        result.SyncedFiles.Add(remoteFile.Path);
                    }
                }
                catch (Exception ex)
                {
                    OnSyncError(new SyncEventArgs 
                    { 
                        FilePath = remoteFile.Path,
                        EventType = SyncEventType.SyncFailed, 
                        Exception = ex 
                    });
                }
            }
        }

        private async Task SyncUploadAsync(
            Dictionary<string, CloudFileMetadata> localState,
            Dictionary<string, CloudFileMetadata> remoteState,
            SyncResult result,
            CancellationToken ct)
        {
            if (!Directory.Exists(_localSyncPath))
                return;

            var localFiles = Directory.GetFiles(_localSyncPath, "*", SearchOption.AllDirectories);

            foreach (var localFile in localFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(localFile);
                    var relativePath = Path.GetRelativePath(_localSyncPath, localFile);
                    var remotePath = relativePath.Replace("\\", "/");

                    if (!remoteState.ContainsKey(remotePath))
                    {
                        // New file - upload it
                        await _provider.UploadFileAsync(localFile, remotePath, ct);
                        var metadata = CreateMetadataFromFile(fileInfo, remotePath);
                        await _stateStore.UpdateLocalStateAsync(remotePath, metadata, ct);
                        result.FilesAdded++;
                        result.SyncedFiles.Add(remotePath);
                    }
                    else if (fileInfo.LastWriteTimeUtc > remoteState[remotePath].Modified)
                    {
                        // Local is newer - upload it
                        await _provider.UploadFileAsync(localFile, remotePath, ct);
                        var metadata = CreateMetadataFromFile(fileInfo, remotePath);
                        await _stateStore.UpdateLocalStateAsync(remotePath, metadata, ct);
                        result.FilesModified++;
                        result.SyncedFiles.Add(remotePath);
                    }
                }
                catch (Exception ex)
                {
                    OnSyncError(new SyncEventArgs 
                    { 
                        FilePath = localFile,
                        EventType = SyncEventType.SyncFailed, 
                        Exception = ex 
                    });
                }
            }

            // Delete remote files not present locally
            var localNames = new HashSet<string>(localFiles.Select(f => 
                Path.GetRelativePath(_localSyncPath, f).Replace("\\", "/")));

            foreach (var remoteFile in remoteState.Values.Where(f => !localNames.Contains(f.Path)))
            {
                try
                {
                    await _provider.DeleteFileAsync(remoteFile.Path, ct);
                    await _stateStore.RemoveFromLocalStateAsync(remoteFile.Path, ct);
                    result.FilesDeleted++;
                }
                catch (Exception ex)
                {
                    OnSyncError(new SyncEventArgs 
                    { 
                        FilePath = remoteFile.Path,
                        EventType = SyncEventType.SyncFailed, 
                        Exception = ex 
                    });
                }
            }
        }

        private async Task ResolveConflictsAsync(SyncResult result, CancellationToken ct)
        {
            // Implement conflict resolution based on configured strategy
            // This is a placeholder for the conflict resolution logic
            await Task.CompletedTask;
        }

        private CloudFileMetadata CreateMetadataFromFile(FileInfo fileInfo, string remotePath)
        {
            return new CloudFileMetadata
            {
                Path = remotePath,
                Name = fileInfo.Name,
                Size = fileInfo.Length,
                Modified = fileInfo.LastWriteTimeUtc,
                ETag = fileInfo.Length.ToString(),
                ContentHash = ComputeSimpleHash(fileInfo.FullName)
            };
        }

        private string ComputeSimpleHash(string filePath)
        {
            // Simple hash based on file size and modification time
            var fileInfo = new FileInfo(filePath);
            var hash = (fileInfo.Length.ToString() + fileInfo.LastWriteTimeUtc.Ticks.ToString()).GetHashCode();
            return Math.Abs(hash).ToString("X8");
        }

        public void SetConflictResolution(SyncConflictResolution resolution)
        {
            _conflictResolution = resolution;
        }

        protected virtual void OnSyncProgress(SyncEventArgs e)
        {
            SyncProgress?.Invoke(this, e);
        }

        protected virtual void OnSyncError(SyncEventArgs e)
        {
            SyncError?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Interface for sync engine
    /// </summary>
    public interface ISyncEngine
    {
        bool IsRunning { get; }
        bool IsOfflineMode { get; }
        event EventHandler<SyncEventArgs> SyncProgress;
        event EventHandler<SyncEventArgs> SyncError;
        
        Task<SyncResult> SyncAsync(CancellationToken ct = default);
        void SetConflictResolution(SyncConflictResolution resolution);
    }
}
