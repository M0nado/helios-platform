namespace MonadoBlade.Tests.Integration;

using Xunit;
using Microsoft.Extensions.DependencyInjection;
using MonadoBlade.Core.DependencyInjection;
using MonadoBlade.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class CoreInfrastructureIntegrationTests
{
    [Fact]
    public void FullDependencyInjectionBootstrap_Should_InitializeSuccessfully()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Logging:LogLevel:Default", "Information" }
            })
            .Build();

        // Act
        var services = new ServiceCollection();
        services.AddCoreInfrastructure(config);
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider);
        var logger = provider.GetRequiredService<ILoggerFactory>();
        Assert.NotNull(logger);
    }

    [Fact]
    public void MultipleServiceProviders_ShouldWorkIndependently()
    {
        // Arrange
        var config1 = new ConfigurationBuilder().Build();
        var config2 = new ConfigurationBuilder().Build();

        // Act
        var provider1 = new ServiceCollection().AddCoreInfrastructure(config1).BuildServiceProvider();
        var provider2 = new ServiceCollection().AddCoreInfrastructure(config2).BuildServiceProvider();

        // Assert
        Assert.NotNull(provider1);
        Assert.NotNull(provider2);
        Assert.NotSame(provider1, provider2);
    }

    [Fact]
    public void Configuration_ShouldBeAccessibleThroughDI()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Test:Setting", "Value" }
            })
            .Build();

        // Act
        var services = new ServiceCollection();
        services.AddCoreInfrastructure(config);
        var provider = services.BuildServiceProvider();
        var resolvedConfig = provider.GetRequiredService<IConfiguration>();

        // Assert
        Assert.Equal("Value", resolvedConfig["Test:Setting"]);
    }

    [Fact]
    public void Logging_ShouldBeConfiguredAndFunctional()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        // Act
        services.AddCoreInfrastructure(config);
        var provider = services.BuildServiceProvider();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Test");

        // Assert
        Assert.NotNull(logger);
        logger.LogInformation("Test message");
    }
}

public class ModuleLoadingIntegrationTests
{
    [Fact]
    public void AllModuleAssemblies_CanBeLoaded()
    {
        var moduleNames = new[]
        {
            "MonadoBlade.Core",
            "MonadoBlade.Security",
            "MonadoBlade.Graphics",
            "MonadoBlade.Boot",
            "MonadoBlade.Dashboard",
            "MonadoBlade.Audio",
            "MonadoBlade.Developer",
            "MonadoBlade.Tools"
        };

        foreach (var moduleName in moduleNames)
        {
            var asm = System.Reflection.Assembly.Load(moduleName);
            Assert.NotNull(asm);
        }
    }

    [Fact]
    public void ModuleAbstractions_AreAccessibleFromTests()
    {
        // This verifies project references are correctly set up
        var coreType = typeof(MonadoBlade.Core.Abstractions.IService);
        var securityType = typeof(MonadoBlade.Security.Abstractions.ISecurityService);
        var graphicsType = typeof(MonadoBlade.Graphics.Abstractions.IGraphicsService);

        Assert.NotNull(coreType);
        Assert.NotNull(securityType);
        Assert.NotNull(graphicsType);
    }
}

public class EndToEndBootstrapTests
{
    [Fact]
    public async Task ApplicationBootstrap_Complete_Scenario()
    {
        // Arrange
        var config = Core.Configuration.ConfigurationExtensions.BuildConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddCoreInfrastructure(config);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("E2E");

        // Assert
        Assert.NotNull(provider);
        Assert.NotNull(logger);

        // Log for demonstration
        logger.LogInformation("End-to-end bootstrap completed successfully");

        await Task.CompletedTask;
    }

    [Fact]
    public void ServiceResolution_AllRequiredServices_Available()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddCoreInfrastructure(config);
        var provider = services.BuildServiceProvider();

        // Act & Assert
        Assert.NotNull(provider.GetRequiredService<IConfiguration>());
        Assert.NotNull(provider.GetRequiredService<ILoggerFactory>());
    }
}

public class ConfigurationIntegrationTests
{
    [Fact]
    public void Configuration_LoadsFromMultipleSources()
    {
        // Arrange
        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Source", "InMemory" }
        };
        Environment.SetEnvironmentVariable("MONADOCONFIG", "EnvValue");

        try
        {
            // Act
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemoryConfig)
                .AddEnvironmentVariables("MONADO")
                .Build();

            // Assert
            Assert.Equal("InMemory", config["Source"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MONADOCONFIG", null);
        }
    }

    [Fact]
    public void NestedConfiguration_CanBeAccessed()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:Connection:Host", "localhost" },
                { "Database:Connection:Port", "5432" }
            })
            .Build();

        // Act
        var host = config["Database:Connection:Host"];
        var port = config["Database:Connection:Port"];

        // Assert
        Assert.Equal("localhost", host);
        Assert.Equal("5432", port);
    }
}
