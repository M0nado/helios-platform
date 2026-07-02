# v3.0 Stream D Bootstrap - Quick Start Guide

## Overview
Three production-ready components for transparent, automated installations in Monado Blade v3.0:
1. **SystemPreflightChecker** - Pre-installation validation
2. **SilentInstallationManager** - Background installer orchestration
3. **ModernProgressUI** - Beautiful progress tracking

---

## Component 1: System Pre-Flight Checker

### Purpose
Verify system readiness BEFORE starting the installation wizard.

### Basic Usage

```csharp
using MonadoBlade.Boot;

// Create checker (auto-uses Serilog if available)
var checker = new SystemPreflightChecker();

// Run all 7 checks (max 30 seconds)
var result = await checker.RunChecksAsync();

// Check results
if (result.AllChecksPassed)
{
    Console.WriteLine("✅ System ready for installation!");
}
else
{
    Console.WriteLine($"⚠️ {result.Results.Count(r => !r.Passed)} issues found:");
    foreach (var check in result.Results.Where(r => !r.Passed))
    {
        Console.WriteLine($"❌ {check.CheckName}: {check.Message}");
        Console.WriteLine($"   Fix: {check.Suggestion}");
    }
}

// Cleanup
checker.Dispose();
```

### Checks Performed
1. **Network Connectivity** - DNS + ICMP ping to 8.8.8.8
2. **Disk Space** - Minimum 100GB available
3. **Admin Permissions** - Running as Administrator
4. **USB Ports** - At least one USB 3.0 port
5. **BitLocker Status** - Warning if enabled
6. **Antivirus Exclusions** - Check installation directory
7. **Firewall Rules** - Verify configuration

### Result Properties
```csharp
result.AllChecksPassed      // bool - all checks passed
result.TotalChecks          // int  - 7
result.PassedChecks         // int  - number passed
result.ElapsedTime          // TimeSpan - duration
result.Results              // List<PreflightCheckResult>
  ├── CheckName             // Name of check
  ├── Passed                // bool - passed/failed
  ├── Message               // User-friendly message
  └── Suggestion            // Remediation hint
```

---

## Component 2: Silent Installation Manager

### Purpose
Orchestrate background installations with zero user pop-ups.

### Basic Usage

```csharp
using MonadoBlade.Boot;

var manager = new SilentInstallationManager();

// Subscribe to events
manager.TaskStarted += msg => Console.WriteLine($"📦 {msg}");
manager.ProgressChanged += (step, pct) => Console.WriteLine($"{step}: {pct}%");
manager.TaskCompleted += result =>
{
    if (result.Success)
        Console.WriteLine($"✅ {result.TaskId} completed");
    else
        Console.WriteLine($"❌ {result.TaskId} failed: {result.ErrorMessage}");
};

// Create installation tasks
var gpuTask = new InstallationTask
{
    Id = "gpu-drivers",
    Name = "GPU Drivers",
    ExecutablePath = "C:\\Installers\\nvidia-driver.exe",
    Arguments = "/S /NoReboot",
    MaxRetries = 3,
    TimeoutSeconds = 300
};

// Execute (auto-retries up to 3 times)
var result = await manager.ExecuteInstallationAsync(gpuTask);

if (!result.Success)
{
    Console.WriteLine($"Failed after {result.Attempt} attempts");
    Console.WriteLine($"Error: {result.ErrorMessage}");
    Console.WriteLine($"Fix: {result.Suggestion}");
}

// Export comprehensive report
var report = manager.ExportReport();
Console.WriteLine(report);

// Access logs
var logDir = manager.GetLogDirectoryPath();
Console.WriteLine($"Logs available at: {logDir}");

manager.Dispose();
```

### Task Configuration
```csharp
new InstallationTask
{
    Id = "unique-id",                    // Unique identifier
    Name = "Display Name",               // User-friendly name
    ExecutablePath = "C:\\installer.exe", // Full path to executable
    Arguments = "/S /D=C:\\Install",     // Command-line arguments
    SilentMode = true,                   // Always true
    TimeoutSeconds = 300,                // 5-minute timeout
    MaxRetries = 3,                      // Retry attempts
    SuccessIndicator = output =>         // Optional custom success check
        output.Contains("completed"),
    Description = "Installing feature..." // Display description
};
```

### Result Tracking
```csharp
result.TaskId              // Which task
result.Success             // bool - passed
result.Attempt             // 1, 2, or 3
result.ErrorMessage        // null if success
result.Output              // Captured output
result.Duration            // TimeSpan
result.Suggestion          // Next steps if failed
```

### Logging
- All output logged to: `%APPDATA%\MonadoBladeInstallLogs\`
- Files: `{task-id}_{timestamp}.log`
- Available even if installation fails

---

## Component 3: Modern Progress UI

### Purpose
Display beautiful, responsive progress with accurate time estimates.

### Basic Usage

```csharp
using MonadoBlade.Boot;

var progress = new ModernProgressUI();

// Subscribe to events
progress.ProgressUpdated += report =>
{
    Console.WriteLine(ModernProgressUI.GetProgressBarVisualization(
        report.Percentage));
    Console.WriteLine($"{report.StepDescription}");
    Console.WriteLine($"ETA: {report.TimeRemaining}");
};

progress.StatusMessageChanged += msg => Console.WriteLine($"Status: {msg}");

// Start tracking
progress.Start();

// Simulate installation steps
for (int i = 0; i <= 100; i += 10)
{
    progress.ReportProgress(
        percentage: i,
        stepDescription: $"Installing component {i / 10}",
        currentStep: i / 10,
        totalSteps: 10,
        subStepInfo: $"Processing: {i * 2} / 200 MB",
        icon: ProgressStepIcon.Installing,
        isIndeterminate: false
    );
    
    await Task.Delay(1000);
}

// Get final stats
var (elapsed, finalProgress) = progress.Stop();
Console.WriteLine($"Completed in {elapsed.TotalSeconds}s");

progress.Dispose();
```

### Progress Report
```csharp
var report = progress.GetCurrentProgress();

report.Percentage                  // 0-100 double
report.StepDescription             // "Installing GPU drivers"
report.CurrentStep                 // 3
report.TotalSteps                  // 8
report.SubStepInfo                 // "100MB / 200MB downloaded"
report.ElapsedTime                 // TimeSpan
report.EstimatedTimeRemaining      // TimeSpan or null
report.StepIcon                    // ProgressStepIcon enum
report.IsIndeterminate             // bool
report.TimeRemaining               // Formatted string: "2m 30s"
report.ElapsedTimeFormatted        // Formatted string: "45s"
```

### Progress Bar Visualization
```csharp
// Get formatted progress bar
var bar = ModernProgressUI.GetProgressBarVisualization(75);
// Output: [████████████████░░░░] 75%

// Get step indicator
var step = ModernProgressUI.GetStepIndicator(3, 8);
// Output: "Step 3 of 8"

// Get icon for step type
var icon = ModernProgressUI.GetIconCharacter(ProgressStepIcon.Installing);
// Output: "⚙️ "
```

### Step Icons Available
- 📦 **Preparing** - Initial setup
- ⬇️ **Downloading** - Fetching files
- ⚙️ **Installing** - Active installation
- ✓ **Verifying** - Post-install checks
- ⏳ **Processing** - Generic work
- 🔨 **Finalizing** - Cleanup/final steps
- ✅ **Success** - Completion
- ❌ **Error** - Failure

### ETA Calculation
- Uses recent progress velocity (last 30 seconds)
- Falls back to overall velocity if insufficient history
- Clamped between 1 second and 12 hours
- Within ±20% accuracy for normal installations

---

## Integration Pattern

### Complete Installation Flow

```csharp
// 1. Pre-flight checks
var preflight = new SystemPreflightChecker();
var preflightResult = await preflight.RunChecksAsync();

if (!preflightResult.AllChecksPassed)
{
    // Show issues and suggestions to user
    return;
}

// 2. Initialize progress UI
var progress = new ModernProgressUI();
progress.Start();

// 3. Execute installations with progress tracking
var installer = new SilentInstallationManager();
var tasks = new[]
{
    new InstallationTask { /* GPU drivers */ },
    new InstallationTask { /* Razer Synapse */ },
    new InstallationTask { /* Monado Engine */ }
};

double currentStep = 0;
foreach (var task in tasks)
{
    progress.ReportProgress(
        currentStep / tasks.Length * 100,
        $"Installing {task.Name}",
        (int)currentStep,
        tasks.Length,
        icon: ProgressStepIcon.Installing
    );
    
    var result = await installer.ExecuteInstallationAsync(task);
    
    if (!result.Success)
    {
        progress.ReportProgress(
            100,
            $"Installation failed",
            tasks.Length,
            tasks.Length,
            icon: ProgressStepIcon.Error
        );
        break;
    }
    
    currentStep++;
}

// 4. Show completion
progress.Stop();
```

---

## Thread Safety

All components are **thread-safe**:
- ✅ Safe to call from UI thread
- ✅ Safe to call from background workers
- ✅ Safe concurrent progress updates
- ✅ Lock-based synchronization internally

---

## Exception Handling

### SystemPreflightChecker
- Operations timeout after 30 seconds
- No exceptions thrown - all errors wrapped in `PreflightCheckResult`

### SilentInstallationManager
- Process timeout → retries automatically
- File not found → throws `FileNotFoundException`
- Exit code != 0 → retries or returns failure

### ModernProgressUI
- Percentage auto-clamped to 0-100
- Progress never decreases
- ETA calculation handles edge cases
- Thread-safe all operations

---

## Performance Targets

| Component | Target | Typical |
|-----------|--------|---------|
| Pre-flight checks | <30s | 3-25s |
| Installation manager | N/A | Background |
| Progress UI updates | <100ms | <50ms |
| Memory footprint | <50MB | ~20MB |

---

## Next Steps

1. **Integrate with UI** - Wrap `ModernProgressUI` in WPF/Windows Forms
2. **Configure installers** - Map actual GPU/Razer/engine installers to tasks
3. **Handle failures** - Implement retry UI for user
4. **Test scenarios** - Run with various system configurations
5. **Monitor telemetry** - Use ETA accuracy for improvements

---

## Support

For issues or questions about the bootstrap components:
1. Check unit tests in `MonadoBlade.Tests.Unit/`
2. Review logs in `%APPDATA%\MonadoBladeInstallLogs\`
3. Check `STREAM_D_BOOTSTRAP_COMPLETE.md` for architecture details

---

**Last Updated:** 2024
**Status:** Production Ready ✅
