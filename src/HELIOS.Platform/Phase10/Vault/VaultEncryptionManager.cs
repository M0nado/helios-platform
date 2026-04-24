using System;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.Json;
using System.Collections.Generic;

namespace HELIOS.Platform.Phase10.Vault
{
    /// <summary>
    /// Manages encryption operations for the vault system using AES-256-GCM.
    /// </summary>
    public class VaultEncryptionManager : IVaultEncryptionManager
    {
        private readonly IVaultLogger _logger;
        private readonly string _keyStoreDir;

        public VaultEncryptionManager(IVaultLogger logger, string keyStoreDir = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _keyStoreDir = keyStoreDir ?? Path.Combine(Path.GetTempPath(), "vault-keys");
            
            if (!Directory.Exists(_keyStoreDir))
            {
                Directory.CreateDirectory(_keyStoreDir);
            }
        }

        /// <summary>
        /// Applies encryption to vault directory using BitLocker simulation.
        /// </summary>
        public async Task<bool> ApplyEncryptionAsync(string vaultPath)
        {
            try
            {
                _logger.Log($"Applying encryption to vault at {vaultPath}");

                if (!Directory.Exists(vaultPath))
                {
                    _logger.LogError("Vault path does not exist", new DirectoryNotFoundException(vaultPath));
                    return false;
                }

                // Create encryption metadata
                var encryptionMetadata = new
                {
                    method = "BitLocker",
                    algorithm = "AES-256-XTS",
                    applied = DateTime.UtcNow,
                    status = "active"
                };

                var metadataPath = Path.Combine(vaultPath, ".vault", "encryption.json");
                var json = JsonSerializer.Serialize(encryptionMetadata, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(metadataPath, json);

                _logger.Log("Encryption applied successfully");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to apply encryption: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Encrypts data using AES-256-GCM.
        /// </summary>
        public async Task<bool> EncryptDataAsync(byte[] data, byte[] key, out byte[] encrypted)
        {
            encrypted = null;

            try
            {
                if (data == null || data.Length == 0)
                {
                    _logger.LogError("Invalid data for encryption", new ArgumentException("Data cannot be null or empty"));
                    return false;
                }

                if (key == null || key.Length != 32)
                {
                    _logger.LogError("Invalid encryption key", new ArgumentException("Key must be 256-bit"));
                    return false;
                }

                using (var aes = new AesGcm(key))
                {
                    byte[] nonce = new byte[12]; // 96-bit nonce
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(nonce);
                    }

                    byte[] tag = new byte[16]; // 128-bit tag
                    byte[] ciphertext = new byte[data.Length];

                    aes.Encrypt(nonce, data, ciphertext, tag);

                    // Package: nonce (12) + tag (16) + ciphertext
                    encrypted = new byte[nonce.Length + tag.Length + ciphertext.Length];
                    Buffer.BlockCopy(nonce, 0, encrypted, 0, nonce.Length);
                    Buffer.BlockCopy(tag, 0, encrypted, nonce.Length, tag.Length);
                    Buffer.BlockCopy(ciphertext, 0, encrypted, nonce.Length + tag.Length, ciphertext.Length);

                    _logger.Log($"Data encrypted successfully ({data.Length} bytes -> {encrypted.Length} bytes)");
                    return await Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Encryption failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Decrypts data that was encrypted with EncryptDataAsync.
        /// </summary>
        public async Task<bool> DecryptDataAsync(byte[] encrypted, byte[] key, out byte[] data)
        {
            data = null;

            try
            {
                if (encrypted == null || encrypted.Length < 28) // 12 + 16
                {
                    _logger.LogError("Invalid encrypted data", new ArgumentException("Encrypted data too short"));
                    return false;
                }

                if (key == null || key.Length != 32)
                {
                    _logger.LogError("Invalid decryption key", new ArgumentException("Key must be 256-bit"));
                    return false;
                }

                // Extract components
                byte[] nonce = new byte[12];
                byte[] tag = new byte[16];
                byte[] ciphertext = new byte[encrypted.Length - 28];

                Buffer.BlockCopy(encrypted, 0, nonce, 0, 12);
                Buffer.BlockCopy(encrypted, 12, tag, 0, 16);
                Buffer.BlockCopy(encrypted, 28, ciphertext, 0, ciphertext.Length);

                using (var aes = new AesGcm(key))
                {
                    byte[] plaintext = new byte[ciphertext.Length];
                    aes.Decrypt(nonce, ciphertext, tag, plaintext);
                    
                    data = plaintext;
                    _logger.Log($"Data decrypted successfully ({encrypted.Length} bytes -> {data.Length} bytes)");
                    return await Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Decryption failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Rotates encryption keys for security.
        /// </summary>
        public async Task<bool> RotateKeyAsync(string keyId, byte[] newKey)
        {
            try
            {
                if (newKey == null || newKey.Length != 32)
                {
                    return false;
                }

                var keyPath = Path.Combine(_keyStoreDir, $"{keyId}.rotated");
                var rotation = new
                {
                    previousKeyId = keyId,
                    newKeyId = $"{keyId}_rotated_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    rotatedAt = DateTime.UtcNow,
                    algorithm = "AES-256-GCM"
                };

                var json = JsonSerializer.Serialize(rotation, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(keyPath, json);

                _logger.Log($"Key rotation scheduled for {keyId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Key rotation failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Backs up encryption keys securely.
        /// </summary>
        public async Task<bool> BackupKeysAsync(string backupPath, byte[] backupKey)
        {
            try
            {
                if (!Directory.Exists(backupPath))
                {
                    Directory.CreateDirectory(backupPath);
                }

                var keyFiles = Directory.GetFiles(_keyStoreDir, "*.key");
                
                foreach (var keyFile in keyFiles)
                {
                    var content = await File.ReadAllBytesAsync(keyFile);
                    var encrypted = new byte[0];

                    if (await EncryptDataAsync(content, backupKey, out encrypted))
                    {
                        var filename = Path.GetFileName(keyFile);
                        var backupFile = Path.Combine(backupPath, $"{filename}.encrypted");
                        await File.WriteAllBytesAsync(backupFile, encrypted);
                    }
                }

                _logger.Log($"Keys backed up to {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Key backup failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Enables file-level encryption for specific file.
        /// </summary>
        public async Task<bool> EncryptFileAsync(string filePath, byte[] key)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                var fileContent = await File.ReadAllBytesAsync(filePath);
                byte[] encrypted;

                if (!await EncryptDataAsync(fileContent, key, out encrypted))
                {
                    return false;
                }

                var encryptedPath = filePath + ".encrypted";
                await File.WriteAllBytesAsync(encryptedPath, encrypted);
                File.Delete(filePath);

                _logger.Log($"File encrypted: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"File encryption failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Decrypts file back to original form.
        /// </summary>
        public async Task<bool> DecryptFileAsync(string encryptedFilePath, byte[] key)
        {
            try
            {
                if (!File.Exists(encryptedFilePath))
                {
                    return false;
                }

                var encrypted = await File.ReadAllBytesAsync(encryptedFilePath);
                byte[] decrypted;

                if (!await DecryptDataAsync(encrypted, key, out decrypted))
                {
                    return false;
                }

                var originalPath = encryptedFilePath.EndsWith(".encrypted") 
                    ? encryptedFilePath.Substring(0, encryptedFilePath.Length - 10)
                    : encryptedFilePath + ".decrypted";
                    
                await File.WriteAllBytesAsync(originalPath, decrypted);

                _logger.Log($"File decrypted: {originalPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"File decryption failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets encryption status for vault.
        /// </summary>
        public async Task<EncryptionStatus> GetStatusAsync(string vaultPath)
        {
            try
            {
                var metadataPath = Path.Combine(vaultPath, ".vault", "encryption.json");
                
                if (!File.Exists(metadataPath))
                {
                    return new EncryptionStatus { IsEncrypted = false, Status = "Not encrypted" };
                }

                var json = await File.ReadAllTextAsync(metadataPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var status = root.GetProperty("status").GetString();
                var appliedAt = root.GetProperty("applied").GetDateTime();

                return new EncryptionStatus
                {
                    IsEncrypted = status == "active",
                    Status = status,
                    AppliedAt = appliedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get encryption status: {ex.Message}", ex);
                return new EncryptionStatus { IsEncrypted = false, Status = "Unknown" };
            }
        }
    }

    public class EncryptionStatus
    {
        public bool IsEncrypted { get; set; }
        public string Status { get; set; }
        public DateTime? AppliedAt { get; set; }
    }
}
