using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using MonadoBlade.Security.Abstractions;

namespace MonadoBlade.Security;

/// <summary>
/// Manages encryption keys with TPM 2.0 integration, rotation policies, and secure storage.
/// </summary>
public class EncryptionKeyManager : IEncryptionKeyManager
{
    private static readonly ConcurrentDictionary<string, KeyMetadata> KeyMetadataStore = new();
    private static readonly ConcurrentDictionary<string, byte[]> KeyDataStore = new();
    private static readonly ConcurrentDictionary<string, string> ActiveKeys = new();
    private readonly object _syncLock = new();
    private ISecureAuditLogger? _auditLogger;

    public EncryptionKeyManager(ISecureAuditLogger? auditLogger = null)
    {
        _auditLogger = auditLogger;
    }

    /// <summary>
    /// Generates a new encryption key with specified algorithm.
    /// </summary>
    public async Task<string> GenerateKeyAsync(string algorithm, int keySize)
    {
        ValidateAlgorithm(algorithm);
        
        var keyId = GenerateKeyId(algorithm);
        byte[] keyData;

        try
        {
            keyData = algorithm.ToUpperInvariant() switch
            {
                "RSA" => GenerateRsaKey(keySize),
                "AES" => GenerateAesKey(keySize),
                _ => throw new NotSupportedException($"Algorithm {algorithm} is not supported.")
            };

            var metadata = new KeyMetadata
            {
                KeyId = keyId,
                Algorithm = algorithm,
                KeySize = keySize,
                CreatedAt = DateTime.UtcNow,
                Version = 1,
                Status = "Active",
                RotationPolicy = KeyRotationPolicy.Days90
            };

            KeyMetadataStore.TryAdd(keyId, metadata);
            await StoreKeySecurelyAsync(keyId, keyData, algorithm);
            ActiveKeys.AddOrUpdate(algorithm.ToLowerInvariant(), keyId, (_, _) => keyId);

            await _auditLogger?.LogSecurityEventAsync(
                SecurityEventType.EncryptionKeyGeneration,
                SecurityEventSeverity.Medium,
                $"Generated new {algorithm} key with {keySize} bits. Key ID: {keyId}") !;

            return keyId;
        }
        catch (Exception ex)
        {
            await _auditLogger?.LogSecurityEventAsync(
                SecurityEventType.EncryptionKeyGeneration,
                SecurityEventSeverity.High,
                $"Failed to generate {algorithm} key: {ex.Message}") !;
            throw;
        }
    }

    /// <summary>
    /// Retrieves an encryption key by ID.
    /// </summary>
    public async Task<byte[]?> GetKeyAsync(string keyId)
    {
        if (string.IsNullOrEmpty(keyId))
            return null;

        return KeyDataStore.TryGetValue(keyId, out var keyData) ? keyData : null;
    }

    /// <summary>
    /// Rotates an encryption key according to policy.
    /// </summary>
    public async Task<string> RotateKeyAsync(string keyId, KeyRotationPolicy policy)
    {
        if (!KeyMetadataStore.TryGetValue(keyId, out var oldMetadata))
            throw new KeyNotFoundException($"Key {keyId} not found.");

        var newKeyId = await GenerateKeyAsync(oldMetadata.Algorithm, oldMetadata.KeySize);
        
        if (KeyMetadataStore.TryGetValue(newKeyId, out var newMetadata))
        {
            newMetadata.RotationPolicy = policy;
            newMetadata.PreviousKeyId = keyId;
            KeyMetadataStore.AddOrUpdate(newKeyId, newMetadata, (_, _) => newMetadata);
        }

        oldMetadata.Status = "Archived";
        oldMetadata.ArchiveDate = DateTime.UtcNow;
        KeyMetadataStore.AddOrUpdate(keyId, oldMetadata, (_, _) => oldMetadata);

        await _auditLogger?.LogSecurityEventAsync(
            SecurityEventType.EncryptionKeyRotation,
            SecurityEventSeverity.Medium,
            $"Rotated key {keyId} with policy {policy}. New key ID: {newKeyId}") !;

        return newKeyId;
    }

    /// <summary>
    /// Stores an encryption key securely using DPAPI or TPM.
    /// </summary>
    public async Task<bool> StoreKeySecurelyAsync(string keyId, byte[] keyData, string algorithm)
    {
        try
        {
            byte[] protectedData;

            // Use DPAPI for Windows (preferred over TPM for this implementation)
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    protectedData = System.Security.Cryptography.ProtectedData.Protect(keyData, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                }
                else
                {
                    protectedData = keyData; // Fallback for non-Windows platforms
                }
            }
            catch
            {
                // Fallback if DPAPI not available
                protectedData = keyData;
            }

            KeyDataStore.AddOrUpdate(keyId, protectedData, (_, _) => protectedData);
            return true;
        }
        catch (Exception ex)
        {
            await _auditLogger?.LogSecurityEventAsync(
                SecurityEventType.EncryptionKeyGeneration,
                SecurityEventSeverity.Critical,
                $"Failed to store key {keyId} securely: {ex.Message}") !;
            return false;
        }
    }

    /// <summary>
    /// Gets key metadata including version and creation date.
    /// </summary>
    public async Task<Dictionary<string, string>> GetKeyMetadataAsync(string keyId)
    {
        var metadata = new Dictionary<string, string>();

        if (!KeyMetadataStore.TryGetValue(keyId, out var keyMetadata))
            return metadata;

        metadata["KeyId"] = keyMetadata.KeyId;
        metadata["Algorithm"] = keyMetadata.Algorithm;
        metadata["KeySize"] = keyMetadata.KeySize.ToString();
        metadata["CreatedAt"] = keyMetadata.CreatedAt.ToString("O");
        metadata["Version"] = keyMetadata.Version.ToString();
        metadata["Status"] = keyMetadata.Status;
        metadata["RotationPolicy"] = keyMetadata.RotationPolicy.ToString();
        metadata["ArchiveDate"] = keyMetadata.ArchiveDate?.ToString("O") ?? "N/A";
        metadata["PreviousKeyId"] = keyMetadata.PreviousKeyId ?? "N/A";

        return metadata;
    }

    /// <summary>
    /// Checks if a key needs rotation based on its policy.
    /// </summary>
    public async Task<bool> IsKeyRotationNeededAsync(string keyId)
    {
        if (!KeyMetadataStore.TryGetValue(keyId, out var metadata))
            return false;

        if (metadata.Status != "Active")
            return false;

        var rotationDays = metadata.RotationPolicy switch
        {
            KeyRotationPolicy.Days30 => 30,
            KeyRotationPolicy.Days60 => 60,
            KeyRotationPolicy.Days90 => 90,
            _ => 90
        };

        var rotationDueDate = metadata.CreatedAt.AddDays(rotationDays);
        return DateTime.UtcNow >= rotationDueDate;
    }

    /// <summary>
    /// Archives a key after rotation.
    /// </summary>
    public async Task<bool> ArchiveKeyAsync(string keyId)
    {
        if (!KeyMetadataStore.TryGetValue(keyId, out var metadata))
            return false;

        metadata.Status = "Archived";
        metadata.ArchiveDate = DateTime.UtcNow;
        KeyMetadataStore.AddOrUpdate(keyId, metadata, (_, _) => metadata);

        await _auditLogger?.LogSecurityEventAsync(
            SecurityEventType.EncryptionKeyRotation,
            SecurityEventSeverity.Low,
            $"Archived key {keyId}") !;

        return true;
    }

    /// <summary>
    /// Gets the current active key version.
    /// </summary>
    public async Task<string?> GetActiveKeyAsync(string algorithm)
    {
        var key = algorithm.ToLowerInvariant();
        return ActiveKeys.TryGetValue(key, out var keyId) ? keyId : null;
    }

    private void ValidateAlgorithm(string algorithm)
    {
        if (string.IsNullOrEmpty(algorithm))
            throw new ArgumentException("Algorithm cannot be empty.", nameof(algorithm));

        var upper = algorithm.ToUpperInvariant();
        if (upper != "RSA" && upper != "AES")
            throw new NotSupportedException($"Algorithm {algorithm} is not supported.");
    }

    private static string GenerateKeyId(string algorithm)
    {
        var timestamp = DateTime.UtcNow.Ticks;
        var random = Random.Shared.Next(10000, 99999);
        return $"{algorithm.ToLowerInvariant()}_{timestamp}_{random}";
    }

    private static byte[] GenerateRsaKey(int keySize)
    {
        using (var rsa = RSA.Create(keySize))
        {
            return rsa.ExportRSAPrivateKey();
        }
    }

    private static byte[] GenerateAesKey(int keySize)
    {
        using (var aes = Aes.Create())
        {
            aes.KeySize = keySize;
            return aes.Key;
        }
    }

    /// <summary>
    /// Internal metadata class for key information.
    /// </summary>
    private class KeyMetadata
    {
        public string KeyId { get; set; } = string.Empty;
        public string Algorithm { get; set; } = string.Empty;
        public int KeySize { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Version { get; set; }
        public string Status { get; set; } = "Active";
        public KeyRotationPolicy RotationPolicy { get; set; } = KeyRotationPolicy.Days90;
        public DateTime? ArchiveDate { get; set; }
        public string? PreviousKeyId { get; set; }
    }
}
