/**
 * HELIOS v4.0 Cache Manager - Comprehensive Test Suite
 */

const assert = require('assert');
const { CacheManager, TTLManager, EvictionPolicy, DistributedCache } = require('../implementation');

describe('TTLManager', () => {
  let ttlManager;

  beforeEach(() => {
    ttlManager = new TTLManager();
  });

  it('should set and retrieve TTL', () => {
    ttlManager.setTTL('key1', 5000);
    assert.strictEqual(ttlManager.getRemainingTTL('key1') > 0, true);
  });

  it('should throw error on invalid key type', () => {
    assert.throws(() => ttlManager.setTTL(123, 5000), TypeError);
  });

  it('should throw error on invalid TTL', () => {
    assert.throws(() => ttlManager.setTTL('key', -100), TypeError);
    assert.throws(() => ttlManager.setTTL('key', 0), TypeError);
  });

  it('should detect expired keys', (done) => {
    ttlManager.setTTL('expiring', 100);
    setTimeout(() => {
      assert.strictEqual(ttlManager.isExpired('expiring'), true);
      done();
    }, 150);
  });

  it('should return null for non-existent key', () => {
    assert.strictEqual(ttlManager.getRemainingTTL('nonexistent'), null);
  });

  it('should remove TTL tracking', () => {
    ttlManager.setTTL('key', 5000);
    ttlManager.removeTTL('key');
    assert.strictEqual(ttlManager.getRemainingTTL('key'), null);
  });

  it('should track size correctly', () => {
    ttlManager.setTTL('k1', 5000);
    ttlManager.setTTL('k2', 5000);
    assert.strictEqual(ttlManager.size(), 2);
  });

  it('should clear all TTLs', () => {
    ttlManager.setTTL('k1', 5000);
    ttlManager.setTTL('k2', 5000);
    ttlManager.clear();
    assert.strictEqual(ttlManager.size(), 0);
  });
});

describe('EvictionPolicy', () => {
  it('should throw error on invalid policy type', () => {
    assert.throws(() => new EvictionPolicy('INVALID'), Error);
  });

  describe('LRU Policy', () => {
    let policy;

    beforeEach(() => {
      policy = new EvictionPolicy('LRU');
    });

    it('should track access by timestamp', () => {
      policy.trackAccess('key1');
      policy.trackAccess('key2');
      assert.strictEqual(policy.selectForEviction(['key1', 'key2']), 'key1');
    });

    it('should update access on second access', (done) => {
      policy.trackAccess('key1');
      setTimeout(() => {
        policy.trackAccess('key2');
        assert.strictEqual(policy.selectForEviction(['key1', 'key2']), 'key1');
        done();
      }, 10);
    });
  });

  describe('LFU Policy', () => {
    let policy;

    beforeEach(() => {
      policy = new EvictionPolicy('LFU');
    });

    it('should track frequency', () => {
      policy.trackAccess('key1');
      policy.trackAccess('key2');
      policy.trackAccess('key2');
      assert.strictEqual(policy.selectForEviction(['key1', 'key2']), 'key1');
    });

    it('should evict least frequently used', () => {
      for (let i = 0; i < 5; i++) policy.trackAccess('key1');
      for (let i = 0; i < 2; i++) policy.trackAccess('key2');
      assert.strictEqual(policy.selectForEviction(['key1', 'key2']), 'key2');
    });
  });

  describe('FIFO Policy', () => {
    let policy;

    beforeEach(() => {
      policy = new EvictionPolicy('FIFO');
    });

    it('should track insertion order', () => {
      policy.trackInsertion('key1');
      policy.trackInsertion('key2');
      assert.strictEqual(policy.selectForEviction(['key1', 'key2']), 'key1');
    });

    it('should ignore access in FIFO', () => {
      policy.trackInsertion('key1');
      policy.trackInsertion('key2');
      policy.trackAccess('key1');
      policy.trackAccess('key1');
      assert.strictEqual(policy.selectForEviction(['key1', 'key2']), 'key1');
    });
  });

  it('should remove key tracking', () => {
    const policy = new EvictionPolicy('LRU');
    policy.trackAccess('key1');
    policy.removeKey('key1');
    assert.strictEqual(policy.selectForEviction(['key1']), 'key1');
  });

  it('should clear all tracking', () => {
    const policy = new EvictionPolicy('LRU');
    policy.trackAccess('key1');
    policy.clear();
    assert.strictEqual(policy.size ? policy.size() : true, true);
  });
});

describe('DistributedCache', () => {
  let distCache;

  beforeEach(() => {
    distCache = new DistributedCache('node1');
  });

  it('should initialize with node ID', () => {
    assert.strictEqual(distCache.nodeId, 'node1');
  });

  it('should register sync listeners', () => {
    const listener = () => {};
    distCache.onSync(listener);
    assert.strictEqual(distCache.syncListeners.length, 1);
  });

  it('should throw error on non-function listener', () => {
    assert.throws(() => distCache.onSync('not-function'), TypeError);
  });

  it('should broadcast sync events', (done) => {
    distCache.onSync((event) => {
      assert.strictEqual(event.operation, 'set');
      assert.strictEqual(event.key, 'key1');
      done();
    });
    distCache.broadcast('set', 'key1', { value: 42 });
  });

  it('should handle sync events', () => {
    const syncEvent = { operation: 'set', key: 'k1', value: { data: 1 }, timestamp: Date.now() };
    distCache.handleSyncEvent(syncEvent);
    assert.strictEqual(distCache.distributedState.size > 0, true);
  });

  it('should throw on invalid sync event', () => {
    assert.throws(() => distCache.handleSyncEvent({}), Error);
  });

  it('should get recent sync events', () => {
    distCache.broadcast('set', 'k1', 'v1');
    const events = distCache.getSyncEvents();
    assert.strictEqual(events.length > 0, true);
  });

  it('should clear distributed state', () => {
    distCache.broadcast('set', 'k1', 'v1');
    distCache.clear();
    assert.strictEqual(distCache.distributedState.size, 0);
  });
});

describe('CacheManager', () => {
  let cache;

  beforeEach(() => {
    cache = new CacheManager({ maxSize: 100 });
  });

  describe('Basic Operations', () => {
    it('should set and get values', () => {
      cache.set('key1', 'value1');
      assert.strictEqual(cache.get('key1'), 'value1');
    });

    it('should throw error on non-string key', () => {
      assert.throws(() => cache.set(123, 'value'), TypeError);
    });

    it('should return undefined for non-existent key', () => {
      assert.strictEqual(cache.get('nonexistent'), undefined);
    });

    it('should check key existence', () => {
      cache.set('key1', 'value1');
      assert.strictEqual(cache.has('key1'), true);
      assert.strictEqual(cache.has('missing'), false);
    });

    it('should delete keys', () => {
      cache.set('key1', 'value1');
      const deleted = cache.delete('key1');
      assert.strictEqual(deleted, true);
      assert.strictEqual(cache.has('key1'), false);
    });

    it('should return false when deleting non-existent key', () => {
      assert.strictEqual(cache.delete('nonexistent'), false);
    });

    it('should clear entire cache', () => {
      cache.set('k1', 'v1');
      cache.set('k2', 'v2');
      cache.clear();
      assert.strictEqual(cache.size(), 0);
    });
  });

  describe('TTL Management', () => {
    it('should expire keys based on TTL', (done) => {
      cache.set('shortlived', 'value', 100);
      setTimeout(() => {
        assert.strictEqual(cache.get('shortlived'), undefined);
        done();
      }, 150);
    });

    it('should use default TTL', (done) => {
      cache = new CacheManager({ defaultTTL: 200 });
      cache.set('key', 'value');
      assert.strictEqual(cache.get('key'), 'value');
      setTimeout(() => {
        assert.strictEqual(cache.get('key'), undefined);
        done();
      }, 250);
    });

    it('should override default TTL', (done) => {
      cache = new CacheManager({ defaultTTL: 5000 });
      cache.set('key', 'value', 100);
      setTimeout(() => {
        assert.strictEqual(cache.get('key'), undefined);
        done();
      }, 150);
    });
  });

  describe('Eviction Policies', () => {
    it('should evict when maxSize reached (LRU)', () => {
      cache = new CacheManager({ maxSize: 3, evictionPolicy: 'LRU' });
      cache.set('k1', 'v1');
      cache.set('k2', 'v2');
      cache.set('k3', 'v3');
      cache.set('k4', 'v4');
      assert.strictEqual(cache.size() <= 3, true);
      assert.strictEqual(cache.stats.evictions, 1);
    });

    it('should evict least frequently used (LFU)', () => {
      cache = new CacheManager({ maxSize: 3, evictionPolicy: 'LFU' });
      cache.set('k1', 'v1');
      cache.get('k1');
      cache.get('k1');
      cache.set('k2', 'v2');
      cache.set('k3', 'v3');
      cache.set('k4', 'v4');
      assert.strictEqual(cache.has('k2'), true);
      assert.strictEqual(cache.stats.evictions, 1);
    });

    it('should evict in FIFO order', () => {
      cache = new CacheManager({ maxSize: 3, evictionPolicy: 'FIFO' });
      cache.set('k1', 'v1');
      cache.set('k2', 'v2');
      cache.set('k3', 'v3');
      cache.set('k4', 'v4');
      assert.strictEqual(cache.has('k1'), false);
    });
  });

  describe('Statistics', () => {
    it('should track cache hits', () => {
      cache.set('key', 'value');
      cache.get('key');
      cache.get('key');
      assert.strictEqual(cache.stats.hits, 2);
    });

    it('should track cache misses', () => {
      cache.get('missing1');
      cache.get('missing2');
      assert.strictEqual(cache.stats.misses, 2);
    });

    it('should calculate hit rate', () => {
      cache.set('key', 'value');
      cache.get('key');
      cache.get('missing');
      const stats = cache.getStats();
      assert.strictEqual(stats.hitRate, '0.50');
    });

    it('should report cache size', () => {
      cache.set('k1', 'v1');
      cache.set('k2', 'v2');
      const stats = cache.getStats();
      assert.strictEqual(stats.size, 2);
    });
  });

  describe('Warmup', () => {
    it('should load initial data', () => {
      const data = { k1: 'v1', k2: 'v2', k3: 'v3' };
      cache.warmup(data);
      assert.strictEqual(cache.size(), 3);
      assert.strictEqual(cache.get('k1'), 'v1');
    });

    it('should throw on invalid data', () => {
      assert.throws(() => cache.warmup('not-object'), TypeError);
      assert.throws(() => cache.warmup(null), TypeError);
    });

    it('should apply warmup TTL', (done) => {
      cache.warmup({ key: 'value' }, 100);
      setTimeout(() => {
        assert.strictEqual(cache.get('key'), undefined);
        done();
      }, 150);
    });
  });

  describe('Key Management', () => {
    it('should list all keys', () => {
      cache.set('k1', 'v1');
      cache.set('k2', 'v2');
      const keys = cache.keys();
      assert.strictEqual(keys.includes('k1'), true);
      assert.strictEqual(keys.includes('k2'), true);
    });

    it('should exclude expired keys from list', (done) => {
      cache.set('k1', 'v1');
      cache.set('k2', 'v2', 100);
      setTimeout(() => {
        const keys = cache.keys();
        assert.strictEqual(keys.includes('k1'), true);
        assert.strictEqual(keys.includes('k2'), false);
        done();
      }, 150);
    });
  });

  describe('Constructor Validation', () => {
    it('should throw on invalid maxSize', () => {
      assert.throws(() => new CacheManager({ maxSize: 0 }), TypeError);
      assert.throws(() => new CacheManager({ maxSize: -10 }), TypeError);
    });

    it('should throw on invalid defaultTTL', () => {
      assert.throws(() => new CacheManager({ defaultTTL: 0 }), TypeError);
      assert.throws(() => new CacheManager({ defaultTTL: -100 }), TypeError);
    });
  });

  describe('Distributed Caching', () => {
    it('should enable distributed cache', () => {
      cache = new CacheManager({ distributed: true });
      assert.strictEqual(cache.distributedCache !== null, true);
    });

    it('should broadcast set operations', (done) => {
      cache = new CacheManager({ distributed: true });
      cache.onDistributedSync(() => {
        done();
      });
      cache.set('key', 'value');
    });

    it('should sync with peers', () => {
      cache = new CacheManager({ distributed: true });
      const events = [{ operation: 'set', key: 'k1', value: 'v1', timestamp: Date.now() }];
      cache.syncWithPeers(events);
      assert.strictEqual(cache.distributedCache.distributedState.size > 0, true);
    });

    it('should throw on sync without distributed enabled', () => {
      cache = new CacheManager({ distributed: false });
      assert.throws(() => cache.onDistributedSync(() => {}), Error);
    });
  });
});
