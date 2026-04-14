/**
 * HELIOS Message Queue Module - Production Implementation
 * Provides reliable message buffering, ordering, and delivery guarantees
 * 
 * @module mod-queue
 * @version 1.0.0
 */

/**
 * Message structure with metadata
 * @typedef {Object} Message
 * @property {string} id - Unique message identifier
 * @property {*} payload - Message content
 * @property {number} priority - Priority level (0-100, higher = more important)
 * @property {number} timestamp - Creation timestamp
 * @property {number} attempts - Delivery attempt count
 * @property {string} deliveryMode - 'at-least-once', 'at-most-once', 'exactly-once'
 * @property {Object} metadata - Additional metadata
 */

/**
 * Delivery guarantee modes
 * @enum {string}
 */
const DeliveryModes = {
  AT_LEAST_ONCE: 'at-least-once',
  AT_MOST_ONCE: 'at-most-once',
  EXACTLY_ONCE: 'exactly-once'
};

/**
 * Manages message ordering and retrieval strategies
 * @class OrderingManager
 */
class OrderingManager {
  /**
   * Create ordering manager
   * @param {string} strategy - 'fifo' or 'priority'
   * @throws {Error} If strategy is invalid
   */
  constructor(strategy = 'fifo') {
    if (!['fifo', 'priority'].includes(strategy)) {
      throw new Error(`Invalid strategy: ${strategy}. Must be 'fifo' or 'priority'`);
    }
    this.strategy = strategy;
    this.queue = [];
  }

  /**
   * Enqueue message with ordering
   * @param {Message} message - Message to enqueue
   * @returns {number} Queue position
   * @throws {Error} If message is invalid
   */
  enqueue(message) {
    if (!message || typeof message !== 'object') {
      throw new Error('Invalid message: must be an object');
    }

    if (this.strategy === 'fifo') {
      this.queue.push(message);
      return this.queue.length - 1;
    } else {
      // Priority ordering: higher priority = earlier in queue
      const position = this.queue.findIndex(m => (m.priority || 0) < (message.priority || 0));
      if (position === -1) {
        this.queue.push(message);
        return this.queue.length - 1;
      }
      this.queue.splice(position, 0, message);
      return position;
    }
  }

  /**
   * Dequeue next message
   * @returns {Message|null} Next message or null if empty
   */
  dequeue() {
    return this.queue.length > 0 ? this.queue.shift() : null;
  }

  /**
   * Peek at next message without removing
   * @returns {Message|null} Next message or null
   */
  peek() {
    return this.queue.length > 0 ? this.queue[0] : null;
  }

  /**
   * Get current queue size
   * @returns {number} Queue length
   */
  size() {
    return this.queue.length;
  }

  /**
   * Clear all messages
   * @returns {void}
   */
  clear() {
    this.queue = [];
  }

  /**
   * Get all messages (non-destructive)
   * @returns {Message[]} All messages in queue
   */
  getAll() {
    return [...this.queue];
  }
}

/**
 * Manages delivery guarantees for messages
 * @class DeliveryGuarantee
 */
class DeliveryGuarantee {
  /**
   * Create delivery guarantee manager
   * @param {string} mode - Delivery mode (at-least-once, at-most-once, exactly-once)
   * @throws {Error} If mode is invalid
   */
  constructor(mode = DeliveryModes.AT_LEAST_ONCE) {
    if (!Object.values(DeliveryModes).includes(mode)) {
      throw new Error(`Invalid delivery mode: ${mode}`);
    }
    this.mode = mode;
    this.acknowledged = new Set();
    this.inFlight = new Map();
    this.processed = new Set();
  }

  /**
   * Mark message as in-flight
   * @param {string} messageId - Message ID
   * @param {number} timestamp - Flight timestamp
   * @returns {boolean} Success
   * @throws {Error} If message already in flight
   */
  markInFlight(messageId, timestamp = Date.now()) {
    if (this.inFlight.has(messageId)) {
      throw new Error(`Message ${messageId} already in-flight`);
    }
    this.inFlight.set(messageId, { timestamp, attempts: 1 });
    return true;
  }

  /**
   * Mark message as acknowledged
   * @param {string} messageId - Message ID
   * @returns {boolean} Should process based on delivery mode
   * @throws {Error} If message not in-flight
   */
  markAcknowledged(messageId) {
    if (!this.inFlight.has(messageId)) {
      throw new Error(`Message ${messageId} not in-flight`);
    }

    this.acknowledged.add(messageId);
    this.inFlight.delete(messageId);

    if (this.mode === DeliveryModes.EXACTLY_ONCE) {
      if (this.processed.has(messageId)) {
        return false; // Already processed
      }
      this.processed.add(messageId);
    }

    return true;
  }

  /**
   * Retry in-flight message
   * @param {string} messageId - Message ID
   * @returns {number} New attempt count
   * @throws {Error} If message not in-flight
   */
  retry(messageId) {
    if (!this.inFlight.has(messageId)) {
      throw new Error(`Message ${messageId} not in-flight`);
    }
    const flight = this.inFlight.get(messageId);
    flight.attempts++;
    return flight.attempts;
  }

  /**
   * Get message status
   * @param {string} messageId - Message ID
   * @returns {Object} Status object
   */
  getStatus(messageId) {
    return {
      inFlight: this.inFlight.has(messageId),
      acknowledged: this.acknowledged.has(messageId),
      processed: this.processed.has(messageId),
      attempts: this.inFlight.get(messageId)?.attempts || 0
    };
  }

  /**
   * Get in-flight messages older than timeout
   * @param {number} timeoutMs - Timeout in milliseconds
   * @returns {string[]} Message IDs that timed out
   */
  getTimedOutMessages(timeoutMs) {
    const now = Date.now();
    return Array.from(this.inFlight.entries())
      .filter(([, data]) => now - data.timestamp > timeoutMs)
      .map(([id]) => id);
  }
}

/**
 * Dead Letter Queue for failed messages
 * @class DeadLetterQueue
 */
class DeadLetterQueue {
  /**
   * Create dead letter queue
   * @param {Object} options - Configuration options
   * @param {number} options.maxSize - Maximum DLQ size (default: 10000)
   * @param {number} options.ttl - Message TTL in ms (default: 86400000 = 24h)
   */
  constructor(options = {}) {
    this.maxSize = options.maxSize || 10000;
    this.ttl = options.ttl || 86400000;
    this.messages = [];
    this.stats = {
      total: 0,
      expired: 0,
      discarded: 0
    };
  }

  /**
   * Add message to DLQ
   * @param {Message} message - Message that failed
   * @param {string} reason - Failure reason
   * @param {Error} error - Original error if available
   * @returns {boolean} Successfully added
   * @throws {Error} If message is invalid
   */
  add(message, reason, error = null) {
    if (!message || typeof message !== 'object') {
      throw new Error('Invalid message for DLQ');
    }

    if (this.messages.length >= this.maxSize) {
      this.stats.discarded++;
      return false;
    }

    this.messages.push({
      messageId: message.id,
      message,
      reason,
      error: error ? { message: error.message, stack: error.stack } : null,
      addedAt: Date.now()
    });

    this.stats.total++;
    return true;
  }

  /**
   * Get dead letter entry
   * @param {string} messageId - Message ID
   * @returns {Object|null} DLQ entry or null
   */
  get(messageId) {
    return this.messages.find(m => m.messageId === messageId) || null;
  }

  /**
   * Remove expired messages
   * @returns {number} Number of messages removed
   */
  cleanup() {
    const before = this.messages.length;
    const now = Date.now();
    this.messages = this.messages.filter(m => now - m.addedAt < this.ttl);
    const removed = before - this.messages.length;
    this.stats.expired += removed;
    return removed;
  }

  /**
   * Get all DLQ messages
   * @returns {Object[]} All messages in DLQ
   */
  getAll() {
    this.cleanup();
    return [...this.messages];
  }

  /**
   * Get DLQ statistics
   * @returns {Object} Stats object
   */
  getStats() {
    return { ...this.stats, current: this.messages.length };
  }

  /**
   * Clear DLQ
   * @returns {number} Number of messages cleared
   */
  clear() {
    const count = this.messages.length;
    this.messages = [];
    return count;
  }
}

/**
 * Main message queue with all features
 * @class MessageQueue
 */
class MessageQueue {
  /**
   * Create message queue
   * @param {Object} options - Configuration options
   * @param {string} options.ordering - 'fifo' or 'priority'
   * @param {string} options.deliveryMode - Delivery guarantee mode
   * @param {number} options.maxRetries - Max retry attempts
   * @param {number} options.retryBackoff - Backoff multiplier
   * @param {number} options.idleTimeout - Timeout for in-flight messages
   * @throws {Error} If options are invalid
   */
  constructor(options = {}) {
    this.ordering = new OrderingManager(options.ordering || 'fifo');
    this.guarantee = new DeliveryGuarantee(options.deliveryMode || DeliveryModes.AT_LEAST_ONCE);
    this.dlq = new DeadLetterQueue(options.dlq);
    this.maxRetries = options.maxRetries || 3;
    this.retryBackoff = options.retryBackoff || 2;
    this.idleTimeout = options.idleTimeout || 30000;
    this.messageCounter = 0;
    this.handlers = new Map();
    this.stats = {
      enqueued: 0,
      dequeued: 0,
      acknowledged: 0,
      failed: 0,
      retried: 0
    };
  }

  /**
   * Generate unique message ID
   * @private
   * @returns {string} Unique message ID
   */
  _generateId() {
    return `msg-${Date.now()}-${++this.messageCounter}`;
  }

  /**
   * Enqueue message
   * @param {*} payload - Message payload
   * @param {Object} options - Enqueue options
   * @param {number} options.priority - Priority level
   * @param {string} options.deliveryMode - Delivery mode
   * @param {Object} options.metadata - Additional metadata
   * @returns {string} Message ID
   * @throws {Error} If payload is null
   */
  enqueue(payload, options = {}) {
    if (payload === null || payload === undefined) {
      throw new Error('Payload cannot be null or undefined');
    }

    const message = {
      id: this._generateId(),
      payload,
      priority: options.priority || 0,
      timestamp: Date.now(),
      attempts: 0,
      deliveryMode: options.deliveryMode || this.guarantee.mode,
      metadata: options.metadata || {}
    };

    this.ordering.enqueue(message);
    this.stats.enqueued++;

    if (this.handlers.has('enqueued')) {
      this.handlers.get('enqueued')(message);
    }

    return message.id;
  }

  /**
   * Dequeue next message
   * @returns {Message|null} Next message or null
   */
  dequeue() {
    const message = this.ordering.dequeue();
    if (!message) return null;

    this.guarantee.markInFlight(message.id);
    message.attempts++;
    this.stats.dequeued++;

    if (this.handlers.has('dequeued')) {
      this.handlers.get('dequeued')(message);
    }

    return message;
  }

  /**
   * Acknowledge message delivery
   * @param {string} messageId - Message ID to acknowledge
   * @returns {boolean} Successfully acknowledged
   * @throws {Error} If message not found
   */
  acknowledge(messageId) {
    if (!this.guarantee.markAcknowledged(messageId)) {
      return false; // Already processed in exactly-once mode
    }

    this.stats.acknowledged++;

    if (this.handlers.has('acknowledged')) {
      this.handlers.get('acknowledged')(messageId);
    }

    return true;
  }

  /**
   * Mark message as failed
   * @param {string} messageId - Message ID
   * @param {Error} error - Error that occurred
   * @returns {boolean} Should retry
   * @throws {Error} If message not in-flight
   */
  fail(messageId, error) {
    const message = this.ordering.getAll().find(m => m.id === messageId);
    if (!message) {
      throw new Error(`Message ${messageId} not found`);
    }

    if (message.attempts >= this.maxRetries) {
      this.dlq.add(message, `Failed after ${message.attempts} attempts`, error);
      this.stats.failed++;

      if (this.handlers.has('failed')) {
        this.handlers.get('failed')(messageId, error);
      }

      return false;
    }

    const newAttempt = this.guarantee.retry(messageId);
    this.stats.retried++;

    if (this.handlers.has('retried')) {
      this.handlers.get('retried')(messageId, newAttempt);
    }

    return true;
  }

  /**
   * Peek at next message without dequeuing
   * @returns {Message|null} Next message
   */
  peek() {
    return this.ordering.peek();
  }

  /**
   * Get current queue size
   * @returns {number} Queue length
   */
  size() {
    return this.ordering.size();
  }

  /**
   * Get all messages
   * @returns {Message[]} All queued messages
   */
  getAll() {
    return this.ordering.getAll();
  }

  /**
   * Register event handler
   * @param {string} event - Event name (enqueued, dequeued, acknowledged, failed, retried)
   * @param {Function} handler - Event handler function
   * @returns {void}
   * @throws {Error} If event name is invalid
   */
  on(event, handler) {
    const validEvents = ['enqueued', 'dequeued', 'acknowledged', 'failed', 'retried'];
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
   * Get queue statistics
   * @returns {Object} Stats object
   */
  getStats() {
    return {
      ...this.stats,
      queueSize: this.ordering.size(),
      dlqStats: this.dlq.getStats()
    };
  }

  /**
   * Clear queue
   * @returns {number} Number of messages cleared
   */
  clear() {
    const count = this.ordering.size();
    this.ordering.clear();
    return count;
  }

  /**
   * Recover timed-out messages
   * @returns {number} Number of recovered messages
   */
  recoverTimedOut() {
    const timedOut = this.guarantee.getTimedOutMessages(this.idleTimeout);
    timedOut.forEach(id => {
      const message = this.ordering.getAll().find(m => m.id === id);
      if (message && message.attempts < this.maxRetries) {
        this.guarantee.retry(id);
        this.stats.retried++;
      }
    });
    return timedOut.length;
  }
}

module.exports = {
  MessageQueue,
  OrderingManager,
  DeliveryGuarantee,
  DeadLetterQueue,
  DeliveryModes
};
