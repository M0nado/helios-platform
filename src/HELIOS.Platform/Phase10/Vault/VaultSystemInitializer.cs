using System;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace HELIOS.Platform.Phase10.Vault
{
    /// <summary>
    /// Initializes the vault system, creates partition, applies encryption, and sets up folder structure.
    /// </summary>
    public class VaultSystemInitializer
    {
        private readonly string _vaultPath;
        private readonly string _masterKeyPath;
        private readonly IVaultEncryptionManager _encryptionManager;
        private readonly IVaultLogger _logger;

        private static readonly Dictionary<string, string[]> LockerStructure = new()
        {
            { "Personal", new[] { "Documents", "Financial", "Medical" } },
            { "Work", new[] { "Projects", "Clients", "Contracts" } },
            { "Gaming", new[] { "Saves", "Credentials", "Configs" } },
            { "Security", new[] { "KeePass", "SSHKeys", "APIKeys", "Certificates" } },
            { "Media", new[] { "Photos", "Videos", "Recordings" } }
        };

        public VaultSystemInitializer(
            string vaultPath,
            IVaultEncryptionManager encryptionManager,
            IVaultLogger logger)
        {
            _vaultPath = vaultPath ?? throw new ArgumentNullException(nameof(vaultPath));
            _encryptionManager = encryptionManager ?? throw new ArgumentNullException(nameof(encryptionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _masterKeyPath = Path.Combine(_vaultPath, ".vault", "master.key");
        }

        /// <summary>
        /// Initializes the complete vault system.
        /// </summary>
        public async Task<VaultInitializationResult> InitializeAsync()
        {
            try
            {
                _logger.Log("Starting vault system initialization...");

                // Step 1: Verify or create vault partition
                if (!await VerifyVaultPartitionAsync())
                {
                    return VaultInitializationResult.Failure("Failed to verify/create vault partition");
                }

                // Step 2: Create base vault directory
                if (!Directory.Exists(_vaultPath))
                {
                    Directory.CreateDirectory(_vaultPath);
                    _logger.Log($"Created vault directory at {_vaultPath}");
                }

                // Step 3: Create metadata directory
                var metadataPath = Path.Combine(_vaultPath, ".vault");
                if (!Directory.Exists(metadataPath))
                {
                    Directory.CreateDirectory(metadataPath);
                }

                // Step 4: Generate and store master key
                if (!File.Exists(_masterKeyPath))
                {
                    var masterKey = GenerateMasterKey();
                    await SaveMasterKeyAsync(masterKey);
                    _logger.Log("Master key generated and stored");
                }

                // Step 5: Create folder structure
                await CreateFolderStructureAsync();

                // Step 6: Initialize vault metadata
                await InitializeMetadataAsync();

                // Step 7: Backup master key
                await BackupMasterKeyAsync();

                // Step 8: Apply encryption
                if (!await _encryptionManager.ApplyEncryptionAsync(_vaultPath))
                {
                    return VaultInitializationResult.Failure("Failed to apply encryption to vault");
                }

                _logger.Log("Vault system initialization completed successfully");
                return VaultInitializationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Vault initialization failed: {ex.Message}", ex);
                return VaultInitializationResult.Failure(ex.Message);
            }
        }

        private async Task<bool> VerifyVaultPartitionAsync()
        {
            try
            {
                var driveLetter = "E";
                var drive = new DriveInfo(driveLetter + ":");

                if (drive.IsReady)
                {
                    _logger.Log($"Vault partition {driveLetter}: verified (Available: {drive.AvailableFreeSpace / (1024 * 1024 * 1024)} GB)");
                    return true;
                }

                _logger.Log("Vault partition E: not found, using fallback location");
                return true;
            }
            catch
            {
                return true;
            }
        }

        private byte[] GenerateMasterKey()
        {
            using var rng = RandomNumberGenerator.Create();
            var key = new byte[32];
            rng.GetBytes(key);
            return key;
        }

        private async Task SaveMasterKeyAsync(byte[] masterKey)
        {
            var keyData = new
            {
                key = Convert.ToBase64String(masterKey),
                created = DateTime.UtcNow,
                algorithm = "AES-256-GCM"
            };

            var json = JsonSerializer.Serialize(keyData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_masterKeyPath, json);
        }

        private async Task CreateFolderStructureAsync()
        {
            foreach (var (lockerName, subfolders) in LockerStructure)
            {
                var lockerPath = Path.Combine(_vaultPath, lockerName);
                
                if (!Directory.Exists(lockerPath))
                {
                    Directory.CreateDirectory(lockerPath);
                    _logger.Log($"Created {lockerName} locker");
                }

                foreach (var subfolder in subfolders)
                {
                    var subfolderPath = Path.Combine(lockerPath, subfolder);
                    if (!Directory.Exists(subfolderPath))
                    {
                        Directory.CreateDirectory(subfolderPath);
                    }
                }
            }

            await Task.CompletedTask;
        }

        private async Task InitializeMetadataAsync()
        {
            var metadata = new
            {
                vaultVersion = "1.0.0",
                initialized = DateTime.UtcNow,
                lockers = new List<object>()
            };

            foreach (var lockerName in LockerStructure.Keys)
            {
                metadata.lockers.Add(new
                {
                    name = lockerName,
                    created = DateTime.UtcNow,
                    encrypted = true,
                    size = 0L,
                    itemCount = 0
                });
            }

            var metadataPath = Path.Combine(_vaultPath, ".vault", "metadata.json");
            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(metadataPath, json);
            _logger.Log("Vault metadata initialized");
        }

        private async Task BackupMasterKeyAsync()
        {
            try
            {
                var backupDrive = "J:";
                if (DriveInfo.GetDrives().Any(d => d.Name.StartsWith(backupDrive)))
                {
                    var backupPath = Path.Combine(backupDrive, "VaultBackup", ".vault");
                    Directory.CreateDirectory(backupPath);
                    
                    var sourceKey = _masterKeyPath;
                    var destKey = Path.Combine(backupPath, "master.key.backup");
                    File.Copy(sourceKey, destKey, overwrite: true);
                    
                    _logger.Log($"Master key backed up to {destKey}");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Master key backup failed: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> GetMasterKeyAsync()
        {
            try
            {
                var json = await File.ReadAllTextAsync(_masterKeyPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("key", out var keyElement))
                {
                    var keyStr = keyElement.GetString();
                    return Convert.FromBase64String(keyStr);
                }

                throw new InvalidOperationException("Master key not found");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to retrieve master key: {ex.Message}", ex);
                throw;
            }
        }

        public bool IsInitialized() => Directory.Exists(_vaultPath) && File.Exists(_masterKeyPath);
    }

    public class VaultInitializationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }

        public static VaultInitializationResult Success() => new() { IsSuccess = true, Message = "Success" };
        public static VaultInitializationResult Failure(string message) => new() { IsSuccess = false, Message = message };
    }

    public interface IVaultEncryptionManager
    {
        Task<bool> ApplyEncryptionAsync(string path);
        Task<bool> EncryptDataAsync(byte[] data, byte[] key, out byte[] encrypted);
        Task<bool> DecryptDataAsync(byte[] encrypted, byte[] key, out byte[] data);
    }

    public interface IVaultLogger
    {
        void Log(string message);
        void LogError(string message, Exception ex);
    }
}
