namespace MonadoBlade.Tests.Unit.Fixtures;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MonadoBlade.Core.DependencyInjection;
using Serilog;
using Xunit;

/// <summary>
/// Base fixture for all unit tests providing common DI and configuration setup.
/// </summary>
public abstract class BaseTestFixture : IAsyncLifetime
{
    protected IServiceProvider ServiceProvider { get; private set; } = null!;
    protected IConfiguration Configuration { get; private set; } = null!;
    protected IServiceCollection Services { get; private set; } = null!;

    public virtual Task InitializeAsync()
    {
        // Create configuration
        var configDict = new Dictionary<string, string?>
        {
            { "Logging:LogLevel:Default", "Debug" },
            { "ApplicationName", "MonadoBlade.Tests" }
        };
        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        // Setup DI
        Services = new ServiceCollection();
        Services.AddCoreInfrastructure(Configuration, "MonadoBlade.Tests");
        ServiceProvider = Services.BuildServiceProvider();

        return Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        if (ServiceProvider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (ServiceProvider is IDisposable disposable)
            disposable.Dispose();

        Log.CloseAndFlush();
    }

    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    protected T? GetServiceOptional<T>() where T : notnull
    {
        return ServiceProvider.GetService<T>();
    }
}
