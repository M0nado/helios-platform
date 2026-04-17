namespace HELIOS.Platform.Core.CLI;

/// <summary>
/// Main CLI interpreter for HELIOS Platform - handles command parsing and execution.
/// </summary>
public class HeliosCLI
{
    private readonly ICommandRegistry _commandRegistry;
    private readonly CommandContext _context;
    private readonly List<string> _commandHistory = new();
    private bool _running = true;

    public HeliosCLI()
    {
        _commandRegistry = new CommandRegistry();
        HeliosCliCommandsFactory.RegisterAllCommands(_commandRegistry);
        
        _context = new CommandContext
        {
            Output = Console.Out,
            Error = Console.Error
        };
    }

    /// <summary>
    /// Starts the interactive CLI loop.
    /// </summary>
    public async Task RunAsync()
    {
        PrintBanner();

        while (_running)
        {
            try
            {
                Console.Write("helios> ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                _commandHistory.Add(input);
                await ExecuteCommandLineAsync(input);
            }
            catch (Exception ex)
            {
                _context.WriteError(ex.Message);
            }
        }
    }

    /// <summary>
    /// Executes a command given as a string (for scripting and batch execution).
    /// </summary>
    public async Task ExecuteCommandAsync(string commandLine)
    {
        await ExecuteCommandLineAsync(commandLine);
    }

    private async Task ExecuteCommandLineAsync(string commandLine)
    {
        var parts = ParseCommandLine(commandLine);
        if (parts.Count == 0)
            return;

        var commandName = parts[0];
        var args = parts.Skip(1).ToArray();

        // Handle built-in commands
        if (commandName.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
            commandName.Equals("quit", StringComparison.OrdinalIgnoreCase))
        {
            _running = false;
            Console.WriteLine("Goodbye!");
            return;
        }

        if (commandName.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            Console.Clear();
            PrintBanner();
            return;
        }

        if (commandName.Equals("history", StringComparison.OrdinalIgnoreCase))
        {
            PrintCommandHistory();
            return;
        }

        if (commandName.Equals("verbose", StringComparison.OrdinalIgnoreCase))
        {
            _context.Verbose = !_context.Verbose;
            Console.WriteLine($"Verbose mode: {(_context.Verbose ? "ON" : "OFF")}");
            return;
        }

        if (commandName.Equals("quiet", StringComparison.OrdinalIgnoreCase))
        {
            _context.Quiet = !_context.Quiet;
            if (!_context.Quiet)
                Console.WriteLine($"Quiet mode: OFF");
            return;
        }

        // Execute registered command
        var result = await _commandRegistry.ExecuteAsync(commandName, args, _context);
        
        if (!result.Success && !_context.Quiet)
        {
            _context.WriteError(result.Message ?? "Unknown error");
        }

        if (result.ExitCode != 0 && !_context.Quiet && !string.IsNullOrEmpty(result.Message))
        {
            Console.WriteLine(result.Message);
        }
    }

    private List<string> ParseCommandLine(string commandLine)
    {
        var parts = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var ch in commandLine)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(ch);
            }
        }

        if (current.Length > 0)
            parts.Add(current.ToString());

        return parts;
    }

    private void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         ⚔️  HELIOS Platform v2.0 - CLI                ║");
        Console.WriteLine("║    Enterprise Automation & System Management           ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine("Type 'help' for commands or 'exit' to quit\n");
    }

    private void PrintCommandHistory()
    {
        Console.WriteLine("Command History:");
        for (int i = 0; i < _commandHistory.Count; i++)
        {
            Console.WriteLine($"  {i + 1}: {_commandHistory[i]}");
        }
    }
}

/// <summary>
/// Script execution engine for running batch HELIOS commands.
/// </summary>
public class HeliosScriptEngine
{
    private readonly HeliosCLI _cli;

    public HeliosScriptEngine()
    {
        _cli = new HeliosCLI();
    }

    /// <summary>
    /// Executes a script file containing HELIOS commands.
    /// </summary>
    public async Task ExecuteScriptAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Script file not found: {filePath}");

        var lines = File.ReadAllLines(filePath);
        var commandCount = 0;
        var errorCount = 0;

        Console.WriteLine($"Executing script: {filePath}");
        Console.WriteLine(new string('-', 50));

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Skip comments and empty lines
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                continue;

            try
            {
                await _cli.ExecuteCommandAsync(trimmed);
                commandCount++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error executing: {trimmed}");
                Console.Error.WriteLine($"  {ex.Message}");
                errorCount++;
            }
        }

        Console.WriteLine(new string('-', 50));
        Console.WriteLine($"Script completed: {commandCount} commands executed, {errorCount} errors");
    }

    /// <summary>
    /// Executes multiple commands programmatically.
    /// </summary>
    public async Task ExecuteCommandsAsync(params string[] commands)
    {
        foreach (var command in commands)
        {
            await _cli.ExecuteCommandAsync(command);
        }
    }
}

/// <summary>
/// Extension methods for CLI integration.
/// </summary>
public static class CLIExtensions
{
    /// <summary>
    /// Converts a JSON object to CLI output format.
    /// </summary>
    public static string ToCLIOutput(this object obj)
    {
        return System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }

    /// <summary>
    /// Formats output for CLI display.
    /// </summary>
    public static string FormatCLI(this string text, bool verbose = false)
    {
        if (verbose)
            return $"[VERBOSE] {text}";
        return text;
    }
}
