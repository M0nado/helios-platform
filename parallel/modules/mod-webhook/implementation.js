/**
 * HELIOS Webhook Manager Module - Production Implementation
 * Provides webhook registration, signature verification, retries, and rate limiting
 * 
 * @module mod-webhook
 * @version 1.0.0
 */

const crypto = require('crypto');

/**
 * Webhook configuration
 * @typedef {Object} Webhook
 * @property {string} id - Webhook identifier
 * @property {string} url - Target URL
 * @property {string[]} events - Events to subscribe
 * @property {string} secret - HMAC secret
 * @property {boolean} active - Active status
 * @property {Object} metadata - Additional metadata
 * @property {number} createdAt - Creation timestamp
 */

/**
 * Signature verification using HMAC-SHA256
 * @class SignatureVerifier
 */
class SignatureVerifier {
  /**
   * Create signature verifier
   * @param {string} algorithm - Hash algorithm (default: 'sha256')
   * @throws {Error} If algorithm is unsupported
   */
  constructor(algorithm = 'sha256') {
    const supported = ['sha256', 'sha512', 'sha1'];
    if (!supported.includes(algorithm)) {
      throw new Error(`Unsupported algorithm: ${algorithm}`);
    }
    this.algorithm = algorithm;
  }

  /**
   * Generate HMAC signature
   * @param {*} payload - Data to sign
   * @param {string} secret - Secret key
   * @returns {string} Hex-encoded signature
   * @throws {Error} If payload or secret is invalid
   */
  sign(payload, secret) {
    if (!secret || typeof secret !== 'string') {
      throw new Error('Secret must be a non-empty string');
    }

    const data = typeof payload === 'string' ? payload : JSON.stringify(payload);
    return crypto
      .createHmac(this.algorithm, secret)
      .update(data)
      .digest('hex');
  }

  /**
   * Verify HMAC signature
   * @param {*} payload - Original data
   * @param {string} signature - Signature to verify
   * @param {string} secret - Secret key
   * @returns {boolean} True if signature is valid
   * @throws {Error} If inputs are invalid
   */
  verify(payload, signature, secret) {
    if (!signature || typeof signature !== 'string') {
      throw new Error('Signature must be a non-empty string');
    }

    const expected = this.sign(payload, secret);
    // Use constant-time comparison to prevent timing attacks
    return crypto.timingSafeEqual(
      Buffer.from(expected),
      Buffer.from(signature)
    );
  }

  /**
   * Generate random secret
   * @param {number} length - Secret length in bytes (default: 32)
   * @returns {string} Random hex string
   */
  generateSecret(length = 32) {
    return crypto.randomBytes(length).toString('hex');
  }
}

/**
 * Manages webhook retries with exponential backoff
 * @class RetryManager
 */
class RetryManager {
  /**
   * Create retry manager
   * @param {Object} options - Configuration options
   * @param {number} options.maxRetries - Max retry attempts (default: 5)
   * @param {number} options.initialDelay - Initial delay in ms (default: 1000)
   * @param {number} options.maxDelay - Maximum delay in ms (default: 300000)
   * @param {number} options.backoffMultiplier - Exponential backoff multiplier (default: 2)
   */
  constructor(options = {}) {
    this.maxRetries = options.maxRetries || 5;
    this.initialDelay = options.initialDelay || 1000;
    this.maxDelay = options.maxDelay || 300000;
    this.backoffMultiplier = options.backoffMultiplier || 2;
    this.attempts = new Map();
    this.timers = new Map();
  }

  /**
   * Calculate next retry delay
   * @param {string} webhookId - Webhook identifier
   * @param {number} attemptNumber - Current attempt number
   * @returns {number} Delay in milliseconds
   */
  getNextDelay(webhookId, attemptNumber) {
    const delay = this.initialDelay * Math.pow(this.backoffMultiplier, attemptNumber - 1);
    return Math.min(delay, this.maxDelay);
  }

  /**
   * Record retry attempt
   * @param {string} webhookId - Webhook identifier
   * @param {Object} event - Event data
   * @returns {Object} Retry record
   */
  recordAttempt(webhookId, event) {
    const key = `${webhookId}-${event.id || Date.now()}`;
    if (!this.attempts.has(key)) {
      this.attempts.set(key, {
        webhookId,
        eventId: event.id,
        attempts: 0,
        firstAttempt: Date.now(),
        lastAttempt: null
      });
    }

    const record = this.attempts.get(key);
    record.attempts++;
    record.lastAttempt = Date.now();
    return record;
  }

  /**
   * Check if should retry
   * @param {string} webhookId - Webhook identifier
   * @param {Object} event - Event data
   * @returns {boolean} True if should retry
   */
  shouldRetry(webhookId, event) {
    const key = `${webhookId}-${event.id || Date.now()}`;
    const record = this.attempts.get(key);
    return record && record.attempts <= this.maxRetries;
  }

  /**
   * Schedule retry with backoff
   * @param {string} webhookId - Webhook identifier
   * @param {Object} event - Event data
   * @param {Function} handler - Retry handler function
   * @returns {NodeJS.Timeout} Timer reference
   */
  scheduleRetry(webhookId, event, handler) {
    const key = `${webhookId}-${event.id || Date.now()}`;
    const record = this.recordAttempt(webhookId, event);
    const delay = this.getNextDelay(webhookId, record.attempts);

    // Clear existing timer if any
    if (this.timers.has(key)) {
      clearTimeout(this.timers.get(key));
    }

    const timer = setTimeout(() => {
      handler(record);
      this.timers.delete(key);
    }, delay);

    this.timers.set(key, timer);
    return timer;
  }

  /**
   * Cancel scheduled retry
   * @param {string} webhookId - Webhook identifier
   * @param {Object} event - Event data
   * @returns {boolean} True if cancelled
   */
  cancelRetry(webhookId, event) {
    const key = `${webhookId}-${event.id || Date.now()}`;
    if (this.timers.has(key)) {
      clearTimeout(this.timers.get(key));
      this.timers.delete(key);
      return true;
    }
    return false;
  }

  /**
   * Get retry record
   * @param {string} webhookId - Webhook identifier
   * @param {Object} event - Event data
   * @returns {Object|null} Retry record or null
   */
  getRecord(webhookId, event) {
    const key = `${webhookId}-${event.id || Date.now()}`;
    return this.attempts.get(key) || null;
  }

  /**
   * Clear retry history
   * @param {string} webhookId - Webhook identifier
   * @returns {number} Number of records cleared
   */
  clearHistory(webhookId) {
    const keys = Array.from(this.attempts.keys())
      .filter(k => k.startsWith(`${webhookId}-`));
    keys.forEach(k => {
      this.attempts.delete(k);
      if (this.timers.has(k)) {
        clearTimeout(this.timers.get(k));
        this.timers.delete(k);
      }
    });
    return keys.length;
  }
}

/**
 * Rate limiter for webhook deliveries
 * @class WebhookRateLimiter
 */
class WebhookRateLimiter {
  /**
   * Create rate limiter
   * @param {Object} options - Configuration options
   * @param {number} options.requestsPerSecond - Rate limit (default: 100)
   * @param {number} options.windowSize - Time window in ms (default: 1000)
   * @param {number} options.maxBurst - Maximum burst size (default: 10)
   */
  constructor(options = {}) {
    this.requestsPerSecond = options.requestsPerSecond || 100;
    this.windowSize = options.windowSize || 1000;
    this.maxBurst = options.maxBurst || 10;
    this.buckets = new Map();
    this.globalBucket = { tokens: this.maxBurst, lastRefill: Date.now() };
  }

  /**
   * Check if webhook can deliver
   * @param {string} webhookId - Webhook identifier
   * @returns {boolean} True if within rate limit
   */
  canDeliver(webhookId) {
    if (!this.buckets.has(webhookId)) {
      this.buckets.set(webhookId, {
        tokens: this.requestsPerSecond,
        lastRefill: Date.now()
      });
    }

    const bucket = this.buckets.get(webhookId);
    const now = Date.now();
    const timePassed = now - bucket.lastRefill;

    // Refill tokens based on elapsed time
    if (timePassed >= this.windowSize) {
      bucket.tokens = this.requestsPerSecond;
      bucket.lastRefill = now;
    } else {
      const tokensToAdd = (timePassed / this.windowSize) * this.requestsPerSecond;
      bucket.tokens = Math.min(this.requestsPerSecond, bucket.tokens + tokensToAdd);
    }

    // Check global limit
    this._refillGlobal();
    if (this.globalBucket.tokens < 1) {
      return false;
    }

    // Check per-webhook limit
    if (bucket.tokens >= 1) {
      bucket.tokens--;
      this.globalBucket.tokens--;
      return true;
    }

    return false;
  }

  /**
   * Refill global bucket
   * @private
   * @returns {void}
   */
  _refillGlobal() {
    const now = Date.now();
    const timePassed = now - this.globalBucket.lastRefill;

    if (timePassed >= this.windowSize) {
      this.globalBucket.tokens = this.maxBurst;
      this.globalBucket.lastRefill = now;
    } else {
      const tokensToAdd = (timePassed / this.windowSize) * this.maxBurst;
      this.globalBucket.tokens = Math.min(this.maxBurst, this.globalBucket.tokens + tokensToAdd);
    }
  }

  /**
   * Get rate limit status
   * @param {string} webhookId - Webhook identifier
   * @returns {Object} Status object
   */
  getStatus(webhookId) {
    this._refillGlobal();
    const bucket = this.buckets.get(webhookId);
    return {
      webhook: {
        tokens: bucket ? Math.floor(bucket.tokens) : this.requestsPerSecond,
        limit: this.requestsPerSecond
      },
      global: {
        tokens: Math.floor(this.globalBucket.tokens),
        limit: this.maxBurst
      }
    };
  }

  /**
   * Reset webhook rate limit
   * @param {string} webhookId - Webhook identifier
   * @returns {void}
   */
  reset(webhookId) {
    this.buckets.delete(webhookId);
  }

  /**
   * Reset all limits
   * @returns {void}
   */
  resetAll() {
    this.buckets.clear();
    this.globalBucket = { tokens: this.maxBurst, lastRefill: Date.now() };
  }
}

/**
 * Main webhook manager with all features
 * @class WebhookManager
 */
class WebhookManager {
  /**
   * Create webhook manager
   * @param {Object} options - Configuration options
   * @param {string} options.algorithm - Signature algorithm
   * @param {Object} options.retry - Retry configuration
   * @param {Object} options.rateLimit - Rate limit configuration
   */
  constructor(options = {}) {
    this.webhooks = new Map();
    this.verifier = new SignatureVerifier(options.algorithm || 'sha256');
    this.retryManager = new RetryManager(options.retry || {});
    this.rateLimiter = new WebhookRateLimiter(options.rateLimit || {});
    this.webhookCounter = 0;
    this.deliveryLog = [];
    this.handlers = new Map();
    this.stats = {
      registered: 0,
      delivered: 0,
      failed: 0,
      retried: 0
    };
  }

  /**
   * Generate unique webhook ID
   * @private
   * @returns {string} Unique webhook ID
   */
  _generateId() {
    return `webhook-${Date.now()}-${++this.webhookCounter}`;
  }

  /**
   * Register webhook
   * @param {string} url - Target URL
   * @param {string[]} events - Events to subscribe
   * @param {Object} options - Registration options
   * @param {string} options.secret - HMAC secret (auto-generated if not provided)
   * @param {Object} options.metadata - Additional metadata
   * @returns {Webhook} Registered webhook
   * @throws {Error} If URL or events invalid
   */
  register(url, events, options = {}) {
    if (!url || typeof url !== 'string') {
      throw new Error('URL must be a non-empty string');
    }

    // Basic URL validation
    try {
      new URL(url);
    } catch {
      throw new Error('Invalid URL format');
    }

    if (!Array.isArray(events) || events.length === 0) {
      throw new Error('Events must be a non-empty array');
    }

    const webhook = {
      id: this._generateId(),
      url,
      events: [...events],
      secret: options.secret || this.verifier.generateSecret(),
      active: true,
      metadata: options.metadata || {},
      createdAt: Date.now()
    };

    this.webhooks.set(webhook.id, webhook);
    this.stats.registered++;

    if (this.handlers.has('registered')) {
      this.handlers.get('registered')(webhook);
    }

    return webhook;
  }

  /**
   * Get webhook by ID
   * @param {string} webhookId - Webhook ID
   * @returns {Webhook|null} Webhook or null
   */
  get(webhookId) {
    return this.webhooks.get(webhookId) || null;
  }

  /**
   * List all webhooks
   * @param {Object} filter - Filter options
   * @param {boolean} filter.active - Only active webhooks
   * @returns {Webhook[]} List of webhooks
   */
  list(filter = {}) {
    let result = Array.from(this.webhooks.values());

    if (filter.active !== undefined) {
      result = result.filter(w => w.active === filter.active);
    }

    return result;
  }

  /**
   * Find webhooks for event
   * @param {string} event - Event name
   * @returns {Webhook[]} Matching webhooks
   */
  findForEvent(event) {
    return this.list({ active: true }).filter(w => w.events.includes(event));
  }

  /**
   * Update webhook
   * @param {string} webhookId - Webhook ID
   * @param {Object} updates - Fields to update
   * @returns {Webhook} Updated webhook
   * @throws {Error} If webhook not found
   */
  update(webhookId, updates) {
    const webhook = this.webhooks.get(webhookId);
    if (!webhook) {
      throw new Error(`Webhook ${webhookId} not found`);
    }

    if (updates.url) {
      try {
        new URL(updates.url);
      } catch {
        throw new Error('Invalid URL format');
      }
      webhook.url = updates.url;
    }

    if (Array.isArray(updates.events) && updates.events.length > 0) {
      webhook.events = [...updates.events];
    }

    if (updates.active !== undefined) {
      webhook.active = Boolean(updates.active);
    }

    if (updates.metadata) {
      webhook.metadata = { ...webhook.metadata, ...updates.metadata };
    }

    return webhook;
  }

  /**
   * Delete webhook
   * @param {string} webhookId - Webhook ID
   * @returns {boolean} True if deleted
   */
  delete(webhookId) {
    const result = this.webhooks.delete(webhookId);
    if (result) {
      this.retryManager.clearHistory(webhookId);
      this.rateLimiter.reset(webhookId);
    }
    return result;
  }

  /**
   * Trigger webhook delivery
   * @param {string} webhookId - Webhook ID
   * @param {string} event - Event name
   * @param {*} data - Event data
   * @returns {Promise<boolean>} Success status
   * @throws {Error} If webhook not found or inactive
   */
  async trigger(webhookId, event, data) {
    const webhook = this.webhooks.get(webhookId);
    if (!webhook) {
      throw new Error(`Webhook ${webhookId} not found`);
    }

    if (!webhook.active) {
      throw new Error(`Webhook ${webhookId} is inactive`);
    }

    if (!webhook.events.includes(event)) {
      throw new Error(`Event ${event} not subscribed by webhook`);
    }

    // Check rate limit
    if (!this.rateLimiter.canDeliver(webhookId)) {
      throw new Error(`Rate limit exceeded for webhook ${webhookId}`);
    }

    const payload = { event, data, timestamp: Date.now() };
    const signature = this.verifier.sign(JSON.stringify(payload), webhook.secret);

    try {
      const response = await this._send(webhook, payload, signature);
      this.stats.delivered++;

      if (this.handlers.has('delivered')) {
        this.handlers.get('delivered')(webhookId, event);
      }

      return true;
    } catch (error) {
      this.stats.failed++;

      if (this.handlers.has('failed')) {
        this.handlers.get('failed')(webhookId, event, error);
      }

      // Schedule retry
      if (this.retryManager.shouldRetry(webhookId, payload)) {
        this.retryManager.scheduleRetry(webhookId, payload, () => {
          this.trigger(webhookId, event, data).catch(() => {
            // Retry failed, error will be logged in trigger
          });
        });
        this.stats.retried++;
      }

      throw error;
    }
  }

  /**
   * Send webhook request
   * @private
   * @param {Webhook} webhook - Webhook configuration
   * @param {Object} payload - Payload to send
   * @param {string} signature - HMAC signature
   * @returns {Promise<Object>} Response
   */
  async _send(webhook, payload, signature) {
    // Mock implementation - replace with actual HTTP client in production
    return new Promise((resolve, reject) => {
      const timeout = setTimeout(
        () => reject(new Error('Webhook delivery timeout')),
        5000
      );

      // Simulate async delivery
      setImmediate(() => {
        clearTimeout(timeout);
        // In production, use fetch/axios to make actual HTTP request
        resolve({
          status: 200,
          webhook: webhook.id
        });
      });
    });
  }

  /**
   * Verify webhook signature
   * @param {*} payload - Original payload
   * @param {string} signature - Signature to verify
   * @param {string} webhookId - Webhook ID for secret lookup
   * @returns {boolean} True if valid
   * @throws {Error} If webhook not found
   */
  verifySignature(payload, signature, webhookId) {
    const webhook = this.webhooks.get(webhookId);
    if (!webhook) {
      throw new Error(`Webhook ${webhookId} not found`);
    }

    return this.verifier.verify(payload, signature, webhook.secret);
  }

  /**
   * Register event handler
   * @param {string} event - Event name (registered, delivered, failed, retried)
   * @param {Function} handler - Event handler function
   * @returns {void}
   * @throws {Error} If event invalid
   */
  on(event, handler) {
    const validEvents = ['registered', 'delivered', 'failed', 'retried'];
    if (!validEvents.includes(event)) {
      throw new Error(`Invalid event: ${event}`);
    }
    if (typeof handler !== 'function') {
      throw new Error('Handler must be a function');
    }
    this.handlers.set(event, handler);
  }

  /**
   * Remove event handler
   * @param {string} event - Event name
   * @returns {void}
   */
  off(event) {
    this.handlers.delete(event);
  }

  /**
   * Get delivery statistics
   * @returns {Object} Stats object
   */
  getStats() {
    return {
      ...this.stats,
      webhookCount: this.webhooks.size,
      activeCount: this.list({ active: true }).length
    };
  }
}

module.exports = {
  WebhookManager,
  SignatureVerifier,
  RetryManager,
  WebhookRateLimiter
};
