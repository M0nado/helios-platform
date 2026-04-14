/**
 * @fileoverview Circuit Breaker Implementation - HELIOS v4.0 Fleet Expansion
 * Provides fault tolerance through circuit breaker pattern with state management,
 * threshold monitoring, and adaptive recovery mechanisms.
 * 
 * Performance Characteristics:
 * - State transitions: O(1)
 * - Threshold checks: O(1) amortized
 * - Recovery operations: O(log n) where n = recovery attempts
 * - Memory footprint: ~2-5 KB per circuit breaker instance
 * 
 * @author HELIOS Fleet Expansion Team
 * @version 4.0.0
 */

/**
 * Circuit States enumeration for state machine
 * @enum {string}
 */
const CircuitState = {
  CLOSED: 'CLOSED',      // Normal operation, requests flow through
  OPEN: 'OPEN',          // Circuit broken, requests are rejected
  HALF_OPEN: 'HALF_OPEN' // Testing recovery, limited requests allowed
};

/**
 * Recovery strategies for circuit recovery
 * @enum {string}
 */
const RecoveryStrategy = {
  EXPONENTIAL: 'exponential',
  LINEAR: 'linear',
  FIXED: 'fixed'
};

/**
 * StateTransitioner - Manages circuit state transitions with validation
 * 
 * @class
 * @example
 * const transitioner = new StateTransitioner();
 * transitioner.canTransition('CLOSED', 'OPEN'); // true
 * transitioner.transit('CLOSED', 'OPEN'); // { from: 'CLOSED', to: 'OPEN', ... }
 */
class StateTransitioner {
  /**
   * Creates a StateTransitioner instance
   * Defines valid state transition rules
   */
  constructor() {
    this.validTransitions = {
      [CircuitState.CLOSED]: [CircuitState.OPEN],
      [CircuitState.OPEN]: [CircuitState.HALF_OPEN],
      [CircuitState.HALF_OPEN]: [CircuitState.CLOSED, CircuitState.OPEN]
    };
  }

  /**
   * Determines if a state transition is allowed
   * @param {string} fromState - Current state
   * @param {string} toState - Target state
   * @returns {boolean} Whether transition is valid
   * @throws {Error} If states are invalid
   */
  canTransition(fromState, toState) {
    if (!this.validTransitions[fromState]) {
      throw new Error(`Invalid state: ${fromState}`);
    }
    return this.validTransitions[fromState].includes(toState);
  }

  /**
   * Performs a state transition with metadata
   * @param {string} fromState - Current state
   * @param {string} toState - Target state
   * @returns {object} Transition metadata
   * @throws {Error} If transition is not allowed
   */
  transit(fromState, toState) {
    if (!this.canTransition(fromState, toState)) {
      throw new Error(`Cannot transition from ${fromState} to ${toState}`);
    }
    return {
      from: fromState,
      to: toState,
      timestamp: Date.now(),
      valid: true
    };
  }

  /**
   * Gets all valid next states for current state
   * @param {string} currentState - Current state
   * @returns {string[]} Array of valid next states
   */
  getValidNextStates(currentState) {
    return this.validTransitions[currentState] || [];
  }
}

/**
 * ThresholdMonitor - Tracks metrics and determines circuit open/close
 * 
 * @class
 * @example
 * const monitor = new ThresholdMonitor({ failureThreshold: 5, successThreshold: 3 });
 * monitor.recordFailure();
 * monitor.shouldOpen(); // true if threshold exceeded
 */
class ThresholdMonitor {
  /**
   * Creates a ThresholdMonitor instance
   * @param {object} options - Configuration options
   * @param {number} options.failureThreshold - Failures needed to open circuit (default: 5)
   * @param {number} options.successThreshold - Successes needed to close circuit (default: 3)
   * @param {number} options.failureRateThreshold - Failure rate % to trigger open (default: 50)
   * @param {number} options.windowSize - Metrics window size in ms (default: 60000)
   */
  constructor(options = {}) {
    this.failureThreshold = options.failureThreshold ?? 5;
    this.successThreshold = options.successThreshold ?? 3;
    this.failureRateThreshold = options.failureRateThreshold ?? 50;
    this.windowSize = options.windowSize ?? 60000;

    this.failures = 0;
    this.successes = 0;
    this.totalRequests = 0;
    this.windowStart = Date.now();
    this.metrics = {
      totalFailures: 0,
      totalSuccesses: 0,
      failureRate: 0,
      currentWindow: {
        failures: 0,
        successes: 0
      }
    };
  }

  /**
   * Resets metrics for a sliding window
   * @private
   */
  _resetWindow() {
    if (Date.now() - this.windowStart > this.windowSize) {
      this.metrics.currentWindow.failures = 0;
      this.metrics.currentWindow.successes = 0;
      this.windowStart = Date.now();
    }
  }

  /**
   * Records a request failure
   * @returns {object} Updated metrics
   */
  recordFailure() {
    this._resetWindow();
    this.failures++;
    this.totalRequests++;
    this.metrics.totalFailures++;
    this.metrics.currentWindow.failures++;
    this._updateFailureRate();
    return this.getMetrics();
  }

  /**
   * Records a request success
   * @returns {object} Updated metrics
   */
  recordSuccess() {
    this._resetWindow();
    this.successes++;
    this.totalRequests++;
    this.metrics.totalSuccesses++;
    this.metrics.currentWindow.successes++;
    this._updateFailureRate();
    return this.getMetrics();
  }

  /**
   * Updates calculated failure rate
   * @private
   */
  _updateFailureRate() {
    if (this.totalRequests === 0) {
      this.metrics.failureRate = 0;
      return;
    }
    this.metrics.failureRate = (this.failures / this.totalRequests) * 100;
  }

  /**
   * Determines if circuit should open based on thresholds
   * @returns {boolean} True if circuit should open
   */
  shouldOpen() {
    return this.failures >= this.failureThreshold ||
           this.metrics.failureRate >= this.failureRateThreshold;
  }

  /**
   * Determines if circuit should close based on successes
   * @returns {boolean} True if circuit should close
   */
  shouldClose() {
    return this.successes >= this.successThreshold;
  }

  /**
   * Gets current metrics snapshot
   * @returns {object} Current metrics
   */
  getMetrics() {
    return {
      ...this.metrics,
      currentFailures: this.failures,
      currentSuccesses: this.successes,
      totalRequests: this.totalRequests
    };
  }

  /**
   * Resets all counters
   * @returns {void}
   */
  reset() {
    this.failures = 0;
    this.successes = 0;
    this.totalRequests = 0;
    this.metrics = {
      totalFailures: this.metrics.totalFailures,
      totalSuccesses: this.metrics.totalSuccesses,
      failureRate: 0,
      currentWindow: { failures: 0, successes: 0 }
    };
  }
}

/**
 * RecoveryManager - Handles circuit recovery with adaptive timing
 * 
 * @class
 * @example
 * const recovery = new RecoveryManager({ strategy: 'exponential', baseDelay: 1000 });
 * const nextRetry = recovery.getNextRetryTime(); // Exponentially increasing delay
 */
class RecoveryManager {
  /**
   * Creates a RecoveryManager instance
   * @param {object} options - Configuration options
   * @param {string} options.strategy - Recovery strategy (default: 'exponential')
   * @param {number} options.baseDelay - Base delay in ms (default: 1000)
   * @param {number} options.maxDelay - Maximum delay in ms (default: 30000)
   * @param {number} options.multiplier - Exponential multiplier (default: 2)
   */
  constructor(options = {}) {
    this.strategy = options.strategy ?? RecoveryStrategy.EXPONENTIAL;
    this.baseDelay = options.baseDelay ?? 1000;
    this.maxDelay = options.maxDelay ?? 30000;
    this.multiplier = options.multiplier ?? 2;

    this.recoveryAttempts = 0;
    this.lastRecoveryTime = null;
    this.recoveryStartTime = null;
  }

  /**
   * Calculates next retry delay based on strategy
   * @returns {number} Delay in milliseconds
   * @private
   */
  _calculateDelay() {
    let delay;

    switch (this.strategy) {
      case RecoveryStrategy.EXPONENTIAL:
        delay = this.baseDelay * Math.pow(this.multiplier, this.recoveryAttempts);
        break;
      case RecoveryStrategy.LINEAR:
        delay = this.baseDelay * (this.recoveryAttempts + 1);
        break;
      case RecoveryStrategy.FIXED:
        delay = this.baseDelay;
        break;
      default:
        delay = this.baseDelay;
    }

    return Math.min(delay, this.maxDelay);
  }

  /**
   * Gets time until next recovery attempt
   * @returns {number} Milliseconds until next retry (0 if ready)
   */
  getNextRetryTime() {
    if (!this.lastRecoveryTime) {
      return 0;
    }

    const delay = this._calculateDelay();
    const elapsed = Date.now() - this.lastRecoveryTime;
    return Math.max(0, delay - elapsed);
  }

  /**
   * Records a recovery attempt
   * @returns {object} Recovery state
   */
  recordAttempt() {
    this.recoveryAttempts++;
    this.lastRecoveryTime = Date.now();
    if (!this.recoveryStartTime) {
      this.recoveryStartTime = this.lastRecoveryTime;
    }
    return this.getRecoveryState();
  }

  /**
   * Resets recovery state after successful recovery
   * @returns {object} Reset recovery state
   */
  reset() {
    const previousAttempts = this.recoveryAttempts;
    this.recoveryAttempts = 0;
    this.lastRecoveryTime = null;
    this.recoveryStartTime = null;
    return { previousAttempts, reset: true };
  }

  /**
   * Gets current recovery state
   * @returns {object} Recovery state metrics
   */
  getRecoveryState() {
    return {
      attempts: this.recoveryAttempts,
      lastAttemptTime: this.lastRecoveryTime,
      nextRetryIn: this.getNextRetryTime(),
      strategy: this.strategy
    };
  }
}

/**
 * CircuitBreaker - Main circuit breaker implementation
 * Prevents cascading failures through fault isolation and adaptive recovery
 * 
 * @class
 * @example
 * const breaker = new CircuitBreaker({
 *   failureThreshold: 5,
 *   successThreshold: 3,
 *   timeout: 30000
 * });
 * 
 * const result = await breaker.execute(async () => {
 *   return await externalService.call();
 * });
 */
class CircuitBreaker {
  /**
   * Creates a CircuitBreaker instance
   * @param {object} options - Configuration options
   * @param {number} options.failureThreshold - Failures to open circuit (default: 5)
   * @param {number} options.successThreshold - Successes to close circuit (default: 3)
   * @param {number} options.timeout - Request timeout in ms (default: 5000)
   * @param {string} options.recoveryStrategy - Recovery strategy (default: 'exponential')
   * @param {number} options.recoveryBaseDelay - Base recovery delay ms (default: 1000)
   * @param {string} options.name - Circuit breaker name for logging
   */
  constructor(options = {}) {
    this.state = CircuitState.CLOSED;
    this.name = options.name || 'CircuitBreaker';
    this.timeout = options.timeout ?? 5000;

    this.stateTransitioner = new StateTransitioner();
    this.thresholdMonitor = new ThresholdMonitor({
      failureThreshold: options.failureThreshold,
      successThreshold: options.successThreshold
    });
    this.recoveryManager = new RecoveryManager({
      strategy: options.recoveryStrategy,
      baseDelay: options.recoveryBaseDelay
    });

    this.statistics = {
      executionCount: 0,
      successCount: 0,
      failureCount: 0,
      rejectionCount: 0,
      lastError: null,
      lastStateChange: Date.now()
    };

    this.listeners = {
      onOpen: options.onOpen || null,
      onClose: options.onClose || null,
      onHalfOpen: options.onHalfOpen || null,
      onError: options.onError || null
    };
  }

  /**
   * Executes operation with circuit breaker protection
   * @param {Function} operation - Async function to execute
   * @param {object} context - Execution context (optional)
   * @returns {Promise} Operation result
   * @throws {Error} CircuitBreakerOpenError or operation error
   */
  async execute(operation, context = {}) {
    if (this.state === CircuitState.OPEN) {
      if (this.recoveryManager.getNextRetryTime() === 0) {
        this._transitionState(CircuitState.HALF_OPEN);
      } else {
        const error = new Error(`Circuit breaker is OPEN - ${this.name}`);
        error.code = 'CIRCUIT_BREAKER_OPEN';
        error.retryAfter = this.recoveryManager.getNextRetryTime();
        throw error;
      }
    }

    this.statistics.executionCount++;

    try {
      const result = await this._executeWithTimeout(operation, context);
      this._handleSuccess();
      return result;
    } catch (error) {
      this._handleFailure(error);
      throw error;
    }
  }

  /**
   * Executes operation with timeout protection
   * @param {Function} operation - Operation to execute
   * @param {object} context - Execution context
   * @returns {Promise} Operation result
   * @private
   */
  async _executeWithTimeout(operation, context) {
    return Promise.race([
      operation(context),
      new Promise((_, reject) =>
        setTimeout(
          () => reject(new Error(`Operation timeout after ${this.timeout}ms`)),
          this.timeout
        )
      )
    ]);
  }

  /**
   * Handles successful operation execution
   * @private
   */
  _handleSuccess() {
    this.statistics.successCount++;
    this.thresholdMonitor.recordSuccess();

    if (this.state === CircuitState.HALF_OPEN) {
      if (this.thresholdMonitor.shouldClose()) {
        this._transitionState(CircuitState.CLOSED);
        this.thresholdMonitor.reset();
        this.recoveryManager.reset();
      }
    }
  }

  /**
   * Handles operation failure
   * @param {Error} error - Failure error
   * @private
   */
  _handleFailure(error) {
    this.statistics.failureCount++;
    this.statistics.lastError = {
      message: error.message,
      code: error.code,
      timestamp: Date.now()
    };

    this.thresholdMonitor.recordFailure();

    if (this.listeners.onError) {
      this.listeners.onError(error, this.state);
    }

    if (this.state === CircuitState.HALF_OPEN) {
      this._transitionState(CircuitState.OPEN);
    } else if (this.state === CircuitState.CLOSED && this.thresholdMonitor.shouldOpen()) {
      this._transitionState(CircuitState.OPEN);
    }
  }

  /**
   * Transitions circuit to new state with notifications
   * @param {string} newState - Target state
   * @private
   */
  _transitionState(newState) {
    const transition = this.stateTransitioner.transit(this.state, newState);
    this.state = newState;
    this.statistics.lastStateChange = Date.now();

    if (newState === CircuitState.OPEN) {
      this.recoveryManager.recordAttempt();
      if (this.listeners.onOpen) {
        this.listeners.onOpen(this.getState());
      }
    } else if (newState === CircuitState.CLOSED) {
      if (this.listeners.onClose) {
        this.listeners.onClose(this.getState());
      }
    } else if (newState === CircuitState.HALF_OPEN) {
      if (this.listeners.onHalfOpen) {
        this.listeners.onHalfOpen(this.getState());
      }
    }
  }

  /**
   * Gets current circuit state and statistics
   * @returns {object} State snapshot
   */
  getState() {
    return {
      name: this.name,
      state: this.state,
      statistics: { ...this.statistics },
      metrics: this.thresholdMonitor.getMetrics(),
      recovery: this.recoveryManager.getRecoveryState()
    };
  }

  /**
   * Manually opens the circuit
   * @returns {object} New state
   */
  open() {
    if (this.state !== CircuitState.OPEN) {
      this._transitionState(CircuitState.OPEN);
    }
    return this.getState();
  }

  /**
   * Manually closes the circuit
   * @returns {object} New state
   */
  close() {
    if (this.state !== CircuitState.CLOSED) {
      this._transitionState(CircuitState.CLOSED);
      this.thresholdMonitor.reset();
      this.recoveryManager.reset();
    }
    return this.getState();
  }

  /**
   * Resets all statistics and state
   * @returns {object} Reset state
   */
  reset() {
    this.statistics = {
      executionCount: 0,
      successCount: 0,
      failureCount: 0,
      rejectionCount: 0,
      lastError: null,
      lastStateChange: Date.now()
    };
    this.thresholdMonitor.reset();
    this.recoveryManager.reset();
    return this.getState();
  }
}

module.exports = {
  CircuitBreaker,
  StateTransitioner,
  ThresholdMonitor,
  RecoveryManager,
  CircuitState,
  RecoveryStrategy
};
