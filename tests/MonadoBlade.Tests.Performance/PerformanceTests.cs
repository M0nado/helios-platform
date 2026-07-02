namespace MonadoBlade.Tests.Performance;

using BenchmarkDotNet.Attributes;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MonadoBlade.Core.DependencyInjection;
using ConfigExt = MonadoBlade.Core.Configuration.ConfigurationExtensions;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3)]
public class DependencyInjectionPerformanceBenchmark
{
    private IConfiguration _config = null!;

    [GlobalSetup]
    public void Setup()
    {
        _config = new ConfigurationBuilder().Build();
    }

    [Benchmark]
    public IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddCoreInfrastructure(_config);
        return services.BuildServiceProvider();
    }

    [Benchmark]
    public object ResolveService()
    {
        var services = new ServiceCollection();
        services.AddCoreInfrastructure(_config);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ILoggerFactory>();
    }
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3)]
public class ConfigurationPerformanceBenchmark
{
    [Benchmark]
    public IConfiguration BuildConfiguration()
    {
        return ConfigExt.BuildConfiguration();
    }

    [Benchmark]
    public IConfiguration BuildConfigurationWithEnvironment()
    {
        return ConfigExt.BuildConfiguration(environment: "Development");
    }
}

public class PerformanceTests
{
    [Fact]
    public void ServiceProviderCreation_Should_Complete_QuicklyForBasic()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var services = new ServiceCollection();
            services.AddCoreInfrastructure(config);
            var provider = services.BuildServiceProvider();
        }
        sw.Stop();

        // Assert - 100 iterations should complete in reasonable time
        Assert.True(sw.ElapsedMilliseconds < 5000, $"Service provider creation took {sw.ElapsedMilliseconds}ms for 100 iterations");
    }

    [Fact]
    public void ConfigurationLoading_Should_BeEfficient()
    {
        // Arrange
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 50; i++)
        {
            _ = ConfigExt.BuildConfiguration();
        }
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < 2000, $"Configuration loading took {sw.ElapsedMilliseconds}ms for 50 iterations");
    }

    [Fact]
    public void ServiceResolution_Should_BeEfficient()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddCoreInfrastructure(config);
        var provider = services.BuildServiceProvider();

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            _ = provider.GetRequiredService<IConfiguration>();
        }
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < 500, $"Service resolution took {sw.ElapsedMilliseconds}ms for 1000 iterations");
    }

    [Fact]
    public void ConcurrentServiceResolution_Should_BeThreadSafe()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddCoreInfrastructure(config);
        var provider = services.BuildServiceProvider();

        var tasks = new System.Threading.Tasks.Task[10];
        var results = new object?[10];

        // Act
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                results[index] = provider.GetRequiredService<IConfiguration>();
            });
        }
        System.Threading.Tasks.Task.WaitAll(tasks);

        // Assert
        Assert.All(results, r => Assert.NotNull(r));
    }
}
