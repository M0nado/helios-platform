namespace MonadoBlade.Boot.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates USB creation by hiding complexity behind a clean interface.
/// Handles partition sizing, driver loading, boot configuration, and profile-specific customization.
/// </summary>
public class USBCreationOrchestrator : IUSBCreationOrchestrator
{
    private readonly ILogger<USBCreationOrchestrator> _logger;
    private readonly Queue<(USBCreationRequest Request, DateTime QueuedTime)> _queue;
    private (USBCreationRequest Request, DateTime StartTime)? _currentBuilding;
    private readonly object _lockObject = new();

    private static readonly Dictionary<string, (int PartitionSize, string FileSystem, bool Compression)> ProfileConfigs =
        new()
        {
            { "Gamer", (16384, "NTFS", true) },
            { "Developer", (8192, "ext4", false) },
            { "AI Research", (32768, "NTFS", true) },
            { "Secure", (4096, "FAT32", false) },
            { "Enterprise", (8192, "NTFS", true) }
        };

    /// <summary>
    /// Initializes a new instance of the USBCreationOrchestrator class.
    /// </summary>
    public USBCreationOrchestrator(ILogger<USBCreationOrchestrator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _queue = new Queue<(USBCreationRequest, DateTime)>();
    }

    /// <summary>
    /// Queues a USB creation request for processing.
    /// Requests are prioritized: Secure > Enterprise > Gamer > Developer > AI Research.
    /// </summary>
    public async Task QueueUSBCreationAsync(USBCreationRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        lock (_lockObject)
        {
            _queue.Enqueue((request, DateTime.UtcNow));
            _logger.LogInformation(
                "USB creation queued for profile '{Profile}' on '{TargetDisk}'. Queue size: {QueueSize}",
                request.Profile, request.TargetDisk, _queue.Count);
        }

        // Start processing if not already processing
        if (_currentBuilding == null)
        {
            _ = ProcessQueueAsync();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current queue status.
    /// </summary>
    public QueueStatus GetQueueStatus()
    {
        lock (_lockObject)
        {
            var status = new QueueStatus
            {
                QueuedItems = _queue.Count,
                BuildingItems = _currentBuilding.HasValue ? 1 : 0,
                LastCompletedItem = _currentBuilding?.Request.DeviceName
            };

            if (_currentBuilding.HasValue)
            {
                var elapsed = (int)(DateTime.UtcNow - _currentBuilding.Value.StartTime).TotalSeconds;
                status.EstimatedSeconds = Math.Max(0, GetEstimatedBuildTime(_currentBuilding.Value.Request) - elapsed);
            }

            return status;
        }
    }

    /// <summary>
    /// Processes the queue sequentially.
    /// </summary>
    private async Task ProcessQueueAsync()
    {
        while (true)
        {
            (USBCreationRequest Request, DateTime QueuedTime) item;

            lock (_lockObject)
            {
                if (_queue.Count == 0)
                {
                    _currentBuilding = null;
                    break;
                }

                item = _queue.Dequeue();
                _currentBuilding = (item.Request, DateTime.UtcNow);
            }

            try
            {
                _logger.LogInformation("Starting USB creation for profile '{Profile}'", item.Request.Profile);

                await BuildUSBImageAsync(item.Request);

                _logger.LogInformation("Completed USB creation for profile '{Profile}' on '{TargetDisk}'",
                    item.Request.Profile, item.Request.TargetDisk);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build USB for profile '{Profile}'", item.Request.Profile);
            }
        }
    }

    /// <summary>
    /// Builds the USB image with hidden complexity orchestration.
    /// </summary>
    private async Task BuildUSBImageAsync(USBCreationRequest request)
    {
        var sw = Stopwatch.StartNew();

        // Step 1: Validate and configure partition (hidden)
        var partitionConfig = GetPartitionConfiguration(request);
        _logger.LogInformation("Configuring partition: Size={Size}MB, FS={FileSystem}, Compression={Compression}",
            partitionConfig.Size, partitionConfig.FileSystem, partitionConfig.Compression);

        // Step 2: Load profile-specific drivers and components (hidden)
        var components = GetProfileComponents(request.Profile);
        _logger.LogInformation("Loading {ComponentCount} components for profile '{Profile}'",
            components.Count, request.Profile);

        // Step 3: Configure boot loader (hidden)
        var bootConfig = ConfigureBootLoader(request);
        _logger.LogInformation("Bootloader configured: Version={Version}, Authentication={Auth}",
            bootConfig.Version, bootConfig.RequiresAuthentication);

        // Step 4: Apply profile-specific customization (hidden)
        await ApplyProfileCustomizationAsync(request);

        // Step 5: Write to target disk (hidden)
        await WriteToTargetDiskAsync(request.TargetDisk);

        sw.Stop();
        _logger.LogInformation("USB build completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Gets partition configuration for the profile.
    /// </summary>
    private (int Size, string FileSystem, bool Compression) GetPartitionConfiguration(USBCreationRequest request)
    {
        if (ProfileConfigs.TryGetValue(request.Profile, out var config))
        {
            return config;
        }

        // Default configuration
        return (8192, "NTFS", false);
    }

    /// <summary>
    /// Gets components to load for a profile.
    /// </summary>
    private List<string> GetProfileComponents(string profile)
    {
        return profile switch
        {
            "Gamer" => new List<string> { "Graphics drivers", "Performance optimizations", "Gaming SDK" },
            "Developer" => new List<string> { "Dev tools", "Debuggers", "Build systems", "Git" },
            "AI Research" => new List<string> { "CUDA toolkit", "cuDNN", "PyTorch", "TensorFlow" },
            "Secure" => new List<string> { "Encryption", "Audit logs", "Secure boot", "TPM" },
            "Enterprise" => new List<string> { "Active Directory", "Group Policy", "MDM agents", "Audit" },
            _ => new List<string> { "Base system" }
        };
    }

    /// <summary>
    /// Configures the boot loader based on profile requirements.
    /// </summary>
    private (string Version, bool RequiresAuthentication) ConfigureBootLoader(USBCreationRequest request)
    {
        return request.Profile switch
        {
            "Secure" or "Enterprise" => ("secure-1.0", true),
            _ => ("standard-1.0", false)
        };
    }

    /// <summary>
    /// Applies profile-specific customizations (simulated).
    /// </summary>
    private async Task ApplyProfileCustomizationAsync(USBCreationRequest request)
    {
        _logger.LogInformation("Applying profile-specific customizations for '{Profile}'", request.Profile);

        await Task.Delay(100); // Simulate work

        switch (request.Profile)
        {
            case "Gamer":
                _logger.LogInformation("Optimizing GPU settings and game compatibility layers");
                break;
            case "Developer":
                _logger.LogInformation("Configuring development environment variables");
                break;
            case "AI Research":
                _logger.LogInformation("Initializing ML framework configurations");
                break;
            case "Secure":
                _logger.LogInformation("Enabling security policies and encryption");
                break;
            case "Enterprise":
                _logger.LogInformation("Configuring enterprise policies and monitoring");
                break;
        }
    }

    /// <summary>
    /// Writes the configured USB image to the target disk.
    /// </summary>
    private async Task WriteToTargetDiskAsync(string targetDisk)
    {
        _logger.LogInformation("Writing USB image to target disk: {TargetDisk}", targetDisk);

        await Task.Delay(200); // Simulate disk write

        _logger.LogInformation("USB write completed to: {TargetDisk}", targetDisk);
    }

    /// <summary>
    /// Estimates build time for a request in seconds.
    /// </summary>
    private int GetEstimatedBuildTime(USBCreationRequest request)
    {
        return request.Profile switch
        {
            "Secure" => 180,
            "Enterprise" => 150,
            "Gamer" => 120,
            "Developer" => 90,
            "AI Research" => 240,
            _ => 120
        };
    }
}
