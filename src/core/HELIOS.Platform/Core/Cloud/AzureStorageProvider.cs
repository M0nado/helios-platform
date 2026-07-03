namespace HELIOS.Platform.Core.Cloud;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Identity;
using HELIOS.Platform.Core.Logging;

/// <summary>
/// Azure Blob Storage provider implementation.
/// </summary>
public class AzureStorageProvider : ICloudStorageProvider
{
    private readonly ILogger _logger;
    private BlobContainerClient? _containerClient;
    private CloudProviderCredentials? _credentials;

    public CloudProviderType ProviderType => CloudProviderType.AzureBlob;

    public AzureStorageProvider(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> AuthenticateAsync(CloudProviderCredentials credentials)
    {
        try
        {
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));

            if (string.IsNullOrEmpty(_credentials.StorageAccountName) ||
                string.IsNullOrEmpty(_credentials.StorageAccountKey))
            {
                _logger.Error("Azure: Storage account name and key are required");
                return false;
            }

            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={_credentials.StorageAccountName};AccountKey={_credentials.StorageAccountKey};EndpointSuffix=core.windows.net";
            var blobServiceClient = new BlobServiceClient(new Uri($"https://{_credentials.StorageAccountName}.blob.core.windows.net"), 
                new StorageSharedKeyCredential(_credentials.StorageAccountName, _credentials.StorageAccountKey));

            _containerClient = blobServiceClient.GetBlobContainerClient("helios-sync");

            await _containerClient.CreateIfNotExistsAsync();
            var isValid = await VerifyConnectionAsync();
            
            if (isValid)
            {
                _logger.Info("Azure Storage: Authentication successful");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.Error($"Azure Storage authentication failed: {ex.Message}");
            return false;
        }
    }

    public async Task<CloudUploadResult> UploadAsync(string localPath, string remotePath)
    {
        var result = new CloudUploadResult { Success = false };

        try
        {
            if (_containerClient == null)
            {
                result.ErrorMessage = "Not authenticated with Azure Storage";
                return result;
            }

            if (!File.Exists(localPath))
            {
                result.ErrorMessage = $"Local file not found: {localPath}";
                return result;
            }

            var fileInfo = new FileInfo(localPath);
            var blobName = NormalizeRemotePath(remotePath);
            var blobClient = _containerClient.GetBlobClient(blobName);

            using var fileStream = File.OpenRead(localPath);
            var uploadedBlob = await blobClient.UploadAsync(fileStream, overwrite: true);

            result.Success = true;
            result.FileId = blobClient.Name;
            result.RemotePath = blobName;
            result.FileSizeBytes = fileInfo.Length;
            result.UploadedAt = DateTime.UtcNow;
            result.ETag = uploadedBlob.Value.ETag.ToString();

            _logger.Info($"Azure Storage: Uploaded {Path.GetFileName(localPath)} ({fileInfo.Length} bytes)");
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.Error($"Azure Storage upload error: {ex.Message}");
            return result;
        }
    }

    public async Task<CloudDownloadResult> DownloadAsync(string remotePath, string localPath)
    {
        var result = new CloudDownloadResult { Success = false };

        try
        {
            if (_containerClient == null)
            {
                result.ErrorMessage = "Not authenticated with Azure Storage";
                return result;
            }

            var blobName = NormalizeRemotePath(remotePath);
            var blobClient = _containerClient.GetBlobClient(blobName);

            var directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var download = await blobClient.DownloadAsync();
            using var fileStream = File.Create(localPath);
            await download.Value.Content.CopyToAsync(fileStream);

            var fileInfo = new FileInfo(localPath);
            result.Success = true;
            result.LocalPath = localPath;
            result.FileSizeBytes = fileInfo.Length;
            result.DownloadedAt = DateTime.UtcNow;

            _logger.Info($"Azure Storage: Downloaded {Path.GetFileName(localPath)} ({fileInfo.Length} bytes)");
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.Error($"Azure Storage download error: {ex.Message}");
            return result;
        }
    }

    public async Task<List<CloudFileInfo>> ListFilesAsync(string remotePath)
    {
        var files = new List<CloudFileInfo>();

        try
        {
            if (_containerClient == null)
            {
                _logger.Warning("Not authenticated with Azure Storage");
                return files;
            }

            var prefix = NormalizeRemotePath(remotePath);
            await foreach (var blob in _containerClient.GetBlobsAsync(BlobTraitsCopy.None, BlobStatesInner.None, prefix))
            {
                var fileInfo = new CloudFileInfo
                {
                    FileId = blob.Name,
                    Name = Path.GetFileName(blob.Name),
                    Path = prefix,
                    SizeBytes = blob.Properties.ContentLength,
                    CreatedAt = blob.Properties.CreatedOn?.UtcDateTime ?? DateTime.UtcNow,
                    ModifiedAt = blob.Properties.LastModified?.UtcDateTime ?? DateTime.UtcNow,
                    ETag = blob.Properties.ETag.ToString(),
                    IsFolder = false
                };
                files.Add(fileInfo);
            }

            _logger.Info($"Azure Storage: Listed {files.Count} items in {remotePath}");
            return files;
        }
        catch (Exception ex)
        {
            _logger.Error($"Azure Storage list error: {ex.Message}");
            return files;
        }
    }

    public async Task<bool> DeleteAsync(string remotePath)
    {
        try
        {
            if (_containerClient == null)
            {
                _logger.Warning("Not authenticated with Azure Storage");
                return false;
            }

            var blobName = NormalizeRemotePath(remotePath);
            var blobClient = _containerClient.GetBlobClient(blobName);
            var deleted = await blobClient.DeleteIfExistsAsync();

            if (deleted)
            {
                _logger.Info($"Azure Storage: Deleted {remotePath}");
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.Error($"Azure Storage delete error: {ex.Message}");
            return false;
        }
    }

    public async Task<CloudQuotaInfo> GetQuotaAsync()
    {
        var quota = new CloudQuotaInfo();

        try
        {
            if (_containerClient == null)
            {
                return quota;
            }

            var properties = await _containerClient.GetPropertiesAsync();
            var blobs = _containerClient.GetBlobs();
            long totalSize = 0;
            int blobCount = 0;

            await Task.Run(() =>
            {
                foreach (var blob in blobs)
                {
                    totalSize += blob.Properties.ContentLength ?? 0;
                    blobCount++;
                }
            });

            // Azure doesn't expose quota like OneDrive, so we estimate
            const long defaultQuota = 1099511627776; // 1TB default
            quota.TotalBytes = defaultQuota;
            quota.UsedBytes = totalSize;
            quota.AvailableBytes = defaultQuota - totalSize;

            _logger.Info($"Azure Storage: Quota - Used: {quota.UsedBytes} bytes, Available: {quota.AvailableBytes} bytes");
            return quota;
        }
        catch (Exception ex)
        {
            _logger.Error($"Azure Storage quota error: {ex.Message}");
            return quota;
        }
    }

    public async Task<bool> VerifyConnectionAsync()
    {
        try
        {
            if (_containerClient == null)
                return false;

            await _containerClient.GetPropertiesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Azure Storage connection verification error: {ex.Message}");
            return false;
        }
    }

    private static string NormalizeRemotePath(string remotePath)
    {
        if (string.IsNullOrEmpty(remotePath) || remotePath == "/")
            return string.Empty;

        remotePath = remotePath.Replace('\\', '/');
        if (remotePath.StartsWith('/'))
            remotePath = remotePath[1..];
        if (remotePath.EndsWith('/'))
            remotePath = remotePath[..^1];

        return remotePath;
    }
}
