/**
 * Performance Monitoring for HELIOS v4.0
 * Collects Web Vitals, API metrics, database performance, and optimization recommendations
 * Integrates with monitoring systems for real-time alerts
 */

class PerformanceMonitor {
  constructor(config = {}) {
    this.config = {
      enableMetrics: config.enableMetrics !== false,
      alertThresholds: config.alertThresholds || this.getDefaultThresholds(),
      metricsRetention: config.metricsRetention || 86400000, // 24 hours
      reportingInterval: config.reportingInterval || 60000, // 60 seconds
      ...config,
    };

    this.metrics = {
      apiMetrics: [],
      dbMetrics: [],
      cacheMetrics: [],
      bundleMetrics: [],
      webVitals: [],
      errors: [],
      recommendations: [],
    };
    Object.defineProperties(this.metrics, {
      requests: { get: () => this.metrics.apiMetrics.length },
      responses: { get: () => this.metrics.apiMetrics.length },
    });

    this.alerts = [];
  }

  /**
   * Get default performance thresholds
   */
  getDefaultThresholds() {
    return {
      apiLatency: {
        p50: 100,
        p95: 200,
        p99: 300,
      },
      dbQuery: {
        p50: 20,
        p95: 40,
        p99: 50,
      },
      cacheHitRate: 80,
      bundleSize: 512000, // 500KB gzipped
      webVitals: {
        CLS: 0.1, // Cumulative Layout Shift
        LCP: 2500, // Largest Contentful Paint (ms)
        FID: 100, // First Input Delay (ms)
        TTFB: 600, // Time to First Byte (ms)
      },
    };
  }

  /**
   * Collect API Performance Metrics
   * Track latency percentiles, request/response sizes
   */
  recordAPIMetric(endpoint, latency, statusCode, responseSize, options = {}) {
    const metric = {
      timestamp: Date.now(),
      endpoint,
      latency,
      statusCode,
      responseSize,
      method: options.method || 'GET',
      cached: options.cached || false,
      userId: options.userId,
    };

    this.metrics.apiMetrics.push(metric);
    this._checkThreshold('api_latency', latency, endpoint);

    return metric;
  }

  /**
   * Collect Database Query Metrics
   * Track query latency, rows affected, index usage
   */
  recordDBMetric(query, latency, rowsAffected, options = {}) {
    const metric = {
      timestamp: Date.now(),
      query: this._sanitizeQuery(query),
      latency,
      rowsAffected,
      indexUsed: options.indexUsed || false,
      cached: options.cached || false,
      connectionPoolSize: options.connectionPoolSize,
    };

    this.metrics.dbMetrics.push(metric);
    this._checkThreshold('db_query', latency);

    return metric;
  }

  /**
   * Collect Cache Performance Metrics
   * Track hit rate, evictions, revalidations
   */
  recordCacheMetric(hitRate, size, evictions, options = {}) {
    const metric = {
      timestamp: Date.now(),
      hitRate,
      cacheSize: size,
      evictions,
      strategy: options.strategy || 'lru',
      namespace: options.namespace,
    };

    this.metrics.cacheMetrics.push(metric);
    this._checkThreshold('cache_hit_rate', hitRate);

    return metric;
  }

  /**
   * Record Web Vitals
   * CLS: Cumulative Layout Shift
   * LCP: Largest Contentful Paint
   * FID: First Input Delay
   */
  recordWebVital(name, value, options = {}) {
    const metric = {
      timestamp: Date.now(),
      name,
      value,
      rating: this._rateWebVital(name, value),
      sessionId: options.sessionId,
      url: options.url,
    };

    this.metrics.webVitals.push(metric);
    this._checkThreshold('web_vital', value, name);

    return metric;
  }

  /**
   * Record Bundle Metrics
   * Track bundle size, chunk sizes, compression ratios
   */
  recordBundleMetric(bundleName, size, gzipped, chunks = []) {
    const metric = {
      timestamp: Date.now(),
      bundleName,
      size,
      gzipped,
      compressionRatio: ((1 - gzipped / size) * 100).toFixed(2),
      chunks: chunks.map(c => ({ name: c.name, size: c.size })),
    };

    this.metrics.bundleMetrics.push(metric);
    this._checkThreshold('bundle_size', gzipped);

    return metric;
  }

  /**
   * Record Error/Exception
   */
  recordError(error, context = {}) {
    const errorMetric = {
      timestamp: Date.now(),
      message: error.message,
      stack: error.stack,
      context,
      severity: context.severity || 'error',
    };

    this.metrics.errors.push(errorMetric);
  }

  /**
   * Compatibility helper for generic metric tests and simple automations.
   */
  recordMetric(type, value, context = {}) {
    if (type === 'request') {
      return this.recordAPIMetric(context.endpoint || '/generic', value, context.statusCode || 200, context.responseSize || 0, context);
    }

    if (type === 'response') {
      return this.recordAPIMetric(context.endpoint || '/generic', context.latency || 0, context.statusCode || 200, value, context);
    }

    if (type === 'latency') {
      return this.recordAPIMetric(context.endpoint || '/generic', value, context.statusCode || 200, context.responseSize || 0, context);
    }

    const metric = { timestamp: Date.now(), type, value, context };
    this.metrics.bundleMetrics.push(metric);
    return metric;
  }

  /**
   * Generate Performance Report
   */
  generateReport(timeWindow = 3600000) {
    const now = Date.now();
    const startTime = now - timeWindow;

    const filterByTime = (metrics) => 
      metrics.filter(m => m.timestamp >= startTime);

    const apiMetrics = filterByTime(this.metrics.apiMetrics);
    const dbMetrics = filterByTime(this.metrics.dbMetrics);
    const cacheMetrics = filterByTime(this.metrics.cacheMetrics);
    const webVitals = filterByTime(this.metrics.webVitals);

    const summary = {
      totalRequests: apiMetrics.length,
      totalQueriesExecuted: dbMetrics.length,
      averageAPILatency: this._calculatePercentile(apiMetrics, 'latency', 50),
      p95APILatency: this._calculatePercentile(apiMetrics, 'latency', 95),
      p99APILatency: this._calculatePercentile(apiMetrics, 'latency', 99),
      averageDBLatency: this._calculatePercentile(dbMetrics, 'latency', 50),
      p99DBLatency: this._calculatePercentile(dbMetrics, 'latency', 99),
      averageCacheHitRate: cacheMetrics.length > 0
        ? (cacheMetrics.reduce((sum, m) => sum + m.hitRate, 0) / cacheMetrics.length).toFixed(2)
        : 0,
      webVitalsStatus: this._assessWebVitals(webVitals),
    };

    const report = {
      generatedAt: new Date().toISOString(),
      timeWindow: `${(timeWindow / 1000 / 60).toFixed(0)} minutes`,
      summary,
      performance: {
        api: this._analyzeAPIMetrics(apiMetrics),
        database: this._analyzeDBMetrics(dbMetrics),
        cache: this._analyzeCacheMetrics(cacheMetrics),
        webVitals: this._analyzeWebVitals(webVitals),
      },
      alerts: this.alerts.slice(-50), // Last 50 alerts
      recommendations: [],
    };

    report.recommendations = this._generateRecommendations(report);
    return report;
  }

  /**
   * Performance Regression Detection
   */
  detectRegressions(baselineMetrics, currentMetrics) {
    const regressions = [];

    const checkRegression = (baseline, current, metric, threshold = 0.1) => {
      const increase = (current - baseline) / baseline;
      if (increase > threshold) {
        regressions.push({
          metric,
          baseline,
          current,
          increase: (increase * 100).toFixed(2),
          severity: increase > 0.3 ? 'high' : 'medium',
        });
      }
    };

    checkRegression(baselineMetrics.avgLatency, currentMetrics.avgLatency, 'API Latency');
    checkRegression(baselineMetrics.dbLatency, currentMetrics.dbLatency, 'Database Latency');
    checkRegression(baselineMetrics.bundleSize, currentMetrics.bundleSize, 'Bundle Size');

    return {
      detected: regressions.length > 0,
      regressions,
      timestamp: Date.now(),
    };
  }

  /**
   * Public Reporting API
   * Expose metrics to clients for dashboard/visualization
   */
  getPublicMetrics() {
    const now = Date.now();
    const oneHourAgo = now - 3600000;

    const recentAPI = this.metrics.apiMetrics.filter(m => m.timestamp >= oneHourAgo);
    const recentDB = this.metrics.dbMetrics.filter(m => m.timestamp >= oneHourAgo);
    const recentCache = this.metrics.cacheMetrics.filter(m => m.timestamp >= oneHourAgo);

    return {
      timestamp: new Date().toISOString(),
      metrics: {
        api: {
          avgLatency: this._calculatePercentile(recentAPI, 'latency', 50),
          p95Latency: this._calculatePercentile(recentAPI, 'latency', 95),
          p99Latency: this._calculatePercentile(recentAPI, 'latency', 99),
          totalRequests: recentAPI.length,
          cacheHitRate: (recentAPI.filter(r => r.cached).length / recentAPI.length * 100).toFixed(2),
          errorRate: (recentAPI.filter(r => r.statusCode >= 400).length / recentAPI.length * 100).toFixed(2),
        },
        database: {
          avgQueryTime: this._calculatePercentile(recentDB, 'latency', 50),
          p99QueryTime: this._calculatePercentile(recentDB, 'latency', 99),
          totalQueries: recentDB.length,
          indexUsageRate: (recentDB.filter(q => q.indexUsed).length / recentDB.length * 100).toFixed(2),
        },
        cache: {
          avgHitRate: recentCache.length > 0
            ? (recentCache.reduce((sum, m) => sum + m.hitRate, 0) / recentCache.length).toFixed(2)
            : 0,
          totalEvictions: recentCache.reduce((sum, m) => sum + m.evictions, 0),
        },
      },
      status: this._calculateHealthStatus(recentAPI, recentDB, recentCache),
    };
  }

  /**
   * Performance Recommendations Engine
   */
  _generateRecommendations(report) {
    const recommendations = [];

    if (report.summary.p99APILatency > this.config.alertThresholds.apiLatency.p99) {
      recommendations.push({
        priority: 'high',
        category: 'API Performance',
        recommendation: 'API latency exceeds target. Consider caching responses or database optimization.',
        action: 'Review API response optimization and caching strategy.',
      });
    }

    if (report.summary.p99DBLatency > this.config.alertThresholds.dbQuery.p99) {
      recommendations.push({
        priority: 'high',
        category: 'Database',
        recommendation: 'Database queries are slow. Add indexes or optimize queries.',
        action: 'Run EXPLAIN PLAN analysis on slow queries.',
      });
    }

    if (report.summary.averageCacheHitRate < this.config.alertThresholds.cacheHitRate) {
      recommendations.push({
        priority: 'medium',
        category: 'Caching',
        recommendation: 'Cache hit rate is low. Increase TTL or warm cache more frequently.',
        action: 'Review cache invalidation strategy and TTL configuration.',
      });
    }

    return recommendations;
  }

  /**
   * Calculate percentile from array of metrics
   */
  _calculatePercentile(metrics, field, percentile) {
    if (metrics.length === 0) return 0;
    const sorted = metrics.map(m => m[field]).sort((a, b) => a - b);
    const index = Math.ceil((percentile / 100) * sorted.length) - 1;
    return sorted[Math.max(0, index)].toFixed(2);
  }

  /**
   * Rate Web Vital
   */
  _rateWebVital(name, value) {
    const thresholds = this.config.alertThresholds.webVitals;
    const threshold = thresholds[name];

    if (!threshold) return 'unknown';
    return value <= threshold ? 'good' : 'poor';
  }

  /**
   * Assess Web Vitals Status
   */
  _assessWebVitals(webVitals) {
    if (webVitals.length === 0) return { status: 'no-data' };

    const good = webVitals.filter(v => v.rating === 'good').length;
    const poor = webVitals.filter(v => v.rating === 'poor').length;

    return {
      status: good > poor ? 'good' : 'needs-improvement',
      good,
      poor,
    };
  }

  /**
   * Analyze API Metrics
   */
  _analyzeAPIMetrics(metrics) {
    if (metrics.length === 0) return { status: 'no-data' };

    return {
      totalRequests: metrics.length,
      averageLatency: this._calculatePercentile(metrics, 'latency', 50),
      p95Latency: this._calculatePercentile(metrics, 'latency', 95),
      p99Latency: this._calculatePercentile(metrics, 'latency', 99),
      cacheHitRate: (metrics.filter(m => m.cached).length / metrics.length * 100).toFixed(2),
      errorRate: (metrics.filter(m => m.statusCode >= 400).length / metrics.length * 100).toFixed(2),
    };
  }

  /**
   * Analyze DB Metrics
   */
  _analyzeDBMetrics(metrics) {
    if (metrics.length === 0) return { status: 'no-data' };

    return {
      totalQueries: metrics.length,
      averageLatency: this._calculatePercentile(metrics, 'latency', 50),
      p99Latency: this._calculatePercentile(metrics, 'latency', 99),
      indexUsageRate: (metrics.filter(m => m.indexUsed).length / metrics.length * 100).toFixed(2),
      cachedQueryRate: (metrics.filter(m => m.cached).length / metrics.length * 100).toFixed(2),
    };
  }

  /**
   * Analyze Cache Metrics
   */
  _analyzeCacheMetrics(metrics) {
    if (metrics.length === 0) return { status: 'no-data' };

    return {
      averageHitRate: (metrics.reduce((sum, m) => sum + m.hitRate, 0) / metrics.length).toFixed(2),
      totalEvictions: metrics.reduce((sum, m) => sum + m.evictions, 0),
      averageCacheSize: (metrics.reduce((sum, m) => sum + m.cacheSize, 0) / metrics.length).toFixed(0),
    };
  }

  /**
   * Analyze Web Vitals
   */
  _analyzeWebVitals(metrics) {
    if (metrics.length === 0) return { status: 'no-data' };

    const vitals = {};
    ['CLS', 'LCP', 'FID', 'TTFB'].forEach(vital => {
      const vitalMetrics = metrics.filter(m => m.name === vital);
      if (vitalMetrics.length > 0) {
        vitals[vital] = {
          average: (vitalMetrics.reduce((sum, m) => sum + m.value, 0) / vitalMetrics.length).toFixed(2),
          min: Math.min(...vitalMetrics.map(m => m.value)),
          max: Math.max(...vitalMetrics.map(m => m.value)),
        };
      }
    });

    return vitals;
  }

  /**
   * Check threshold and create alert if exceeded
   */
  _checkThreshold(metricType, value, context = '') {
    let threshold = null;

    switch (metricType) {
      case 'api_latency':
        threshold = this.config.alertThresholds.apiLatency.p99;
        break;
      case 'db_query':
        threshold = this.config.alertThresholds.dbQuery.p99;
        break;
      case 'cache_hit_rate':
        threshold = this.config.alertThresholds.cacheHitRate;
        break;
      case 'bundle_size':
        threshold = this.config.alertThresholds.bundleSize;
        break;
      default:
        return;
    }

    if (value > threshold) {
      const alert = {
        timestamp: Date.now(),
        type: metricType,
        value,
        threshold,
        context,
        severity: value > threshold * 1.5 ? 'critical' : 'warning',
      };

      this.alerts.push(alert);
    }
  }

  /**
   * Sanitize query for logging
   */
  _sanitizeQuery(query) {
    return query
      .replace(/\b[0-9]+\b/g, '?')
      .replace(/\b[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}\b/g, '?')
      .substring(0, 100);
  }

  /**
   * Calculate health status
   */
  _calculateHealthStatus(apiMetrics, dbMetrics, cacheMetrics) {
    let score = 100;

    // Deduct for poor API latency
    if (apiMetrics.length > 0) {
      const p99 = this._calculatePercentile(apiMetrics, 'latency', 99);
      if (p99 > 300) score -= Math.min(20, (p99 - 300) / 50);
    }

    // Deduct for poor DB latency
    if (dbMetrics.length > 0) {
      const p99 = this._calculatePercentile(dbMetrics, 'latency', 99);
      if (p99 > 50) score -= Math.min(20, (p99 - 50) / 10);
    }

    // Deduct for low cache hit rate
    if (cacheMetrics.length > 0) {
      const avgHitRate = cacheMetrics.reduce((sum, m) => sum + m.hitRate, 0) / cacheMetrics.length;
      if (avgHitRate < 80) score -= Math.min(15, (80 - avgHitRate) / 5);
    }

    return {
      score: Math.max(0, score).toFixed(0),
      status: score >= 80 ? 'healthy' : score >= 60 ? 'degraded' : 'critical',
    };
  }
}

module.exports = PerformanceMonitor;
