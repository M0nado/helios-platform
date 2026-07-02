namespace MonadoBlade.Boot;

using System.Diagnostics;
using MonadoBlade.Boot.Abstractions;
using Serilog;

/// <summary>
/// Represents progress information for display in the UI.
/// </summary>
public class ProgressReport
{
    /// <summary>
    /// Overall completion percentage (0-100).
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Current step description (e.g., "Installing GPU drivers").
    /// </summary>
    public string StepDescription { get; set; } = string.Empty;

    /// <summary>
    /// Current step number and total steps (e.g., "3 of 8").
    /// </summary>
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }

    /// <summary>
    /// Sub-step information (e.g., "Downloading drivers: 50MB / 200MB").
    /// </summary>
    public string? SubStepInfo { get; set; }

    /// <summary>
    /// Time elapsed since start.
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    /// Estimated time remaining.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Visual icon for current step (building, installing, verifying, etc).
    /// </summary>
    public ProgressStepIcon StepIcon { get; set; } = ProgressStepIcon.Processing;

    /// <summary>
    /// Whether progress is indeterminate (unknown duration).
    /// </summary>
    public bool IsIndeterminate { get; set; }

    /// <summary>
    /// Gets a human-readable time remaining string.
    /// </summary>
    public string TimeRemaining
    {
        get
        {
            if (EstimatedTimeRemaining == null)
                return "Calculating...";

            var remaining = EstimatedTimeRemaining.Value;
            if (remaining.TotalSeconds < 60)
                return $"{(int)remaining.TotalSeconds}s";
            if (remaining.TotalMinutes < 60)
                return $"{(int)remaining.TotalMinutes}m {remaining.Seconds}s";
            return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
        }
    }

    /// <summary>
    /// Gets a human-readable elapsed time string.
    /// </summary>
    public string ElapsedTimeFormatted
    {
        get
        {
            if (ElapsedTime.TotalSeconds < 60)
                return $"{(int)ElapsedTime.TotalSeconds}s";
            if (ElapsedTime.TotalMinutes < 60)
                return $"{(int)ElapsedTime.TotalMinutes}m {ElapsedTime.Seconds}s";
            return $"{(int)ElapsedTime.TotalHours}h {ElapsedTime.Minutes}m";
        }
    }
}

/// <summary>
/// Icons representing different installation steps.
/// </summary>
public enum ProgressStepIcon
{
    Preparing = 0,
    Downloading = 1,
    Installing = 2,
    Verifying = 3,
    Processing = 4,
    Finalizing = 5,
    Success = 6,
    Error = 7
}

/// <summary>
/// Modern progress UI manager with smooth animations and detailed status display.
/// Provides real-time progress updates with ETA calculations.
/// </summary>
public class ModernProgressUI : IBootService
{
    private readonly ILogger _logger;
    private readonly Stopwatch _stopwatch = new();
    private double _lastPercentage = 0;
    private DateTime _lastUpdateTime = DateTime.UtcNow;
    private readonly List<(DateTime Time, double Percentage)> _progressHistory = [];
    private readonly object _lockObject = new();

    public event Action<ProgressReport>? ProgressUpdated;
    public event Action<string>? StatusMessageChanged;

    public ModernProgressUI(ILogger? logger = null)
    {
        _logger = logger ?? Log.Logger;
    }

    /// <summary>
    /// Starts the progress timer.
    /// </summary>
    public void Start()
    {
        lock (_lockObject)
        {
            _stopwatch.Restart();
            _progressHistory.Clear();
            _lastPercentage = 0;
            _lastUpdateTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Reports progress update with automatic ETA calculation.
    /// </summary>
    public void ReportProgress(
        double percentage,
        string stepDescription,
        int currentStep,
        int totalSteps,
        string? subStepInfo = null,
        ProgressStepIcon icon = ProgressStepIcon.Processing,
        bool isIndeterminate = false)
    {
        lock (_lockObject)
        {
            // Clamp percentage between 0 and 100
            percentage = Math.Max(0, Math.Min(100, percentage));

            var now = DateTime.UtcNow;
            var elapsedTime = _stopwatch.Elapsed;

            // Prevent percentage from going backwards
            if (percentage < _lastPercentage)
                percentage = _lastPercentage;

            // Calculate ETA based on progress history
            var eta = CalculateETA(percentage, elapsedTime);

            // Record this progress point
            _progressHistory.Add((now, percentage));
            _lastPercentage = percentage;
            _lastUpdateTime = now;

            var report = new ProgressReport
            {
                Percentage = percentage,
                StepDescription = stepDescription,
                CurrentStep = currentStep,
                TotalSteps = totalSteps,
                SubStepInfo = subStepInfo,
                ElapsedTime = elapsedTime,
                EstimatedTimeRemaining = eta,
                StepIcon = icon,
                IsIndeterminate = isIndeterminate
            };

            OnProgressUpdated(report);

            _logger.Debug(
                "Progress: {StepDescription} {CurrentStep}/{TotalSteps} - {Percentage}% [{ElapsedTime}]",
                stepDescription, currentStep, totalSteps, percentage, report.ElapsedTimeFormatted);
        }
    }

    /// <summary>
    /// Reports a status message without changing progress percentage.
    /// </summary>
    public void ReportStatus(string message)
    {
        OnStatusMessageChanged(message);
        _logger.Information("Status: {Message}", message);
    }

    /// <summary>
    /// Gets the current progress report snapshot.
    /// </summary>
    public ProgressReport GetCurrentProgress()
    {
        lock (_lockObject)
        {
            var elapsedTime = _stopwatch.Elapsed;
            var eta = CalculateETA(_lastPercentage, elapsedTime);

            return new ProgressReport
            {
                Percentage = _lastPercentage,
                ElapsedTime = elapsedTime,
                EstimatedTimeRemaining = eta
            };
        }
    }

    /// <summary>
    /// Stops the progress timer and returns final statistics.
    /// </summary>
    public (TimeSpan ElapsedTime, double TotalProgress) Stop()
    {
        lock (_lockObject)
        {
            _stopwatch.Stop();
            return (_stopwatch.Elapsed, _lastPercentage);
        }
    }

    /// <summary>
    /// Calculates estimated time remaining based on progress velocity.
    /// </summary>
    private TimeSpan? CalculateETA(double currentPercentage, TimeSpan elapsedTime)
    {
        if (currentPercentage <= 0 || elapsedTime.TotalSeconds < 1)
            return null;

        // Need at least 2 historical data points for better accuracy
        if (_progressHistory.Count < 2)
        {
            // Simple linear extrapolation if not enough history
            var secondsPerPercent = elapsedTime.TotalSeconds / Math.Max(currentPercentage, 1);
            var remainingSeconds = secondsPerPercent * (100 - currentPercentage);
            return TimeSpan.FromSeconds(Math.Max(0, remainingSeconds));
        }

        // Use recent progress velocity (last 30 seconds) for more accurate ETA
        var recentThreshold = DateTime.UtcNow.AddSeconds(-30);
        var recentHistory = _progressHistory.Where(h => h.Time >= recentThreshold).ToList();

        double progressVelocity;

        if (recentHistory.Count >= 2)
        {
            var firstEntry = recentHistory.First();
            var lastEntry = recentHistory.Last();
            var timeDiff = (lastEntry.Time - firstEntry.Time).TotalSeconds;
            var progressDiff = lastEntry.Percentage - firstEntry.Percentage;

            progressVelocity = timeDiff > 0 ? progressDiff / timeDiff : 0;
        }
        else
        {
            // Fallback: overall average velocity
            progressVelocity = currentPercentage / Math.Max(elapsedTime.TotalSeconds, 1);
        }

        if (progressVelocity <= 0)
            return null;

        var remainingProgress = 100 - currentPercentage;
        var etaSeconds = remainingProgress / progressVelocity;

        // Clamp ETA between 1 second and 12 hours (reasonable limits)
        etaSeconds = Math.Max(1, Math.Min(etaSeconds, 12 * 3600));

        return TimeSpan.FromSeconds(etaSeconds);
    }

    /// <summary>
    /// Gets formatted progress bar visualization (visual representation).
    /// </summary>
    public static string GetProgressBarVisualization(double percentage, int width = 20)
    {
        var fillCount = (int)(percentage / 100.0 * width);
        var emptyCount = width - fillCount;

        return $"[{new string('█', fillCount)}{new string('░', emptyCount)}] {percentage:F0}%";
    }

    /// <summary>
    /// Gets a formatted step indicator (e.g., "Step 3 of 8").
    /// </summary>
    public static string GetStepIndicator(int currentStep, int totalSteps)
    {
        return $"Step {currentStep} of {totalSteps}";
    }

    /// <summary>
    /// Gets icon character for the given step type.
    /// </summary>
    public static string GetIconCharacter(ProgressStepIcon icon)
    {
        return icon switch
        {
            ProgressStepIcon.Preparing => "📦",
            ProgressStepIcon.Downloading => "⬇️ ",
            ProgressStepIcon.Installing => "⚙️ ",
            ProgressStepIcon.Verifying => "✓ ",
            ProgressStepIcon.Processing => "⏳",
            ProgressStepIcon.Finalizing => "🔨",
            ProgressStepIcon.Success => "✅",
            ProgressStepIcon.Error => "❌",
            _ => "▪ "
        };
    }

    /// <summary>
    /// Gets progress history data for analytics or display.
    /// </summary>
    public IReadOnlyList<(DateTime Time, double Percentage)> GetProgressHistory()
    {
        lock (_lockObject)
        {
            return _progressHistory.AsReadOnly();
        }
    }

    /// <summary>
    /// Resets the progress tracker.
    /// </summary>
    public void Reset()
    {
        lock (_lockObject)
        {
            _stopwatch.Reset();
            _progressHistory.Clear();
            _lastPercentage = 0;
            _lastUpdateTime = DateTime.UtcNow;
        }
    }

    private void OnProgressUpdated(ProgressReport report)
    {
        ProgressUpdated?.Invoke(report);
    }

    private void OnStatusMessageChanged(string message)
    {
        StatusMessageChanged?.Invoke(message);
    }

    public void Dispose()
    {
        // Stopwatch doesn't need explicit disposal in .NET 8.0+
    }
}
