using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Core.Server
{
    /// <summary>
    /// Deployment configuration and status information.
    /// </summary>
    public class DeploymentInfo
    {
        public string DeploymentId { get; set; } = Guid.NewGuid().ToString();
        public string ApplicationName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DeploymentType DeploymentType { get; set; } = DeploymentType.Standard;
        public DeploymentStatus Status { get; set; } = DeploymentStatus.Pending;
        public List<string> TargetServers { get; set; } = new();
        public List<string> SuccessfulServers { get; set; } = new();
        public List<string> FailedServers { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? PreviousVersion { get; set; }
        public bool CanRollback { get; set; }
        public string? ErrorMessage { get; set; }
        public int CompletionPercentage { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public enum DeploymentType
    {
        Standard = 0,
        BlueGreen = 1,
        RollingUpdate = 2,
        Canary = 3
    }

    public enum DeploymentStatus
    {
        Pending = 0,
        InProgress = 1,
        Verifying = 2,
        Completed = 3,
        RolledBack = 4,
        Failed = 5,
        Cancelled = 6
    }

    /// <summary>
    /// Rolling update deployment configuration.
    /// </summary>
    public class RollingUpdateConfig
    {
        public List<int> StagedPercentages { get; set; } = new() { 25, 50, 75, 100 };
        public int WaitBetweenStages { get; set; } = 60;
        public int HealthCheckTimeout { get; set; } = 120;
        public int MaxConcurrentServers { get; set; } = 5;
    }

    /// <summary>
    /// Canary deployment configuration.
    /// </summary>
    public class CanaryConfig
    {
        public int CanaryServerCount { get; set; } = 1;
        public int CanaryDuration { get; set; } = 300;
        public int MetricsCheckInterval { get; set; } = 30;
        public double ErrorRateThreshold { get; set; } = 5.0;
        public double LatencyThreshold { get; set; } = 20.0;
        public bool AutoPromote { get; set; } = true;
    }

    /// <summary>
    /// Blue/Green deployment configuration.
    /// </summary>
    public class BlueGreenConfig
    {
        public string BlueEnvironment { get; set; } = string.Empty;
        public string GreenEnvironment { get; set; } = string.Empty;
        public int TrafficSwitchTimeout { get; set; } = 300;
        public bool ValidateBeforeSwitch { get; set; } = true;
    }
}
