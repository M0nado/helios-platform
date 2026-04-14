/**
 * HELIOS REST API - Deep Specialist (Depth 3)
 * Agent 4: Features & Monitoring
 * 
 * Responsible for:
 * - Error recovery & handling
 * - Telemetry collection
 * - Health checks
 * - Observability
 * 
 * @module features
 * @version 1.0.0
 */

/**
 * Features & Monitoring Manager
 * Handles features, error recovery, and monitoring
 * @class Features
 */
class Features {
  /**
   * Initialize Features Manager
   * @param {Object} config - Configuration
   * @param {Object} config.monitoring - Monitoring config
   */
  constructor(config = {}) {
    this.monitoringConfig = config.monitoring || { serviceName: 'rest-api' };
    
    this.metrics = {
      requests: 0,
      errors: 0,
      cacheHits: 0,
      cacheMisses: 0,
      avgResponseTime: 0,
      responseTimes: []
    };
    
    this.circuitBreaker = {
      failures: 0,
      threshold: 5,
      timeout: 60000,
      lastFailureTime: null,
      state: 'closed'
    };
  }

  /**
   * Format success response
   * @param {any} data - Response data
   * @param {Object} options - Options
   * @returns {Object} Response
   */
  formatSuccessResponse(data, options = {}) {
    return {
      success: true,
      status: options.status || 200,
      data,
      timestamp: new Date().toISOString(),
      cached: options.cached || false
    };
  }

  /**
   * Format error response
   * @param {number} status - HTTP status
   * @param {string} message - Error message
   * @param {Object} details - Details
   * @returns {Object} Response
   */
  formatErrorResponse(status, message, details = {}) {
    return {
      success: false,
      status,
      error: {
        message,
        code: `ERR_${status}`,
        details
      },
      timestamp: new Date().toISOString()
    };
  }

  /**
   * Track metrics
   * @param {number} duration - Duration in ms
   * @param {boolean} isError - Is error
   */
  trackMetrics(duration, isError = false) {
    this.metrics.requests++;
    if (isError) {
      this.metrics.errors++;
    }
    this.metrics.responseTimes.push(duration);
    this.metrics.avgResponseTime = 
      this.metrics.responseTimes.reduce((a, b) => a + b, 0) / 
      this.metrics.responseTimes.length;
  }

  /**
   * Track cache hit
   */
  trackCacheHit() {
    this.metrics.cacheHits++;
  }

  /**
   * Track cache miss
   */
  trackCacheMiss() {
    this.metrics.cacheMisses++;
  }

  /**
   * Check circuit breaker
   * @returns {boolean} Circuit is open
   */
  isCircuitOpen() {
    if (this.circuitBreaker.state === 'open') {
      const timeSinceFailure = Date.now() - this.circuitBreaker.lastFailureTime;
      if (timeSinceFailure > this.circuitBreaker.timeout) {
        this.circuitBreaker.state = 'half-open';
        this.circuitBreaker.failures = 0;
        return false;
      }
      return true;
    }
    return false;
  }

  /**
   * Record failure in circuit breaker
   */
  recordFailure() {
    this.circuitBreaker.failures++;
    this.circuitBreaker.lastFailureTime = Date.now();
    
    if (this.circuitBreaker.failures >= this.circuitBreaker.threshold) {
      this.circuitBreaker.state = 'open';
    }
  }

  /**
   * Record success in circuit breaker
   */
  recordSuccess() {
    if (this.circuitBreaker.state === 'half-open') {
      this.circuitBreaker.state = 'closed';
      this.circuitBreaker.failures = 0;
    }
  }

  /**
   * Get metrics
   * @returns {Object} Metrics
   */
  getMetrics() {
    return {
      ...this.metrics,
      circuitBreakerState: this.circuitBreaker.state
    };
  }

  /**
   * Reset metrics
   */
  resetMetrics() {
    this.metrics = {
      requests: 0,
      errors: 0,
      cacheHits: 0,
      cacheMisses: 0,
      avgResponseTime: 0,
      responseTimes: []
    };
  }

  /**
   * Health check
   * @returns {Object} Health status
   */
  healthCheck() {
    return {
      status: this.circuitBreaker.state === 'closed' ? 'healthy' : 'degraded',
      circuitBreaker: this.circuitBreaker.state,
      uptime: Date.now(),
      metrics: {
        totalRequests: this.metrics.requests,
        errorRate: this.metrics.requests > 0 ? 
          (this.metrics.errors / this.metrics.requests * 100).toFixed(2) + '%' : '0%',
        avgResponseTime: this.metrics.avgResponseTime.toFixed(2) + 'ms'
      }
    };
  }
}

module.exports = Features;
