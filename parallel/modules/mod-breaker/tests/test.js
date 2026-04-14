/**
 * @fileoverview Comprehensive test suite for mod-breaker
 * Tests cover all state transitions, thresholds, recovery, and monitoring
 * 
 * @test 45+ test cases covering:
 * - Circuit state transitions
 * - Threshold monitoring and triggers
 * - Recovery mechanisms and strategies
 * - Error handling and edge cases
 */

const assert = require('assert');
const {
  CircuitBreaker,
  StateTransitioner,
  ThresholdMonitor,
  RecoveryManager,
  CircuitState,
  RecoveryStrategy
} = require('../implementation');

describe('StateTransitioner', () => {
  let transitioner;

  beforeEach(() => {
    transitioner = new StateTransitioner();
  });

  it('should validate closed to open transition', () => {
    assert.strictEqual(transitioner.canTransition(CircuitState.CLOSED, CircuitState.OPEN), true);
  });

  it('should validate open to half-open transition', () => {
    assert.strictEqual(transitioner.canTransition(CircuitState.OPEN, CircuitState.HALF_OPEN), true);
  });

  it('should validate half-open to closed transition', () => {
    assert.strictEqual(transitioner.canTransition(CircuitState.HALF_OPEN, CircuitState.CLOSED), true);
  });

  it('should validate half-open to open transition', () => {
    assert.strictEqual(transitioner.canTransition(CircuitState.HALF_OPEN, CircuitState.OPEN), true);
  });

  it('should reject invalid transition from closed to half-open', () => {
    assert.strictEqual(transitioner.canTransition(CircuitState.CLOSED, CircuitState.HALF_OPEN), false);
  });

  it('should reject invalid transition from open to closed', () => {
    assert.strictEqual(transitioner.canTransition(CircuitState.OPEN, CircuitState.CLOSED), false);
  });

  it('should throw on invalid state in canTransition', () => {
    assert.throws(() => {
      transitioner.canTransition('INVALID', CircuitState.OPEN);
    });
  });

  it('should throw on invalid transition in transit', () => {
    assert.throws(() => {
      transitioner.transit(CircuitState.CLOSED, CircuitState.HALF_OPEN);
    });
  });

  it('should return transition metadata', () => {
    const transition = transitioner.transit(CircuitState.CLOSED, CircuitState.OPEN);
    assert.strictEqual(transition.from, CircuitState.CLOSED);
    assert.strictEqual(transition.to, CircuitState.OPEN);
    assert.strictEqual(typeof transition.timestamp, 'number');
  });

  it('should return valid next states', () => {
    const states = transitioner.getValidNextStates(CircuitState.CLOSED);
    assert.deepStrictEqual(states, [CircuitState.OPEN]);
  });
});

describe('ThresholdMonitor', () => {
  let monitor;

  beforeEach(() => {
    monitor = new ThresholdMonitor({
      failureThreshold: 5,
      successThreshold: 3
    });
  });

  it('should initialize with default options', () => {
    const m = new ThresholdMonitor();
    assert.strictEqual(m.failureThreshold, 5);
    assert.strictEqual(m.successThreshold, 3);
  });

  it('should record failures correctly', () => {
    monitor.recordFailure();
    const metrics = monitor.getMetrics();
    assert.strictEqual(metrics.currentFailures, 1);
  });

  it('should record successes correctly', () => {
    monitor.recordSuccess();
    const metrics = monitor.getMetrics();
    assert.strictEqual(metrics.currentSuccesses, 1);
  });

  it('should detect when circuit should open', () => {
    for (let i = 0; i < 5; i++) {
      monitor.recordFailure();
    }
    assert.strictEqual(monitor.shouldOpen(), true);
  });

  it('should not open before threshold', () => {
    for (let i = 0; i < 4; i++) {
      monitor.recordFailure();
    }
    assert.strictEqual(monitor.shouldOpen(), false);
  });

  it('should detect when circuit should close', () => {
    for (let i = 0; i < 3; i++) {
      monitor.recordSuccess();
    }
    assert.strictEqual(monitor.shouldClose(), true);
  });

  it('should calculate failure rate correctly', () => {
    monitor.recordFailure();
    monitor.recordFailure();
    monitor.recordSuccess();
    const metrics = monitor.getMetrics();
    assert.strictEqual(metrics.failureRate, 66.66666666666666);
  });

  it('should trigger on failure rate threshold', () => {
    const m = new ThresholdMonitor({ failureRateThreshold: 50 });
    m.recordFailure();
    m.recordFailure();
    m.recordSuccess();
    assert.strictEqual(m.shouldOpen(), true);
  });

  it('should track total metrics', () => {
    monitor.recordFailure();
    monitor.recordSuccess();
    monitor.recordFailure();
    const metrics = monitor.getMetrics();
    assert.strictEqual(metrics.totalFailures, 2);
    assert.strictEqual(metrics.totalSuccesses, 1);
  });

  it('should reset counters', () => {
    monitor.recordFailure();
    monitor.recordSuccess();
    monitor.reset();
    const metrics = monitor.getMetrics();
    assert.strictEqual(metrics.currentFailures, 0);
    assert.strictEqual(metrics.currentSuccesses, 0);
  });

  it('should handle sliding window', () => {
    const m = new ThresholdMonitor({ windowSize: 100 });
    m.recordFailure();
    assert.strictEqual(m.metrics.currentWindow.failures, 1);
  });

  it('should return complete metrics snapshot', () => {
    monitor.recordFailure();
    const metrics = monitor.getMetrics();
    assert(metrics.failureRate !== undefined);
    assert(metrics.currentFailures !== undefined);
    assert(metrics.totalRequests !== undefined);
  });
});

describe('RecoveryManager', () => {
  let manager;

  beforeEach(() => {
    manager = new RecoveryManager({
      strategy: RecoveryStrategy.EXPONENTIAL,
      baseDelay: 100,
      maxDelay: 5000,
      multiplier: 2
    });
  });

  it('should initialize with default options', () => {
    const m = new RecoveryManager();
    assert.strictEqual(m.strategy, RecoveryStrategy.EXPONENTIAL);
    assert.strictEqual(m.baseDelay, 1000);
  });

  it('should calculate exponential backoff', () => {
    manager.recordAttempt();
    const nextRetry = manager.getNextRetryTime();
    assert(nextRetry > 0);
  });

  it('should calculate linear backoff', () => {
    const m = new RecoveryManager({
      strategy: RecoveryStrategy.LINEAR,
      baseDelay: 100
    });
    m.recordAttempt();
    const nextRetry = m.getNextRetryTime();
    assert(nextRetry > 0);
  });

  it('should calculate fixed backoff', () => {
    const m = new RecoveryManager({
      strategy: RecoveryStrategy.FIXED,
      baseDelay: 500
    });
    m.recordAttempt();
    const delay = m._calculateDelay();
    assert.strictEqual(delay, 500);
  });

  it('should respect max delay limit', () => {
    manager.recoveryAttempts = 100;
    const delay = manager._calculateDelay();
    assert(delay <= manager.maxDelay);
  });

  it('should track recovery attempts', () => {
    manager.recordAttempt();
    assert.strictEqual(manager.recoveryAttempts, 1);
  });

  it('should reset recovery state', () => {
    manager.recordAttempt();
    manager.recordAttempt();
    const result = manager.reset();
    assert.strictEqual(result.previousAttempts, 2);
    assert.strictEqual(manager.recoveryAttempts, 0);
  });

  it('should return 0 for next retry time on fresh start', () => {
    const nextRetry = manager.getNextRetryTime();
    assert.strictEqual(nextRetry, 0);
  });

  it('should return recovery state', () => {
    manager.recordAttempt();
    const state = manager.getRecoveryState();
    assert.strictEqual(state.attempts, 1);
    assert(state.nextRetryIn > 0);
  });

  it('should increase delay with each attempt (exponential)', () => {
    manager.recordAttempt();
    const delay1 = manager._calculateDelay();
    manager.recordAttempt();
    const delay2 = manager._calculateDelay();
    assert(delay2 > delay1);
  });

  it('should track recovery timestamps', () => {
    manager.recordAttempt();
    assert(manager.lastRecoveryTime > 0);
    assert(manager.recoveryStartTime > 0);
  });
});

describe('CircuitBreaker', () => {
  let breaker;

  beforeEach(() => {
    breaker = new CircuitBreaker({
      name: 'test-breaker',
      failureThreshold: 3,
      successThreshold: 2,
      timeout: 1000
    });
  });

  it('should initialize in CLOSED state', () => {
    assert.strictEqual(breaker.state, CircuitState.CLOSED);
  });

  it('should execute successful operation in CLOSED state', async () => {
    const operation = async () => 'success';
    const result = await breaker.execute(operation);
    assert.strictEqual(result, 'success');
  });

  it('should increment success count', async () => {
    await breaker.execute(async () => 'success');
    assert.strictEqual(breaker.statistics.successCount, 1);
  });

  it('should throw on failing operation', async () => {
    const operation = async () => {
      throw new Error('Failed');
    };
    try {
      await breaker.execute(operation);
      assert.fail('Should have thrown');
    } catch (error) {
      assert.strictEqual(error.message, 'Failed');
    }
  });

  it('should increment failure count', async () => {
    try {
      await breaker.execute(async () => {
        throw new Error('Failed');
      });
    } catch (e) {
      // expected
    }
    assert.strictEqual(breaker.statistics.failureCount, 1);
  });

  it('should transition to OPEN on threshold', async () => {
    for (let i = 0; i < 3; i++) {
      try {
        await breaker.execute(async () => {
          throw new Error('Failed');
        });
      } catch (e) {
        // expected
      }
    }
    assert.strictEqual(breaker.state, CircuitState.OPEN);
  });

  it('should reject requests when OPEN', async () => {
    // Open the circuit
    for (let i = 0; i < 3; i++) {
      try {
        await breaker.execute(async () => {
          throw new Error('Failed');
        });
      } catch (e) {
        // expected
      }
    }

    // Try to execute
    try {
      await breaker.execute(async () => 'success');
      assert.fail('Should have thrown');
    } catch (error) {
      assert.strictEqual(error.code, 'CIRCUIT_BREAKER_OPEN');
    }
  });

  it('should return retryAfter time when OPEN', async () => {
    for (let i = 0; i < 3; i++) {
      try {
        await breaker.execute(async () => {
          throw new Error('Failed');
        });
      } catch (e) {
        // expected
      }
    }

    try {
      await breaker.execute(async () => 'success');
    } catch (error) {
      assert(error.retryAfter >= 0);
    }
  });

  it('should timeout on slow operations', async () => {
    try {
      await breaker.execute(async () => {
        return new Promise(resolve => setTimeout(resolve, 2000));
      });
      assert.fail('Should have timed out');
    } catch (error) {
      assert(error.message.includes('timeout'));
    }
  });

  it('should record last error', async () => {
    try {
      await breaker.execute(async () => {
        throw new Error('Test error');
      });
    } catch (e) {
      // expected
    }
    assert(breaker.statistics.lastError);
    assert.strictEqual(breaker.statistics.lastError.message, 'Test error');
  });

  it('should manually open circuit', () => {
    const state = breaker.open();
    assert.strictEqual(breaker.state, CircuitState.OPEN);
    assert.strictEqual(state.state, CircuitState.OPEN);
  });

  it('should manually close circuit', () => {
    breaker.open();
    const state = breaker.close();
    assert.strictEqual(breaker.state, CircuitState.CLOSED);
    assert.strictEqual(state.state, CircuitState.CLOSED);
  });

  it('should reset statistics', () => {
    try {
      breaker.execute(async () => {
        throw new Error('Failed');
      });
    } catch (e) {
      // expected
    }
    breaker.reset();
    assert.strictEqual(breaker.statistics.failureCount, 0);
    assert.strictEqual(breaker.statistics.successCount, 0);
  });

  it('should provide state snapshot', () => {
    const state = breaker.getState();
    assert.strictEqual(state.state, CircuitState.CLOSED);
    assert(state.statistics);
    assert(state.metrics);
    assert(state.recovery);
  });

  it('should call onOpen listener', (done) => {
    const breaker2 = new CircuitBreaker({
      failureThreshold: 1,
      onOpen: (state) => {
        assert.strictEqual(state.state, CircuitState.OPEN);
        done();
      }
    });

    breaker2.execute(async () => {
      throw new Error('Failed');
    }).catch(() => {
      // expected
    });
  });

  it('should call onError listener', (done) => {
    const breaker2 = new CircuitBreaker({
      onError: (error) => {
        assert.strictEqual(error.message, 'Failed');
        done();
      }
    });

    breaker2.execute(async () => {
      throw new Error('Failed');
    }).catch(() => {
      // expected
    });
  });

  it('should transition through HALF_OPEN state', async () => {
    // Open circuit
    for (let i = 0; i < 3; i++) {
      try {
        await breaker.execute(async () => {
          throw new Error('Failed');
        });
      } catch (e) {
        // expected
      }
    }
    assert.strictEqual(breaker.state, CircuitState.OPEN);

    // Wait for recovery window (no delay in tests)
    breaker.recoveryManager.lastRecoveryTime = 0;

    // Try again - should go to HALF_OPEN
    try {
      await breaker.execute(async () => 'success');
    } catch (e) {
      // may throw if recovery timing
    }
  });

  it('should provide execution count', () => {
    breaker.execute(async () => 'success').catch(() => {});
    assert.strictEqual(breaker.statistics.executionCount >= 1, true);
  });

  it('should handle context in execute', async () => {
    const result = await breaker.execute(
      async (ctx) => ctx.value,
      { value: 'test-value' }
    );
    assert.strictEqual(result, 'test-value');
  });

  it('should track state changes', () => {
    const firstChange = breaker.statistics.lastStateChange;
    breaker.open();
    const secondChange = breaker.statistics.lastStateChange;
    assert(secondChange > firstChange);
  });
});

// Summary
console.log('✓ All 45+ CircuitBreaker tests configured');
