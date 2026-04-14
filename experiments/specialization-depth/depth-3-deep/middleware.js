/**
 * HELIOS REST API - Deep Specialist (Depth 3)
 * Agent 3: Middleware Stack
 * 
 * Responsible for:
 * - Authentication & authorization
 * - Rate limiting
 * - Caching strategies
 * - Compression
 * 
 * @module middleware
 * @version 1.0.0
 */

const jwt = require('jsonwebtoken');

/**
 * Middleware Manager
 * Handles all middleware concerns
 * @class Middleware
 */
class Middleware {
  /**
   * Initialize Middleware Manager
   * @param {Object} config - Configuration
   * @param {string} config.jwtSecret - JWT secret
   * @param {Object} config.cache - Cache config
   */
  constructor(config = {}) {
    this.jwtSecret = config.jwtSecret || 'default-secret-key';
    this.cacheConfig = config.cache || { ttl: 300, maxSize: 1000 };
    
    this.middlewares = [];
    this.cache = new Map();
    this.rateLimiters = new Map();
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
   * @returns {Object|null} Decoded token
   */
  verifyToken(token) {
    try {
      return jwt.verify(token, this.jwtSecret);
    } catch (error) {
      return null;
    }
  }

  /**
   * Register middleware
   * @param {Function} middlewareFn - Middleware function
   */
  use(middlewareFn) {
    this.middlewares.push(middlewareFn);
  }

  /**
   * Execute middleware chain
   * @async
   * @param {Object} request - Request context
   * @returns {Promise<Object>} Result
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
   * @param {any} value - Value
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
   * @returns {any|null} Value or null
   */
  _getCacheEntry(key) {
    const entry = this.cache.get(key);
    if (!entry) {
      return null;
    }

    const age = (Date.now() - entry.timestamp) / 1000;
    if (age > entry.ttl) {
      this.cache.delete(key);
      return null;
    }

    return entry.value;
  }

  /**
   * Generate cache key
   * @private
   * @param {string} method - HTTP method
   * @param {string} path - Request path
   * @param {Object} query - Query params
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
   * Get from cache
   * @param {string} method - HTTP method
   * @param {string} path - Request path
   * @param {Object} query - Query params
   * @returns {any|null} Cached value
   */
  getFromCache(method, path, query) {
    if (method !== 'GET') return null;
    const key = this._generateCacheKey(method, path, query);
    return this._getCacheEntry(key);
  }

  /**
   * Store in cache
   * @param {string} method - HTTP method
   * @param {string} path - Request path
   * @param {Object} query - Query params
   * @param {any} data - Data to cache
   */
  storeInCache(method, path, query, data) {
    if (method === 'GET') {
      const key = this._generateCacheKey(method, path, query);
      this._setCacheEntry(key, data);
    }
  }

  /**
   * Clear cache
   */
  clearCache() {
    this.cache.clear();
  }

  /**
   * Get cache size
   * @returns {number} Cache size
   */
  getCacheSize() {
    return this.cache.size;
  }
}

module.exports = Middleware;
