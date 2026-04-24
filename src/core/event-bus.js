/**
 * Event Bus System - Unified pub/sub architecture for cross-service communication
 * Manages all events flowing through HELIOS platform with persistence, validation, and retry logic
 * @module core/event-bus
 */

const EventEmitter = require('events');
const crypto = require('crypto');

/**
 * EventValidator - Validates events against predefined schemas
 * @class
 */
class EventValidator {
  constructor() {
    this.schemas = new Map();
    this.registerDefaultSchemas();
  }

  /**
   * Register default event schemas
   * @private
   */
  registerDefaultSchemas() {
    this.schemas.set('ai:suggestion', {
      type: 'object',
      properties: {
        suggestionsId: { type: 'string' },
        content: { type: 'string' },
        confidence: { type: 'number', min: 0, max: 1 },
        metadata: { type: 'object' },
      },
      required: ['suggestionsId', 'content', 'confidence'],
    });

    this.schemas.set('analytics:recorded', {
      type: 'object',
      properties: {
        eventName: { type: 'string' },
        userId: { type: 'string' },
        timestamp: { type: 'number' },
        properties: { type: 'object' },
      },
      required: ['eventName', 'userId', 'timestamp'],
    });

    this.schemas.set('sync:conflict', {
      type: 'object',
      properties: {
        resourceId: { type: 'string' },
        localVersion: { type: 'number' },
        remoteVersion: { type: 'number' },
        conflictType: { type: 'string', enum: ['content', 'delete', 'timestamp'] },
      },
      required: ['resourceId', 'localVersion', 'remoteVersion', 'conflictType'],
    });

    this.schemas.set('plugin:installed', {
      type: 'object',
      properties: {
        pluginId: { type: 'string' },
        name: { type: 'string' },
        version: { type: 'string' },
        permissions: { type: 'array' },
      },
      required: ['pluginId', 'name', 'version'],
    });

    this.schemas.set('user:authenticated', {
      type: 'object',
      properties: {
        userId: { type: 'string' },
        token: { type: 'string' },
        expiresAt: { type: 'number' },
        scopes: { type: 'array' },
      },
      required: ['userId', 'token', 'expiresAt'],
    });

    this.schemas.set('error:occurred', {
      type: 'object',
      properties: {
        errorId: { type: 'string' },
        message: { type: 'string' },
        service: { type: 'string' },
        severity: { type: 'string', enum: ['low', 'medium', 'high', 'critical'] },
        stack: { type: 'string' },
      },
      required: ['errorId', 'message', 'service', 'severity'],
    });
  }

  /**
   * Validate event against schema
   * @param {string} eventType - Event type to validate
   * @param {Object} data - Event data
   * @returns {Object} Validation result {valid: boolean, errors: string[]}
   */
  validate(eventType, data) {
    const schema = this.schemas.get(eventType);
    if (!schema) {
      return { valid: false, errors: [`Unknown event type: ${eventType}`] };
    }

    const errors = this.validateData(data, schema);
    return { valid: errors.length === 0, errors };
  }

  /**
   * Recursively validate data against schema
   * @private
   */
  validateData(data, schema) {
    const errors = [];

    if (schema.required) {
      for (const field of schema.required) {
        if (!(field in data)) {
          errors.push(`Missing required field: ${field}`);
        }
      }
    }

    if (schema.properties) {
      for (const [field, fieldSchema] of Object.entries(schema.properties)) {
        if (field in data) {
          const fieldErrors = this.validateField(data[field], fieldSchema);
          errors.push(...fieldErrors);
        }
      }
    }

    return errors;
  }

  /**
   * Validate single field
   * @private
   */
  validateField(value, schema) {
    const errors = [];

    if (schema.type && typeof value !== schema.type) {
      errors.push(`Invalid type for field: expected ${schema.type}, got ${typeof value}`);
    }

    if (schema.min !== undefined && value < schema.min) {
      errors.push(`Value below minimum: ${schema.min}`);
    }

    if (schema.max !== undefined && value > schema.max) {
      errors.push(`Value above maximum: ${schema.max}`);
    }

    if (schema.enum && !schema.enum.includes(value)) {
      errors.push(`Invalid enum value: ${value}`);
    }

    return errors;
  }

  /**
   * Register custom event schema
   * @param {string} eventType - Event type name
   * @param {Object} schema - Schema definition
   */
  registerSchema(eventType, schema) {
    this.schemas.set(eventType, schema);
  }
}

/**
 * EventPersistence - Store and retrieve events for async processing
 * @class
 */
class EventPersistence {
  constructor() {
    this.events = [];
    this.maxSize = 10000;
  }

  /**
   * Store event for replay
   * @param {string} eventType - Event type
   * @param {Object} data - Event data
   * @param {string} correlationId - Correlation ID
   * @returns {string} Event ID
   */
  store(eventType, data, correlationId) {
    const eventId = crypto.randomUUID();
    const storedEvent = {
      id: eventId,
      type: eventType,
      data,
      correlationId,
      timestamp: Date.now(),
      processed: false,
      retries: 0,
    };

    this.events.push(storedEvent);

    if (this.events.length > this.maxSize) {
      this.events.shift();
    }

    return eventId;
  }

  /**
   * Retrieve stored events
   * @param {Object} filters - Filter options {eventType, processed, limit}
   * @returns {Array} Stored events
   */
  retrieve(filters = {}) {
    let results = this.events;

    if (filters.eventType) {
      results = results.filter(e => e.type === filters.eventType);
    }

    if (filters.processed !== undefined) {
      results = results.filter(e => e.processed === filters.processed);
    }

    if (filters.limit) {
      results = results.slice(-filters.limit);
    }

    return results;
  }

  /**
   * Mark event as processed
   * @param {string} eventId - Event ID
   */
  markProcessed(eventId) {
    const event = this.events.find(e => e.id === eventId);
    if (event) {
      event.processed = true;
      event.processedAt = Date.now();
    }
  }

  /**
   * Replay events
   * @param {Function} handler - Handler function
   * @param {Object} filters - Filter options
   */
  async replay(handler, filters = {}) {
    const events = this.retrieve({ ...filters, processed: false });
    for (const event of events) {
      try {
        await handler(event);
        this.markProcessed(event.id);
      } catch (error) {
        console.error(`Replay failed for event ${event.id}:`, error.message);
      }
    }
  }
}

/**
 * DeadLetterQueue - Handle failed event processing
 * @class
 */
class DeadLetterQueue {
  constructor() {
    this.queue = [];
    this.maxRetries = 3;
  }

  /**
   * Add failed event to DLQ
   * @param {Object} event - Failed event
   * @param {Error} error - Error that occurred
   */
  add(event, error) {
    this.queue.push({
      id: crypto.randomUUID(),
      originalEvent: event,
      error: error.message,
      stack: error.stack,
      timestamp: Date.now(),
      retryCount: event.retries || 0,
      canRetry: (event.retries || 0) < this.maxRetries,
    });
  }

  /**
   * Get failed events
   * @param {Object} filters - Filter options
   * @returns {Array} DLQ items
   */
  getItems(filters = {}) {
    let items = this.queue;

    if (filters.canRetry !== undefined) {
      items = items.filter(i => i.canRetry === filters.canRetry);
    }

    return items;
  }

  /**
   * Retry failed event
   * @param {string} dlqId - DLQ item ID
   * @param {Function} handler - Handler function
   * @returns {boolean} Success status
   */
  async retry(dlqId, handler) {
    const item = this.queue.find(i => i.id === dlqId);
    if (!item) return false;

    try {
      await handler(item.originalEvent);
      this.queue = this.queue.filter(i => i.id !== dlqId);
      return true;
    } catch (error) {
      item.retryCount++;
      item.error = error.message;
      item.canRetry = item.retryCount < this.maxRetries;
      return false;
    }
  }

  /**
   * Clear DLQ
   */
  clear() {
    this.queue = [];
  }
}

/**
 * EventRetry - Automatic retry with exponential backoff
 * @class
 */
class EventRetry {
  constructor() {
    this.retryQueue = new Map();
    this.baseDelay = 1000;
    this.maxDelay = 60000;
  }

  /**
   * Schedule retry with exponential backoff
   * @param {string} eventId - Event ID
   * @param {Function} handler - Handler function
   * @param {number} attempt - Attempt number
   * @returns {Promise} Retry promise
   */
  scheduleRetry(eventId, handler, attempt = 1) {
    const delay = Math.min(this.baseDelay * Math.pow(2, attempt - 1), this.maxDelay);

    return new Promise((resolve, reject) => {
      const timeout = setTimeout(async () => {
        try {
          await handler();
          this.retryQueue.delete(eventId);
          resolve(true);
        } catch (error) {
          this.retryQueue.delete(eventId);
          reject(error);
        }
      }, delay);

      this.retryQueue.set(eventId, { timeout, attempt });
    });
  }

  /**
   * Cancel retry
   * @param {string} eventId - Event ID
   */
  cancel(eventId) {
    const retry = this.retryQueue.get(eventId);
    if (retry) {
      clearTimeout(retry.timeout);
      this.retryQueue.delete(eventId);
    }
  }

  /**
   * Get pending retries
   * @returns {Array} Pending retry information
   */
  getPending() {
    return Array.from(this.retryQueue.entries()).map(([eventId, retry]) => ({
      eventId,
      attempt: retry.attempt,
    }));
  }
}

/**
 * SubscriberManagement - Register/unregister event handlers
 * @class
 */
class SubscriberManagement {
  constructor() {
    this.subscribers = new Map();
  }

  /**
   * Register event subscriber
   * @param {string} eventType - Event type
   * @param {string} subscriberId - Subscriber ID
   * @param {Function} handler - Handler function
   * @returns {Function} Unsubscribe function
   */
  subscribe(eventType, subscriberId, handler) {
    if (!this.subscribers.has(eventType)) {
      this.subscribers.set(eventType, new Map());
    }

    const handlers = this.subscribers.get(eventType);
    handlers.set(subscriberId, handler);

    return () => this.unsubscribe(eventType, subscriberId);
  }

  /**
   * Unregister event subscriber
   * @param {string} eventType - Event type
   * @param {string} subscriberId - Subscriber ID
   * @returns {boolean} Success status
   */
  unsubscribe(eventType, subscriberId) {
    const handlers = this.subscribers.get(eventType);
    if (!handlers) return false;

    const result = handlers.delete(subscriberId);
    if (handlers.size === 0) {
      this.subscribers.delete(eventType);
    }
    return result;
  }

  /**
   * Get all subscribers for event type
   * @param {string} eventType - Event type
   * @returns {Array} Subscribers
   */
  getSubscribers(eventType) {
    const handlers = this.subscribers.get(eventType);
    if (!handlers) return [];
    return Array.from(handlers.entries()).map(([subscriberId, handler]) => ({
      subscriberId,
      handler,
    }));
  }

  /**
   * Clear all subscribers for event type
   * @param {string} eventType - Event type
   */
  clearSubscribers(eventType) {
    this.subscribers.delete(eventType);
  }

  /**
   * Get total subscriber count
   * @returns {number} Total subscribers
   */
  getTotalSubscribers() {
    let count = 0;
    for (const handlers of this.subscribers.values()) {
      count += handlers.size;
    }
    return count;
  }
}

/**
 * EventBus - Main event bus system
 * @class
 */
class EventBus extends EventEmitter {
  constructor() {
    super();
    this.validator = new EventValidator();
    this.persistence = new EventPersistence();
    this.dlq = new DeadLetterQueue();
    this.retry = new EventRetry();
    this.subscribers = new SubscriberManagement();
    this.correlationIds = new Map();
  }

  /**
   * Publish event
   * @param {string} eventType - Event type
   * @param {Object} data - Event data
   * @param {Object} options - Publish options {correlationId, persist, async}
   * @returns {Promise|void} Event ID or promise
   */
  publish(eventType, data, options = {}) {
    const { correlationId = crypto.randomUUID(), persist = true, async = false } = options;

    const validation = this.validator.validate(eventType, data);
    if (!validation.valid) {
      throw new Error(`Validation failed: ${validation.errors.join(', ')}`);
    }

    const eventId = persist ? this.persistence.store(eventType, data, correlationId) : crypto.randomUUID();

    const event = { id: eventId, type: eventType, data, correlationId, timestamp: Date.now() };

    if (async) {
      return this.publishAsync(event);
    } else {
      this.publishSync(event);
      return eventId;
    }
  }

  /**
   * Publish event synchronously
   * @private
   */
  publishSync(event) {
    const subscribers = this.subscribers.getSubscribers(event.type);

    for (const { subscriberId, handler } of subscribers) {
      try {
        handler(event.data, event);
      } catch (error) {
        console.error(`Handler ${subscriberId} failed:`, error);
        this.dlq.add(event, error);
      }
    }

    this.emit('published', event);
  }

  /**
   * Publish event asynchronously
   * @private
   */
  async publishAsync(event) {
    return new Promise((resolve) => {
      setImmediate(async () => {
        const subscribers = this.subscribers.getSubscribers(event.type);

        for (const { subscriberId, handler } of subscribers) {
          try {
            await handler(event.data, event);
          } catch (error) {
            console.error(`Handler ${subscriberId} failed:`, error);
            this.dlq.add(event, error);
          }
        }

        this.emit('published', event);
        resolve(event.id);
      });
    });
  }

  /**
   * Subscribe to events
   * @param {string} eventType - Event type
   * @param {Function} handler - Handler function
   * @param {Object} options - Subscribe options {subscriberId}
   * @returns {Function} Unsubscribe function
   */
  subscribe(eventType, handler, options = {}) {
    const subscriberId = options.subscriberId || `subscriber-${crypto.randomUUID()}`;
    return this.subscribers.subscribe(eventType, subscriberId, handler);
  }

  /**
   * Get event bus statistics
   * @returns {Object} Statistics
   */
  getStats() {
    return {
      totalSubscribers: this.subscribers.getTotalSubscribers(),
      storedEvents: this.persistence.events.length,
      dlqSize: this.dlq.queue.length,
      pendingRetries: this.retry.getPending().length,
    };
  }

  /**
   * Clear all state
   */
  reset() {
    this.subscribers = new SubscriberManagement();
    this.persistence = new EventPersistence();
    this.dlq = new DeadLetterQueue();
    this.retry = new EventRetry();
  }
}

module.exports = {
  EventBus,
  EventValidator,
  EventPersistence,
  DeadLetterQueue,
  EventRetry,
  SubscriberManagement,
};
