/**
 * Adaptive cache strategy for HELIOS v4.0.
 *
 * Provides a dependency-free cache-first layer used by benchmarks and runtime
 * orchestration. Supports LRU and LFU eviction modes, stale fallback, and
 * metrics that are stable in offline automation environments.
 */

class CacheStrategy {
  constructor(config = {}) {
    this.config = {
      maxEntries: config.maxEntries || 1000,
      ttlMs: config.ttlMs || config.ttl || 300000,
      staleTtlMs: config.staleTtlMs || 600000,
      strategy: config.strategy || 'lru',
      ...config,
    };

    this.activeStrategy = this.config.strategy;
    this.store = new Map();
    this.metrics = {
      hits: 0,
      misses: 0,
      sets: 0,
      evictions: 0,
      staleCacheServed: 0,
    };
  }

  set(key, value, ttlMs = this.config.ttlMs) {
    const now = Date.now();
    this.store.set(key, {
      value,
      expiresAt: now + ttlMs,
      staleUntil: now + ttlMs + this.config.staleTtlMs,
      hits: 0,
      lastAccessed: now,
      createdAt: now,
    });
    this.metrics.sets++;
    this._evictIfNeeded();
    return value;
  }

  get(key, options = {}) {
    const entry = this.store.get(key);
    const now = Date.now();

    if (!entry) {
      this.metrics.misses++;
      return null;
    }

    if (entry.expiresAt < now) {
      if (options.allowStale && entry.staleUntil >= now) {
        this.metrics.hits++;
        this.metrics.staleCacheServed++;
        this._touch(entry);
        return entry.value;
      }

      this.store.delete(key);
      this.metrics.misses++;
      return null;
    }

    this.metrics.hits++;
    this._touch(entry);
    return entry.value;
  }

  has(key) {
    return this.get(key) !== null;
  }

  delete(key) {
    return this.store.delete(key);
  }

  clear() {
    this.store.clear();
  }

  async cacheFirst(key, loader, options = {}) {
    const cached = this.get(key, { allowStale: options.allowStale !== false });
    if (cached !== null) {
      return { value: cached, data: cached, source: 'cache' };
    }

    const value = await loader();
    this.set(key, value, options.ttlMs || this.config.ttlMs);
    return { value, data: value, source: 'origin' };
  }

  switchStrategy(strategy) {
    if (!['lru', 'lfu'].includes(strategy)) {
      throw new Error(`Unsupported cache strategy: ${strategy}`);
    }
    this.activeStrategy = strategy;
  }

  getStats() {
    return {
      hits: this.metrics.hits,
      misses: this.metrics.misses,
      hitRate: this._formatHitRate(),
      size: this.store.size,
      evictions: this.metrics.evictions,
    };
  }

  getStatistics() {
    return {
      totalCacheHits: this.metrics.hits,
      totalCacheMisses: this.metrics.misses,
      hitRate: this._formatHitRate(),
      staleCacheServed: this.metrics.staleCacheServed,
      cacheStatus: {
        strategy: this.activeStrategy,
        staticCacheSize: this.store.size,
        maxEntries: this.config.maxEntries,
      },
    };
  }

  _touch(entry) {
    entry.hits++;
    entry.lastAccessed = Date.now();
  }

  _formatHitRate() {
    const total = this.metrics.hits + this.metrics.misses;
    return total === 0 ? '0.00%' : `${((this.metrics.hits / total) * 100).toFixed(2)}%`;
  }

  _evictIfNeeded() {
    while (this.store.size > this.config.maxEntries) {
      const entries = [...this.store.entries()];
      const [keyToDelete] = entries.sort((a, b) => {
        if (this.activeStrategy === 'lfu') {
          return a[1].hits - b[1].hits || a[1].lastAccessed - b[1].lastAccessed;
        }
        return a[1].lastAccessed - b[1].lastAccessed;
      })[0];
      this.store.delete(keyToDelete);
      this.metrics.evictions++;
    }
  }
}

module.exports = CacheStrategy;
