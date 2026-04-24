using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Server
{
    /// <summary>
    /// Query plan analyzer interface for SQL query performance analysis, cost estimation, and optimization suggestions.
    /// </summary>
    public interface IQueryPlanAnalyzer
    {
        /// <summary>
        /// Analyzes a SQL query and returns performance metrics and optimization suggestions.
        /// </summary>
        Task<QueryAnalysisResult> AnalyzeAsync(string sqlQuery);

        /// <summary>
        /// Estimates the execution cost of a query based on heuristics.
        /// </summary>
        Task<double> EstimateCostAsync(string sqlQuery);

        /// <summary>
        /// Identifies missing indexes that could improve query performance.
        /// </summary>
        Task<List<IndexSuggestion>> IdentifyMissingIndexesAsync(string sqlQuery);

        /// <summary>
        /// Gets optimization suggestions for a query.
        /// </summary>
        Task<List<OptimizationSuggestion>> GetOptimizationSuggestionsAsync(string sqlQuery);

        /// <summary>
        /// Analyzes table statistics and estimates rows affected.
        /// </summary>
        Task<TableStatistics> GetTableStatisticsAsync(string tableName);

        /// <summary>
        /// Caches and stores query analysis results for benchmarking.
        /// </summary>
        Task<bool> CacheAnalysisResultAsync(string queryHash, QueryAnalysisResult result);

        /// <summary>
        /// Retrieves cached analysis result.
        /// </summary>
        Task<QueryAnalysisResult?> GetCachedAnalysisResultAsync(string queryHash);
    }

    /// <summary>
    /// Result of query analysis.
    /// </summary>
    public class QueryAnalysisResult
    {
        public string Query { get; set; }
        public string QueryHash { get; set; }
        public double EstimatedCost { get; set; }
        public long EstimatedRowsAffected { get; set; }
        public List<OptimizationSuggestion> Suggestions { get; set; } = new();
        public List<IndexSuggestion> MissingIndexes { get; set; } = new();
        public int JoinCount { get; set; }
        public int TableCount { get; set; }
        public List<string> Tables { get; set; } = new();
        public DateTime AnalyzedAt { get; set; }
    }

    /// <summary>
    /// Suggestion for optimizing a query.
    /// </summary>
    public class OptimizationSuggestion
    {
        public string Type { get; set; } // "Index", "Join", "Where", "Subquery", "Aggregate"
        public string Problem { get; set; }
        public string Suggestion { get; set; }
        public double PotentialImprovementPercent { get; set; }
        public int Severity { get; set; } // 1-5, where 5 is critical
    }

    /// <summary>
    /// Suggestion for creating an index.
    /// </summary>
    public class IndexSuggestion
    {
        public string TableName { get; set; }
        public List<string> Columns { get; set; } = new();
        public string SuggestedIndexName { get; set; }
        public double PerformanceGainPercent { get; set; }
        public int Priority { get; set; } // 1-10, where 10 is highest
    }

    /// <summary>
    /// Table statistics for cost estimation.
    /// </summary>
    public class TableStatistics
    {
        public string TableName { get; set; }
        public long RowCount { get; set; }
        public int ColumnCount { get; set; }
        public long SizeBytes { get; set; }
        public List<ColumnStatistics> Columns { get; set; } = new();
        public DateTime LastAnalyzedAt { get; set; }
    }

    /// <summary>
    /// Column statistics.
    /// </summary>
    public class ColumnStatistics
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public bool IsIndexed { get; set; }
        public bool IsNullable { get; set; }
        public int DistinctValueCount { get; set; }
        public double Selectivity { get; set; } // 0-1
    }
}
