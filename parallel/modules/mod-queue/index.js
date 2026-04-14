/**
 * Message Queue Module - Main Export
 * @module mod-queue
 */

const {
  MessageQueue,
  OrderingManager,
  DeliveryGuarantee,
  DeadLetterQueue,
  DeliveryModes
} = require('./implementation');

/**
 * Public API
 * @namespace mod-queue
 */
module.exports = {
  // Classes
  MessageQueue,
  OrderingManager,
  DeliveryGuarantee,
  DeadLetterQueue,

  // Enums
  DeliveryModes,

  // Factory function for convenience
  /**
   * Create new message queue instance
   * @param {Object} options - Queue configuration
   * @returns {MessageQueue} New queue instance
   */
  createQueue: (options) => new MessageQueue(options),

  /**
   * Create new ordering manager
   * @param {string} strategy - 'fifo' or 'priority'
   * @returns {OrderingManager} New manager instance
   */
  createOrderingManager: (strategy) => new OrderingManager(strategy),

  /**
   * Create new delivery guarantee manager
   * @param {string} mode - Delivery mode
   * @returns {DeliveryGuarantee} New manager instance
   */
  createDeliveryGuarantee: (mode) => new DeliveryGuarantee(mode),

  /**
   * Create new dead letter queue
   * @param {Object} options - DLQ configuration
   * @returns {DeadLetterQueue} New DLQ instance
   */
  createDeadLetterQueue: (options) => new DeadLetterQueue(options)
};
