using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Server
{
    /// <summary>
    /// Query plan analyzer implementation with heuristic-based cost estimation and optimization suggestions.
    /// </summary>
    public class QueryPlanAnalyzer : IQueryPlanAnalyzer
    {
        private readonly Dictionary<string, QueryAnalysisResult> _cache = new();
        private readonly Dictionary<string, TableStatistics> _tableStats = new();
        private readonly object _lock = new();

        public QueryPlanAnalyzer()
        {
            InitializeDefaultTableStats();
        }

        public Task<QueryAnalysisResult> AnalyzeAsync(string sqlQuery)
        {
            if (string.IsNullOrWhiteSpace(sqlQuery))
                throw new ArgumentException("SQL query cannot be empty", nameof(sqlQuery));

            var hash = ComputeQueryHash(sqlQuery);
            lock (_lock)
            {
                if (_cache.TryGetValue(hash, out var cached))
                    return Task.FromResult(cached);
            }

            var result = new QueryAnalysisResult
            {
                Query = sqlQuery,
                QueryHash = hash,
                AnalyzedAt = DateTime.UtcNow
            };

            // Parse query
            ExtractTablesFromQuery(sqlQuery, result);
            result.JoinCount = CountJoins(sqlQuery);
            result.EstimatedRowsAffected = EstimateRowsAffected(sqlQuery, result);
            result.EstimatedCost = EstimateCostInternal(result);

            // Generate suggestions
            result.Suggestions = GenerateOptimizationSuggestions(sqlQuery, result);
            result.MissingIndexes = IdentifyMissingIndexesInternal(sqlQuery, result);

            lock (_lock)
            {
                _cache[hash] = result;
            }

            return Task.FromResult(result);
        }

        public Task<double> EstimateCostAsync(string sqlQuery)
        {
            if (string.IsNullOrWhiteSpace(sqlQuery))
                throw new ArgumentException("SQL query cannot be empty", nameof(sqlQuery));

            var result = new QueryAnalysisResult
            {
                Query = sqlQuery,
                QueryHash = ComputeQueryHash(sqlQuery)
            };

            ExtractTablesFromQuery(sqlQuery, result);
            result.JoinCount = CountJoins(sqlQuery);
            result.EstimatedRowsAffected = EstimateRowsAffected(sqlQuery, result);
            result.EstimatedCost = EstimateCostInternal(result);

            return Task.FromResult(result.EstimatedCost);
        }

        public Task<List<IndexSuggestion>> IdentifyMissingIndexesAsync(string sqlQuery)
        {
            if (string.IsNullOrWhiteSpace(sqlQuery))
                throw new ArgumentException("SQL query cannot be empty", nameof(sqlQuery));

            var result = new QueryAnalysisResult
            {
                Query = sqlQuery,
                QueryHash = ComputeQueryHash(sqlQuery)
            };

            ExtractTablesFromQuery(sqlQuery, result);
            var suggestions = IdentifyMissingIndexesInternal(sqlQuery, result);

            return Task.FromResult(suggestions);
        }

        public Task<List<OptimizationSuggestion>> GetOptimizationSuggestionsAsync(string sqlQuery)
        {
            if (string.IsNullOrWhiteSpace(sqlQuery))
                throw new ArgumentException("SQL query cannot be empty", nameof(sqlQuery));

            var result = new QueryAnalysisResult
            {
                Query = sqlQuery,
                QueryHash = ComputeQueryHash(sqlQuery)
            };

            ExtractTablesFromQuery(sqlQuery, result);
            result.JoinCount = CountJoins(sqlQuery);
            var suggestions = GenerateOptimizationSuggestions(sqlQuery, result);

            return Task.FromResult(suggestions);
        }

        public Task<TableStatistics> GetTableStatisticsAsync(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be empty", nameof(tableName));

            lock (_lock)
            {
                if (_tableStats.TryGetValue(tableName.ToLower(), out var stats))
                    return Task.FromResult(stats);

                // Return default stats for unknown tables
                return Task.FromResult(new TableStatistics
                {
                    TableName = tableName,
                    RowCount = 100000,
                    ColumnCount = 10,
                    SizeBytes = 1000000,
                    LastAnalyzedAt = DateTime.UtcNow
                });
            }
        }

        public Task<bool> CacheAnalysisResultAsync(string queryHash, QueryAnalysisResult result)
        {
            if (string.IsNullOrWhiteSpace(queryHash))
                throw new ArgumentException("Query hash cannot be empty", nameof(queryHash));
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            lock (_lock)
            {
                _cache[queryHash] = result;
                return Task.FromResult(true);
            }
        }

        public Task<QueryAnalysisResult?> GetCachedAnalysisResultAsync(string queryHash)
        {
            if (string.IsNullOrWhiteSpace(queryHash))
                throw new ArgumentException("Query hash cannot be empty", nameof(queryHash));

            lock (_lock)
            {
                _cache.TryGetValue(queryHash, out var result);
                return Task.FromResult(result);
            }
        }

        private string ComputeQueryHash(string query)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(query.ToLower().Trim()));
                return Convert.ToBase64String(hash).Substring(0, 16);
            }
        }

        private void ExtractTablesFromQuery(string query, QueryAnalysisResult result)
        {
            var upperQuery = query.ToUpper();
            var tablePattern = @"\bFROM\s+(\w+)|\bJOIN\s+(\w+)";
            var matches = Regex.Matches(upperQuery, tablePattern);

            foreach (Match match in matches)
            {
                var table = match.Groups[1].Value.Length > 0 ? match.Groups[1].Value : match.Groups[2].Value;
                if (!string.IsNullOrEmpty(table))
                    result.Tables.Add(table);
            }

            result.TableCount = result.Tables.Count;
        }

        private int CountJoins(string query)
        {
            return Regex.Matches(query, @"\bJOIN\b", RegexOptions.IgnoreCase).Count;
        }

        private long EstimateRowsAffected(string query, QueryAnalysisResult result)
        {
            long baseRows = result.Tables.Count > 0
                ? _tableStats.Values.FirstOrDefault()?.RowCount ?? 100000
                : 1;

            // Cost multiplier based on joins
            baseRows = (long)(baseRows * Math.Pow(2, result.JoinCount));

            // Check for aggregation
            if (Regex.IsMatch(query, @"\b(GROUP\s+BY|COUNT|SUM|AVG|MAX|MIN)\b", RegexOptions.IgnoreCase))
                baseRows = Math.Max(1, baseRows / 10);

            // Check for LIMIT clause
            var limitMatch = Regex.Match(query, @"\bLIMIT\s+(\d+)", RegexOptions.IgnoreCase);
            if (limitMatch.Success && int.TryParse(limitMatch.Groups[1].Value, out var limit))
                baseRows = Math.Min(baseRows, limit);

            return baseRows;
        }

        private double EstimateCostInternal(QueryAnalysisResult result)
        {
            double cost = 1.0;

            // Base cost from row estimate
            cost += result.EstimatedRowsAffected / 1000.0;

            // Join penalty
            cost += result.JoinCount * 10.0;

            // Table count penalty
            cost += result.TableCount * 5.0;

            // Normalize
            return Math.Min(100.0, cost);
        }

        private List<OptimizationSuggestion> GenerateOptimizationSuggestions(string query, QueryAnalysisResult result)
        {
            var suggestions = new List<OptimizationSuggestion>();

            // Check for missing WHERE clause
            if (!Regex.IsMatch(query, @"\bWHERE\b", RegexOptions.IgnoreCase) && result.JoinCount > 0)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = "Where",
                    Problem = "Query has joins but no WHERE clause",
                    Suggestion = "Add WHERE conditions to reduce rows processed early",
                    PotentialImprovementPercent = 30,
                    Severity = 4
                });
            }

            // Check for SELECT *
            if (Regex.IsMatch(query, @"\bSELECT\s+\*\b", RegexOptions.IgnoreCase))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = "SelectColumn",
                    Problem = "Query uses SELECT *",
                    Suggestion = "Specify only needed columns",
                    PotentialImprovementPercent = 20,
                    Severity = 2
                });
            }

            // Check for complex joins
            if (result.JoinCount > 3)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = "Join",
                    Problem = $"Query contains {result.JoinCount} joins",
                    Suggestion = "Consider using materialized views or denormalization for frequent patterns",
                    PotentialImprovementPercent = 25,
                    Severity = 3
                });
            }

            // Check for functions in WHERE clause
            if (Regex.IsMatch(query, @"\bWHERE\b.*\(.*\)", RegexOptions.IgnoreCase))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = "Where",
                    Problem = "WHERE clause contains function calls",
                    Suggestion = "Avoid functions on indexed columns in WHERE clause",
                    PotentialImprovementPercent = 40,
                    Severity = 5
                });
            }

            // Check for OR in WHERE
            if (Regex.IsMatch(query, @"\bWHERE\b.*\bOR\b", RegexOptions.IgnoreCase))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = "Where",
                    Problem = "WHERE clause uses OR",
                    Suggestion = "Consider rewriting with IN or UNION for better index usage",
                    PotentialImprovementPercent = 35,
                    Severity = 3
                });
            }

            return suggestions;
        }

        private List<IndexSuggestion> IdentifyMissingIndexesInternal(string query, QueryAnalysisResult result)
        {
            var suggestions = new List<IndexSuggestion>();

            // Extract WHERE conditions
            var whereMatch = Regex.Match(query, @"\bWHERE\b\s*(.+?)(?:\bGROUP\b|\bORDER\b|\bLIMIT\b|$)", RegexOptions.IgnoreCase);
            if (whereMatch.Success)
            {
                var whereClause = whereMatch.Groups[1].Value;
                var columnPattern = @"(\w+)\s*[=<>]";
                var matches = Regex.Matches(whereClause, columnPattern);

                foreach (Match match in matches)
                {
                    var column = match.Groups[1].Value;
                    if (!string.IsNullOrEmpty(column) && result.Tables.Count > 0)
                    {
                        suggestions.Add(new IndexSuggestion
                        {
                            TableName = result.Tables[0],
                            Columns = new List<string> { column },
                            SuggestedIndexName = $"IDX_{result.Tables[0]}_{column}",
                            PerformanceGainPercent = 50,
                            Priority = 8
                        });
                    }
                }
            }

            // Extract ORDER BY columns
            var orderMatch = Regex.Match(query, @"\bORDER\s+BY\s+(.+?)(?:\bLIMIT\b|$)", RegexOptions.IgnoreCase);
            if (orderMatch.Success)
            {
                var orderClause = orderMatch.Groups[1].Value;
                var columns = orderClause.Split(',').Select(c => c.Trim().Split()[0]).ToList();

                if (result.Tables.Count > 0)
                {
                    suggestions.Add(new IndexSuggestion
                    {
                        TableName = result.Tables[0],
                        Columns = columns,
                        SuggestedIndexName = $"IDX_{result.Tables[0]}_ORDER",
                        PerformanceGainPercent = 60,
                        Priority = 7
                    });
                }
            }

            return suggestions;
        }

        private void InitializeDefaultTableStats()
        {
            lock (_lock)
            {
                _tableStats["users"] = new TableStatistics
                {
                    TableName = "users",
                    RowCount = 50000,
                    ColumnCount = 8,
                    SizeBytes = 5000000,
                    LastAnalyzedAt = DateTime.UtcNow
                };

                _tableStats["orders"] = new TableStatistics
                {
                    TableName = "orders",
                    RowCount = 500000,
                    ColumnCount = 10,
                    SizeBytes = 50000000,
                    LastAnalyzedAt = DateTime.UtcNow
                };

                _tableStats["products"] = new TableStatistics
                {
                    TableName = "products",
                    RowCount = 10000,
                    ColumnCount = 15,
                    SizeBytes = 1000000,
                    LastAnalyzedAt = DateTime.UtcNow
                };
            }
        }
    }
}
