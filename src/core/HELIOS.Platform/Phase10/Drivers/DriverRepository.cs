using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.Drivers
{
    /// <summary>
    /// Manages local driver repository and caching.
    /// </summary>
    public class DriverRepository
    {
        private readonly string _repositoryPath;
        private readonly string _cacheDirectory;
        private readonly SemaphoreSlim _semaphore;
        private Dictionary<string, DriverInfo> _cache;

        public DriverRepository(string repositoryPath = null)
        {
            _repositoryPath = repositoryPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "HELIOS", "Drivers");
            _cacheDirectory = Path.Combine(_repositoryPath, "Cache");
            _semaphore = new SemaphoreSlim(1);
            _cache = new Dictionary<string, DriverInfo>();
        }

        /// <summary>
        /// Initialize repository.
        /// </summary>
        public async Task InitializeAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                Directory.CreateDirectory(_repositoryPath);
                Directory.CreateDirectory(_cacheDirectory);
                await LoadCacheAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Load cache from disk.
        /// </summary>
        private async Task LoadCacheAsync()
        {
            var indexPath = Path.Combine(_repositoryPath, "drivers.json");
            if (File.Exists(indexPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(indexPath);
                    var drivers = JsonSerializer.Deserialize<List<DriverInfo>>(json);
                    _cache = drivers?.ToDictionary(d => d.DriverId) ?? new Dictionary<string, DriverInfo>();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load cache: {ex.Message}");
                    _cache = new Dictionary<string, DriverInfo>();
                }
            }
        }

        /// <summary>
        /// Save cache to disk.
        /// </summary>
        private async Task SaveCacheAsync()
        {
            var indexPath = Path.Combine(_repositoryPath, "drivers.json");
            try
            {
                var json = JsonSerializer.Serialize(_cache.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(indexPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Get driver by ID.
        /// </summary>
        public async Task<DriverInfo> GetDriverAsync(string driverId)
        {
            await _semaphore.WaitAsync();
            try
            {
                return _cache.ContainsKey(driverId) ? _cache[driverId] : null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Get all cached drivers.
        /// </summary>
        public async Task<List<DriverInfo>> GetAllDriversAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return _cache.Values.ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Get drivers by type.
        /// </summary>
        public async Task<List<DriverInfo>> GetDriversByTypeAsync(string driverType)
        {
            await _semaphore.WaitAsync();
            try
            {
                return _cache.Values
                    .Where(d => d.DriverType.Equals(driverType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Get drivers by manufacturer.
        /// </summary>
        public async Task<List<DriverInfo>> GetDriversByManufacturerAsync(string manufacturer)
        {
            await _semaphore.WaitAsync();
            try
            {
                return _cache.Values
                    .Where(d => d.Manufacturer.Equals(manufacturer, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Store driver in repository.
        /// </summary>
        public async Task StoreDriverAsync(DriverInfo driver, string sourceFilePath)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists(sourceFilePath))
                    throw new FileNotFoundException($"Source file not found: {sourceFilePath}");

                string driverFileName = Path.GetFileName(sourceFilePath);
                string destinationPath = Path.Combine(_cacheDirectory, driver.DriverId, driverFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                File.Copy(sourceFilePath, destinationPath, overwrite: true);

                driver.LastUpdated = DateTime.UtcNow;
                _cache[driver.DriverId] = driver;

                await SaveCacheAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Remove driver from repository.
        /// </summary>
        public async Task RemoveDriverAsync(string driverId)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_cache.ContainsKey(driverId))
                {
                    _cache.Remove(driverId);
                }

                var driverPath = Path.Combine(_cacheDirectory, driverId);
                if (Directory.Exists(driverPath))
                {
                    Directory.Delete(driverPath, recursive: true);
                }

                await SaveCacheAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Clear all cached drivers.
        /// </summary>
        public async Task ClearCacheAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (Directory.Exists(_cacheDirectory))
                {
                    Directory.Delete(_cacheDirectory, recursive: true);
                    Directory.CreateDirectory(_cacheDirectory);
                }

                _cache.Clear();
                await SaveCacheAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Get driver file path.
        /// </summary>
        public async Task<string> GetDriverFilePathAsync(string driverId)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_cache.ContainsKey(driverId))
                    return null;

                var driverDirectory = Path.Combine(_cacheDirectory, driverId);
                if (!Directory.Exists(driverDirectory))
                    return null;

                var files = Directory.GetFiles(driverDirectory);
                return files.FirstOrDefault();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Check if driver exists.
        /// </summary>
        public async Task<bool> DriverExistsAsync(string driverId)
        {
            await _semaphore.WaitAsync();
            try
            {
                return _cache.ContainsKey(driverId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Get repository size in bytes.
        /// </summary>
        public async Task<long> GetRepositorySizeAsync()
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(_cacheDirectory))
                    return 0;

                var dirInfo = new DirectoryInfo(_cacheDirectory);
                return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
            });
        }

        /// <summary>
        /// Get driver count.
        /// </summary>
        public async Task<int> GetDriverCountAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return _cache.Count;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Update driver info.
        /// </summary>
        public async Task UpdateDriverInfoAsync(string driverId, DriverInfo updatedInfo)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_cache.ContainsKey(driverId))
                {
                    updatedInfo.DriverId = driverId;
                    updatedInfo.LastUpdated = DateTime.UtcNow;
                    _cache[driverId] = updatedInfo;
                    await SaveCacheAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Cleanup old drivers (older than specified days).
        /// </summary>
        public async Task CleanupOldDriversAsync(int daysOld)
        {
            await _semaphore.WaitAsync();
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
                var oldDrivers = _cache.Values
                    .Where(d => d.LastUpdated < cutoffDate)
                    .Select(d => d.DriverId)
                    .ToList();

                foreach (var driverId in oldDrivers)
                {
                    await RemoveDriverAsync(driverId);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Export driver info to file.
        /// </summary>
        public async Task ExportDriverListAsync(string exportPath)
        {
            await _semaphore.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(_cache.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(exportPath, json);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Get most recently updated drivers.
        /// </summary>
        public async Task<List<DriverInfo>> GetRecentDriversAsync(int count = 10)
        {
            await _semaphore.WaitAsync();
            try
            {
                return _cache.Values
                    .OrderByDescending(d => d.LastUpdated)
                    .Take(count)
                    .ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
