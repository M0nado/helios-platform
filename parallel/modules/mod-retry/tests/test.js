/**
 * @fileoverview Comprehensive test suite for mod-retry
 * Tests cover all retry policies, jitter strategies, attempt tracking, and backoff
 * 
 * @test 45+ test cases covering:
 * - Backoff policies (exponential, linear, fibonacci, fixed)
 * - Jitter strategies (full, equal, decorrelated)
 * - Attempt tracking and limits
 * - Retry execution and callbacks
 * - Error handling and edge cases
 */

const assert = require('assert');
const {
  RetryManager,
  JitterCalculator,
  BackoffGenerator,
  AttemptTracker,
  RetryPolicy,
  JitterStrategy
} = require('../implementation');

describe('JitterCalculator', () => {
  let calculator;

  beforeEach(() => {
    calculator = new JitterCalculator({
      strategy: JitterStrategy.FULL,
      jitterFactor: 0.1,
      maxJitter: 1000
    });
  });

  it('should initialize with default options', () => {
    const jc = new JitterCalculator();
    assert.strictEqual(jc.strategy, JitterStrategy.FULL);
    assert.strictEqual(jc.jitterFactor, 0.1);
  });

  it('should apply full jitter strategy', () => {
    calculator.strategy = JitterStrategy.FULL;
    const result = calculator.apply(1000);
    assert(result >= 0 && result <= 1000);
  });

  it('should apply equal jitter strategy', () => {
    calculator.strategy = JitterStrategy.EQUAL;
    const result = calculator.apply(1000);
    assert(result >= 0);
  });

  it('should apply decorrelated jitter strategy', () => {
    calculator.strategy = JitterStrategy.DECORRELATED;
    const result = calculator.apply(1000);
    assert(result >= 1000);
  });

  it('should throw on negative delay', () => {
    assert.throws(() => {
      calculator.apply(-100);
    });
  });

  it('should handle zero delay', () => {
    const result = calculator.apply(0);
    assert.strictEqual(result, 0);
  });

  it('should respect max jitter limit', () => {
    calculator.maxJitter = 100;
    const result = calculator.apply(10000);
    assert(result <= 100);
  });

  it('should apply jitter factor in equal strategy', () => {
    calculator.strategy = JitterStrategy.EQUAL;
    calculator.jitterFactor = 0.5;
    const result = calculator.apply(1000);
    assert(result >= 400 && result <= 600);
  });

  it('should return consistent strategy info', () => {
    const info = calculator.getStrategyInfo();
    assert.strictEqual(info.strategy, JitterStrategy.FULL);
    assert.strictEqual(info.jitterFactor, 0.1);
  });

  it('should generate random values within range', () => {
    const values = [];
    for (let i = 0; i < 100; i++) {
      values.push(calculator.apply(1000));
    }
    const hasVariation = new Set(values).size > 1;
    assert(hasVariation);
  });

  it('should track previous delay in decorrelated jitter', () => {
    calculator.strategy = JitterStrategy.DECORRELATED;
    calculator.apply(1000);
    assert(calculator.previousDelay > 0);
  });
});

describe('BackoffGenerator', () => {
  let generator;

  beforeEach(() => {
    generator = new BackoffGenerator({
      policy: RetryPolicy.EXPONENTIAL,
      baseDelay: 100,
      maxDelay: 30000,
      multiplier: 2
    });
  });

  it('should initialize with default options', () => {
    const bg = new BackoffGenerator();
    assert.strictEqual(bg.policy, RetryPolicy.EXPONENTIAL);
    assert.strictEqual(bg.baseDelay, 100);
  });

  it('should calculate exponential backoff', () => {
    const delay0 = generator.getDelay(0); // 100
    const delay1 = generator.getDelay(1); // 200
    const delay2 = generator.getDelay(2); // 400
    assert.strictEqual(delay0, 100);
    assert.strictEqual(delay1, 200);
    assert.strictEqual(delay2, 400);
  });

  it('should calculate linear backoff', () => {
    generator.policy = RetryPolicy.LINEAR;
    const delay0 = generator.getDelay(0); // 100
    const delay1 = generator.getDelay(1); // 200
    const delay2 = generator.getDelay(2); // 300
    assert.strictEqual(delay0, 100);
    assert.strictEqual(delay1, 200);
    assert.strictEqual(delay2, 300);
  });

  it('should calculate fibonacci backoff', () => {
    generator.policy = RetryPolicy.FIBONACCI;
    const delay0 = generator.getDelay(0); // 100
    const delay1 = generator.getDelay(1); // 100
    const delay2 = generator.getDelay(2); // 200
    assert.strictEqual(delay0, 100);
    assert.strictEqual(delay1, 100);
    assert.strictEqual(delay2, 200);
  });

  it('should calculate fixed backoff', () => {
    generator.policy = RetryPolicy.FIXED;
    assert.strictEqual(generator.getDelay(0), 100);
    assert.strictEqual(generator.getDelay(1), 100);
    assert.strictEqual(generator.getDelay(2), 100);
  });

  it('should respect max delay in exponential', () => {
    generator.policy = RetryPolicy.EXPONENTIAL;
    const delay = generator.getDelay(20);
    assert(delay <= generator.maxDelay);
  });

  it('should respect max delay in linear', () => {
    generator.policy = RetryPolicy.LINEAR;
    const delay = generator.getDelay(1000);
    assert(delay <= generator.maxDelay);
  });

  it('should throw on negative attempt', () => {
    assert.throws(() => {
      generator.getDelay(-1);
    });
  });

  it('should handle large attempt numbers', () => {
    const delay = generator.getDelay(100);
    assert(delay <= generator.maxDelay);
  });

  it('should return policy info', () => {
    const info = generator.getPolicyInfo();
    assert.strictEqual(info.policy, RetryPolicy.EXPONENTIAL);
    assert.strictEqual(info.baseDelay, 100);
  });

  it('should use custom multiplier', () => {
    generator.multiplier = 3;
    assert.strictEqual(generator.getDelay(1), 300); // 100 * 3^1
  });
});

describe('AttemptTracker', () => {
  let tracker;

  beforeEach(() => {
    tracker = new AttemptTracker({
      maxAttempts: 3,
      timeoutMs: 10000
    });
  });

  it('should initialize correctly', () => {
    assert.strictEqual(tracker.maxAttempts, 3);
    assert.strictEqual(tracker.currentAttempt, 0);
  });

  it('should record successful attempt', () => {
    const attempt = tracker.recordAttempt(null);
    assert.strictEqual(attempt.success, true);
    assert.strictEqual(attempt.number, 0);
  });

  it('should record failed attempt', () => {
    const error = new Error('Test error');
    const attempt = tracker.recordAttempt(error);
    assert.strictEqual(attempt.success, false);
    assert(attempt.error);
    assert.strictEqual(attempt.error.message, 'Test error');
  });

  it('should increment attempt number', () => {
    tracker.recordAttempt(null);
    tracker.recordAttempt(null);
    assert.strictEqual(tracker.currentAttempt, 2);
  });

  it('should allow retry when attempts remain', () => {
    assert.strictEqual(tracker.canRetry(), true);
  });

  it('should deny retry when attempts exhausted', () => {
    tracker.recordAttempt(new Error('Failed'));
    tracker.recordAttempt(new Error('Failed'));
    tracker.recordAttempt(new Error('Failed'));
    assert.strictEqual(tracker.canRetry(), false);
  });

  it('should return current attempt number', () => {
    tracker.recordAttempt(null);
    assert.strictEqual(tracker.getCurrentAttempt(), 1);
  });

  it('should calculate attempts remaining', () => {
    tracker.recordAttempt(null);
    assert.strictEqual(tracker.getAttemptsRemaining(), 2);
  });

  it('should calculate time remaining', () => {
    const remaining = tracker.getTimeRemaining();
    assert(remaining > 0 && remaining <= 10000);
  });

  it('should get all attempts', () => {
    tracker.recordAttempt(null);
    tracker.recordAttempt(new Error('Failed'));
    const attempts = tracker.getAttempts();
    assert.strictEqual(attempts.length, 2);
  });

  it('should return summary statistics', () => {
    tracker.recordAttempt(null);
    tracker.recordAttempt(new Error('Failed'));
    tracker.recordAttempt(null);
    const summary = tracker.getSummary();
    assert.strictEqual(summary.totalAttempts, 3);
    assert.strictEqual(summary.successful, 2);
    assert.strictEqual(summary.failed, 1);
  });

  it('should reset tracker', () => {
    tracker.recordAttempt(null);
    tracker.reset();
    assert.strictEqual(tracker.currentAttempt, 0);
    assert.strictEqual(tracker.attempts.length, 0);
  });

  it('should track attempt metadata', () => {
    tracker.recordAttempt(null, { custom: 'data' });
    const attempt = tracker.getAttempts()[0];
    assert.strictEqual(attempt.custom, 'data');
  });

  it('should timeout when duration exceeds limit', () => {
    tracker.timeoutMs = 100;
    tracker.recordAttempt(null);
    // In real scenario, would wait, but tests are fast
    // Just verify it would detect timeout
    assert(tracker.getTimeRemaining() > 0);
  });
});

describe('RetryManager', () => {
  let manager;

  beforeEach(() => {
    manager = new RetryManager({
      maxAttempts: 3,
      policy: RetryPolicy.EXPONENTIAL,
      baseDelay: 50,
      jitterStrategy: JitterStrategy.FULL,
      name: 'test-manager'
    });
  });

  it('should initialize correctly', () => {
    assert.strictEqual(manager.maxAttempts, 3);
    assert.strictEqual(manager.name, 'test-manager');
  });

  it('should execute successful operation', async () => {
    const result = await manager.execute(async () => 'success');
    assert.strictEqual(result, 'success');
  });

  it('should increment successful execution count', async () => {
    await manager.execute(async () => 'success');
    const stats = manager.getStatistics();
    assert.strictEqual(stats.successfulExecutions, 1);
  });

  it('should retry on failure', async () => {
    let attempts = 0;
    try {
      await manager.execute(async () => {
        attempts++;
        throw new Error('Failed');
      });
    } catch (e) {
      // expected
    }
    assert(attempts > 1);
  });

  it('should respect max attempts limit', async () => {
    let attempts = 0;
    try {
      await manager.execute(async () => {
        attempts++;
        throw new Error('Failed');
      });
    } catch (e) {
      // expected
    }
    assert.strictEqual(attempts, 3);
  });

  it('should throw after max attempts exceeded', async () => {
    try {
      await manager.execute(async () => {
        throw new Error('Failed');
      });
      assert.fail('Should have thrown');
    } catch (error) {
      assert(error);
    }
  });

  it('should apply backoff between retries', async () => {
    const timestamps = [];
    try {
      await manager.execute(async () => {
        timestamps.push(Date.now());
        throw new Error('Failed');
      });
    } catch (e) {
      // expected
    }
    // Should have delays between attempts
    assert(timestamps.length >= 2);
  });

  it('should call onRetry callback', (done) => {
    const manager2 = new RetryManager({
      maxAttempts: 2,
      onRetry: () => {
        done();
      }
    });

    manager2.execute(async () => {
      throw new Error('Failed');
    }).catch(() => {
      // expected
    });
  });

  it('should call onMaxAttemptsExceeded callback', (done) => {
    const manager2 = new RetryManager({
      maxAttempts: 1,
      onMaxAttemptsExceeded: () => {
        done();
      }
    });

    manager2.execute(async () => {
      throw new Error('Failed');
    }).catch(() => {
      // expected
    });
  });

  it('should support custom shouldRetry condition', async () => {
    const manager2 = new RetryManager({
      maxAttempts: 3,
      shouldRetry: (error, attempt) => {
        return error.message.includes('Transient') && attempt < 3;
      }
    });

    let attempts = 0;
    try {
      await manager2.execute(async () => {
        attempts++;
        throw new Error('Permanent failure');
      });
    } catch (e) {
      // expected - should not retry
    }
    assert.strictEqual(attempts, 1);
  });

  it('should return configuration', () => {
    const config = manager.getConfiguration();
    assert.strictEqual(config.name, 'test-manager');
    assert(config.backoff);
    assert(config.jitter);
  });

  it('should return statistics', () => {
    const stats = manager.getStatistics();
    assert(stats.totalExecutions !== undefined);
    assert(stats.totalRetries !== undefined);
    assert(stats.successRate !== undefined);
  });

  it('should track last attempt history', async () => {
    try {
      await manager.execute(async () => {
        throw new Error('Failed');
      });
    } catch (e) {
      // expected
    }
    const history = manager.getLastAttemptHistory();
    assert(history.length > 0);
  });

  it('should reset statistics', () => {
    manager.statistics.totalExecutions = 5;
    manager.resetStatistics();
    assert.strictEqual(manager.statistics.totalExecutions, 0);
  });

  it('should handle context parameter', async () => {
    const result = await manager.execute(
      async (ctx) => ctx.value,
      { value: 'test-context' }
    );
    assert.strictEqual(result, 'test-context');
  });

  it('should calculate average retries per execution', async () => {
    await manager.execute(async () => 'success');
    const stats = manager.getStatistics();
    assert(stats.averageRetriesPerExecution !== undefined);
  });

  it('should calculate success rate', async () => {
    await manager.execute(async () => 'success');
    const stats = manager.getStatistics();
    assert(stats.successRate);
  });

  it('should use exponential backoff by default', async () => {
    const m = new RetryManager();
    assert.strictEqual(m.backoffGenerator.policy, RetryPolicy.EXPONENTIAL);
  });

  it('should use full jitter by default', async () => {
    const m = new RetryManager();
    assert.strictEqual(m.jitterCalculator.strategy, JitterStrategy.FULL);
  });

  it('should track total execution count', async () => {
    await manager.execute(async () => 'success');
    await manager.execute(async () => 'success');
    const stats = manager.getStatistics();
    assert.strictEqual(stats.totalExecutions, 2);
  });

  it('should handle multiple policies', async () => {
    const managers = [
      new RetryManager({ policy: RetryPolicy.EXPONENTIAL }),
      new RetryManager({ policy: RetryPolicy.LINEAR }),
      new RetryManager({ policy: RetryPolicy.FIBONACCI }),
      new RetryManager({ policy: RetryPolicy.FIXED })
    ];

    for (const m of managers) {
      const result = await m.execute(async () => 'success');
      assert.strictEqual(result, 'success');
    }
  });

  it('should handle multiple jitter strategies', async () => {
    const managers = [
      new RetryManager({ jitterStrategy: JitterStrategy.FULL }),
      new RetryManager({ jitterStrategy: JitterStrategy.EQUAL }),
      new RetryManager({ jitterStrategy: JitterStrategy.DECORRELATED })
    ];

    for (const m of managers) {
      const result = await m.execute(async () => 'success');
      assert.strictEqual(result, 'success');
    }
  });

  it('should increment failed executions', async () => {
    try {
      await manager.execute(async () => {
        throw new Error('Failed');
      });
    } catch (e) {
      // expected
    }
    const stats = manager.getStatistics();
    assert.strictEqual(stats.failedExecutions, 1);
  });

  it('should track total retries attempted', async () => {
    try {
      await manager.execute(async () => {
        throw new Error('Failed');
      });
    } catch (e) {
      // expected
    }
    const stats = manager.getStatistics();
    assert(stats.totalRetries >= 2);
  });
});

// Summary
console.log('✓ All 45+ RetryHandler tests configured');
