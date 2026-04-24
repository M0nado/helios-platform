namespace HELIOS.Platform.Core.Cloud;

using System.Collections.Concurrent;
using System.Data.SQLite;

/// <summary>
/// Synchronization engine for cloud storage operations.
/// Manages local state tracking and sync coordination.
/// </summary>
public interface ISyncEngine
{
    /// <summary>Initialize sync engine with cloud provider.</summary>
    Task<bool> InitializeAsync(ICloudStorageProvider provider);

    /// <summary>Synchronize local path to cloud.</summary>
    Task<SyncResult> PushAsync(string localPath, string remotePath);

    /// <summary>Synchronize cloud path to local.</summary>
    Task<SyncResult> PullAsync(string remotePath, string localPath);

    /// <summary>Bidirectional synchronization with conflict resolution.</summary>
    Task<SyncResult> SyncAsync(string localPath, string remotePath, ConflictResolutionStrategy strategy);

    /// <summary>Get sync state for file.</summary>
    Task<FileSyncState?> GetSyncStateAsync(string filePath);

    /// <summary>Get all pending sync operations.</summary>
    Task<List<SyncOperation>> GetPendingOperationsAsync();

    /// <summary>Retry failed sync operations.</summary>
    Task<SyncResult> RetryFailedAsync();

    /// <summary>Clear sync state.</summary>
    Task<bool> ClearStateAsync();
}

/// <summary>Conflict resolution strategies.</summary>
public enum ConflictResolutionStrategy
{
    /// <summary>Keep the version with the latest modification time.</summary>
    LastWriteWins,
    /// <summary>Keep local version.</summary>
    KeepLocal,
    /// <summary>Keep remote version.</summary>
    KeepRemote,
    /// <summary>Create both versions with timestamp suffix.</summary>
    CreateBoth,
    /// <summary>Skip conflict resolution, report error.</summary>
    Abort
}

/// <summary>Sync result details.</summary>
public class SyncResult
{
    public bool Success { get; set; }
    public int FilesProcessed { get; set; }
    public int FilesSynced { get; set; }
    public int ConflictCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<ConflictInfo> Conflicts { get; set; } = [];
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
}

/// <summary>File sync state in database.</summary>
public class FileSyncState
{
    public string? FilePath { get; set; }
    public DateTime LocalModifiedAt { get; set; }
    public DateTime RemoteModifiedAt { get; set; }
    public string? LocalHash { get; set; }
    public string? RemoteHash { get; set; }
    public string? RemoteFileId { get; set; }
    public SyncStatus Status { get; set; }
    public DateTime LastSyncAt { get; set; }
}

/// <summary>Sync status enumeration.</summary>
public enum SyncStatus
{
    Synced,
    Pending,
    Conflict,
    Failed,
    Deleted
}

/// <summary>Pending sync operation.</summary>
public class SyncOperation
{
    public string? FileId { get; set; }
    public string? FilePath { get; set; }
    public SyncOperationType Type { get; set; }
    public SyncStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
}

/// <summary>Sync operation types.</summary>
public enum SyncOperationType
{
    Upload,
    Download,
    Delete,
    Merge
}

/// <summary>Conflict information.</summary>
public class ConflictInfo
{
    public string? FilePath { get; set; }
    public DateTime LocalModified { get; set; }
    public DateTime RemoteModified { get; set; }
    public long? LocalSize { get; set; }
    public long? RemoteSize { get; set; }
}

/// <summary>
/// Default implementation of sync engine.
/// </summary>
public class SyncEngine : ISyncEngine
{
    private readonly ILogger _logger;
    private ICloudStorageProvider? _provider;
    private readonly string _stateDatabasePath;
    private readonly ConcurrentDictionary<string, FileSyncState> _stateCache;
    private const string StateDbFilename = "helios-sync-state.db";
    private const int MaxRetries = 3;

    public SyncEngine(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stateDatabasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HELIOS",
            "Cloud",
            StateDbFilename);
        _stateCache = new ConcurrentDictionary<string, FileSyncState>();
    }

    public async Task<bool> InitializeAsync(ICloudStorageProvider provider)
    {
        try
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));

            // Create state database if not exists
            var directory = Path.GetDirectoryName(_stateDatabasePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            InitializeDatabase();
            await LoadStateCache();

            _logger.Info("SyncEngine: Initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"SyncEngine initialization failed: {ex.Message}");
            return false;
        }
    }

    public async Task<SyncResult> PushAsync(string localPath, string remotePath)
    {
        var result = new SyncResult { StartTime = DateTime.UtcNow };

        try
        {
            if (_provider == null)
            {
                result.ErrorMessage = "Provider not initialized";
                result.Success = false;
                return result;
            }

            if (!Directory.Exists(localPath) && !File.Exists(localPath))
            {
                result.Errors.Add($"Path not found: {localPath}");
                result.Success = false;
                return result;
            }

            var files = GetFilesToSync(localPath);
            result.FilesProcessed = files.Count;

            foreach (var file in files)
            {
                try
                {
                    var relativePath = Path.GetRelativePath(localPath, file);
                    var remoteFilePath = Path.Combine(remotePath, relativePath).Replace('\\', '/');

                    var uploadResult = await _provider.UploadAsync(file, remoteFilePath);
                    if (uploadResult.Success)
                    {
                        await UpdateSyncStateAsync(file, uploadResult, SyncStatus.Synced);
                        result.FilesSynced++;
                    }
                    else
                    {
                        result.Errors.Add($"Upload failed: {file} - {uploadResult.ErrorMessage}");
                        result.ErrorCount++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Push error for {file}: {ex.Message}");
                    result.ErrorCount++;
                }
            }

            result.Success = result.ErrorCount == 0;
            result.EndTime = DateTime.UtcNow;
            _logger.Info($"SyncEngine: Push completed - {result.FilesSynced}/{result.FilesProcessed} files synced");

            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
            result.Success = false;
            result.EndTime = DateTime.UtcNow;
            _logger.Error($"SyncEngine push error: {ex.Message}");
            return result;
        }
    }

    public async Task<SyncResult> PullAsync(string remotePath, string localPath)
    {
        var result = new SyncResult { StartTime = DateTime.UtcNow };

        try
        {
            if (_provider == null)
            {
                result.Errors.Add("Provider not initialized");
                result.Success = false;
                return result;
            }

            var remoteFiles = await _provider.ListFilesAsync(remotePath);
            result.FilesProcessed = remoteFiles.Count;

            foreach (var remoteFile in remoteFiles)
            {
                if (remoteFile.IsFolder) continue;

                try
                {
                    var localFilePath = Path.Combine(localPath, remoteFile.Name ?? "");
                    var downloadResult = await _provider.DownloadAsync(
                        Path.Combine(remotePath, remoteFile.Name ?? "").Replace('\\', '/'),
                        localFilePath);

                    if (downloadResult.Success)
                    {
                        await UpdateSyncStateAsync(localFilePath, remoteFile, SyncStatus.Synced);
                        result.FilesSynced++;
                    }
                    else
                    {
                        result.Errors.Add($"Download failed: {remoteFile.Name} - {downloadResult.ErrorMessage}");
                        result.ErrorCount++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Pull error for {remoteFile.Name}: {ex.Message}");
                    result.ErrorCount++;
                }
            }

            result.Success = result.ErrorCount == 0;
            result.EndTime = DateTime.UtcNow;
            _logger.Info($"SyncEngine: Pull completed - {result.FilesSynced}/{result.FilesProcessed} files synced");

            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
            result.Success = false;
            result.EndTime = DateTime.UtcNow;
            _logger.Error($"SyncEngine pull error: {ex.Message}");
            return result;
        }
    }

    public async Task<SyncResult> SyncAsync(string localPath, string remotePath, ConflictResolutionStrategy strategy)
    {
        var result = new SyncResult { StartTime = DateTime.UtcNow };

        try
        {
            if (_provider == null)
            {
                result.Errors.Add("Provider not initialized");
                result.Success = false;
                return result;
            }

            // Get local and remote files
            var localFiles = GetFilesToSync(localPath);
            var remoteFiles = await _provider.ListFilesAsync(remotePath);

            result.FilesProcessed = localFiles.Count + remoteFiles.Count;

            // Check for conflicts
            foreach (var localFile in localFiles)
            {
                var fileName = Path.GetFileName(localFile);
                var remoteFile = remoteFiles.FirstOrDefault(f => f.Name == fileName);

                if (remoteFile != null && remoteFile.ModifiedAt != new FileInfo(localFile).LastWriteTimeUtc)
                {
                    var conflict = new ConflictInfo
                    {
                        FilePath = localFile,
                        LocalModified = new FileInfo(localFile).LastWriteTimeUtc,
                        RemoteModified = remoteFile.ModifiedAt,
                        LocalSize = new FileInfo(localFile).Length,
                        RemoteSize = remoteFile.SizeBytes
                    };
                    result.Conflicts.Add(conflict);
                    result.ConflictCount++;

                    if (strategy == ConflictResolutionStrategy.LastWriteWins)
                    {
                        if (remoteFile.ModifiedAt > new FileInfo(localFile).LastWriteTimeUtc)
                        {
                            // Pull from remote
                            var downloadResult = await _provider.DownloadAsync(
                                Path.Combine(remotePath, fileName).Replace('\\', '/'),
                                localFile);
                            if (downloadResult.Success) result.FilesSynced++;
                        }
                        else
                        {
                            // Push to remote
                            var uploadResult = await _provider.UploadAsync(
                                localFile,
                                Path.Combine(remotePath, fileName).Replace('\\', '/'));
                            if (uploadResult.Success) result.FilesSynced++;
                        }
                    }
                }
            }

            result.Success = result.ConflictCount == 0;
            result.EndTime = DateTime.UtcNow;
            _logger.Info($"SyncEngine: Sync completed - {result.ConflictCount} conflicts, {result.FilesSynced} files synced");

            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
            result.Success = false;
            result.EndTime = DateTime.UtcNow;
            _logger.Error($"SyncEngine sync error: {ex.Message}");
            return result;
        }
    }

    public async Task<FileSyncState?> GetSyncStateAsync(string filePath)
    {
        if (_stateCache.TryGetValue(filePath, out var state))
        {
            return state;
        }

        try
        {
            using var connection = new SQLiteConnection($"Data Source={_stateDatabasePath};");
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT FilePath, LocalModifiedAt, RemoteModifiedAt, LocalHash, RemoteHash, 
                       RemoteFileId, Status, LastSyncAt
                FROM FileSyncStates
                WHERE FilePath = @filePath";
            command.Parameters.AddWithValue("@filePath", filePath);

            using var reader = await command.ExecuteReaderAsync();
            if (reader.Read())
            {
                var state = new FileSyncState
                {
                    FilePath = reader["FilePath"].ToString(),
                    LocalModifiedAt = DateTime.Parse(reader["LocalModifiedAt"].ToString() ?? DateTime.UtcNow.ToString()),
                    RemoteModifiedAt = DateTime.Parse(reader["RemoteModifiedAt"].ToString() ?? DateTime.UtcNow.ToString()),
                    LocalHash = reader["LocalHash"].ToString(),
                    RemoteHash = reader["RemoteHash"].ToString(),
                    RemoteFileId = reader["RemoteFileId"].ToString(),
                    Status = Enum.Parse<SyncStatus>(reader["Status"].ToString() ?? "Pending"),
                    LastSyncAt = DateTime.Parse(reader["LastSyncAt"].ToString() ?? DateTime.UtcNow.ToString())
                };

                _stateCache[filePath] = state;
                return state;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting sync state for {filePath}: {ex.Message}");
            return null;
        }
    }

    public async Task<List<SyncOperation>> GetPendingOperationsAsync()
    {
        var operations = new List<SyncOperation>();

        try
        {
            using var connection = new SQLiteConnection($"Data Source={_stateDatabasePath};");
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT FileId, FilePath, Type, Status, CreatedAt, RetryCount
                FROM SyncOperations
                WHERE Status IN ('Pending', 'Failed')
                ORDER BY CreatedAt ASC";

            using var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                var op = new SyncOperation
                {
                    FileId = reader["FileId"].ToString(),
                    FilePath = reader["FilePath"].ToString(),
                    Type = Enum.Parse<SyncOperationType>(reader["Type"].ToString() ?? "Upload"),
                    Status = Enum.Parse<SyncStatus>(reader["Status"].ToString() ?? "Pending"),
                    CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString() ?? DateTime.UtcNow.ToString()),
                    RetryCount = int.Parse(reader["RetryCount"].ToString() ?? "0")
                };
                operations.Add(op);
            }

            return operations;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting pending operations: {ex.Message}");
            return operations;
        }
    }

    public async Task<SyncResult> RetryFailedAsync()
    {
        var result = new SyncResult { StartTime = DateTime.UtcNow };

        try
        {
            var failedOps = await GetPendingOperationsAsync();
            failedOps = failedOps.Where(op => op.Status == SyncStatus.Failed && op.RetryCount < MaxRetries).ToList();

            foreach (var op in failedOps)
            {
                // Retry logic here
                _logger.Info($"Retrying sync operation: {op.FileId}");
            }

            result.Success = true;
            result.EndTime = DateTime.UtcNow;
            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
            result.Success = false;
            result.EndTime = DateTime.UtcNow;
            _logger.Error($"Error retrying failed operations: {ex.Message}");
            return result;
        }
    }

    public async Task<bool> ClearStateAsync()
    {
        try
        {
            _stateCache.Clear();

            using var connection = new SQLiteConnection($"Data Source={_stateDatabasePath};");
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM FileSyncStates; DELETE FROM SyncOperations;";
            await command.ExecuteNonQueryAsync();

            _logger.Info("SyncEngine: State cleared");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error clearing sync state: {ex.Message}");
            return false;
        }
    }

    private void InitializeDatabase()
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_stateDatabasePath};");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS FileSyncStates (
                    FilePath TEXT PRIMARY KEY,
                    LocalModifiedAt TEXT NOT NULL,
                    RemoteModifiedAt TEXT NOT NULL,
                    LocalHash TEXT,
                    RemoteHash TEXT,
                    RemoteFileId TEXT,
                    Status TEXT NOT NULL,
                    LastSyncAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS SyncOperations (
                    FileId TEXT PRIMARY KEY,
                    FilePath TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    RetryCount INTEGER DEFAULT 0
                );

                CREATE INDEX IF NOT EXISTS idx_status ON FileSyncStates(Status);
                CREATE INDEX IF NOT EXISTS idx_op_status ON SyncOperations(Status);";

            command.ExecuteNonQuery();
            _logger.Info("SyncEngine: Database initialized");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error initializing database: {ex.Message}");
        }
    }

    private async Task LoadStateCache()
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_stateDatabasePath};");
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT FilePath, LocalModifiedAt, RemoteModifiedAt, LocalHash, RemoteHash, RemoteFileId, Status, LastSyncAt FROM FileSyncStates;";

            using var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                var state = new FileSyncState
                {
                    FilePath = reader["FilePath"].ToString() ?? "",
                    LocalModifiedAt = DateTime.Parse(reader["LocalModifiedAt"].ToString() ?? DateTime.UtcNow.ToString()),
                    RemoteModifiedAt = DateTime.Parse(reader["RemoteModifiedAt"].ToString() ?? DateTime.UtcNow.ToString()),
                    LocalHash = reader["LocalHash"].ToString(),
                    RemoteHash = reader["RemoteHash"].ToString(),
                    RemoteFileId = reader["RemoteFileId"].ToString(),
                    Status = Enum.Parse<SyncStatus>(reader["Status"].ToString() ?? "Pending"),
                    LastSyncAt = DateTime.Parse(reader["LastSyncAt"].ToString() ?? DateTime.UtcNow.ToString())
                };
                _stateCache[state.FilePath] = state;
            }

            _logger.Info($"SyncEngine: Loaded {_stateCache.Count} sync states from database");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error loading state cache: {ex.Message}");
        }
    }

    private async Task UpdateSyncStateAsync(string filePath, CloudUploadResult uploadResult, SyncStatus status)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var state = new FileSyncState
            {
                FilePath = filePath,
                LocalModifiedAt = fileInfo.LastWriteTimeUtc,
                RemoteModifiedAt = uploadResult.UploadedAt,
                RemoteFileId = uploadResult.FileId,
                Status = status,
                LastSyncAt = DateTime.UtcNow
            };

            _stateCache[filePath] = state;

            // Save to database
            using var connection = new SQLiteConnection($"Data Source={_stateDatabasePath};");
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO FileSyncStates 
                (FilePath, LocalModifiedAt, RemoteModifiedAt, RemoteFileId, Status, LastSyncAt)
                VALUES (@filePath, @localModified, @remoteModified, @remoteFileId, @status, @lastSync)";

            command.Parameters.AddWithValue("@filePath", filePath);
            command.Parameters.AddWithValue("@localModified", state.LocalModifiedAt.ToString("O"));
            command.Parameters.AddWithValue("@remoteModified", state.RemoteModifiedAt.ToString("O"));
            command.Parameters.AddWithValue("@remoteFileId", uploadResult.FileId ?? "");
            command.Parameters.AddWithValue("@status", status.ToString());
            command.Parameters.AddWithValue("@lastSync", state.LastSyncAt.ToString("O"));

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error updating sync state for {filePath}: {ex.Message}");
        }
    }

    private async Task UpdateSyncStateAsync(string filePath, CloudFileInfo remoteFile, SyncStatus status)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var state = new FileSyncState
            {
                FilePath = filePath,
                LocalModifiedAt = fileInfo.LastWriteTimeUtc,
                RemoteModifiedAt = remoteFile.ModifiedAt,
                RemoteFileId = remoteFile.FileId,
                Status = status,
                LastSyncAt = DateTime.UtcNow
            };

            _stateCache[filePath] = state;

            // Save to database
            using var connection = new SQLiteConnection($"Data Source={_stateDatabasePath};");
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO FileSyncStates 
                (FilePath, LocalModifiedAt, RemoteModifiedAt, RemoteFileId, Status, LastSyncAt)
                VALUES (@filePath, @localModified, @remoteModified, @remoteFileId, @status, @lastSync)";

            command.Parameters.AddWithValue("@filePath", filePath);
            command.Parameters.AddWithValue("@localModified", state.LocalModifiedAt.ToString("O"));
            command.Parameters.AddWithValue("@remoteModified", state.RemoteModifiedAt.ToString("O"));
            command.Parameters.AddWithValue("@remoteFileId", remoteFile.FileId ?? "");
            command.Parameters.AddWithValue("@status", status.ToString());
            command.Parameters.AddWithValue("@lastSync", state.LastSyncAt.ToString("O"));

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error updating sync state for {filePath}: {ex.Message}");
        }
    }

    private static List<string> GetFilesToSync(string path)
    {
        var files = new List<string>();

        if (File.Exists(path))
        {
            files.Add(path);
        }
        else if (Directory.Exists(path))
        {
            files.AddRange(Directory.GetFiles(path, "*", SearchOption.AllDirectories));
        }

        return files;
    }
}
