using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Observability.Interfaces;

/// <summary>
/// Grafana dashboard provisioning and integration service.
/// Manages dashboard creation, updates, templating, and variable management.
/// </summary>
public interface IGrafanaIntegration
{
    /// <summary>
    /// Create a new Grafana dashboard with specified panels and configuration.
    /// </summary>
    /// <param name="dashboardName">Name of the dashboard</param>
    /// <param name="description">Dashboard description</param>
    /// <param name="tags">Tags for organization</param>
    /// <param name="panels">Panel definitions</param>
    /// <returns>Dashboard ID and URL</returns>
    Task<(int DashboardId, string Url)> CreateDashboardAsync(
        string dashboardName,
        string description,
        List<string> tags,
        List<GrafanaPanel> panels);

    /// <summary>
    /// Update an existing dashboard.
    /// </summary>
    Task<bool> UpdateDashboardAsync(int dashboardId, List<GrafanaPanel> panels);

    /// <summary>
    /// Provision dashboard from JSON template.
    /// </summary>
    Task<int> ProvisionFromTemplateAsync(string jsonTemplate, string? folderId = null);

    /// <summary>
    /// Create a Grafana data source (Prometheus, InfluxDB, etc).
    /// </summary>
    Task<int> CreateDataSourceAsync(string name, string dsType, string url, Dictionary<string, object>? settings = null);

    /// <summary>
    /// Test connection to a data source.
    /// </summary>
    Task<bool> TestDataSourceAsync(int dataSourceId);

    /// <summary>
    /// Get all dashboards matching criteria.
    /// </summary>
    Task<List<GrafanaDashboard>> GetDashboardsAsync(string? search = null, string? tag = null);

    /// <summary>
    /// Delete a dashboard.
    /// </summary>
    Task<bool> DeleteDashboardAsync(int dashboardId);

    /// <summary>
    /// Export dashboard as JSON.
    /// </summary>
    Task<string> ExportDashboardAsync(int dashboardId);

    /// <summary>
    /// Set alert rules on a dashboard.
    /// </summary>
    Task<bool> SetAlertRulesAsync(int dashboardId, List<GrafanaAlert> alerts);
}

/// <summary>
/// Grafana panel definition.
/// </summary>
public class GrafanaPanel
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = "graph";
    public int GridPos { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public string Legend { get; set; } = "avg";
    public Dictionary<string, object> Options { get; set; } = new();
}

/// <summary>
/// Grafana dashboard metadata.
/// </summary>
public class GrafanaDashboard
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Grafana alert rule.
/// </summary>
public class GrafanaAlert
{
    public string Name { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public double Threshold { get; set; }
    public string Evaluator { get; set; } = "gt";
    public TimeSpan Duration { get; set; }
    public string? Message { get; set; }
}
