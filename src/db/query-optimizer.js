/**
 * Database Query Optimizer for HELIOS v4.0
 * Implements strategic indexing, query caching, N+1 elimination, and connection pooling
 * Performance Target: <50ms query latency (from 100ms)
 */

class SimpleNodeCache {
  constructor(options = {}) {
    this.defaultTtlMs = (options.stdTTL || 300) * 1000;
    this.store = new Map();
  }

  get(key) {
    const entry = this.store.get(key);
    if (!entry) return undefined;
    if (entry.expiresAt <= Date.now()) {
      this.store.delete(key);
      return undefined;
    }
    return entry.value;
  }

  set(key, value, ttlSeconds) {
    const ttlMs = ttlSeconds ? ttlSeconds * 1000 : this.defaultTtlMs;
    this.store.set(key, { value, expiresAt: Date.now() + ttlMs });
    return true;
  }

  del(key) {
    return this.store.delete(key) ? 1 : 0;
  }

  keys() {
    return [...this.store.keys()];
  }
}

const NodeCache = SimpleNodeCache;

class DatabaseOptimizer {
  constructor(config = {}) {
    this.config = {
      poolSize: config.poolSize || 20,
      connectionTimeout: config.connectionTimeout || 5000,
      idleTimeout: config.idleTimeout || 30000,
      cacheTTL: config.cacheTTL || 300,
      ...config,
    };
    this.queryCache = new NodeCache({ stdTTL: this.config.cacheTTL, checkperiod: 60 });
    this.metrics = {
      queriesExecuted: 0,
      cacheHits: 0,
      cacheMisses: 0,
      totalLatency: 0,
      n1QueriesPrevented: 0,
    };
  }

  /**
   * Strategic Indexes Configuration
   * Indexes on frequently queried columns to reduce query latency
   */
  getIndexConfiguration() {
    return [
      {
        table: 'users',
        indexes: [
          { columns: ['id'], type: 'primary', name: 'idx_users_id' },
          { columns: ['email'], type: 'unique', name: 'idx_users_email' },
          { columns: ['created_at'], type: 'btree', name: 'idx_users_created_at' },
        ],
      },
      {
        table: 'profiles',
        indexes: [
          { columns: ['user_id'], type: 'btree', name: 'idx_profiles_user_id' },
          { columns: ['type'], type: 'btree', name: 'idx_profiles_type' },
          { columns: ['user_id', 'type'], type: 'composite', name: 'idx_profiles_user_type' },
        ],
      },
      {
        table: 'analytics_events',
        indexes: [
          { columns: ['user_id'], type: 'btree', name: 'idx_events_user_id' },
          { columns: ['created_at'], type: 'btree', name: 'idx_events_created_at' },
          { columns: ['event_type'], type: 'btree', name: 'idx_events_type' },
          { columns: ['user_id', 'created_at'], type: 'composite', name: 'idx_events_user_created' },
        ],
      },
      {
        table: 'sync_state',
        indexes: [
          { columns: ['device_id'], type: 'btree', name: 'idx_sync_device_id' },
          { columns: ['updated_at'], type: 'btree', name: 'idx_sync_updated_at' },
        ],
      },
      {
        table: 'plugins',
        indexes: [
          { columns: ['enabled'], type: 'btree', name: 'idx_plugins_enabled' },
          { columns: ['category'], type: 'btree', name: 'idx_plugins_category' },
        ],
      },
      {
        table: 'audit_logs',
        indexes: [
          { columns: ['user_id'], type: 'btree', name: 'idx_audit_user_id' },
          { columns: ['created_at'], type: 'btree', name: 'idx_audit_created_at' },
          { columns: ['action'], type: 'btree', name: 'idx_audit_action' },
        ],
      },
    ];
  }

  /**
   * Apply indexes to database
   * In production, run migrations; here we generate SQL
   */
  generateIndexSQL() {
    const indexes = this.getIndexConfiguration();
    const sql = [];

    indexes.forEach(({ table, indexes: tableIndexes }) => {
      tableIndexes.forEach(({ columns, name }) => {
        const columnList = columns.join(', ');
        sql.push(`CREATE INDEX IF NOT EXISTS ${name} ON ${table} (${columnList});`);
      });
    });

    return sql.join('\n');
  }

  /**
   * Cache Query Results
   * Stores results of expensive queries with TTL
   */
  async executeQuery(query, params = [], options = {}) {
    const startTime = Date.now();
    const cacheKey = this._generateCacheKey(query, params);
    const forceRefresh = options.forceRefresh || false;

    if (!forceRefresh) {
      const cachedResult = this.queryCache.get(cacheKey);
      if (cachedResult) {
        this.metrics.cacheHits++;
        return { data: cachedResult, cached: true, latency: Date.now() - startTime };
      }
    }

    this.metrics.cacheMisses++;
    
    // Simulate query execution
    const data = await this._executeActualQuery(query, params);
    const latency = Date.now() - startTime;

    // Cache result if not explicitly disabled
    if (!options.noCache) {
      this.queryCache.set(cacheKey, data, options.cacheTTL || this.config.cacheTTL);
    }

    this.metrics.queriesExecuted++;
    this.metrics.totalLatency += latency;

    return { data, cached: false, latency };
  }

  /**
   * N+1 Query Elimination
   * Batch related queries to prevent N+1 pattern
   */
  async batchQuery(table, ids, relationships = {}) {
    const startTime = Date.now();
    
    // Main query
    const placeholders = ids.map(() => '?').join(',');
    const mainQuery = `SELECT * FROM ${table} WHERE id IN (${placeholders})`;
    
    const result = await this.executeQuery(mainQuery, ids, { noCache: true });
    const items = result.data;

    // Batch load relationships
    const relationshipData = {};
    for (const [relationKey, relationConfig] of Object.entries(relationships)) {
      const relatedIds = items.map(item => item[relationConfig.foreignKey]);
      const uniqueIds = [...new Set(relatedIds)];
      
      const relQuery = `SELECT * FROM ${relationConfig.table} WHERE id IN (${uniqueIds.map(() => '?').join(',')})`;
      const relResult = await this.executeQuery(relQuery, uniqueIds);
      
      relationshipData[relationKey] = relResult.data;
      this.metrics.n1QueriesPrevented++;
    }

    const latency = Date.now() - startTime;
    return { items, relationships: relationshipData, latency };
  }

  /**
   * Connection Pool Tuning
   * Manages database connection pool efficiently
   */
  getConnectionPoolConfig() {
    return {
      min: Math.ceil(this.config.poolSize * 0.2),
      max: this.config.poolSize,
      acquireTimeoutMillis: this.config.connectionTimeout,
      idleTimeoutMillis: this.config.idleTimeout,
      connectionTimeoutMillis: this.config.connectionTimeout,
      statement_timeout: 30000,
      idle_in_transaction_session_timeout: 60000,
    };
  }

  /**
   * Index Performance Monitoring
   * Tracks index efficiency and query optimization metrics
   */
  getIndexMetrics() {
    const hitRate = this.metrics.cacheHits + this.metrics.cacheMisses > 0
      ? ((this.metrics.cacheHits / (this.metrics.cacheHits + this.metrics.cacheMisses)) * 100).toFixed(2)
      : 0;

    const avgLatency = this.metrics.queriesExecuted > 0
      ? (this.metrics.totalLatency / this.metrics.queriesExecuted).toFixed(2)
      : 0;

    return {
      queriesExecuted: this.metrics.queriesExecuted,
      cacheHitRate: `${hitRate}%`,
      cacheHits: this.metrics.cacheHits,
      cacheMisses: this.metrics.cacheMisses,
      avgLatencyMs: parseFloat(avgLatency),
      n1QueriesPrevented: this.metrics.n1QueriesPrevented,
      indexConfiguration: this.getIndexConfiguration(),
    };
  }

  /**
   * Invalidate cache for specific table
   */
  invalidateCache(table, id = null) {
    const keys = this.queryCache.keys();
    const pattern = id ? `${table}:${id}:` : `${table}:`;
    
    keys.forEach(key => {
      if (key.startsWith(pattern)) {
        this.queryCache.del(key);
      }
    });
  }

  _generateCacheKey(query, params) {
    const paramString = JSON.stringify(params);
    return `${query}:${paramString}`;
  }

  async _executeActualQuery(query, params) {
    // Simulate query execution with random latency (20-80ms)
    const delay = Math.random() * 60 + 20;
    await new Promise(resolve => setTimeout(resolve, delay));
    return { rows: [], count: 0 };
  }

  /**
   * Health check for database connection
   */
  async healthCheck() {
    try {
      const startTime = Date.now();
      await this.executeQuery('SELECT 1', [], { noCache: true });
      return {
        status: 'healthy',
        responseTimeMs: Date.now() - startTime,
        timestamp: new Date().toISOString(),
      };
    } catch (error) {
      return {
        status: 'unhealthy',
        error: error.message,
        timestamp: new Date().toISOString(),
      };
    }
  }
}

module.exports = DatabaseOptimizer;
