/**
 * HELIOS v4.0 Rate Limiter - Test Suite
 * 45+ comprehensive tests covering all rate limiter functionality
 */

const assert = require('assert');
const {
  DistributedLimiter,
  QuotaEnforcer,
  MetricsTracker,
  MemoryBackend,
  RateLimitError,
  QuotaExceededError,
  QuotaError
} = require('../implementation');

// ============================================================================
// DistributedLimiter Tests (14 tests)
// ============================================================================
describe('DistributedLimiter', () => {
  it('should create a limiter with default options', () => {
    const limiter = new DistributedLimiter();
    assert.strictEqual(limiter.maxRequests, 100);
    assert.strictEqual(limiter.windowMs, 60000);
  });

  it('should check if request is allowed', async () => {
    const limiter = new DistributedLimiter({ maxRequests: 2 });
    const req = { ip: '192.168.1.1' };

    const status1 = await limiter.check(req);
    assert.strictEqual(status1.allowed, true);
    assert.strictEqual(status1.remaining, 1);

    const status2 = await limiter.check(req);
    assert.strictEqual(status2.allowed, true);
    assert.strictEqual(status2.remaining, 0);

    const status3 = await limiter.check(req);
    assert.strictEqual(status3.allowed, false);
  });

  it('should track remaining requests', async () => {
    const limiter = new DistributedLimiter({ maxRequests: 5 });
    const req = { ip: '192.168.1.2' };

    for (let i = 0; i < 5; i++) {
      const status = await limiter.check(req);
      assert.strictEqual(status.remaining, 5 - i - 1);
    }
  });

  it('should reset after time window expires', async () => {
    const limiter = new DistributedLimiter({ maxRequests: 1, windowMs: 100 });
    const req = { ip: '192.168.1.3' };

    const status1 = await limiter.check(req);
    assert.strictEqual(status1.allowed, true);

    const status2 = await limiter.check(req);
    assert.strictEqual(status2.allowed, false);

    await new Promise(resolve => setTimeout(resolve, 150));

    const status3 = await limiter.check(req);
    assert.strictEqual(status3.allowed, true);
  });

  it('should support custom identifier function', async () => {
    const limiter = new DistributedLimiter({
      maxRequests: 2,
      identifierFn: (req) => req.userId
    });

    const req1 = { userId: 'user-1' };
    const req2 = { userId: 'user-2' };

    const status1 = await limiter.check(req1);
    const status2 = await limiter.check(req2);

    assert.strictEqual(status1.allowed, true);
    assert.strictEqual(status2.allowed, true);
  });

  it('should get status without incrementing', async () => {
    const limiter = new DistributedLimiter({ maxRequests: 3 });
    const req = { ip: '192.168.1.4' };

    await limiter.check(req);
    const status = await limiter.status(req);

    assert.strictEqual(status.current, 1);

    const statusAgain = await limiter.status(req);
    assert.strictEqual(statusAgain.current, 1);
  });

  it('should reset individual identifiers', async () => {
    const limiter = new DistributedLimiter({ maxRequests: 2 });
    const req = { ip: '192.168.1.5' };

    await limiter.check(req);
    await limiter.check(req);

    let status = await limiter.check(req);
    assert.strictEqual(status.allowed, false);

    await limiter.reset('192.168.1.5');

    status = await limiter.status(req);
    assert.strictEqual(status.remaining, 2);
  });

  it('should reset all identifiers', async () => {
    const limiter = new DistributedLimiter({ maxRequests: 1 });

    await limiter.check({ ip: '192.168.1.6' });
    await limiter.check({ ip: '192.168.1.7' });

    await limiter.resetAll();

    const status1 = await limiter.status({ ip: '192.168.1.6' });
    const status2 = await limiter.status({ ip: '192.168.1.7' });

    assert.strictEqual(status1.remaining, 1);
    assert.strictEqual(status2.remaining, 1);
  });

  it('should provide rate limit statistics', async () => {
    const limiter = new DistributedLimiter();
    await limiter.check({ ip: '192.168.1.8' });
    await limiter.check({ ip: '192.168.1.9' });

    const stats = await limiter.stats();

    assert.strictEqual(stats.maxRequests, 100);
    assert.strictEqual(stats.activeIdentifiers, 2);
  });

  it('should validate requests parameter', async () => {
    const limiter = new DistributedLimiter();
    assert.rejects(() => limiter.check(null));
  });

  it('should validate identifier function result', async () => {
    const limiter = new DistributedLimiter({
      identifierFn: () => null
    });
    assert.rejects(() => limiter.check({ ip: '192.168.1.10' }));
  });

  it('should set custom identifier function', async () => {
    const limiter = new DistributedLimiter();
    limiter.setIdentifierFn((req) => req.userId);

    const req = { userId: 'user-custom' };
    const status = await limiter.check(req);

    assert.strictEqual(status.identifier, 'user-custom');
  });

  it('should update configuration', async () => {
    const limiter = new DistributedLimiter();
    limiter.updateConfig({ maxRequests: 50, windowMs: 30000 });

    assert.strictEqual(limiter.maxRequests, 50);
    assert.strictEqual(limiter.windowMs, 30000);
  });
});

// ============================================================================
// QuotaEnforcer Tests (12 tests)
// ============================================================================
describe('QuotaEnforcer', () => {
  it('should define quotas', () => {
    const enforcer = new QuotaEnforcer();
    enforcer.define('api_calls', { limit: 1000, resetMs: 86400000 });

    const quotas = enforcer.getQuotas();
    assert.strictEqual(quotas.length, 1);
    assert.strictEqual(quotas[0].name, 'api_calls');
  });

  it('should enforce quotas', async () => {
    const enforcer = new QuotaEnforcer({ strict: false });
    enforcer.define('bandwidth', { limit: 1000 });

    const status = await enforcer.enforce('bandwidth', { user: 'user-1' }, 100);

    assert.strictEqual(status.allowed, true);
    assert.strictEqual(status.used, 100);
    assert.strictEqual(status.remaining, 900);
  });

  it('should track quota usage', async () => {
    const enforcer = new QuotaEnforcer({ strict: false });
    enforcer.define('requests', { limit: 500 });

    await enforcer.enforce('requests', { userId: 'u1' }, 100);
    await enforcer.enforce('requests', { userId: 'u1' }, 100);

    const usage = await enforcer.getUsage('requests', { userId: 'u1' });

    assert.strictEqual(usage.used, 200);
    assert.strictEqual(usage.remaining, 300);
  });

  it('should detect quota exceeded', async () => {
    const enforcer = new QuotaEnforcer({ strict: false });
    enforcer.define('small', { limit: 100 });

    const status = await enforcer.enforce('small', { user: 'u1' }, 150);

    assert.strictEqual(status.exceeded, true);
  });

  it('should throw on quota exceeded in strict mode', async () => {
    const enforcer = new QuotaEnforcer({ strict: true });
    enforcer.define('strict_quota', { limit: 50 });

    try {
      await enforcer.enforce('strict_quota', { user: 'u1' }, 100);
      assert.fail('Should have thrown');
    } catch (error) {
      assert.strictEqual(error.name, 'QuotaExceededError');
    }
  });

  it('should calculate warning threshold', async () => {
    const enforcer = new QuotaEnforcer({ strict: false });
    enforcer.define('warn_test', { limit: 100, warnThreshold: 80 });

    const status1 = await enforcer.enforce('warn_test', { user: 'u1' }, 75);
    assert.strictEqual(status1.warned, false);

    const status2 = await enforcer.enforce('warn_test', { user: 'u1' }, 10);
    assert.strictEqual(status2.warned, true);
  });

  it('should support multiple quota dimensions', async () => {
    const enforcer = new QuotaEnforcer();
    enforcer.define('multi', {
      limit: 1000,
      dimensions: ['userId', 'month']
    });

    const status1 = await enforcer.enforce('multi',
      { userId: 'u1', month: 'jan' }, 100
    );
    const status2 = await enforcer.enforce('multi',
      { userId: 'u1', month: 'feb' }, 100
    );

    assert.strictEqual(status1.used, 100);
    assert.strictEqual(status2.used, 100);
  });

  it('should reset quota usage', async () => {
    const enforcer = new QuotaEnforcer({ strict: false });
    enforcer.define('reset_test', { limit: 100 });

    await enforcer.enforce('reset_test', { user: 'u1' }, 50);
    await enforcer.resetUsage('reset_test', { user: 'u1' });

    const usage = await enforcer.getUsage('reset_test', { user: 'u1' });
    assert.strictEqual(usage.used, 0);
  });

  it('should call onQuotaExceeded callback', async () => {
    let callbackCalled = false;
    const enforcer = new QuotaEnforcer({
      strict: false,
      onQuotaExceeded: () => {
        callbackCalled = true;
      }
    });

    enforcer.define('callback_test', { limit: 10 });
    await enforcer.enforce('callback_test', { user: 'u1' }, 20);

    assert.strictEqual(callbackCalled, true);
  });

  it('should reject invalid quota names', async () => {
    const enforcer = new QuotaEnforcer();
    assert.rejects(() => enforcer.enforce('unknown_quota', {}));
  });

  it('should reject invalid amounts', async () => {
    const enforcer = new QuotaEnforcer();
    enforcer.define('amount_test', { limit: 100 });

    try {
      await enforcer.enforce('amount_test', { user: 'u1' }, -1);
      assert.fail('Should have thrown');
    } catch (error) {
      assert.strictEqual(error.message.includes('positive'), true);
    }
  });
});

// ============================================================================
// MetricsTracker Tests (11 tests)
// ============================================================================
describe('MetricsTracker', () => {
  it('should create tracker with default options', () => {
    const tracker = new MetricsTracker();
    assert.strictEqual(tracker.enabled, true);
    assert.strictEqual(tracker.historySize, 1000);
  });

  it('should record rate limit events', () => {
    const tracker = new MetricsTracker();
    tracker.record({
      type: 'allowed',
      context: { ip: '192.168.1.1' }
    });

    const metrics = tracker.getMetrics({ ip: '192.168.1.1' });
    assert.strictEqual(metrics.allowed, 1);
  });

  it('should track multiple event types', () => {
    const tracker = new MetricsTracker();
    tracker.record({ type: 'allowed', context: { ip: 'a' } });
    tracker.record({ type: 'blocked', context: { ip: 'a' } });
    tracker.record({ type: 'quota_exceeded', context: { ip: 'a' } });

    const metrics = tracker.getMetrics({ ip: 'a' });
    assert.strictEqual(metrics.allowed, 1);
    assert.strictEqual(metrics.blocked, 1);
    assert.strictEqual(metrics.quotaExceeded, 1);
  });

  it('should get all metrics', () => {
    const tracker = new MetricsTracker();
    tracker.record({ type: 'allowed', context: { ip: 'a' } });
    tracker.record({ type: 'allowed', context: { ip: 'b' } });

    const all = tracker.getAllMetrics();
    assert.strictEqual(all.length, 2);
  });

  it('should export Prometheus format', () => {
    const tracker = new MetricsTracker();
    tracker.record({ type: 'allowed', context: { ip: '192.168.1.1' } });

    const prometheus = tracker.exportPrometheus();
    assert.strictEqual(prometheus.includes('rate_limiter_allowed'), true);
  });

  it('should export JSON format', () => {
    const tracker = new MetricsTracker();
    tracker.record({ type: 'blocked', context: { ip: '192.168.1.1' } });

    const json = tracker.exportJSON();
    assert.strictEqual(json.timestamp !== undefined, true);
    assert.strictEqual(json.metrics !== undefined, true);
  });

  it('should get metrics summary', () => {
    const tracker = new MetricsTracker();
    tracker.record({ type: 'allowed', context: { ip: 'a' } });
    tracker.record({ type: 'blocked', context: { ip: 'a' } });

    const summary = tracker.getSummary();
    assert.strictEqual(summary.totalAllowed, 1);
    assert.strictEqual(summary.totalBlocked, 1);
  });

  it('should maintain history limit', () => {
    const tracker = new MetricsTracker({ historySize: 5 });

    for (let i = 0; i < 10; i++) {
      tracker.record({ type: 'allowed', context: { ip: 'a' } });
    }

    assert.strictEqual(tracker.history.length, 5);
  });

  it('should clear metrics', () => {
    const tracker = new MetricsTracker();
    tracker.record({ type: 'allowed', context: { ip: 'a' } });

    tracker.clear();

    assert.strictEqual(tracker.metrics.size, 0);
    assert.strictEqual(tracker.history.length, 0);
  });

  it('should skip recording when disabled', () => {
    const tracker = new MetricsTracker({ enabled: false });
    tracker.record({ type: 'allowed', context: { ip: 'a' } });

    const metrics = tracker.getMetrics({ ip: 'a' });
    assert.strictEqual(metrics, null);
  });

  it('should handle invalid events', () => {
    const tracker = new MetricsTracker();
    assert.throws(() => tracker.record({ context: {} })); // No type
  });
});

// ============================================================================
// MemoryBackend Tests (6 tests)
// ============================================================================
describe('MemoryBackend', () => {
  it('should store and retrieve data', async () => {
    const backend = new MemoryBackend();
    await backend.set('key1', { value: 'data' });

    const data = await backend.get('key1');
    assert.strictEqual(data.value, 'data');
  });

  it('should return null for non-existent keys', async () => {
    const backend = new MemoryBackend();
    const data = await backend.get('nonexistent');
    assert.strictEqual(data, undefined);
  });

  it('should delete data', async () => {
    const backend = new MemoryBackend();
    await backend.set('key1', { value: 'data' });
    await backend.delete('key1');

    const data = await backend.get('key1');
    assert.strictEqual(data, undefined);
  });

  it('should clear all data', async () => {
    const backend = new MemoryBackend();
    await backend.set('key1', { value: 'a' });
    await backend.set('key2', { value: 'b' });

    await backend.clear();

    const count = await backend.count();
    assert.strictEqual(count, 0);
  });

  it('should count stored items', async () => {
    const backend = new MemoryBackend();
    await backend.set('key1', {});
    await backend.set('key2', {});
    await backend.set('key3', {});

    const count = await backend.count();
    assert.strictEqual(count, 3);
  });

  it('should support TTL', async () => {
    const backend = new MemoryBackend();
    await backend.set('ttl_key', { value: 'data' }, 100);

    let data = await backend.get('ttl_key');
    assert.notStrictEqual(data, undefined);

    await new Promise(resolve => setTimeout(resolve, 150));

    data = await backend.get('ttl_key');
    assert.strictEqual(data, undefined);
  });
});

// ============================================================================
// Integration Tests (10 tests)
// ============================================================================
describe('Integration', () => {
  it('should combine limiter and quota', async () => {
    const limiter = new DistributedLimiter({ maxRequests: 10 });
    const enforcer = new QuotaEnforcer({ strict: false });

    enforcer.define('daily', { limit: 1000 });

    const req = { ip: '192.168.1.1' };

    const limitStatus = await limiter.check(req);
    const quotaStatus = await enforcer.enforce('daily', { ip: '192.168.1.1' });

    assert.strictEqual(limitStatus.allowed, true);
    assert.strictEqual(quotaStatus.allowed, true);
  });

  it('should track combined metrics', async () => {
    const limiter = new DistributedLimiter();
    const metrics = new MetricsTracker();

    const req = { ip: 'a' };
    const status = await limiter.check(req);

    metrics.record({
      type: status.allowed ? 'allowed' : 'blocked',
      context: { ip: 'a' }
    });

    const summary = metrics.getSummary();
    assert.strictEqual(summary.totalAllowed, 1);
  });

  it('should handle rate limit with quota enforcement', async () => {
    const limiter = new DistributedLimiter({ maxRequests: 5 });
    const enforcer = new QuotaEnforcer({ strict: false });

    enforcer.define('api', { limit: 1000 });

    const req = { ip: 'b' };

    for (let i = 0; i < 3; i++) {
      const limitStatus = await limiter.check(req);
      const quotaStatus = await enforcer.enforce('api', { ip: 'b' }, 100);

      assert.strictEqual(limitStatus.allowed, true);
      assert.strictEqual(quotaStatus.allowed, true);
    }
  });

  it('should reset both limiter and enforcer', async () => {
    const limiter = new DistributedLimiter({ maxRequests: 2 });
    const enforcer = new QuotaEnforcer();

    enforcer.define('reset', { limit: 100 });

    const req = { ip: 'c' };
    await limiter.check(req);
    await limiter.check(req);

    await enforcer.enforce('reset', { ip: 'c' }, 100);

    await limiter.reset('c');
    await enforcer.resetUsage('reset', { ip: 'c' });

    const limitStatus = await limiter.status(req);
    const usage = await enforcer.getUsage('reset', { ip: 'c' });

    assert.strictEqual(limitStatus.remaining, 2);
    assert.strictEqual(usage.used, 0);
  });

  it('should handle parallel requests', async () => {
    const limiter = new DistributedLimiter({ maxRequests: 100 });

    const promises = [];
    for (let i = 0; i < 10; i++) {
      promises.push(limiter.check({ ip: 'd' }));
    }

    const results = await Promise.all(promises);
    assert.strictEqual(results.length, 10);
  });

  it('should manage different identifiers independently', async () => {
    const limiter = new DistributedLimiter({ maxRequests: 5 });

    const status1 = await limiter.check({ ip: 'ip1' });
    const status2 = await limiter.check({ ip: 'ip2' });

    assert.strictEqual(status1.remaining, 4);
    assert.strictEqual(status2.remaining, 4);
  });

  it('should provide accurate statistics', async () => {
    const limiter = new DistributedLimiter({ maxRequests: 1000 });

    await limiter.check({ ip: 'stat1' });
    await limiter.check({ ip: 'stat2' });
    await limiter.check({ ip: 'stat1' });

    const stats = await limiter.stats();

    assert.strictEqual(stats.activeIdentifiers, 2);
    assert.strictEqual(stats.maxRequests, 1000);
  });

  it('should track metrics across events', async () => {
    const tracker = new MetricsTracker();

    tracker.record({ type: 'allowed', context: { ip: 'e' } });
    tracker.record({ type: 'allowed', context: { ip: 'e' } });
    tracker.record({ type: 'blocked', context: { ip: 'e' } });

    const summary = tracker.getSummary();

    assert.strictEqual(summary.totalAllowed, 2);
    assert.strictEqual(summary.totalBlocked, 1);
    assert.strictEqual(summary.totalRequests, 3);
  });

  it('should support distributed limiter with custom backend', async () => {
    const backend = new MemoryBackend();
    const limiter = new DistributedLimiter({
      maxRequests: 10,
      distributed: true,
      backend
    });

    const req = { ip: 'distributed' };
    const status = await limiter.check(req);

    assert.strictEqual(status.allowed, true);
  });
});

// ============================================================================
// Error Handling Tests (5 tests)
// ============================================================================
describe('Error Handling', () => {
  it('should throw RateLimitError on backend failure', async () => {
    const failingBackend = {
      get: async () => { throw new Error('Backend error'); },
      set: async () => { throw new Error('Backend error'); }
    };

    const limiter = new DistributedLimiter({
      distributed: true,
      backend: failingBackend
    });

    try {
      await limiter.check({ ip: 'test' });
      assert.fail('Should have thrown');
    } catch (error) {
      assert.strictEqual(error.name, 'RateLimitError');
    }
  });

  it('should throw QuotaError on enforcement failure', async () => {
    const enforcer = new QuotaEnforcer();
    enforcer.define('test', { limit: 100 });

    try {
      await enforcer.enforce('test', null);
      assert.fail('Should have thrown');
    } catch (error) {
      assert.strictEqual(error.message.includes('Context'), true);
    }
  });

  it('should throw on invalid quota definition', () => {
    const enforcer = new QuotaEnforcer();

    assert.throws(() => enforcer.define('', { limit: 100 }));
    assert.throws(() => enforcer.define('test', { limit: -1 }));
  });

  it('should handle quota exceeded gracefully', async () => {
    const enforcer = new QuotaEnforcer({ strict: false });
    enforcer.define('test', { limit: 10 });

    const status = await enforcer.enforce('test', { user: 'u1' }, 20);

    assert.strictEqual(status.exceeded, true);
    assert.strictEqual(status.used, 20);
  });

  it('should throw on quota exceeded in strict mode', async () => {
    const enforcer = new QuotaEnforcer({ strict: true });
    enforcer.define('test', { limit: 10 });

    try {
      await enforcer.enforce('test', { user: 'u1' }, 20);
      assert.fail('Should have thrown');
    } catch (error) {
      assert.strictEqual(error.name, 'QuotaExceededError');
    }
  });
});

console.log('All tests defined');
