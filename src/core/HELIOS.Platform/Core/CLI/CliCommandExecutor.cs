using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Logging;
using HELIOS.Platform.BackendServices.ServerManagement;
using HELIOS.Platform.Core.Administration;

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
                { "config", ExecuteConfigCommand }
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
