using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.CloudSync
{
    /// <summary>
    /// Abstraction for cloud storage providers (OneDrive, Azure, etc.)
    /// </summary>
    public interface ICloudStorageProvider
    {
        string ProviderName { get; }
        bool IsAuthenticated { get; }
        
        Task<bool> AuthenticateAsync(CancellationToken ct = default);
        Task<bool> IsConnectedAsync(CancellationToken ct = default);
        Task UploadFileAsync(string localPath, string remotePath, CancellationToken ct = default);
        Task DownloadFileAsync(string remotePath, string localPath, CancellationToken ct = default);
        Task<bool> FileExistsAsync(string remotePath, CancellationToken ct = default);
        Task DeleteFileAsync(string remotePath, CancellationToken ct = default);
        Task<CloudFileMetadata> GetFileMetadataAsync(string remotePath, CancellationToken ct = default);
        Task<List<CloudFileMetadata>> ListFilesAsync(string remotePath, CancellationToken ct = default);
    }

    /// <summary>
    /// Metadata for cloud files
    /// </summary>
    public class CloudFileMetadata
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime Modified { get; set; }
        public string ETag { get; set; }
        public string ContentHash { get; set; }
    }

    /// <summary>
    /// Sync state for conflict resolution
    /// </summary>
    public enum SyncConflictResolution
    {
        LastWriteWins,
        LocalWins,
        CloudWins,
        Manual
    }

    /// <summary>
    /// Sync operation result
    /// </summary>
    public class SyncResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> ConflictedFiles { get; set; } = new();
        public List<string> SyncedFiles { get; set; } = new();
        public int FilesAdded { get; set; }
        public int FilesModified { get; set; }
        public int FilesDeleted { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Sync state tracking for local changes
    /// </summary>
    public interface ISyncStateStore
    {
        Task<Dictionary<string, CloudFileMetadata>> GetLocalStateAsync(CancellationToken ct = default);
        Task UpdateLocalStateAsync(string path, CloudFileMetadata metadata, CancellationToken ct = default);
        Task RemoveFromLocalStateAsync(string path, CancellationToken ct = default);
        Task ClearLocalStateAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Event args for sync events
    /// </summary>
    public class SyncEventArgs : EventArgs
    {
        public string FilePath { get; set; }
        public SyncEventType EventType { get; set; }
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// Types of sync events
    /// </summary>
    public enum SyncEventType
    {
        SyncStarted,
        FileUploaded,
        FileDownloaded,
        FileConflict,
        SyncCompleted,
        SyncFailed,
        OfflineModeEnabled,
        OnlineModeEnabled
    }
}
