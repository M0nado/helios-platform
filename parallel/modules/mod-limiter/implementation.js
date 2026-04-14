/**
 * HELIOS v4.0 Rate Limiter Module - Implementation
 * Distributed rate limiting with quota enforcement and metric tracking
 * 
 * @module mod-limiter/implementation
 * @version 4.0.0
 */

/**
 * DistributedLimiter: Manages distributed rate limiting across multiple nodes
 * Supports per-IP, per-user, per-endpoint rate limiting with Redis backend
 * 
 * Performance: Rate limiting check is O(1) with Redis
 * Memory: ~50B per limiter instance, Redis storage depends on active identifiers
 */
class DistributedLimiter {
  /**
   * Create a new DistributedLimiter instance
   * @param {Object} options - Configuration options
   * @param {number} options.maxRequests - Maximum requests per window (default: 100)
   * @param {number} options.windowMs - Time window in milliseconds (default: 60000)
   * @param {Object} options.backend - Backend storage (Redis/Memory)
   * @param {boolean} options.distributed - Enable distributed mode (default: false)
   * @param {Function} options.identifierFn - Function to extract identifier from request
   * @param {string} options.keyPrefix - Redis key prefix (default: 'limiter:')
   */
  constructor(options = {}) {
    this.maxRequests = options.maxRequests ?? 100;
    this.windowMs = options.windowMs ?? 60000;
    this.backend = options.backend || new MemoryBackend();
    this.distributed = options.distributed ?? false;
    this.identifierFn = options.identifierFn || ((req) => req.ip);
    this.keyPrefix = options.keyPrefix ?? 'limiter:';
    this.hits = new Map();
    this.resetTimes = new Map();
  }

  /**
   * Check if request is allowed within rate limit
   * @async
   * @param {Object} req - Request object with ip property
   * @param {Object} options - Check options
   * @param {boolean} options.increment - Increment counter (default: true)
   * @returns {Promise<Object>} Rate limit status
   * @throws {Error} If backend operation fails
   */
  async check(req, options = {}) {
    if (!req || typeof req !== 'object') {
      throw new Error('Request object is required');
    }

    const shouldIncrement = options.increment ?? true;
    const identifier = this.identifierFn(req);

    if (!identifier) {
      throw new Error('Invalid identifier: unable to extract from request');
    }

    const key = `${this.keyPrefix}${identifier}`;
    const now = Date.now();

    try {
      let data = this.distributed
        ? await this.backend.get(key)
        : this.hits.get(identifier);

      if (!data) {
        data = { count: 0, resetTime: now + this.windowMs };
      }

      const isExpired = now >= data.resetTime;
      if (isExpired) {
        data.count = 0;
        data.resetTime = now + this.windowMs;
      }

      const isAllowed = data.count < this.maxRequests;

      if (shouldIncrement) {
        data.count++;
        if (this.distributed) {
          await this.backend.set(key, data, this.windowMs);
        } else {
          this.hits.set(identifier, data);
          this.resetTimes.set(identifier, data.resetTime);
        }
      }

      return {
        allowed: isAllowed,
        limit: this.maxRequests,
        current: data.count,
        remaining: Math.max(0, this.maxRequests - data.count),
        resetTime: data.resetTime,
        resetIn: Math.max(0, data.resetTime - now),
        identifier
      };
    } catch (error) {
      throw new RateLimitError(`Rate limit check failed: ${error.message}`, error);
    }
  }

  /**
   * Get current limit status without incrementing
   * @async
   * @param {Object} req - Request object
   * @returns {Promise<Object>} Current rate limit status
   */
  async status(req) {
    return this.check(req, { increment: false });
  }

  /**
   * Reset rate limit for an identifier
   * @async
   * @param {string} identifier - Identifier (IP, user ID, etc.)
   * @returns {Promise<void>}
   */
  async reset(identifier) {
    if (!identifier) {
      throw new Error('Identifier is required');
    }

    const key = `${this.keyPrefix}${identifier}`;

    if (this.distributed) {
      await this.backend.delete(key);
    } else {
      this.hits.delete(identifier);
      this.resetTimes.delete(identifier);
    }
  }

  /**
   * Reset all rate limits
   * @async
   * @returns {Promise<void>}
   */
  async resetAll() {
    if (this.distributed) {
      await this.backend.clear();
    } else {
      this.hits.clear();
      this.resetTimes.clear();
    }
  }

  /**
   * Get rate limit statistics
   * @async
   * @returns {Promise<Object>} Statistics object
   */
  async stats() {
    const count = this.distributed ? await this.backend.count() : this.hits.size;
    const totalRequests = Array.from(this.hits.values()).reduce((sum, d) => sum + d.count, 0);

    return {
      windowMs: this.windowMs,
      maxRequests: this.maxRequests,
      activeIdentifiers: count,
      totalRequests,
      distributedMode: this.distributed,
      backendType: this.backend.constructor.name
    };
  }

  /**
   * Set custom identifier function
   * @param {Function} fn - Function to extract identifier from request
   * @returns {void}
   */
  setIdentifierFn(fn) {
    if (typeof fn !== 'function') {
      throw new Error('Identifier function must be a function');
    }
    this.identifierFn = fn;
  }

  /**
   * Update rate limit configuration
   * @param {Object} config - Configuration updates
   * @returns {void}
   */
  updateConfig(config) {
    if (config.maxRequests !== undefined) {
      this.maxRequests = config.maxRequests;
    }
    if (config.windowMs !== undefined) {
      this.windowMs = config.windowMs;
    }
  }

  /**
   * Cleanup expired entries
   * @private
   */
  _cleanup() {
    const now = Date.now();
    for (const [id, resetTime] of this.resetTimes.entries()) {
      if (now >= resetTime) {
        this.hits.delete(id);
        this.resetTimes.delete(id);
      }
    }
  }
}

/**
 * QuotaEnforcer: Manages quota limits and enforcement
 * Supports multiple quota levels and flexible quota allocation
 * 
 * Performance: Quota checking is O(1)
 * Memory: ~100B per quota definition
 */
class QuotaEnforcer {
  /**
   * Create a new QuotaEnforcer instance
   * @param {Object} options - Configuration options
   * @param {boolean} options.strict - Enforce hard quotas (default: true)
   * @param {Function} options.onQuotaExceeded - Callback on quota exceeded
   * @param {Object} options.backend - Storage backend
   */
  constructor(options = {}) {
    this.strict = options.strict ?? true;
    this.onQuotaExceeded = options.onQuotaExceeded || null;
    this.backend = options.backend || new MemoryBackend();
    this.quotas = new Map();
    this.usage = new Map();
    this.limits = new Map();
  }

  /**
   * Define a quota with limits
   * @param {string} name - Quota name
   * @param {Object} config - Quota configuration
   * @param {number} config.limit - Quota limit
   * @param {number} config.resetMs - Reset interval in milliseconds
   * @param {number} config.warnThreshold - Warning threshold (0-100)
   * @param {Array<string>} config.dimensions - Quota dimensions (user, endpoint, method)
   * @returns {void}
   * @throws {Error} If invalid configuration
   */
  define(name, config) {
    if (!name || typeof name !== 'string') {
      throw new Error('Quota name must be a non-empty string');
    }
    if (!config || typeof config !== 'object') {
      throw new Error('Quota config must be an object');
    }
    if (typeof config.limit !== 'number' || config.limit <= 0) {
      throw new Error('Quota limit must be a positive number');
    }

    const quota = {
      name,
      limit: config.limit,
      resetMs: config.resetMs || 86400000,
      warnThreshold: config.warnThreshold || 80,
      dimensions: config.dimensions || ['user'],
      createdAt: Date.now()
    };

    this.quotas.set(name, quota);
  }

  /**
   * Check and enforce quota
   * @async
   * @param {string} quotaName - Quota name
   * @param {Object} context - Quota context with dimensions
   * @param {number} amount - Amount to deduct from quota (default: 1)
   * @returns {Promise<Object>} Quota status
   * @throws {Error} If quota exceeded and strict mode enabled
   */
  async enforce(quotaName, context, amount = 1) {
    if (!quotaName || !this.quotas.has(quotaName)) {
      throw new Error(`Unknown quota: ${quotaName}`);
    }
    if (!context || typeof context !== 'object') {
      throw new Error('Context is required');
    }
    if (amount <= 0) {
      throw new Error('Amount must be positive');
    }

    const quota = this.quotas.get(quotaName);
    const key = this._buildKey(quotaName, context);

    try {
      const usage = (this.usage.get(key) || 0) + amount;
      const remaining = Math.max(0, quota.limit - usage);
      const exceeded = usage > quota.limit;
      const percentUsed = (usage / quota.limit) * 100;
      const warned = percentUsed >= quota.warnThreshold;

      const status = {
        quotaName,
        context,
        limit: quota.limit,
        used: usage,
        remaining,
        percentUsed: Math.min(100, percentUsed),
        exceeded,
        warned,
        resetAt: Date.now() + quota.resetMs
      };

      if (exceeded) {
        if (this.onQuotaExceeded) {
          this.onQuotaExceeded(status);
        }
        if (this.strict) {
          throw new QuotaExceededError(`Quota exceeded: ${quotaName}`, status);
        }
      }

      this.usage.set(key, usage);
      return status;
    } catch (error) {
      if (error instanceof QuotaExceededError) throw error;
      throw new QuotaError(`Quota enforcement failed: ${error.message}`, error);
    }
  }

  /**
   * Get quota usage
   * @async
   * @param {string} quotaName - Quota name
   * @param {Object} context - Context object
   * @returns {Promise<Object>} Usage information
   */
  async getUsage(quotaName, context) {
    const quota = this.quotas.get(quotaName);
    if (!quota) return null;

    const key = this._buildKey(quotaName, context);
    const used = this.usage.get(key) || 0;

    return {
      quotaName,
      limit: quota.limit,
      used,
      remaining: Math.max(0, quota.limit - used),
      percentUsed: Math.min(100, (used / quota.limit) * 100)
    };
  }

  /**
   * Reset quota usage
   * @async
   * @param {string} quotaName - Quota name
   * @param {Object} context - Context object (optional)
   * @returns {Promise<void>}
   */
  async resetUsage(quotaName, context = null) {
    if (context) {
      const key = this._buildKey(quotaName, context);
      this.usage.delete(key);
    } else {
      for (const key of this.usage.keys()) {
        if (key.startsWith(quotaName + ':')) {
          this.usage.delete(key);
        }
      }
    }
  }

  /**
   * Get all quotas
   * @returns {Array} Array of quota definitions
   */
  getQuotas() {
    return Array.from(this.quotas.values());
  }

  /**
   * Internal: Build cache key from quota name and context
   * @private
   * @param {string} quotaName - Quota name
   * @param {Object} context - Context object
   * @returns {string} Cache key
   */
  _buildKey(quotaName, context) {
    const quota = this.quotas.get(quotaName);
    const dimensions = quota.dimensions.map(d => context[d] || 'unknown');
    return `${quotaName}:${dimensions.join(':')}`;
  }
}

/**
 * MetricsTracker: Tracks and exports rate limiting metrics
 * Supports Prometheus-style metrics and custom exporters
 * 
 * Performance: Metric recording is O(1)
 * Memory: ~1KB per metric tracked
 */
class MetricsTracker {
  /**
   * Create a new MetricsTracker instance
   * @param {Object} options - Configuration options
   * @param {boolean} options.enabled - Enable metric tracking (default: true)
   * @param {number} options.historySize - Keep history size (default: 1000)
   * @param {Array<string>} options.labels - Metric labels to track
   */
  constructor(options = {}) {
    this.enabled = options.enabled ?? true;
    this.historySize = options.historySize ?? 1000;
    this.labels = options.labels || ['ip', 'method', 'path'];
    this.metrics = new Map();
    this.history = [];
    this.startTime = Date.now();
  }

  /**
   * Record a rate limit event
   * @param {Object} event - Event object
   * @param {string} event.type - Event type (allowed, blocked, quota_exceeded)
   * @param {Object} event.context - Event context
   * @param {Object} event.details - Additional details
   * @returns {void}
   */
  record(event) {
    if (!this.enabled) return;

    if (!event || !event.type) {
      throw new Error('Event type is required');
    }

    const key = this._buildKey(event.context);
    const now = Date.now();

    if (!this.metrics.has(key)) {
      this.metrics.set(key, {
        key,
        context: event.context,
        allowed: 0,
        blocked: 0,
        quotaExceeded: 0,
        totalRequests: 0,
        lastUpdate: now
      });
    }

    const metric = this.metrics.get(key);
    metric.totalRequests++;

    switch (event.type) {
      case 'allowed':
        metric.allowed++;
        break;
      case 'blocked':
        metric.blocked++;
        break;
      case 'quota_exceeded':
        metric.quotaExceeded++;
        break;
    }

    metric.lastUpdate = now;

    this.history.push({
      timestamp: now,
      type: event.type,
      context: event.context,
      details: event.details
    });

    if (this.history.length > this.historySize) {
      this.history.shift();
    }
  }

  /**
   * Get metrics for a context
   * @param {Object} context - Context object
   * @returns {Object} Metrics object
   */
  getMetrics(context) {
    const key = this._buildKey(context);
    return this.metrics.get(key) || null;
  }

  /**
   * Get all metrics
   * @returns {Array} Array of metric objects
   */
  getAllMetrics() {
    return Array.from(this.metrics.values());
  }

  /**
   * Export metrics in Prometheus format
   * @returns {string} Prometheus-style metrics
   */
  exportPrometheus() {
    let output = '';
    const uptime = Date.now() - this.startTime;

    output += `# HELP rate_limiter_uptime_ms Rate limiter uptime in milliseconds\n`;
    output += `# TYPE rate_limiter_uptime_ms gauge\n`;
    output += `rate_limiter_uptime_ms ${uptime}\n\n`;

    for (const metric of this.metrics.values()) {
      const labels = this._formatLabels(metric.context);

      output += `# HELP rate_limiter_allowed Total allowed requests\n`;
      output += `# TYPE rate_limiter_allowed counter\n`;
      output += `rate_limiter_allowed{${labels}} ${metric.allowed}\n\n`;

      output += `# HELP rate_limiter_blocked Total blocked requests\n`;
      output += `# TYPE rate_limiter_blocked counter\n`;
      output += `rate_limiter_blocked{${labels}} ${metric.blocked}\n\n`;

      output += `# HELP rate_limiter_quota_exceeded Total quota exceeded\n`;
      output += `# TYPE rate_limiter_quota_exceeded counter\n`;
      output += `rate_limiter_quota_exceeded{${labels}} ${metric.quotaExceeded}\n\n`;
    }

    return output;
  }

  /**
   * Export metrics as JSON
   * @returns {Object} JSON-formatted metrics
   */
  exportJSON() {
    return {
      timestamp: Date.now(),
      uptime: Date.now() - this.startTime,
      totalMetrics: this.metrics.size,
      historySize: this.history.length,
      metrics: this.getAllMetrics(),
      recentEvents: this.history.slice(-10)
    };
  }

  /**
   * Get metrics summary
   * @returns {Object} Summary statistics
   */
  getSummary() {
    let totalAllowed = 0;
    let totalBlocked = 0;
    let totalQuotaExceeded = 0;

    for (const metric of this.metrics.values()) {
      totalAllowed += metric.allowed;
      totalBlocked += metric.blocked;
      totalQuotaExceeded += metric.quotaExceeded;
    }

    const total = totalAllowed + totalBlocked + totalQuotaExceeded;
    const blockRate = total > 0 ? (totalBlocked / total) * 100 : 0;

    return {
      totalAllowed,
      totalBlocked,
      totalQuotaExceeded,
      totalRequests: total,
      blockRate: blockRate.toFixed(2),
      activeIdentifiers: this.metrics.size
    };
  }

  /**
   * Clear metrics
   * @returns {void}
   */
  clear() {
    this.metrics.clear();
    this.history = [];
  }

  /**
   * Internal: Build key from context
   * @private
   * @param {Object} context - Context object
   * @returns {string} Key
   */
  _buildKey(context) {
    if (!context) return 'unknown';
    return this.labels.map(label => context[label] || 'unknown').join(':');
  }

  /**
   * Internal: Format labels for Prometheus
   * @private
   * @param {Object} context - Context object
   * @returns {string} Formatted labels
   */
  _formatLabels(context) {
    if (!context) return '';
    return this.labels.map(label => `${label}="${context[label] || 'unknown'}"`).join(',');
  }
}

/**
 * MemoryBackend: In-memory storage backend for rate limiting
 */
class MemoryBackend {
  constructor() {
    this.store = new Map();
  }

  async get(key) {
    return this.store.get(key);
  }

  async set(key, value, ttl) {
    this.store.set(key, value);
    if (ttl) {
      setTimeout(() => this.store.delete(key), ttl);
    }
  }

  async delete(key) {
    this.store.delete(key);
  }

  async clear() {
    this.store.clear();
  }

  async count() {
    return this.store.size;
  }
}

/**
 * Custom error: Rate limit exceeded
 */
class RateLimitError extends Error {
  /**
   * @param {string} message - Error message
   * @param {Error} originalError - Original error
   */
  constructor(message, originalError) {
    super(message);
    this.name = 'RateLimitError';
    this.originalError = originalError;
  }
}

/**
 * Custom error: Quota exceeded
 */
class QuotaExceededError extends Error {
  /**
   * @param {string} message - Error message
   * @param {Object} status - Quota status
   */
  constructor(message, status) {
    super(message);
    this.name = 'QuotaExceededError';
    this.status = status;
  }
}

/**
 * Custom error: Quota enforcement error
 */
class QuotaError extends Error {
  /**
   * @param {string} message - Error message
   * @param {Error} originalError - Original error
   */
  constructor(message, originalError) {
    super(message);
    this.name = 'QuotaError';
    this.originalError = originalError;
  }
}

module.exports = {
  DistributedLimiter,
  QuotaEnforcer,
  MetricsTracker,
  MemoryBackend,
  RateLimitError,
  QuotaExceededError,
  QuotaError
};
