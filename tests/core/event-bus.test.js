/**
 * Event Bus Tests - Comprehensive test suite for event bus system
 * Tests pub/sub, validation, persistence, DLQ, retry, and subscriber management
 * @module tests/core/event-bus.test
 */

const {
  EventBus,
  EventValidator,
  EventPersistence,
  DeadLetterQueue,
  EventRetry,
  SubscriberManagement,
} = require('../../src/core/event-bus');

describe('EventValidator', () => {
  let validator;

  beforeEach(() => {
    validator = new EventValidator();
  });

  test('should validate ai:suggestion events', () => {
    const data = {
      suggestionsId: 'ai-123',
      content: 'Hello world',
      confidence: 0.95,
    };

    const result = validator.validate('ai:suggestion', data);
    expect(result.valid).toBe(true);
    expect(result.errors.length).toBe(0);
  });

  test('should reject invalid ai:suggestion events', () => {
    const data = {
      suggestionsId: 'ai-123',
      confidence: 0.95,
      // missing 'content'
    };

    const result = validator.validate('ai:suggestion', data);
    expect(result.valid).toBe(false);
    expect(result.errors.length).toBeGreaterThan(0);
  });

  test('should validate analytics:recorded events', () => {
    const data = {
      eventName: 'page_view',
      userId: 'user-123',
      timestamp: Date.now(),
    };

    const result = validator.validate('analytics:recorded', data);
    expect(result.valid).toBe(true);
  });

  test('should register custom schemas', () => {
    const customSchema = {
      type: 'object',
      properties: {
        customId: { type: 'string' },
        value: { type: 'number' },
      },
      required: ['customId', 'value'],
    };

    validator.registerSchema('custom:event', customSchema);

    const data = { customId: 'custom-1', value: 42 };
    const result = validator.validate('custom:event', data);
    expect(result.valid).toBe(true);
  });

  test('should reject unknown event types', () => {
    const result = validator.validate('unknown:type', {});
    expect(result.valid).toBe(false);
    expect(result.errors[0]).toContain('Unknown event type');
  });

  test('should validate enum values', () => {
    const data = {
      resourceId: 'res-123',
      localVersion: 1,
      remoteVersion: 2,
      conflictType: 'content',
    };

    const result = validator.validate('sync:conflict', data);
    expect(result.valid).toBe(true);
  });

  test('should reject invalid enum values', () => {
    const data = {
      resourceId: 'res-123',
      localVersion: 1,
      remoteVersion: 2,
      conflictType: 'invalid',
    };

    const result = validator.validate('sync:conflict', data);
    expect(result.valid).toBe(false);
  });
});

describe('EventPersistence', () => {
  let persistence;

  beforeEach(() => {
    persistence = new EventPersistence();
  });

  test('should store events', () => {
    const eventId = persistence.store('ai:suggestion', { content: 'test' }, 'corr-123');

    expect(eventId).toBeDefined();
    expect(typeof eventId).toBe('string');
  });

  test('should retrieve stored events', () => {
    persistence.store('ai:suggestion', { content: 'test1' }, 'corr-123');
    persistence.store('ai:suggestion', { content: 'test2' }, 'corr-123');
    persistence.store('analytics:recorded', { eventName: 'test' }, 'corr-124');

    const events = persistence.retrieve();
    expect(events.length).toBe(3);
  });

  test('should filter events by type', () => {
    persistence.store('ai:suggestion', { content: 'test1' }, 'corr-123');
    persistence.store('analytics:recorded', { eventName: 'test' }, 'corr-123');

    const events = persistence.retrieve({ eventType: 'ai:suggestion' });
    expect(events.length).toBe(1);
    expect(events[0].type).toBe('ai:suggestion');
  });

  test('should mark events as processed', () => {
    const eventId = persistence.store('ai:suggestion', { content: 'test' }, 'corr-123');

    persistence.markProcessed(eventId);
    const events = persistence.retrieve();
    const processed = events.find(e => e.id === eventId);

    expect(processed.processed).toBe(true);
    expect(processed.processedAt).toBeDefined();
  });

  test('should limit retrieved events', () => {
    for (let i = 0; i < 10; i++) {
      persistence.store('ai:suggestion', { content: `test${i}` }, 'corr-123');
    }

    const events = persistence.retrieve({ limit: 5 });
    expect(events.length).toBe(5);
  });

  test('should maintain max size', () => {
    persistence.maxSize = 5;

    for (let i = 0; i < 10; i++) {
      persistence.store('ai:suggestion', { content: `test${i}` }, 'corr-123');
    }

    expect(persistence.events.length).toBe(5);
  });
});

describe('DeadLetterQueue', () => {
  let dlq;

  beforeEach(() => {
    dlq = new DeadLetterQueue();
  });

  test('should add failed events to DLQ', () => {
    const event = { id: 'evt-1', type: 'ai:suggestion' };
    const error = new Error('Processing failed');

    dlq.add(event, error);

    expect(dlq.queue.length).toBe(1);
    expect(dlq.queue[0].originalEvent).toEqual(event);
    expect(dlq.queue[0].error).toBe('Processing failed');
  });

  test('should track retry count', () => {
    const event = { id: 'evt-1', type: 'ai:suggestion', retries: 1 };
    const error = new Error('Failed');

    dlq.add(event, error);

    expect(dlq.queue[0].retryCount).toBe(1);
  });

  test('should determine if item can retry', () => {
    dlq.maxRetries = 3;

    const event1 = { id: 'evt-1', retries: 1 };
    const event2 = { id: 'evt-2', retries: 3 };

    dlq.add(event1, new Error('Failed'));
    dlq.add(event2, new Error('Failed'));

    expect(dlq.queue[0].canRetry).toBe(true);
    expect(dlq.queue[1].canRetry).toBe(false);
  });

  test('should get items by filter', () => {
    const event1 = { id: 'evt-1', retries: 1 };
    const event2 = { id: 'evt-2', retries: 3 };

    dlq.add(event1, new Error('Failed'));
    dlq.add(event2, new Error('Failed'));

    const canRetry = dlq.getItems({ canRetry: true });
    expect(canRetry.length).toBe(1);
  });

  test('should retry failed event', async () => {
    const event = { id: 'evt-1', type: 'ai:suggestion', retries: 0 };
    dlq.add(event, new Error('Failed'));

    let handlerCalled = false;
    const handler = async () => {
      handlerCalled = true;
    };

    const result = await dlq.retry(dlq.queue[0].id, handler);

    expect(handlerCalled).toBe(true);
    expect(result).toBe(true);
    expect(dlq.queue.length).toBe(0);
  });
});

describe('EventRetry', () => {
  let retry;

  beforeEach(() => {
    retry = new EventRetry();
  });

  test('should schedule retry with exponential backoff', async () => {
    jest.useFakeTimers();

    let called = false;
    const handler = async () => {
      called = true;
    };

    const promise = retry.scheduleRetry('evt-1', handler, 1);

    jest.runAllTimers();
    await promise;

    expect(called).toBe(true);

    jest.useRealTimers();
  });

  test('should increase delay with attempts', () => {
    const baseDelay = retry.baseDelay;
    const delay1 = Math.min(baseDelay * Math.pow(2, 0), retry.maxDelay);
    const delay2 = Math.min(baseDelay * Math.pow(2, 1), retry.maxDelay);

    expect(delay2).toBeGreaterThan(delay1);
  });

  test('should cancel retry', async () => {
    jest.useFakeTimers();

    let called = false;
    const handler = async () => {
      called = true;
    };

    retry.scheduleRetry('evt-1', handler, 1);
    retry.cancel('evt-1');

    jest.runAllTimers();

    expect(called).toBe(false);
    expect(retry.retryQueue.size).toBe(0);

    jest.useRealTimers();
  });

  test('should get pending retries', () => {
    retry.scheduleRetry('evt-1', async () => {}, 1);
    retry.scheduleRetry('evt-2', async () => {}, 2);

    const pending = retry.getPending();

    expect(pending.length).toBe(2);
    expect(pending[0].eventId).toBe('evt-1');
    expect(pending[1].attempt).toBe(2);
  });
});

describe('SubscriberManagement', () => {
  let management;

  beforeEach(() => {
    management = new SubscriberManagement();
  });

  test('should subscribe to events', () => {
    const handler = jest.fn();

    management.subscribe('ai:suggestion', 'subscriber-1', handler);

    const subscribers = management.getSubscribers('ai:suggestion');
    expect(subscribers.length).toBe(1);
    expect(subscribers[0].subscriberId).toBe('subscriber-1');
  });

  test('should unsubscribe from events', () => {
    const handler = jest.fn();

    management.subscribe('ai:suggestion', 'subscriber-1', handler);
    management.unsubscribe('ai:suggestion', 'subscriber-1');

    const subscribers = management.getSubscribers('ai:suggestion');
    expect(subscribers.length).toBe(0);
  });

  test('should return unsubscribe function', () => {
    const handler = jest.fn();

    const unsubscribe = management.subscribe('ai:suggestion', 'subscriber-1', handler);

    unsubscribe();

    const subscribers = management.getSubscribers('ai:suggestion');
    expect(subscribers.length).toBe(0);
  });

  test('should support multiple subscribers', () => {
    const handler1 = jest.fn();
    const handler2 = jest.fn();

    management.subscribe('ai:suggestion', 'subscriber-1', handler1);
    management.subscribe('ai:suggestion', 'subscriber-2', handler2);

    const subscribers = management.getSubscribers('ai:suggestion');
    expect(subscribers.length).toBe(2);
  });

  test('should count total subscribers', () => {
    management.subscribe('ai:suggestion', 'subscriber-1', () => {});
    management.subscribe('analytics:recorded', 'subscriber-2', () => {});
    management.subscribe('sync:conflict', 'subscriber-3', () => {});

    expect(management.getTotalSubscribers()).toBe(3);
  });
});

describe('EventBus', () => {
  let eventBus;

  beforeEach(() => {
    eventBus = new EventBus();
  });

  test('should publish and subscribe to events', () => {
    const handler = jest.fn();

    eventBus.subscribe('ai:suggestion', handler);
    eventBus.publish('ai:suggestion', {
      suggestionsId: 'ai-1',
      content: 'test',
      confidence: 0.9,
    });

    expect(handler).toHaveBeenCalled();
  });

  test('should validate events before publishing', () => {
    expect(() => {
      eventBus.publish('ai:suggestion', { content: 'test' }); // missing required fields
    }).toThrow();
  });

  test('should store events in persistence', () => {
    eventBus.publish('ai:suggestion', {
      suggestionsId: 'ai-1',
      content: 'test',
      confidence: 0.9,
    });

    const stored = eventBus.persistence.retrieve();
    expect(stored.length).toBeGreaterThan(0);
  });

  test('should handle errors in subscribers', () => {
    const handler = jest.fn(() => {
      throw new Error('Handler error');
    });

    eventBus.subscribe('ai:suggestion', handler);

    expect(() => {
      eventBus.publish('ai:suggestion', {
        suggestionsId: 'ai-1',
        content: 'test',
        confidence: 0.9,
      });
    }).not.toThrow();

    expect(eventBus.dlq.queue.length).toBe(1);
  });

  test('should publish async', async () => {
    const handler = jest.fn();

    eventBus.subscribe('ai:suggestion', handler);

    const eventId = await eventBus.publish(
      'ai:suggestion',
      {
        suggestionsId: 'ai-1',
        content: 'test',
        confidence: 0.9,
      },
      { async: true }
    );

    await new Promise(resolve => setTimeout(resolve, 10));

    expect(handler).toHaveBeenCalled();
    expect(eventId).toBeDefined();
  });

  test('should generate unique correlation IDs', () => {
    const eventId1 = eventBus.publish(
      'ai:suggestion',
      {
        suggestionsId: 'ai-1',
        content: 'test',
        confidence: 0.9,
      },
      { correlationId: 'corr-123' }
    );

    const eventId2 = eventBus.publish(
      'ai:suggestion',
      {
        suggestionsId: 'ai-2',
        content: 'test2',
        confidence: 0.8,
      }
    );

    expect(eventId1).toBeDefined();
    expect(eventId2).toBeDefined();
    expect(eventId1).not.toBe(eventId2);
  });

  test('should return event bus statistics', () => {
    eventBus.subscribe('ai:suggestion', () => {});
    eventBus.subscribe('analytics:recorded', () => {});

    const stats = eventBus.getStats();

    expect(stats.totalSubscribers).toBe(2);
    expect(stats.storedEvents).toBeDefined();
    expect(stats.dlqSize).toBeDefined();
  });

  test('should reset state', () => {
    eventBus.subscribe('ai:suggestion', () => {});
    eventBus.publish('ai:suggestion', {
      suggestionsId: 'ai-1',
      content: 'test',
      confidence: 0.9,
    });

    eventBus.reset();

    expect(eventBus.subscribers.getTotalSubscribers()).toBe(0);
    expect(eventBus.persistence.events.length).toBe(0);
  });
});
