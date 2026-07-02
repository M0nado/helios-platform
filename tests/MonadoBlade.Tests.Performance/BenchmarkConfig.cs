namespace MonadoBlade.Tests.Performance;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Loggers;
using System.Collections.Immutable;

/// <summary>
/// Global benchmark configuration for MonadoBlade performance tests.
/// Defines diagnosers, exporters, and runtime settings.
/// </summary>
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        // Add memory diagnostics
        AddDiagnoser(MemoryDiagnoser.Default);
        AddDiagnoser(new ThreadingDiagnoser());

        // Add exporters for multiple output formats
        AddExporter(MarkdownExporter.GitHub);
        AddExporter(MarkdownExporter.Default);
        AddExporter(HtmlExporter.Default);
        AddExporter(new CsvExporter(CsvSeparator.Semicolon));
        AddExporter(JsonExporter.Default);

        // Configure logger
        AddLogger(ConsoleLogger.Default);

        // Runtime settings
        WithOptions(ConfigOptions.DisableOptimizationsValidator);
        WithOption(ConfigOptions.JoinSummary, true);

        // Use multiple runtimes for comparison
        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core80)
            .WithEnvironmentVariable("DOTNET_TieredCompilation", "1")
            .WithEnvironmentVariable("DOTNET_TC_QuickJitForLoops", "1")
            .WithWarmupCount(5)
            .WithTargetCount(10));

        // Add baseline job
        AddJob(Job.Default
            .AsBaseline()
            .WithRuntime(CoreRuntime.Core80)
            .WithWarmupCount(3)
            .WithTargetCount(5));
    }
}
