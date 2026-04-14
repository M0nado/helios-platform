/**
 * @fileoverview mod-retry Public API
 * Retry Handler module for HELIOS v4.0 Fleet Expansion
 * 
 * @module mod-retry
 * @exports {RetryManager, JitterCalculator, BackoffGenerator, AttemptTracker, RetryPolicy, JitterStrategy}
 */

const {
  RetryManager,
  JitterCalculator,
  BackoffGenerator,
  AttemptTracker,
  RetryPolicy,
  JitterStrategy
} = require('./implementation');

/**
 * Creates a new retry manager instance with default configuration
 * @function
 * @param {object} options - Configuration options
 * @param {number} options.maxAttempts - Maximum retry attempts
 * @param {string} options.policy - Backoff policy (exponential|linear|fibonacci|fixed)
 * @param {string} options.jitterStrategy - Jitter strategy (full|equal|decorrelated)
 * @param {string} options.name - Manager name for logging
 * @returns {RetryManager} New retry manager instance
 * @example
 * const manager = createRetryManager({ 
 *   maxAttempts: 5, 
 *   policy: 'exponential',
 *   jitterStrategy: 'decorrelated'
 * });
 */
function createRetryManager(options = {}) {
  return new RetryManager(options);
}

module.exports = {
  // Classes
  RetryManager,
  JitterCalculator,
  BackoffGenerator,
  AttemptTracker,

  // Enums
  RetryPolicy,
  JitterStrategy,

  // Factory functions
  createRetryManager,

  // Metadata
  version: '4.0.0',
  name: 'mod-retry',
  description: 'Retry handler with backoff and jitter for HELIOS v4.0'
};
