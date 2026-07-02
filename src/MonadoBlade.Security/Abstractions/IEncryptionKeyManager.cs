namespace MonadoBlade.Security.Abstractions;

/// <summary>
/// Enum for key rotation policies.
/// </summary>
public enum KeyRotationPolicy
{
    Days30,
    Days60,
    Days90
}

/// <summary>
/// Interface for secure encryption key management.
/// </summary>
public interface IEncryptionKeyManager
{
    /// <summary>
    /// Generates a new encryption key with specified algorithm.
    /// </summary>
    /// <param name="algorithm">Algorithm type: RSA, AES, etc.</param>
    /// <param name="keySize">Size of the key in bits.</param>
    /// <returns>The key ID of the generated key.</returns>
    Task<string> GenerateKeyAsync(string algorithm, int keySize);

    /// <summary>
    /// Retrieves an encryption key by ID.
    /// </summary>
    /// <param name="keyId">The key identifier.</param>
    /// <returns>The encryption key data.</returns>
    Task<byte[]?> GetKeyAsync(string keyId);

    /// <summary>
    /// Rotates an encryption key according to policy.
    /// </summary>
    /// <param name="keyId">The key to rotate.</param>
    /// <param name="policy">The rotation policy to apply.</param>
    /// <returns>The ID of the new key.</returns>
    Task<string> RotateKeyAsync(string keyId, KeyRotationPolicy policy);

    /// <summary>
    /// Stores an encryption key securely (DPAPI on Windows, TPM when available).
    /// </summary>
    /// <param name="keyId">The key identifier.</param>
    /// <param name="keyData">The key data to store.</param>
    /// <param name="algorithm">The algorithm type.</param>
    /// <returns>True if successfully stored.</returns>
    Task<bool> StoreKeySecurelyAsync(string keyId, byte[] keyData, string algorithm);

    /// <summary>
    /// Gets key metadata including version and creation date.
    /// </summary>
    /// <param name="keyId">The key identifier.</param>
    /// <returns>Dictionary with key metadata.</returns>
    Task<Dictionary<string, string>> GetKeyMetadataAsync(string keyId);

    /// <summary>
    /// Checks if a key needs rotation based on its policy.
    /// </summary>
    /// <param name="keyId">The key identifier.</param>
    /// <returns>True if key should be rotated.</returns>
    Task<bool> IsKeyRotationNeededAsync(string keyId);

    /// <summary>
    /// Archives a key after rotation.
    /// </summary>
    /// <param name="keyId">The key to archive.</param>
    /// <returns>True if successfully archived.</returns>
    Task<bool> ArchiveKeyAsync(string keyId);

    /// <summary>
    /// Gets the current active key version.
    /// </summary>
    /// <param name="algorithm">The algorithm to get the current key for.</param>
    /// <returns>The key ID of the current active key.</returns>
    Task<string?> GetActiveKeyAsync(string algorithm);
}
