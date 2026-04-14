/**
 * HELIOS v4.0 Health Checks Module - Public API
 * @module feat-health
 */

const {
  LivenessProbe,
  ReadinessCheck,
  CircuitBreakerIntegration
} = require('./implementation');

/**
 * Create and configure a health check system
 * @param {Object} options - Configuration
 * @param {Function} options.onLivenessChange - Liveness state change callback
 * @param {Function} options.onReadinessChange - Readiness change callback
 * @param {Function} options.onCircuitChange - Circuit breaker change callback
 * @returns {Object} Health check system
 */
function createHealthSystem(options = {}) {
  const liveness = new LivenessProbe({
    onStateChange: options.onLivenessChange
  });

  const readiness = new ReadinessCheck({
    onReadinessChange: options.onReadinessChange
  });

  const circuitBreaker = new CircuitBreakerIntegration({
    onStateChange: options.onCircuitChange
  });

  return {
    liveness,
    readiness,
    circuitBreaker,
    getStatus: () => ({
      liveness: liveness.getStatus(),
      readiness: readiness.getStatus(),
      circuitBreaker: circuitBreaker.getStatus()
    }),
    shutdown: () => {
      liveness.reset();
      readiness.reset();
      circuitBreaker.reset();
    }
  };
}

module.exports = {
  LivenessProbe,
  ReadinessCheck,
  CircuitBreakerIntegration,
  createHealthSystem
};
