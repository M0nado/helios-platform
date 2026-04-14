/**
 * HELIOS v4.0 Event Bus Module - Implementation
 * Provides event publishing, subscription, routing, and persistence
 * @module mod-eventbus/implementation
 */

/**
 * Message Queue - FIFO queue for ordered message processing
 * @class MessageQueue
 */
class MessageQueue {
  constructor() {
    /** @type {object[]} Queue of pending messages */
    this.queue = [];
    /** @type {boolean} Whether queue is processing */
    this.isProcessing = false;
    /** @type {Function|null} Current processor function */
    this.processor = null;
  }

  /**
   * Enqueue a message
   * @param {object} message - Message to enqueue
   * @throws {TypeError} If message is not an object
   * @returns {void}
   */
  enqueue(message) {
    if (typeof message !== 'object' || message === null) {
      throw new TypeError('Message must be an object');
    }
    this.queue.push({
      ...message,
      queuedAt: Date.now(),
    });
  }

  /**
   * Dequeue next message
   * @returns {object|null} Next message or null if queue empty
   */
  dequeue() {
    return this.queue.shift() || null;
  }

  /**
   * Peek at next message without removing
   * @returns {object|null} Next message or null if queue empty
   */
  peek() {
    return this.queue[0] || null;
  }

  /**
   * Get queue size
   * @returns {number} Number of queued messages
   */
  size() {
    return this.queue.length;
  }

  /**
   * Check if queue is empty
   * @returns {boolean} True if queue is empty
   */
  isEmpty() {
    return this.queue.length === 0;
  }

  /**
   * Clear queue
   * @returns {void}
   */
  clear() {
    this.queue = [];
  }

  /**
   * Process queue with provided handler
   * @param {Function} handler - Async handler for each message
   * @returns {Promise<void>}
   * @throws {TypeError} If handler is not a function
   */
  async process(handler) {
    if (typeof handler !== 'function') throw new TypeError('Handler must be a function');

    if (this.isProcessing) return;
    this.isProcessing = true;

    try {
      while (!this.isEmpty()) {
        const message = this.dequeue();
        await handler(message);
      }
    } finally {
      this.isProcessing = false;
    }
  }
}

/**
 * Event Router - Routes events to appropriate subscribers
 * @class EventRouter
 */
class EventRouter {
  constructor() {
    /** @type {Map<string, Set<Function>>} Exact topic subscribers */
    this.exactRoutes = new Map();
    /** @type {object[]} Wildcard route patterns */
    this.wildcardRoutes = [];
    /** @type {Map<string, Set<Function>>} Filter-based subscribers */
    this.filterRoutes = new Map();
  }

  /**
   * Register subscriber for exact topic
   * @param {string} topic - Topic name
   * @param {Function} handler - Handler function
   * @throws {TypeError} If topic is not string or handler is not function
   * @returns {Function} Unsubscribe function
   */
  subscribe(topic, handler) {
    if (typeof topic !== 'string') throw new TypeError('Topic must be a string');
    if (typeof handler !== 'function') throw new TypeError('Handler must be a function');

    if (!this.exactRoutes.has(topic)) {
      this.exactRoutes.set(topic, new Set());
    }
    this.exactRoutes.get(topic).add(handler);

    return () => this.unsubscribe(topic, handler);
  }

  /**
   * Unsubscribe from topic
   * @param {string} topic - Topic name
   * @param {Function} handler - Handler function
   * @returns {boolean} True if unsubscribed, false if not found
   */
  unsubscribe(topic, handler) {
    const subscribers = this.exactRoutes.get(topic);
    if (subscribers) {
      return subscribers.delete(handler);
    }
    return false;
  }

  /**
   * Subscribe with wildcard pattern
   * @param {string} pattern - Topic pattern (e.g., 'orders.*', 'user.*.updated')
   * @param {Function} handler - Handler function
   * @returns {Function} Unsubscribe function
   */
  subscribeWildcard(pattern, handler) {
    if (typeof pattern !== 'string') throw new TypeError('Pattern must be a string');
    if (typeof handler !== 'function') throw new TypeError('Handler must be a function');

    const route = { pattern, regex: this._patternToRegex(pattern), handler };
    this.wildcardRoutes.push(route);

    return () => {
      const index = this.wildcardRoutes.indexOf(route);
      if (index > -1) this.wildcardRoutes.splice(index, 1);
    };
  }

  /**
   * Subscribe with filter predicate
   * @param {Function} predicate - Filter function(event) => boolean
   * @param {Function} handler - Handler function
   * @returns {Function} Unsubscribe function
   */
  subscribeWithFilter(predicate, handler) {
    if (typeof predicate !== 'function') throw new TypeError('Predicate must be a function');
    if (typeof handler !== 'function') throw new TypeError('Handler must be a function');

    const id = `filter_${Date.now()}_${Math.random()}`;
    const entry = { id, predicate, handler };

    if (!this.filterRoutes.has(id)) {
      this.filterRoutes.set(id, new Set());
    }
    this.filterRoutes.get(id).add(entry);

    return () => this.filterRoutes.delete(id);
  }

  /**
   * Route event to all matching subscribers
   * @param {string} topic - Topic name
   * @param {object} event - Event data
   * @returns {Function[]} Handlers that matched
   */
  route(topic, event) {
    const handlers = [];

    const exactHandlers = this.exactRoutes.get(topic);
    if (exactHandlers) {
      handlers.push(...exactHandlers);
    }

    this.wildcardRoutes.forEach(route => {
      if (route.regex.test(topic)) {
        handlers.push(route.handler);
      }
    });

    this.filterRoutes.forEach(entries => {
      entries.forEach(entry => {
        try {
          if (entry.predicate(event)) {
            handlers.push(entry.handler);
          }
        } catch (error) {
          console.error(`Filter predicate error: ${error.message}`);
        }
      });
    });

    return handlers;
  }

  /**
   * Convert wildcard pattern to regex
   * @private
   * @param {string} pattern - Pattern like 'orders.*'
   * @returns {RegExp} Compiled regex
   */
  _patternToRegex(pattern) {
    const escaped = pattern.replace(/[.+^${}()|[\]\\]/g, '\\$&');
    const withWildcard = escaped.replace(/\\\*/g, '[^.]*');
    return new RegExp(`^${withWildcard}$`);
  }

  /**
   * Get subscriber count for topic
   * @param {string} topic - Topic name
   * @returns {number} Number of subscribers
   */
  getSubscriberCount(topic) {
    let count = 0;
    const exactHandlers = this.exactRoutes.get(topic);
    if (exactHandlers) count += exactHandlers.size;
    count += this.wildcardRoutes.filter(r => r.regex.test(topic)).length;
    count += this.filterRoutes.size;
    return count;
  }

  /**
   * Clear all subscriptions
   * @returns {void}
   */
  clear() {
    this.exactRoutes.clear();
    this.wildcardRoutes = [];
    this.filterRoutes.clear();
  }
}

/**
 * Persistence Layer - Stores and retrieves events
 * @class PersistenceLayer
 */
class PersistenceLayer {
  constructor() {
    /** @type {object[]} In-memory event log */
    this.eventLog = [];
    /** @type {number} Max events to retain in memory */
    this.maxEvents = 10000;
    /** @type {boolean} Whether persistence is enabled */
    this.enabled = true;
  }

  /**
   * Record an event
   * @param {object} event - Event to persist
   * @throws {TypeError} If event is not an object
   * @returns {void}
   */
  record(event) {
    if (typeof event !== 'object' || event === null) {
      throw new TypeError('Event must be an object');
    }

    if (!this.enabled) return;

    const persistedEvent = {
      ...event,
      persistedAt: Date.now(),
      id: `${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
    };

    this.eventLog.push(persistedEvent);

    if (this.eventLog.length > this.maxEvents) {
      this.eventLog.shift();
    }
  }

  /**
   * Query events by topic
   * @param {string} topic - Topic filter
   * @param {object} [options] - Query options
   * @param {number} [options.limit=100] - Maximum results
   * @param {number} [options.offset=0] - Result offset
   * @returns {object[]} Matching events
   */
  query(topic, options = {}) {
    const { limit = 100, offset = 0 } = options;

    let results = this.eventLog;
    if (topic) {
      results = results.filter(e => e.topic === topic);
    }

    return results.slice(offset, offset + limit);
  }

  /**
   * Replay events matching criteria
   * @param {Function} handler - Handler for each event
   * @param {object} [criteria] - Filter criteria
   * @param {string} [criteria.topic] - Topic to replay
   * @param {number} [criteria.since] - Timestamp filter
   * @returns {Promise<number>} Number of events replayed
   */
  async replay(handler, criteria = {}) {
    if (typeof handler !== 'function') throw new TypeError('Handler must be a function');

    let events = this.eventLog;

    if (criteria.topic) {
      events = events.filter(e => e.topic === criteria.topic);
    }

    if (criteria.since) {
      events = events.filter(e => e.persistedAt >= criteria.since);
    }

    let count = 0;
    for (const event of events) {
      try {
        await handler(event);
        count++;
      } catch (error) {
        console.error(`Replay handler error: ${error.message}`);
      }
    }

    return count;
  }

  /**
   * Get total recorded events
   * @returns {number} Event count
   */
  size() {
    return this.eventLog.length;
  }

  /**
   * Clear event log
   * @returns {void}
   */
  clear() {
    this.eventLog = [];
  }

  /**
   * Enable/disable persistence
   * @param {boolean} enabled - Enable flag
   * @returns {void}
   */
  setEnabled(enabled) {
    this.enabled = !!enabled;
  }

  /**
   * Export all events
   * @returns {object[]} Copy of event log
   */
  export() {
    return JSON.parse(JSON.stringify(this.eventLog));
  }
}

/**
 * Event Bus - Main pub/sub implementation
 * @class EventBus
 */
class EventBus {
  /**
   * @param {object} options - Configuration options
   * @param {boolean} [options.persistence=true] - Enable event persistence
   * @param {number} [options.maxQueueSize=1000] - Max queue size
   * @throws {TypeError} If options are invalid
   */
  constructor(options = {}) {
    const { persistence = true, maxQueueSize = 1000 } = options;

    if (typeof maxQueueSize !== 'number' || maxQueueSize <= 0) {
      throw new TypeError('maxQueueSize must be a positive number');
    }

    /** @type {EventRouter} Router instance */
    this.router = new EventRouter();
    /** @type {MessageQueue} Message queue */
    this.messageQueue = new MessageQueue();
    /** @type {PersistenceLayer} Persistence layer */
    this.persistence = new PersistenceLayer();
    this.persistence.setEnabled(persistence);

    /** @type {number} Max queue size */
    this.maxQueueSize = maxQueueSize;
    /** @type {object} Statistics */
    this.stats = { published: 0, delivered: 0, failed: 0 };
    /** @type {boolean} Whether processing is active */
    this.isProcessing = false;
  }

  /**
   * Publish an event
   * @param {string} topic - Topic name
   * @param {*} data - Event data
   * @param {object} [options] - Publishing options
   * @param {boolean} [options.async=true] - Async processing
   * @param {number} [options.delay=0] - Delay in milliseconds
   * @throws {TypeError} If topic is not string
   * @returns {Promise<object>} Published event
   */
  async publish(topic, data, options = {}) {
    if (typeof topic !== 'string') throw new TypeError('Topic must be a string');

    const { async: isAsync = true, delay = 0 } = options;

    const event = {
      topic,
      data,
      timestamp: Date.now(),
      id: `${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
    };

    if (delay > 0) {
      await new Promise(resolve => setTimeout(resolve, delay));
    }

    this.persistence.record(event);

    if (isAsync) {
      if (this.messageQueue.size() >= this.maxQueueSize) {
        throw new Error('Message queue full');
      }
      this.messageQueue.enqueue(event);
      this.stats.published++;

      if (!this.isProcessing) {
        this._processQueue();
      }

      return event;
    } else {
      return this._deliverEvent(event);
    }
  }

  /**
   * Subscribe to topic
   * @param {string} topic - Topic name
   * @param {Function} handler - Event handler
   * @returns {Function} Unsubscribe function
   */
  subscribe(topic, handler) {
    return this.router.subscribe(topic, handler);
  }

  /**
   * Subscribe with wildcard pattern
   * @param {string} pattern - Pattern like 'orders.*'
   * @param {Function} handler - Event handler
   * @returns {Function} Unsubscribe function
   */
  subscribeWildcard(pattern, handler) {
    return this.router.subscribeWildcard(pattern, handler);
  }

  /**
   * Subscribe with filter predicate
   * @param {Function} predicate - Filter function
   * @param {Function} handler - Event handler
   * @returns {Function} Unsubscribe function
   */
  subscribeWithFilter(predicate, handler) {
    return this.router.subscribeWithFilter(predicate, handler);
  }

  /**
   * Deliver event to all subscribers
   * @private
   * @param {object} event - Event to deliver
   * @returns {Promise<object>} Event with delivery results
   */
  async _deliverEvent(event) {
    const handlers = this.router.route(event.topic, event);
    const results = [];

    for (const handler of handlers) {
      try {
        await handler(event);
        results.push({ success: true });
        this.stats.delivered++;
      } catch (error) {
        results.push({ success: false, error: error.message });
        this.stats.failed++;
      }
    }

    return { ...event, deliveryResults: results };
  }

  /**
   * Process message queue
   * @private
   * @returns {Promise<void>}
   */
  async _processQueue() {
    if (this.isProcessing) return;
    this.isProcessing = true;

    try {
      while (!this.messageQueue.isEmpty()) {
        const event = this.messageQueue.dequeue();
        await this._deliverEvent(event);
      }
    } finally {
      this.isProcessing = false;
    }
  }

  /**
   * Replay persisted events
   * @param {object} criteria - Replay criteria
   * @param {Function} handler - Handler for replayed events
   * @returns {Promise<number>} Number of events replayed
   */
  async replay(criteria, handler) {
    return this.persistence.replay(handler, criteria);
  }

  /**
   * Clear all subscribers
   * @returns {void}
   */
  clear() {
    this.router.clear();
  }

  /**
   * Get event bus statistics
   * @returns {object} Statistics
   */
  getStats() {
    return {
      ...this.stats,
      queueSize: this.messageQueue.size(),
      persistedEvents: this.persistence.size(),
      isProcessing: this.isProcessing,
    };
  }

  /**
   * Get subscriber count for topic
   * @param {string} topic - Topic name
   * @returns {number} Subscriber count
   */
  getSubscriberCount(topic) {
    return this.router.getSubscriberCount(topic);
  }

  /**
   * Export persisted events
   * @returns {object[]} Array of events
   */
  exportEvents() {
    return this.persistence.export();
  }

  /**
   * Enable/disable event persistence
   * @param {boolean} enabled - Enable flag
   * @returns {void}
   */
  setPersistenceEnabled(enabled) {
    this.persistence.setEnabled(enabled);
  }

  /**
   * Wait for queue to be empty
   * @returns {Promise<void>}
   */
  async drain() {
    while (!this.messageQueue.isEmpty() || this.isProcessing) {
      await new Promise(resolve => setTimeout(resolve, 10));
    }
  }
}

module.exports = {
  EventBus,
  EventRouter,
  MessageQueue,
  PersistenceLayer,
};
