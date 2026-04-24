using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HELIOS.Platform.Core.API.Interfaces;
using HELIOS.Platform.Core.Performance;

namespace HELIOS.Platform.Core.API.Services;

/// <summary>
/// High-performance GraphQL server implementation with caching and monitoring.
/// </summary>
public class GraphQLServer : IGraphQLServer
{
    private readonly ILogger<GraphQLServer> _logger;
    private readonly IL1CacheService? _cache;
    private readonly Dictionary<string, Type> _types = new();
    private readonly Dictionary<string, Func<Task<object>>> _queryFields = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private long _queryCount = 0;
    private long _cacheHits = 0;
    private long _cacheMisses = 0;

    public GraphQLServer(ILogger<GraphQLServer> logger, IL1CacheService? cache = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache;
        _logger.LogInformation("GraphQL Server initialized");
    }

    /// <summary>
    /// Executes a GraphQL query with performance optimization and caching.
    /// </summary>
    public async Task<string> ExecuteQueryAsync(string query, Dictionary<string, object>? variables = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be empty", nameof(query));

        var startTime = DateTime.UtcNow;
        var cacheKey = $"graphql:{query}";

        try
        {
            Interlocked.Increment(ref _queryCount);

            // Try cache first
            if (_cache != null && _cache.TryGet(cacheKey, out string cachedResult))
            {
                Interlocked.Increment(ref _cacheHits);
                _logger.LogDebug("GraphQL cache hit for query (latency: <1ms)");
                return cachedResult;
            }

            Interlocked.Increment(ref _cacheMisses);

            await _semaphore.WaitAsync();
            try
            {
                var result = await ExecuteQueryInternalAsync(query, variables);
                var elapsed = DateTime.UtcNow - startTime;

                if (elapsed.TotalMilliseconds > 100)
                    _logger.LogWarning("Slow GraphQL query detected ({Latency}ms): {Query}",
                        elapsed.TotalMilliseconds, query);

                // Cache successful queries
                if (_cache != null && result != null)
                    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

                _logger.LogDebug("GraphQL query executed ({Latency}ms)", elapsed.TotalMilliseconds);
                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GraphQL query: {Query}", query);
            throw;
        }
    }

    private async Task<string> ExecuteQueryInternalAsync(string query, Dictionary<string, object>? variables)
    {
        // Parse and execute query - simplified implementation
        // In production, this would use a full GraphQL parser
        var result = new
        {
            data = variables ?? new Dictionary<string, object>(),
            errors = (string[])null
        };

        return System.Text.Json.JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Registers a CLR type for GraphQL schema.
    /// </summary>
    public async Task<bool> RegisterTypeAsync(string typeName, Type clrType)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("Type name cannot be empty", nameof(typeName));
        if (clrType == null)
            throw new ArgumentNullException(nameof(clrType));

        await _semaphore.WaitAsync();
        try
        {
            _types[typeName] = clrType;
            _logger.LogInformation("GraphQL type registered: {Type} ({CLRType})", typeName, clrType.Name);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Registers a query field resolver.
    /// </summary>
    public async Task<bool> RegisterQueryFieldAsync(string fieldName, Func<Task<object>> resolver)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentException("Field name cannot be empty", nameof(fieldName));
        if (resolver == null)
            throw new ArgumentNullException(nameof(resolver));

        await _semaphore.WaitAsync();
        try
        {
            _queryFields[fieldName] = resolver;
            _logger.LogInformation("GraphQL query field registered: {Field}", fieldName);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets the current GraphQL schema.
    /// </summary>
    public async Task<GraphQLSchema> GetSchemaAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var schema = new GraphQLSchema
            {
                Types = _types.Select(kvp => new GraphQLType
                {
                    Name = kvp.Key,
                    Fields = GetTypeFields(kvp.Value)
                }).ToList()
            };

            _logger.LogDebug("GraphQL schema retrieved with {TypeCount} types", schema.Types.Count);
            return schema;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private List<GraphQLField> GetTypeFields(Type type)
    {
        return type.GetProperties()
            .Select(p => new GraphQLField
            {
                Name = p.Name,
                Type = p.PropertyType.Name
            })
            .ToList();
    }

    /// <summary>
    /// Gets performance metrics.
    /// </summary>
    public GraphQLMetrics GetMetrics()
    {
        return new GraphQLMetrics
        {
            TotalQueries = Interlocked.Read(ref _queryCount),
            CacheHits = Interlocked.Read(ref _cacheHits),
            CacheMisses = Interlocked.Read(ref _cacheMisses),
            RegisteredTypes = _types.Count,
            RegisteredFields = _queryFields.Count
        };
    }
}

/// <summary>
/// GraphQL performance metrics.
/// </summary>
public class GraphQLMetrics
{
    public long TotalQueries { get; set; }
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    public double CacheHitRate => (CacheHits + CacheMisses) > 0 ? (double)CacheHits / (CacheHits + CacheMisses) : 0;
    public int RegisteredTypes { get; set; }
    public int RegisteredFields { get; set; }
}
