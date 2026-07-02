namespace HELIOS.Platform.Optimization.UXAnalytics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class UserBehaviorEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; }
    public string EventType { get; set; } // Click, PageView, FormSubmit, Error, etc.
    public string Page { get; set; }
    public string Component { get; set; }
    public double DurationMs { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class UXPainPoint
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; }
    public string Description { get; set; }
    public string Location { get; set; } // Page or component
    public int AffectedUsers { get; set; }
    public double ImpactPercentage { get; set; }
    public string Severity { get; set; } // Low, Medium, High, Critical
    public List<string> SuggestedFixes { get; set; } = new();
    public DateTime IdentifiedAt { get; set; } = DateTime.UtcNow;
}

public class UserJourney
{
    public string UserId { get; set; }
    public List<UserBehaviorEvent> Events { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string ConversionStatus { get; set; } // Completed, Abandoned, etc.
    public double TimeSpentSeconds { get; set; }
}

public interface IUXAnalytics
{
    Task RecordUserEventAsync(UserBehaviorEvent uiEvent);
    Task<List<UXPainPoint>> IdentifyUXPainPointsAsync();
    Task<UserJourney> BuildUserJourneyAsync(string userId);
    Task<ABTestResult> AnalyzeUIABTestAsync(string testId);
    Task<List<string>> GenerateDesignRecommendationsAsync();
}

public class ABTestResult
{
    public string TestId { get; set; }
    public string VariantA { get; set; }
    public string VariantB { get; set; }
    public int SampleSizeA { get; set; }
    public int SampleSizeB { get; set; }
    public double ConversionRateA { get; set; }
    public double ConversionRateB { get; set; }
    public string Winner { get; set; }
    public double ConfidenceLevel { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

public class UXAnalytics : IUXAnalytics
{
    private readonly List<UserBehaviorEvent> _allEvents = new();
    private readonly Dictionary<string, List<UserBehaviorEvent>> _userSessions = new();
    private readonly object _lockObj = new();

    public async Task RecordUserEventAsync(UserBehaviorEvent uiEvent)
    {
        if (uiEvent == null) throw new ArgumentNullException(nameof(uiEvent));

        await Task.Run(() =>
        {
            lock (_lockObj)
            {
                _allEvents.Add(uiEvent);

                if (!_userSessions.ContainsKey(uiEvent.UserId))
                {
                    _userSessions[uiEvent.UserId] = new List<UserBehaviorEvent>();
                }

                _userSessions[uiEvent.UserId].Add(uiEvent);

                // Cleanup old data (keep only 30 days)
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                _allEvents.RemoveAll(e => e.Timestamp < thirtyDaysAgo);
            }
        });
    }

    public async Task<List<UXPainPoint>> IdentifyUXPainPointsAsync()
    {
        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                var painPoints = new List<UXPainPoint>();
                var eventsByComponent = _allEvents.GroupBy(e => e.Component)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var (component, events) in eventsByComponent)
                {
                    if (string.IsNullOrEmpty(component) || events.Count < 10)
                        continue;

                    var failureRate = events.Count(e => !e.Success) / (double)events.Count;
                    var avgDuration = events.Average(e => e.DurationMs);

                    // Identify high failure rate
                    if (failureRate > 0.1)
                    {
                        painPoints.Add(new UXPainPoint
                        {
                            Title = $"High Error Rate in {component}",
                            Description = $"{component} has {failureRate * 100:F1}% failure rate",
                            Location = component,
                            AffectedUsers = events.Select(e => e.UserId).Distinct().Count(),
                            ImpactPercentage = failureRate * 100,
                            Severity = failureRate > 0.3 ? "Critical" : failureRate > 0.2 ? "High" : "Medium",
                            SuggestedFixes = new List<string> 
                            { 
                                $"Debug error handling in {component}",
                                "Add input validation",
                                "Improve error messaging"
                            }
                        });
                    }

                    // Identify slow interactions
                    if (avgDuration > 2000)
                    {
                        painPoints.Add(new UXPainPoint
                        {
                            Title = $"Slow Interaction in {component}",
                            Description = $"{component} takes {avgDuration:F0}ms on average",
                            Location = component,
                            AffectedUsers = events.Select(e => e.UserId).Distinct().Count(),
                            ImpactPercentage = Math.Min(100, (avgDuration / 5000) * 100),
                            Severity = avgDuration > 5000 ? "High" : "Medium",
                            SuggestedFixes = new List<string> 
                            { 
                                "Optimize database queries",
                                "Add caching layer",
                                "Reduce component complexity"
                            }
                        });
                    }

                    // Identify abandonment patterns
                    var abandonedSessions = _userSessions.Values
                        .Where(session => session.Any(e => e.Component == component) && 
                                        !session.Any(e => e.EventType == "Conversion"))
                        .Count();

                    if (abandonedSessions > events.Count / 4)
                    {
                        painPoints.Add(new UXPainPoint
                        {
                            Title = $"High Abandonment at {component}",
                            Description = $"Users frequently abandon at {component}",
                            Location = component,
                            AffectedUsers = abandonedSessions,
                            ImpactPercentage = (abandonedSessions / (double)_userSessions.Count) * 100,
                            Severity = "High",
                            SuggestedFixes = new List<string>
                            {
                                "Simplify the interface",
                                "Add help text or tooltips",
                                "Consider progressive disclosure"
                            }
                        });
                    }
                }

                return painPoints.OrderByDescending(p => p.ImpactPercentage).ToList();
            }
        });
    }

    public async Task<UserJourney> BuildUserJourneyAsync(string userId)
    {
        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                if (!_userSessions.TryGetValue(userId, out var events))
                    return null;

                var sortedEvents = events.OrderBy(e => e.Timestamp).ToList();

                return new UserJourney
                {
                    UserId = userId,
                    Events = sortedEvents,
                    StartTime = sortedEvents.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow,
                    EndTime = sortedEvents.LastOrDefault()?.Timestamp ?? DateTime.UtcNow,
                    TimeSpentSeconds = (sortedEvents.LastOrDefault()?.Timestamp ?? DateTime.UtcNow -
                        (sortedEvents.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow)).TotalSeconds,
                    ConversionStatus = sortedEvents.Any(e => e.EventType == "Conversion") ? "Completed" : "Abandoned"
                };
            }
        });
    }

    public async Task<ABTestResult> AnalyzeUIABTestAsync(string testId)
    {
        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                // Simulate A/B test analysis
                var variantAEvents = _allEvents.Where(e => e.Properties.ContainsKey("variant") && 
                    e.Properties["variant"].ToString() == "A").ToList();
                var variantBEvents = _allEvents.Where(e => e.Properties.ContainsKey("variant") && 
                    e.Properties["variant"].ToString() == "B").ToList();

                var result = new ABTestResult
                {
                    TestId = testId,
                    VariantA = "Control",
                    VariantB = "Experimental",
                    SampleSizeA = variantAEvents.Count,
                    SampleSizeB = variantBEvents.Count,
                    ConversionRateA = variantAEvents.Count > 0 ? 
                        (double)variantAEvents.Count(e => e.Success) / variantAEvents.Count : 0,
                    ConversionRateB = variantBEvents.Count > 0 ? 
                        (double)variantBEvents.Count(e => e.Success) / variantBEvents.Count : 0,
                };

                result.Winner = result.ConversionRateB > result.ConversionRateA ? "B" : "A";
                result.ConfidenceLevel = Math.Min(95, Math.Abs(result.ConversionRateB - result.ConversionRateA) * 200);

                return result;
            }
        });
    }

    public async Task<List<string>> GenerateDesignRecommendationsAsync()
    {
        var painPoints = await IdentifyUXPainPointsAsync();
        var recommendations = new List<string>();

        foreach (var point in painPoints.Take(5))
        {
            recommendations.AddRange(point.SuggestedFixes);
            recommendations.Add($"Priority: {point.Severity} - {point.Title}");
        }

        recommendations.Add("Conduct user interviews to validate pain points");
        recommendations.Add("Create wireframes for proposed improvements");
        recommendations.Add("Run A/B tests on significant changes");

        return recommendations;
    }
}

public class HeatmapGenerator
{
    private readonly List<UserBehaviorEvent> _clickEvents = new();
    private readonly object _lockObj = new();

    public async Task RecordClickAsync(UserBehaviorEvent clickEvent)
    {
        await Task.Run(() =>
        {
            lock (_lockObj)
            {
                if (clickEvent.EventType == "Click")
                {
                    _clickEvents.Add(clickEvent);
                }
            }
        });
    }

    public async Task<Dictionary<string, int>> GenerateHeatmapAsync(string page)
    {
        return await Task.Run(() =>
        {
            lock (_lockObj)
            {
                return _clickEvents
                    .Where(e => e.Page == page)
                    .GroupBy(e => e.Component)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
        });
    }
}
