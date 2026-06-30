using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Logging;
using HELIOS.Platform.Phase10.BootEnvironment;
using Xunit;

namespace HELIOS.Platform.Tests.Phase10.BootEnvironment
{
    public sealed class Channel3BootTimeAutomationOrchestratorTests : IDisposable
    {
        private readonly string _root;
        private readonly string? _previousRoot;
        private readonly string? _previousSimulation;
        private readonly TestLogger _logger = new();

        public Channel3BootTimeAutomationOrchestratorTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "helios-boot-tests", Guid.NewGuid().ToString("N"));
            _previousRoot = Environment.GetEnvironmentVariable("HELIOS_BOOT_AUTOMATION_ROOT");
            _previousSimulation = Environment.GetEnvironmentVariable("HELIOS_BOOT_AUTOMATION_SIMULATION");
            Environment.SetEnvironmentVariable("HELIOS_BOOT_AUTOMATION_ROOT", _root);
            Environment.SetEnvironmentVariable("HELIOS_BOOT_AUTOMATION_SIMULATION", null);
        }

        [Fact]
        public async Task LoadAIProviderConfigAsync_WritesProviderConfigAndRealExecutionLog()
        {
            var orchestrator = new Channel3BootTimeAutomationOrchestrator(_logger);

            await orchestrator.LoadAIProviderConfigAsync(CancellationToken.None);

            var configPath = Path.Combine(_root, "config", "ai-providers.json");
            var config = await File.ReadAllTextAsync(configPath);
            Assert.Contains("Claude", config, StringComparison.Ordinal);
            Assert.Contains("GPT-4", config, StringComparison.Ordinal);
            Assert.Contains("[REAL]", _logger.JoinedMessages, StringComparison.Ordinal);
            Assert.DoesNotContain("[SIMULATION]", _logger.JoinedMessages, StringComparison.Ordinal);
        }

        [Fact]
        public async Task InitializeAIHubAsync_WhenRootPathIsAFile_PropagatesFailure()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_root)!);
            await File.WriteAllTextAsync(_root, "not a directory");
            var orchestrator = new Channel3BootTimeAutomationOrchestrator(_logger);

            await Assert.ThrowsAnyAsync<IOException>(() => orchestrator.InitializeAIHubAsync(CancellationToken.None));
        }

        [Fact]
        public async Task InitializeLearningDatabaseAsync_WhenCanceled_ThrowsOperationCanceledException()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var orchestrator = new Channel3BootTimeAutomationOrchestrator(_logger);

            await Assert.ThrowsAsync<OperationCanceledException>(() => orchestrator.InitializeLearningDatabaseAsync(cts.Token));
        }

        [Fact]
        public async Task ServiceAndSecurityOperations_InSimulationMode_WriteMarkersAndSimulationLogs()
        {
            Environment.SetEnvironmentVariable("HELIOS_BOOT_AUTOMATION_SIMULATION", "true");
            var orchestrator = new Channel3BootTimeAutomationOrchestrator(_logger);

            await orchestrator.StartServiceAsync("ExampleService", CancellationToken.None);
            await orchestrator.SetServiceAutoStartAsync("ExampleService", CancellationToken.None);
            await orchestrator.ApplySecureBootPolicyAsync(CancellationToken.None);
            await orchestrator.EnableCompressionAsync(Path.Combine(_root, "cache"), CancellationToken.None);

            Assert.True(File.Exists(Path.Combine(_root, "services", "ExampleService.start.simulated")));
            Assert.True(File.Exists(Path.Combine(_root, "services", "ExampleService.autostart.simulated")));
            Assert.True(File.Exists(Path.Combine(_root, "security", "secure-boot-policy.simulated")));
            Assert.True(File.Exists(Path.Combine(_root, "cache", ".compression.simulated")));
            Assert.Contains("[SIMULATION]", _logger.JoinedMessages, StringComparison.Ordinal);
        }

        [Fact]
        public async Task StartServiceAsync_WithoutSimulationOnNonWindows_FailsFast()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var orchestrator = new Channel3BootTimeAutomationOrchestrator(_logger);

            await Assert.ThrowsAsync<PlatformNotSupportedException>(() => orchestrator.StartServiceAsync("ExampleService", CancellationToken.None));
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("HELIOS_BOOT_AUTOMATION_ROOT", _previousRoot);
            Environment.SetEnvironmentVariable("HELIOS_BOOT_AUTOMATION_SIMULATION", _previousSimulation);
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
            else if (File.Exists(_root))
            {
                File.Delete(_root);
            }
        }

        private sealed class TestLogger : ILogger
        {
            private readonly List<string> _messages = new();
            public string JoinedMessages => string.Join(Environment.NewLine, _messages);
            public void Debug(string message) => _messages.Add(message);
            public void Info(string message) => _messages.Add(message);
            public void Warning(string message) => _messages.Add(message);
            public void Error(string message, Exception? ex = null) => _messages.Add(message);
            public void Critical(string message, Exception? ex = null) => _messages.Add(message);
        }
    }
}
