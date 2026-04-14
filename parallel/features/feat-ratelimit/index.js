/**
 * HELIOS v4.0 - Rate Limiting Module Public API
 * @module feat-ratelimit
 */

const {
  TokenBucket,
  SlidingWindow,
  DistributedLimiter,
  QuotaManager
} = require('./implementation');

/**
 * Create a token bucket limiter with sensible defaults
 * @param {number} capacity - Bucket capacity
 * @param {number} refillRate - Tokens per second
 * @returns {TokenBucket} Configured token bucket instance
 */
const createTokenBucket = (capacity, refillRate) => {
  return new TokenBucket(capacity, refillRate);
};

/**
 * Create a sliding window limiter
 * @param {number} windowMs - Window size in milliseconds (default: 60000)
 * @param {number} maxRequests - Max requests per window (default: 100)
 * @returns {SlidingWindow} Configured sliding window instance
 */
const createSlidingWindow = (windowMs = 60000, maxRequests = 100) => {
  return new SlidingWindow(windowMs, maxRequests);
};

/**
 * Create a distributed rate limiter
 * @param {Object} [backend] - Storage backend (optional)
 * @returns {DistributedLimiter} Configured distributed limiter
 */
const createDistributedLimiter = (backend) => {
  return new DistributedLimiter(backend);
};

/**
 * Create a quota manager
 * @param {Object} [storage] - Storage backend (optional)
 * @returns {QuotaManager} Configured quota manager
 */
const createQuotaManager = (storage) => {
  return new QuotaManager(storage);
};

/**
 * Express-like middleware factory for token bucket rate limiting
 * @param {TokenBucket} bucket - Token bucket instance
 * @param {number} [tokensPerRequest=1] - Tokens consumed per request
 * @returns {Function} Middleware function
 */
const createTokenBucketMiddleware = (bucket, tokensPerRequest = 1) => {
  return (req, res, next) => {
    if (bucket.consume(tokensPerRequest)) {
      next();
    } else {
      const waitTime = bucket.getWaitTime(tokensPerRequest);
      res.status(429).json({
        error: 'Rate limit exceeded',
        retryAfter: Math.ceil(waitTime / 1000)
      });
    }
  };
};

/**
 * Express-like middleware factory for distributed rate limiting
 * @param {DistributedLimiter} limiter - Distributed limiter instance
 * @param {Function} [identifierFn] - Function to extract identifier from request
 * @returns {Function} Middleware function
 */
const createDistributedMiddleware = (limiter, identifierFn = (req) => req.ip) => {
  return (req, res, next) => {
    const identifier = identifierFn(req);
    const result = limiter.checkLimit(identifier);
    
    res.set('X-RateLimit-Limit', limiter.maxRequests);
    res.set('X-RateLimit-Remaining', result.remaining);
    res.set('X-RateLimit-Reset', Math.ceil(result.resetTime / 1000));
    
    if (!result.allowed) {
      res.status(429).json({
        error: 'Rate limit exceeded',
        retryAfter: Math.ceil(result.retryAfter / 1000)
      });
    } else {
      next();
    }
  };
};

module.exports = {
  TokenBucket,
  SlidingWindow,
  DistributedLimiter,
  QuotaManager,
  createTokenBucket,
  createSlidingWindow,
  createDistributedLimiter,
  createQuotaManager,
  createTokenBucketMiddleware,
  createDistributedMiddleware
};
