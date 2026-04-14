/**
 * @fileoverview mod-breaker Public API
 * Circuit Breaker module for HELIOS v4.0 Fleet Expansion
 * 
 * @module mod-breaker
 * @exports {CircuitBreaker, StateTransitioner, ThresholdMonitor, RecoveryManager, CircuitState, RecoveryStrategy}
 */

const {
  CircuitBreaker,
  StateTransitioner,
  ThresholdMonitor,
  RecoveryManager,
  CircuitState,
  RecoveryStrategy
} = require('./implementation');

/**
 * Creates a new circuit breaker instance with default configuration
 * @function
 * @param {object} options - Configuration options
 * @param {string} options.name - Circuit breaker name
 * @param {number} options.failureThreshold - Failures to open (default: 5)
 * @param {number} options.successThreshold - Successes to close (default: 3)
 * @param {number} options.timeout - Request timeout ms (default: 5000)
 * @returns {CircuitBreaker} New circuit breaker instance
 * @example
 * const breaker = createBreaker({ name: 'api-service', timeout: 10000 });
 */
function createBreaker(options = {}) {
  return new CircuitBreaker(options);
}

module.exports = {
  // Classes
  CircuitBreaker,
  StateTransitioner,
  ThresholdMonitor,
  RecoveryManager,

  // Enums
  CircuitState,
  RecoveryStrategy,

  // Factory functions
  createBreaker,

  // Metadata
  version: '4.0.0',
  name: 'mod-breaker',
  description: 'Circuit breaker pattern implementation for HELIOS v4.0'
};
