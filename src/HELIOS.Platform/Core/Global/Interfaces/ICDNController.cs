using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Global.Interfaces;

/// <summary>
/// CDN integration and management service.
/// </summary>
public interface ICDNController
{
    /// <summary>Register a CDN provider.</summary>
    Task<bool> RegisterCDNProviderAsync(string providerName, string apiKey, CDNConfiguration config);
    
    /// <summary>Purge content from CDN cache.</summary>
    Task<bool> PurgeContentAsync(string cdnProvider, List<string> contentPaths);
    
    /// <summary>Get CDN performance metrics.</summary>
    Task<CDNMetrics> GetMetricsAsync(string cdnProvider);
    
    /// <summary>Get content delivery statistics.</summary>
    Task<ContentDeliveryStats> GetDeliveryStatsAsync(string cdnProvider);
    
    /// <summary>Configure CDN origin settings.</summary>
    Task<bool> ConfigureOriginAsync(string cdnProvider, CDNOriginConfig originConfig);
    
    /// <summary>Get CDN coverage map.</summary>
    Task<CDNCoverageMap> GetCoveragMapAsync(string cdnProvider);
    
    /// <summary>Analyze CDN performance and generate recommendations.</summary>
    Task<List<CDNOptimizationRecommendation>> AnalyzePerformanceAsync(string cdnProvider);
    
    /// <summary>Get list of registered CDN providers.</summary>
    Task<List<CDNProviderStatus>> GetProviderStatusAsync();
}

/// <summary>
/// CDN provider configuration.
/// </summary>
public class CDNConfiguration
{
    public string ProviderName { get; set; }
    public string ApiKey { get; set; }
    public string ApiSecret { get; set; }
    public string DefaultOriginDomain { get; set; }
    public bool Enabled { get; set; } = true;
    public Dictionary<string, string> CustomHeaders { get; set; } = [];
    public List<string> AllowedOrigins { get; set; } = [];
}

/// <summary>
/// CDN metrics.
/// </summary>
public class CDNMetrics
{
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    public string CDNProvider { get; set; }
    public long TotalBytesDelivered { get; set; }
    public long TotalRequests { get; set; }
    public double CacheHitRatePercent { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public double BandwidthGbps { get; set; }
    public int EdgeLocationsActive { get; set; }
    public Dictionary<string, long> BytesByRegion { get; set; } = [];
    public Dictionary<string, double> HitRateByRegion { get; set; } = [];
}

/// <summary>
/// Content delivery statistics.
/// </summary>
public class ContentDeliveryStats
{
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    public string CDNProvider { get; set; }
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double SuccessRate { get; set; }
    public long TotalBytesDelivered { get; set; }
    public long CachedBytesDelivered { get; set; }
    public double CacheHitRate { get; set; }
    public TimeSpan MeasurementPeriod { get; set; }
    public Dictionary<string, long> RequestsByContentType { get; set; } = [];
    public List<string> TopDeliveredFiles { get; set; } = [];
}

/// <summary>
/// CDN origin server configuration.
/// </summary>
public class CDNOriginConfig
{
    public string OriginDomain { get; set; }
    public int OriginPort { get; set; } = 443;
    public string Protocol { get; set; } = "HTTPS";
    public int OriginConnectTimeoutMs { get; set; } = 5000;
    public int OriginReadTimeoutMs { get; set; } = 30000;
    public List<string> OriginHeaders { get; set; } = [];
    public bool UseOriginSslCert { get; set; } = true;
    public string HostHeader { get; set; }
}

/// <summary>
/// CDN coverage map.
/// </summary>
public class CDNCoverageMap
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string CDNProvider { get; set; }
    public List<EdgeLocation> EdgeLocations { get; set; } = [];
    public int TotalEdgeLocations { get; set; }
    public List<string> CoveredCountries { get; set; } = [];
    public List<string> CoveredRegions { get; set; } = [];
    public Dictionary<string, int> LocationsByRegion { get; set; } = [];
}

/// <summary>
/// Edge location in CDN network.
/// </summary>
public class EdgeLocation
{
    public string LocationId { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
    public string Region { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsActive { get; set; }
    public int ServerCount { get; set; }
    public double CapacityGbps { get; set; }
}

/// <summary>
/// CDN optimization recommendation.
/// </summary>
public class CDNOptimizationRecommendation
{
    public string RecommendationId { get; set; } = Guid.NewGuid().ToString();
    public string CDNProvider { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Category { get; set; } // Caching, Compression, Origins, OriginShield
    public double EstimatedImprovementPercent { get; set; }
    public int Priority { get; set; } // 1-10
    public string ImplementationSteps { get; set; }
}

/// <summary>
/// CDN provider status.
/// </summary>
public class CDNProviderStatus
{
    public string ProviderName { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsHealthy { get; set; }
    public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;
    public double AvailabilityPercent { get; set; }
    public int EdgeLocationsCount { get; set; }
    public double AverageLatencyMs { get; set; }
    public double CacheHitRatePercent { get; set; }
    public string Status { get; set; } // Online, Offline, Degraded, Maintenance
}
