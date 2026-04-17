using System;
using System.Diagnostics;
using HELIOS.Platform.Core.Logging;
using Microsoft.EntityFrameworkCore;

namespace HELIOS.Platform.Core.Performance
{
    /// <summary>
    /// Database connection pooling and optimization service
    /// </summary>
    public interface IConnectionPoolService
    {
        DbContextOptions<T> CreateOptimizedOptions<T>(string connectionString) where T : DbContext;
        void LogPoolStats();
    }

    /// <summary>
    /// Optimized connection pooling implementation
    /// </summary>
    public class ConnectionPoolService : IConnectionPoolService
    {
        private readonly Logging.ILogger _logger;
        private const int MaxPoolSize = 25;
        private const int MinPoolSize = 5;

        public ConnectionPoolService(Logging.ILogger logger) => _logger = logger;

        public DbContextOptions<T> CreateOptimizedOptions<T>(string connectionString) where T : DbContext
        {
            var builder = new DbContextOptionsBuilder<T>();
            
            // For SQLite (if used)
            if (connectionString.Contains(".db") || connectionString.Contains("sqlite", StringComparison.OrdinalIgnoreCase))
            {
                // SQLite connection pooling
                var optimizedConnectionString = $"{connectionString};Pooling=true;Max Pool Size={MaxPoolSize};";
                builder.UseSqlite(optimizedConnectionString, options =>
                {
                    options.CommandTimeout(30);
                    options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
            }
            else if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
            {
                // SQL Server connection pooling
                var optimizedConnectionString = $"{connectionString};Max Pool Size={MaxPoolSize};Min Pool Size={MinPoolSize};";
                builder.UseSqlServer(optimizedConnectionString, options =>
                {
                    options.CommandTimeout(30);
                    options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
            }

            // Common optimizations for all providers
            builder.EnableSensitiveDataLogging(false);
            builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            return builder.Options;
        }

        public void LogPoolStats()
        {
            _logger?.Info($"Database Connection Pool: Max Size={MaxPoolSize}, Min Size={MinPoolSize}");
        }
    }
}
