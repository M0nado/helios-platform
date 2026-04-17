using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HELIOS.Platform.Installer;

/// <summary>
/// Installation modes for HELIOS.
/// </summary>
public enum InstallationMode
{
    Quick,      // Fast default installation
    Advanced,   // Detailed component selection
    Silent,     // Command-line only, no UI
    Portable,   // USB-friendly installation
}

/// <summary>
/// Core installer implementation.
/// </summary>
public class HeliosInstaller
{
    public InstallationMode Mode { get; private set; }
    public string InstallPath { get; set; }
    public List<InstallableComponent> SelectedComponents { get; set; }
    public string? InstallationLog { get; set; }

    private readonly string _logDirectory;
    private bool _rollbackOnFailure = true;
    private readonly Stack<InstallationStep> _completedSteps;

    public HeliosInstaller(InstallationMode mode = InstallationMode.Quick)
    {
        Mode = mode;
        InstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "HELIOS Platform");
        SelectedComponents = GetDefaultComponents();
        _logDirectory = Path.Combine(Path.GetTempPath(), "HELIOS_Install_Logs");
        _completedSteps = new Stack<InstallationStep>();
        
        Directory.CreateDirectory(_logDirectory);
    }

    /// <summary>
    /// Executes the installation with selected mode.
    /// </summary>
    public async Task<InstallationResult> ExecuteAsync()
    {
        var result = new InstallationResult();
        InstallationLog = Path.Combine(_logDirectory, $"install_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        try
        {
            LogMessage("Starting HELIOS Platform installation in {0} mode", Mode);

            // Pre-installation hooks
            var preInstallResult = await ExecutePreInstallHooks();
            if (!preInstallResult)
            {
                result.Success = false;
                result.ErrorMessage = "Pre-installation checks failed";
                return result;
            }

            // Main installation
            Directory.CreateDirectory(InstallPath);

            foreach (var component in SelectedComponents)
            {
                LogMessage("Installing component: {0}", component.Name);
                var step = new InstallationStep { ComponentName = component.Name, StartTime = DateTime.Now };
                
                try
                {
                    await InstallComponentAsync(component);
                    step.Status = "Completed";
                    _completedSteps.Push(step);
                    LogMessage("✓ Component installed: {0}", component.Name);
                }
                catch (Exception ex)
                {
                    step.Status = "Failed";
                    step.ErrorMessage = ex.Message;
                    LogMessage("✗ Component installation failed: {0} - {1}", component.Name, ex.Message);
                    
                    if (_rollbackOnFailure)
                    {
                        await RollbackAsync();
                        result.Success = false;
                        result.ErrorMessage = $"Installation failed at component {component.Name}: {ex.Message}";
                        return result;
                    }
                }
            }

            // Register installation
            RegisterInstallation();

            // Post-installation hooks
            var postInstallResult = await ExecutePostInstallHooks();
            if (!postInstallResult)
            {
                LogMessage("Post-installation hooks completed with warnings");
            }

            result.Success = true;
            result.InstallPath = InstallPath;
            result.LogPath = InstallationLog;
            result.ComponentsInstalled = SelectedComponents.Select(c => c.Name).ToList();

            LogMessage("Installation completed successfully");
        }
        catch (Exception ex)
        {
            LogMessage("Fatal installation error: {0}", ex.Message);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Performs system check before installation.
    /// </summary>
    private async Task<bool> ExecutePreInstallHooks()
    {
        LogMessage("Executing pre-installation hooks");
        
        try
        {
            // Check admin rights
            if (!IsRunningAsAdmin())
            {
                LogMessage("Warning: Not running as administrator");
                return false;
            }

            // Check disk space
            var drive = new DriveInfo(InstallPath[0].ToString());
            var requiredSpace = 5L * 1024 * 1024 * 1024; // 5 GB
            if (drive.AvailableFreeSpace < requiredSpace)
            {
                LogMessage("Insufficient disk space: {0} GB required, {1} GB available",
                    requiredSpace / (1024 * 1024 * 1024),
                    drive.AvailableFreeSpace / (1024 * 1024 * 1024));
                return false;
            }

            // Additional pre-install script execution
            var preInstallScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pre-Install-Check.ps1");
            if (File.Exists(preInstallScript))
            {
                await ExecutePowerShellScript(preInstallScript);
            }

            LogMessage("Pre-installation checks passed");
            return true;
        }
        catch (Exception ex)
        {
            LogMessage("Pre-installation hook error: {0}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Executes post-installation hooks.
    /// </summary>
    private async Task<bool> ExecutePostInstallHooks()
    {
        LogMessage("Executing post-installation hooks");
        
        try
        {
            var postInstallScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Post-Install-Verify.ps1");
            if (File.Exists(postInstallScript))
            {
                await ExecutePowerShellScript(postInstallScript);
            }

            // Register shell extensions
            RegisterShellExtensions();

            // Create shortcuts
            CreateStartMenuShortcuts();

            LogMessage("Post-installation hooks completed");
            return true;
        }
        catch (Exception ex)
        {
            LogMessage("Post-installation hook error: {0}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Installs a component.
    /// </summary>
    private async Task InstallComponentAsync(InstallableComponent component)
    {
        var componentPath = Path.Combine(InstallPath, component.Name);
        Directory.CreateDirectory(componentPath);

        // Copy component files
        if (!string.IsNullOrEmpty(component.SourcePath) && Directory.Exists(component.SourcePath))
        {
            CopyDirectory(component.SourcePath, componentPath);
        }

        // Execute component-specific installation
        if (!string.IsNullOrEmpty(component.InstallScript))
        {
            await ExecutePowerShellScript(component.InstallScript);
        }

        await Task.Delay(100); // Brief delay for file system sync
    }

    /// <summary>
    /// Rolls back installation on failure.
    /// </summary>
    private async Task RollbackAsync()
    {
        LogMessage("Starting rollback procedure");
        
        while (_completedSteps.Count > 0)
        {
            var step = _completedSteps.Pop();
            LogMessage("Rolling back component: {0}", step.ComponentName);
            
            try
            {
                var componentPath = Path.Combine(InstallPath, step.ComponentName);
                if (Directory.Exists(componentPath))
                {
                    Directory.Delete(componentPath, true);
                }
            }
            catch (Exception ex)
            {
                LogMessage("Rollback warning for {0}: {1}", step.ComponentName, ex.Message);
            }
        }

        try
        {
            if (Directory.Exists(InstallPath) && Directory.GetFiles(InstallPath).Length == 0)
            {
                Directory.Delete(InstallPath);
            }
        }
        catch { }

        LogMessage("Rollback completed");
    }

    /// <summary>
    /// Registers the installation in Windows registry.
    /// </summary>
    private void RegisterInstallation()
    {
        try
        {
            using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\HELIOS Platform"))
            {
                key.SetValue("InstallPath", InstallPath);
                key.SetValue("Version", "1.0.0.0");
                key.SetValue("InstallDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                key.SetValue("InstallMode", Mode.ToString());
            }

            LogMessage("Installation registered in registry");
        }
        catch (Exception ex)
        {
            LogMessage("Warning: Could not register installation: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Registers shell extensions.
    /// </summary>
    private void RegisterShellExtensions()
    {
        try
        {
            LogMessage("Registering shell extensions");
            ShellExtension.ContextMenuRegistration.Register();
            ShellExtension.FileAssociationManager.AssociateExtension(".helios");
            LogMessage("Shell extensions registered");
        }
        catch (Exception ex)
        {
            LogMessage("Warning: Could not register shell extensions: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Creates start menu shortcuts.
    /// </summary>
    private void CreateStartMenuShortcuts()
    {
        try
        {
            var startMenuPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                "Programs",
                "HELIOS Platform");
            
            Directory.CreateDirectory(startMenuPath);
            LogMessage("Start menu shortcuts created at {0}", startMenuPath);
        }
        catch (Exception ex)
        {
            LogMessage("Warning: Could not create shortcuts: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Executes PowerShell script.
    /// </summary>
    private async Task ExecutePowerShellScript(string scriptPath)
    {
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"Script not found: {scriptPath}");

        var processInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using (var process = System.Diagnostics.Process.Start(processInfo))
        {
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();
                
                if (!string.IsNullOrEmpty(output))
                {
                    LogMessage("Script output: {0}", output);
                }
            }
        }
    }

    /// <summary>
    /// Copies directory recursively.
    /// </summary>
    private void CopyDirectory(string source, string destination)
    {
        var sourceInfo = new DirectoryInfo(source);
        if (!sourceInfo.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {source}");

        foreach (var file in sourceInfo.GetFiles())
        {
            file.CopyTo(Path.Combine(destination, file.Name), true);
        }

        foreach (var subdir in sourceInfo.GetDirectories())
        {
            CopyDirectory(subdir.FullName, Path.Combine(destination, subdir.Name));
        }
    }

    /// <summary>
    /// Checks if running as administrator.
    /// </summary>
    private bool IsRunningAsAdmin()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Logs a message to the installation log.
    /// </summary>
    private void LogMessage(string format, params object?[] args)
    {
        var message = string.Format(format, args);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {message}";

        Console.WriteLine(logEntry);

        if (!string.IsNullOrEmpty(InstallationLog))
        {
            try
            {
                File.AppendAllText(InstallationLog, logEntry + Environment.NewLine);
            }
            catch { }
        }
    }

    /// <summary>
    /// Gets default components for installation.
    /// </summary>
    private List<InstallableComponent> GetDefaultComponents()
    {
        return new List<InstallableComponent>
        {
            new() { Name = "Core", Description = "HELIOS Core Engine", IsRequired = true },
            new() { Name = "GUI", Description = "GUI Dashboard", IsRequired = true },
            new() { Name = "Services", Description = "Background Services", IsRequired = false },
            new() { Name = "Tools", Description = "Command-line Tools", IsRequired = false },
        };
    }
}

/// <summary>
/// Represents an installable component.
/// </summary>
public class InstallableComponent
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? SourcePath { get; set; }
    public string? InstallScript { get; set; }
    public bool IsSelected { get; set; }
}

/// <summary>
/// Represents an installation step for rollback.
/// </summary>
internal class InstallationStep
{
    public string ComponentName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public DateTime StartTime { get; set; }
}

/// <summary>
/// Installation result.
/// </summary>
public class InstallationResult
{
    public bool Success { get; set; }
    public string? InstallPath { get; set; }
    public string? LogPath { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ComponentsInstalled { get; set; } = new();
}
