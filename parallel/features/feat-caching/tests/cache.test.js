/**
 * feat-caching - Comprehensive Test Suite
 * 45+ tests covering cache strategies, ETags, headers, and conditional requests
 */

const assert = require('assert');
const {
  HTTPCache,
  ETagManager,
  CacheControl,
  ConditionalRequest
} = require('../implementation');

describe('HTTPCache', () => {
  let cache;

  beforeEach(() => {
    cache = new HTTPCache();
  });

  describe('Constructor & Configuration', () => {
    it('should initialize with default options', () => {
      assert.strictEqual(cache.maxSize, 104857600);
      assert.strictEqual(cache.maxEntries, 10000);
      assert.strictEqual(cache.defaultTTL, 3600000);
    });

    it('should accept custom options', () => {
      const custom = new HTTPCache({
        maxSize: 1000000,
        maxEntries: 500,
        defaultTTL: 7200000
      });
      assert.strictEqual(custom.maxSize, 1000000);
      assert.strictEqual(custom.maxEntries, 500);
      assert.strictEqual(custom.defaultTTL, 7200000);
    });

    it('should throw error for invalid options', () => {
      assert.throws(() => new HTTPCache(null));
      assert.throws(() => new HTTPCache({ maxSize: -1 }));
      assert.throws(() => new HTTPCache({ maxEntries: 0 }));
    });
  });

  describe('set() method', () => {
    it('should store response in cache', () => {
      const result = cache.set('key1', {
        data: { test: 'data' },
        headers: { 'content-type': 'application/json' },
        status: 200
      });
      assert.strictEqual(result, true);
    });

    it('should return false if content exceeds maxSize', () => {
      const largeData = 'x'.repeat(cache.maxSize + 1);
      const result = cache.set('large', {
        data: largeData,
        headers: {},
        status: 200
      });
      assert.strictEqual(result, false);
    });

    it('should throw error for non-string key', () => {
      assert.throws(() => cache.set(123, { data: 'test', status: 200 }));
      assert.throws(() => cache.set(null, { data: 'test', status: 200 }));
    });

    it('should throw error for invalid value', () => {
      assert.throws(() => cache.set('key', null));
      assert.throws(() => cache.set('key', 'string'));
    });

    it('should store with custom TTL', () => {
      cache.set('ttl-test', { data: 'test', status: 200 }, { ttl: 5000 });
      const entry = cache.cache.get('ttl-test');
      assert(entry.expiresAt > Date.now());
    });

    it('should mark entries as private when specified', () => {
      cache.set('private', { data: 'secret', status: 200 }, { isPrivate: true });
      const entry = cache.cache.get('private');
      assert.strictEqual(entry.isPrivate, true);
    });
  });

  describe('get() method', () => {
    it('should retrieve cached response', () => {
      const data = { test: 'data' };
      cache.set('key1', { data, headers: {}, status: 200 });
      
      const result = cache.get('key1');
      assert.deepStrictEqual(result.data, data);
    });

    it('should return null for missing key', () => {
      const result = cache.get('missing');
      assert.strictEqual(result, null);
    });

    it('should return null for expired entry', (done) => {
      cache.set('expiring', { data: 'test', status: 200 }, { ttl: 50 });
      
      setTimeout(() => {
        const result = cache.get('expiring');
        assert.strictEqual(result, null);
        done();
      }, 100);
    });

    it('should throw error for non-string key', () => {
      assert.throws(() => cache.get(123));
    });

    it('should include status and headers', () => {
      const headers = { 'content-type': 'application/json' };
      cache.set('test', { data: 'data', headers, status: 201 });
      
      const result = cache.get('test');
      assert.strictEqual(result.status, 201);
      assert.deepStrictEqual(result.headers, headers);
    });

    it('should track cache hits', () => {
      cache.set('key1', { data: 'test', status: 200 });
      cache.get('key1');
      cache.get('key1');
      
      assert.strictEqual(cache.stats.hits, 2);
    });

    it('should track cache misses', () => {
      cache.get('missing1');
      cache.get('missing2');
      
      assert.strictEqual(cache.stats.misses, 2);
    });
  });

  describe('isFresh() & isStale() methods', () => {
    it('should indicate fresh cache', (done) => {
      cache.set('fresh', { data: 'test', status: 200 }, { ttl: 5000 });
      assert.strictEqual(cache.isFresh('fresh'), true);
      assert.strictEqual(cache.isStale('fresh'), false);
      done();
    });

    it('should indicate stale cache', (done) => {
      cache.set('stale', { data: 'test', status: 200 }, { ttl: 50 });
      
      setTimeout(() => {
        assert.strictEqual(cache.isFresh('stale'), false);
        assert.strictEqual(cache.isStale('stale'), true);
        done();
      }, 100);
    });

    it('should return false for missing entry', () => {
      assert.strictEqual(cache.isFresh('missing'), false);
      assert.strictEqual(cache.isStale('missing'), false);
    });
  });

  describe('invalidate() method', () => {
    it('should remove cache entry', () => {
      cache.set('key1', { data: 'test', status: 200 });
      assert.strictEqual(cache.invalidate('key1'), true);
      assert.strictEqual(cache.get('key1'), null);
    });

    it('should return false for non-existent entry', () => {
      assert.strictEqual(cache.invalidate('missing'), false);
    });

    it('should support multiple invalidations', () => {
      cache.set('key1', { data: 'test', status: 200 });
      cache.set('key2', { data: 'test', status: 200 });
      
      assert.strictEqual(cache.invalidate('key1'), true);
      assert.strictEqual(cache.invalidate('key2'), true);
      assert.strictEqual(cache.cache.size, 0);
    });
  });

  describe('invalidateByPattern() method', () => {
    it('should invalidate entries matching pattern', () => {
      cache.set('api/users/1', { data: 'user1', status: 200 });
      cache.set('api/users/2', { data: 'user2', status: 200 });
      cache.set('api/posts/1', { data: 'post1', status: 200 });
      
      const count = cache.invalidateByPattern(/^api\/users\//);
      assert.strictEqual(count, 2);
      assert.strictEqual(cache.get('api/posts/1'), null);
    });

    it('should accept string pattern', () => {
      cache.set('user:1', { data: 'test', status: 200 });
      cache.set('user:2', { data: 'test', status: 200 });
      cache.set('post:1', { data: 'test', status: 200 });
      
      const count = cache.invalidateByPattern('user:');
      assert.strictEqual(count, 2);
    });

    it('should return 0 if no matches', () => {
      cache.set('api/users/1', { data: 'test', status: 200 });
      
      const count = cache.invalidateByPattern(/^admin\//);
      assert.strictEqual(count, 0);
    });
  });

  describe('clear() method', () => {
    it('should remove all entries', () => {
      cache.set('key1', { data: 'test', status: 200 });
      cache.set('key2', { data: 'test', status: 200 });
      
      cache.clear();
      assert.strictEqual(cache.cache.size, 0);
    });

    it('should reset size counter', () => {
      cache.set('key1', { data: 'x'.repeat(1000), status: 200 });
      cache.clear();
      
      assert.strictEqual(cache.stats.size, 0);
    });
  });

  describe('getStats() & getHitRatio() methods', () => {
    it('should return cache statistics', () => {
      cache.set('key1', { data: 'test', status: 200 });
      cache.get('key1');
      cache.get('missing');
      
      const stats = cache.getStats();
      assert.strictEqual(stats.hits, 1);
      assert.strictEqual(stats.misses, 1);
      assert(stats.size > 0);
    });

    it('should calculate correct hit ratio', () => {
      cache.set('key1', { data: 'test', status: 200 });
      cache.get('key1');
      cache.get('key1');
      cache.get('missing');
      
      const ratio = cache.getHitRatio();
      assert.strictEqual(ratio, 2 / 3);
    });

    it('should return 0 hit ratio for empty cache', () => {
      assert.strictEqual(cache.getHitRatio(), 0);
    });
  });
});

describe('ETagManager', () => {
  let manager;

  beforeEach(() => {
    manager = new ETagManager();
  });

  describe('generate() method', () => {
    it('should generate ETag from string', () => {
      const etag = manager.generate('test content');
      assert(etag.startsWith('"'));
      assert(etag.endsWith('"'));
    });

    it('should generate ETag from Buffer', () => {
      const buffer = Buffer.from('test content');
      const etag = manager.generate(buffer);
      assert(etag.startsWith('"'));
      assert(etag.endsWith('"'));
    });

    it('should generate consistent ETag for same content', () => {
      const etag1 = manager.generate('content');
      const etag2 = manager.generate('content');
      assert.strictEqual(etag1, etag2);
    });

    it('should generate different ETag for different content', () => {
      const etag1 = manager.generate('content1');
      const etag2 = manager.generate('content2');
      assert.notStrictEqual(etag1, etag2);
    });

    it('should generate weak ETag when configured', () => {
      const weakManager = new ETagManager({ weak: true });
      const etag = weakManager.generate('content');
      assert(etag.startsWith('W/'));
    });

    it('should throw error for invalid content', () => {
      assert.throws(() => manager.generate(null));
      assert.throws(() => manager.generate(123));
    });
  });

  describe('matches() method', () => {
    it('should return true for identical ETags', () => {
      const etag = manager.generate('content');
      assert.strictEqual(manager.matches(etag, etag), true);
    });

    it('should return false for different ETags', () => {
      const etag1 = manager.generate('content1');
      const etag2 = manager.generate('content2');
      assert.strictEqual(manager.matches(etag1, etag2), false);
    });

    it('should return false for weak vs strong ETag', () => {
      const strongETag = manager.generate('content');
      const weakManager = new ETagManager({ weak: true });
      const weakETag = weakManager.generate('content');
      
      assert.strictEqual(manager.matches(strongETag, weakETag), false);
    });
  });

  describe('weakMatches() method', () => {
    it('should match weak and strong ETag', () => {
      const strongETag = manager.generate('content');
      const weakManager = new ETagManager({ weak: true });
      const weakETag = weakManager.generate('content');
      
      assert.strictEqual(weakManager.weakMatches(strongETag, weakETag), true);
    });

    it('should match identical weak ETags', () => {
      const weakManager = new ETagManager({ weak: true });
      const etag = weakManager.generate('content');
      assert.strictEqual(weakManager.weakMatches(etag, etag), true);
    });
  });

  describe('isWeak() method', () => {
    it('should detect weak ETag', () => {
      const weakManager = new ETagManager({ weak: true });
      const etag = weakManager.generate('content');
      assert.strictEqual(manager.isWeak(etag), true);
    });

    it('should return false for strong ETag', () => {
      const etag = manager.generate('content');
      assert.strictEqual(manager.isWeak(etag), false);
    });
  });
});

describe('CacheControl', () => {
  let cc;

  beforeEach(() => {
    cc = new CacheControl();
  });

  describe('parse() method', () => {
    it('should parse flag directives', () => {
      const result = cc.parse('public, must-revalidate');
      assert.strictEqual(result.public, true);
      assert.strictEqual(result['must-revalidate'], true);
    });

    it('should parse value directives', () => {
      const result = cc.parse('max-age=3600, s-maxage=86400');
      assert.strictEqual(result['max-age'], 3600);
      assert.strictEqual(result['s-maxage'], 86400);
    });

    it('should parse mixed directives', () => {
      const result = cc.parse('public, max-age=3600, must-revalidate');
      assert.strictEqual(result.public, true);
      assert.strictEqual(result['max-age'], 3600);
      assert.strictEqual(result['must-revalidate'], true);
    });

    it('should handle empty header', () => {
      const result = cc.parse('');
      assert.deepStrictEqual(result, {});
    });

    it('should throw error for non-string', () => {
      assert.throws(() => cc.parse(null));
      assert.throws(() => cc.parse(123));
    });
  });

  describe('generate() method', () => {
    it('should generate flag directives', () => {
      const result = cc.generate({ public: true, 'must-revalidate': true });
      assert(result.includes('public'));
      assert(result.includes('must-revalidate'));
    });

    it('should generate value directives', () => {
      const result = cc.generate({ 'max-age': 3600, 's-maxage': 86400 });
      assert(result.includes('max-age=3600'));
      assert(result.includes('s-maxage=86400'));
    });

    it('should skip false values', () => {
      const result = cc.generate({ public: false, 'max-age': 3600 });
      assert(!result.includes('public'));
      assert(result.includes('max-age'));
    });

    it('should throw error for non-object', () => {
      assert.throws(() => cc.generate('invalid'));
    });
  });

  describe('isCacheable() method', () => {
    it('should return false for no-store', () => {
      const directives = cc.parse('no-store');
      assert.strictEqual(cc.isCacheable(directives, 200), false);
    });

    it('should return true for 200 OK', () => {
      const directives = cc.parse('public, max-age=3600');
      assert.strictEqual(cc.isCacheable(directives, 200), true);
    });

    it('should handle cacheable 3xx and 4xx', () => {
      assert.strictEqual(cc.isCacheable({}, 300), true);
      assert.strictEqual(cc.isCacheable({}, 404), true);
    });

    it('should return false for error statuses', () => {
      assert.strictEqual(cc.isCacheable({}, 502), false);
      assert.strictEqual(cc.isCacheable({}, 503), false);
    });
  });

  describe('needsRevalidation() method', () => {
    it('should return true for must-revalidate', () => {
      const directives = { 'must-revalidate': true };
      assert.strictEqual(cc.needsRevalidation(directives), true);
    });

    it('should return false without must-revalidate', () => {
      const directives = { public: true, 'max-age': 3600 };
      assert.strictEqual(cc.needsRevalidation(directives), false);
    });
  });
});

describe('ConditionalRequest', () => {
  let conditional;

  beforeEach(() => {
    conditional = new ConditionalRequest();
  });

  describe('checkIfNoneMatch() method', () => {
    it('should return true for matching ETag', () => {
      const result = conditional.checkIfNoneMatch('"abc123"', '"abc123"');
      assert.strictEqual(result, true);
    });

    it('should return false for different ETag', () => {
      const result = conditional.checkIfNoneMatch('"abc123"', '"def456"');
      assert.strictEqual(result, false);
    });

    it('should handle wildcard *', () => {
      const result = conditional.checkIfNoneMatch('*', '"abc123"');
      assert.strictEqual(result, false);
    });

    it('should throw error for invalid input', () => {
      assert.throws(() => conditional.checkIfNoneMatch(null, '"abc"'));
      assert.throws(() => conditional.checkIfNoneMatch('"abc"', null));
    });
  });

  describe('checkIfModifiedSince() method', () => {
    it('should return true if not modified', () => {
      const date = new Date('2024-01-15');
      const result = conditional.checkIfModifiedSince(date, date);
      assert.strictEqual(result, true);
    });

    it('should return false if modified after', () => {
      const ifModified = new Date('2024-01-15');
      const lastModified = new Date('2024-01-16');
      const result = conditional.checkIfModifiedSince(ifModified, lastModified);
      assert.strictEqual(result, false);
    });

    it('should throw error for invalid dates', () => {
      assert.throws(() => conditional.checkIfModifiedSince('invalid', new Date()));
      assert.throws(() => conditional.checkIfModifiedSince(new Date(), 'invalid'));
    });
  });

  describe('checkIfMatch() method', () => {
    it('should return true for matching ETag', () => {
      const result = conditional.checkIfMatch('"abc123"', '"abc123"');
      assert.strictEqual(result, true);
    });

    it('should return true for wildcard', () => {
      const result = conditional.checkIfMatch('*', '"abc123"');
      assert.strictEqual(result, true);
    });

    it('should return false for different ETag', () => {
      const result = conditional.checkIfMatch('"abc123"', '"def456"');
      assert.strictEqual(result, false);
    });
  });

  describe('checkIfUnmodifiedSince() method', () => {
    it('should return true if unmodified', () => {
      const date = new Date('2024-01-15');
      const result = conditional.checkIfUnmodifiedSince(date, date);
      assert.strictEqual(result, true);
    });

    it('should return false if modified', () => {
      const ifUnmodified = new Date('2024-01-15');
      const lastModified = new Date('2024-01-16');
      const result = conditional.checkIfUnmodifiedSince(ifUnmodified, lastModified);
      assert.strictEqual(result, false);
    });
  });

  describe('buildResponse() method', () => {
    it('should build 304 response', () => {
      const response = conditional.buildResponse(
        { ifNoneMatch: true, ifModifiedSince: false },
        '"abc123"',
        'Mon, 15 Jan 2024 00:00:00 GMT'
      );
      
      assert.strictEqual(response.status, 304);
      assert.strictEqual(response.body, null);
      assert.strictEqual(response.headers.etag, '"abc123"');
    });

    it('should build 200 response', () => {
      const response = conditional.buildResponse(
        { ifNoneMatch: false, ifModifiedSince: false },
        '"abc123"',
        'Mon, 15 Jan 2024 00:00:00 GMT'
      );
      
      assert.strictEqual(response.status, 200);
      assert.strictEqual(response.headers.etag, '"abc123"');
    });
  });
});

console.log('\n✓ All feat-caching tests defined\n');
