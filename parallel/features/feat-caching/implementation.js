/**
 * HTTP Caching Module - feat-caching
 * Production-ready HTTP cache implementation with ETag support,
 * cache control headers, and conditional request handling.
 * @module feat-caching/implementation
 */

/**
 * HTTPCache - Core HTTP cache storage and retrieval
 * Implements RFC 7234 HTTP caching semantics
 * Performance: O(1) cache hits, O(n) invalidation where n = cache size
 */
class HTTPCache {
  /**
   * Initialize HTTP cache with size limits
   * @param {Object} options - Configuration options
   * @param {number} [options.maxSize=104857600] - Max cache size in bytes (default 100MB)
   * @param {number} [options.maxEntries=10000] - Max number of entries
   * @param {number} [options.defaultTTL=3600000] - Default TTL in ms (1 hour)
   * @throws {Error} If options are invalid
   */
  constructor(options = {}) {
    if (typeof options !== 'object') {
      throw new Error('Options must be an object');
    }
    
    this.maxSize = options.maxSize || 104857600;
    this.maxEntries = options.maxEntries || 10000;
    this.defaultTTL = options.defaultTTL || 3600000;
    this.cache = new Map();
    this.stats = {
      hits: 0,
      misses: 0,
      evictions: 0,
      size: 0
    };
    
    if (this.maxSize <= 0 || this.maxEntries <= 0) {
      throw new Error('maxSize and maxEntries must be positive numbers');
    }
  }

  /**
   * Store response in cache with metadata
   * @param {string} key - Cache key (URL)
   * @param {Object} value - Response object
   * @param {*} value.data - Response body
   * @param {Object} value.headers - Response headers
   * @param {number} value.status - HTTP status code
   * @param {Object} [options] - Caching options
   * @param {number} [options.ttl] - Time to live in milliseconds
   * @param {boolean} [options.isPrivate=false] - Private cache only
   * @returns {boolean} True if cached successfully
   * @throws {Error} If key is not a string
   */
  set(key, value, options = {}) {
    if (typeof key !== 'string') {
      throw new Error('Cache key must be a string');
    }
    if (!value || typeof value !== 'object') {
      throw new Error('Cache value must be an object');
    }

    const ttl = options.ttl || this.defaultTTL;
    const size = this._estimateSize(value);
    
    if (size > this.maxSize) {
      return false;
    }

    // Evict old entries if necessary
    while (this.cache.size >= this.maxEntries && this.cache.size > 0) {
      const firstKey = this.cache.keys().next().value;
      this.cache.delete(firstKey);
      this.stats.evictions++;
    }

    const entry = {
      data: value.data,
      headers: value.headers || {},
      status: value.status || 200,
      timestamp: Date.now(),
      expiresAt: Date.now() + ttl,
      isPrivate: options.isPrivate || false
    };

    this.cache.set(key, entry);
    this.stats.size += size;
    return true;
  }

  /**
   * Retrieve cached response
   * @param {string} key - Cache key
   * @returns {Object|null} Cached response or null if expired/missing
   * @throws {Error} If key is not a string
   */
  get(key) {
    if (typeof key !== 'string') {
      throw new Error('Cache key must be a string');
    }

    const entry = this.cache.get(key);
    
    if (!entry) {
      this.stats.misses++;
      return null;
    }

    if (Date.now() > entry.expiresAt) {
      this.cache.delete(key);
      this.stats.misses++;
      return null;
    }

    this.stats.hits++;
    return entry;
  }

  /**
   * Check if entry is fresh (not expired)
   * @param {string} key - Cache key
   * @returns {boolean} True if entry exists and is fresh
   */
  isFresh(key) {
    const entry = this.cache.get(key);
    return entry && Date.now() <= entry.expiresAt;
  }

  /**
   * Check if entry is stale
   * @param {string} key - Cache key
   * @returns {boolean} True if entry exists and is expired
   */
  isStale(key) {
    const entry = this.cache.get(key);
    return entry && Date.now() > entry.expiresAt;
  }

  /**
   * Invalidate cache entry
   * @param {string} key - Cache key
   * @returns {boolean} True if entry was deleted
   */
  invalidate(key) {
    return this.cache.delete(key);
  }

  /**
   * Invalidate all cache entries matching pattern
   * @param {RegExp|string} pattern - Pattern to match keys
   * @returns {number} Number of entries invalidated
   */
  invalidateByPattern(pattern) {
    let count = 0;
    const regex = pattern instanceof RegExp ? pattern : new RegExp(pattern);
    
    for (const key of this.cache.keys()) {
      if (regex.test(key)) {
        this.cache.delete(key);
        count++;
      }
    }
    return count;
  }

  /**
   * Clear entire cache
   * @returns {void}
   */
  clear() {
    this.cache.clear();
    this.stats.size = 0;
  }

  /**
   * Get cache statistics
   * @returns {Object} Stats object with hits, misses, evictions, size
   */
  getStats() {
    return { ...this.stats };
  }

  /**
   * Calculate hit ratio
   * @returns {number} Hit ratio between 0 and 1
   */
  getHitRatio() {
    const total = this.stats.hits + this.stats.misses;
    return total === 0 ? 0 : this.stats.hits / total;
  }

  /**
   * Estimate size of value in bytes
   * @private
   * @param {Object} value - Value to measure
   * @returns {number} Estimated size in bytes
   */
  _estimateSize(value) {
    return JSON.stringify(value).length * 2; // UTF-16 estimate
  }
}

/**
 * ETagManager - HTTP ETag generation and validation
 * Implements RFC 7232 entity tags
 * Performance: O(1) ETag validation
 */
class ETagManager {
  /**
   * Initialize ETag manager
   * @param {Object} [options] - Configuration options
   * @param {boolean} [options.weak=false] - Generate weak ETags
   */
  constructor(options = {}) {
    this.weak = options.weak || false;
    this.etagCache = new Map();
  }

  /**
   * Generate ETag from content
   * @param {string|Buffer} content - Content to hash
   * @returns {string} ETag string
   * @throws {Error} If content is invalid
   */
  generate(content) {
    if (typeof content !== 'string' && !Buffer.isBuffer(content)) {
      throw new Error('Content must be string or Buffer');
    }

    const hash = this._simpleHash(content);
    const prefix = this.weak ? 'W/' : '';
    return `${prefix}"${hash}"`;
  }

  /**
   * Validate ETag match (strong comparison)
   * @param {string} etagA - First ETag
   * @param {string} etagB - Second ETag
   * @returns {boolean} True if ETags match
   */
  matches(etagA, etagB) {
    return this._normalize(etagA) === this._normalize(etagB);
  }

  /**
   * Validate ETag match (weak comparison)
   * @param {string} etagA - First ETag
   * @param {string} etagB - Second ETag
   * @returns {boolean} True if ETags match (ignoring weak prefix)
   */
  weakMatches(etagA, etagB) {
    return this._normalize(etagA, true) === this._normalize(etagB, true);
  }

  /**
   * Check if ETag is weak
   * @param {string} etag - ETag string
   * @returns {boolean} True if weak ETag
   */
  isWeak(etag) {
    return etag.startsWith('W/');
  }

  /**
   * Normalize ETag for comparison
   * @private
   * @param {string} etag - ETag to normalize
   * @param {boolean} [ignoreWeak=false] - Ignore weak prefix
   * @returns {string} Normalized ETag
   */
  _normalize(etag, ignoreWeak = false) {
    let normalized = etag.trim();
    if (ignoreWeak && normalized.startsWith('W/')) {
      normalized = normalized.substring(2);
    }
    return normalized;
  }

  /**
   * Simple hash function for ETag generation
   * @private
   * @param {string|Buffer} content - Content to hash
   * @returns {string} Hash string
   */
  _simpleHash(content) {
    const str = content.toString();
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash;
    }
    return Math.abs(hash).toString(16);
  }
}

/**
 * CacheControl - Parse and generate Cache-Control headers
 * Implements RFC 7234 cache directives
 * Performance: O(n) where n = number of directives
 */
class CacheControl {
  /**
   * Initialize CacheControl parser/generator
   */
  constructor() {
    this.directives = {
      'public': { type: 'flag' },
      'private': { type: 'flag' },
      'no-cache': { type: 'flag' },
      'no-store': { type: 'flag' },
      'must-revalidate': { type: 'flag' },
      'proxy-revalidate': { type: 'flag' },
      'immutable': { type: 'flag' },
      'max-age': { type: 'value' },
      's-maxage': { type: 'value' },
      'stale-while-revalidate': { type: 'value' },
      'stale-if-error': { type: 'value' }
    };
  }

  /**
   * Parse Cache-Control header
   * @param {string} headerValue - Cache-Control header value
   * @returns {Object} Parsed directives
   * @throws {Error} If header format is invalid
   */
  parse(headerValue) {
    if (typeof headerValue !== 'string') {
      throw new Error('Header value must be a string');
    }

    const result = {};
    const parts = headerValue.split(',');

    for (const part of parts) {
      const [key, value] = part.split('=').map(s => s.trim());
      
      if (!key) continue;
      
      const lowerKey = key.toLowerCase();
      if (this.directives[lowerKey]) {
        result[lowerKey] = this.directives[lowerKey].type === 'value' 
          ? parseInt(value) || 0 
          : true;
      }
    }

    return result;
  }

  /**
   * Generate Cache-Control header value
   * @param {Object} config - Cache directives
   * @returns {string} Cache-Control header value
   * @throws {Error} If config is invalid
   */
  generate(config) {
    if (typeof config !== 'object') {
      throw new Error('Config must be an object');
    }

    const parts = [];

    for (const [key, value] of Object.entries(config)) {
      if (this.directives[key]) {
        if (this.directives[key].type === 'flag' && value) {
          parts.push(key);
        } else if (this.directives[key].type === 'value' && value) {
          parts.push(`${key}=${value}`);
        }
      }
    }

    return parts.join(', ');
  }

  /**
   * Check if response is cacheable
   * @param {Object} cacheControl - Parsed Cache-Control directives
   * @param {number} status - HTTP status code
   * @returns {boolean} True if cacheable
   */
  isCacheable(cacheControl, status) {
    if (cacheControl['no-store']) return false;
    if (cacheControl['private'] && cacheControl.privateCache === false) return false;
    
    const cacheableStatuses = [200, 203, 204, 206, 300, 301, 404, 405, 410, 414, 501];
    return cacheableStatuses.includes(status);
  }

  /**
   * Check if cache needs revalidation
   * @param {Object} cacheControl - Parsed Cache-Control directives
   * @returns {boolean} True if revalidation required
   */
  needsRevalidation(cacheControl) {
    return cacheControl['must-revalidate'] === true;
  }
}

/**
 * ConditionalRequest - Handle conditional HTTP requests
 * Implements RFC 7232 conditional requests (If-None-Match, If-Modified-Since)
 * Performance: O(1) condition evaluation
 */
class ConditionalRequest {
  /**
   * Initialize conditional request handler
   */
  constructor() {
    this.etagManager = new ETagManager();
  }

  /**
   * Check If-None-Match condition
   * @param {string} etagHeader - If-None-Match header value
   * @param {string} currentETag - Current ETag
   * @returns {boolean} True if condition is true (resource not modified)
   * @throws {Error} If headers are invalid
   */
  checkIfNoneMatch(etagHeader, currentETag) {
    if (typeof etagHeader !== 'string' || typeof currentETag !== 'string') {
      throw new Error('ETag headers must be strings');
    }

    if (etagHeader === '*') return false;

    const tags = etagHeader.split(',').map(t => t.trim());
    return tags.some(tag => this.etagManager.matches(tag, currentETag));
  }

  /**
   * Check If-Modified-Since condition
   * @param {Date|string|number} ifModifiedSince - If-Modified-Since header value
   * @param {Date|string|number} lastModified - Last-Modified date
   * @returns {boolean} True if condition is true (not modified since)
   * @throws {Error} If dates are invalid
   */
  checkIfModifiedSince(ifModifiedSince, lastModified) {
    const ifTime = new Date(ifModifiedSince).getTime();
    const modTime = new Date(lastModified).getTime();

    if (isNaN(ifTime) || isNaN(modTime)) {
      throw new Error('Invalid date format');
    }

    return modTime <= ifTime;
  }

  /**
   * Check If-Match condition
   * @param {string} etagHeader - If-Match header value
   * @param {string} currentETag - Current ETag
   * @returns {boolean} True if condition is true (resource matches)
   */
  checkIfMatch(etagHeader, currentETag) {
    if (etagHeader === '*') return true;
    
    const tags = etagHeader.split(',').map(t => t.trim());
    return tags.some(tag => this.etagManager.matches(tag, currentETag));
  }

  /**
   * Check If-Unmodified-Since condition
   * @param {Date|string|number} ifUnmodifiedSince - If-Unmodified-Since header
   * @param {Date|string|number} lastModified - Last-Modified date
   * @returns {boolean} True if condition is true (unmodified since)
   */
  checkIfUnmodifiedSince(ifUnmodifiedSince, lastModified) {
    const ifTime = new Date(ifUnmodifiedSince).getTime();
    const modTime = new Date(lastModified).getTime();

    if (isNaN(ifTime) || isNaN(modTime)) {
      throw new Error('Invalid date format');
    }

    return modTime <= ifTime;
  }

  /**
   * Build response for conditional request
   * @param {Object} conditions - Condition check results
   * @param {boolean} conditions.ifNoneMatch - If-None-Match result
   * @param {boolean} conditions.ifModifiedSince - If-Modified-Since result
   * @param {string} etag - Resource ETag
   * @param {string} lastModified - Last-Modified date
   * @returns {Object} Response object with status and headers
   */
  buildResponse(conditions, etag, lastModified) {
    if (conditions.ifNoneMatch || conditions.ifModifiedSince) {
      return {
        status: 304,
        headers: {
          'etag': etag,
          'last-modified': lastModified,
          'cache-control': 'public, max-age=3600'
        },
        body: null
      };
    }

    return {
      status: 200,
      headers: {
        'etag': etag,
        'last-modified': lastModified
      }
    };
  }
}

module.exports = {
  HTTPCache,
  ETagManager,
  CacheControl,
  ConditionalRequest
};
