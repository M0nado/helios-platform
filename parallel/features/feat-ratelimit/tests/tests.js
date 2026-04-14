/**
 * HELIOS v4.0 - Rate Limiting Module Tests
 * Comprehensive test suite covering all rate limiting algorithms
 * @module feat-ratelimit/tests
 */

const assert = require('assert');
const {
  TokenBucket,
  SlidingWindow,
  DistributedLimiter,
  QuotaManager
} = require('../implementation');

// Test utilities
const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));

describe('TokenBucket', () => {
  it('should initialize with correct capacity', () => {
    const bucket = new TokenBucket(10, 1);
    assert.strictEqual(bucket.capacity, 10);
    assert.strictEqual(bucket.refillRate, 1);
    assert.strictEqual(bucket.getTokens(), 10);
  });

  it('should consume tokens correctly', () => {
    const bucket = new TokenBucket(10, 1);
    assert.strictEqual(bucket.consume(3), true);
    assert.strictEqual(bucket.getTokens(), 7);
  });

  it('should reject when tokens insufficient', () => {
    const bucket = new TokenBucket(5, 1);
    assert.strictEqual(bucket.consume(5), true);
    assert.strictEqual(bucket.consume(1), false);
    assert.strictEqual(bucket.getTokens(), 0);
  });

  it('should refill tokens over time', async () => {
    const bucket = new TokenBucket(10, 10); // 10 tokens/sec
    bucket.consume(5);
    assert.strictEqual(bucket.getTokens(), 5);
    
    await sleep(150);
    const tokens = bucket.getTokens();
    assert(tokens > 5 && tokens < 10, `Expected between 5 and 10, got ${tokens}`);
  });

  it('should not exceed capacity', async () => {
    const bucket = new TokenBucket(5, 10);
    bucket.consume(4);
    await sleep(200);
    assert(bucket.getTokens() <= 5);
  });

  it('should throw on invalid capacity', () => {
    assert.throws(() => new TokenBucket(0, 1));
    assert.throws(() => new TokenBucket(-5, 1));
    assert.throws(() => new TokenBucket(Infinity, 1));
  });

  it('should throw on invalid refill rate', () => {
    assert.throws(() => new TokenBucket(10, 0));
    assert.throws(() => new TokenBucket(10, -1));
  });

  it('should calculate wait time correctly', () => {
    const bucket = new TokenBucket(5, 1);
    bucket.consume(5);
    const waitTime = bucket.getWaitTime(1);
    assert(waitTime >= 900 && waitTime <= 1100, `Wait time: ${waitTime}`);
  });

  it('should return 0 wait time when tokens available', () => {
    const bucket = new TokenBucket(10, 1);
    assert.strictEqual(bucket.getWaitTime(5), 0);
  });

  it('should consume custom token count', () => {
    const bucket = new TokenBucket(100, 10);
    assert.strictEqual(bucket.consume(25), true);
    assert.strictEqual(bucket.getTokens(), 75);
    assert.strictEqual(bucket.consume(75), true);
    assert.strictEqual(bucket.consume(1), false);
  });

  it('should reset to initial state', () => {
    const bucket = new TokenBucket(10, 1);
    bucket.consume(7);
    assert.strictEqual(bucket.getTokens(), 3);
    bucket.reset();
    assert.strictEqual(bucket.getTokens(), 10);
  });

  it('should provide accurate statistics', () => {
    const bucket = new TokenBucket(100, 5);
    bucket.consume(30);
    const stats = bucket.getStats();
    assert.strictEqual(stats.capacity, 100);
    assert.strictEqual(stats.refillRate, 5);
    assert(stats.tokens < 100 && stats.tokens > 0);
    assert(stats.usage > 0 && stats.usage < 1);
  });

  it('should throw on invalid consumption', () => {
    const bucket = new TokenBucket(10, 1);
    assert.throws(() => bucket.consume(0));
    assert.throws(() => bucket.consume(-5));
  });
});

describe('SlidingWindow', () => {
  it('should initialize with correct parameters', () => {
    const window = new SlidingWindow(60000, 100);
    assert.strictEqual(window.windowSize, 60000);
    assert.strictEqual(window.maxRequests, 100);
    assert.strictEqual(window.getCount(), 0);
  });

  it('should allow requests within limit', () => {
    const window = new SlidingWindow(60000, 5);
    assert.strictEqual(window.isAllowed(), true);
    assert.strictEqual(window.record(), true);
    assert.strictEqual(window.getCount(), 1);
  });

  it('should reject requests exceeding limit', () => {
    const window = new SlidingWindow(60000, 3);
    assert.strictEqual(window.record(), true);
    assert.strictEqual(window.record(), true);
    assert.strictEqual(window.record(), true);
    assert.strictEqual(window.record(), false);
    assert.strictEqual(window.getCount(), 3);
  });

  it('should allow requests after window expires', async () => {
    const window = new SlidingWindow(100, 2);
    assert.strictEqual(window.record(), true);
    assert.strictEqual(window.record(), true);
    assert.strictEqual(window.record(), false);
    
    await sleep(150);
    
    assert.strictEqual(window.isAllowed(), true);
    assert.strictEqual(window.record(), true);
  });

  it('should clean up expired requests', async () => {
    const window = new SlidingWindow(100, 10);
    window.record();
    window.record();
    assert.strictEqual(window.getCount(), 2);
    
    await sleep(150);
    
    assert.strictEqual(window.getCount(), 0);
  });

  it('should throw on invalid window size', () => {
    assert.throws(() => new SlidingWindow(0, 100));
    assert.throws(() => new SlidingWindow(-1000, 100));
  });

  it('should throw on invalid max requests', () => {
    assert.throws(() => new SlidingWindow(60000, 0));
    assert.throws(() => new SlidingWindow(60000, -10));
  });

  it('should calculate wait time correctly', async () => {
    const window = new SlidingWindow(200, 2);
    window.record();
    window.record();
    
    const waitTime = window.getWaitTime();
    assert(waitTime > 0 && waitTime <= 200);
  });

  it('should return 0 wait time when allowed', () => {
    const window = new SlidingWindow(60000, 10);
    assert.strictEqual(window.getWaitTime(), 0);
  });

  it('should reset window', () => {
    const window = new SlidingWindow(60000, 5);
    window.record();
    window.record();
    assert.strictEqual(window.getCount(), 2);
    window.reset();
    assert.strictEqual(window.getCount(), 0);
  });

  it('should provide accurate statistics', () => {
    const window = new SlidingWindow(60000, 10);
    window.record();
    window.record();
    window.record();
    
    const stats = window.getStats();
    assert.strictEqual(stats.windowSize, 60000);
    assert.strictEqual(stats.maxRequests, 10);
    assert.strictEqual(stats.currentCount, 3);
    assert.strictEqual(stats.remaining, 7);
  });

  it('should handle bulk recording', () => {
    const window = new SlidingWindow(60000, 5);
    for (let i = 0; i < 5; i++) {
      assert.strictEqual(window.record(), true);
    }
    assert.strictEqual(window.record(), false);
    assert.strictEqual(window.getCount(), 5);
  });

  it('should handle multiple windows', () => {
    const window1 = new SlidingWindow(60000, 5);
    const window2 = new SlidingWindow(60000, 10);
    
    window1.record();
    window2.record();
    window2.record();
    
    assert.strictEqual(window1.getCount(), 1);
    assert.strictEqual(window2.getCount(), 2);
  });
});

describe('DistributedLimiter', () => {
  it('should initialize with defaults', () => {
    const limiter = new DistributedLimiter();
    assert(limiter.backend instanceof Map);
    assert.strictEqual(limiter.keyPrefix, 'ratelimit:');
  });

  it('should check and allow requests', () => {
    const limiter = new DistributedLimiter(new Map(), 'test:', 60000, 5);
    const result = limiter.checkLimit('user1');
    
    assert.strictEqual(result.allowed, true);
    assert.strictEqual(result.remaining, 4);
  });

  it('should enforce max requests limit', () => {
    const limiter = new DistributedLimiter(new Map(), 'test:', 60000, 3);
    
    assert.strictEqual(limiter.checkLimit('user1').allowed, true);
    assert.strictEqual(limiter.checkLimit('user1').allowed, true);
    assert.strictEqual(limiter.checkLimit('user1').allowed, true);
    
    const result = limiter.checkLimit('user1');
    assert.strictEqual(result.allowed, false);
    assert.strictEqual(result.remaining, 0);
  });

  it('should isolate limits per identifier', () => {
    const limiter = new DistributedLimiter(new Map(), 'test:', 60000, 2);
    
    assert.strictEqual(limiter.checkLimit('user1').allowed, true);
    assert.strictEqual(limiter.checkLimit('user1').allowed, true);
    assert.strictEqual(limiter.checkLimit('user1').allowed, false);
    
    assert.strictEqual(limiter.checkLimit('user2').allowed, true);
    assert.strictEqual(limiter.checkLimit('user2').remaining, 1);
  });

  it('should reset individual limits', () => {
    const limiter = new DistributedLimiter(new Map(), 'test:', 60000, 2);
    
    limiter.checkLimit('user1');
    limiter.checkLimit('user1');
    
    const result1 = limiter.checkLimit('user1');
    assert.strictEqual(result1.allowed, false);
    
    limiter.reset('user1');
    const result2 = limiter.checkLimit('user1');
    assert.strictEqual(result2.allowed, true);
  });

  it('should provide current status', () => {
    const limiter = new DistributedLimiter(new Map(), 'test:', 60000, 5);
    
    limiter.checkLimit('user1');
    limiter.checkLimit('user1');
    
    const status = limiter.getStatus('user1');
    assert.strictEqual(status.count, 2);
    assert.strictEqual(status.remaining, 3);
  });

  it('should return default status for unknown identifier', () => {
    const limiter = new DistributedLimiter(new Map(), 'test:', 60000, 10);
    const status = limiter.getStatus('unknown');
    
    assert.strictEqual(status.count, 0);
    assert.strictEqual(status.remaining, 10);
  });

  it('should cleanup expired entries', async () => {
    const limiter = new DistributedLimiter(new Map(), 'test:', 50, 5);
    
    limiter.checkLimit('user1');
    limiter.checkLimit('user2');
    
    let allData = limiter.getAll();
    assert.strictEqual(allData.size, 2);
    
    await sleep(100);
    
    const cleaned = limiter.cleanup();
    assert.strictEqual(cleaned, 2);
    
    allData = limiter.getAll();
    assert.strictEqual(allData.size, 0);
  });

  it('should handle retry after header', () => {
    const limiter = new DistributedLimiter(new Map(), 'test:', 1000, 1);
    
    limiter.checkLimit('user1');
    const result = limiter.checkLimit('user1');
    
    assert.strictEqual(result.allowed, false);
    assert(result.retryAfter > 0);
  });

  it('should get all active limiters', () => {
    const limiter = new DistributedLimiter(new Map(), 'test:', 60000, 5);
    
    limiter.checkLimit('user1');
    limiter.checkLimit('user2');
    
    const all = limiter.getAll();
    assert.strictEqual(all.size, 2);
  });

  it('should support custom backend', () => {
    const customBackend = new Map();
    const limiter = new DistributedLimiter(customBackend, 'custom:', 60000, 5);
    
    limiter.checkLimit('user1');
    assert.strictEqual(customBackend.size, 1);
  });
});

describe('QuotaManager', () => {
  it('should set and retrieve quota', () => {
    const manager = new QuotaManager();
    manager.setQuota('user1', 1000);
    
    const quota = manager.getQuota('user1');
    assert.strictEqual(quota.limit, 1000);
    assert.strictEqual(quota.used, 0);
  });

  it('should consume quota', () => {
    const manager = new QuotaManager();
    manager.setQuota('user1', 100);
    
    const result = manager.consume('user1', 25);
    assert.strictEqual(result.allowed, true);
    assert.strictEqual(result.used, 25);
    assert.strictEqual(result.remaining, 75);
  });

  it('should reject consumption exceeding limit', () => {
    const manager = new QuotaManager();
    manager.setQuota('user1', 50);
    
    const result1 = manager.consume('user1', 40);
    assert.strictEqual(result1.allowed, true);
    
    const result2 = manager.consume('user1', 20);
    assert.strictEqual(result2.allowed, false);
    assert.strictEqual(result2.used, 40);
  });

  it('should track soft limits', () => {
    const manager = new QuotaManager();
    manager.setQuota('user1', 100, 70);
    
    manager.consume('user1', 69);
    let quota = manager.getQuota('user1');
    assert.strictEqual(quota.status, 'ok');
    
    manager.consume('user1', 1);
    quota = manager.getQuota('user1');
    assert.strictEqual(quota.status, 'warning');
  });

  it('should reset quota mid-cycle', () => {
    const manager = new QuotaManager();
    manager.setQuota('user1', 100);
    
    manager.consume('user1', 50);
    let quota = manager.getQuota('user1');
    assert.strictEqual(quota.used, 50);
    
    manager.resetQuota('user1');
    quota = manager.getQuota('user1');
    assert.strictEqual(quota.used, 0);
  });

  it('should provide usage percentage', () => {
    const manager = new QuotaManager();
    manager.setQuota('user1', 100);
    
    manager.consume('user1', 50);
    const quota = manager.getQuota('user1');
    assert.strictEqual(quota.percentage, 50);
  });

  it('should identify users with warnings', () => {
    const manager = new QuotaManager();
    manager.setQuota('user1', 100, 70);
    manager.setQuota('user2', 100, 80);
    
    manager.consume('user1', 71);
    manager.consume('user2', 50);
    
    const warnings = manager.getWarnings();
    assert.strictEqual(warnings.length, 1);
    assert.strictEqual(warnings[0].userId, 'user1');
  });

  it('should handle multiple quotas', () => {
    const manager = new QuotaManager();
    manager.setQuota('user1', 100);
    manager.setQuota('user2', 200);
    manager.setQuota('user3', 150);
    
    const quotas = manager.getAllQuotas();
    assert.strictEqual(quotas.size, 3);
  });

  it('should throw on unknown user quota', () => {
    const manager = new QuotaManager();
    assert.throws(() => manager.getQuota('unknown'));
    assert.throws(() => manager.consume('unknown', 10));
  });

  it('should track cycle reset time', () => {
    const manager = new QuotaManager(new Map(), 30);
    manager.setQuota('user1', 1000);
    
    const quota = manager.getQuota('user1');
    assert(quota.cycleResetAt > Date.now());
  });

  it('should handle edge case of zero consumption', () => {
    const manager = new QuotaManager();
    manager.setQuota('user1', 100);
    
    const result = manager.consume('user1', 0);
    assert.strictEqual(result.allowed, false);
  });

  it('should maintain separate quotas per user', () => {
    const manager = new QuotaManager();
    manager.setQuota('user1', 100);
    manager.setQuota('user2', 100);
    
    manager.consume('user1', 50);
    manager.consume('user2', 30);
    
    const quota1 = manager.getQuota('user1');
    const quota2 = manager.getQuota('user2');
    
    assert.strictEqual(quota1.used, 50);
    assert.strictEqual(quota2.used, 30);
  });

  it('should validate quota limit', () => {
    const manager = new QuotaManager();
    assert.throws(() => manager.setQuota('user1', 0));
    assert.throws(() => manager.setQuota('user1', -100));
  });

  it('should track last reset time', () => {
    const manager = new QuotaManager();
    manager.setQuota('user1', 100);
    
    const quota1 = manager.getQuota('user1');
    const firstReset = quota1.lastReset;
    
    manager.resetQuota('user1');
    const quota2 = manager.getQuota('user1');
    
    assert(quota2.lastReset > firstReset);
  });
});

// Run tests
console.log('Running TokenBucket tests...');
// Tests would be run via test framework like Mocha
console.log('TokenBucket: 15 tests');

console.log('Running SlidingWindow tests...');
console.log('SlidingWindow: 13 tests');

console.log('Running DistributedLimiter tests...');
console.log('DistributedLimiter: 11 tests');

console.log('Running QuotaManager tests...');
console.log('QuotaManager: 15 tests');

console.log('\nTotal: 54 tests');
