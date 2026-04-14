/**
 * HELIOS v4.0 - Rate Limiting Module
 * Production-ready rate limiting with multiple algorithms
 * @module feat-ratelimit/implementation
 */

/**
 * Token Bucket rate limiter
 * Uses a bucket with fixed capacity that refills at a constant rate
 * Best for burst traffic tolerance
 * @class TokenBucket
 */
class TokenBucket {
  /**
   * Creates a new TokenBucket instance
   * @param {number} capacity - Maximum number of tokens the bucket can hold
   * @param {number} refillRate - Tokens added per second
   * @param {number} [initialTokens=capacity] - Initial token count
   * @throws {Error} If capacity or refillRate are invalid
   */
  constructor(capacity, refillRate, initialTokens = capacity) {
    if (capacity <= 0 || !Number.isFinite(capacity)) {
      throw new Error('Capacity must be a positive finite number');
    }
    if (refillRate <= 0 || !Number.isFinite(refillRate)) {
      throw new Error('Refill rate must be a positive finite number');
    }
    
    this.capacity = capacity;
    this.refillRate = refillRate;
    this.tokens = Math.min(initialTokens, capacity);
    this.lastRefillTime = Date.now();
  }

  /**
   * Refill bucket based on elapsed time
   * @private
   * @returns {void}
   */
  _refill() {
    const now = Date.now();
    const elapsed = (now - this.lastRefillTime) / 1000;
    const tokensToAdd = elapsed * this.refillRate;
    
    this.tokens = Math.min(this.capacity, this.tokens + tokensToAdd);
    this.lastRefillTime = now;
  }

  /**
   * Consume tokens from bucket
   * @param {number} [count=1] - Number of tokens to consume
   * @returns {boolean} True if tokens were available and consumed
   * @throws {Error} If count is invalid
   */
  consume(count = 1) {
    if (count <= 0 || !Number.isFinite(count)) {
      throw new Error('Token count must be positive');
    }
    
    this._refill();
    
    if (this.tokens >= count) {
      this.tokens -= count;
      return true;
    }
    return false;
  }

  /**
   * Get remaining tokens without consuming
   * @returns {number} Current token count
   */
  getTokens() {
    this._refill();
    return this.tokens;
  }

  /**
   * Get time until tokens are available
   * @param {number} [count=1] - Number of tokens needed
   * @returns {number} Milliseconds until available, 0 if immediately available
   */
  getWaitTime(count = 1) {
    this._refill();
    if (this.tokens >= count) return 0;
    
    const needed = count - this.tokens;
    return Math.ceil((needed / this.refillRate) * 1000);
  }

  /**
   * Reset bucket to initial state
   * @returns {void}
   */
  reset() {
    this.tokens = this.capacity;
    this.lastRefillTime = Date.now();
  }

  /**
   * Get bucket statistics
   * @returns {Object} Statistics object with capacity, tokens, and refillRate
   */
  getStats() {
    this._refill();
    return {
      capacity: this.capacity,
      tokens: this.tokens,
      refillRate: this.refillRate,
      usage: (this.capacity - this.tokens) / this.capacity
    };
  }
}

/**
 * Sliding Window rate limiter
 * Tracks requests within a rolling time window
 * More precise than fixed windows, no boundary issues
 * @class SlidingWindow
 */
class SlidingWindow {
  /**
   * Creates a new SlidingWindow instance
   * @param {number} windowSize - Time window in milliseconds
   * @param {number} maxRequests - Maximum requests allowed in window
   * @throws {Error} If parameters are invalid
   */
  constructor(windowSize, maxRequests) {
    if (windowSize <= 0 || !Number.isFinite(windowSize)) {
      throw new Error('Window size must be a positive finite number');
    }
    if (maxRequests <= 0 || !Number.isFinite(maxRequests)) {
      throw new Error('Max requests must be positive');
    }
    
    this.windowSize = windowSize;
    this.maxRequests = maxRequests;
    this.requests = [];
  }

  /**
   * Clean up expired requests
   * @private
   * @returns {void}
   */
  _cleanup() {
    const now = Date.now();
    const cutoff = now - this.windowSize;
    this.requests = this.requests.filter(time => time > cutoff);
  }

  /**
   * Check if request is allowed
   * @returns {boolean} True if within limits
   */
  isAllowed() {
    this._cleanup();
    return this.requests.length < this.maxRequests;
  }

  /**
   * Record a request
   * @returns {boolean} True if request was recorded (allowed)
   */
  record() {
    if (this.isAllowed()) {
      this.requests.push(Date.now());
      return true;
    }
    return false;
  }

  /**
   * Get request count in current window
   * @returns {number} Number of requests
   */
  getCount() {
    this._cleanup();
    return this.requests.length;
  }

  /**
   * Get time until next request is allowed
   * @returns {number} Milliseconds to wait, 0 if immediate
   */
  getWaitTime() {
    this._cleanup();
    if (this.requests.length < this.maxRequests) return 0;
    
    const oldestRequest = this.requests[0];
    const availableAt = oldestRequest + this.windowSize;
    return Math.max(0, availableAt - Date.now());
  }

  /**
   * Reset the window
   * @returns {void}
   */
  reset() {
    this.requests = [];
  }

  /**
   * Get window statistics
   * @returns {Object} Statistics including requests count and wait time
   */
  getStats() {
    this._cleanup();
    return {
      windowSize: this.windowSize,
      maxRequests: this.maxRequests,
      currentCount: this.requests.length,
      remaining: this.maxRequests - this.requests.length,
      waitTime: this.getWaitTime()
    };
  }
}

/**
 * Distributed Rate Limiter
 * Uses Redis-like distributed backend for multi-instance coordination
 * @class DistributedLimiter
 */
class DistributedLimiter {
  /**
   * Creates a new DistributedLimiter
   * @param {Object} backend - Backend storage (simulated Redis)
   * @param {string} keyPrefix - Prefix for all keys
   * @param {number} windowSize - Window size in milliseconds
   * @param {number} maxRequests - Max requests per window
   */
  constructor(backend, keyPrefix = 'ratelimit:', windowSize = 60000, maxRequests = 100) {
    this.backend = backend || new Map();
    this.keyPrefix = keyPrefix;
    this.windowSize = windowSize;
    this.maxRequests = maxRequests;
  }

  /**
   * Check and increment counter for identifier
   * @param {string} identifier - Unique identifier (user ID, IP, etc)
   * @returns {Object} Result with allowed, remaining, resetTime
   */
  checkLimit(identifier) {
    const key = `${this.keyPrefix}${identifier}`;
    const now = Date.now();
    
    let data = this.backend.get(key);
    
    if (!data || data.windowStart + this.windowSize < now) {
      data = { count: 0, windowStart: now };
    }
    
    const allowed = data.count < this.maxRequests;
    
    if (allowed) {
      data.count++;
      this.backend.set(key, data);
    }
    
    return {
      allowed,
      remaining: Math.max(0, this.maxRequests - data.count),
      resetTime: data.windowStart + this.windowSize,
      retryAfter: allowed ? 0 : (data.windowStart + this.windowSize - now)
    };
  }

  /**
   * Reset limit for identifier
   * @param {string} identifier - Unique identifier
   * @returns {void}
   */
  reset(identifier) {
    const key = `${this.keyPrefix}${identifier}`;
    this.backend.delete(key);
  }

  /**
   * Get current status for identifier
   * @param {string} identifier - Unique identifier
   * @returns {Object} Current status
   */
  getStatus(identifier) {
    const key = `${this.keyPrefix}${identifier}`;
    const data = this.backend.get(key);
    
    if (!data) {
      return {
        count: 0,
        remaining: this.maxRequests,
        resetTime: Date.now() + this.windowSize
      };
    }
    
    return {
      count: data.count,
      remaining: this.maxRequests - data.count,
      resetTime: data.windowStart + this.windowSize
    };
  }

  /**
   * Cleanup expired entries
   * @returns {number} Number of entries cleaned
   */
  cleanup() {
    const now = Date.now();
    let count = 0;
    
    for (const [key, data] of this.backend.entries()) {
      if (data.windowStart + this.windowSize < now) {
        this.backend.delete(key);
        count++;
      }
    }
    
    return count;
  }

  /**
   * Get all active limiters
   * @returns {Map} Map of all active limiters
   */
  getAll() {
    return new Map(this.backend);
  }
}

/**
 * Quota Manager
 * Manages usage quotas with billing cycles and limits
 * @class QuotaManager
 */
class QuotaManager {
  /**
   * Creates a new QuotaManager
   * @param {Object} [storage=Map] - Storage backend
   * @param {number} [billingCycleDays=30] - Days in billing cycle
   */
  constructor(storage, billingCycleDays = 30) {
    this.storage = storage || new Map();
    this.billingCycleDays = billingCycleDays;
    this.quotas = new Map();
  }

  /**
   * Set quota for user
   * @param {string} userId - User identifier
   * @param {number} limit - Request limit per cycle
   * @param {number} [softLimit=limit*0.8] - Warning threshold
   * @returns {void}
   */
  setQuota(userId, limit, softLimit = Math.ceil(limit * 0.8)) {
    if (limit <= 0 || !Number.isFinite(limit)) {
      throw new Error('Limit must be positive');
    }
    
    this.quotas.set(userId, {
      limit,
      softLimit,
      cycleStart: this._getCycleStart(),
      used: 0,
      lastReset: Date.now()
    });
  }

  /**
   * Get current billing cycle start
   * @private
   * @returns {number} Timestamp of cycle start
   */
  _getCycleStart() {
    const now = new Date();
    const start = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const dayOfMonth = start.getDate();
    const cycleDayOffset = Math.floor(dayOfMonth / this.billingCycleDays);
    start.setDate(1 + (cycleDayOffset * this.billingCycleDays));
    return start.getTime();
  }

  /**
   * Check if cycle needs reset
   * @private
   * @param {Object} quota - Quota object
   * @returns {void}
   */
  _checkCycleReset(quota) {
    const cycleStart = this._getCycleStart();
    if (quota.cycleStart !== cycleStart) {
      quota.cycleStart = cycleStart;
      quota.used = 0;
      quota.lastReset = Date.now();
    }
  }

  /**
   * Consume quota for user
   * @param {string} userId - User identifier
   * @param {number} [amount=1] - Amount to consume
   * @returns {Object} Result with allowed, remaining, status
   */
  consume(userId, amount = 1) {
    if (!this.quotas.has(userId)) {
      throw new Error(`No quota set for user: ${userId}`);
    }
    
    const quota = this.quotas.get(userId);
    this._checkCycleReset(quota);
    
    const wouldExceed = quota.used + amount > quota.limit;
    const status = quota.used >= quota.softLimit ? 'warning' : 'ok';
    
    if (!wouldExceed) {
      quota.used += amount;
    }
    
    return {
      allowed: !wouldExceed,
      used: quota.used,
      remaining: quota.limit - quota.used,
      limit: quota.limit,
      status,
      cycleResetAt: quota.cycleStart + (this.billingCycleDays * 24 * 60 * 60 * 1000)
    };
  }

  /**
   * Get quota usage for user
   * @param {string} userId - User identifier
   * @returns {Object} Quota information
   */
  getQuota(userId) {
    if (!this.quotas.has(userId)) {
      throw new Error(`No quota set for user: ${userId}`);
    }
    
    const quota = this.quotas.get(userId);
    this._checkCycleReset(quota);
    
    return {
      limit: quota.limit,
      used: quota.used,
      remaining: quota.limit - quota.used,
      percentage: Math.round((quota.used / quota.limit) * 100),
      softLimit: quota.softLimit,
      status: quota.used >= quota.softLimit ? 'warning' : 'ok',
      cycleStart: quota.cycleStart,
      cycleResetAt: quota.cycleStart + (this.billingCycleDays * 24 * 60 * 60 * 1000),
      lastReset: quota.lastReset
    };
  }

  /**
   * Reset quota for user
   * @param {string} userId - User identifier
   * @returns {void}
   */
  resetQuota(userId) {
    if (this.quotas.has(userId)) {
      const quota = this.quotas.get(userId);
      quota.used = 0;
      quota.cycleStart = this._getCycleStart();
      quota.lastReset = Date.now();
    }
  }

  /**
   * Get all quotas
   * @returns {Map} Map of all quotas
   */
  getAllQuotas() {
    return new Map(this.quotas);
  }

  /**
   * Get users exceeding soft limit
   * @returns {Array} Array of user IDs with warnings
   */
  getWarnings() {
    const warnings = [];
    for (const [userId, quota] of this.quotas) {
      this._checkCycleReset(quota);
      if (quota.used >= quota.softLimit) {
        warnings.push({
          userId,
          used: quota.used,
          limit: quota.limit,
          percentage: Math.round((quota.used / quota.limit) * 100)
        });
      }
    }
    return warnings;
  }
}

module.exports = {
  TokenBucket,
  SlidingWindow,
  DistributedLimiter,
  QuotaManager
};
