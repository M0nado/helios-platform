/**
 * Error Recovery Module - feat-recovery
 * Production-ready error recovery with retry logic, exponential backoff,
 * fallback strategies, and graceful degradation.
 * @module feat-recovery/implementation
 */

/**
 * RetryPolicy - Configure and execute retry logic
 * Implements configurable retry strategies with various conditions
 * Performance: O(1) per retry evaluation
 */
class RetryPolicy {
  /**
   * Initialize retry policy
   * @param {Object} options - Configuration options
   * @param {number} [options.maxRetries=3] - Maximum retry attempts
   * @param {number} [options.initialDelay=1000] - Initial delay in ms
   * @param {number} [options.maxDelay=30000] - Maximum delay in ms
   * @param {Function} [options.shouldRetry] - Custom retry condition function
   * @param {Array<number>} [options.retryableStatuses=[408,429,500,502,503,504]] - Retryable HTTP statuses
   * @throws {Error} If options are invalid
   */
  constructor(options = {}) {
    if (typeof options !== 'object') {
      throw new Error('Options must be an object');
    }

    this.maxRetries = options.maxRetries || 3;
    this.initialDelay = options.initialDelay || 1000;
    this.maxDelay = options.maxDelay || 30000;
    this.retryableStatuses = options.retryableStatuses || [408, 429, 500, 502, 503, 504];
    this.shouldRetry = options.shouldRetry || this._defaultShouldRetry.bind(this);
    
    if (this.maxRetries < 0 || this.initialDelay < 0) {
      throw new Error('Retry values must be non-negative');
    }
  }

  /**
   * Execute function with retry logic
   * @param {Function} fn - Function to execute
   * @param {Array} [args=[]] - Arguments for function
   * @returns {Promise} Result of function execution
   * @throws {Error} After max retries exhausted
   */
  async execute(fn, args = []) {
    if (typeof fn !== 'function') {
      throw new Error('First argument must be a function');
    }

    let lastError;
    let delay = this.initialDelay;

    for (let attempt = 0; attempt <= this.maxRetries; attempt++) {
      try {
        return await fn(...args);
      } catch (error) {
        lastError = error;
        
        if (attempt === this.maxRetries || !this._shouldRetryError(error)) {
          break;
        }

        await this._delay(delay);
        delay = Math.min(delay * 2, this.maxDelay);
      }
    }

    throw lastError;
  }

  /**
   * Default retry condition (retryable status codes and errors)
   * @private
   * @param {Error} error - Error object
   * @returns {boolean} True if error is retryable
   */
  _defaultShouldRetry(error) {
    if (error.status && this.retryableStatuses.includes(error.status)) {
      return true;
    }
    
    const retryableMessages = ['ECONNREFUSED', 'ECONNRESET', 'ETIMEDOUT', 'EHOSTUNREACH'];
    return retryableMessages.some(msg => error.code === msg || error.message.includes(msg));
  }

  /**
   * Check if error is retryable
   * @private
   * @param {Error} error - Error to check
   * @returns {boolean} True if retryable
   */
  _shouldRetryError(error) {
    return this.shouldRetry(error);
  }

  /**
   * Sleep for specified milliseconds
   * @private
   * @param {number} ms - Milliseconds to sleep
   * @returns {Promise} Resolves after delay
   */
  _delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

/**
 * ExponentialBackoff - Implement exponential backoff with jitter
 * Prevents thundering herd when retrying failed requests
 * Performance: O(1) backoff calculation
 */
class ExponentialBackoff {
  /**
   * Initialize exponential backoff strategy
   * @param {Object} options - Configuration options
   * @param {number} [options.baseDelay=1000] - Base delay in ms
   * @param {number} [options.maxDelay=60000] - Maximum delay in ms
   * @param {number} [options.multiplier=2] - Backoff multiplier
   * @param {number} [options.jitterFactor=0.1] - Jitter as fraction (0-1)
   * @throws {Error} If options are invalid
   */
  constructor(options = {}) {
    this.baseDelay = options.baseDelay || 1000;
    this.maxDelay = options.maxDelay || 60000;
    this.multiplier = options.multiplier || 2;
    this.jitterFactor = options.jitterFactor || 0.1;
    
    if (this.baseDelay < 0 || this.multiplier < 1 || this.jitterFactor < 0 || this.jitterFactor > 1) {
      throw new Error('Invalid backoff configuration');
    }
  }

  /**
   * Calculate delay for attempt number
   * @param {number} attempt - Attempt number (0-indexed)
   * @returns {number} Delay in milliseconds
   * @throws {Error} If attempt is negative
   */
  calculateDelay(attempt) {
    if (attempt < 0) {
      throw new Error('Attempt must be non-negative');
    }

    const exponentialDelay = this.baseDelay * Math.pow(this.multiplier, attempt);
    const cappedDelay = Math.min(exponentialDelay, this.maxDelay);
    const jitter = cappedDelay * this.jitterFactor * Math.random();
    
    return Math.round(cappedDelay + jitter);
  }

  /**
   * Calculate delay with full jitter strategy
   * Better distribution for concurrent requests
   * @param {number} attempt - Attempt number
   * @returns {number} Delay in milliseconds
   */
  calculateDelayFullJitter(attempt) {
    if (attempt < 0) {
      throw new Error('Attempt must be non-negative');
    }

    const exponentialDelay = this.baseDelay * Math.pow(this.multiplier, attempt);
    const maxJitterDelay = Math.min(exponentialDelay, this.maxDelay);
    
    return Math.round(Math.random() * maxJitterDelay);
  }

  /**
   * Calculate delay with equal jitter strategy
   * Balanced approach between full jitter and linear backoff
   * @param {number} attempt - Attempt number
   * @returns {number} Delay in milliseconds
   */
  calculateDelayEqualJitter(attempt) {
    if (attempt < 0) {
      throw new Error('Attempt must be non-negative');
    }

    const exponentialDelay = this.baseDelay * Math.pow(this.multiplier, attempt);
    const cappedDelay = Math.min(exponentialDelay, this.maxDelay);
    const jitterRange = cappedDelay / 2;
    
    return Math.round(jitterRange + Math.random() * jitterRange);
  }

  /**
   * Execute function with exponential backoff
   * @param {Function} fn - Function to execute
   * @param {number} [maxAttempts=3] - Maximum attempts
   * @returns {Promise} Function result
   * @throws {Error} After max attempts exhausted
   */
  async execute(fn, maxAttempts = 3) {
    if (typeof fn !== 'function') {
      throw new Error('First argument must be a function');
    }

    let lastError;

    for (let attempt = 0; attempt < maxAttempts; attempt++) {
      try {
        return await fn();
      } catch (error) {
        lastError = error;
        if (attempt < maxAttempts - 1) {
          const delay = this.calculateDelay(attempt);
          await new Promise(resolve => setTimeout(resolve, delay));
        }
      }
    }

    throw lastError;
  }
}

/**
 * FallbackStrategy - Implement fallback chains for failover
 * Execute alternative strategies when primary fails
 * Performance: O(n) where n = number of fallback strategies
 */
class FallbackStrategy {
  /**
   * Initialize fallback strategy
   * @param {Array<Function>} strategies - Array of strategy functions
   * @param {Object} [options] - Configuration options
   * @param {boolean} [options.allMustFail=true] - All strategies must fail to throw
   * @throws {Error} If strategies array is empty or invalid
   */
  constructor(strategies, options = {}) {
    if (!Array.isArray(strategies) || strategies.length === 0) {
      throw new Error('Strategies must be a non-empty array');
    }

    if (!strategies.every(s => typeof s === 'function')) {
      throw new Error('All strategies must be functions');
    }

    this.strategies = strategies;
    this.allMustFail = options.allMustFail !== false;
    this.executionLog = [];
  }

  /**
   * Execute fallback chain
   * @returns {Promise} Result from first successful strategy
   * @throws {Error} If all strategies fail
   */
  async execute() {
    this.executionLog = [];
    const errors = [];

    for (let i = 0; i < this.strategies.length; i++) {
      try {
        const result = await this.strategies[i]();
        this.executionLog.push({
          strategyIndex: i,
          success: true,
          result
        });
        return result;
      } catch (error) {
        errors.push(error);
        this.executionLog.push({
          strategyIndex: i,
          success: false,
          error: error.message
        });

        if (!this.allMustFail) {
          throw error;
        }
      }
    }

    const error = new Error('All fallback strategies failed');
    error.causes = errors;
    throw error;
  }

  /**
   * Execute with timeout per strategy
   * @param {number} timeout - Timeout in ms per strategy
   * @returns {Promise} Result from first successful strategy
   */
  async executeWithTimeout(timeout) {
    this.executionLog = [];
    const errors = [];

    for (let i = 0; i < this.strategies.length; i++) {
      try {
        const result = await Promise.race([
          this.strategies[i](),
          new Promise((_, reject) =>
            setTimeout(() => reject(new Error('Strategy timeout')), timeout)
          )
        ]);
        
        this.executionLog.push({
          strategyIndex: i,
          success: true,
          result
        });
        return result;
      } catch (error) {
        errors.push(error);
        this.executionLog.push({
          strategyIndex: i,
          success: false,
          error: error.message
        });
      }
    }

    const error = new Error('All fallback strategies failed or timed out');
    error.causes = errors;
    throw error;
  }

  /**
   * Get execution log
   * @returns {Array} Log of strategy executions
   */
  getExecutionLog() {
    return [...this.executionLog];
  }
}

/**
 * GracefulDegradation - Degrade functionality gracefully under load
 * Maintain partial functionality when full functionality fails
 * Performance: O(1) per degradation check
 */
class GracefulDegradation {
  /**
   * Initialize graceful degradation strategy
   * @param {Object} modes - Named degradation modes
   * @param {string} [options.defaultMode='full'] - Default mode name
   * @throws {Error} If modes are invalid
   */
  constructor(modes, options = {}) {
    if (typeof modes !== 'object' || Object.keys(modes).length === 0) {
      throw new Error('Modes must be a non-empty object');
    }

    this.modes = modes;
    this.currentMode = options.defaultMode || 'full';
    this.failureCount = 0;
    this.successCount = 0;
    this.degradationLog = [];

    if (!this.modes[this.currentMode]) {
      throw new Error(`Default mode '${this.currentMode}' not found`);
    }
  }

  /**
   * Execute with degradation fallback
   * @param {string} modeChain - Comma-separated mode names to try in order
   * @param {Function} fn - Function to execute
   * @returns {Promise} Result or degraded response
   */
  async execute(modeChain, fn) {
    if (typeof modeChain !== 'string' || typeof fn !== 'function') {
      throw new Error('Mode chain must be string, function required');
    }

    const modes = modeChain.split(',').map(m => m.trim());

    for (const mode of modes) {
      if (!this.modes[mode]) {
        throw new Error(`Mode '${mode}' not found`);
      }

      try {
        const result = await fn(this.modes[mode]);
        this.successCount++;
        this.currentMode = mode;
        this._logDegradation(mode, true, null);
        return result;
      } catch (error) {
        this.failureCount++;
        this._logDegradation(mode, false, error.message);

        if (mode === modes[modes.length - 1]) {
          throw error;
        }
      }
    }
  }

  /**
   * Check if system is degraded
   * @returns {boolean} True if not in full mode
   */
  isDegraded() {
    return this.currentMode !== 'full';
  }

  /**
   * Get current degradation level
   * @returns {Object} Current mode info
   */
  getStatus() {
    return {
      currentMode: this.currentMode,
      isDegraded: this.isDegraded(),
      failureCount: this.failureCount,
      successCount: this.successCount,
      healthRatio: this.successCount / (this.successCount + this.failureCount) || 1
    };
  }

  /**
   * Reset degradation state
   * @returns {void}
   */
  reset() {
    this.currentMode = 'full';
    this.failureCount = 0;
    this.successCount = 0;
    this.degradationLog = [];
  }

  /**
   * Log degradation event
   * @private
   * @param {string} mode - Mode attempted
   * @param {boolean} success - Whether successful
   * @param {string} error - Error message if failed
   */
  _logDegradation(mode, success, error) {
    this.degradationLog.push({
      timestamp: Date.now(),
      mode,
      success,
      error
    });

    if (this.degradationLog.length > 1000) {
      this.degradationLog = this.degradationLog.slice(-500);
    }
  }

  /**
   * Get degradation log
   * @returns {Array} Log of degradation events
   */
  getLog() {
    return [...this.degradationLog];
  }
}

module.exports = {
  RetryPolicy,
  ExponentialBackoff,
  FallbackStrategy,
  GracefulDegradation
};
