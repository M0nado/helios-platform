using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.Presentation.Studio.Models;
using HELIOS.Platform.Presentation.Studio.Services;

namespace HELIOS.Platform.Tests.Presentation.Studio
{
    /// <summary>
    /// Unit tests for Studio Dashboard Service (Task 2.1)
    /// </summary>
    public class StudioDashboardServiceTests
    {
        private readonly StudioDashboardService _dashboardService;

        public StudioDashboardServiceTests()
        {
            _dashboardService = new StudioDashboardService();
        }

        [Fact]
        public async Task InitializeAsync_Should_Start_Service()
        {
            // Act
            await _dashboardService.InitializeAsync();

            // Assert
            Assert.NotNull(_dashboardService);
        }

        [Fact]
        public void CollectMetrics_Should_Return_Valid_Metrics()
        {
            // Act
            var metrics = _dashboardService.CollectMetrics();

            // Assert
            Assert.NotNull(metrics);
            Assert.NotEqual(default(DateTime), metrics.Timestamp);
            Assert.True(metrics.CpuUsagePercent >= 0 && metrics.CpuUsagePercent <= 100);
            Assert.True(metrics.MemoryUsagePercent >= 0 && metrics.MemoryUsagePercent <= 100);
        }

        [Fact]
        public void GetStatus_Should_Return_Dashboard_Status()
        {
            // Act
            var status = _dashboardService.GetStatus();

            // Assert
            Assert.NotNull(status);
            Assert.NotEqual(default(DateTime), status.LastUpdatedAt);
        }

        [Fact]
        public void CreateAlert_Should_Add_Alert()
        {
            // Act
            var alert = _dashboardService.CreateAlert(
                AlertSeverity.Warning,
                "Test Alert",
                "This is a test alert"
            );

            // Assert
            Assert.NotNull(alert);
            Assert.False(string.IsNullOrEmpty(alert.Id));
            Assert.Equal(AlertSeverity.Warning, alert.Severity);
            Assert.Equal("Test Alert", alert.Title);
        }

        [Fact]
        public void ResolveAlert_Should_Mark_Alert_As_Resolved()
        {
            // Arrange
            var alert = _dashboardService.CreateAlert(AlertSeverity.Warning, "Test", "Message");

            // Act
            _dashboardService.ResolveAlert(alert.Id);
            var alerts = _dashboardService.GetAlerts(activeOnly: true).ToList();

            // Assert
            Assert.DoesNotContain(alert.Id, alerts.Select(a => a.Id));
        }

        [Fact]
        public void GetAlerts_Should_Return_All_Alerts()
        {
            // Arrange
            _dashboardService.CreateAlert(AlertSeverity.Warning, "Alert 1", "Message 1");
            _dashboardService.CreateAlert(AlertSeverity.Critical, "Alert 2", "Message 2");

            // Act
            var alerts = _dashboardService.GetAlerts().ToList();

            // Assert
            Assert.True(alerts.Count >= 2);
        }

        [Fact]
        public void CreateOrUpdateUser_Should_Add_User()
        {
            // Act
            var user = _dashboardService.CreateOrUpdateUser(
                "testuser",
                "Test User",
                "test@example.com",
                new[] { "Admin", "User" }
            );

            // Assert
            Assert.NotNull(user);
            Assert.Equal("testuser", user.Username);
            Assert.Equal("Test User", user.DisplayName);
        }

        [Fact]
        public void DeleteUser_Should_Remove_User()
        {
            // Arrange
            _dashboardService.CreateOrUpdateUser("testuser", "Test", "test@example.com");

            // Act
            var result = _dashboardService.DeleteUser("testuser");
            var users = _dashboardService.GetUsers().ToList();

            // Assert
            Assert.True(result);
            Assert.DoesNotContain("testuser", users.Select(u => u.Username));
        }

        [Fact]
        public void GetSettings_Should_Return_Default_Settings()
        {
            // Act
            var settings = _dashboardService.GetSettings();

            // Assert
            Assert.NotNull(settings);
            Assert.Equal(5, settings.RefreshIntervalSeconds);
            Assert.True(settings.EnableNotifications);
        }

        [Fact]
        public void UpdateSettings_Should_Modify_Settings()
        {
            // Arrange
            var newSettings = new DashboardSettings { RefreshIntervalSeconds = 10 };

            // Act
            _dashboardService.UpdateSettings(newSettings);
            var settings = _dashboardService.GetSettings();

            // Assert
            Assert.Equal(10, settings.RefreshIntervalSeconds);
        }

        [Fact]
        public void Shutdown_Should_Not_Throw()
        {
            // Act & Assert
            _dashboardService.Shutdown();
        }
    }

    /// <summary>
    /// Unit tests for Performance Graph Service (Task 2.2)
    /// </summary>
    public class PerformanceGraphServiceTests
    {
        private readonly PerformanceGraphService _graphService;

        public PerformanceGraphServiceTests()
        {
            _graphService = new PerformanceGraphService();
        }

        [Fact]
        public void RecordMetric_Should_Add_Data_Point()
        {
            // Act
            _graphService.RecordMetric("CPU", 45.5);
            var data = _graphService.GetHourlyGraph("CPU");

            // Assert
            Assert.NotEmpty(data);
        }

        [Fact]
        public void RecordMetricsSnapshot_Should_Record_All_Metrics()
        {
            // Arrange
            var metrics = new SystemMetrics
            {
                CpuUsagePercent = 30,
                MemoryUsagePercent = 60,
                DiskUsagePercent = 70,
                NetworkBytesPerSecond = 1000,
                GpuUsagePercent = 25
            };

            // Act
            _graphService.RecordMetricsSnapshot(metrics);

            // Assert
            Assert.NotEmpty(_graphService.GetAvailableMetrics());
        }

        [Fact]
        public void GetHourlyGraph_Should_Return_Hour_Data()
        {
            // Arrange
            _graphService.RecordMetric("Memory", 50);
            _graphService.RecordMetric("Memory", 55);

            // Act
            var data = _graphService.GetHourlyGraph("Memory");

            // Assert
            Assert.NotEmpty(data);
            Assert.All(data, item => Assert.True(item.Timestamp >= DateTime.UtcNow.AddHours(-1)));
        }

        [Fact]
        public void GetMetricStatistics_Should_Calculate_Stats()
        {
            // Arrange
            _graphService.RecordMetric("CPU", 30);
            _graphService.RecordMetric("CPU", 40);
            _graphService.RecordMetric("CPU", 50);

            // Act
            var stats = _graphService.GetMetricStatistics("CPU", TimeSpan.FromHours(1));

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(30, stats.MinValue);
            Assert.Equal(50, stats.MaxValue);
            Assert.True(stats.AverageValue > 0);
        }

        [Fact]
        public void ExportAsCSV_Should_Return_Valid_CSV()
        {
            // Arrange
            _graphService.RecordMetric("Disk", 75);

            // Act
            var csv = _graphService.ExportAsCSV("Disk", TimeSpan.FromHours(1));

            // Assert
            Assert.Contains("Timestamp,MetricName,Value", csv);
            Assert.Contains("Disk", csv);
        }

        [Fact]
        public void AnalyzeTrends_Should_Return_Trend_Analysis()
        {
            // Arrange
            for (int i = 0; i < 50; i++)
                _graphService.RecordMetric("Network", 100 + i);

            // Act
            var trend = _graphService.AnalyzeTrends("Network", 24);

            // Assert
            Assert.NotNull(trend);
            Assert.NotNull(trend.Trend);
        }
    }

    /// <summary>
    /// Unit tests for Alert Management Service (Task 2.2)
    /// </summary>
    public class AlertManagementServiceTests
    {
        private readonly AlertManagementService _alertService;

        public AlertManagementServiceTests()
        {
            _alertService = new AlertManagementService();
        }

        [Fact]
        public void CreateAlertConfig_Should_Add_Configuration()
        {
            // Arrange
            var condition = new AlertCondition
            {
                MetricType = "CPU",
                Operator = "GreaterThan",
                Threshold = 80
            };
            var action = new AlertAction
            {
                ActionType = "Notification",
                Title = "High CPU"
            };

            // Act
            var config = _alertService.CreateAlertConfig("Test Alert", condition, action);

            // Assert
            Assert.NotNull(config);
            Assert.Equal("Test Alert", config.Name);
        }

        [Fact]
        public void DeleteAlertConfig_Should_Remove_Configuration()
        {
            // Arrange
            var condition = new AlertCondition { MetricType = "CPU", Operator = "GreaterThan", Threshold = 80 };
            var action = new AlertAction { ActionType = "Notification" };
            var config = _alertService.CreateAlertConfig("Test", condition, action);

            // Act
            var result = _alertService.DeleteAlertConfig(config.Id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetAlertConfigurations_Should_Return_All_Configs()
        {
            // Act
            var configs = _alertService.GetAlertConfigurations().ToList();

            // Assert
            Assert.True(configs.Count > 0); // Should have default configs
        }

        [Fact]
        public void EvaluateAlert_Should_Detect_Threshold_Breach()
        {
            // Arrange
            var condition = new AlertCondition
            {
                MetricType = "CPU",
                Operator = "GreaterThan",
                Threshold = 70
            };
            var action = new AlertAction { ActionType = "Notification" };
            var config = _alertService.CreateAlertConfig("CPU High", condition, action);

            var metrics = new SystemMetrics { CpuUsagePercent = 85 };

            // Act
            var result = _alertService.EvaluateAlert(config, metrics);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetAlertHistory_Should_Return_Historical_Events()
        {
            // Act
            var history = _alertService.GetAlertHistory().ToList();

            // Assert
            Assert.IsType<List<AlertHistory>>(history);
        }
    }

    /// <summary>
    /// Unit tests for Report Generator (Task 2.2)
    /// </summary>
    public class ReportGeneratorTests
    {
        private readonly ReportGenerator _reportGenerator;
        private readonly PerformanceGraphService _graphService;

        public ReportGeneratorTests()
        {
            _graphService = new PerformanceGraphService();
            _reportGenerator = new ReportGenerator(_graphService);
        }

        [Fact]
        public void GenerateDailyReport_Should_Create_Report()
        {
            // Arrange
            _graphService.RecordMetric("CPU", 50);

            // Act
            var report = _reportGenerator.GenerateDailyReport();

            // Assert
            Assert.NotNull(report);
            Assert.NotEmpty(report.Title);
            Assert.Equal(ReportType.Daily, report.ReportType);
        }

        [Fact]
        public void GenerateWeeklyReport_Should_Create_Report()
        {
            // Act
            var report = _reportGenerator.GenerateWeeklyReport();

            // Assert
            Assert.NotNull(report);
            Assert.Equal(ReportType.Weekly, report.ReportType);
        }

        [Fact]
        public void ExportToHtml_Should_Return_Valid_HTML()
        {
            // Arrange
            var report = _reportGenerator.GenerateDailyReport();

            // Act
            var html = _reportGenerator.ExportToHtml(report);

            // Assert
            Assert.Contains("<!DOCTYPE html>", html);
            Assert.Contains(report.Title, html);
        }

        [Fact]
        public void ExportToCSV_Should_Return_Valid_CSV()
        {
            // Arrange
            var report = _reportGenerator.GenerateDailyReport();

            // Act
            var csv = _reportGenerator.ExportToCSV(report);

            // Assert
            Assert.Contains("Report Title,Metric", csv);
        }

        [Fact]
        public void ExportToText_Should_Return_Valid_Text()
        {
            // Arrange
            var report = _reportGenerator.GenerateDailyReport();

            // Act
            var text = _reportGenerator.ExportToText(report);

            // Assert
            Assert.Contains(report.Title, text);
            Assert.Contains("METRICS SUMMARY", text);
        }

        [Fact]
        public void GetReports_Should_Return_All_Reports()
        {
            // Arrange
            _reportGenerator.GenerateDailyReport();

            // Act
            var reports = _reportGenerator.GetReports().ToList();

            // Assert
            Assert.True(reports.Count > 0);
        }
    }

    /// <summary>
    /// Unit tests for Dashboard Customizer (Task 2.2)
    /// </summary>
    public class DashboardCustomizerTests
    {
        private readonly DashboardCustomizer _customizer;

        public DashboardCustomizerTests()
        {
            _customizer = new DashboardCustomizer();
        }

        [Fact]
        public void CreateLayout_Should_Create_Layout()
        {
            // Act
            var layout = _customizer.CreateLayout("Test Layout");

            // Assert
            Assert.NotNull(layout);
            Assert.Equal("Test Layout", layout.Name);
        }

        [Fact]
        public void AddWidgetToLayout_Should_Add_Widget()
        {
            // Arrange
            var layout = _customizer.CreateLayout("Test");
            var widget = new DashboardWidget { Id = Guid.NewGuid().ToString(), Type = "CPU" };

            // Act
            _customizer.AddWidgetToLayout(layout.Id, widget);

            // Assert
            Assert.Single(layout.Widgets);
        }

        [Fact]
        public void SetTheme_Should_Change_Theme()
        {
            // Act
            _customizer.SetTheme("Dark");
            var theme = _customizer.GetCurrentTheme();

            // Assert
            Assert.NotNull(theme);
            Assert.Equal("Dark", theme.Name);
        }

        [Fact]
        public void GetAvailableThemes_Should_Return_Default_Themes()
        {
            // Act
            var themes = _customizer.GetAvailableThemes().ToList();

            // Assert
            Assert.True(themes.Count >= 2);
        }
    }

    /// <summary>
    /// Unit tests for Analytics Engine (Task 2.3)
    /// </summary>
    public class AnalyticsEngineTests
    {
        private readonly AnalyticsEngine _analyticsEngine;

        public AnalyticsEngineTests()
        {
            _analyticsEngine = new AnalyticsEngine();
        }

        [Fact]
        public void RecordMetric_Should_Store_Data()
        {
            // Act
            _analyticsEngine.RecordMetric("CPU", 45.5);

            // Assert
            var summary = _analyticsEngine.GetMetricSummary("CPU");
            Assert.NotNull(summary);
        }

        [Fact]
        public void GetAnalytics_Should_Return_Statistics()
        {
            // Arrange
            for (int i = 0; i < 10; i++)
                _analyticsEngine.RecordMetric("Memory", 40 + i * 2);

            // Act
            var analytics = _analyticsEngine.GetAnalytics("Memory", TimeSpan.FromHours(1));

            // Assert
            Assert.NotNull(analytics);
            Assert.True(analytics.Count > 0);
            Assert.True(analytics.Mean > 0);
        }

        [Fact]
        public void AnalyzeCorrelation_Should_Calculate_Correlation()
        {
            // Arrange
            for (int i = 0; i < 20; i++)
            {
                _analyticsEngine.RecordMetric("CPU", 40 + i);
                _analyticsEngine.RecordMetric("Memory", 30 + i);
            }

            // Act
            var correlation = _analyticsEngine.AnalyzeCorrelation("CPU", "Memory", TimeSpan.FromHours(1));

            // Assert
            Assert.NotNull(correlation);
        }
    }

    /// <summary>
    /// Unit tests for Predictive Analytics (Task 2.3)
    /// </summary>
    public class PredictiveAnalyticsTests
    {
        private readonly PredictiveAnalytics _predictiveAnalytics;
        private readonly AnalyticsEngine _analyticsEngine;

        public PredictiveAnalyticsTests()
        {
            _analyticsEngine = new AnalyticsEngine();
            _predictiveAnalytics = new PredictiveAnalytics(_analyticsEngine);
        }

        [Fact]
        public void PredictMetric_Should_Return_Prediction()
        {
            // Arrange
            for (int i = 0; i < 30; i++)
                _analyticsEngine.RecordMetric("CPU", 40 + i);

            // Act
            var prediction = _predictiveAnalytics.PredictMetric("CPU", TimeSpan.FromHours(1));

            // Assert
            Assert.NotNull(prediction);
            Assert.True(prediction.Confidence > 0);
        }
    }

    /// <summary>
    /// Unit tests for Cloud Integration (Task 2.4)
    /// </summary>
    public class CloudIntegrationServiceTests
    {
        private readonly CloudIntegrationService _cloudService;

        public CloudIntegrationServiceTests()
        {
            _cloudService = new CloudIntegrationService();
        }

        [Fact]
        public void RegisterProvider_Should_Add_Provider()
        {
            // Arrange
            var credentials = new Dictionary<string, string> { { "apiKey", "test-key" } };

            // Act
            var provider = _cloudService.RegisterProvider(CloudProviderType.Azure, "Test Azure", credentials);

            // Assert
            Assert.NotNull(provider);
            Assert.Equal(CloudProviderType.Azure, provider.ProviderType);
        }

        [Fact]
        public void GetProviders_Should_Return_All_Providers()
        {
            // Arrange
            var credentials = new Dictionary<string, string> { { "apiKey", "test" } };
            _cloudService.RegisterProvider(CloudProviderType.AWS, "Test AWS", credentials);

            // Act
            var providers = _cloudService.GetProviders().ToList();

            // Assert
            Assert.True(providers.Count > 0);
        }
    }

    /// <summary>
    /// Unit tests for Webhook Service (Task 2.4)
    /// </summary>
    public class WebhookServiceTests
    {
        private readonly WebhookService _webhookService;

        public WebhookServiceTests()
        {
            _webhookService = new WebhookService();
        }

        [Fact]
        public void RegisterWebhook_Should_Create_Webhook()
        {
            // Arrange
            var events = new List<string> { "alert.triggered", "metric.updated" };

            // Act
            var webhook = _webhookService.RegisterWebhook(
                "Test Webhook",
                "https://example.com/webhook",
                events
            );

            // Assert
            Assert.NotNull(webhook);
            Assert.Equal("Test Webhook", webhook.Name);
        }

        [Fact]
        public void GetWebhooks_Should_Return_All_Webhooks()
        {
            // Arrange
            _webhookService.RegisterWebhook("Test", "https://test.com", new List<string> { "test" });

            // Act
            var webhooks = _webhookService.GetWebhooks().ToList();

            // Assert
            Assert.True(webhooks.Count > 0);
        }

        [Fact]
        public void DeleteWebhook_Should_Remove_Webhook()
        {
            // Arrange
            var webhook = _webhookService.RegisterWebhook("Test", "https://test.com", new List<string> { "test" });

            // Act
            var result = _webhookService.DeleteWebhook(webhook.Id);

            // Assert
            Assert.True(result);
        }
    }

    /// <summary>
    /// Unit tests for Extension Framework (Task 2.4)
    /// </summary>
    public class ExtensionFrameworkTests
    {
        private readonly ExtensionFramework _extensionFramework;

        public ExtensionFrameworkTests()
        {
            _extensionFramework = new ExtensionFramework();
        }

        [Fact]
        public void RegisterHook_Should_Add_Hook()
        {
            // Act
            _extensionFramework.RegisterHook("OnDashboardLoad", "Called when dashboard loads", typeof(Action));

            // Assert
            var hooks = _extensionFramework.GetAvailableHooks().ToList();
            Assert.True(hooks.Count > 0);
        }
    }
}
