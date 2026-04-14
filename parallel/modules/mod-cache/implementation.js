/**
 * HELIOS v4.0 Cache Manager Module - Implementation
 * Provides advanced caching with TTL management, eviction policies, and distributed caching
 * @module mod-cache/implementation
 */

/**
 * TTL Manager - Handles time-to-live expiration
 * @class TTLManager
 */
class TTLManager {
  constructor() {
    /**
     * Map of key -> expiration timestamp
     * @type {Map<string, number>}
     * @private
     */
    this.expirations = new Map();

    /**
     * Timer for cleanup checks
     * @type {NodeJS.Timer|null}
     * @private
     */
    this.cleanupTimer = null;
  }

  /**
   * Set TTL for a key
   * @param {string} key - Cache key
   * @param {number} ttlMs - Time to live in milliseconds
   * @throws {TypeError} If key is not a string or ttlMs is not a positive number
   * @returns {void}
   */
  setTTL(key, ttlMs) {
    if (typeof key !== 'string') throw new TypeError('Key must be a string');
    if (typeof ttlMs !== 'number' || ttlMs <= 0) throw new TypeError('TTL must be a positive number');
    this.expirations.set(key, Date.now() + ttlMs);
  }

  /**
   * Check if a key has expired
   * @param {string} key - Cache key
   * @returns {boolean} True if expired, false if valid or key not found
   */
  isExpired(key) {
    const expiration = this.expirations.get(key);
    if (!expiration) return false;
    if (Date.now() > expiration) {
      this.expirations.delete(key);
      return true;
    }
    return false;
  }

  /**
   * Get remaining TTL for a key
   * @param {string} key - Cache key
   * @returns {number|null} Remaining TTL in milliseconds, or null if key not found
   */
  getRemainingTTL(key) {
    const expiration = this.expirations.get(key);
    if (!expiration) return null;
    const remaining = expiration - Date.now();
    return remaining > 0 ? remaining : null;
  }

  /**
   * Remove TTL tracking for a key
   * @param {string} key - Cache key
   * @returns {void}
   */
  removeTTL(key) {
    this.expirations.delete(key);
  }

  /**
   * Get count of tracked TTLs
   * @returns {number} Number of keys with TTL
   */
  size() {
    return this.expirations.size;
  }

  /**
   * Clear all TTL tracking
   * @returns {void}
   */
  clear() {
    this.expirations.clear();
  }
}

/**
 * Eviction Policy - Manages cache eviction strategies
 * @class EvictionPolicy
 */
class EvictionPolicy {
  /**
   * @param {string} type - Eviction type: 'LRU', 'LFU', 'FIFO'
   * @throws {Error} If unsupported eviction type
   */
  constructor(type = 'LRU') {
    if (!['LRU', 'LFU', 'FIFO'].includes(type)) throw new Error(`Unsupported eviction type: ${type}`);
    this.type = type;
    /** @type {Map<string, number>} Access/frequency tracking */
    this.accessMap = new Map();
    /** @type {Map<string, number>} Insertion order for FIFO */
    this.insertionOrder = new Map();
    this.counter = 0;
  }

  /**
   * Track an access
   * @param {string} key - Cache key
   * @returns {void}
   */
  trackAccess(key) {
    if (this.type === 'LRU') {
      this.accessMap.set(key, Date.now());
    } else if (this.type === 'LFU') {
      this.accessMap.set(key, (this.accessMap.get(key) || 0) + 1);
    } else if (this.type === 'FIFO' && !this.insertionOrder.has(key)) {
      this.insertionOrder.set(key, this.counter++);
    }
  }

  /**
   * Track key insertion
   * @param {string} key - Cache key
   * @returns {void}
   */
  trackInsertion(key) {
    if (this.type === 'FIFO') {
      this.insertionOrder.set(key, this.counter++);
    } else {
      this.trackAccess(key);
    }
  }

  /**
   * Select key to evict
   * @param {string[]} keys - Available keys to consider
   * @returns {string|null} Key to evict, or null if no candidates
   */
  selectForEviction(keys) {
    if (keys.length === 0) return null;

    if (this.type === 'LRU') {
      return keys.reduce((oldest, key) => {
        const oldestTime = this.accessMap.get(oldest) || 0;
        const currentTime = this.accessMap.get(key) || 0;
        return currentTime < oldestTime ? key : oldest;
      });
    }

    if (this.type === 'LFU') {
      return keys.reduce((least, key) => {
        const leastFreq = this.accessMap.get(least) || 0;
        const currentFreq = this.accessMap.get(key) || 0;
        return currentFreq < leastFreq ? key : least;
      });
    }

    if (this.type === 'FIFO') {
      return keys.reduce((oldest, key) => {
        const oldestOrder = this.insertionOrder.get(oldest) ?? Infinity;
        const currentOrder = this.insertionOrder.get(key) ?? Infinity;
        return currentOrder < oldestOrder ? key : oldest;
      });
    }
  }

  /**
   * Remove tracking for evicted key
   * @param {string} key - Cache key
   * @returns {void}
   */
  removeKey(key) {
    this.accessMap.delete(key);
    this.insertionOrder.delete(key);
  }

  /**
   * Clear all tracking data
   * @returns {void}
   */
  clear() {
    this.accessMap.clear();
    this.insertionOrder.clear();
  }
}

/**
 * Distributed Cache - Manages cache synchronization across instances
 * @class DistributedCache
 */
class DistributedCache {
  /**
   * @param {string} nodeId - Unique identifier for this cache instance
   */
  constructor(nodeId = 'default') {
    /** @type {string} Node identifier */
    this.nodeId = nodeId;
    /** @type {Map<string, object>} Distributed state */
    this.distributedState = new Map();
    /** @type {Function[]} Event listeners for sync events */
    this.syncListeners = [];
    /** @type {number} Last sync timestamp */
    this.lastSyncTime = Date.now();
    /** @type {number} Sync interval in milliseconds */
    this.syncInterval = 1000;
  }

  /**
   * Register a sync listener
   * @param {Function} listener - Callback for sync events
   * @returns {void}
   * @throws {TypeError} If listener is not a function
   */
  onSync(listener) {
    if (typeof listener !== 'function') throw new TypeError('Listener must be a function');
    this.syncListeners.push(listener);
  }

  /**
   * Broadcast a cache operation to peers
   * @param {string} operation - Operation type: 'set', 'delete', 'clear'
   * @param {string} key - Cache key (if applicable)
   * @param {*} value - Cache value (if applicable)
   * @returns {void}
   */
  broadcast(operation, key, value) {
    const syncEvent = {
      nodeId: this.nodeId,
      operation,
      key,
      value,
      timestamp: Date.now(),
    };

    this.distributedState.set(`${operation}:${key}`, syncEvent);
    this.syncListeners.forEach(listener => {
      try {
        listener(syncEvent);
      } catch (error) {
        console.error(`Sync listener error: ${error.message}`);
      }
    });
  }

  /**
   * Handle sync event from peer
   * @param {object} syncEvent - Event from peer node
   * @returns {void}
   */
  handleSyncEvent(syncEvent) {
    if (typeof syncEvent !== 'object' || !syncEvent.operation) {
      throw new Error('Invalid sync event');
    }
    this.distributedState.set(`${syncEvent.operation}:${syncEvent.key}`, syncEvent);
    this.lastSyncTime = Date.now();
  }

  /**
   * Get sync events since last sync
   * @returns {object[]} Array of sync events
   */
  getSyncEvents() {
    return Array.from(this.distributedState.values()).filter(
      event => event.timestamp > this.lastSyncTime - this.syncInterval
    );
  }

  /**
   * Clear distributed state
   * @returns {void}
   */
  clear() {
    this.distributedState.clear();
  }
}

/**
 * Cache Manager - Main cache implementation
 * @class CacheManager
 */
class CacheManager {
  /**
   * @param {object} options - Configuration options
   * @param {number} [options.maxSize=1000] - Maximum cache entries
   * @param {number} [options.defaultTTL=3600000] - Default TTL in milliseconds
   * @param {string} [options.evictionPolicy='LRU'] - Eviction strategy
   * @param {boolean} [options.distributed=false] - Enable distributed caching
   * @param {string} [options.nodeId='default'] - Node identifier for distributed cache
   * @throws {TypeError} If options are invalid
   */
  constructor(options = {}) {
    this.maxSize = options.maxSize || 1000;
    this.defaultTTL = options.defaultTTL || 3600000;

    if (typeof this.maxSize !== 'number' || this.maxSize <= 0) {
      throw new TypeError('maxSize must be a positive number');
    }
    if (typeof this.defaultTTL !== 'number' || this.defaultTTL <= 0) {
      throw new TypeError('defaultTTL must be a positive number');
    }

    /** @type {Map<string, *>} Main cache storage */
    this.cache = new Map();
    /** @type {TTLManager} TTL manager instance */
    this.ttlManager = new TTLManager();
    /** @type {EvictionPolicy} Eviction policy instance */
    this.evictionPolicy = new EvictionPolicy(options.evictionPolicy || 'LRU');
    /** @type {DistributedCache|null} Distributed cache instance */
    this.distributedCache = options.distributed ? new DistributedCache(options.nodeId) : null;
    /** @type {number} Statistics: hits */
    this.stats = { hits: 0, misses: 0, evictions: 0 };
  }

  /**
   * Set a value in cache
   * @param {string} key - Cache key
   * @param {*} value - Value to cache
   * @param {number} [ttl] - Optional TTL in milliseconds
   * @throws {TypeError} If key is not a string
   * @returns {void}
   */
  set(key, value, ttl = null) {
    if (typeof key !== 'string') throw new TypeError('Key must be a string');

    if (this.cache.size >= this.maxSize && !this.cache.has(key)) {
      this._evict();
    }

    this.cache.set(key, value);
    this.evictionPolicy.trackInsertion(key);

    const effectiveTTL = ttl || this.defaultTTL;
    this.ttlManager.setTTL(key, effectiveTTL);

    if (this.distributedCache) {
      this.distributedCache.broadcast('set', key, value);
    }
  }

  /**
   * Get a value from cache
   * @param {string} key - Cache key
   * @returns {*} Cached value, or undefined if not found or expired
   */
  get(key) {
    if (this.ttlManager.isExpired(key)) {
      this._delete(key);
      this.stats.misses++;
      return undefined;
    }

    if (this.cache.has(key)) {
      this.evictionPolicy.trackAccess(key);
      this.stats.hits++;
      return this.cache.get(key);
    }

    this.stats.misses++;
    return undefined;
  }

  /**
   * Check if key exists and is valid
   * @param {string} key - Cache key
   * @returns {boolean} True if key exists and not expired
   */
  has(key) {
    if (this.ttlManager.isExpired(key)) {
      this._delete(key);
      return false;
    }
    return this.cache.has(key);
  }

  /**
   * Delete a key from cache
   * @param {string} key - Cache key
   * @returns {boolean} True if key was deleted, false if not found
   */
  delete(key) {
    return this._delete(key);
  }

  /**
   * Internal delete with distribution support
   * @private
   * @param {string} key - Cache key
   * @returns {boolean} True if deleted
   */
  _delete(key) {
    const deleted = this.cache.delete(key);
    if (deleted) {
      this.ttlManager.removeTTL(key);
      this.evictionPolicy.removeKey(key);
      if (this.distributedCache) {
        this.distributedCache.broadcast('delete', key);
      }
    }
    return deleted;
  }

  /**
   * Evict oldest/least-used entry
   * @private
   * @returns {void}
   */
  _evict() {
    const keys = Array.from(this.cache.keys());
    const keyToEvict = this.evictionPolicy.selectForEviction(keys);
    if (keyToEvict) {
      this._delete(keyToEvict);
      this.stats.evictions++;
    }
  }

  /**
   * Clear entire cache
   * @returns {void}
   */
  clear() {
    this.cache.clear();
    this.ttlManager.clear();
    this.evictionPolicy.clear();
    if (this.distributedCache) {
      this.distributedCache.broadcast('clear');
    }
  }

  /**
   * Warm up cache with initial data
   * @param {object} data - Key-value pairs to load
   * @param {number} [ttl] - Optional TTL for all entries
   * @throws {TypeError} If data is not an object
   * @returns {void}
   */
  warmup(data, ttl = null) {
    if (typeof data !== 'object' || data === null) {
      throw new TypeError('Warmup data must be an object');
    }

    Object.entries(data).forEach(([key, value]) => {
      this.set(key, value, ttl);
    });
  }

  /**
   * Get cache statistics
   * @returns {object} Statistics including hits, misses, evictions, size
   */
  getStats() {
    return {
      ...this.stats,
      size: this.cache.size,
      maxSize: this.maxSize,
      hitRate: this.stats.hits + this.stats.misses > 0
        ? (this.stats.hits / (this.stats.hits + this.stats.misses)).toFixed(2)
        : 'N/A',
    };
  }

  /**
   * Register distributed sync listener
   * @param {Function} listener - Callback for sync events
   * @returns {void}
   * @throws {Error} If distributed caching not enabled
   */
  onDistributedSync(listener) {
    if (!this.distributedCache) {
      throw new Error('Distributed caching not enabled');
    }
    this.distributedCache.onSync(listener);
  }

  /**
   * Sync with distributed peers
   * @param {object[]} syncEvents - Events from peer nodes
   * @returns {void}
   */
  syncWithPeers(syncEvents) {
    if (!this.distributedCache) return;
    syncEvents.forEach(event => {
      try {
        this.distributedCache.handleSyncEvent(event);
      } catch (error) {
        console.error(`Sync error: ${error.message}`);
      }
    });
  }

  /**
   * Get all keys in cache
   * @returns {string[]} Array of all cache keys
   */
  keys() {
    return Array.from(this.cache.keys()).filter(key => !this.ttlManager.isExpired(key));
  }

  /**
   * Get cache size
   * @returns {number} Number of valid entries
   */
  size() {
    return this.cache.size;
  }
}

module.exports = {
  CacheManager,
  TTLManager,
  EvictionPolicy,
  DistributedCache,
};
