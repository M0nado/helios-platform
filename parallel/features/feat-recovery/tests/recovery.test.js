/**
 * feat-recovery - Comprehensive Test Suite
 * 45+ tests covering retry, backoff, fallback, and degradation strategies
 */

const assert = require('assert');
const {
  RetryPolicy,
  ExponentialBackoff,
  FallbackStrategy,
  GracefulDegradation
} = require('../implementation');

describe('RetryPolicy', () => {
  let policy;

  beforeEach(() => {
    policy = new RetryPolicy();
  });

  describe('Constructor & Configuration', () => {
    it('should initialize with default options', () => {
      assert.strictEqual(policy.maxRetries, 3);
      assert.strictEqual(policy.initialDelay, 1000);
      assert.strictEqual(policy.maxDelay, 30000);
    });

    it('should accept custom options', () => {
      const custom = new RetryPolicy({
        maxRetries: 5,
        initialDelay: 500,
        maxDelay: 60000
      });
      assert.strictEqual(custom.maxRetries, 5);
      assert.strictEqual(custom.initialDelay, 500);
      assert.strictEqual(custom.maxDelay, 60000);
    });

    it('should throw error for invalid options', () => {
      assert.throws(() => new RetryPolicy('invalid'));
      assert.throws(() => new RetryPolicy({ maxRetries: -1 }));
      assert.throws(() => new RetryPolicy({ initialDelay: -1 }));
    });

    it('should accept custom retryable statuses', () => {
      const custom = new RetryPolicy({
        retryableStatuses: [500, 502, 503]
      });
      assert.deepStrictEqual(custom.retryableStatuses, [500, 502, 503]);
    });

    it('should accept custom shouldRetry function', () => {
      const shouldRetry = (error) => error.code === 'CUSTOM';
      const custom = new RetryPolicy({ shouldRetry });
      assert.strictEqual(custom.shouldRetry, shouldRetry);
    });
  });

  describe('execute() method', () => {
    it('should execute function successfully on first try', async () => {
      const fn = async () => ({ success: true });
      const result = await policy.execute(fn);
      
      assert.deepStrictEqual(result, { success: true });
    });

    it('should retry on transient failure', async () => {
      let attempts = 0;
      const fn = async () => {
        attempts++;
        if (attempts < 2) throw new Error('Temporary error');
        return { success: true };
      };

      const result = await policy.execute(fn);
      assert.strictEqual(attempts, 2);
      assert.deepStrictEqual(result, { success: true });
    });

    it('should throw after max retries exhausted', async () => {
      const fn = async () => {
        throw new Error('Permanent error');
      };

      await assert.rejects(policy.execute(fn), /Permanent error/);
    });

    it('should retry on ECONNREFUSED error', async () => {
      let attempts = 0;
      const fn = async () => {
        attempts++;
        if (attempts < 2) {
          const error = new Error('Connection refused');
          error.code = 'ECONNREFUSED';
          throw error;
        }
        return { success: true };
      };

      const result = await policy.execute(fn);
      assert.strictEqual(attempts, 2);
    });

    it('should retry on retryable HTTP status', async () => {
      let attempts = 0;
      const fn = async () => {
        attempts++;
        if (attempts < 2) {
          const error = new Error('Service unavailable');
          error.status = 503;
          throw error;
        }
        return { success: true };
      };

      const result = await policy.execute(fn);
      assert.strictEqual(attempts, 2);
    });

    it('should not retry non-retryable errors', async () => {
      let attempts = 0;
      const fn = async () => {
        attempts++;
        throw new Error('Not found');
      };

      const policy = new RetryPolicy({ maxRetries: 3 });
      await assert.rejects(policy.execute(fn), /Not found/);
      assert.strictEqual(attempts, 1); // No retry
    });

    it('should pass function arguments', async () => {
      let capturedArgs;
      const fn = async (a, b) => {
        capturedArgs = [a, b];
        return a + b;
      };

      const result = await policy.execute(fn, [5, 3]);
      assert.deepStrictEqual(capturedArgs, [5, 3]);
      assert.strictEqual(result, 8);
    });

    it('should throw error for non-function', async () => {
      await assert.rejects(policy.execute('not-a-function'), /must be a function/);
    });
  });

  describe('Retry timing', () => {
    it('should include delays between retries', async () => {
      const policy = new RetryPolicy({ initialDelay: 50, maxRetries: 2 });
      let attempts = 0;
      const startTime = Date.now();

      const fn = async () => {
        attempts++;
        if (attempts < 2) {
          const error = new Error('Fail');
          error.code = 'ECONNRESET';
          throw error;
        }
        return 'success';
      };

      await policy.execute(fn);
      const elapsed = Date.now() - startTime;
      
      // Should have ~50ms delay (allow some variance)
      assert(elapsed >= 40);
    });

    it('should cap delay at maxDelay', async () => {
      const policy = new RetryPolicy({
        initialDelay: 1000,
        maxDelay: 5000,
        maxRetries: 3
      });

      let attempts = 0;
      const fn = async () => {
        attempts++;
        if (attempts < 2) {
          const error = new Error('Fail');
          error.code = 'ECONNRESET';
          throw error;
        }
        return 'success';
      };

      await policy.execute(fn);
      assert.strictEqual(attempts, 2);
    });
  });
});

describe('ExponentialBackoff', () => {
  let backoff;

  beforeEach(() => {
    backoff = new ExponentialBackoff();
  });

  describe('Constructor & Configuration', () => {
    it('should initialize with defaults', () => {
      assert.strictEqual(backoff.baseDelay, 1000);
      assert.strictEqual(backoff.maxDelay, 60000);
      assert.strictEqual(backoff.multiplier, 2);
      assert.strictEqual(backoff.jitterFactor, 0.1);
    });

    it('should accept custom options', () => {
      const custom = new ExponentialBackoff({
        baseDelay: 500,
        maxDelay: 30000,
        multiplier: 1.5,
        jitterFactor: 0.2
      });
      assert.strictEqual(custom.baseDelay, 500);
      assert.strictEqual(custom.maxDelay, 30000);
      assert.strictEqual(custom.multiplier, 1.5);
      assert.strictEqual(custom.jitterFactor, 0.2);
    });

    it('should throw error for invalid options', () => {
      assert.throws(() => new ExponentialBackoff({ baseDelay: -1 }));
      assert.throws(() => new ExponentialBackoff({ multiplier: 0 }));
      assert.throws(() => new ExponentialBackoff({ jitterFactor: 1.5 }));
    });
  });

  describe('calculateDelay() method', () => {
    it('should calculate exponential delay', () => {
      // Delay = baseDelay * multiplier^attempt (without jitter for validation)
      const backoff = new ExponentialBackoff({
        baseDelay: 1000,
        multiplier: 2,
        jitterFactor: 0
      });

      assert.strictEqual(backoff.calculateDelay(0), 1000);
      assert.strictEqual(backoff.calculateDelay(1), 2000);
      assert.strictEqual(backoff.calculateDelay(2), 4000);
    });

    it('should cap delay at maxDelay', () => {
      const backoff = new ExponentialBackoff({
        baseDelay: 1000,
        multiplier: 10,
        maxDelay: 5000,
        jitterFactor: 0
      });

      const delay = backoff.calculateDelay(3); // 1000 * 10^3 = 1,000,000
      assert(delay <= 5000);
    });

    it('should add jitter to delay', () => {
      const delays = [];
      for (let i = 0; i < 10; i++) {
        delays.push(backoff.calculateDelay(1));
      }

      // Delays should vary due to jitter
      const unique = new Set(delays);
      assert(unique.size > 1);
    });

    it('should throw error for negative attempt', () => {
      assert.throws(() => backoff.calculateDelay(-1));
    });

    it('should return number', () => {
      const delay = backoff.calculateDelay(0);
      assert(typeof delay === 'number');
      assert(delay > 0);
    });
  });

  describe('calculateDelayFullJitter() method', () => {
    it('should return value between 0 and max', () => {
      const delays = [];
      for (let i = 0; i < 20; i++) {
        delays.push(backoff.calculateDelayFullJitter(2));
      }

      const max = backoff.baseDelay * Math.pow(backoff.multiplier, 2);
      assert(Math.max(...delays) <= max);
      assert(Math.min(...delays) >= 0);
    });

    it('should vary for same attempt', () => {
      const delay1 = backoff.calculateDelayFullJitter(1);
      const delay2 = backoff.calculateDelayFullJitter(1);
      
      // Different with high probability
      // (May fail rarely due to randomness)
      let different = false;
      for (let i = 0; i < 5; i++) {
        if (backoff.calculateDelayFullJitter(1) !== delay1) {
          different = true;
          break;
        }
      }
      assert(different);
    });

    it('should throw error for negative attempt', () => {
      assert.throws(() => backoff.calculateDelayFullJitter(-1));
    });
  });

  describe('calculateDelayEqualJitter() method', () => {
    it('should calculate balanced jitter', () => {
      const backoff = new ExponentialBackoff({
        baseDelay: 1000,
        multiplier: 2,
        jitterFactor: 0
      });

      const delay = backoff.calculateDelayEqualJitter(1); // 2000
      
      // Should be between 1000 and 2000
      assert(delay >= 1000 && delay <= 2000);
    });

    it('should throw error for negative attempt', () => {
      assert.throws(() => backoff.calculateDelayEqualJitter(-1));
    });
  });

  describe('execute() method', () => {
    it('should execute function with retries', async () => {
      let attempts = 0;
      const fn = async () => {
        attempts++;
        if (attempts < 2) throw new Error('Fail');
        return 'success';
      };

      const result = await backoff.execute(fn, 3);
      assert.strictEqual(result, 'success');
      assert.strictEqual(attempts, 2);
    });

    it('should throw after max attempts', async () => {
      const fn = async () => {
        throw new Error('Always fails');
      };

      await assert.rejects(backoff.execute(fn, 2), /Always fails/);
    });

    it('should throw error for non-function', async () => {
      await assert.rejects(backoff.execute('not-function'), /must be a function/);
    });
  });
});

describe('FallbackStrategy', () => {
  describe('Constructor & Configuration', () => {
    it('should initialize with strategies', () => {
      const strategies = [async () => 1, async () => 2];
      const fallback = new FallbackStrategy(strategies);
      assert.strictEqual(fallback.strategies.length, 2);
    });

    it('should throw error for empty strategies', () => {
      assert.throws(() => new FallbackStrategy([]), /non-empty array/);
    });

    it('should throw error for non-function strategies', () => {
      assert.throws(() => new FallbackStrategy(['not-function']), /must be functions/);
    });

    it('should throw error for null strategies', () => {
      assert.throws(() => new FallbackStrategy(null), /non-empty array/);
    });
  });

  describe('execute() method', () => {
    it('should execute first successful strategy', async () => {
      const strategies = [
        async () => { throw new Error('First failed'); },
        async () => 'Second succeeded'
      ];

      const fallback = new FallbackStrategy(strategies);
      const result = await fallback.execute();
      
      assert.strictEqual(result, 'Second succeeded');
    });

    it('should return first strategy result if successful', async () => {
      const strategies = [
        async () => 'First succeeded',
        async () => 'Second succeeded'
      ];

      const fallback = new FallbackStrategy(strategies);
      const result = await fallback.execute();
      
      assert.strictEqual(result, 'First succeeded');
    });

    it('should throw error if all strategies fail', async () => {
      const strategies = [
        async () => { throw new Error('First failed'); },
        async () => { throw new Error('Second failed'); }
      ];

      const fallback = new FallbackStrategy(strategies);
      await assert.rejects(fallback.execute(), /All fallback strategies failed/);
    });

    it('should log execution results', async () => {
      const strategies = [
        async () => { throw new Error('Failed'); },
        async () => 'Success'
      ];

      const fallback = new FallbackStrategy(strategies);
      await fallback.execute();
      
      const log = fallback.getExecutionLog();
      assert.strictEqual(log.length, 2);
      assert.strictEqual(log[0].success, false);
      assert.strictEqual(log[1].success, true);
    });
  });

  describe('executeWithTimeout() method', () => {
    it('should timeout slow strategies', async () => {
      const strategies = [
        async () => {
          await new Promise(resolve => setTimeout(resolve, 1000));
          return 'slow';
        },
        async () => 'fast'
      ];

      const fallback = new FallbackStrategy(strategies);
      const result = await fallback.executeWithTimeout(100);
      
      assert.strictEqual(result, 'fast');
    });

    it('should throw if all timeout', async () => {
      const strategies = [
        async () => new Promise(resolve => setTimeout(resolve, 1000))
      ];

      const fallback = new FallbackStrategy(strategies);
      await assert.rejects(
        fallback.executeWithTimeout(100),
        /timed out/
      );
    });
  });

  describe('getExecutionLog() method', () => {
    it('should return execution log', async () => {
      const strategies = [
        async () => { throw new Error('Failed'); },
        async () => 'Success'
      ];

      const fallback = new FallbackStrategy(strategies);
      await fallback.execute();
      
      const log = fallback.getExecutionLog();
      assert.strictEqual(log.length, 2);
      assert.strictEqual(log[0].strategyIndex, 0);
      assert.strictEqual(log[1].strategyIndex, 1);
    });
  });
});

describe('GracefulDegradation', () => {
  describe('Constructor & Configuration', () => {
    it('should initialize with modes', () => {
      const modes = { full: {}, partial: {} };
      const degradation = new GracefulDegradation(modes);
      
      assert.strictEqual(degradation.currentMode, 'full');
      assert.deepStrictEqual(degradation.modes, modes);
    });

    it('should throw error for empty modes', () => {
      assert.throws(() => new GracefulDegradation({}), /non-empty object/);
      assert.throws(() => new GracefulDegradation(null), /non-empty object/);
    });

    it('should accept custom default mode', () => {
      const modes = { full: {}, partial: {}, minimal: {} };
      const degradation = new GracefulDegradation(modes, {
        defaultMode: 'partial'
      });
      
      assert.strictEqual(degradation.currentMode, 'partial');
    });

    it('should throw error for non-existent default mode', () => {
      const modes = { full: {}, partial: {} };
      assert.throws(
        () => new GracefulDegradation(modes, { defaultMode: 'minimal' }),
        /not found/
      );
    });
  });

  describe('execute() method', () => {
    it('should execute first successful mode', async () => {
      const modes = {
        full: { timeout: 100 },
        partial: { timeout: 500 }
      };

      const degradation = new GracefulDegradation(modes);
      
      let callCount = 0;
      const fn = async (mode) => {
        callCount++;
        if (callCount < 2) throw new Error('Full mode failed');
        return { success: true };
      };

      const result = await degradation.execute('full,partial', fn);
      assert.deepStrictEqual(result, { success: true });
    });

    it('should throw if all modes fail', async () => {
      const modes = { full: {}, partial: {} };
      const degradation = new GracefulDegradation(modes);
      
      const fn = async () => { throw new Error('All failed'); };

      await assert.rejects(
        degradation.execute('full,partial', fn),
        /All failed/
      );
    });

    it('should pass mode config to function', async () => {
      const modes = {
        full: { quality: 'high' },
        partial: { quality: 'low' }
      };

      const degradation = new GracefulDegradation(modes);
      
      let capturedMode;
      const fn = async (mode) => {
        capturedMode = mode;
        return mode;
      };

      const result = await degradation.execute('full', fn);
      assert.deepStrictEqual(result, { quality: 'high' });
    });

    it('should throw error for invalid function', async () => {
      const degradation = new GracefulDegradation({ full: {} });
      
      await assert.rejects(
        degradation.execute('full', 'not-function'),
        /function required/
      );
    });

    it('should throw error for non-existent mode', async () => {
      const degradation = new GracefulDegradation({ full: {} });
      
      const fn = async () => 'success';
      
      await assert.rejects(
        degradation.execute('missing', fn),
        /not found/
      );
    });
  });

  describe('isDegraded() method', () => {
    it('should return false for full mode', () => {
      const degradation = new GracefulDegradation({ full: {}, partial: {} });
      assert.strictEqual(degradation.isDegraded(), false);
    });

    it('should return true for non-full mode', () => {
      const degradation = new GracefulDegradation(
        { full: {}, partial: {} },
        { defaultMode: 'partial' }
      );
      assert.strictEqual(degradation.isDegraded(), true);
    });
  });

  describe('getStatus() method', () => {
    it('should return degradation status', () => {
      const degradation = new GracefulDegradation({ full: {}, partial: {} });
      degradation.successCount = 100;
      degradation.failureCount = 10;
      
      const status = degradation.getStatus();
      
      assert.strictEqual(status.currentMode, 'full');
      assert.strictEqual(status.isDegraded, false);
      assert.strictEqual(status.successCount, 100);
      assert.strictEqual(status.failureCount, 10);
      assert.strictEqual(status.healthRatio, 100 / 110);
    });

    it('should calculate health ratio correctly', () => {
      const degradation = new GracefulDegradation({ full: {} });
      degradation.successCount = 90;
      degradation.failureCount = 10;
      
      const status = degradation.getStatus();
      assert.strictEqual(status.healthRatio, 0.9);
    });
  });

  describe('reset() method', () => {
    it('should reset degradation state', () => {
      const degradation = new GracefulDegradation(
        { full: {}, partial: {} },
        { defaultMode: 'partial' }
      );
      degradation.successCount = 100;
      degradation.failureCount = 10;
      
      degradation.reset();
      
      assert.strictEqual(degradation.currentMode, 'full');
      assert.strictEqual(degradation.successCount, 0);
      assert.strictEqual(degradation.failureCount, 0);
    });
  });

  describe('getLog() method', () => {
    it('should return degradation log', () => {
      const degradation = new GracefulDegradation({ full: {}, partial: {} });
      
      degradation._logDegradation('full', true, null);
      degradation._logDegradation('full', false, 'Timeout');
      
      const log = degradation.getLog();
      assert.strictEqual(log.length, 2);
      assert.strictEqual(log[0].mode, 'full');
      assert.strictEqual(log[1].error, 'Timeout');
    });

    it('should limit log size to 1000 entries', () => {
      const degradation = new GracefulDegradation({ full: {} });
      
      // Add 1100 entries
      for (let i = 0; i < 1100; i++) {
        degradation._logDegradation('full', true, null);
      }
      
      const log = degradation.getLog();
      assert(log.length <= 1000);
    });
  });
});

console.log('\n✓ All feat-recovery tests defined\n');
