namespace HELIOS.Platform.Core.AdvancedOptimization.Interfaces
{
    /// <summary>
    /// Interface for the Performance Predictor AI service.
    /// Provides performance forecasting and resource prediction.
    /// </summary>
    public interface IPerformancePredictorAI : IService
    {
        /// <summary>
        /// Predicts future system performance based on historical data.
        /// </summary>
        /// <param name="historicalPerformance">Historical performance data points.</param>
        /// <param name="forecastMinutes">Number of minutes ahead to forecast.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with performance prediction.</returns>
        Task<PerformancePrediction> PredictPerformanceAsync(List<PerformanceDataPoint> historicalPerformance, int forecastMinutes, CancellationToken cancellationToken = default);

        /// <summary>
        /// Forecasts load patterns for the system.
        /// </summary>
        /// <param name="historicalLoad">Historical load data.</param>
        /// <param name="forecastHours">Number of hours to forecast.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with load forecast.</returns>
        Task<LoadForecast> ForecastLoadAsync(List<LoadDataPoint> historicalLoad, int forecastHours, CancellationToken cancellationToken = default);

        /// <summary>
        /// Predicts resource requirements based on trends.
        /// </summary>
        /// <param name="historicalResources">Historical resource usage.</param>
        /// <param name="forecastMinutes">Number of minutes to forecast.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with resource prediction.</returns>
        Task<ResourcePrediction> PredictResourcesAsync(List<ResourceDataPoint> historicalResources, int forecastMinutes, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records performance data point for learning.
        /// </summary>
        /// <param name="dataPoint">Performance data point.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RecordPerformanceDataAsync(PerformanceDataPoint dataPoint);

        /// <summary>
        /// Gets prediction history.
        /// </summary>
        /// <param name="limit">Maximum records to retrieve.</param>
        /// <returns>List of historical predictions.</returns>
        Task<List<PerformancePrediction>> GetPredictionHistoryAsync(int limit = 100);
    }

    /// <summary>
    /// Represents a performance data point.
    /// </summary>
    public class PerformanceDataPoint
    {
        /// <summary>
        /// Data point timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// CPU usage percentage (0-100).
        /// </summary>
        public double CpuUsage { get; set; }

        /// <summary>
        /// Memory usage percentage (0-100).
        /// </summary>
        public double MemoryUsage { get; set; }

        /// <summary>
        /// Disk I/O in operations per second.
        /// </summary>
        public double DiskIO { get; set; }

        /// <summary>
        /// Network throughput in Mbps.
        /// </summary>
        public double NetworkThroughput { get; set; }

        /// <summary>
        /// Response time in milliseconds.
        /// </summary>
        public double ResponseTime { get; set; }

        /// <summary>
        /// Request throughput per second.
        /// </summary>
        public double RequestsPerSecond { get; set; }

        /// <summary>
        /// Error rate (0-1).
        /// </summary>
        public double ErrorRate { get; set; }
    }

    /// <summary>
    /// Represents performance prediction.
    /// </summary>
    public class PerformancePrediction
    {
        /// <summary>
        /// Prediction identifier.
        /// </summary>
        public string PredictionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Prediction timestamp.
        /// </summary>
        public DateTime PredictionTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Forecast period in minutes.
        /// </summary>
        public int ForecastMinutes { get; set; }

        /// <summary>
        /// Predicted CPU usage (0-100).
        /// </summary>
        public double PredictedCpuUsage { get; set; }

        /// <summary>
        /// Predicted memory usage (0-100).
        /// </summary>
        public double PredictedMemoryUsage { get; set; }

        /// <summary>
        /// Predicted response time in milliseconds.
        /// </summary>
        public double PredictedResponseTime { get; set; }

        /// <summary>
        /// Predicted requests per second.
        /// </summary>
        public double PredictedRequestsPerSecond { get; set; }

        /// <summary>
        /// Confidence score for prediction (0-1).
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Prediction intervals by metric.
        /// </summary>
        public Dictionary<string, PredictionInterval> PredictionIntervals { get; set; } = new();

        /// <summary>
        /// Risk indicators if performance degradation expected.
        /// </summary>
        public List<string> RiskIndicators { get; set; } = new();
    }

    /// <summary>
    /// Represents a prediction interval with confidence bounds.
    /// </summary>
    public class PredictionInterval
    {
        /// <summary>
        /// Lower bound of prediction.
        /// </summary>
        public double LowerBound { get; set; }

        /// <summary>
        /// Point estimate of prediction.
        /// </summary>
        public double PointEstimate { get; set; }

        /// <summary>
        /// Upper bound of prediction.
        /// </summary>
        public double UpperBound { get; set; }

        /// <summary>
        /// Confidence level percentage.
        /// </summary>
        public double ConfidenceLevel { get; set; }
    }

    /// <summary>
    /// Represents a load data point.
    /// </summary>
    public class LoadDataPoint
    {
        /// <summary>
        /// Data point timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Current load (0-100).
        /// </summary>
        public double Load { get; set; }

        /// <summary>
        /// Active connection count.
        /// </summary>
        public int ActiveConnections { get; set; }

        /// <summary>
        /// Queue depth.
        /// </summary>
        public int QueueDepth { get; set; }

        /// <summary>
        /// Hour of day for pattern matching.
        /// </summary>
        public int HourOfDay { get; set; }

        /// <summary>
        /// Day of week for pattern matching.
        /// </summary>
        public int DayOfWeek { get; set; }
    }

    /// <summary>
    /// Represents load forecast.
    /// </summary>
    public class LoadForecast
    {
        /// <summary>
        /// Forecast identifier.
        /// </summary>
        public string ForecastId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Forecast generation time.
        /// </summary>
        public DateTime ForecastTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Forecast period in hours.
        /// </summary>
        public int ForecastHours { get; set; }

        /// <summary>
        /// Hourly load forecast.
        /// </summary>
        public List<HourlyLoadForecast> HourlyForecasts { get; set; } = new();

        /// <summary>
        /// Peak load expected during forecast period.
        /// </summary>
        public double PeakLoadExpected { get; set; }

        /// <summary>
        /// Average load expected during forecast period.
        /// </summary>
        public double AverageLoadExpected { get; set; }

        /// <summary>
        /// Identified patterns.
        /// </summary>
        public List<string> IdentifiedPatterns { get; set; } = new();

        /// <summary>
        /// Forecast confidence (0-1).
        /// </summary>
        public double ConfidenceScore { get; set; }
    }

    /// <summary>
    /// Represents hourly load forecast.
    /// </summary>
    public class HourlyLoadForecast
    {
        /// <summary>
        /// Hour starting time.
        /// </summary>
        public DateTime HourStart { get; set; }

        /// <summary>
        /// Predicted average load.
        /// </summary>
        public double PredictedLoad { get; set; }

        /// <summary>
        /// Expected minimum load.
        /// </summary>
        public double MinLoad { get; set; }

        /// <summary>
        /// Expected maximum load.
        /// </summary>
        public double MaxLoad { get; set; }
    }

    /// <summary>
    /// Represents resource data point.
    /// </summary>
    public class ResourceDataPoint
    {
        /// <summary>
        /// Data point timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Resource usage by type.
        /// </summary>
        public Dictionary<string, double> ResourceUsage { get; set; } = new();

        /// <summary>
        /// System load at this point.
        /// </summary>
        public double SystemLoad { get; set; }
    }

    /// <summary>
    /// Represents resource prediction.
    /// </summary>
    public class ResourcePrediction
    {
        /// <summary>
        /// Prediction identifier.
        /// </summary>
        public string PredictionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Prediction timestamp.
        /// </summary>
        public DateTime PredictionTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Forecast period in minutes.
        /// </summary>
        public int ForecastMinutes { get; set; }

        /// <summary>
        /// Predicted resource requirements.
        /// </summary>
        public Dictionary<string, double> PredictedResources { get; set; } = new();

        /// <summary>
        /// Resource trend analysis.
        /// </summary>
        public Dictionary<string, string> ResourceTrends { get; set; } = new();

        /// <summary>
        /// Confidence scores by resource.
        /// </summary>
        public Dictionary<string, double> ConfidenceScores { get; set; } = new();

        /// <summary>
        /// Resource constraints expected.
        /// </summary>
        public List<string> ConstraintsExpected { get; set; } = new();

        /// <summary>
        /// Recommendations to meet predicted requirements.
        /// </summary>
        public List<string> Recommendations { get; set; } = new();
    }
}
