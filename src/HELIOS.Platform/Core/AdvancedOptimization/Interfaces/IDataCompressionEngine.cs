namespace HELIOS.Platform.Core.AdvancedOptimization.Interfaces
{
    /// <summary>
    /// Interface for the Data Compression Engine service.
    /// Provides intelligent data compression with format selection.
    /// </summary>
    public interface IDataCompressionEngine : IService
    {
        /// <summary>
        /// Compresses data using optimal algorithm.
        /// </summary>
        /// <param name="data">Data to compress.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with compressed data.</returns>
        Task<CompressionResult> CompressAsync(byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Decompresses data.
        /// </summary>
        /// <param name="compressedData">Compressed data.</param>
        /// <param name="compressionFormat">Format used for compression.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with decompressed data.</returns>
        Task<DecompressionResult> DecompressAsync(byte[] compressedData, string compressionFormat, CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes compression ratio based on data characteristics.
        /// </summary>
        /// <param name="sampleData">Sample of data to analyze.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation with optimization results.</returns>
        Task<CompressionOptimizationResult> OptimizeCompressionAsync(byte[] sampleData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes data characteristics.
        /// </summary>
        /// <param name="data">Data to analyze.</param>
        /// <returns>Analysis of data characteristics.</returns>
        Task<DataCharacteristics> AnalyzeDataAsync(byte[] data);

        /// <summary>
        /// Gets compression statistics.
        /// </summary>
        /// <param name="limit">Maximum records to retrieve.</param>
        /// <returns>List of historical compression statistics.</returns>
        Task<List<CompressionStatistics>> GetStatisticsAsync(int limit = 100);
    }

    /// <summary>
    /// Represents compression result.
    /// </summary>
    public class CompressionResult
    {
        /// <summary>
        /// Result identifier.
        /// </summary>
        public string ResultId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Original data size in bytes.
        /// </summary>
        public long OriginalSize { get; set; }

        /// <summary>
        /// Compressed data size in bytes.
        /// </summary>
        public long CompressedSize { get; set; }

        /// <summary>
        /// Compression ratio (0-1).
        /// </summary>
        public double CompressionRatio { get; set; }

        /// <summary>
        /// Compression format used.
        /// </summary>
        public string CompressionFormat { get; set; } = string.Empty;

        /// <summary>
        /// Compressed data.
        /// </summary>
        public byte[] CompressedData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Compression time in milliseconds.
        /// </summary>
        public long CompressionTimeMs { get; set; }

        /// <summary>
        /// Whether compression was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Compression timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents decompression result.
    /// </summary>
    public class DecompressionResult
    {
        /// <summary>
        /// Result identifier.
        /// </summary>
        public string ResultId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Decompressed data.
        /// </summary>
        public byte[] DecompressedData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Decompression format used.
        /// </summary>
        public string CompressionFormat { get; set; } = string.Empty;

        /// <summary>
        /// Decompression time in milliseconds.
        /// </summary>
        public long DecompressionTimeMs { get; set; }

        /// <summary>
        /// Whether decompression was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if decompression failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Decompression timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents compression optimization result.
    /// </summary>
    public class CompressionOptimizationResult
    {
        /// <summary>
        /// Optimization timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Recommended compression format.
        /// </summary>
        public string RecommendedFormat { get; set; } = string.Empty;

        /// <summary>
        /// Expected compression ratio for recommended format.
        /// </summary>
        public double ExpectedCompressionRatio { get; set; }

        /// <summary>
        /// Compression options by format.
        /// </summary>
        public Dictionary<string, CompressionFormatOption> FormatOptions { get; set; } = new();

        /// <summary>
        /// Data characteristics analysis.
        /// </summary>
        public DataCharacteristics DataCharacteristics { get; set; } = new();

        /// <summary>
        /// Optimization notes.
        /// </summary>
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents compression format option.
    /// </summary>
    public class CompressionFormatOption
    {
        /// <summary>
        /// Format name.
        /// </summary>
        public string Format { get; set; } = string.Empty;

        /// <summary>
        /// Expected compression ratio.
        /// </summary>
        public double CompressionRatio { get; set; }

        /// <summary>
        /// Estimated compression speed (MB/s).
        /// </summary>
        public double CompressionSpeed { get; set; }

        /// <summary>
        /// Estimated decompression speed (MB/s).
        /// </summary>
        public double DecompressionSpeed { get; set; }

        /// <summary>
        /// Whether format is recommended.
        /// </summary>
        public bool IsRecommended { get; set; }
    }

    /// <summary>
    /// Represents data characteristics.
    /// </summary>
    public class DataCharacteristics
    {
        /// <summary>
        /// Total data size in bytes.
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// Data entropy (0-8 for byte data).
        /// </summary>
        public double Entropy { get; set; }

        /// <summary>
        /// Detected data type (Text, Binary, Structured, etc.).
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// Repetition ratio (0-1).
        /// </summary>
        public double RepetitionRatio { get; set; }

        /// <summary>
        /// Compressibility score (0-100).
        /// </summary>
        public double CompressibilityScore { get; set; }

        /// <summary>
        /// Byte frequency distribution.
        /// </summary>
        public Dictionary<byte, int> ByteFrequency { get; set; } = new();

        /// <summary>
        /// Analysis timestamp.
        /// </summary>
        public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents compression statistics.
    /// </summary>
    public class CompressionStatistics
    {
        /// <summary>
        /// Statistics timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Total data processed in bytes.
        /// </summary>
        public long TotalDataProcessed { get; set; }

        /// <summary>
        /// Average compression ratio achieved.
        /// </summary>
        public double AverageCompressionRatio { get; set; }

        /// <summary>
        /// Most used compression format.
        /// </summary>
        public string MostUsedFormat { get; set; } = string.Empty;

        /// <summary>
        /// Number of compressions performed.
        /// </summary>
        public int CompressionCount { get; set; }

        /// <summary>
        /// Number of decompressions performed.
        /// </summary>
        public int DecompressionCount { get; set; }

        /// <summary>
        /// Total time spent compressing in milliseconds.
        /// </summary>
        public long TotalCompressionTime { get; set; }

        /// <summary>
        /// Failure count.
        /// </summary>
        public int FailureCount { get; set; }
    }
}
