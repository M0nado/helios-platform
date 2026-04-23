using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Drivers
{
    /// <summary>
    /// Downloads drivers from manufacturer sources.
    /// </summary>
    public class DriverDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly DriverRepository _repository;
        private readonly SemaphoreSlim _semaphore;
        private readonly Dictionary<string, string> _manufacturerUrls;
        private readonly string _tempDirectory;

        public DriverDownloader(DriverRepository repository)
        {
            _repository = repository;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
            _semaphore = new SemaphoreSlim(1);
            _tempDirectory = Path.Combine(Path.GetTempPath(), "HELIOS_Driver_Downloads");
            Directory.CreateDirectory(_tempDirectory);

            _manufacturerUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Intel", "https://downloadcenter.intel.com/download/" },
                { "AMD", "https://www.amd.com/en/support/download/drivers.html" },
                { "NVIDIA", "https://www.nvidia.com/Download/driverDetails.aspx/" },
                { "Realtek", "https://www.realtek.com/Download/" },
                { "Broadcom", "https://www.broadcom.com/support/download-search/" },
                { "Qualcomm", "https://www.qualcomm.com/developer/downloads/" }
            };
        }

        /// <summary>
        /// Download driver with progress tracking.
        /// </summary>
        public async Task<DriverInfo> DownloadDriverAsync(string driverId, string downloadUrl, Action<DownloadProgress> progressCallback = null)
        {
            await _semaphore.WaitAsync();
            try
            {
                var tempPath = Path.Combine(_tempDirectory, $"{driverId}.tmp");
                var progress = new DownloadProgress
                {
                    DriverName = driverId,
                    StartTime = DateTime.UtcNow
                };

                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    progress.TotalBytes = response.Content.Headers.ContentLength ?? -1L;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            progress.BytesDownloaded += bytesRead;

                            if (progress.TotalBytes > 0)
                            {
                                progress.Percentage = (int)((progress.BytesDownloaded * 100) / progress.TotalBytes);
                                var elapsed = DateTime.UtcNow - progress.StartTime;
                                if (elapsed.TotalSeconds > 0)
                                {
                                    var rate = progress.BytesDownloaded / elapsed.TotalSeconds;
                                    var remaining = (progress.TotalBytes - progress.BytesDownloaded) / rate;
                                    progress.EstimatedTimeRemaining = TimeSpan.FromSeconds(remaining);
                                }
                            }

                            progressCallback?.Invoke(progress);
                        }
                    }
                }

                progress.IsComplete = true;
                progressCallback?.Invoke(progress);

                var driverInfo = new DriverInfo
                {
                    DriverId = driverId,
                    Name = driverId,
                    Version = "1.0.0",
                    LastUpdated = DateTime.UtcNow,
                    IsStable = true
                };

                await _repository.StoreDriverAsync(driverInfo, tempPath);
                File.Delete(tempPath);

                return driverInfo;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Download Intel driver.
        /// </summary>
        public async Task<DriverInfo> DownloadIntelDriverAsync(string chipsetId, Action<DownloadProgress> progressCallback = null)
        {
            var url = $"https://downloadcenter.intel.com/api/download/";
            var driverId = $"intel_{chipsetId}_{DateTime.UtcNow.Ticks}";
            return await DownloadDriverAsync(driverId, url, progressCallback);
        }

        /// <summary>
        /// Download AMD driver.
        /// </summary>
        public async Task<DriverInfo> DownloadAmdDriverAsync(string chipsetId, Action<DownloadProgress> progressCallback = null)
        {
            var url = $"https://www.amd.com/en/support/download/";
            var driverId = $"amd_{chipsetId}_{DateTime.UtcNow.Ticks}";
            return await DownloadDriverAsync(driverId, url, progressCallback);
        }

        /// <summary>
        /// Download NVIDIA driver.
        /// </summary>
        public async Task<DriverInfo> DownloadNvidiaDriverAsync(string gpuId, Action<DownloadProgress> progressCallback = null)
        {
            var url = $"https://www.nvidia.com/Download/driverDetails.aspx/";
            var driverId = $"nvidia_{gpuId}_{DateTime.UtcNow.Ticks}";
            return await DownloadDriverAsync(driverId, url, progressCallback);
        }

        /// <summary>
        /// Resume interrupted download.
        /// </summary>
        public async Task<DriverInfo> ResumeDownloadAsync(string driverId, string downloadUrl, Action<DownloadProgress> progressCallback = null)
        {
            var tempPath = Path.Combine(_tempDirectory, $"{driverId}.tmp");
            long existingSize = File.Exists(tempPath) ? new FileInfo(tempPath).Length : 0;

            var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            request.Headers.Add("Range", $"bytes={existingSize}-");

            await _semaphore.WaitAsync();
            try
            {
                using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var progress = new DownloadProgress
                    {
                        DriverName = driverId,
                        StartTime = DateTime.UtcNow,
                        BytesDownloaded = existingSize,
                        TotalBytes = response.Content.Headers.ContentLength ?? -1L
                    };

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempPath, FileMode.Append, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            progress.BytesDownloaded += bytesRead;
                            progress.Percentage = (int)((progress.BytesDownloaded * 100) / progress.TotalBytes);
                            progressCallback?.Invoke(progress);
                        }
                    }

                    progress.IsComplete = true;
                    progressCallback?.Invoke(progress);

                    var driverInfo = new DriverInfo
                    {
                        DriverId = driverId,
                        Name = driverId,
                        Version = "1.0.0",
                        LastUpdated = DateTime.UtcNow,
                        IsStable = true
                    };

                    await _repository.StoreDriverAsync(driverInfo, tempPath);
                    File.Delete(tempPath);

                    return driverInfo;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Verify file checksum.
        /// </summary>
        public async Task<bool> VerifyChecksumAsync(string filePath, string expectedChecksum)
        {
            return await Task.Run(() =>
            {
                if (!File.Exists(filePath))
                    return false;

                try
                {
                    using (var sha256 = SHA256.Create())
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        var hashBytes = sha256.ComputeHash(fileStream);
                        var computedChecksum = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                        return computedChecksum.Equals(expectedChecksum.ToLowerInvariant());
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Checksum verification failed: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Calculate file checksum.
        /// </summary>
        public async Task<string> CalculateChecksumAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var sha256 = SHA256.Create())
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        var hashBytes = sha256.ComputeHash(fileStream);
                        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Checksum calculation failed: {ex.Message}");
                    return null;
                }
            });
        }

        /// <summary>
        /// Cancel download.
        /// </summary>
        public void CancelDownload()
        {
            _httpClient?.CancelPendingRequests();
        }

        /// <summary>
        /// Cleanup temporary files.
        /// </summary>
        public async Task CleanupAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (Directory.Exists(_tempDirectory))
                    {
                        Directory.Delete(_tempDirectory, recursive: true);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Cleanup failed: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Get download URL for driver type.
        /// </summary>
        public string GetManufacturerUrl(string manufacturer)
        {
            return _manufacturerUrls.TryGetValue(manufacturer, out var url) ? url : null;
        }

        /// <summary>
        /// Get temp directory path.
        /// </summary>
        public string GetTempDirectory()
        {
            return _tempDirectory;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _semaphore?.Dispose();
        }
    }
}
