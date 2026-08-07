using HELIOS.Platform;
using Xunit;

namespace HELIOS.Platform.Launcher.Tests;

public sealed class LauncherTests
{
    [Fact]
    public void Parse_defaults_to_safe_quick_start()
    {
        var options = LauncherOptions.Parse([]);

        Assert.Equal(LauncherCommand.Start, options.Command);
        Assert.Equal("quick", options.Profile);
        Assert.False(options.DryRun);
        Assert.InRange(options.MaxWorkers, 1, 4);
    }

    [Fact]
    public void Parse_accepts_dashboard_options()
    {
        var options = LauncherOptions.Parse([
            "dashboard", "--repo", "/work/helios", "--profile", "full", "--max-workers", "8", "--dry-run"]);

        Assert.Equal(LauncherCommand.Dashboard, options.Command);
        Assert.Equal("/work/helios", options.Repository);
        Assert.Equal("full", options.Profile);
        Assert.Equal(8, options.MaxWorkers);
        Assert.True(options.DryRun);
    }

    [Theory]
    [InlineData("--profile", "unsafe")]
    [InlineData("--max-workers", "0")]
    [InlineData("--max-workers", "65")]
    public void Parse_rejects_unsafe_values(string option, string value)
    {
        Assert.Throws<LauncherException>(() => LauncherOptions.Parse(["start", option, value]));
    }

    [Fact]
    public void RepositoryLocator_validates_an_explicit_repository()
    {
        var root = CreateRepository();
        try
        {
            Assert.Equal(root, RepositoryLocator.Find(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RepositoryLocator_rejects_a_directory_without_the_contract()
    {
        var root = Path.Combine(Path.GetTempPath(), $"helios-launcher-invalid-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            Assert.Throws<LauncherException>(() => RepositoryLocator.Find(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CommandBuilder_uses_argument_list_without_shell_interpolation()
    {
        var root = CreateRepository();
        try
        {
            var options = new LauncherOptions(LauncherCommand.Start, root, "quick", 3, false);

            var invocation = CommandBuilder.Build(root, options, isWindows: false);

            Assert.Equal("bash", invocation.FileName);
            Assert.Equal(Path.Combine(root, "helios.sh"), invocation.Arguments[0]);
            Assert.Equal(["setup", "--profile", "quick", "--changed-only", "--max-workers", "3"], invocation.Arguments.Skip(1));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CommandBuilder_uses_native_powershell_on_windows()
    {
        var root = CreateRepository();
        try
        {
            var options = new LauncherOptions(LauncherCommand.Dashboard, root, "full", 6, false);

            var invocation = CommandBuilder.Build(root, options, isWindows: true);

            Assert.Equal("powershell.exe", invocation.FileName);
            Assert.Contains(Path.Combine(root, "scripts", "setup", "helios-dev.ps1"), invocation.Arguments);
            Assert.Contains("-Serve", invocation.Arguments);
            Assert.Contains("6", invocation.Arguments);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateRepository()
    {
        var root = Path.Combine(Path.GetTempPath(), $"helios-launcher-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "config", "integrations"));
        File.WriteAllText(Path.Combine(root, "helios.sh"), "#!/usr/bin/env bash\n");
        File.WriteAllText(Path.Combine(root, "config", "integrations", "event-contract.schema.json"), "{}");
        return root;
    }
}
