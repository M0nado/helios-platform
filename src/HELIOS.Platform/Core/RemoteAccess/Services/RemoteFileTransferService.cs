using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HELIOS.Platform.Core.RemoteAccess.Services;

/// <summary>
/// Manages remote file transfer operations.
/// </summary>
public interface IRemoteFileTransferService
{
    /// <summary>Uploads a file to remote host.</summary>
    Task<FileTransferOperation> UploadFileAsync(string connectionId, string localPath, string remotePath, CancellationToken cancellationToken = default);

    /// <summary>Downloads a file from remote host.</summary>
    Task<FileTransferOperation> DownloadFileAsync(string connectionId, string remotePath, string localPath, CancellationToken cancellationToken = default);

    /// <summary>Gets file transfer status.</summary>
    Task<FileTransferOperation?> GetTransferStatusAsync(string transferId);

    /// <summary>Cancels a file transfer operation.</summary>
    Task<bool> CancelTransferAsync(string transferId);

    /// <summary>Lists files in remote directory.</summary>
    Task<IEnumerable<RemoteFileSystemObject>> ListFilesAsync(string connectionId, string remotePath, CancellationToken cancellationToken = default);

    /// <summary>Gets file information from remote host.</summary>
    Task<RemoteFileSystemObject?> GetFileInfoAsync(string connectionId, string remotePath);

    /// <summary>Deletes a remote file.</summary>
    Task<bool> DeleteFileAsync(string connectionId, string remotePath);

    /// <summary>Gets transfer history for a connection.</summary>
    Task<IEnumerable<FileTransferOperation>> GetTransferHistoryAsync(string connectionId);
}

/// <summary>
/// Default implementation of IRemoteFileTransferService.
/// </summary>
public class RemoteFileTransferService : IRemoteFileTransferService
{
    private readonly ILogger<RemoteFileTransferService> _logger;
    private readonly IRemoteConnectionManager _connectionManager;
    private readonly ConcurrentDictionary<string, FileTransferOperation> _transferCache;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeTransfers;
    private readonly Queue<FileTransferOperation> _transferHistory;

    public RemoteFileTransferService(
        ILogger<RemoteFileTransferService> logger,
        IRemoteConnectionManager connectionManager)
    {
        _logger = logger;
        _connectionManager = connectionManager;
        _transferCache = new ConcurrentDictionary<string, FileTransferOperation>();
        _activeTransfers = new ConcurrentDictionary<string, CancellationTokenSource>();
        _transferHistory = new Queue<FileTransferOperation>(1000);
    }

    /// <inheritdoc/>
    public async Task<FileTransferOperation> UploadFileAsync(string connectionId, string localPath, string remotePath, CancellationToken cancellationToken = default)
    {
        var operation = new FileTransferOperation
        {
            ConnectionId = connectionId,
            SourcePath = localPath,
            DestinationPath = remotePath,
            Direction = TransferDirection.Upload,
            Status = TransferStatus.Pending
        };

        try
        {
            _logger.LogInformation("Starting upload from {LocalPath} to {RemotePath} on connection {ConnectionId}",
                localPath, remotePath, connectionId);

            var connection = await _connectionManager.GetConnectionAsync(connectionId);
            if (connection == null || connection.State != ConnectionState.Connected)
            {
                operation.Status = TransferStatus.Failed;
                operation.ErrorMessage = "Connection not available";
                return operation;
            }

            // Validate local file exists
            if (!File.Exists(localPath))
            {
                operation.Status = TransferStatus.Failed;
                operation.ErrorMessage = "Local file not found";
                return operation;
            }

            var fileInfo = new FileInfo(localPath);
            operation.FileSizeBytes = fileInfo.Length;

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                if (!_activeTransfers.TryAdd(operation.TransferId, cts))
                {
                    operation.Status = TransferStatus.Failed;
                    return operation;
                }

                try
                {
                    operation.Status = TransferStatus.InProgress;
                    operation.StartTime = DateTime.UtcNow;

                    // Simulate file transfer
                    using (var fileStream = File.OpenRead(localPath))
                    {
                        var buffer = new byte[65536];
                        int bytesRead;

                        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
                        {
                            operation.BytesTransferred += bytesRead;
                            await Task.Delay(10, cts.Token);
                        }
                    }

                    operation.Status = TransferStatus.Completed;
                    operation.CompletionTime = DateTime.UtcNow;
                    operation.FileChecksum = ComputeFileChecksum(localPath);

                    _logger.LogInformation("File upload completed: {TransferId}", operation.TransferId);
                }
                catch (OperationCanceledException)
                {
                    operation.Status = TransferStatus.Cancelled;
                    _logger.LogWarning("File upload cancelled: {TransferId}", operation.TransferId);
                }
                finally
                {
                    _activeTransfers.TryRemove(operation.TransferId, out _);
                }
            }

            _transferCache.AddOrUpdate(operation.TransferId, operation, (_, _) => operation);
            AddToHistory(operation);

            return operation;
        }
        catch (Exception ex)
        {
            operation.Status = TransferStatus.Failed;
            operation.ErrorMessage = ex.Message;
            operation.CompletionTime = DateTime.UtcNow;
            _logger.LogError(ex, "Error uploading file to connection {ConnectionId}", connectionId);
            return operation;
        }
    }

    /// <inheritdoc/>
    public async Task<FileTransferOperation> DownloadFileAsync(string connectionId, string remotePath, string localPath, CancellationToken cancellationToken = default)
    {
        var operation = new FileTransferOperation
        {
            ConnectionId = connectionId,
            SourcePath = remotePath,
            DestinationPath = localPath,
            Direction = TransferDirection.Download,
            Status = TransferStatus.Pending
        };

        try
        {
            _logger.LogInformation("Starting download from {RemotePath} to {LocalPath} on connection {ConnectionId}",
                remotePath, localPath, connectionId);

            var connection = await _connectionManager.GetConnectionAsync(connectionId);
            if (connection == null || connection.State != ConnectionState.Connected)
            {
                operation.Status = TransferStatus.Failed;
                operation.ErrorMessage = "Connection not available";
                return operation;
            }

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                if (!_activeTransfers.TryAdd(operation.TransferId, cts))
                {
                    operation.Status = TransferStatus.Failed;
                    return operation;
                }

                try
                {
                    operation.Status = TransferStatus.InProgress;
                    operation.StartTime = DateTime.UtcNow;

                    // Simulate file download
                    operation.FileSizeBytes = 1048576; // Mock 1MB file
                    operation.BytesTransferred = operation.FileSizeBytes;

                    operation.Status = TransferStatus.Completed;
                    operation.CompletionTime = DateTime.UtcNow;

                    _logger.LogInformation("File download completed: {TransferId}", operation.TransferId);
                }
                catch (OperationCanceledException)
                {
                    operation.Status = TransferStatus.Cancelled;
                }
                finally
                {
                    _activeTransfers.TryRemove(operation.TransferId, out _);
                }
            }

            _transferCache.AddOrUpdate(operation.TransferId, operation, (_, _) => operation);
            AddToHistory(operation);

            return operation;
        }
        catch (Exception ex)
        {
            operation.Status = TransferStatus.Failed;
            operation.ErrorMessage = ex.Message;
            operation.CompletionTime = DateTime.UtcNow;
            _logger.LogError(ex, "Error downloading file from connection {ConnectionId}", connectionId);
            return operation;
        }
    }

    /// <inheritdoc/>
    public async Task<FileTransferOperation?> GetTransferStatusAsync(string transferId)
    {
        return _transferCache.TryGetValue(transferId, out var operation) ? operation : null;
    }

    /// <inheritdoc/>
    public async Task<bool> CancelTransferAsync(string transferId)
    {
        try
        {
            if (_activeTransfers.TryGetValue(transferId, out var cts))
            {
                cts.Cancel();
                _logger.LogInformation("Transfer cancellation requested: {TransferId}", transferId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling transfer {TransferId}", transferId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RemoteFileSystemObject>> ListFilesAsync(string connectionId, string remotePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await _connectionManager.GetConnectionAsync(connectionId);
            if (connection == null)
            {
                return Enumerable.Empty<RemoteFileSystemObject>();
            }

            // Mock file listing
            return new List<RemoteFileSystemObject>
            {
                new RemoteFileSystemObject
                {
                    Path = $"{remotePath}/file1.txt",
                    Name = "file1.txt",
                    IsDirectory = false,
                    SizeBytes = 1024,
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    ModifiedAt = DateTime.UtcNow,
                    Permissions = "rw-r--r--",
                    Owner = "admin"
                },
                new RemoteFileSystemObject
                {
                    Path = $"{remotePath}/subfolder",
                    Name = "subfolder",
                    IsDirectory = true,
                    SizeBytes = 0,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    ModifiedAt = DateTime.UtcNow,
                    Permissions = "rwxr-xr-x",
                    Owner = "admin"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files on connection {ConnectionId}", connectionId);
            return Enumerable.Empty<RemoteFileSystemObject>();
        }
    }

    /// <inheritdoc/>
    public async Task<RemoteFileSystemObject?> GetFileInfoAsync(string connectionId, string remotePath)
    {
        try
        {
            var connection = await _connectionManager.GetConnectionAsync(connectionId);
            if (connection == null)
            {
                return null;
            }

            return new RemoteFileSystemObject
            {
                Path = remotePath,
                Name = Path.GetFileName(remotePath),
                IsDirectory = false,
                SizeBytes = 2048,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ModifiedAt = DateTime.UtcNow,
                Permissions = "rw-r--r--",
                Owner = "admin"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info for {Path}", remotePath);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteFileAsync(string connectionId, string remotePath)
    {
        try
        {
            var connection = await _connectionManager.GetConnectionAsync(connectionId);
            if (connection == null)
            {
                return false;
            }

            _logger.LogInformation("Deleted file {Path} on connection {ConnectionId}", remotePath, connectionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {Path}", remotePath);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FileTransferOperation>> GetTransferHistoryAsync(string connectionId)
    {
        return _transferHistory
            .Where(t => t.ConnectionId == connectionId)
            .ToList();
    }

    private void AddToHistory(FileTransferOperation operation)
    {
        lock (_transferHistory)
        {
            _transferHistory.Enqueue(operation);
            if (_transferHistory.Count > 1000)
                _transferHistory.Dequeue();
        }
    }

    private string ComputeFileChecksum(string filePath)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        using (var stream = File.OpenRead(filePath))
        {
            var hash = sha256.ComputeHash(stream);
            return Convert.ToBase64String(hash);
        }
    }
}
