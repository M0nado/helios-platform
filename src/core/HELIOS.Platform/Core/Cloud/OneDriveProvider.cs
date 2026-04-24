namespace HELIOS.Platform.Core.Cloud;

using System.Net.Http.Headers;
using System.Text.Json;

/// <summary>
/// OneDrive cloud storage provider implementation.
/// Supports OAuth2 authentication and Microsoft Graph API.
/// </summary>
public class OneDriveProvider : ICloudStorageProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private CloudProviderCredentials? _credentials;
    private const string GraphApiBaseUrl = "https://graph.microsoft.com/v1.0";
    private const string OneDriveRoot = "/me/drive/root";

    public CloudProviderType ProviderType => CloudProviderType.OneDrive;

    public OneDriveProvider(ILogger logger, HttpClient? httpClient = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<bool> AuthenticateAsync(CloudProviderCredentials credentials)
    {
        try
        {
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));

            if (string.IsNullOrEmpty(_credentials.AccessToken))
            {
                _logger.Error("OneDrive: Access token is required");
                return false;
            }

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _credentials.AccessToken);

            var isValid = await VerifyConnectionAsync();
            if (isValid)
            {
                _logger.Info("OneDrive: Authentication successful");
            }
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.Error($"OneDrive authentication failed: {ex.Message}");
            return false;
        }
    }

    public async Task<CloudUploadResult> UploadAsync(string localPath, string remotePath)
    {
        var result = new CloudUploadResult { Success = false };

        try
        {
            if (!File.Exists(localPath))
            {
                result.ErrorMessage = $"Local file not found: {localPath}";
                return result;
            }

            var fileInfo = new FileInfo(localPath);
            var fileName = Path.GetFileName(localPath);
            var uploadPath = NormalizeRemotePath(remotePath);

            // For small files (<4MB), use simple upload
            if (fileInfo.Length < 4 * 1024 * 1024)
            {
                using var fileStream = File.OpenRead(localPath);
                using var content = new StreamContent(fileStream);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var uploadUrl = $"{GraphApiBaseUrl}{OneDriveRoot}:{uploadPath}/{fileName}:/content";
                var response = await _httpClient.PutAsync(uploadUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonContent);
                    var root = doc.RootElement;

                    result.Success = true;
                    result.FileId = root.GetProperty("id").GetString();
                    result.RemotePath = $"{uploadPath}/{fileName}";
                    result.FileSizeBytes = fileInfo.Length;
                    result.UploadedAt = DateTime.UtcNow;
                    result.ETag = root.TryGetProperty("eTag", out var etag) 
                        ? etag.GetString() 
                        : null;

                    _logger.Info($"OneDrive: Uploaded {fileName} ({fileInfo.Length} bytes)");
                }
                else
                {
                    result.ErrorMessage = $"Upload failed: {response.StatusCode}";
                }
            }
            else
            {
                // For large files, implement resumable upload
                result.ErrorMessage = "Large file uploads require resumable upload session";
            }

            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.Error($"OneDrive upload error: {ex.Message}");
            return result;
        }
    }

    public async Task<CloudDownloadResult> DownloadAsync(string remotePath, string localPath)
    {
        var result = new CloudDownloadResult { Success = false };

        try
        {
            var normalizedPath = NormalizeRemotePath(remotePath);
            var downloadUrl = $"{GraphApiBaseUrl}{OneDriveRoot}:{normalizedPath}:/content";

            var response = await _httpClient.GetAsync(downloadUrl);
            if (response.IsSuccessStatusCode)
            {
                var directory = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var fileStream = File.Create(localPath);
                await response.Content.CopyToAsync(fileStream);

                var fileInfo = new FileInfo(localPath);
                result.Success = true;
                result.LocalPath = localPath;
                result.FileSizeBytes = fileInfo.Length;
                result.DownloadedAt = DateTime.UtcNow;

                _logger.Info($"OneDrive: Downloaded {Path.GetFileName(localPath)} ({fileInfo.Length} bytes)");
            }
            else
            {
                result.ErrorMessage = $"Download failed: {response.StatusCode}";
            }

            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.Error($"OneDrive download error: {ex.Message}");
            return result;
        }
    }

    public async Task<List<CloudFileInfo>> ListFilesAsync(string remotePath)
    {
        var files = new List<CloudFileInfo>();

        try
        {
            var normalizedPath = NormalizeRemotePath(remotePath);
            var listUrl = $"{GraphApiBaseUrl}{OneDriveRoot}:{normalizedPath}:/children";

            var response = await _httpClient.GetAsync(listUrl);
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                if (root.TryGetProperty("value", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var fileInfo = new CloudFileInfo
                        {
                            FileId = item.GetProperty("id").GetString(),
                            Name = item.GetProperty("name").GetString(),
                            Path = normalizedPath,
                            CreatedAt = DateTime.Parse(item.GetProperty("createdDateTime").GetString() ?? DateTime.UtcNow.ToString()),
                            ModifiedAt = DateTime.Parse(item.GetProperty("lastModifiedDateTime").GetString() ?? DateTime.UtcNow.ToString()),
                            ETag = item.TryGetProperty("eTag", out var etag) ? etag.GetString() : null,
                            IsFolder = item.TryGetProperty("folder", out _)
                        };

                        if (item.TryGetProperty("size", out var size))
                        {
                            fileInfo.SizeBytes = size.GetInt64();
                        }

                        files.Add(fileInfo);
                    }
                }
            }

            _logger.Info($"OneDrive: Listed {files.Count} items in {remotePath}");
            return files;
        }
        catch (Exception ex)
        {
            _logger.Error($"OneDrive list error: {ex.Message}");
            return files;
        }
    }

    public async Task<bool> DeleteAsync(string remotePath)
    {
        try
        {
            var normalizedPath = NormalizeRemotePath(remotePath);
            var deleteUrl = $"{GraphApiBaseUrl}{OneDriveRoot}:{normalizedPath}";

            var response = await _httpClient.DeleteAsync(deleteUrl);
            if (response.IsSuccessStatusCode)
            {
                _logger.Info($"OneDrive: Deleted {remotePath}");
                return true;
            }
            else
            {
                _logger.Warning($"OneDrive: Delete failed for {remotePath}: {response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"OneDrive delete error: {ex.Message}");
            return false;
        }
    }

    public async Task<CloudQuotaInfo> GetQuotaAsync()
    {
        var quota = new CloudQuotaInfo();

        try
        {
            var quotaUrl = $"{GraphApiBaseUrl}/me/drive";
            var response = await _httpClient.GetAsync(quotaUrl);

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                if (root.TryGetProperty("quota", out var quotaData))
                {
                    quota.TotalBytes = quotaData.GetProperty("total").GetInt64();
                    quota.UsedBytes = quotaData.GetProperty("used").GetInt64();
                    quota.AvailableBytes = quotaData.GetProperty("remaining").GetInt64();
                }
            }

            _logger.Info($"OneDrive: Quota - Used: {quota.UsedBytes} bytes, Available: {quota.AvailableBytes} bytes");
            return quota;
        }
        catch (Exception ex)
        {
            _logger.Error($"OneDrive quota error: {ex.Message}");
            return quota;
        }
    }

    public async Task<bool> VerifyConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{GraphApiBaseUrl}/me");
            var isValid = response.IsSuccessStatusCode;

            if (!isValid)
            {
                _logger.Warning("OneDrive: Connection verification failed");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.Error($"OneDrive connection verification error: {ex.Message}");
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
