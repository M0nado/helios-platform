using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using HELIOS.Platform.BackendServices.Encryption;

namespace HELIOS.Platform.Tests.Security
{
    public class EncryptionServiceTests
    {
        private readonly Mock<ILogger<EncryptionService>> _mockLogger;
        private readonly EncryptionService _encryptionService;

        public EncryptionServiceTests()
        {
            _mockLogger = new Mock<ILogger<EncryptionService>>();
            _encryptionService = new EncryptionService(_mockLogger.Object);
        }

        [Fact]
        public void GenerateSalt_ReturnsRandomBytes()
        {
            // Act
            var salt1 = _encryptionService.GenerateSalt(16);
            var salt2 = _encryptionService.GenerateSalt(16);

            // Assert
            Assert.NotNull(salt1);
            Assert.NotNull(salt2);
            Assert.Equal(16, salt1.Length);
            Assert.NotEqual(salt1, salt2); // Should be different each time
        }

        [Fact]
        public void GenerateRandomBytes_ReturnsCorrectLength()
        {
            // Act
            var bytes = _encryptionService.GenerateRandomBytes(32);

            // Assert
            Assert.NotNull(bytes);
            Assert.Equal(32, bytes.Length);
        }

        [Fact]
        public void DeriveKeyPbkdf2_ReturnsConsistentKey()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = _encryptionService.GenerateSalt();

            // Act
            var key1 = _encryptionService.DeriveKeyPbkdf2(password, salt);
            var key2 = _encryptionService.DeriveKeyPbkdf2(password, salt);

            // Assert
            Assert.NotNull(key1);
            Assert.NotNull(key2);
            Assert.Equal(32, key1.Length); // AES-256
            Assert.Equal(key1, key2); // Same password + salt = same key
        }

        [Fact]
        public void EncryptDecrypt_AES256_ReturnsOriginalText()
        {
            // Arrange
            var plaintext = "Sensitive data for encryption";
            var key = _encryptionService.GenerateRandomBytes(32);
            var iv = _encryptionService.GenerateRandomBytes(16);

            // Act
            var encrypted = _encryptionService.EncryptAes256(plaintext, key, iv);
            var decrypted = _encryptionService.DecryptAes256(encrypted, key, iv);

            // Assert
            Assert.NotEqual(plaintext, encrypted);
            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        public void EncryptWithMasterPassword_EncryptsAndDecrypts()
        {
            // Arrange
            var data = "Confidential information";
            var masterPassword = "VerySecurePassword123!";

            // Act
            var encrypted = _encryptionService.EncryptWithMasterPassword(data, masterPassword);
            var decrypted = _encryptionService.DecryptWithMasterPassword(encrypted, masterPassword);

            // Assert
            Assert.NotEqual(data, encrypted);
            Assert.Equal(data, decrypted);
        }

        [Fact]
        public void DecryptWithWrongPassword_ThrowsException()
        {
            // Arrange
            var data = "Secret";
            var correctPassword = "CorrectPassword123!";
            var wrongPassword = "WrongPassword123!";
            var encrypted = _encryptionService.EncryptWithMasterPassword(data, correctPassword);

            // Act & Assert
            Assert.Throws<Exception>(() =>
                _encryptionService.DecryptWithMasterPassword(encrypted, wrongPassword));
        }

        [Fact]
        public void EncryptAes256_WithInvalidKeyLength_ThrowsException()
        {
            // Arrange
            var plaintext = "Test";
            var invalidKey = new byte[16]; // Should be 32
            var iv = new byte[16];

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _encryptionService.EncryptAes256(plaintext, invalidKey, iv));
        }
    }
}
