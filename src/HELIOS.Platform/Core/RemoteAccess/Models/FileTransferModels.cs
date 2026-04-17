namespace HELIOS.Platform.Core.RemoteAccess.Models;

/// <summary>
/// Represents a file transfer operation.
/// </summary>
public class FileTransferOperation
{
    /// <summary>Gets or sets the unique transfer ID.</summary>
    public string TransferId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the connection ID.</summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>Gets or sets the source file path.</summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination file path.</summary>
    public string DestinationPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the file size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Gets or sets the transfer direction.</summary>
    public TransferDirection Direction { get; set; } = TransferDirection.Upload;

    /// <summary>Gets or sets the transfer status.</summary>
    public TransferStatus Status { get; set; } = TransferStatus.Pending;

    /// <summary>Gets or sets bytes transferred.</summary>
    public long BytesTransferred { get; set; }

    /// <summary>Gets or sets the progress percentage.</summary>
    public double ProgressPercent => FileSizeBytes > 0 ? (BytesTransferred * 100.0) / FileSizeBytes : 0;

    /// <summary>Gets or sets the transfer start time.</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>Gets or sets the transfer completion time.</summary>
    public DateTime? CompletionTime { get; set; }

    /// <summary>Gets or sets file checksum for integrity verification.</summary>
    public string? FileChecksum { get; set; }

    /// <summary>Gets or sets whether to verify checksum after transfer.</summary>
    public bool VerifyChecksum { get; set; } = true;

    /// <summary>Gets or sets error message if transfer failed.</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Enumeration of transfer directions.
/// </summary>
public enum TransferDirection
{
    /// <summary>Upload to remote</summary>
    Upload,

    /// <summary>Download from remote</summary>
    Download
}

/// <summary>
/// Enumeration of transfer statuses.
/// </summary>
public enum TransferStatus
{
    /// <summary>Transfer pending</summary>
    Pending,

    /// <summary>Transfer in progress</summary>
    InProgress,

    /// <summary>Transfer completed successfully</summary>
    Completed,

    /// <summary>Transfer failed</summary>
    Failed,

    /// <summary>Transfer cancelled</summary>
    Cancelled,

    /// <summary>Transfer paused</summary>
    Paused
}

/// <summary>
/// Represents a remote file system object.
/// </summary>
public class RemoteFileSystemObject
{
    /// <summary>Gets or sets the object path.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Gets or sets the object name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this is a directory.</summary>
    public bool IsDirectory { get; set; }

    /// <summary>Gets or sets the file size in bytes.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the modification timestamp.</summary>
    public DateTime ModifiedAt { get; set; }

    /// <summary>Gets or sets the file permissions.</summary>
    public string Permissions { get; set; } = string.Empty;

    /// <summary>Gets or sets the owner information.</summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>Gets or sets the group information.</summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>Gets or sets file metadata.</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
