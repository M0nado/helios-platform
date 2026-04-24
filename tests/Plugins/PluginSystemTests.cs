// ═══════════════════════════════════════════════════════════════════════════
// Plugin System Comprehensive Test Suite
// Tests for loading, security, registry, and UI components (45+ tests)
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using HELIOS.Platform.Plugins.Interfaces;
using HELIOS.Platform.Plugins.Loader;
using HELIOS.Platform.Plugins.Registry;
using HELIOS.Platform.Plugins.UI;

namespace HELIOS.Tests.Plugins
{
    /// <summary>
    /// Tests for plugin interfaces and base classes
    /// </summary>
    public class PluginInterfaceTests
    {
        [Fact]
        public void PluginAttribute_WithValidParameters_CreatesInstance()
        {
            var attr = new PluginAttribute(
                "test-plugin",
                "Test Plugin",
                "1.0.0",
                "Test Author",
                "Test Description",
                PluginCategory.Utility,
                new[] { "dependency1" });

            Assert.Equal("test-plugin", attr.Id);
            Assert.Equal("Test Plugin", attr.Name);
            Assert.Equal("1.0.0", attr.Version);
            Assert.Equal("Test Author", attr.Author);
            Assert.Equal("Test Description", attr.Description);
            Assert.Equal(PluginCategory.Utility, attr.Category);
            Assert.Single(attr.Dependencies);
        }

        [Fact]
        public void PluginAttribute_WithoutDependencies_CreatesEmptyArray()
        {
            var attr = new PluginAttribute("id", "name", "1.0", "author", "desc");
            Assert.Empty(attr.Dependencies);
        }

        [Fact]
        public void PluginStatus_Enum_ContainsAllStates()
        {
            Assert.True(Enum.GetNames(typeof(PluginStatus)).Length >= 8);
        }

        [Fact]
        public void PluginCategory_Enum_ContainsAllCategories()
        {
            Assert.True(Enum.GetNames(typeof(PluginCategory)).Length >= 8);
        }

        [Fact]
        public void AnalysisOptions_WithDefaults_HasCorrectValues()
        {
            var options = new AnalysisOptions();
            Assert.True(options.IncludeDetailedMetrics);
            Assert.False(options.EnableComparisonWithBaseline);
            Assert.Equal(300, options.TimeoutSeconds);
        }

        [Fact]
        public void OptimizationOptions_WithDefaults_HasCorrectValues()
        {
            var options = new OptimizationOptions();
            Assert.Equal(OptimizationLevel.Moderate, options.Level);
            Assert.True(options.DryRun);
            Assert.False(options.AllowAutoApply);
            Assert.Equal(600, options.TimeoutSeconds);
        }

        [Fact]
        public void AnalysisFinding_WithData_ContainsAllProperties()
        {
            var finding = new AnalysisFinding
            {
                Id = "f1",
                Title = "Test Finding",
                Severity = FindingSeverity.Warning,
                Category = "Security"
            };

            Assert.Equal("f1", finding.Id);
            Assert.Equal("Test Finding", finding.Title);
            Assert.Equal(FindingSeverity.Warning, finding.Severity);
            Assert.Equal("Security", finding.Category);
        }

        [Fact]
        public void OptimizationRecommendation_WithData_ContainsAllProperties()
        {
            var rec = new OptimizationRecommendation
            {
                Id = "r1",
                Title = "Test Recommendation",
                Level = OptimizationLevel.Aggressive,
                EstimatedImpactPercent = 25.5
            };

            Assert.Equal("r1", rec.Id);
            Assert.Equal("Test Recommendation", rec.Title);
            Assert.Equal(OptimizationLevel.Aggressive, rec.Level);
            Assert.Equal(25.5, rec.EstimatedImpactPercent);
        }

        [Fact]
        public void PluginStatusChangedEventArgs_WithData_ContainsAllData()
        {
            var now = DateTime.UtcNow;
            var args = new PluginStatusChangedEventArgs
            {
                PluginId = "test",
                OldStatus = PluginStatus.NotLoaded,
                NewStatus = PluginStatus.Loaded,
                Message = "Loaded successfully"
            };

            Assert.Equal("test", args.PluginId);
            Assert.Equal(PluginStatus.NotLoaded, args.OldStatus);
            Assert.Equal(PluginStatus.Loaded, args.NewStatus);
            Assert.Equal("Loaded successfully", args.Message);
        }
    }

    /// <summary>
    /// Tests for plugin loader functionality
    /// </summary>
    public class PluginLoaderTests : IDisposable
    {
        private readonly string _testPluginDirectory;
        private readonly Mock<ILogger<PluginLoader>> _mockLogger;
        private readonly Mock<IPluginSecurityValidator> _mockSecurityValidator;
        private readonly PluginLoader _loader;

        public PluginLoaderTests()
        {
            _testPluginDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testPluginDirectory);
            _mockLogger = new Mock<ILogger<PluginLoader>>();
            _mockSecurityValidator = new Mock<IPluginSecurityValidator>();
            _mockSecurityValidator
                .Setup(x => x.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _loader = new PluginLoader(_testPluginDirectory, _mockLogger.Object, _mockSecurityValidator.Object);
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            Assert.NotNull(_loader);
        }

        [Fact]
        public void Constructor_WithNullDirectory_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PluginLoader(null, _mockLogger.Object, _mockSecurityValidator.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PluginLoader(_testPluginDirectory, null, _mockSecurityValidator.Object));
        }

        [Fact]
        public void Constructor_WithNullValidator_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PluginLoader(_testPluginDirectory, _mockLogger.Object, null));
        }

        [Fact]
        public void EnsurePluginDirectoryExists_CreatesDirectory()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Assert.False(Directory.Exists(tempPath));

            var loader = new PluginLoader(tempPath, _mockLogger.Object, _mockSecurityValidator.Object);
            Assert.True(Directory.Exists(tempPath));

            Directory.Delete(tempPath);
        }

        [Fact]
        public async Task DiscoverPluginsAsync_WithNoPlugins_ReturnsEmptyList()
        {
            var results = await _loader.DiscoverPluginsAsync();
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task GetLoadedPlugins_WithoutLoading_ReturnsEmpty()
        {
            var plugins = _loader.GetLoadedPlugins();
            Assert.NotNull(plugins);
            Assert.Empty(plugins);
        }

        [Fact]
        public void IsPluginLoaded_WithNonExistentPlugin_ReturnsFalse()
        {
            Assert.False(_loader.IsPluginLoaded("non-existent"));
        }

        [Fact]
        public async Task UnloadPluginAsync_WithNonExistentPlugin_ReturnsFalse()
        {
            var result = await _loader.UnloadPluginAsync("non-existent");
            Assert.False(result);
        }

        public void Dispose()
        {
            _loader?.Dispose();
            if (Directory.Exists(_testPluginDirectory))
            {
                Directory.Delete(_testPluginDirectory, true);
            }
        }
    }

    /// <summary>
    /// Tests for plugin security validator
    /// </summary>
    public class PluginSecurityValidatorTests
    {
        private readonly Mock<ILogger<PluginSecurityValidator>> _mockLogger;
        private readonly PluginSecurityValidator _validator;

        public PluginSecurityValidatorTests()
        {
            _mockLogger = new Mock<ILogger<PluginSecurityValidator>>();
            _validator = new PluginSecurityValidator(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithLogger_CreatesInstance()
        {
            Assert.NotNull(_validator);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new PluginSecurityValidator(null));
        }

        [Fact]
        public async Task VerifySignatureAsync_WithNonExistentFile_ReturnsFalse()
        {
            var result = await _validator.VerifySignatureAsync("/non/existent/file.dll", "signature");
            Assert.False(result);
        }

        [Fact]
        public async Task VerifySignatureAsync_WithMismatchedSignature_ReturnsFalse()
        {
            // Create a temporary file
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "test content");
                var result = await _validator.VerifySignatureAsync(tempFile, "wrongsignature");
                Assert.False(result);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void AddTrustedPublisher_WithValidName_AddsPublisher()
        {
            _validator.AddTrustedPublisher("CustomPublisher");
            var publishers = _validator.GetTrustedPublishers();
            Assert.Contains("CustomPublisher", publishers);
        }

        [Fact]
        public void RemoveTrustedPublisher_WithValidName_RemovesPublisher()
        {
            _validator.AddTrustedPublisher("ToRemove");
            _validator.RemoveTrustedPublisher("ToRemove");
            var publishers = _validator.GetTrustedPublishers();
            Assert.DoesNotContain("ToRemove", publishers);
        }

        [Fact]
        public void BanNamespace_WithValidNamespace_BansNamespace()
        {
            _validator.BanNamespace("CustomNamespace");
            var banned = _validator.GetBannedNamespaces();
            Assert.Contains("CustomNamespace", banned);
        }

        [Fact]
        public void GetTrustedPublishers_InitiallyContainsDefaults()
        {
            var publishers = _validator.GetTrustedPublishers();
            Assert.NotEmpty(publishers);
        }

        [Fact]
        public void GetBannedNamespaces_InitiallyContainsDefaults()
        {
            var banned = _validator.GetBannedNamespaces();
            Assert.NotEmpty(banned);
        }
    }

    /// <summary>
    /// Tests for plugin registry
    /// </summary>
    public class PluginRegistryTests
    {
        private readonly Mock<ILogger<PluginRegistry>> _mockLogger;
        private readonly PluginRegistry _registry;
        private readonly Mock<IPlugin> _mockPlugin;
        private readonly PluginMetadata _mockMetadata;

        public PluginRegistryTests()
        {
            _mockLogger = new Mock<ILogger<PluginRegistry>>();
            _registry = new PluginRegistry(_mockLogger.Object);
            _mockPlugin = new Mock<IPlugin>();
            _mockPlugin.Setup(x => x.Id).Returns("test-plugin");
            _mockPlugin.Setup(x => x.Name).Returns("Test Plugin");
            _mockPlugin.Setup(x => x.Version).Returns("1.0.0");
            _mockPlugin.Setup(x => x.Author).Returns("Test Author");
            _mockPlugin.Setup(x => x.Description).Returns("Test Description");
            _mockPlugin.Setup(x => x.Category).Returns(PluginCategory.Utility);
            _mockPlugin.Setup(x => x.Dependencies).Returns(new List<string>());
            _mockMetadata = new PluginMetadata
            {
                Id = "test-plugin",
                Name = "Test Plugin",
                Version = "1.0.0",
                Author = "Test Author",
                Description = "Test Description",
                Category = PluginCategory.Utility,
                Dependencies = new List<string>(),
                IsEnabled = true
            };
        }

        [Fact]
        public void RegisterPlugin_WithValidPlugin_AddsToRegistry()
        {
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            var plugins = _registry.GetAllPlugins();
            Assert.Single(plugins);
        }

        [Fact]
        public void RegisterPlugin_WithNullPluginId_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _registry.RegisterPlugin(null, _mockPlugin.Object, _mockMetadata));
        }

        [Fact]
        public void RegisterPlugin_WithNullPlugin_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _registry.RegisterPlugin("test", null, _mockMetadata));
        }

        [Fact]
        public void RegisterPlugin_WithNullMetadata_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _registry.RegisterPlugin("test", _mockPlugin.Object, null));
        }

        [Fact]
        public void RegisterPlugin_WithDuplicate_ThrowsException()
        {
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            Assert.Throws<InvalidOperationException>(() =>
                _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata));
        }

        [Fact]
        public void UnregisterPlugin_WithValidId_RemovesFromRegistry()
        {
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            _registry.UnregisterPlugin("test-plugin");
            var plugins = _registry.GetAllPlugins();
            Assert.Empty(plugins);
        }

        [Fact]
        public void UnregisterPlugin_WithNonExistentId_ThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                _registry.UnregisterPlugin("non-existent"));
        }

        [Fact]
        public void SetPluginStatus_WithValidId_ChangesStatus()
        {
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            _registry.SetPluginStatus("test-plugin", PluginStatus.Running);
            var entry = _registry.GetRegistrySnapshot()["test-plugin"];
            Assert.Equal(PluginStatus.Running, entry.Status);
        }

        [Fact]
        public void EnablePlugin_WithValidId_EnablesPlugin()
        {
            _mockMetadata.IsEnabled = false;
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            _registry.EnablePlugin("test-plugin");
            var entry = _registry.GetRegistrySnapshot()["test-plugin"];
            Assert.True(entry.IsEnabled);
        }

        [Fact]
        public void DisablePlugin_WithValidId_DisablesPlugin()
        {
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            _registry.DisablePlugin("test-plugin");
            var entry = _registry.GetRegistrySnapshot()["test-plugin"];
            Assert.False(entry.IsEnabled);
        }

        [Fact]
        public void GetPluginMetadata_WithValidId_ReturnsMetadata()
        {
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            var metadata = _registry.GetPluginMetadata("test-plugin");
            Assert.NotNull(metadata);
            Assert.Equal("test-plugin", metadata.Id);
        }

        [Fact]
        public void GetPluginMetadata_WithInvalidId_ReturnsNull()
        {
            var metadata = _registry.GetPluginMetadata("non-existent");
            Assert.Null(metadata);
        }

        [Fact]
        public void GetPluginDependencies_WithValidId_ReturnsList()
        {
            _mockMetadata.Dependencies = new List<string> { "dep1", "dep2" };
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            var deps = _registry.GetPluginDependencies("test-plugin");
            Assert.Equal(2, deps.Count);
        }

        [Fact]
        public void GetDependentPlugins_WithValidId_ReturnsList()
        {
            _mockMetadata.Dependencies = new List<string> { "dep-plugin" };
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            var dependents = _registry.GetDependentPlugins("dep-plugin");
            Assert.Contains("test-plugin", dependents);
        }

        [Fact]
        public void CanLoadPlugin_WithAllDependenciesLoaded_ReturnsTrue()
        {
            // Register dependency
            var depMetadata = new PluginMetadata
            {
                Id = "dep-plugin",
                Name = "Dep Plugin",
                Version = "1.0.0",
                Author = "Author",
                Description = "Desc",
                Category = PluginCategory.General,
                Dependencies = new List<string>(),
                IsEnabled = true
            };
            var depPlugin = new Mock<IPlugin>();
            depPlugin.Setup(x => x.Id).Returns("dep-plugin");
            depPlugin.Setup(x => x.Dependencies).Returns(new List<string>());

            _registry.RegisterPlugin("dep-plugin", depPlugin.Object, depMetadata);

            // Register plugin with dependency
            _mockMetadata.Dependencies = new List<string> { "dep-plugin" };
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            _registry.SetPluginStatus("dep-plugin", PluginStatus.Running);

            var canLoad = _registry.CanLoadPlugin("test-plugin");
            Assert.True(canLoad);
        }

        [Fact]
        public void CanLoadPlugin_WithMissingDependencies_ReturnsFalse()
        {
            _mockMetadata.Dependencies = new List<string> { "missing-dep" };
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            var canLoad = _registry.CanLoadPlugin("test-plugin");
            Assert.False(canLoad);
        }

        [Fact]
        public void SetPluginConfiguration_WithValidKey_StoresValue()
        {
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            _registry.SetPluginConfiguration("test-plugin", "key1", "value1");
            var value = _registry.GetPluginConfiguration("test-plugin", "key1");
            Assert.Equal("value1", value);
        }

        [Fact]
        public void GetPluginConfiguration_WithNonExistentKey_ReturnsNull()
        {
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            var value = _registry.GetPluginConfiguration("test-plugin", "nonexistent");
            Assert.Null(value);
        }

        [Fact]
        public void ClearRegistry_RemovesAllPlugins()
        {
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            _registry.ClearRegistry();
            var plugins = _registry.GetAllPlugins();
            Assert.Empty(plugins);
        }

        [Fact]
        public void PluginRegistered_EventFires_WhenPluginAdded()
        {
            var eventFired = false;
            _registry.PluginRegistered += (s, e) => { eventFired = true; };
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            Assert.True(eventFired);
        }

        [Fact]
        public void PluginStatusChanged_EventFires_WhenStatusChanges()
        {
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            var eventFired = false;
            _registry.PluginStatusChanged += (s, e) => { eventFired = true; };
            _registry.SetPluginStatus("test-plugin", PluginStatus.Running);
            Assert.True(eventFired);
        }

        [Fact]
        public void PluginConfigurationChanged_EventFires_WhenConfigChanges()
        {
            _registry.RegisterPlugin("test-plugin", _mockPlugin.Object, _mockMetadata);
            var eventFired = false;
            _registry.PluginConfigurationChanged += (s, e) => { eventFired = true; };
            _registry.SetPluginConfiguration("test-plugin", "key1", "value1");
            Assert.True(eventFired);
        }
    }

    /// <summary>
    /// Tests for plugin marketplace
    /// </summary>
    public class PluginMarketplaceTests
    {
        private readonly Mock<IPluginLoader> _mockLoader;
        private readonly Mock<IPluginRegistry> _mockRegistry;
        private readonly Mock<ILogger<PluginMarketplace>> _mockLogger;
        private readonly PluginMarketplace _marketplace;

        public PluginMarketplaceTests()
        {
            _mockLoader = new Mock<IPluginLoader>();
            _mockRegistry = new Mock<IPluginRegistry>();
            _mockLogger = new Mock<ILogger<PluginMarketplace>>();
            _marketplace = new PluginMarketplace(_mockLoader.Object, _mockRegistry.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            Assert.NotNull(_marketplace);
        }

        [Fact]
        public void Constructor_WithNullLoader_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PluginMarketplace(null, _mockRegistry.Object, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullRegistry_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PluginMarketplace(_mockLoader.Object, null, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PluginMarketplace(_mockLoader.Object, _mockRegistry.Object, null));
        }

        [Fact]
        public async Task BrowseAvailablePluginsAsync_WithNoPlugins_ReturnsEmpty()
        {
            _mockLoader.Setup(x => x.DiscoverPluginsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<PluginDiscoveryResult>());

            var results = await _marketplace.BrowseAvailablePluginsAsync();
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task BrowseAvailablePluginsAsync_WithCategory_FiltersResults()
        {
            var discoveries = new[]
            {
                new PluginDiscoveryResult
                {
                    PluginId = "ui-plugin",
                    Name = "UI Plugin",
                    Version = "1.0",
                    Author = "Author",
                    Description = "UI",
                    Category = PluginCategory.UI
                },
                new PluginDiscoveryResult
                {
                    PluginId = "analyzer",
                    Name = "Analyzer",
                    Version = "1.0",
                    Author = "Author",
                    Description = "Analysis",
                    Category = PluginCategory.Analyzer
                }
            };

            _mockLoader.Setup(x => x.DiscoverPluginsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(discoveries);
            _mockRegistry.Setup(x => x.GetPluginMetadata(It.IsAny<string>()))
                .Returns((IPluginMetadata)null);

            var results = await _marketplace.BrowseAvailablePluginsAsync(category: "UI");
            Assert.Single(results);
        }

        [Fact]
        public async Task BrowseAvailablePluginsAsync_WithSearchTerm_FiltersResults()
        {
            var discoveries = new[]
            {
                new PluginDiscoveryResult
                {
                    PluginId = "plugin1",
                    Name = "Security Plugin",
                    Version = "1.0",
                    Author = "Author",
                    Description = "Security",
                    Category = PluginCategory.Security
                }
            };

            _mockLoader.Setup(x => x.DiscoverPluginsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(discoveries);
            _mockRegistry.Setup(x => x.GetPluginMetadata(It.IsAny<string>()))
                .Returns((IPluginMetadata)null);

            var results = await _marketplace.BrowseAvailablePluginsAsync(searchTerm: "Security");
            Assert.Single(results);
        }

        [Fact]
        public async Task GetInstalledPluginsAsync_ReturnsInstalledPlugins()
        {
            var mockPlugin = new Mock<IPlugin>();
            mockPlugin.Setup(x => x.Id).Returns("installed-plugin");
            mockPlugin.Setup(x => x.Name).Returns("Installed Plugin");
            mockPlugin.Setup(x => x.Version).Returns("1.0");
            mockPlugin.Setup(x => x.Author).Returns("Author");
            mockPlugin.Setup(x => x.Description).Returns("Desc");
            mockPlugin.Setup(x => x.Category).Returns(PluginCategory.Utility);

            _mockRegistry.Setup(x => x.GetAllPlugins())
                .Returns(new Dictionary<string, IPlugin> { { "installed-plugin", mockPlugin.Object } });

            var results = await _marketplace.GetInstalledPluginsAsync();
            Assert.Single(results);
        }
    }

    /// <summary>
    /// Integration tests for the plugin system
    /// </summary>
    public class PluginSystemIntegrationTests
    {
        [Fact]
        public void DefaultPluginContext_WithPlugin_CreatesInstance()
        {
            var mockPlugin = new Mock<IPlugin>();
            var context = new DefaultPluginContext(mockPlugin.Object);
            Assert.NotNull(context);
        }

        [Fact]
        public void DefaultPluginLogger_LogInformation_ExecutesWithoutError()
        {
            var logger = new DefaultPluginLogger();
            logger.LogInformation("Test {0}", "message");
            // If we get here, no exception was thrown
            Assert.True(true);
        }

        [Fact]
        public void DefaultPluginLogger_LogWarning_ExecutesWithoutError()
        {
            var logger = new DefaultPluginLogger();
            logger.LogWarning("Test warning");
            Assert.True(true);
        }

        [Fact]
        public void DefaultPluginLogger_LogError_ExecutesWithoutError()
        {
            var logger = new DefaultPluginLogger();
            logger.LogError("Test error", new Exception("test"));
            Assert.True(true);
        }

        [Fact]
        public void DefaultPluginLogger_LogDebug_ExecutesWithoutError()
        {
            var logger = new DefaultPluginLogger();
            logger.LogDebug("Debug message");
            Assert.True(true);
        }

        [Fact]
        public void DefaultPluginLogger_LogTrace_ExecutesWithoutError()
        {
            var logger = new DefaultPluginLogger();
            logger.LogTrace("Trace message");
            Assert.True(true);
        }

        [Fact]
        public void PluginLoadContext_WithValidName_CreatesInstance()
        {
            var context = new PluginLoadContext
            {
                ContextName = "test-context",
                IsolationEnabled = true
            };
            Assert.Equal("test-context", context.ContextName);
            Assert.True(context.IsolationEnabled);
        }

        [Fact]
        public void AnalysisResult_WithData_ContainsAllProperties()
        {
            var result = new AnalysisResult
            {
                AnalyzerId = "analyzer1",
                IsSuccessful = true,
                ExecutionTime = TimeSpan.FromMilliseconds(100)
            };
            Assert.Equal("analyzer1", result.AnalyzerId);
            Assert.True(result.IsSuccessful);
            Assert.Equal(100, result.ExecutionTime.TotalMilliseconds);
        }

        [Fact]
        public void OptimizationResult_WithData_ContainsAllProperties()
        {
            var result = new OptimizationResult
            {
                OptimizerId = "opt1",
                IsSuccessful = true,
                EstimatedImprovementPercent = 15.5
            };
            Assert.Equal("opt1", result.OptimizerId);
            Assert.True(result.IsSuccessful);
            Assert.Equal(15.5, result.EstimatedImprovementPercent);
        }

        [Fact]
        public void PluginListingViewModel_WithData_ContainsAllProperties()
        {
            var listing = new PluginListingViewModel
            {
                PluginId = "plugin1",
                Name = "Test Plugin",
                Version = "1.0",
                IsInstalled = false
            };
            Assert.Equal("plugin1", listing.PluginId);
            Assert.Equal("Test Plugin", listing.Name);
            Assert.Equal("1.0", listing.Version);
            Assert.False(listing.IsInstalled);
        }

        [Fact]
        public void PluginConfigurationViewModel_WithData_ContainsAllProperties()
        {
            var config = new PluginConfigurationViewModel
            {
                PluginId = "plugin1",
                ConfigurationSettings = new Dictionary<string, object> { { "key1", "value1" } }
            };
            Assert.Equal("plugin1", config.PluginId);
            Assert.Single(config.ConfigurationSettings);
        }
    }
}
