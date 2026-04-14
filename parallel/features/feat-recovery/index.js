/**
 * feat-recovery - Error Recovery Module
 * Production-ready error recovery with retry logic, exponential backoff,
 * fallback strategies, and graceful degradation.
 * @module feat-recovery
 * @version 1.0.0
 */

const {
  RetryPolicy,
  ExponentialBackoff,
  FallbackStrategy,
  GracefulDegradation
} = require('./implementation');

module.exports = {
  RetryPolicy,
  ExponentialBackoff,
  FallbackStrategy,
  GracefulDegradation,

  // Factory functions for common use cases

  /**
   * Create retry policy with default configuration
   * @param {Object} [options] - Override options
   * @returns {RetryPolicy} Configured retry policy
   */
  createRetryPolicy: (options) => new RetryPolicy(options),

  /**
   * Create exponential backoff strategy
   * @param {Object} [options] - Configuration options
   * @returns {ExponentialBackoff} Backoff strategy instance
   */
  createExponentialBackoff: (options) => new ExponentialBackoff(options),

  /**
   * Create fallback strategy chain
   * @param {Array<Function>} strategies - Strategy functions
   * @param {Object} [options] - Configuration options
   * @returns {FallbackStrategy} Fallback strategy instance
   */
  createFallbackStrategy: (strategies, options) => new FallbackStrategy(strategies, options),

  /**
   * Create graceful degradation handler
   * @param {Object} modes - Degradation modes
   * @param {Object} [options] - Configuration options
   * @returns {GracefulDegradation} Degradation handler instance
   */
  createGracefulDegradation: (modes, options) => new GracefulDegradation(modes, options),

  /**
   * Create integrated recovery system (all components together)
   * @param {Object} [options] - Configuration options
   * @returns {Object} Integrated recovery system
   */
  createIntegratedRecovery: (options) => ({
    retry: new RetryPolicy(options?.retry),
    backoff: new ExponentialBackoff(options?.backoff),
    fallback: new FallbackStrategy([], options?.fallback),
    degrade: new GracefulDegradation({ full: {}, partial: {} }, options?.degrade)
  })
};
