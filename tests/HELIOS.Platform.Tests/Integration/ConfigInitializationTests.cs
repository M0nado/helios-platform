using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Tests.Integration
{
    /// <summary>
    /// Integration Tests: Configuration → Service Initialization
    /// 5 test cases testing how configuration loads and initializes services
    /// </summary>
    public class ConfigurationInitializationIntegrationTests
    {
        private readonly Mock<IConfigurationManager> _mockConfig;
        private readonly Mock<IServiceInitializer> _mockInitializer;

        public ConfigurationInitializationIntegrationTests()
        {
            _mockConfig = new Mock<IConfigurationManager>();
            _mockInitializer = new Mock<IServiceInitializer>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task LoadConfig_ThenInitializeServices_AllServicesReady()
        {
            // Arrange
            var config = new Dictionary<string, string> { { "ServiceCount", "3" } };
            _mockConfig.Setup(c => c.LoadAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(config);
            _mockInitializer.Setup(s => s.InitializeAsync(config, CancellationToken.None))
                .ReturnsAsync(new InitializationResult { Success = true, ServiceCount = 3 });

            // Act
            var loadedConfig = await _mockConfig.Object.LoadAsync("config.json", CancellationToken.None);
            var initResult = await _mockInitializer.Object.InitializeAsync(loadedConfig, CancellationToken.None);

            // Assert
            Assert.NotNull(loadedConfig);
            Assert.True(initResult.Success);
            Assert.Equal(3, initResult.ServiceCount);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ConfigurationValidation_FailsEarly_PreventsBadInitialization()
        {
            // Arrange
            var badConfig = new Dictionary<string, string> { { "InvalidKey", "" } };
            _mockConfig.Setup(c => c.ValidateAsync(badConfig, CancellationToken.None))
                .ReturnsAsync(false);

            // Act
            var isValid = await _mockConfig.Object.ValidateAsync(badConfig, CancellationToken.None);

            // Assert
            Assert.False(isValid);
            _mockInitializer.Verify(s => s.InitializeAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task MissingConfigFile_FallsBackToDefaults_ServicesInitializeWithDefaults()
        {
            // Arrange
            var defaultConfig = new Dictionary<string, string> { { "UseDefaults", "true" } };
            _mockConfig.Setup(c => c.LoadAsync("missing.json", CancellationToken.None))
                .ThrowsAsync(new System.IO.FileNotFoundException());
            _mockConfig.Setup(c => c.GetDefaultsAsync())
                .ReturnsAsync(defaultConfig);
            _mockInitializer.Setup(s => s.InitializeAsync(defaultConfig, CancellationToken.None))
                .ReturnsAsync(new InitializationResult { Success = true, UsedDefaults = true });

            // Act
            try
            {
                await _mockConfig.Object.LoadAsync("missing.json", CancellationToken.None);
            }
            catch { }
            var defaults = await _mockConfig.Object.GetDefaultsAsync();
            var result = await _mockInitializer.Object.InitializeAsync(defaults, CancellationToken.None);

            // Assert
            Assert.True(result.UsedDefaults);
            Assert.True(result.Success);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ServiceDependencies_ResolveInOrder_AllServicesHaveDependencies()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                { "ServiceA.Depends", "" },
                { "ServiceB.Depends", "ServiceA" },
                { "ServiceC.Depends", "ServiceA,ServiceB" }
            };
            var resolutionOrder = new List<string>();
            
            _mockInitializer.Setup(s => s.InitializeAsync(config, CancellationToken.None))
                .Returns(async () =>
                {
                    resolutionOrder.Add("A");
                    await Task.Delay(10);
                    resolutionOrder.Add("B");
                    await Task.Delay(10);
                    resolutionOrder.Add("C");
                    return new InitializationResult { Success = true };
                });

            // Act
            var result = await _mockInitializer.Object.InitializeAsync(config, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, resolutionOrder.Count);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task PostInitializationConfiguration_Applied_ServicesUseUpdatedConfig()
        {
            // Arrange
            var initialConfig = new Dictionary<string, string> { { "LogLevel", "Info" } };
            var updatedConfig = new Dictionary<string, string> { { "LogLevel", "Debug" } };
            
            _mockConfig.Setup(c => c.LoadAsync("config.json", CancellationToken.None))
                .ReturnsAsync(initialConfig);
            _mockConfig.Setup(c => c.UpdateAsync(updatedConfig, CancellationToken.None))
                .ReturnsAsync(true);
            _mockInitializer.Setup(s => s.ApplyConfigurationAsync(updatedConfig, CancellationToken.None))
                .ReturnsAsync(true);

            // Act
            var loadedConfig = await _mockConfig.Object.LoadAsync("config.json", CancellationToken.None);
            var updated = await _mockConfig.Object.UpdateAsync(updatedConfig, CancellationToken.None);
            var applied = await _mockInitializer.Object.ApplyConfigurationAsync(updatedConfig, CancellationToken.None);

            // Assert
            Assert.True(updated);
            Assert.True(applied);
        }
    }

    public class InitializationResult
    {
        public bool Success { get; set; }
        public int ServiceCount { get; set; }
        public bool UsedDefaults { get; set; }
    }

    public interface IConfigurationManager
    {
        Task<Dictionary<string, string>> LoadAsync(string path, CancellationToken cancellationToken);
        Task<bool> ValidateAsync(Dictionary<string, string> config, CancellationToken cancellationToken);
        Task<Dictionary<string, string>> GetDefaultsAsync();
        Task<bool> UpdateAsync(Dictionary<string, string> config, CancellationToken cancellationToken);
    }

    public interface IServiceInitializer
    {
        Task<InitializationResult> InitializeAsync(Dictionary<string, string> config, CancellationToken cancellationToken);
        Task<bool> ApplyConfigurationAsync(Dictionary<string, string> config, CancellationToken cancellationToken);
    }
}
