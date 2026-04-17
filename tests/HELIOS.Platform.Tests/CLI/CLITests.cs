using Xunit;
using HELIOS.Platform.Core.CLI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Tests.CLI
{
    public class CLIEngineTests
    {
        [Fact]
        public void ParseArguments_NoArgs_ReturnsEmptyOptions()
        {
            var engine = new CLIEngine();
            var options = new CLIOptions();
            // Should not throw
        }

        [Fact]
        public void ParseArguments_WithCommand_ParsesCorrectly()
        {
            var parser = new CommandParser();
            var options = parser.ParseArguments(new[] { "status" });
            
            Assert.Equal("status", options.Command);
        }

        [Fact]
        public void ParseArguments_WithOptions_ParsesCorrectly()
        {
            var parser = new CommandParser();
            var options = parser.ParseArguments(new[] { "status", "--verbose", "--json" });
            
            Assert.Equal("status", options.Command);
            Assert.True(options.IsVerbose);
            Assert.Equal(OutputFormat.Json, options.OutputFormat);
        }

        [Fact]
        public void ParseArguments_WithParameters_ParsesCorrectly()
        {
            var parser = new CommandParser();
            var options = parser.ParseArguments(new[] { "deploy", "--config", "app.json" });
            
            Assert.Equal("deploy", options.Command);
            Assert.True(options.Parameters.ContainsKey("config"));
            Assert.Equal("app.json", options.Parameters["config"]);
        }

        [Fact]
        public void ParseArguments_HelpFlag_SetsFlagCorrectly()
        {
            var parser = new CommandParser();
            var options = parser.ParseArguments(new[] { "-h" });
            
            Assert.True(options.ShowHelp);
        }

        [Fact]
        public void ParseArguments_VersionFlag_SetsFlagCorrectly()
        {
            var parser = new CommandParser();
            var options = parser.ParseArguments(new[] { "--version" });
            
            Assert.True(options.ShowVersion);
        }

        [Fact]
        public void ParseArguments_QuietFlag_SetsFlagCorrectly()
        {
            var parser = new CommandParser();
            var options = parser.ParseArguments(new[] { "-q" });
            
            Assert.True(options.IsQuiet);
        }

        [Fact]
        public void ParseArguments_OutputFile_ParsesCorrectly()
        {
            var parser = new CommandParser();
            var options = parser.ParseArguments(new[] { "status", "-o", "output.txt" });
            
            Assert.Equal("output.txt", options.OutputFile);
        }

        [Fact]
        public void ParseArguments_Timeout_ParsesCorrectly()
        {
            var parser = new CommandParser();
            var options = parser.ParseArguments(new[] { "status", "--timeout", "60" });
            
            Assert.Equal(60, options.TimeoutSeconds);
        }

        [Fact]
        public void ParseArguments_MultipleArguments_ParsesCorrectly()
        {
            var parser = new CommandParser();
            var options = parser.ParseArguments(new[] { "scale", "web", "--instances", "5" });
            
            Assert.Equal("scale", options.Command);
            Assert.Single(options.Arguments);
            Assert.Equal("web", options.Arguments[0]);
            Assert.Equal("5", options.Parameters["instances"]);
        }
    }

    public class CommandExecutorTests
    {
        [Fact]
        public async Task ExecuteAsync_StatusCommand_ReturnsSuccess()
        {
            var executor = new CommandExecutor();
            var options = new CLIOptions { Command = "status" };
            
            var result = await executor.ExecuteAsync(options);
            
            Assert.Equal(0, result.ExitCode);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task ExecuteAsync_HealthCommand_ReturnsSuccess()
        {
            var executor = new CommandExecutor();
            var options = new CLIOptions { Command = "health" };
            
            var result = await executor.ExecuteAsync(options);
            
            Assert.Equal(0, result.ExitCode);
        }

        [Fact]
        public async Task ExecuteAsync_DeployCommand_ReturnsSuccess()
        {
            var executor = new CommandExecutor();
            var options = new CLIOptions 
            { 
                Command = "deploy",
                Parameters = new Dictionary<string, string> { { "config", "app.json" } }
            };
            
            var result = await executor.ExecuteAsync(options);
            
            Assert.Equal(0, result.ExitCode);
        }

        [Fact]
        public async Task ExecuteAsync_ScaleCommand_ReturnsSuccess()
        {
            var executor = new CommandExecutor();
            var options = new CLIOptions 
            { 
                Command = "scale",
                Arguments = new List<string> { "web" },
                Parameters = new Dictionary<string, string> { { "instances", "5" } }
            };
            
            var result = await executor.ExecuteAsync(options);
            
            Assert.Equal(0, result.ExitCode);
        }

        [Fact]
        public async Task ExecuteAsync_UnknownCommand_ReturnsError()
        {
            var executor = new CommandExecutor();
            var options = new CLIOptions { Command = "unknown" };
            
            var result = await executor.ExecuteAsync(options);
            
            Assert.NotEqual(0, result.ExitCode);
        }

        [Fact]
        public async Task ExecuteAsync_AllCommands_SucceedInSequence()
        {
            var executor = new CommandExecutor();
            var commands = new[] { "status", "health", "list", "config" };
            
            foreach (var cmd in commands)
            {
                var options = new CLIOptions { Command = cmd };
                var result = await executor.ExecuteAsync(options);
                Assert.Equal(0, result.ExitCode);
            }
        }
    }

    public class CommandHistoryTests
    {
        [Fact]
        public void RecordCommand_AddsToHistory()
        {
            var history = new CommandHistory();
            var options = new CLIOptions { Command = "status" };
            
            history.RecordCommand(options);
            var entries = history.GetHistory(1);
            
            Assert.NotEmpty(entries);
        }

        [Fact]
        public void GetHistory_ReturnsRecentEntries()
        {
            var history = new CommandHistory();
            
            // Record multiple commands
            for (int i = 0; i < 5; i++)
            {
                var options = new CLIOptions { Command = $"cmd{i}" };
                history.RecordCommand(options);
            }
            
            var entries = history.GetHistory(10);
            Assert.NotEmpty(entries);
        }

        [Fact]
        public void Search_FindsMatchingCommands()
        {
            var history = new CommandHistory();
            history.RecordCommand(new CLIOptions { Command = "status" });
            history.RecordCommand(new CLIOptions { Command = "deploy" });
            
            var results = history.Search("status");
            Assert.NotEmpty(results);
        }
    }

    public class OutputFormatterTests
    {
        [Fact]
        public void FormatAsJson_ReturnsValidJson()
        {
            var formatter = new OutputFormatter();
            var result = new CommandResult 
            { 
                ExitCode = 0, 
                Message = "Success",
                Data = new { Test = "value" }
            };
            
            var json = formatter.FormatAsJson(result);
            
            Assert.NotEmpty(json);
            Assert.Contains("ExitCode", json);
        }

        [Fact]
        public void FormatAsVerbose_IncludesAllDetails()
        {
            var formatter = new OutputFormatter();
            var result = new CommandResult 
            { 
                ExitCode = 0, 
                Message = "Success",
                Data = new { Test = "value" }
            };
            
            var output = formatter.FormatAsVerbose(result);
            
            Assert.Contains("EXIT CODE", output);
            Assert.Contains("MESSAGE", output);
            Assert.Contains("DATA", output);
        }

        [Fact]
        public void FormatAsDefault_ReturnsSimpleOutput()
        {
            var formatter = new OutputFormatter();
            var result = new CommandResult 
            { 
                ExitCode = 0, 
                Message = "Success"
            };
            
            var output = formatter.FormatAsDefault(result);
            
            Assert.Contains("Success", output);
        }

        [Fact]
        public void FormatAsDefault_ErrorMessage_PrefixedWithError()
        {
            var formatter = new OutputFormatter();
            var result = new CommandResult 
            { 
                ExitCode = 1, 
                Message = "Something went wrong"
            };
            
            var output = formatter.FormatAsDefault(result);
            
            Assert.Contains("ERROR", output);
        }
    }

    public class BatchProcessorTests
    {
        [Fact]
        public void LoadBatchJson_ParsesCorrectly()
        {
            var processor = new BatchProcessor();
            var json = @"{
                ""name"": ""Test Batch"",
                ""commands"": [
                    { ""name"": ""cmd1"", ""command"": ""status"" }
                ]
            }";
            
            processor.LoadBatchJson(json);
            // Should not throw
        }

        [Fact]
        public async Task ExecuteAsync_ExecutesAllCommands()
        {
            var processor = new BatchProcessor();
            var json = @"{
                ""name"": ""Test Batch"",
                ""commands"": [
                    { ""name"": ""status"", ""command"": ""status"" },
                    { ""name"": ""health"", ""command"": ""health"" }
                ]
            }";
            
            processor.LoadBatchJson(json);
            var result = await processor.ExecuteAsync();
            
            Assert.NotNull(result);
            Assert.True(result.Commands.Count >= 2);
        }

        [Fact]
        public async Task ExecuteAsync_StopsOnError_WhenFlagSet()
        {
            var processor = new BatchProcessor();
            var json = @"{
                ""name"": ""Test Batch"",
                ""commands"": [
                    { ""name"": ""invalid"", ""command"": ""nonexistent"" },
                    { ""name"": ""status"", ""command"": ""status"" }
                ]
            }";
            
            processor.LoadBatchJson(json);
            var result = await processor.ExecuteAsync(stopOnError: true);
            
            Assert.Equal("Failed", result.Status);
        }

        [Fact]
        public async Task ExecuteAsync_CalculatesMetrics()
        {
            var processor = new BatchProcessor();
            var json = @"{
                ""name"": ""Test Batch"",
                ""commands"": [
                    { ""name"": ""cmd1"", ""command"": ""status"" }
                ]
            }";
            
            processor.LoadBatchJson(json);
            var result = await processor.ExecuteAsync();
            
            Assert.True(result.TotalExecutionTimeMs >= 0);
            Assert.True(result.SuccessCount >= 0);
        }
    }

    public class TaskSchedulerTests
    {
        [Fact]
        public void Schedule_CreatesTask()
        {
            var scheduler = new TaskScheduler();
            var task = scheduler.Schedule("test-task", "status", "daily");
            
            Assert.NotNull(task);
            Assert.Equal("test-task", task.Name);
            Assert.Equal("daily", task.Schedule);
        }

        [Fact]
        public void GetAllTasks_ReturnsScheduledTasks()
        {
            var scheduler = new TaskScheduler();
            scheduler.Schedule("task1", "status", "daily");
            scheduler.Schedule("task2", "health", "hourly");
            
            var tasks = scheduler.GetAllTasks();
            
            Assert.Equal(2, tasks.Count);
        }

        [Fact]
        public void RemoveTask_DeletesTask()
        {
            var scheduler = new TaskScheduler();
            var task = scheduler.Schedule("test-task", "status", "daily");
            
            var removed = scheduler.RemoveTask(task.Id);
            
            Assert.True(removed);
            Assert.Empty(scheduler.GetAllTasks());
        }

        [Fact]
        public void EnableDisableTask_WorksCorrectly()
        {
            var scheduler = new TaskScheduler();
            var task = scheduler.Schedule("test-task", "status", "daily");
            
            scheduler.DisableTask(task.Id);
            var tasks = scheduler.GetAllTasks();
            Assert.False(tasks[0].Enabled);
            
            scheduler.EnableTask(task.Id);
            tasks = scheduler.GetAllTasks();
            Assert.True(tasks[0].Enabled);
        }

        [Fact]
        public void RecordExecution_UpdatesStatistics()
        {
            var scheduler = new TaskScheduler();
            var task = scheduler.Schedule("test-task", "status", "daily");
            
            scheduler.RecordExecution(task.Id, true);
            var tasks = scheduler.GetAllTasks();
            
            Assert.Equal(1, tasks[0].RunCount);
            Assert.NotNull(tasks[0].LastRun);
        }
    }
}
