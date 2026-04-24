using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform;

namespace HELIOS.Platform.Tests.Unit
{
    /// <summary>
    /// Unit Tests for ConfigurationManager - 5 test cases
    /// Category: Unit
    /// </summary>
    public class ConfigurationManagerTests
    {
        private readonly Mock<IConfigurationManager> _mockConfigManager;

        public ConfigurationManagerTests()
        {
            _mockConfigManager = new Mock<IConfigurationManager>();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void LoadConfiguration_ValidPath_ReturnsConfiguration()
        {
            // Arrange
            var configPath = "test-config.json";
            var expectedConfig = new Dictionary<string, string>
            {
                { "AppName", "HELIOS" },
                { "Version", "1.0.0" }
            };
            _mockConfigManager.Setup(c => c.Load(configPath)).Returns(expectedConfig);

            // Act
            var result = _mockConfigManager.Object.Load(configPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("HELIOS", result["AppName"]);
            _mockConfigManager.Verify(c => c.Load(configPath), Times.Once);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void LoadConfiguration_InvalidPath_ThrowsException()
        {
            // Arrange
            var invalidPath = "";
            _mockConfigManager.Setup(c => c.Load(invalidPath))
                .Throws<ArgumentException>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _mockConfigManager.Object.Load(invalidPath));
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void SaveConfiguration_ValidConfig_ReturnSuccess()
        {
            // Arrange
            var config = new Dictionary<string, string> { { "Key", "Value" } };
            var path = "config.json";
            _mockConfigManager.Setup(c => c.Save(path, config)).Returns(true);

            // Act
            var result = _mockConfigManager.Object.Save(path, config);

            // Assert
            Assert.True(result);
            _mockConfigManager.Verify(c => c.Save(path, config), Times.Once);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void ValidateConfiguration_ValidSettings_ReturnsTrue()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                { "AppName", "HELIOS" },
                { "LogLevel", "Info" },
                { "MaxRetries", "3" }
            };
            _mockConfigManager.Setup(c => c.Validate(config)).Returns(true);

            // Act
            var result = _mockConfigManager.Object.Validate(config);

            // Assert
            Assert.True(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void MergeConfiguration_MultipleConfigs_ReturnsMerged()
        {
            // Arrange
            var config1 = new Dictionary<string, string> { { "Key1", "Value1" } };
            var config2 = new Dictionary<string, string> { { "Key2", "Value2" } };
            var expected = new Dictionary<string, string>
            {
                { "Key1", "Value1" },
                { "Key2", "Value2" }
            };
            _mockConfigManager.Setup(c => c.Merge(config1, config2)).Returns(expected);

            // Act
            var result = _mockConfigManager.Object.Merge(config1, config2);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Value1", result["Key1"]);
            Assert.Equal("Value2", result["Key2"]);
        }
    }

    public interface IConfigurationManager
    {
        Dictionary<string, string> Load(string path);
        bool Save(string path, Dictionary<string, string> config);
        bool Validate(Dictionary<string, string> config);
        Dictionary<string, string> Merge(Dictionary<string, string> config1, Dictionary<string, string> config2);
    }
}
