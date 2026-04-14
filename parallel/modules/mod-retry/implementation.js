/**
 * @fileoverview Retry Handler Implementation - HELIOS v4.0 Fleet Expansion
 * Provides intelligent retry mechanisms with jitter, exponential backoff,
 * and configurable policies for transient failure recovery.
 * 
 * Performance Characteristics:
 * - Backoff calculation: O(1)
 * - Jitter application: O(1)
 * - Attempt tracking: O(1)
 * - Memory footprint: ~1-3 KB per retry manager instance
 * 
 * @author HELIOS Fleet Expansion Team
 * @version 4.0.0
 */

/**
 * Retry policy types enumeration
 * @enum {string}
 */
const RetryPolicy = {
  EXPONENTIAL: 'exponential',
  LINEAR: 'linear',
  FIBONACCI: 'fibonacci',
  FIXED: 'fixed'
};

/**
 * Jitter strategies enumeration
 * @enum {string}
 */
const JitterStrategy = {
  FULL: 'full',           // Random between 0 and max
  EQUAL: 'equal',         // Add/subtract equal amount
  DECORRELATED: 'decorrelated' // AWS-style decorrelated jitter
};

/**
 * JitterCalculator - Computes jitter values to prevent thundering herd
 * 
 * @class
 * @example
 * const jitter = new JitterCalculator({ strategy: 'decorrelated' });
 * const delayWithJitter = jitter.apply(1000); // 1000ms ± jitter
 */
class JitterCalculator {
  /**
   * Creates a JitterCalculator instance
   * @param {object} options - Configuration options
   * @param {string} options.strategy - Jitter strategy (default: 'full')
   * @param {number} options.jitterFactor - Jitter factor 0-1 (default: 0.1)
   * @param {number} options.maxJitter - Maximum jitter in ms (default: 1000)
   */
  constructor(options = {}) {
    this.strategy = options.strategy ?? JitterStrategy.FULL;
    this.jitterFactor = options.jitterFactor ?? 0.1;
    this.maxJitter = options.maxJitter ?? 1000;
    this.previousDelay = 0;
  }

  /**
   * Generates random value between min and max
   * @param {number} min - Minimum value
   * @param {number} max - Maximum value
   * @returns {number} Random value
   * @private
   */
  _random(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
  }

  /**
   * Applies full jitter strategy - random between 0 and max delay
   * @param {number} maxDelay - Maximum delay value
   * @returns {number} Jittered delay
   * @private
   */
  _applyFullJitter(maxDelay) {
    return this._random(0, Math.min(maxDelay, this.maxJitter));
  }

  /**
   * Applies equal jitter strategy - delay/2 + random portion
   * @param {number} delay - Base delay value
   * @returns {number} Jittered delay
   * @private
   */
  _applyEqualJitter(delay) {
    const jitterAmount = Math.min(delay * this.jitterFactor, this.maxJitter);
    const random = this._random(0, jitterAmount);
    return Math.floor(delay / 2) + random;
  }

  /**
   * Applies decorrelated jitter strategy - AWS style
   * Recommended for distributed systems
   * @param {number} delay - Current delay value
   * @returns {number} Jittered delay
   * @private
   */
  _applyDecorrelatedJitter(delay) {
    const jitterAmount = Math.min(delay * 3, this.maxJitter);
    const random = this._random(delay, jitterAmount);
    this.previousDelay = random;
    return random;
  }

  /**
   * Applies jitter to a delay value based on strategy
   * @param {number} delay - Base delay in milliseconds
   * @returns {number} Jittered delay in milliseconds
   */
  apply(delay) {
    if (delay < 0) {
      throw new Error('Delay must be non-negative');
    }

    switch (this.strategy) {
      case JitterStrategy.FULL:
        return this._applyFullJitter(delay);
      case JitterStrategy.EQUAL:
        return this._applyEqualJitter(delay);
      case JitterStrategy.DECORRELATED:
        return this._applyDecorrelatedJitter(delay);
      default:
        return delay;
    }
  }

  /**
   * Gets current jitter strategy info
   * @returns {object} Strategy configuration
   */
  getStrategyInfo() {
    return {
      strategy: this.strategy,
      jitterFactor: this.jitterFactor,
      maxJitter: this.maxJitter
    };
  }
}

/**
 * BackoffGenerator - Generates retry delays using various backoff strategies
 * 
 * @class
 * @example
 * const backoff = new BackoffGenerator({ policy: 'exponential', baseDelay: 100 });
 * const delay = backoff.getDelay(3); // Delay for 3rd attempt
 */
class BackoffGenerator {
  /**
   * Creates a BackoffGenerator instance
   * @param {object} options - Configuration options
   * @param {string} options.policy - Backoff policy (default: 'exponential')
   * @param {number} options.baseDelay - Base delay in ms (default: 100)
   * @param {number} options.maxDelay - Maximum delay in ms (default: 30000)
   * @param {number} options.multiplier - Exponential multiplier (default: 2)
   */
  constructor(options = {}) {
    this.policy = options.policy ?? RetryPolicy.EXPONENTIAL;
    this.baseDelay = options.baseDelay ?? 100;
    this.maxDelay = options.maxDelay ?? 30000;
    this.multiplier = options.multiplier ?? 2;

    this.fibonacci = [1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144];
  }

  /**
   * Calculates exponential backoff: baseDelay * (multiplier ^ attempt)
   * @param {number} attempt - Attempt number (0-indexed)
   * @returns {number} Delay in milliseconds
   * @private
   */
  _calculateExponential(attempt) {
    const delay = this.baseDelay * Math.pow(this.multiplier, attempt);
    return Math.min(delay, this.maxDelay);
  }

  /**
   * Calculates linear backoff: baseDelay * (attempt + 1)
   * @param {number} attempt - Attempt number (0-indexed)
   * @returns {number} Delay in milliseconds
   * @private
   */
  _calculateLinear(attempt) {
    const delay = this.baseDelay * (attempt + 1);
    return Math.min(delay, this.maxDelay);
  }

  /**
   * Calculates fibonacci backoff
   * @param {number} attempt - Attempt number (0-indexed)
   * @returns {number} Delay in milliseconds
   * @private
   */
  _calculateFibonacci(attempt) {
    const fib = attempt < this.fibonacci.length
      ? this.fibonacci[attempt]
      : this.fibonacci[this.fibonacci.length - 1];
    const delay = this.baseDelay * fib;
    return Math.min(delay, this.maxDelay);
  }

  /**
   * Calculates fixed backoff: always returns baseDelay
   * @param {number} attempt - Attempt number (unused)
   * @returns {number} Delay in milliseconds
   * @private
   */
  _calculateFixed(attempt) {
    return this.baseDelay;
  }

  /**
   * Gets delay for a specific retry attempt
   * @param {number} attempt - Attempt number (0-indexed)
   * @returns {number} Delay in milliseconds
   * @throws {Error} If attempt is negative
   */
  getDelay(attempt) {
    if (attempt < 0) {
      throw new Error('Attempt must be non-negative');
    }

    switch (this.policy) {
      case RetryPolicy.EXPONENTIAL:
        return this._calculateExponential(attempt);
      case RetryPolicy.LINEAR:
        return this._calculateLinear(attempt);
      case RetryPolicy.FIBONACCI:
        return this._calculateFibonacci(attempt);
      case RetryPolicy.FIXED:
        return this._calculateFixed(attempt);
      default:
        return this.baseDelay;
    }
  }

  /**
   * Gets backoff configuration info
   * @returns {object} Policy configuration
   */
  getPolicyInfo() {
    return {
      policy: this.policy,
      baseDelay: this.baseDelay,
      maxDelay: this.maxDelay,
      multiplier: this.multiplier
    };
  }
}

/**
 * AttemptTracker - Tracks retry attempt history and state
 * 
 * @class
 * @example
 * const tracker = new AttemptTracker({ maxAttempts: 5 });
 * tracker.recordAttempt(new Error('Failed'));
 * tracker.canRetry(); // true if attempts remaining
 */
class AttemptTracker {
  /**
   * Creates an AttemptTracker instance
   * @param {object} options - Configuration options
   * @param {number} options.maxAttempts - Maximum retry attempts (default: 3)
   * @param {number} options.timeoutMs - Total timeout in ms (default: 60000)
   */
  constructor(options = {}) {
    this.maxAttempts = options.maxAttempts ?? 3;
    this.timeoutMs = options.timeoutMs ?? 60000;

    this.attempts = [];
    this.startTime = Date.now();
    this.currentAttempt = 0;
  }

  /**
   * Records an attempt with result
   * @param {Error|null} error - Error if failed, null if succeeded
   * @param {object} metadata - Additional attempt metadata
   * @returns {object} Attempt record
   */
  recordAttempt(error = null, metadata = {}) {
    const attempt = {
      number: this.currentAttempt,
      timestamp: Date.now(),
      elapsed: Date.now() - this.startTime,
      error: error ? {
        message: error.message,
        code: error.code,
        name: error.name
      } : null,
      success: !error,
      ...metadata
    };

    this.attempts.push(attempt);
    this.currentAttempt++;
    return attempt;
  }

  /**
   * Determines if another retry attempt is possible
   * @returns {boolean} True if retry is possible
   */
  canRetry() {
    const attemptsRemaining = this.maxAttempts > this.currentAttempt;
    const timeRemaining = this.timeoutMs > (Date.now() - this.startTime);
    return attemptsRemaining && timeRemaining;
  }

  /**
   * Gets current attempt number (1-indexed for user display)
   * @returns {number} Current attempt number
   */
  getCurrentAttempt() {
    return this.currentAttempt;
  }

  /**
   * Gets attempts remaining
   * @returns {number} Number of remaining attempts
   */
  getAttemptsRemaining() {
    return Math.max(0, this.maxAttempts - this.currentAttempt);
  }

  /**
   * Gets time remaining until timeout
   * @returns {number} Milliseconds remaining
   */
  getTimeRemaining() {
    const elapsed = Date.now() - this.startTime;
    return Math.max(0, this.timeoutMs - elapsed);
  }

  /**
   * Gets all attempt records
   * @returns {object[]} Array of attempt records
   */
  getAttempts() {
    return [...this.attempts];
  }

  /**
   * Gets summary statistics
   * @returns {object} Summary data
   */
  getSummary() {
    const successful = this.attempts.filter(a => a.success).length;
    const failed = this.attempts.filter(a => !a.success).length;

    return {
      totalAttempts: this.currentAttempt,
      successful,
      failed,
      maxAttempts: this.maxAttempts,
      attemptsRemaining: this.getAttemptsRemaining(),
      totalElapsed: Date.now() - this.startTime,
      timeRemaining: this.getTimeRemaining()
    };
  }

  /**
   * Resets tracker for new retry sequence
   * @returns {void}
   */
  reset() {
    this.attempts = [];
    this.startTime = Date.now();
    this.currentAttempt = 0;
  }
}

/**
 * RetryManager - High-level retry coordination with policies
 * 
 * @class
 * @example
 * const manager = new RetryManager({
 *   maxAttempts: 5,
 *   policy: 'exponential',
 *   jitterStrategy: 'decorrelated'
 * });
 * 
 * const result = await manager.execute(async () => {
 *   return await unstableService.call();
 * });
 */
class RetryManager {
  /**
   * Creates a RetryManager instance
   * @param {object} options - Configuration options
   * @param {number} options.maxAttempts - Maximum attempts (default: 3)
   * @param {string} options.policy - Backoff policy (default: 'exponential')
   * @param {string} options.jitterStrategy - Jitter strategy (default: 'full')
   * @param {number} options.baseDelay - Base delay ms (default: 100)
   * @param {number} options.maxDelay - Max delay ms (default: 30000)
   * @param {string} options.name - Manager name for logging
   * @param {Function} options.shouldRetry - Custom retry condition
   */
  constructor(options = {}) {
    this.name = options.name || 'RetryManager';
    this.maxAttempts = options.maxAttempts ?? 3;

    this.backoffGenerator = new BackoffGenerator({
      policy: options.policy,
      baseDelay: options.baseDelay,
      maxDelay: options.maxDelay
    });

    this.jitterCalculator = new JitterCalculator({
      strategy: options.jitterStrategy
    });

    this.attemptTracker = new AttemptTracker({
      maxAttempts: this.maxAttempts
    });

    this.shouldRetry = options.shouldRetry || this._defaultShouldRetry;
    this.onRetry = options.onRetry || null;
    this.onMaxAttemptsExceeded = options.onMaxAttemptsExceeded || null;

    this.statistics = {
      totalExecutions: 0,
      totalRetries: 0,
      successfulExecutions: 0,
      failedExecutions: 0
    };
  }

  /**
   * Default retry condition - retries on any error
   * @param {Error} error - Operation error
   * @param {number} attempt - Current attempt number
   * @returns {boolean} Whether to retry
   * @private
   */
  _defaultShouldRetry(error, attempt) {
    return attempt < this.maxAttempts;
  }

  /**
   * Executes operation with retry logic
   * @param {Function} operation - Async function to execute
   * @param {object} context - Execution context
   * @returns {Promise} Operation result
   * @throws {Error} Final error after all retries exhausted
   */
  async execute(operation, context = {}) {
    this.statistics.totalExecutions++;
    this.attemptTracker.reset();

    while (this.attemptTracker.canRetry()) {
      try {
        const result = await operation(context);
        this.attemptTracker.recordAttempt(null);
        this.statistics.successfulExecutions++;
        return result;
      } catch (error) {
        const attempt = this.attemptTracker.getCurrentAttempt();
        this.attemptTracker.recordAttempt(error);

        if (!this.shouldRetry(error, attempt)) {
          this.statistics.failedExecutions++;
          if (this.onMaxAttemptsExceeded) {
            this.onMaxAttemptsExceeded(error, this.attemptTracker.getSummary());
          }
          throw error;
        }

        this.statistics.totalRetries++;

        if (this.onRetry) {
          this.onRetry(error, attempt, this.attemptTracker.getSummary());
        }

        const delay = this.backoffGenerator.getDelay(attempt);
        const jitteredDelay = this.jitterCalculator.apply(delay);

        await this._sleep(jitteredDelay);
      }
    }

    this.statistics.failedExecutions++;
    const lastError = this.attemptTracker.getAttempts().pop()?.error;
    throw new Error(`Max retry attempts exceeded for ${this.name}`);
  }

  /**
   * Sleeps for specified milliseconds
   * @param {number} ms - Milliseconds to sleep
   * @returns {Promise<void>} Resolves after delay
   * @private
   */
  _sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  /**
   * Gets current retry configuration
   * @returns {object} Configuration snapshot
   */
  getConfiguration() {
    return {
      name: this.name,
      maxAttempts: this.maxAttempts,
      backoff: this.backoffGenerator.getPolicyInfo(),
      jitter: this.jitterCalculator.getStrategyInfo()
    };
  }

  /**
   * Gets statistics on retry execution
   * @returns {object} Statistics snapshot
   */
  getStatistics() {
    return {
      ...this.statistics,
      averageRetriesPerExecution: this.statistics.totalExecutions > 0
        ? (this.statistics.totalRetries / this.statistics.totalExecutions).toFixed(2)
        : 0,
      successRate: this.statistics.totalExecutions > 0
        ? ((this.statistics.successfulExecutions / this.statistics.totalExecutions) * 100).toFixed(2) + '%'
        : 'N/A'
    };
  }

  /**
   * Gets last execution attempt history
   * @returns {object[]} Array of attempts
   */
  getLastAttemptHistory() {
    return this.attemptTracker.getAttempts();
  }

  /**
   * Resets statistics
   * @returns {void}
   */
  resetStatistics() {
    this.statistics = {
      totalExecutions: 0,
      totalRetries: 0,
      successfulExecutions: 0,
      failedExecutions: 0
    };
  }
}

module.exports = {
  RetryManager,
  JitterCalculator,
  BackoffGenerator,
  AttemptTracker,
  RetryPolicy,
  JitterStrategy
};
