namespace MonadoBlade.Tests.Performance.Benchmarks;

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonadoBlade.Core.DependencyInjection;
using ConfigExt = MonadoBlade.Core.Configuration.ConfigurationExtensions;

/// <summary>
/// Benchmarks for core MonadoBlade modules.
/// Measures performance of DI container, configuration, and service resolution.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, targetCount: 5)]
[ThreadingDiagnoser]
public class CoreModuleBenchmarks
{
    private IConfiguration _config = null!;
    private IServiceProvider _serviceProvider = null!;

    [GlobalSetup]
    public void Setup()
    {
        _config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddCoreInfrastructure(_config);
        _serviceProvider = services.BuildServiceProvider();
    }

    [Benchmark(Description = "ServiceProvider creation")]
    public IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddCoreInfrastructure(_config);
        return services.BuildServiceProvider();
    }

    [Benchmark(Description = "Single service resolution")]
    public object? ResolveSingleService()
    {
        return _serviceProvider.GetService<IConfiguration>();
    }

    [Benchmark(Description = "Logger factory resolution")]
    public ILoggerFactory ResolveLoggerFactory()
    {
        return _serviceProvider.GetRequiredService<ILoggerFactory>();
    }

    [Benchmark(Description = "Create logger from factory")]
    public ILogger CreateLogger()
    {
        var factory = _serviceProvider.GetRequiredService<ILoggerFactory>();
        return factory.CreateLogger("Benchmark");
    }

    [Benchmark(Description = "Configuration building")]
    public IConfiguration BuildConfiguration()
    {
        return ConfigExt.BuildConfiguration();
    }

    [Benchmark(Description = "Configuration with environment")]
    public IConfiguration BuildConfigurationWithEnvironment()
    {
        return ConfigExt.BuildConfiguration(environment: "Development");
    }

    [Benchmark(Description = "Multiple service resolution")]
    public int MultipleServiceResolution()
    {
        int count = 0;
        for (int i = 0; i < 100; i++)
        {
            _ = _serviceProvider.GetRequiredService<IConfiguration>();
            count++;
        }
        return count;
    }

    [Benchmark(Description = "Concurrent service resolution")]
    public int ConcurrentServiceResolution()
    {
        int count = 0;
        var tasks = new Task[Environment.ProcessorCount];

        for (int t = 0; t < tasks.Length; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    _ = _serviceProvider.GetRequiredService<IConfiguration>();
                    Interlocked.Increment(ref count);
                }
            });
        }

        Task.WaitAll(tasks);
        return count;
    }

    [Benchmark(Description = "Scoped service creation")]
    public IServiceProvider CreateScopedProvider()
    {
        return _serviceProvider.CreateScope().ServiceProvider;
    }

    [Benchmark(Description = "Service resolution from scope")]
    public int ResolveFromScope()
    {
        int count = 0;
        for (int i = 0; i < 10; i++)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                _ = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                count++;
            }
        }
        return count;
    }
}
