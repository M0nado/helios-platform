/**
 * feat-caching - HTTP Response Caching Module
 * Production-ready HTTP caching with ETag support, cache-control headers,
 * and conditional request handling per RFC 7234.
 * @module feat-caching
 * @version 1.0.0
 */

const {
  HTTPCache,
  ETagManager,
  CacheControl,
  ConditionalRequest
} = require('./implementation');

module.exports = {
  HTTPCache,
  ETagManager,
  CacheControl,
  ConditionalRequest,
  
  // Factory functions for common use cases
  
  /**
   * Create HTTP cache with default configuration
   * @param {Object} [options] - Override options
   * @returns {HTTPCache} Configured cache instance
   */
  createCache: (options) => new HTTPCache(options),
  
  /**
   * Create ETag manager
   * @param {Object} [options] - Configuration options
   * @returns {ETagManager} ETag manager instance
   */
  createETagManager: (options) => new ETagManager(options),
  
  /**
   * Create Cache-Control parser/generator
   * @returns {CacheControl} Cache control instance
   */
  createCacheControl: () => new CacheControl(),
  
  /**
   * Create conditional request handler
   * @returns {ConditionalRequest} Conditional request handler
   */
  createConditionalRequest: () => new ConditionalRequest(),
  
  /**
   * Create integrated cache middleware (all components together)
   * @param {Object} [options] - Configuration options
   * @returns {Object} Object with cache, etagManager, cacheControl, conditionalRequest
   */
  createIntegratedCache: (options) => ({
    cache: new HTTPCache(options),
    etagManager: new ETagManager({ weak: false }),
    cacheControl: new CacheControl(),
    conditionalRequest: new ConditionalRequest()
  })
};
