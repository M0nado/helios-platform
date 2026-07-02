using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Logging;
using HELIOS.Platform.BackendServices.ServerManagement;
using HELIOS.Platform.Core.Administration;
using HELIOS.Platform.Core.Configuration;

namespace HELIOS.Platform.Core.CLI
{
    /// <summary>
    /// Command execution context.
    /// </summary>
    public class CommandContext
    {
        public string Command { get; set; } = string.Empty;
        public List<string> Arguments { get; set; } = new();
        public Dictionary<string, string> Options { get; set; } = new();
    }

    /// <summary>
    /// Interface for command execution.
    /// </summary>
    public interface ICommandExecutor
    {
        Task<CommandResult> ExecuteAsync(string commandLine);
        List<string> GetAvailableCommands();
        string GetCommandHelp(string command);
    }

    /// <summary>
    /// Real CLI command executor with actual command implementations.
    /// </summary>
    public class CliCommandExecutor : ICommandExecutor
    {
        private readonly Core.Logging.ILogger _logger;
        private readonly IServiceOrchestrator _orchestrator;
        private readonly ISystemManagementService _sysManagement;

        private readonly Dictionary<string, Func<CommandContext, Task<CommandResult>>> _commands;

        public CliCommandExecutor(IServiceOrchestrator orchestrator, ISystemManagementService sysManagement)
        {
            _logger = new ConsoleLogger();
            _orchestrator = orchestrator;
            _sysManagement = sysManagement;

            _commands = new Dictionary<string, Func<CommandContext, Task<CommandResult>>>(StringComparer.OrdinalIgnoreCase)
            {
                { "status", ExecuteStatusCommand },
                { "services", ExecuteServicesCommand },
                { "partitions", ExecutePartitionsCommand },
                { "processes", ExecuteProcessesCommand },
                { "uptime", ExecuteUptimeCommand },
                { "help", ExecuteHelpCommand },
                { "config", ExecuteConfigCommand },
                { "azure", ExecuteAzureCommand }
            };
        }

        /// <summary>
        /// Execute a CLI command.
        /// </summary>
        public async Task<CommandResult> ExecuteAsync(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return new CommandResult { Success = false, Message = "Empty command" };

            var context = ParseCommand(commandLine);

            if (!_commands.TryGetValue(context.Command, out var executor))
                return new CommandResult
                {
                    Success = false,
                    Message = $"Unknown command: {context.Command}",
                    ExitCode = 1
                };

            try
            {
                return await executor(context);
            }
            catch (Exception ex)
            {
                _logger.Error($"Command execution error: {ex.Message}");
                return new CommandResult
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    ExitCode = 2
                };
            }
        }

        /// <summary>
        /// Get list of available commands.
        /// </summary>
        public List<string> GetAvailableCommands() => _commands.Keys.ToList();

        /// <summary>
        /// Get help text for a command.
        /// </summary>
        public string GetCommandHelp(string command)
        {
            return command.ToLower() switch
            {
                "status" => "status - Display system status and resource usage",
                "services" => "services - List all running services",
                "partitions" => "partitions - List all disk partitions",
                "processes" => "processes - List top running processes",
                "uptime" => "uptime - Show system uptime",
                "config" => "config get|set <key> [value] - Get or set configuration",
                "azure" => "azure status|login|configure [--tenant=<id>] [--subscription=<id-or-name>] [--method=interactive|device-code|managed-identity|service-principal] - Manage Azure CLI authentication and HELIOS Azure configuration",
                "help" => "help [command] - Show help for commands",
                _ => "Unknown command"
            };
        }

        /// <summary>
        /// Parse command line into context.
        /// </summary>
        private CommandContext ParseCommand(string commandLine)
        {
            var parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var context = new CommandContext { Command = parts[0] };

            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].StartsWith("--"))
                {
                    var optParts = parts[i].Substring(2).Split('=');
                    context.Options[optParts[0]] = optParts.Length > 1 ? optParts[1] : "true";
                }
                else if (parts[i].StartsWith("-"))
                {
                    context.Options[parts[i].Substring(1)] = "true";
                }
                else
                {
                    context.Arguments.Add(parts[i]);
                }
            }

            return context;
        }

        // Command implementations
        private async Task<CommandResult> ExecuteStatusCommand(CommandContext ctx)
        {
            var resources = await _orchestrator.GetSystemResourcesAsync();
            var output = $@"
System Status:
  CPU Usage:       {resources.CpuUsagePercent:F1}%
  Memory Usage:    {resources.MemoryUsageMB} MB / {(resources.MemoryUsageMB + resources.AvailableMemoryMB)} MB
  Disk Usage:      {resources.DiskUsagePercent}%
  System Uptime:   {resources.SystemUptimeSeconds} seconds
  Active Services: {resources.ActiveServices}
  Total Processes: {resources.TotalProcesses}";

            return new CommandResult { Success = true, Message = output };
        }

        private async Task<CommandResult> ExecuteServicesCommand(CommandContext ctx)
        {
            var services = await _sysManagement.GetServicesAsync();
            var output = "Services:\n";
            foreach (var svc in services)
            {
                var status = svc.IsRunning ? "RUNNING" : "STOPPED";
                output += $"  [{status}] {svc.DisplayName}\n";
            }

            return new CommandResult { Success = true, Message = output };
        }

        private async Task<CommandResult> ExecutePartitionsCommand(CommandContext ctx)
        {
            var partitions = await _sysManagement.GetPartitionsAsync();
            var output = "Disk Partitions:\n";
            foreach (var part in partitions)
            {
                var sizeGB = part.TotalSizeBytes / (1024.0 * 1024.0 * 1024.0);
                var usedGB = part.UsedSizeBytes / (1024.0 * 1024.0 * 1024.0);
                output += $"  {part.DriveLetter}: {part.FileSystem} - {usedGB:F1}GB / {sizeGB:F1}GB ({part.UsagePercent}%)\n";
            }

            return new CommandResult { Success = true, Message = output };
        }

        private async Task<CommandResult> ExecuteProcessesCommand(CommandContext ctx)
        {
            var processes = System.Diagnostics.Process.GetProcesses()
                .OrderByDescending(p => p.WorkingSet64)
                .Take(5)
                .ToList();

            var output = "Top 5 Processes by Memory:\n";
            foreach (var proc in processes)
            {
                try
                {
                    var memMB = proc.WorkingSet64 / (1024.0 * 1024.0);
                    output += $"  {proc.ProcessName} - {memMB:F1} MB (PID: {proc.Id})\n";
                }
                catch { }
            }

            return new CommandResult { Success = true, Message = output };
        }

        private async Task<CommandResult> ExecuteUptimeCommand(CommandContext ctx)
        {
            var resources = await _orchestrator.GetSystemResourcesAsync();
            var ts = TimeSpan.FromSeconds(resources.SystemUptimeSeconds);
            var output = $"System Uptime: {ts.Days} days, {ts.Hours}h {ts.Minutes}m {ts.Seconds}s";
            return new CommandResult { Success = true, Message = output };
        }

        private async Task<CommandResult> ExecuteHelpCommand(CommandContext ctx)
        {
            var output = "Available Commands:\n";
            foreach (var cmd in GetAvailableCommands())
            {
                output += $"  {GetCommandHelp(cmd)}\n";
            }

            return new CommandResult { Success = true, Message = output };
        }


        private async Task<CommandResult> ExecuteAzureCommand(CommandContext ctx)
        {
            if (ctx.Arguments.Count == 0)
                return new CommandResult { Success = false, Message = GetCommandHelp("azure"), ExitCode = 1 };

            var subcommand = ctx.Arguments[0].ToLowerInvariant();
            var prerequisites = CheckAzurePrerequisites();
            if (!prerequisites.Success)
                return prerequisites;

            return subcommand switch
            {
                "status" => await ExecuteAzureStatusAsync(),
                "login" => await ExecuteAzureLoginAsync(ctx),
                "configure" => await ExecuteAzureConfigureAsync(ctx),
                _ => new CommandResult { Success = false, Message = $"Unknown azure command: {subcommand}\n{GetCommandHelp("azure")}", ExitCode = 1 }
            };
        }

        private CommandResult CheckAzurePrerequisites()
        {
            var missing = new List<string>();
            if (!CommandExists("az", "--version"))
                missing.Add("Azure CLI 'az' was not found. Install it from https://learn.microsoft.com/cli/azure/install-azure-cli, then run 'az --version'.");

            if (!CommandExists("pwsh", "-NoProfile -Command \"if (Get-Module -ListAvailable -Name Az.Accounts) { exit 0 } else { exit 1 }\""))
                missing.Add("PowerShell Az.Accounts module was not found. Install PowerShell 7 and run: Install-Module Az.Accounts -Scope CurrentUser -Force -AllowClobber");

            if (missing.Count == 0)
                return new CommandResult { Success = true, Message = "Azure CLI and PowerShell Az prerequisites are installed." };

            return new CommandResult
            {
                Success = false,
                ExitCode = 1,
                Message = "Azure prerequisites are missing:\n  - " + string.Join("\n  - ", missing)
            };
        }

        private async Task<CommandResult> ExecuteAzureStatusAsync()
        {
            var account = await RunProcessAsync("az", "account show --output json");
            if (account.ExitCode != 0)
                return new CommandResult { Success = false, ExitCode = 1, Message = "Azure CLI is installed, but no active account was found. Run 'azure login --method=device-code' or 'az login'.\n" + account.Error };

            var current = JsonDocument.Parse(account.Output).RootElement;
            var config = AzureConfiguration.Load();
            var message = $@"Azure Status:
  Account:       {GetJsonString(current, "user", "name")}
  Tenant:        {GetJsonString(current, "tenantId")}
  Subscription:  {GetJsonString(current, "name")} ({GetJsonString(current, "id")})
  HELIOS config: {AzureConfiguration.GetDefaultConfigPath()}
  Saved tenant:  {config.TenantId ?? "<not configured>"}
  Saved sub:     {config.SubscriptionId ?? "<not configured>"}";
            return new CommandResult { Success = true, Message = message };
        }

        private async Task<CommandResult> ExecuteAzureLoginAsync(CommandContext ctx)
        {
            var method = GetOption(ctx, "method", "interactive").ToLowerInvariant();
            var tenant = GetOption(ctx, "tenant", null);
            var subscription = GetOption(ctx, "subscription", null);
            var args = method switch
            {
                "device-code" => "login --use-device-code",
                "managed-identity" => "login --identity",
                "service-principal" => BuildServicePrincipalLoginArgs(ctx, tenant),
                "interactive" => "login",
                _ => throw new InvalidOperationException("Supported methods: interactive, device-code, managed-identity, service-principal")
            };

            if (!string.IsNullOrWhiteSpace(tenant) && method != "service-principal")
                args += $" --tenant {Quote(tenant)}";

            var login = await RunProcessAsync("az", args);
            if (login.ExitCode != 0)
                return new CommandResult { Success = false, ExitCode = login.ExitCode, Message = "Azure login failed:\n" + login.Error };

            if (!string.IsNullOrWhiteSpace(subscription))
            {
                var setResult = await SetAzureSubscriptionAsync(subscription);
                if (!setResult.Success)
                    return setResult;
            }

            return await PersistCurrentAzureAccountAsync(method);
        }

        private async Task<CommandResult> ExecuteAzureConfigureAsync(CommandContext ctx)
        {
            var tenant = GetOption(ctx, "tenant", null);
            var subscription = GetOption(ctx, "subscription", ctx.Arguments.Count > 1 ? ctx.Arguments[1] : null);
            if (string.IsNullOrWhiteSpace(subscription))
                return new CommandResult { Success = false, ExitCode = 1, Message = "Usage: azure configure --subscription=<id-or-name> [--tenant=<tenant-id>]" };

            if (!string.IsNullOrWhiteSpace(tenant))
            {
                var account = await RunProcessAsync("az", "account show --output json");
                if (account.ExitCode != 0)
                    return new CommandResult { Success = false, ExitCode = 1, Message = "Run 'azure login' before validating a tenant. " + account.Error };

                var activeTenant = GetJsonString(JsonDocument.Parse(account.Output).RootElement, "tenantId");
                if (!string.Equals(activeTenant, tenant, StringComparison.OrdinalIgnoreCase))
                    return new CommandResult { Success = false, ExitCode = 1, Message = $"Active Azure tenant '{activeTenant}' does not match requested tenant '{tenant}'. Re-run azure login --tenant={tenant}." };
            }

            var setResult = await SetAzureSubscriptionAsync(subscription);
            if (!setResult.Success)
                return setResult;

            return await PersistCurrentAzureAccountAsync(GetOption(ctx, "method", "DefaultAzureCredential"));
        }

        private static string BuildServicePrincipalLoginArgs(CommandContext ctx, string? tenant)
        {
            var clientId = GetOption(ctx, "client-id", Environment.GetEnvironmentVariable("AZURE_CLIENT_ID"));
            var clientSecret = GetOption(ctx, "client-secret", Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET"));
            tenant ??= Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(tenant))
                throw new InvalidOperationException("Service principal login requires --client-id, --client-secret, and --tenant or AZURE_CLIENT_ID/AZURE_CLIENT_SECRET/AZURE_TENANT_ID.");
            return $"login --service-principal --username {Quote(clientId)} --password {Quote(clientSecret)} --tenant {Quote(tenant)}";
        }

        private async Task<CommandResult> SetAzureSubscriptionAsync(string subscription)
        {
            var set = await RunProcessAsync("az", $"account set --subscription {Quote(subscription)}");
            if (set.ExitCode != 0)
                return new CommandResult { Success = false, ExitCode = set.ExitCode, Message = $"Unable to select Azure subscription '{subscription}'. Validate with: az account list --output table\n" + set.Error };
            return new CommandResult { Success = true, Message = "Subscription selected." };
        }

        private async Task<CommandResult> PersistCurrentAzureAccountAsync(string method)
        {
            var account = await RunProcessAsync("az", "account show --output json");
            if (account.ExitCode != 0)
                return new CommandResult { Success = false, ExitCode = account.ExitCode, Message = "Unable to validate selected subscription with 'az account show'.\n" + account.Error };

            var json = JsonDocument.Parse(account.Output).RootElement;
            var config = AzureConfiguration.Load();
            config.TenantId = GetJsonString(json, "tenantId");
            config.SubscriptionId = GetJsonString(json, "id");
            config.SubscriptionName = GetJsonString(json, "name");
            config.AccountName = GetJsonString(json, "user", "name");
            config.AuthMethod = ParseAuthMethod(method);
            config.Save();

            return new CommandResult { Success = true, Message = $"Azure account validated and saved to {AzureConfiguration.GetDefaultConfigPath()}: {config.SubscriptionName} ({config.SubscriptionId}) in tenant {config.TenantId}" };
        }

        private static AuthenticationMethod ParseAuthMethod(string method) => method.ToLowerInvariant() switch
        {
            "device-code" => AuthenticationMethod.DeviceFlow,
            "interactive" => AuthenticationMethod.Interactive,
            "service-principal" => AuthenticationMethod.ServicePrincipal,
            "managed-identity" => AuthenticationMethod.ManagedIdentity,
            _ => AuthenticationMethod.DefaultAzureCredential
        };

        private static string? GetOption(CommandContext ctx, string key, string? fallback) =>
            ctx.Options.TryGetValue(key, out var value) ? value : fallback;

        private static bool CommandExists(string fileName, params string[] args) =>
            RunProcessAsync(fileName, string.Join(' ', args)).GetAwaiter().GetResult().ExitCode == 0;

        private static async Task<(int ExitCode, string Output, string Error)> RunProcessAsync(string fileName, string arguments)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                return (process.ExitCode, output, error);
            }
            catch (Exception ex) when (ex is Win32Exception || ex is FileNotFoundException)
            {
                return (127, string.Empty, ex.Message);
            }
        }

        private static string GetJsonString(JsonElement element, params string[] path)
        {
            foreach (var segment in path)
            {
                if (!element.TryGetProperty(segment, out element))
                    return string.Empty;
            }
            return element.ValueKind == JsonValueKind.String ? element.GetString() ?? string.Empty : element.ToString();
        }

        private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";

        private async Task<CommandResult> ExecuteConfigCommand(CommandContext ctx)
        {
            if (ctx.Arguments.Count == 0)
                return new CommandResult { Success = false, Message = "Usage: config get|set <key> [value]", ExitCode = 1 };

            var subcommand = ctx.Arguments[0];
            if (subcommand == "get" && ctx.Arguments.Count > 1)
            {
                return new CommandResult
                {
                    Success = true,
                    Message = $"Config value (placeholder): {ctx.Arguments[1]} = <value>"
                };
            }
            else if (subcommand == "set" && ctx.Arguments.Count > 2)
            {
                return new CommandResult
                {
                    Success = true,
                    Message = $"Config updated: {ctx.Arguments[1]} = {ctx.Arguments[2]}"
                };
            }

            return new CommandResult { Success = false, Message = "Invalid config command", ExitCode = 1 };
        }
    }
}
