using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HELIOS.Platform.Presentation.Studio.Models;

namespace HELIOS.Platform.Presentation.Studio.Services
{
    /// <summary>
    /// Generates reports for system metrics and performance analysis
    /// Supports daily, weekly, and monthly reports with PDF export
    /// </summary>
    public class ReportGenerator
    {
        private readonly PerformanceGraphService _graphService;
        private readonly List<Report> _generatedReports;

        public ReportGenerator(PerformanceGraphService graphService)
        {
            _graphService = graphService;
            _generatedReports = new List<Report>();
        }

        /// <summary>
        /// Generate a daily report
        /// </summary>
        public Report GenerateDailyReport(string title = null, string description = null)
        {
            var report = new Report
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = title ?? $"Daily Report - {DateTime.UtcNow:yyyy-MM-dd}",
                Description = description ?? "Daily system performance report",
                GeneratedAt = DateTime.UtcNow,
                ReportType = ReportType.Daily,
                Period = ReportPeriod.Daily
            };

            PopulateReportMetrics(report, TimeSpan.FromHours(24));
            _generatedReports.Add(report);

            return report;
        }

        /// <summary>
        /// Generate a weekly report
        /// </summary>
        public Report GenerateWeeklyReport(string title = null, string description = null)
        {
            var report = new Report
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = title ?? $"Weekly Report - {DateTime.UtcNow:yyyy-MM-dd}",
                Description = description ?? "Weekly system performance report",
                GeneratedAt = DateTime.UtcNow,
                ReportType = ReportType.Weekly,
                Period = ReportPeriod.Weekly
            };

            PopulateReportMetrics(report, TimeSpan.FromDays(7));
            _generatedReports.Add(report);

            return report;
        }

        /// <summary>
        /// Generate a monthly report
        /// </summary>
        public Report GenerateMonthlyReport(string title = null, string description = null)
        {
            var report = new Report
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = title ?? $"Monthly Report - {DateTime.UtcNow:yyyy-MM}",
                Description = description ?? "Monthly system performance report",
                GeneratedAt = DateTime.UtcNow,
                ReportType = ReportType.Monthly,
                Period = ReportPeriod.Monthly
            };

            PopulateReportMetrics(report, TimeSpan.FromDays(30));
            _generatedReports.Add(report);

            return report;
        }

        /// <summary>
        /// Generate custom period report
        /// </summary>
        public Report GenerateCustomReport(TimeSpan period, string title = null)
        {
            var report = new Report
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = title ?? $"Custom Report - {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                Description = $"Custom {period.TotalHours:F1}-hour report",
                GeneratedAt = DateTime.UtcNow,
                ReportType = ReportType.Custom,
                Period = ReportPeriod.Custom
            };

            PopulateReportMetrics(report, period);
            _generatedReports.Add(report);

            return report;
        }

        /// <summary>
        /// Export report to HTML
        /// </summary>
        public string ExportToHtml(Report report)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset=\"UTF-8\">");
            html.AppendLine($"<title>{report.Title}</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }");
            html.AppendLine(".container { max-width: 1200px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            html.AppendLine("h1 { color: #333; border-bottom: 2px solid #007bff; padding-bottom: 10px; }");
            html.AppendLine(".metric-card { display: inline-block; width: 30%; margin: 10px; padding: 15px; border: 1px solid #ddd; border-radius: 5px; background-color: #f9f9f9; }");
            html.AppendLine(".metric-label { font-weight: bold; color: #555; }");
            html.AppendLine(".metric-value { font-size: 24px; color: #007bff; margin: 10px 0; }");
            html.AppendLine(".metric-unit { font-size: 12px; color: #999; }");
            html.AppendLine(".summary { margin-top: 20px; padding: 15px; background-color: #e7f3ff; border-left: 4px solid #007bff; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            html.AppendLine("<div class=\"container\">");
            html.AppendLine($"<h1>{report.Title}</h1>");
            html.AppendLine($"<p>{report.Description}</p>");
            html.AppendLine($"<p><strong>Generated:</strong> {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}</p>");

            // Metrics section
            html.AppendLine("<h2>Metrics Summary</h2>");
            foreach (var stat in report.MetricsStatistics)
            {
                html.AppendLine("<div class=\"metric-card\">");
                html.AppendLine($"<div class=\"metric-label\">{stat.MetricName}</div>");
                html.AppendLine($"<div class=\"metric-value\">{stat.CurrentValue:F2}%</div>");
                html.AppendLine($"<div>Avg: {stat.AverageValue:F2}% | Min: {stat.MinValue:F2}% | Max: {stat.MaxValue:F2}%</div>");
                html.AppendLine("</div>");
            }

            // Summary section
            html.AppendLine("<div class=\"summary\">");
            html.AppendLine("<h3>Executive Summary</h3>");
            html.AppendLine($"<p><strong>Report Type:</strong> {report.ReportType}</p>");
            html.AppendLine($"<p><strong>Period:</strong> {report.Period}</p>");
            html.AppendLine($"<p><strong>Data Points:</strong> {report.MetricsStatistics.Sum(s => s.DataPointCount)}</p>");
            html.AppendLine("</div>");

            html.AppendLine("</div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        /// <summary>
        /// Export report to CSV
        /// </summary>
        public string ExportToCSV(Report report)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Report Title,Metric,Current Value,Average,Min,Max,Data Points");
            csv.AppendLine($"\"{report.Title}\"");
            csv.AppendLine();

            foreach (var stat in report.MetricsStatistics)
            {
                csv.AppendLine($"{stat.MetricName},{stat.CurrentValue:F2},{stat.AverageValue:F2},{stat.MinValue:F2},{stat.MaxValue:F2},{stat.DataPointCount}");
            }

            return csv.ToString();
        }

        /// <summary>
        /// Export report as plain text
        /// </summary>
        public string ExportToText(Report report)
        {
            var text = new StringBuilder();
            text.AppendLine("================================================================================");
            text.AppendLine(report.Title);
            text.AppendLine("================================================================================");
            text.AppendLine();
            text.AppendLine(report.Description);
            text.AppendLine();
            text.AppendLine($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            text.AppendLine($"Report Type: {report.ReportType}");
            text.AppendLine($"Period: {report.Period}");
            text.AppendLine();
            text.AppendLine("METRICS SUMMARY");
            text.AppendLine("--------------------------------------------------------------------------------");

            foreach (var stat in report.MetricsStatistics)
            {
                text.AppendLine();
                text.AppendLine($"{stat.MetricName}:");
                text.AppendLine($"  Current Value: {stat.CurrentValue:F2}%");
                text.AppendLine($"  Average Value: {stat.AverageValue:F2}%");
                text.AppendLine($"  Minimum Value: {stat.MinValue:F2}%");
                text.AppendLine($"  Maximum Value: {stat.MaxValue:F2}%");
                text.AppendLine($"  Std Deviation: {stat.StandardDeviation:F2}");
                text.AppendLine($"  Data Points: {stat.DataPointCount}");
            }

            text.AppendLine();
            text.AppendLine("================================================================================");

            return text.ToString();
        }

        /// <summary>
        /// Get all generated reports
        /// </summary>
        public IEnumerable<Report> GetReports()
        {
            return _generatedReports.OrderByDescending(r => r.GeneratedAt);
        }

        /// <summary>
        /// Delete an old report
        /// </summary>
        public bool DeleteReport(string reportId)
        {
            return _generatedReports.RemoveAll(r => r.Id == reportId) > 0;
        }

        /// <summary>
        /// Clear reports older than specified days
        /// </summary>
        public void ClearOldReports(int daysToKeep = 30)
        {
            var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
            _generatedReports.RemoveAll(r => r.GeneratedAt < cutoff);
        }

        private void PopulateReportMetrics(Report report, TimeSpan period)
        {
            var metrics = new[] { "CPU", "Memory", "Disk", "Network", "GPU" };

            foreach (var metricName in metrics)
            {
                var stats = _graphService.GetMetricStatistics(metricName, period);
                if (stats != null)
                {
                    report.MetricsStatistics.Add(stats);
                }
            }
        }
    }

    /// <summary>
    /// Report data structure
    /// </summary>
    public class Report
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime GeneratedAt { get; set; }
        public ReportType ReportType { get; set; }
        public ReportPeriod Period { get; set; }
        public List<MetricStatistics> MetricsStatistics { get; set; } = new();
    }

    public enum ReportType
    {
        Daily,
        Weekly,
        Monthly,
        Custom
    }

    public enum ReportPeriod
    {
        Daily,
        Weekly,
        Monthly,
        Custom
    }
}
