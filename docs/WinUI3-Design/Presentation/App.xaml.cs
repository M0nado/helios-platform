using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HELIOS.Platform.Core;
using HELIOS.Platform.BackendServices.ServerManagement;
using HELIOS.Platform.Core.Logging;

namespace HELIOS.Platform
{
    public partial class App : Application
    {
        public static MainWindow MainWindow { get; private set; }

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            try
            {
                // Initialize core services
                InitializeServices();

                // Create main window
                MainWindow = new MainWindow();
                MainWindow.Activate();
            }
            catch (Exception ex)
            {
                var logger = ServiceContainer.Instance.GetService<Core.Logging.ILogger>();
                logger?.Error("Failed to launch application", ex);
                throw;
            }
        }

        private void InitializeServices()
        {
            // Initialize logger
            var logger = new ConsoleLogger();
            ServiceContainer.Instance.RegisterSingleton<Core.Logging.ILogger>(logger);

            // Initialize service orchestrator
            var orchestrator = new ServiceOrchestrator();
            ServiceContainer.Instance.RegisterSingleton<IServiceOrchestrator>(orchestrator);

            logger.Info("HELIOS Platform services initialized");
        }
    }
}
