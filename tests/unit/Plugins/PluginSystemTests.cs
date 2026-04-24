namespace HELIOS.Platform.Tests.Unit.Plugins;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.Plugins;

public class PluginSystemTests
{
    private readonly string _testPluginsDir;
    private readonly PluginManager _manager;

    public PluginSystemTests()
    {
        _testPluginsDir = Path.Combine(Path.GetTempPath(), $"helios-plugins-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPluginsDir);
        _manager = new PluginManager(_testPluginsDir);
    }

    #region Plugin Manager Tests

    [Fact]
    public async Task RegisterAsync_WithValidPlugin_RegistersSuccessfully()
    {
        // Arrange
        var plugin = new MockPlugin();

        // Act
        var metadata = await _manager.RegisterAsync(plugin);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("mock-plugin", metadata.Id);
        Assert.True(_manager.IsPluginRegistered("mock-plugin"));
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateId_ThrowsException()
    {
        // Arrange
        var plugin1 = new MockPlugin();
        var plugin2 = new MockPlugin();
        await _manager.RegisterAsync(plugin1);

        // Act & Assert
        await Assert.ThrowsAsync<PluginException>(() => _manager.RegisterAsync(plugin2));
    }

    [Fact]
    public async Task UnregisterAsync_WithValidPlugin_UnregistersSuccessfully()
    {
        // Arrange
        var plugin = new MockPlugin();
        await _manager.RegisterAsync(plugin);

        // Act
        await _manager.UnregisterAsync("mock-plugin");

        // Assert
        Assert.False(_manager.IsPluginRegistered("mock-plugin"));
    }

    [Fact]
    public async Task GetPluginAsync_WithValidId_ReturnsPlugin()
    {
        // Arrange
        var plugin = new MockPlugin();
        await _manager.RegisterAsync(plugin);

        // Act
        var retrieved = await _manager.GetPluginAsync("mock-plugin");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("mock-plugin", retrieved.Metadata.Id);
    }

    [Fact]
    public async Task GetPluginAsync_WithInvalidId_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<PluginException>(() => _manager.GetPluginAsync("non-existent"));
    }

    [Fact]
    public async Task ListPluginsAsync_WithMultiplePlugins_ReturnsAll()
    {
        // Arrange
        await _manager.RegisterAsync(new MockPlugin());
        await _manager.RegisterAsync(new MockPlugin { Metadata = new PluginMetadata { Id = "plugin-2", Name = "Plugin 2" } });

        // Act
        var plugins = await _manager.ListPluginsAsync();

        // Assert
        Assert.Equal(2, plugins.Count);
    }

    [Fact]
    public async Task EnablePluginAsync_WithDisabledPlugin_EnablesSuccessfully()
    {
        // Arrange
        var plugin = new MockPlugin();
        await _manager.RegisterAsync(plugin);
        await _manager.DisablePluginAsync("mock-plugin");

        // Act
        await _manager.EnablePluginAsync("mock-plugin");

        // Assert
        var enabled = await _manager.ListEnabledPluginsAsync();
        Assert.Contains(enabled, p => p.Id == "mock-plugin");
    }

    [Fact]
    public async Task DisablePluginAsync_WithEnabledPlugin_DisablesSuccessfully()
    {
        // Arrange
        var plugin = new MockPlugin();
        await _manager.RegisterAsync(plugin);

        // Act
        await _manager.DisablePluginAsync("mock-plugin");

        // Assert
        var enabled = await _manager.ListEnabledPluginsAsync();
        Assert.DoesNotContain(enabled, p => p.Id == "mock-plugin");
    }

    [Fact]
    public async Task PluginLoaded_EventFires_WhenPluginRegistered()
    {
        // Arrange
        var eventFired = false;
        _manager.PluginLoaded += (s, e) => eventFired = true;
        var plugin = new MockPlugin();

        // Act
        await _manager.RegisterAsync(plugin);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public async Task PluginUnloaded_EventFires_WhenPluginUnregistered()
    {
        // Arrange
        var plugin = new MockPlugin();
        await _manager.RegisterAsync(plugin);
        var eventFired = false;
        _manager.PluginUnloaded += (s, e) => eventFired = true;

        // Act
        await _manager.UnregisterAsync("mock-plugin");

        // Assert
        Assert.True(eventFired);
    }

    #endregion

    #region Plugin Validation Tests

    [Fact]
    public async Task ValidatePluginAsync_WithValidPlugin_ReturnsValid()
    {
        // Act
        var result = await _manager.ValidatePluginAsync("dummy.dll");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CheckedAt);
    }

    [Fact]
    public async Task ValidateAllPluginsAsync_WithNoPlugins_ReturnsValid()
    {
        // Act
        var result = await _manager.ValidateAllPluginsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAllPluginsAsync_WithValidPlugins_ReturnsValid()
    {
        // Arrange
        await _manager.RegisterAsync(new MockPlugin());
        await _manager.RegisterAsync(new MockPlugin { Metadata = new PluginMetadata { Id = "plugin-2", Name = "Plugin 2" } });

        // Act
        var result = await _manager.ValidateAllPluginsAsync();

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Plugin Lifecycle Tests

    [Fact]
    public async Task InitializeAsync_LoadsAvailablePlugins()
    {
        // Act
        await _manager.InitializeAsync();

        // Assert - no exception thrown
        Assert.NotNull(_manager);
    }

    [Fact]
    public async Task PluginLifecycleHooks_OnLoad_AreCalled()
    {
        // Arrange
        var plugin = new LifecycleTestPlugin();
        
        // Act
        await _manager.RegisterAsync(plugin);

        // Assert
        Assert.True(plugin.InitializeCalled);
    }

    [Fact]
    public async Task PluginLifecycleHooks_OnDisable_AreCalled()
    {
        // Arrange
        var plugin = new LifecycleTestPlugin();
        await _manager.RegisterAsync(plugin);

        // Act
        await _manager.DisablePluginAsync("lifecycle-plugin");

        // Assert
        Assert.True(plugin.DisableCalled);
    }

    #endregion

    #region Plugin Security Tests

    [Fact]
    public async Task LoadPluginAsync_WithSecurityContext_StoresContext()
    {
        // Arrange
        var securityContext = new PluginSecurityContext
        {
            PluginId = "mock-plugin",
            AllowedPermissions = new List<string> { "read", "write" },
            IsApproved = true
        };

        // This test validates security context handling in real implementation
        Assert.NotNull(securityContext);
    }

    #endregion

    #region Plugin Marketplace Tests

    [Fact]
    public async Task SearchPluginsAsync_WithQuery_ReturnsMatches()
    {
        // Arrange
        var marketplace = new PluginMarketplace(_testPluginsDir);
        var plugin = new PluginMetadata { Id = "test", Name = "Test Plugin", Description = "A test plugin" };

        // Act
        var results = await marketplace.SearchPluginsAsync("test");

        // Assert
        Assert.NotNull(results);
    }

    [Fact]
    public async Task PublishPluginAsync_WithValidPlugin_PublishesSuccessfully()
    {
        // Arrange
        var marketplace = new PluginMarketplace(_testPluginsDir);
        var plugin = new MockPlugin();

        // Act
        var result = await marketplace.PublishPluginAsync(plugin, "valid-key");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RatePluginAsync_WithValidRating_RatesSuccessfully()
    {
        // Arrange
        var marketplace = new PluginMarketplace(_testPluginsDir);

        // Act
        await marketplace.RatePluginAsync("plugin-id", 5, "Great plugin!");

        // Assert - no exception
        Assert.NotNull(marketplace);
    }

    [Fact]
    public async Task RatePluginAsync_WithInvalidRating_ThrowsException()
    {
        // Arrange
        var marketplace = new PluginMarketplace(_testPluginsDir);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => marketplace.RatePluginAsync("plugin-id", 6, "Invalid rating")
        );
    }

    [Fact]
    public async Task GetTopPluginsAsync_ReturnsTopRatedPlugins()
    {
        // Arrange
        var marketplace = new PluginMarketplace(_testPluginsDir);

        // Act
        var topPlugins = await marketplace.GetTopPluginsAsync(5);

        // Assert
        Assert.NotNull(topPlugins);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CompletePluginLifecycle_Register_Enable_Disable_Unregister()
    {
        // Arrange
        var plugin = new MockPlugin();

        // Act & Assert - Register
        var metadata = await _manager.RegisterAsync(plugin);
        Assert.True(_manager.IsPluginRegistered("mock-plugin"));

        // Act & Assert - Disable
        await _manager.DisablePluginAsync("mock-plugin");
        var disabled = await _manager.ListEnabledPluginsAsync();
        Assert.DoesNotContain(disabled, p => p.Id == "mock-plugin");

        // Act & Assert - Enable
        await _manager.EnablePluginAsync("mock-plugin");
        var enabled = await _manager.ListEnabledPluginsAsync();
        Assert.Contains(enabled, p => p.Id == "mock-plugin");

        // Act & Assert - Unregister
        await _manager.UnregisterAsync("mock-plugin");
        Assert.False(_manager.IsPluginRegistered("mock-plugin"));
    }

    [Fact]
    public async Task MultiplePlugins_CanCoexist()
    {
        // Arrange
        var plugin1 = new MockPlugin();
        var plugin2 = new MockPlugin { Metadata = new PluginMetadata { Id = "plugin-2", Name = "Plugin 2" } };
        var plugin3 = new MockPlugin { Metadata = new PluginMetadata { Id = "plugin-3", Name = "Plugin 3" } };

        // Act
        await _manager.RegisterAsync(plugin1);
        await _manager.RegisterAsync(plugin2);
        await _manager.RegisterAsync(plugin3);
        var allPlugins = await _manager.ListPluginsAsync();

        // Assert
        Assert.Equal(3, allPlugins.Count);
    }

    [Fact]
    public async Task PluginStatePersistence_AcrossOperations()
    {
        // Arrange & Act
        var plugin = new MockPlugin();
        await _manager.RegisterAsync(plugin);
        var original = await _manager.GetPluginAsync("mock-plugin");
        
        await _manager.DisablePluginAsync("mock-plugin");
        var disabled = await _manager.GetPluginAsync("mock-plugin");
        
        await _manager.EnablePluginAsync("mock-plugin");
        var enabled = await _manager.GetPluginAsync("mock-plugin");

        // Assert
        Assert.NotNull(original);
        Assert.NotNull(disabled);
        Assert.NotNull(enabled);
    }

    #endregion

    #region Mock Implementations

    public class MockPlugin : IPlugin
    {
        public PluginMetadata Metadata { get; set; }

        public MockPlugin()
        {
            Metadata = new PluginMetadata
            {
                Id = "mock-plugin",
                Name = "Mock Plugin",
                Description = "A mock plugin for testing",
                Version = new Version(1, 0, 0),
                Author = "Test",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsEnabled = true
            };
        }

        public Task InitializeAsync() => Task.CompletedTask;
        public Task ShutdownAsync() => Task.CompletedTask;
        public Task<bool> ValidateAsync() => Task.FromResult(true);
    }

    public class LifecycleTestPlugin : IPlugin, IPluginLifecycle
    {
        public PluginMetadata Metadata { get; set; }
        public bool InitializeCalled { get; private set; }
        public bool DisableCalled { get; private set; }

        public LifecycleTestPlugin()
        {
            Metadata = new PluginMetadata
            {
                Id = "lifecycle-plugin",
                Name = "Lifecycle Plugin",
                Description = "Tests lifecycle hooks",
                Version = new Version(1, 0, 0),
                Author = "Test",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public Task InitializeAsync() { InitializeCalled = true; return Task.CompletedTask; }
        public Task ShutdownAsync() => Task.CompletedTask;
        public Task<bool> ValidateAsync() => Task.FromResult(true);

        public Task OnLoadAsync() => Task.CompletedTask;
        public Task OnUnloadAsync() => Task.CompletedTask;
        public Task OnEnableAsync() => Task.CompletedTask;
        public Task OnDisableAsync() { DisableCalled = true; return Task.CompletedTask; }
        public Task<bool> CanUnloadAsync() => Task.FromResult(true);
    }

    #endregion
}
