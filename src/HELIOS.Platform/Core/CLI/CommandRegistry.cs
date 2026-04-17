using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.CLI
{
    /// <summary>
    /// Result of executing a command.
    /// </summary>
    public class CommandResult
    {
        public required bool Success { get; init; }
        public string? Message { get; init; }
        public int ExitCode { get; init; }
        public object? Data { get; init; }
    }

    /// <summary>
    /// Base class for CLI commands.
    /// </summary>
    public abstract class Command
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public abstract Task<CommandResult> ExecuteAsync();

        protected CommandResult Success(string message = "Success", object data = null)
        {
            return new CommandResult
            {
                Success = true,
                Message = message,
                ExitCode = 0,
                Data = data
            };
        }

        protected CommandResult Error(string message, int exitCode = 1)
        {
            return new CommandResult
            {
                Success = false,
                Message = message,
                ExitCode = exitCode
            };
        }
    }

    /// <summary>
    /// CLI command registry.
    /// </summary>
    public class CommandRegistry
    {
        private static readonly Dictionary<string, Command> _commands = new();

        public static void Register(Command command)
        {
            _commands[command.Name] = command;
        }

        public static Command GetCommand(string name)
        {
            return _commands.TryGetValue(name, out var cmd) ? cmd : null;
        }

        public static IEnumerable<Command> GetAllCommands()
        {
            return _commands.Values;
        }
    }
}
