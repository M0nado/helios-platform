/**
 * HELIOS v4.0 Telemetry Module - Public API
 * @module feat-telemetry
 */

const {
  RequestTracer,
  MetricsCollector,
  EventLogger,
  CorrelationManager
} = require('./implementation');

/**
 * Create and configure a telemetry system
 * @param {Object} options - Configuration
 * @param {string} options.serviceName - Service name
 * @param {number} options.samplingRate - Trace sampling (0-1)
 * @param {Function} options.onTraceComplete - Trace callback
 * @param {Function} options.onMetricReady - Metrics callback
 * @param {Function} options.onEventBatch - Event batch callback
 * @returns {Object} Telemetry system
 */
function createTelemetrySystem(options = {}) {
  const tracer = new RequestTracer({
    samplingRate: options.samplingRate,
    serviceName: options.serviceName,
    onTraceComplete: options.onTraceComplete
  });

  const metrics = new MetricsCollector({
    onMetricReady: options.onMetricReady
  });

  const logger = new EventLogger({
    onBatchReady: options.onEventBatch
  });

  const correlator = new CorrelationManager({
    idGenerator: options.idGenerator
  });

  return {
    tracer,
    metrics,
    logger,
    correlator,
    shutdown: () => {
      logger.stop();
      tracer.clearOldTraces();
      correlator.clearOldContexts();
    }
  };
}

module.exports = {
  RequestTracer,
  MetricsCollector,
  EventLogger,
  CorrelationManager,
  createTelemetrySystem
};
