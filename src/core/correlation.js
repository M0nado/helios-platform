/**
 * Request Correlation System - Unified request tracking across services
 * Enables distributed tracing and debugging with correlation IDs
 * @module core/correlation
 */

const crypto = require('crypto');

/**
 * CorrelationIDGenerator - Generate unique IDs per request
 * @class
 */
class CorrelationIDGenerator {
  constructor() {
    this.prefix = 'corr';
    this.counter = 0;
  }

  /**
   * Generate unique correlation ID
   * @returns {string} Correlation ID
   */
  generate() {
    const timestamp = Date.now().toString(36);
    const random = crypto.randomBytes(8).toString('hex');
    const count = (++this.counter).toString(36);
    return `${this.prefix}-${timestamp}-${random}-${count}`;
  }

  /**
   * Validate correlation ID format
   * @param {string} correlationId - ID to validate
   * @returns {boolean} Valid status
   */
  validate(correlationId) {
    const pattern = /^corr-[a-z0-9]+-[a-f0-9]+-[a-z0-9]+$/;
    return pattern.test(correlationId);
  }

  /**
   * Parse correlation ID components
   * @param {string} correlationId - ID to parse
   * @returns {Object} Parsed components
   */
  parse(correlationId) {
    const parts = correlationId.split('-');
    if (parts.length < 4) return null;

    return {
      prefix: parts[0],
      timestamp: parseInt(parts[1], 36),
      random: parts[2],
      counter: parseInt(parts[3], 36),
    };
  }
}

/**
 * TraceIDPropagation - Pass trace IDs through service calls
 * @class
 */
class TraceIDPropagation {
  constructor() {
    this.traceBag = new Map();
    this.traceContext = new WeakMap();
  }

  /**
   * Create trace context for request
   * @param {string} correlationId - Correlation ID
   * @param {Object} metadata - Trace metadata
   * @returns {Object} Trace context
   */
  createContext(correlationId, metadata = {}) {
    const context = {
      correlationId,
      traceId: crypto.randomUUID(),
      spanId: crypto.randomUUID(),
      parentSpanId: null,
      metadata,
      createdAt: Date.now(),
      service: 'unknown',
    };

    this.traceBag.set(correlationId, context);
    return context;
  }

  /**
   * Create child span from parent context
   * @param {string} correlationId - Correlation ID
   * @param {string} service - Service name
   * @returns {Object} Child span context
   */
  createChildSpan(correlationId, service) {
    const parentContext = this.traceBag.get(correlationId);
    if (!parentContext) {
      return this.createContext(correlationId, { service });
    }

    const childSpan = {
      ...parentContext,
      spanId: crypto.randomUUID(),
      parentSpanId: parentContext.spanId,
      service,
      createdAt: Date.now(),
    };

    this.traceBag.set(correlationId, childSpan);
    return childSpan;
  }

  /**
   * Get trace context
   * @param {string} correlationId - Correlation ID
   * @returns {Object|null} Trace context
   */
  getContext(correlationId) {
    return this.traceBag.get(correlationId) || null;
  }

  /**
   * Extract trace headers for outgoing request
   * @param {string} correlationId - Correlation ID
   * @returns {Object} Headers to include in request
   */
  extractHeaders(correlationId) {
    const context = this.getContext(correlationId);
    if (!context) return {};

    return {
      'x-correlation-id': correlationId,
      'x-trace-id': context.traceId,
      'x-span-id': context.spanId,
      'x-parent-span-id': context.parentSpanId,
    };
  }

  /**
   * Inject trace headers from incoming request
   * @param {Object} headers - Request headers
   * @returns {Object} Extracted trace info
   */
  injectHeaders(headers) {
    return {
      correlationId: headers['x-correlation-id'] || null,
      traceId: headers['x-trace-id'] || null,
      spanId: headers['x-span-id'] || null,
      parentSpanId: headers['x-parent-span-id'] || null,
    };
  }

  /**
   * Clean up old trace contexts
   * @param {number} maxAgeMs - Maximum age in milliseconds
   * @returns {number} Cleaned count
   */
  cleanup(maxAgeMs = 3600000) {
    const now = Date.now();
    let count = 0;

    for (const [correlationId, context] of this.traceBag.entries()) {
      if (now - context.createdAt > maxAgeMs) {
        this.traceBag.delete(correlationId);
        count++;
      }
    }

    return count;
  }
}

/**
 * RequestLifecycleTracker - Track request through stages
 * @class
 */
class RequestLifecycleTracker {
  constructor() {
    this.requests = new Map();
  }

  /**
   * Stage types
   * @readonly
   */
  static STAGES = {
    RECEIVED: 'received',
    VALIDATED: 'validated',
    PROCESSING: 'processing',
    RESPONSE_PREPARED: 'response_prepared',
    DELIVERED: 'delivered',
  };

  /**
   * Start tracking request
   * @param {string} correlationId - Correlation ID
   * @param {Object} metadata - Request metadata
   * @returns {Object} Lifecycle tracker
   */
  startTracking(correlationId, metadata = {}) {
    const tracker = {
      correlationId,
      stages: [{ stage: RequestLifecycleTracker.STAGES.RECEIVED, timestamp: Date.now() }],
      metadata,
      duration: 0,
      errors: [],
    };

    this.requests.set(correlationId, tracker);
    return tracker;
  }

  /**
   * Mark stage completion
   * @param {string} correlationId - Correlation ID
   * @param {string} stage - Stage name
   * @param {Object} data - Stage data
   */
  markStage(correlationId, stage, data = {}) {
    const tracker = this.requests.get(correlationId);
    if (!tracker) return;

    tracker.stages.push({
      stage,
      timestamp: Date.now(),
      data,
    });
  }

  /**
   * Record error in lifecycle
   * @param {string} correlationId - Correlation ID
   * @param {Error} error - Error object
   */
  recordError(correlationId, error) {
    const tracker = this.requests.get(correlationId);
    if (!tracker) return;

    tracker.errors.push({
      message: error.message,
      stack: error.stack,
      timestamp: Date.now(),
    });
  }

  /**
   * Complete tracking
   * @param {string} correlationId - Correlation ID
   * @returns {Object} Complete lifecycle data
   */
  completeTracking(correlationId) {
    const tracker = this.requests.get(correlationId);
    if (!tracker) return null;

    const firstStage = tracker.stages[0];
    const lastStage = tracker.stages[tracker.stages.length - 1];
    tracker.duration = lastStage.timestamp - firstStage.timestamp;

    return tracker;
  }

  /**
   * Get request lifecycle
   * @param {string} correlationId - Correlation ID
   * @returns {Object|null} Lifecycle data
   */
  getLifecycle(correlationId) {
    return this.requests.get(correlationId) || null;
  }

  /**
   * Get stats for requests
   * @returns {Object} Statistics
   */
  getStats() {
    const lifecycles = Array.from(this.requests.values());
    const durations = lifecycles.map(l => l.duration || 0);
    const avgDuration = durations.length > 0 ? durations.reduce((a, b) => a + b, 0) / durations.length : 0;

    return {
      total: lifecycles.length,
      avgDuration: Math.round(avgDuration),
      maxDuration: Math.max(...durations, 0),
      minDuration: durations.length > 0 ? Math.min(...durations) : 0,
      errorsCount: lifecycles.reduce((sum, l) => sum + l.errors.length, 0),
    };
  }

  /**
   * Clean up old requests
   * @param {number} maxAgeMs - Maximum age in milliseconds
   * @returns {number} Cleaned count
   */
  cleanup(maxAgeMs = 3600000) {
    const now = Date.now();
    let count = 0;

    for (const [correlationId, tracker] of this.requests.entries()) {
      if (tracker.stages.length > 0) {
        const lastStage = tracker.stages[tracker.stages.length - 1];
        if (now - lastStage.timestamp > maxAgeMs) {
          this.requests.delete(correlationId);
          count++;
        }
      }
    }

    return count;
  }
}

/**
 * DistributedTracingHooks - OpenTelemetry integration
 * @class
 */
class DistributedTracingHooks {
  constructor() {
    this.hooks = new Map();
    this.tracingEnabled = false;
  }

  /**
   * Enable distributed tracing
   */
  enable() {
    this.tracingEnabled = true;
  }

  /**
   * Disable distributed tracing
   */
  disable() {
    this.tracingEnabled = false;
  }

  /**
   * Register hook
   * @param {string} hookName - Hook name
   * @param {Function} callback - Callback function
   */
  registerHook(hookName, callback) {
    if (!this.hooks.has(hookName)) {
      this.hooks.set(hookName, []);
    }
    this.hooks.get(hookName).push(callback);
  }

  /**
   * Execute hooks
   * @param {string} hookName - Hook name
   * @param {Object} context - Execution context
   */
  async executeHooks(hookName, context) {
    if (!this.tracingEnabled) return;

    const hookCallbacks = this.hooks.get(hookName) || [];
    for (const callback of hookCallbacks) {
      try {
        await callback(context);
      } catch (error) {
        console.error(`Hook ${hookName} failed:`, error.message);
      }
    }
  }

  /**
   * Create span for operation
   * @param {string} operationName - Operation name
   * @param {Function} operation - Operation function
   * @param {string} correlationId - Correlation ID
   * @returns {Promise} Operation result
   */
  async createSpan(operationName, operation, correlationId) {
    const span = {
      operationName,
      correlationId,
      startTime: Date.now(),
      attributes: {},
    };

    await this.executeHooks('span:start', span);

    try {
      const result = await operation();
      span.status = 'success';
      return result;
    } catch (error) {
      span.status = 'error';
      span.error = error.message;
      throw error;
    } finally {
      span.endTime = Date.now();
      span.duration = span.endTime - span.startTime;
      await this.executeHooks('span:end', span);
    }
  }
}

/**
 * LogCorrelation - Automatically correlate logs
 * @class
 */
class LogCorrelation {
  constructor() {
    this.correlationMap = new Map();
    this.logBuffer = [];
    this.maxBufferSize = 1000;
  }

  /**
   * Correlate log with request
   * @param {string} correlationId - Correlation ID
   * @param {Object} logEntry - Log entry
   */
  correlateLog(correlationId, logEntry) {
    const correlatedLog = {
      ...logEntry,
      correlationId,
      timestamp: Date.now(),
    };

    this.logBuffer.push(correlatedLog);

    if (this.logBuffer.length > this.maxBufferSize) {
      this.logBuffer.shift();
    }

    if (!this.correlationMap.has(correlationId)) {
      this.correlationMap.set(correlationId, []);
    }
    this.correlationMap.get(correlationId).push(correlatedLog);
  }

  /**
   * Get logs for correlation ID
   * @param {string} correlationId - Correlation ID
   * @returns {Array} Logs
   */
  getLogsForCorrelation(correlationId) {
    return this.correlationMap.get(correlationId) || [];
  }

  /**
   * Create logging middleware
   * @param {string} correlationIdHeader - Header name for correlation ID
   * @returns {Function} Middleware function
   */
  middleware(correlationIdHeader = 'x-correlation-id') {
    return (req, res, next) => {
      const correlationId = req.headers[correlationIdHeader] || crypto.randomUUID();
      req.correlationId = correlationId;

      const originalLog = console.log;
      console.log = (...args) => {
        this.correlateLog(correlationId, {
          level: 'info',
          message: args.join(' '),
          context: 'console.log',
        });
        originalLog(...args);
      };

      res.on('finish', () => {
        console.log = originalLog;
      });

      next();
    };
  }

  /**
   * Export logs for analysis
   * @param {string} correlationId - Correlation ID
   * @returns {Object} Exported logs
   */
  exportLogs(correlationId) {
    const logs = this.getLogsForCorrelation(correlationId);
    return {
      correlationId,
      logCount: logs.length,
      logs,
      firstLog: logs[0]?.timestamp,
      lastLog: logs[logs.length - 1]?.timestamp,
      duration: logs.length > 0 ? logs[logs.length - 1].timestamp - logs[0].timestamp : 0,
    };
  }

  /**
   * Clear correlation logs
   * @param {string} correlationId - Correlation ID
   */
  clearCorrelation(correlationId) {
    this.correlationMap.delete(correlationId);
  }
}

module.exports = {
  CorrelationIDGenerator,
  TraceIDPropagation,
  RequestLifecycleTracker,
  DistributedTracingHooks,
  LogCorrelation,
};
