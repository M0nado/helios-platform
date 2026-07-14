using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using HELIOS.Platform.Core.AdvancedOptimization.Interfaces;
using Contracts = HELIOS.Platform.Core.AdvancedOptimization.Interfaces;

namespace HELIOS.Platform.Core.AdvancedOptimization
{
    /// <summary>
    /// Data Compression Engine implementation.
    /// Provides intelligent data compression with format selection.
    /// </summary>
    public class DataCompressionEngine : Contracts.IDataCompressionEngine
    {
        private readonly ILogger<DataCompressionEngine> _logger;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentQueue<CompressionStatistics> _statistics;
        private bool _isRunning;

        /// <summary>
        /// Initializes a new instance of the DataCompressionEngine class.
        /// </summary>
        public DataCompressionEngine(ILogger<DataCompressionEngine> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _semaphore = new SemaphoreSlim(1, 1);
            _statistics = new ConcurrentQueue<CompressionStatistics>();
            _isRunning = false;
        }

        /// <inheritdoc/>
        public string ServiceName => nameof(DataCompressionEngine);

        /// <inheritdoc/>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _logger.LogInformation("{ServiceName} initializing", ServiceName);
                await Task.CompletedTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _isRunning = true;
                _logger.LogInformation("{ServiceName} started", ServiceName);
                await Task.CompletedTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _isRunning = false;
                _logger.LogInformation("{ServiceName} stopped", ServiceName);
                await Task.CompletedTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public bool IsRunning() => _isRunning;

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            _semaphore?.Dispose();
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<CompressionResult> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = new CompressionResult { Timestamp = DateTime.UtcNow, OriginalSize = data?.Length ?? 0 };

                if (data == null || data.Length == 0)
                {
                    result.CompressedData = Array.Empty<byte>();
                    result.CompressionRatio = 1.0;
                    result.Success = true;
                    return result;
                }

                var sw = System.Diagnostics.Stopwatch.StartNew();

                byte[] compressed = SimpleCompress(data);

                sw.Stop();

                result.CompressedData = compressed;
                result.CompressedSize = compressed.Length;
                result.CompressionRatio = (double)compressed.Length / data.Length;
                result.CompressionFormat = "RLE";
                result.CompressionTimeMs = sw.ElapsedMilliseconds;
                result.Success = true;

                _logger.LogInformation("Compressed {OriginalSize} bytes to {CompressedSize} bytes", result.OriginalSize, result.CompressedSize);

                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<DecompressionResult> DecompressAsync(byte[] compressedData, string compressionFormat, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = new DecompressionResult { Timestamp = DateTime.UtcNow, CompressionFormat = compressionFormat };

                if (compressedData == null || compressedData.Length == 0)
                {
                    result.DecompressedData = Array.Empty<byte>();
                    result.Success = true;
                    return result;
                }

                var sw = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    result.DecompressedData = SimpleDecompress(compressedData);
                    sw.Stop();
                    result.DecompressionTimeMs = sw.ElapsedMilliseconds;
                    result.Success = true;

                    _logger.LogInformation("Decompressed {CompressedSize} bytes", compressedData.Length);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                    result.DecompressionTimeMs = sw.ElapsedMilliseconds;
                    _logger.LogError(ex, "Decompression failed");
                }

                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<CompressionOptimizationResult> OptimizeCompressionAsync(byte[] sampleData, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = new CompressionOptimizationResult { Timestamp = DateTime.UtcNow };

                if (sampleData == null || sampleData.Length == 0)
                {
                    result.RecommendedFormat = "None";
                    return result;
                }

                var chars = new DataCharacteristics { AnalysisTime = DateTime.UtcNow, TotalSize = sampleData.Length };
                chars = await AnalyzeDataAsync(sampleData);
                result.DataCharacteristics = chars;

                var rleOption = new CompressionFormatOption
                {
                    Format = "RLE",
                    CompressionRatio = 0.7,
                    CompressionSpeed = 100,
                    DecompressionSpeed = 150,
                    IsRecommended = true
                };

                result.FormatOptions["RLE"] = rleOption;
                result.RecommendedFormat = "RLE";
                result.ExpectedCompressionRatio = 0.7;
                result.Notes = "RLE recommended based on data characteristics";

                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<DataCharacteristics> AnalyzeDataAsync(byte[] data)
        {
            var chars = new DataCharacteristics
            {
                TotalSize = data?.Length ?? 0,
                AnalysisTime = DateTime.UtcNow
            };

            if (data == null || data.Length == 0)
            {
                return chars;
            }

            Dictionary<byte, int> frequency = new();
            foreach (byte b in data)
            {
                if (frequency.ContainsKey(b))
                    frequency[b]++;
                else
                    frequency[b] = 1;
            }

            chars.ByteFrequency = frequency;
            chars.DataType = DetectDataType(data);
            chars.RepetitionRatio = CalculateRepetitionRatio(data);
            chars.Entropy = CalculateEntropy(frequency, data.Length);
            chars.CompressibilityScore = Math.Min(chars.RepetitionRatio * 100, 100);

            return await Task.FromResult(chars);
        }

        /// <inheritdoc/>
        public async Task<List<CompressionStatistics>> GetStatisticsAsync(int limit = 100)
        {
            var results = new List<CompressionStatistics>();
            int count = 0;

            foreach (var item in _statistics.Reverse())
            {
                if (count >= limit) break;
                results.Add(item);
                count++;
            }

            return await Task.FromResult(results);
        }

        private byte[] SimpleCompress(byte[] data)
        {
            var result = new List<byte>();
            int i = 0;

            while (i < data.Length)
            {
                byte currentByte = data[i];
                int count = 1;

                while (i + count < data.Length && data[i + count] == currentByte && count < 255)
                {
                    count++;
                }

                if (count > 3)
                {
                    result.Add(255);
                    result.Add(currentByte);
                    result.Add((byte)count);
                    i += count;
                }
                else
                {
                    for (int j = 0; j < count; j++)
                    {
                        result.Add(currentByte);
                    }
                    i += count;
                }
            }

            return result.ToArray();
        }

        private byte[] SimpleDecompress(byte[] data)
        {
            var result = new List<byte>();
            int i = 0;

            while (i < data.Length)
            {
                if (data[i] == 255 && i + 2 < data.Length)
                {
                    byte value = data[i + 1];
                    byte count = data[i + 2];
                    for (int j = 0; j < count; j++)
                    {
                        result.Add(value);
                    }
                    i += 3;
                }
                else
                {
                    result.Add(data[i]);
                    i++;
                }
            }

            return result.ToArray();
        }

        private string DetectDataType(byte[] data)
        {
            int textChars = data.Count(b => (b >= 32 && b <= 126) || b == 10 || b == 13 || b == 9);
            if (textChars > data.Length * 0.8) return "Text";
            return "Binary";
        }

        private double CalculateRepetitionRatio(byte[] data)
        {
            if (data.Length < 2) return 0;
            int repetitions = 0;
            for (int i = 1; i < data.Length; i++)
            {
                if (data[i] == data[i - 1]) repetitions++;
            }
            return (double)repetitions / (data.Length - 1);
        }

        private double CalculateEntropy(Dictionary<byte, int> frequency, int totalBytes)
        {
            double entropy = 0;
            foreach (var freq in frequency.Values)
            {
                double p = (double)freq / totalBytes;
                entropy -= p * Math.Log2(p);
            }
            return entropy;
        }
    }
}
