using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Observability.Interfaces;

/// <summary>
/// Dashboard management service for creating, organizing, and rendering monitoring dashboards.
/// </summary>
public interface IDashboardManager
{
    /// <summary>
    /// Create a new custom dashboard.
    /// </summary>
    Task<DashboardDefinition> CreateDashboardAsync(
        string name,
        string description,
        DashboardType type,
        List<WidgetConfig> widgets);

    /// <summary>
    /// Update an existing dashboard.
    /// </summary>
    Task<bool> UpdateDashboardAsync(string dashboardId, List<WidgetConfig> widgets);

    /// <summary>
    /// Clone an existing dashboard.
    /// </summary>
    Task<DashboardDefinition> CloneDashboardAsync(string sourceDashboardId, string newName);

    /// <summary>
    /// Get dashboard by ID.
    /// </summary>
    Task<DashboardDefinition> GetDashboardAsync(string dashboardId);

    /// <summary>
    /// List all dashboards with optional filtering.
    /// </summary>
    Task<List<DashboardDefinition>> ListDashboardsAsync(string? type = null, string? tag = null);

    /// <summary>
    /// Delete a dashboard.
    /// </summary>
    Task<bool> DeleteDashboardAsync(string dashboardId);

    /// <summary>
    /// Share dashboard with users or teams.
    /// </summary>
    Task<bool> ShareDashboardAsync(string dashboardId, List<string> userIds, DashboardPermission permission);

    /// <summary>
    /// Set dashboard refresh interval.
    /// </summary>
    Task<bool> SetRefreshIntervalAsync(string dashboardId, TimeSpan interval);

    /// <summary>
    /// Export dashboard as JSON.
    /// </summary>
    Task<string> ExportDashboardAsync(string dashboardId);

    /// <summary>
    /// Import dashboard from JSON.
    /// </summary>
    Task<DashboardDefinition> ImportDashboardAsync(string jsonDefinition);

    /// <summary>
    /// Add dashboard to favorites.
    /// </summary>
    Task<bool> AddFavoriteAsync(string dashboardId, string userId);

    /// <summary>
    /// Get popular dashboards.
    /// </summary>
    Task<List<DashboardDefinition>> GetPopularDashboardsAsync(int topN = 10);
}

/// <summary>
/// Dashboard type enumeration.
/// </summary>
public enum DashboardType
{
    SystemOverview,
    ApplicationPerformance,
    SecurityMonitoring,
    CapacityPlanning,
    CustomMetrics,
    SLA
}

/// <summary>
/// Dashboard permission level.
/// </summary>
public enum DashboardPermission
{
    View,
    Edit,
    Admin
}

/// <summary>
/// Dashboard definition.
/// </summary>
public class DashboardDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DashboardType Type { get; set; }
    public List<WidgetConfig> Widgets { get; set; } = new();
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromSeconds(30);
    public List<string> Tags { get; set; } = new();
    public bool IsPublic { get; set; }
}

/// <summary>
/// Widget configuration for dashboard panels.
/// </summary>
public class WidgetConfig
{
    public string WidgetId { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public WidgetType Type { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
    public string MetricQuery { get; set; } = string.Empty;
}

/// <summary>
/// Widget type enumeration.
/// </summary>
public enum WidgetType
{
    TimeSeries,
    Gauge,
    Stat,
    Table,
    Pie,
    Bar,
    Heatmap,
    Alert,
    Text,
    SingleStat
}
