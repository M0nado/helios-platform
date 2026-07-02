namespace MonadoBlade.Boot.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MonadoBlade.Core.Abstractions;

/// <summary>
/// Ultra-simplified USB creation wizard providing a single-page experience for users.
/// Hides all complexity in advanced mode (Ctrl+Shift+A).
/// </summary>
public class SimpleUSBWizard : IService
{
    private readonly ILogger<SimpleUSBWizard> _logger;
    private readonly IUSBCreationOrchestrator _orchestrator;
    private bool _advancedModeEnabled;

    /// <summary>
    /// Initializes a new instance of the SimpleUSBWizard class.
    /// </summary>
    public SimpleUSBWizard(ILogger<SimpleUSBWizard> logger, IUSBCreationOrchestrator orchestrator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _advancedModeEnabled = false;
    }

    /// <summary>
    /// Gets the system hostname for pre-filling the device name field.
    /// </summary>
    public string SystemHostname => Environment.MachineName;

    /// <summary>
    /// Gets available removable media drives filtered for USB.
    /// </summary>
    /// <returns>List of removable drive paths.</returns>
    public IReadOnlyList<string> GetAvailableRemovableDrives()
    {
        var drives = new List<string>();
        try
        {
            var driveInfo = DriveInfo.GetDrives();
            foreach (var drive in driveInfo)
            {
                if (drive.DriveType == DriveType.Removable && drive.IsReady)
                {
                    drives.Add(drive.Name.TrimEnd('\\'));
                    _logger.LogInformation("Found removable drive: {DriveName}", drive.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating removable drives");
        }

        return drives.AsReadOnly();
    }

    /// <summary>
    /// Gets available USB creation profiles.
    /// </summary>
    /// <returns>Enumeration of profile names.</returns>
    public IEnumerable<string> GetAvailableProfiles()
    {
        return new[] { "Gamer", "Developer", "AI Research", "Secure", "Enterprise" };
    }

    /// <summary>
    /// Initiates USB creation with the specified parameters.
    /// </summary>
    /// <param name="deviceName">The name for the USB device.</param>
    /// <param name="targetDisk">The target removable drive path.</param>
    /// <param name="profile">The profile to use (Gamer, Developer, AI Research, Secure, Enterprise).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CreateUSBAsync(string deviceName, string targetDisk, string profile)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentException("Device name cannot be empty", nameof(deviceName));
        }

        if (string.IsNullOrWhiteSpace(targetDisk))
        {
            throw new ArgumentException("Target disk cannot be empty", nameof(targetDisk));
        }

        if (string.IsNullOrWhiteSpace(profile))
        {
            throw new ArgumentException("Profile cannot be empty", nameof(profile));
        }

        try
        {
            _logger.LogInformation("Creating USB with profile '{Profile}' on drive '{TargetDisk}' named '{DeviceName}'",
                profile, targetDisk, deviceName);

            var request = new USBCreationRequest
            {
                DeviceName = deviceName,
                TargetDisk = targetDisk,
                Profile = profile,
                AdvancedModeEnabled = _advancedModeEnabled
            };

            await _orchestrator.QueueUSBCreationAsync(request);
            _logger.LogInformation("USB creation queued successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create USB");
            throw;
        }
    }

    /// <summary>
    /// Enables advanced mode when Ctrl+Shift+A is pressed.
    /// This provides access to additional configuration options.
    /// </summary>
    public void EnableAdvancedMode()
    {
        _advancedModeEnabled = true;
        _logger.LogInformation("Advanced mode enabled");
    }

    /// <summary>
    /// Disables advanced mode.
    /// </summary>
    public void DisableAdvancedMode()
    {
        _advancedModeEnabled = false;
        _logger.LogInformation("Advanced mode disabled");
    }

    /// <summary>
    /// Gets whether advanced mode is currently enabled.
    /// </summary>
    public bool IsAdvancedModeEnabled => _advancedModeEnabled;

    /// <summary>
    /// Gets extended configuration options (only available in advanced mode).
    /// </summary>
    /// <returns>Dictionary of advanced options with default values.</returns>
    public Dictionary<string, object> GetAdvancedOptions()
    {
        if (!_advancedModeEnabled)
        {
            _logger.LogWarning("Advanced options requested but advanced mode is not enabled");
            return new Dictionary<string, object>();
        }

        return new Dictionary<string, object>
        {
            { "PartitionSize", 0 },
            { "FileSystem", "FAT32" },
            { "EnableCompression", false },
            { "CustomDrivers", "" },
            { "BootLoaderVersion", "auto" },
            { "WriteVerification", true }
        };
    }
}

/// <summary>
/// Represents a USB creation request.
/// </summary>
public class USBCreationRequest
{
    /// <summary>
    /// Gets or sets the device name.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target disk path.
    /// </summary>
    public string TargetDisk { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation profile.
    /// </summary>
    public string Profile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether advanced mode is enabled.
    /// </summary>
    public bool AdvancedModeEnabled { get; set; }

    /// <summary>
    /// Gets or sets advanced configuration options.
    /// </summary>
    public Dictionary<string, object> AdvancedOptions { get; set; } = new();
}

/// <summary>
/// Interface for USB creation orchestration.
/// </summary>
public interface IUSBCreationOrchestrator : IService
{
    /// <summary>
    /// Queues a USB creation request for processing.
    /// </summary>
    /// <param name="request">The USB creation request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task QueueUSBCreationAsync(USBCreationRequest request);

    /// <summary>
    /// Gets the current queue status.
    /// </summary>
    /// <returns>The queue status.</returns>
    QueueStatus GetQueueStatus();
}

/// <summary>
/// Represents the status of the USB creation queue.
/// </summary>
public class QueueStatus
{
    /// <summary>
    /// Gets or sets the number of items in the queue.
    /// </summary>
    public int QueuedItems { get; set; }

    /// <summary>
    /// Gets or sets the currently building item count.
    /// </summary>
    public int BuildingItems { get; set; }

    /// <summary>
    /// Gets or sets the estimated time to completion in seconds.
    /// </summary>
    public int EstimatedSeconds { get; set; }

    /// <summary>
    /// Gets or sets the last completed item.
    /// </summary>
    public string? LastCompletedItem { get; set; }
}
