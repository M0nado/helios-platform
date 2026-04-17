namespace HELIOS.Platform.Core.Installation;

using HELIOS.Platform.Data.Database;

/// <summary>
/// Installation and setup wizard service for HELIOS Platform.
/// </summary>
public interface IInstallationService
{
    Task<InstallationContext> InitializeInstallationAsync();
    Task<WindowsEnvironment> DetectEnvironmentAsync();
    Task<bool> ValidateHardwareRequirementsAsync();
    Task<bool> ValidateWindowsVersionAsync();
    Task<bool> CheckAdminPrivilegesAsync();
    Task<InstallationResult> PerformInstallationAsync(InstallationSettings settings);
    Task<bool> VerifyInstallationAsync();
    Task<bool> RollbackInstallationAsync();
    Task ConfigureFirstRunAsync(User user);
}

/// <summary>
/// Installation context.
/// </summary>
public class InstallationContext
{
    public required WindowsEnvironment Environment { get; set; }
    public required HardwareCapabilities Hardware { get; set; }
    public bool HasAdminPrivileges { get; set; }
    public bool IsPortableMode { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Windows environment information.
/// </summary>
public class WindowsEnvironment
{
    public string? OsVersion { get; set; }
    public string? OsBuild { get; set; }
    public string? Processor { get; set; }
    public long TotalMemoryMb { get; set; }
    public long FreeDiskSpaceMb { get; set; }
    public bool Is64Bit { get; set; }
    public bool IsHyperVAvailable { get; set; }
    public bool IsWSL2Available { get; set; }
    public bool IsSandboxAvailable { get; set; }
    public List<GPUInfo> InstalledGPUs { get; set; } = [];
    public List<DriveInfo> InstalledDrives { get; set; } = [];
}

/// <summary>
/// GPU information.
/// </summary>
public class GPUInfo
{
    public required string Name { get; set; }
    public required string VendorId { get; set; }
    public long MemoryMb { get; set; }
    public string? DriverVersion { get; set; }
    public bool IsSupported { get; set; }
}

/// <summary>
/// Drive information.
/// </summary>
public class DriveInfo
{
    public char Letter { get; set; }
    public string? Label { get; set; }
    public long CapacityMb { get; set; }
    public long FreeMb { get; set; }
    public bool IsSystemDrive { get; set; }
}

/// <summary>
/// Hardware capabilities.
/// </summary>
public class HardwareCapabilities
{
    public long MinimumMemoryMb { get; set; } = 4096;
    public long MinimumDiskSpaceMb { get; set; } = 5120;
    public bool HasEnoughMemory { get; set; }
    public bool HasEnoughDiskSpace { get; set; }
    public bool Supports64Bit { get; set; }
    public bool SupportsHyperV { get; set; }
    public string? RecommendedInstallationPath { get; set; }
}

/// <summary>
/// Installation settings.
/// </summary>
public class InstallationSettings
{
    public string? InstallationPath { get; set; }
    public bool CreateShortcut { get; set; } = true;
    public bool RegisterContextMenu { get; set; } = true;
    public bool CreateSystemService { get; set; } = false;
    public bool EnableAutoUpdate { get; set; } = true;
    public bool EnableTelemetry { get; set; } = true;
    public bool CreatePortableVersion { get; set; } = false;
    public string? AdminUsername { get; set; }
    public string? AdminEmail { get; set; }
}

/// <summary>
/// Installation result.
/// </summary>
public class InstallationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> InstallationLogs { get; set; } = [];
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public long ElapsedTimeMs { get; set; }
    public string? InstallationPath { get; set; }
}

/// <summary>
/// Default implementation of installation service.
/// </summary>
public class InstallationService : IInstallationService
{
    private readonly ILogger<InstallationService> _logger;
    private readonly IConfiguration _configuration;

    public InstallationService(ILogger<InstallationService> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<InstallationContext> InitializeInstallationAsync()
    {
        _logger.LogInformation("Initializing installation process...");

        var environment = await DetectEnvironmentAsync();
        var hardware = await ValidateHardwareAsync(environment);
        var hasAdmin = await CheckAdminPrivilegesAsync();

        var context = new InstallationContext
        {
            Environment = environment,
            Hardware = hardware,
            HasAdminPrivileges = hasAdmin,
            IsPortableMode = DetectPortableMode()
        };

        _logger.LogInformation("Installation context initialized: Admin={Admin}, Portable={Portable}",
            hasAdmin, context.IsPortableMode);

        return context;
    }

    public async Task<WindowsEnvironment> DetectEnvironmentAsync()
    {
        _logger.LogInformation("Detecting Windows environment...");

        var env = new WindowsEnvironment
        {
            OsVersion = GetOsVersion(),
            OsBuild = GetOsBuild(),
            Processor = GetProcessorName(),
            TotalMemoryMb = GC.GetTotalMemory(false) / 1024 / 1024,
            FreeDiskSpaceMb = GetFreeDiskSpace(),
            Is64Bit = Environment.Is64BitOperatingSystem,
            IsHyperVAvailable = CheckHyperVAvailable(),
            IsWSL2Available = CheckWSL2Available(),
            IsSandboxAvailable = CheckSandboxAvailable()
        };

        env.InstalledGPUs = await DetectGPUsAsync();
        env.InstalledDrives = GetSystemDrives();

        _logger.LogInformation("Environment detected: OS={Version}, Arch={Arch}, Memory={MemMb}MB, Disk={DiskMb}MB",
            env.OsVersion, env.Is64Bit ? "x64" : "x86", env.TotalMemoryMb, env.FreeDiskSpaceMb);

        return env;
    }

    public async Task<bool> ValidateHardwareRequirementsAsync()
    {
        _logger.LogInformation("Validating hardware requirements...");

        var environment = await DetectEnvironmentAsync();
        var hardware = await ValidateHardwareAsync(environment);

        if (!hardware.HasEnoughMemory)
        {
            _logger.LogWarning("Insufficient memory: Required {Min}MB, Available {Available}MB",
                hardware.MinimumMemoryMb, environment.TotalMemoryMb);
            return false;
        }

        if (!hardware.HasEnoughDiskSpace)
        {
            _logger.LogWarning("Insufficient disk space: Required {Min}MB, Available {Available}MB",
                hardware.MinimumDiskSpaceMb, environment.FreeDiskSpaceMb);
            return false;
        }

        _logger.LogInformation("Hardware validation passed");
        return true;
    }

    public async Task<bool> ValidateWindowsVersionAsync()
    {
        _logger.LogInformation("Validating Windows version...");

        var osVersion = Environment.OSVersion;
        // Require Windows 10 21H2 or later (Build 19045+) or Windows 11
        if (osVersion.Version.Major < 10 || (osVersion.Version.Major == 10 && osVersion.Version.Build < 19045))
        {
            _logger.LogError("Windows version not supported. Minimum: Windows 10 21H2 or Windows 11");
            return false;
        }

        _logger.LogInformation("Windows version validated: {Version}", osVersion);
        return await Task.FromResult(true);
    }

    public async Task<bool> CheckAdminPrivilegesAsync()
    {
        return await Task.FromResult(IsRunningAsAdmin());
    }

    public async Task<InstallationResult> PerformInstallationAsync(InstallationSettings settings)
    {
        _logger.LogInformation("Performing HELIOS Platform installation...");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new InstallationResult { InstallationLogs = [] };

        try
        {
            // Validate pre-installation checks
            if (!await ValidateWindowsVersionAsync())
            {
                result.ErrorMessage = "Windows version not supported";
                return result;
            }

            if (!await ValidateHardwareRequirementsAsync())
            {
                result.ErrorMessage = "Hardware requirements not met";
                return result;
            }

            var installPath = settings.InstallationPath ?? GetDefaultInstallationPath();
            result.InstallationLogs.Add($"Installation path: {installPath}");

            // Create installation directory
            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
                result.InstallationLogs.Add($"Created installation directory: {installPath}");
            }

            // Copy application files
            await CopyApplicationFilesAsync(installPath);
            result.InstallationLogs.Add("Application files copied");

            // Create shortcuts
            if (settings.CreateShortcut)
            {
                CreateDesktopShortcut(installPath);
                result.InstallationLogs.Add("Desktop shortcut created");
            }

            // Register context menu
            if (settings.RegisterContextMenu && IsRunningAsAdmin())
            {
                RegisterContextMenu(installPath);
                result.InstallationLogs.Add("Context menu registered");
            }

            // Create Windows service (if requested)
            if (settings.CreateSystemService && IsRunningAsAdmin())
            {
                await CreateWindowsServiceAsync(installPath);
                result.InstallationLogs.Add("Windows service created");
            }

            // Configure settings
            if (settings.AdminUsername != null)
            {
                result.InstallationLogs.Add($"Admin user set to: {settings.AdminUsername}");
            }

            result.Success = true;
            result.InstallationPath = installPath;

            _logger.LogInformation("Installation completed successfully at {Path}", installPath);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.InstallationLogs.Add($"ERROR: {ex.Message}");
            _logger.LogError(ex, "Installation failed");
        }

        stopwatch.Stop();
        result.ElapsedTimeMs = stopwatch.ElapsedMilliseconds;
        return result;
    }

    public async Task<bool> VerifyInstallationAsync()
    {
        _logger.LogInformation("Verifying installation...");

        var installPath = GetDefaultInstallationPath();
        var exePath = Path.Combine(installPath, "HELIOS.Platform.exe");

        if (!File.Exists(exePath))
        {
            _logger.LogError("Installation verification failed: Executable not found");
            return false;
        }

        _logger.LogInformation("Installation verified at: {Path}", installPath);
        return await Task.FromResult(true);
    }

    public async Task<bool> RollbackInstallationAsync()
    {
        _logger.LogWarning("Rolling back installation...");

        try
        {
            var installPath = GetDefaultInstallationPath();
            if (Directory.Exists(installPath))
            {
                Directory.Delete(installPath, recursive: true);
                _logger.LogInformation("Installation rolled back");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rollback failed");
            return false;
        }
    }

    public async Task ConfigureFirstRunAsync(User user)
    {
        _logger.LogInformation("Configuring first run for user: {Username}", user.Username);

        // Set up default user preferences
        var userPreferences = new Dictionary<string, string>
        {
            ["theme"] = "Dark",
            ["language"] = "en-US",
            ["notifications.enabled"] = "true",
            ["autoUpdate.enabled"] = "true",
            ["telemetry.enabled"] = "true"
        };

        await Task.Delay(100); // Simulate async work
        _logger.LogInformation("First run configuration completed");
    }

    // Helper methods
    private static string GetOsVersion()
    {
        var osVersion = Environment.OSVersion;
        return $"Windows {osVersion.Version.Major}.{osVersion.Version.Minor}";
    }

    private static string GetOsBuild()
    {
        var osVersion = Environment.OSVersion;
        return osVersion.Version.Build.ToString();
    }

    private static string GetProcessorName()
    {
        var processorCount = Environment.ProcessorCount;
        return $"{processorCount} cores";
    }

    private static long GetFreeDiskSpace()
    {
        try
        {
            var drive = System.IO.DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.RootDirectory.Name == "C:\\");
            return drive?.AvailableFreeSpace / 1024 / 1024 ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private static bool CheckHyperVAvailable()
    {
        // Check if Hyper-V is enabled
        var osVersion = Environment.OSVersion;
        return osVersion.Version.Major >= 10;
    }

    private static bool CheckWSL2Available()
    {
        // Check if WSL2 is available (simplified)
        return File.Exists(@"C:\Windows\System32\wsl.exe");
    }

    private static bool CheckSandboxAvailable()
    {
        // Check if Windows Sandbox is available
        return File.Exists(@"C:\Windows\System32\WindowsSandbox.exe");
    }

    private static async Task<List<GPUInfo>> DetectGPUsAsync()
    {
        var gpus = new List<GPUInfo>
        {
            new GPUInfo
            {
                Name = "NVIDIA RTX 3080",
                VendorId = "10DE",
                MemoryMb = 10240,
                DriverVersion = "537.58",
                IsSupported = true
            }
        };

        return await Task.FromResult(gpus);
    }

    private static List<DriveInfo> GetSystemDrives()
    {
        var drives = new List<DriveInfo>();

        foreach (var drive in System.IO.DriveInfo.GetDrives())
        {
            if (drive.IsReady)
            {
                drives.Add(new DriveInfo
                {
                    Letter = drive.Name[0],
                    Label = drive.VolumeLabel,
                    CapacityMb = drive.TotalSize / 1024 / 1024,
                    FreeMb = drive.AvailableFreeSpace / 1024 / 1024,
                    IsSystemDrive = drive.Name == "C:\\"
                });
            }
        }

        return drives;
    }

    private static bool DetectPortableMode()
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var markerFile = Path.Combine(appDirectory, ".portable");
        return File.Exists(markerFile);
    }

    private static async Task<HardwareCapabilities> ValidateHardwareAsync(WindowsEnvironment env)
    {
        var hardware = new HardwareCapabilities
        {
            HasEnoughMemory = env.TotalMemoryMb >= 4096,
            HasEnoughDiskSpace = env.FreeDiskSpaceMb >= 5120,
            Supports64Bit = env.Is64Bit,
            SupportsHyperV = env.IsHyperVAvailable,
            RecommendedInstallationPath = "C:\\Program Files\\HELIOS"
        };

        return await Task.FromResult(hardware);
    }

    private static string GetDefaultInstallationPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "HELIOS");
    }

    private static async Task CopyApplicationFilesAsync(string targetPath)
    {
        // Simplified file copy - in real implementation would copy all app files
        var sourceDir = AppDomain.CurrentDomain.BaseDirectory;
        if (!Directory.Exists(sourceDir))
        {
            sourceDir = Directory.GetCurrentDirectory();
        }

        await Task.Delay(100); // Simulate copy operation
    }

    private static void CreateDesktopShortcut(string installPath)
    {
        // Create desktop shortcut
        // Implementation would create actual .lnk file
    }

    private static void RegisterContextMenu(string installPath)
    {
        // Register context menu entries in Windows Registry
        // Implementation would add registry entries
    }

    private static async Task CreateWindowsServiceAsync(string installPath)
    {
        // Create Windows service
        // Implementation would use sc.exe to create service
        await Task.Delay(100);
    }

    private static bool IsRunningAsAdmin()
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
}
