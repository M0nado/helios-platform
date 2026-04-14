/**
 * Webhook Manager Module - Main Export
 * @module mod-webhook
 */

const {
  WebhookManager,
  SignatureVerifier,
  RetryManager,
  WebhookRateLimiter
} = require('./implementation');

/**
 * Public API
 * @namespace mod-webhook
 */
module.exports = {
  // Classes
  WebhookManager,
  SignatureVerifier,
  RetryManager,
  WebhookRateLimiter,

  // Factory functions
  /**
   * Create new webhook manager instance
   * @param {Object} options - Manager configuration
   * @returns {WebhookManager} New manager instance
   */
  createManager: (options) => new WebhookManager(options),

  /**
   * Create new signature verifier
   * @param {string} algorithm - Hash algorithm
   * @returns {SignatureVerifier} New verifier instance
   */
  createVerifier: (algorithm) => new SignatureVerifier(algorithm),

  /**
   * Create new retry manager
   * @param {Object} options - Retry configuration
   * @returns {RetryManager} New manager instance
   */
  createRetryManager: (options) => new RetryManager(options),

  /**
   * Create new rate limiter
   * @param {Object} options - Rate limit configuration
   * @returns {WebhookRateLimiter} New limiter instance
   */
  createRateLimiter: (options) => new WebhookRateLimiter(options)
};
