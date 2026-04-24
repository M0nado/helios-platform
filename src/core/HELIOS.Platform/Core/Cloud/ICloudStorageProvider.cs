namespace HELIOS.Platform.Core.Cloud;

/// <summary>
/// Cloud storage provider abstraction for multiple cloud services.
/// </summary>
public interface ICloudStorageProvider
{
    /// <summary>Authenticate with the cloud provider.</summary>
    Task<bool> AuthenticateAsync(CloudProviderCredentials credentials);

    /// <summary>Upload file to cloud storage.</summary>
    Task<CloudUploadResult> UploadAsync(string localPath, string remotePath);

    /// <summary>Download file from cloud storage.</summary>
    Task<CloudDownloadResult> DownloadAsync(string remotePath, string localPath);

    /// <summary>List files in remote path.</summary>
    Task<List<CloudFileInfo>> ListFilesAsync(string remotePath);

    /// <summary>Delete file from cloud storage.</summary>
    Task<bool> DeleteAsync(string remotePath);

    /// <summary>Get cloud storage quota and usage.</summary>
    Task<CloudQuotaInfo> GetQuotaAsync();

    /// <summary>Verify provider connection status.</summary>
    Task<bool> VerifyConnectionAsync();

    /// <summary>Get cloud provider type.</summary>
    CloudProviderType ProviderType { get; }
}

/// <summary>Cloud provider types.</summary>
public enum CloudProviderType
{
    Unknown,
    OneDrive,
    AzureBlob,
    AzureFile,
    GoogleDrive,
    Dropbox
}

/// <summary>Cloud provider credentials.</summary>
public class CloudProviderCredentials
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? SubscriptionId { get; set; }
    public string? StorageAccountName { get; set; }
    public string? StorageAccountKey { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>File upload result.</summary>
public class CloudUploadResult
{
    public bool Success { get; set; }
    public string? FileId { get; set; }
    public string? RemotePath { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? ETag { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>File download result.</summary>
public class CloudDownloadResult
{
    public bool Success { get; set; }
    public string? LocalPath { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTime DownloadedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>Cloud file information.</summary>
public class CloudFileInfo
{
    public string? FileId { get; set; }
    public string? Name { get; set; }
    public string? Path { get; set; }
    public long? SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string? ETag { get; set; }
    public bool IsFolder { get; set; }
}

/// <summary>Cloud quota information.</summary>
public class CloudQuotaInfo
{
    public long TotalBytes { get; set; }
    public long UsedBytes { get; set; }
    public long AvailableBytes { get; set; }
    public double UsagePercent => TotalBytes > 0 ? (UsedBytes * 100.0) / TotalBytes : 0;
}
