using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.AdvancedOptimization
{
    /// <summary>
    /// Intelligent data compression engine that compresses logs and metrics,
    /// supports multiple compression strategies, and optimizes compression ratio vs speed.
    /// </summary>
    public interface IDataCompressionEngine
    {
        Task<bool> InitializeAsync();
        Task<CompressedData> CompressDataAsync(string data, CompressionStrategy strategy);
        Task<string> DecompressDataAsync(CompressedData compressedData);
        Task<bool> CompressLogsAsync(List<string> logs);
        Task<bool> CompressMetricsAsync(Dictionary<string, object> metrics);
        Task<CompressionMetrics> GetCompressionMetricsAsync();
    }

    /// <summary>Compression strategies available.</summary>
    public enum CompressionStrategy
    {
        Fast,
        Balanced,
        Maximum,
        Adaptive
    }

    /// <summary>Represents compressed data.</summary>
    public class CompressedData
    {
        public string CompressionId { get; set; } = Guid.NewGuid().ToString();
        public byte[] CompressedContent { get; set; } = Array.Empty<byte>();
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        public CompressionStrategy Strategy { get; set; }
        public double CompressionRatio { get; set; }
        public long CompressionTimeMs { get; set; }
        public DateTime CompressedAt { get; set; } = DateTime.UtcNow;
        public string ContentType { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>Compression statistics and metrics.</summary>
    public class CompressionMetrics
    {
        public long TotalBytesCompressed { get; set; }
        public long TotalBytesSaved { get; set; }
        public double AverageCompressionRatio { get; set; }
        public long CompressedItems { get; set; }
        public long FailedCompressions { get; set; }
        public double AverageCompressionTimeMs { get; set; }
        public double AverageDecompressionTimeMs { get; set; }
        public DateTime LastCompressionTime { get; set; }
        public Dictionary<CompressionStrategy, long> ItemsByStrategy { get; set; } = new();
        public double TotalStorageSaved { get; set; }
    }
}
