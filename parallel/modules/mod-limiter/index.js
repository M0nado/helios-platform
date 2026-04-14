/**
 * HELIOS v4.0 Rate Limiter Module - Public API
 * @module mod-limiter
 * @version 4.0.0
 */

const {
  DistributedLimiter,
  QuotaEnforcer,
  MetricsTracker,
  MemoryBackend,
  RateLimitError,
  QuotaExceededError,
  QuotaError
} = require('./implementation');

/**
 * Create a complete rate limiting system
 * @param {Object} options - Configuration options
 * @returns {Object} Rate limiter system with all components
 */
function createRateLimiter(options = {}) {
  return {
    limiter: new DistributedLimiter(options.limiter),
    quota: new QuotaEnforcer(options.quota),
    metrics: new MetricsTracker(options.metrics)
  };
}

/**
 * Create a simple per-IP rate limiter
 * @param {Object} options - Configuration options
 * @returns {Object} Simple rate limiter with check method
 */
function createSimpleLimiter(options = {}) {
  const limiter = new DistributedLimiter({
    maxRequests: options.maxRequests || 100,
    windowMs: options.windowMs || 60000,
    identifierFn: (req) => req.ip || req.remoteAddress || 'unknown'
  });

  const metrics = new MetricsTracker();

  return {
    /**
     * Check if request is allowed
     * @async
     * @param {Object} req - Request object
     * @returns {Promise<Object>} Rate limit status
     */
    check: async (req) => {
      const status = await limiter.check(req);
      metrics.record({
        type: status.allowed ? 'allowed' : 'blocked',
        context: { ip: status.identifier }
      });
      return status;
    },

    /**
     * Get current status without incrementing
     * @async
     * @param {Object} req - Request object
     * @returns {Promise<Object>} Current status
     */
    status: (req) => limiter.status(req),

    /**
     * Reset limit for identifier
     * @async
     * @param {string} identifier - Identifier
     * @returns {Promise<void>}
     */
    reset: (identifier) => limiter.reset(identifier),

    /**
     * Get metrics
     * @returns {Object} Metrics summary
     */
    getMetrics: () => metrics.getSummary(),

    /**
     * Export metrics as JSON
     * @returns {Object} JSON metrics
     */
    exportMetrics: () => metrics.exportJSON(),

    /**
     * Export metrics as Prometheus
     * @returns {string} Prometheus format
     */
    exportPrometheus: () => metrics.exportPrometheus()
  };
}

/**
 * Create middleware for Express/Connect
 * @param {Object} options - Middleware options
 * @returns {Function} Express middleware function
 */
function createMiddleware(options = {}) {
  const limiter = new DistributedLimiter({
    maxRequests: options.maxRequests || 100,
    windowMs: options.windowMs || 60000
  });

  const metrics = new MetricsTracker();
  const onLimitExceeded = options.onLimitExceeded || null;

  return async (req, res, next) => {
    try {
      const status = await limiter.check(req);

      res.setHeader('X-RateLimit-Limit', status.limit);
      res.setHeader('X-RateLimit-Remaining', status.remaining);
      res.setHeader('X-RateLimit-Reset', Math.ceil(status.resetTime / 1000));

      metrics.record({
        type: status.allowed ? 'allowed' : 'blocked',
        context: { ip: status.identifier, path: req.path, method: req.method }
      });

      if (!status.allowed) {
        res.status(429);
        if (onLimitExceeded) {
          return onLimitExceeded(req, res, status);
        }
        return res.json({
          error: 'Too many requests',
          retryAfter: Math.ceil(status.resetIn / 1000)
        });
      }

      next();
    } catch (error) {
      next(error);
    }
  };
}

module.exports = {
  DistributedLimiter,
  QuotaEnforcer,
  MetricsTracker,
  MemoryBackend,
  RateLimitError,
  QuotaExceededError,
  QuotaError,
  createRateLimiter,
  createSimpleLimiter,
  createMiddleware,
  version: '4.0.0'
};
