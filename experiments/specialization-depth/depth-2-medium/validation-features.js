/**
 * HELIOS REST API - Medium Specialist (Depth 2)
 * Agent 2: REST Middleware & Features
 * 
 * Responsible for:
 * - Authentication & authorization
 * - Caching strategies
 * - Request/response validation
 * - Error handling
 * - Monitoring & telemetry
 * 
 * @module validation-features
 * @version 1.0.0
 */

const jwt = require('jsonwebtoken');

/**
 * Validation & Features Manager
 * Handles auth, caching, validation, errors, and monitoring
 * @class ValidationFeaturesManager
 */
class ValidationFeaturesManager {
  /**
   * Initialize Validation & Features Manager
   * @param {Object} config - Configuration
   * @param {string} config.jwtSecret - JWT secret
   * @param {Object} config.cache - Cache config
   * @param {number} config.cache.ttl - Cache TTL
   * @param {number} config.cache.maxSize - Max cache size
   */
  constructor(config = {}) {
    this.jwtSecret = config.jwtSecret || 'default-secret-key';
    this.cacheConfig = config.cache || { ttl: 300, maxSize: 1000 };
    
    this.cache = new Map();
    this.metrics = {
      requests: 0,
      errors: 0,
      cacheHits: 0,
      cacheMisses: 0,
      avgResponseTime: 0,
      responseTimes: []
    };
    this.middlewares = [];
  }

  /**
   * Create JWT token
   * @param {Object} payload - Token payload
   * @param {Object} options - JWT options
   * @returns {string} JWT token
   */
  createToken(payload, options = {}) {
    return jwt.sign(payload, this.jwtSecret, {
      expiresIn: options.expiresIn || '24h',
      ...options
    });
  }

  /**
   * Verify JWT token
   * @param {string} token - JWT token
   * @returns {Object|null} Decoded token or null
   */
  verifyToken(token) {
    try {
      return jwt.verify(token, this.jwtSecret);
    } catch (error) {
      return null;
    }
  }

  /**
   * Validate request data against schema
   * @param {Object} data - Data to validate
   * @param {Object} schema - JSON Schema
   * @returns {Object} Validation result
   */
  validateRequest(data, schema) {
    const errors = [];

    if (schema.required) {
      for (const field of schema.required) {
        if (!(field in data)) {
          errors.push(`Missing required field: ${field}`);
        }
      }
    }

    if (schema.properties) {
      for (const [field, fieldSchema] of Object.entries(schema.properties)) {
        if (field in data) {
          const value = data[field];
          const expectedType = fieldSchema.type;

          if (expectedType && typeof value !== expectedType) {
            errors.push(
              `Field '${field}' expected type '${expectedType}', got '${typeof value}'`
            );
          }

          if (fieldSchema.pattern && typeof value === 'string') {
            const regex = new RegExp(fieldSchema.pattern);
            if (!regex.test(value)) {
              errors.push(`Field '${field}' does not match pattern: ${fieldSchema.pattern}`);
            }
          }

          if (fieldSchema.minLength && value.length < fieldSchema.minLength) {
            errors.push(
              `Field '${field}' must be at least ${fieldSchema.minLength} characters`
            );
          }

          if (fieldSchema.maxLength && value.length > fieldSchema.maxLength) {
            errors.push(
              `Field '${field}' must be at most ${fieldSchema.maxLength} characters`
            );
          }

          if (fieldSchema.enum && !fieldSchema.enum.includes(value)) {
            errors.push(
              `Field '${field}' must be one of: ${fieldSchema.enum.join(', ')}`
            );
          }
        }
      }
    }

    return {
      valid: errors.length === 0,
      errors
    };
  }

  /**
   * Register middleware function
   * @param {Function} middlewareFn - Middleware function
   */
  use(middlewareFn) {
    this.middlewares.push(middlewareFn);
  }

  /**
   * Execute middleware chain
   * @async
   * @param {Object} request - Request context
   * @returns {Promise<Object>} Middleware result
   */
  async executeMiddlewares(request) {
    let context = { ...request, passed: true };

    for (const middleware of this.middlewares) {
      context = await middleware(context);
      if (context.passed === false) {
        break;
      }
    }

    return context;
  }

  /**
   * Set cache entry
   * @private
   * @param {string} key - Cache key
   * @param {any} value - Value to cache
   */
  _setCacheEntry(key, value) {
    if (this.cache.size >= this.cacheConfig.maxSize) {
      const firstKey = this.cache.keys().next().value;
      this.cache.delete(firstKey);
    }

    this.cache.set(key, {
      value,
      timestamp: Date.now(),
      ttl: this.cacheConfig.ttl
    });
  }

  /**
   * Get cache entry
   * @private
   * @param {string} key - Cache key
   * @returns {any|null} Cached value or null
   */
  _getCacheEntry(key) {
    const entry = this.cache.get(key);
    if (!entry) {
      this.metrics.cacheMisses++;
      return null;
    }

    const age = (Date.now() - entry.timestamp) / 1000;
    if (age > entry.ttl) {
      this.cache.delete(key);
      this.metrics.cacheMisses++;
      return null;
    }

    this.metrics.cacheHits++;
    return entry.value;
  }

  /**
   * Generate cache key
   * @private
   * @param {string} method - HTTP method
   * @param {string} path - Request path
   * @param {Object} query - Query parameters
   * @returns {string} Cache key
   */
  _generateCacheKey(method, path, query = {}) {
    const queryStr = Object.entries(query)
      .sort()
      .map(([k, v]) => `${k}=${v}`)
      .join('&');
    return `${method}:${path}:${queryStr}`;
  }

  /**
   * Attempt to get from cache
   * @param {string} method - HTTP method
   * @param {string} path - Request path
   * @param {Object} query - Query parameters
   * @returns {any|null} Cached value or null
   */
  getFromCache(method, path, query) {
    if (method !== 'GET') {
      return null;
    }
    const key = this._generateCacheKey(method, path, query);
    return this._getCacheEntry(key);
  }

  /**
   * Store in cache
   * @param {string} method - HTTP method
   * @param {string} path - Request path
   * @param {Object} query - Query parameters
   * @param {any} data - Data to cache
   */
  storeInCache(method, path, query, data) {
    if (method === 'GET') {
      const key = this._generateCacheKey(method, path, query);
      this._setCacheEntry(key, data);
    }
  }

  /**
   * Format success response
   * @param {any} data - Response data
   * @param {Object} options - Response options
   * @returns {Object} Formatted response
   */
  formatSuccessResponse(data, options = {}) {
    return {
      success: true,
      status: options.status || 200,
      data,
      timestamp: new Date().toISOString(),
      cached: options.cached || false
    };
  }

  /**
   * Format error response
   * @param {number} status - HTTP status
   * @param {string} message - Error message
   * @param {Object} details - Error details
   * @returns {Object} Formatted error
   */
  formatErrorResponse(status, message, details = {}) {
    return {
      success: false,
      status,
      error: {
        message,
        code: `ERR_${status}`,
        details
      },
      timestamp: new Date().toISOString()
    };
  }

  /**
   * Track request metrics
   * @param {number} duration - Request duration in ms
   * @param {boolean} isError - Is error request
   */
  trackMetrics(duration, isError = false) {
    this.metrics.requests++;
    if (isError) {
      this.metrics.errors++;
    }
    this.metrics.responseTimes.push(duration);
    this.metrics.avgResponseTime = 
      this.metrics.responseTimes.reduce((a, b) => a + b, 0) / 
      this.metrics.responseTimes.length;
  }

  /**
   * Get current metrics
   * @returns {Object} Current metrics
   */
  getMetrics() {
    return {
      ...this.metrics,
      cacheSize: this.cache.size
    };
  }

  /**
   * Clear cache
   */
  clearCache() {
    this.cache.clear();
    this.metrics.cacheHits = 0;
    this.metrics.cacheMisses = 0;
  }

  /**
   * Reset metrics
   */
  resetMetrics() {
    this.metrics = {
      requests: 0,
      errors: 0,
      cacheHits: 0,
      cacheMisses: 0,
      avgResponseTime: 0,
      responseTimes: []
    };
  }
}

module.exports = ValidationFeaturesManager;
