namespace MonadoBlade.GUI;

using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MonadoBlade.Core.DependencyInjection;
using ConfigExt = MonadoBlade.Core.Configuration.ConfigurationExtensions;
using Serilog;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Configure application
        var config = ConfigExt.BuildConfiguration();
        var services = new ServiceCollection();
        services.AddCoreInfrastructure(config);
        _serviceProvider = services.BuildServiceProvider();

        var logger = _serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<App>();
        logger.LogInformation("MonadoBlade application started");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        (_serviceProvider as IAsyncDisposable)?.DisposeAsync().GetAwaiter().GetResult();
        base.OnExit(e);
    }
}
