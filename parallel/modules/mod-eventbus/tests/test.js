/**
 * HELIOS v4.0 Event Bus - Comprehensive Test Suite
 */

const assert = require('assert');
const { EventBus, EventRouter, MessageQueue, PersistenceLayer } = require('../implementation');

describe('MessageQueue', () => {
  let queue;

  beforeEach(() => {
    queue = new MessageQueue();
  });

  it('should enqueue messages', () => {
    queue.enqueue({ type: 'test' });
    assert.strictEqual(queue.size(), 1);
  });

  it('should throw on non-object message', () => {
    assert.throws(() => queue.enqueue('string'), TypeError);
    assert.throws(() => queue.enqueue(null), TypeError);
  });

  it('should dequeue in FIFO order', () => {
    queue.enqueue({ id: 1 });
    queue.enqueue({ id: 2 });
    const msg1 = queue.dequeue();
    const msg2 = queue.dequeue();
    assert.strictEqual(msg1.id, 1);
    assert.strictEqual(msg2.id, 2);
  });

  it('should peek without removing', () => {
    queue.enqueue({ id: 1 });
    const peeked = queue.peek();
    assert.strictEqual(peeked.id, 1);
    assert.strictEqual(queue.size(), 1);
  });

  it('should report empty state', () => {
    assert.strictEqual(queue.isEmpty(), true);
    queue.enqueue({ id: 1 });
    assert.strictEqual(queue.isEmpty(), false);
  });

  it('should clear queue', () => {
    queue.enqueue({ id: 1 });
    queue.enqueue({ id: 2 });
    queue.clear();
    assert.strictEqual(queue.size(), 0);
  });

  it('should return null on dequeue from empty', () => {
    assert.strictEqual(queue.dequeue(), null);
  });

  it('should process messages with handler', async () => {
    queue.enqueue({ id: 1 });
    queue.enqueue({ id: 2 });
    const processed = [];
    await queue.process(msg => {
      processed.push(msg.id);
      return Promise.resolve();
    });
    assert.strictEqual(processed.length, 2);
  });

  it('should throw on non-function handler', async () => {
    assert.rejects(() => queue.process('not-function'), TypeError);
  });

  it('should add queuedAt timestamp', () => {
    queue.enqueue({ type: 'test' });
    const msg = queue.dequeue();
    assert.strictEqual(typeof msg.queuedAt, 'number');
  });
});

describe('EventRouter', () => {
  let router;

  beforeEach(() => {
    router = new EventRouter();
  });

  describe('Exact Topic Subscriptions', () => {
    it('should subscribe to exact topic', () => {
      const handler = () => {};
      router.subscribe('orders.created', handler);
      assert.strictEqual(router.getSubscriberCount('orders.created'), 1);
    });

    it('should throw on non-string topic', () => {
      assert.throws(() => router.subscribe(123, () => {}), TypeError);
    });

    it('should throw on non-function handler', () => {
      assert.throws(() => router.subscribe('topic', 'not-function'), TypeError);
    });

    it('should unsubscribe handler', () => {
      const handler = () => {};
      router.subscribe('topic', handler);
      router.unsubscribe('topic', handler);
      assert.strictEqual(router.getSubscriberCount('topic'), 0);
    });

    it('should return unsubscribe function', () => {
      const handler = () => {};
      const unsub = router.subscribe('topic', handler);
      assert.strictEqual(typeof unsub, 'function');
      unsub();
      assert.strictEqual(router.getSubscriberCount('topic'), 0);
    });

    it('should route to exact subscribers', () => {
      const handlers = [];
      router.subscribe('orders.created', () => handlers.push(1));
      router.subscribe('orders.updated', () => handlers.push(2));
      const routed = router.route('orders.created', {});
      routed.forEach(h => h());
      assert.strictEqual(handlers.includes(1), true);
      assert.strictEqual(handlers.includes(2), false);
    });
  });

  describe('Wildcard Subscriptions', () => {
    it('should subscribe with wildcard pattern', () => {
      router.subscribeWildcard('orders.*', () => {});
      assert.strictEqual(router.wildcardRoutes.length, 1);
    });

    it('should throw on non-string pattern', () => {
      assert.throws(() => router.subscribeWildcard(123, () => {}), TypeError);
    });

    it('should throw on non-function handler', () => {
      assert.throws(() => router.subscribeWildcard('pattern', 'not-function'), TypeError);
    });

    it('should match wildcard patterns', () => {
      router.subscribeWildcard('orders.*', () => {});
      const handlers = router.route('orders.created', {});
      assert.strictEqual(handlers.length, 1);
    });

    it('should not match non-matching patterns', () => {
      router.subscribeWildcard('orders.*', () => {});
      const handlers = router.route('users.created', {});
      assert.strictEqual(handlers.length, 0);
    });

    it('should support multi-level wildcards', () => {
      router.subscribeWildcard('user.*.updated', () => {});
      const handlers = router.route('user.profile.updated', {});
      assert.strictEqual(handlers.length, 1);
    });

    it('should return unsubscribe function', () => {
      const unsub = router.subscribeWildcard('orders.*', () => {});
      unsub();
      assert.strictEqual(router.wildcardRoutes.length, 0);
    });
  });

  describe('Filter Subscriptions', () => {
    it('should subscribe with filter predicate', () => {
      router.subscribeWithFilter(() => true, () => {});
      assert.strictEqual(router.filterRoutes.size, 1);
    });

    it('should throw on non-function predicate', () => {
      assert.throws(() => router.subscribeWithFilter('not-function', () => {}), TypeError);
    });

    it('should throw on non-function handler', () => {
      assert.throws(() => router.subscribeWithFilter(() => true, 'not-function'), TypeError);
    });

    it('should filter events by predicate', () => {
      router.subscribeWithFilter(
        event => event.amount > 100,
        () => {}
      );
      const handlers = router.route('order', { amount: 150 });
      assert.strictEqual(handlers.length, 1);
    });

    it('should exclude non-matching events', () => {
      router.subscribeWithFilter(
        event => event.amount > 100,
        () => {}
      );
      const handlers = router.route('order', { amount: 50 });
      assert.strictEqual(handlers.length, 0);
    });

    it('should handle predicate errors', () => {
      router.subscribeWithFilter(
        () => { throw new Error('Test error'); },
        () => {}
      );
      assert.doesNotThrow(() => router.route('topic', {}));
    });
  });

  it('should clear all subscriptions', () => {
    router.subscribe('topic', () => {});
    router.subscribeWildcard('pattern.*', () => {});
    router.clear();
    assert.strictEqual(router.exactRoutes.size, 0);
    assert.strictEqual(router.wildcardRoutes.length, 0);
  });
});

describe('PersistenceLayer', () => {
  let persistence;

  beforeEach(() => {
    persistence = new PersistenceLayer();
  });

  it('should record events', () => {
    persistence.record({ topic: 'orders', data: { id: 1 } });
    assert.strictEqual(persistence.size(), 1);
  });

  it('should throw on non-object event', () => {
    assert.throws(() => persistence.record('not-object'), TypeError);
    assert.throws(() => persistence.record(null), TypeError);
  });

  it('should add metadata to recorded events', () => {
    persistence.record({ topic: 'orders' });
    const event = persistence.export()[0];
    assert.strictEqual(typeof event.persistedAt, 'number');
    assert.strictEqual(typeof event.id, 'string');
  });

  it('should query events by topic', () => {
    persistence.record({ topic: 'orders', data: {} });
    persistence.record({ topic: 'users', data: {} });
    const results = persistence.query('orders');
    assert.strictEqual(results.length, 1);
    assert.strictEqual(results[0].topic, 'orders');
  });

  it('should support query offset and limit', () => {
    for (let i = 0; i < 10; i++) {
      persistence.record({ topic: 'test' });
    }
    const results = persistence.query('test', { limit: 3, offset: 2 });
    assert.strictEqual(results.length, 3);
  });

  it('should replay events', async () => {
    persistence.record({ topic: 'orders', data: { id: 1 } });
    persistence.record({ topic: 'orders', data: { id: 2 } });
    const replayed = [];
    const count = await persistence.replay(
      event => { replayed.push(event); return Promise.resolve(); },
      { topic: 'orders' }
    );
    assert.strictEqual(count, 2);
    assert.strictEqual(replayed.length, 2);
  });

  it('should throw on non-function handler', async () => {
    assert.rejects(() => persistence.replay('not-function'), TypeError);
  });

  it('should filter replay by since timestamp', async () => {
    const now = Date.now();
    persistence.record({ topic: 'test', data: { id: 1 } });
    await new Promise(r => setTimeout(r, 10));
    persistence.record({ topic: 'test', data: { id: 2 } });
    
    const replayed = [];
    await persistence.replay(
      event => { replayed.push(event); return Promise.resolve(); },
      { since: now + 5 }
    );
    assert.strictEqual(replayed.length, 1);
  });

  it('should export event log', () => {
    persistence.record({ topic: 'test', data: 1 });
    const exported = persistence.export();
    assert.strictEqual(Array.isArray(exported), true);
    assert.strictEqual(exported.length, 1);
  });

  it('should enable/disable persistence', () => {
    persistence.setEnabled(false);
    persistence.record({ topic: 'test' });
    assert.strictEqual(persistence.size(), 0);
    persistence.setEnabled(true);
    persistence.record({ topic: 'test' });
    assert.strictEqual(persistence.size(), 1);
  });

  it('should clear event log', () => {
    persistence.record({ topic: 'test' });
    persistence.clear();
    assert.strictEqual(persistence.size(), 0);
  });

  it('should maintain max events limit', () => {
    persistence.maxEvents = 5;
    for (let i = 0; i < 10; i++) {
      persistence.record({ topic: 'test', id: i });
    }
    assert.strictEqual(persistence.size(), 5);
  });
});

describe('EventBus', () => {
  let bus;

  beforeEach(() => {
    bus = new EventBus();
  });

  describe('Publishing', () => {
    it('should publish events', async () => {
      const event = await bus.publish('orders.created', { orderId: 1 });
      assert.strictEqual(event.topic, 'orders.created');
      assert.strictEqual(event.data.orderId, 1);
    });

    it('should throw on non-string topic', async () => {
      assert.rejects(() => bus.publish(123, {}), TypeError);
    });

    it('should add timestamp to events', async () => {
      const event = await bus.publish('test', {});
      assert.strictEqual(typeof event.timestamp, 'number');
    });

    it('should add unique ID to events', async () => {
      const e1 = await bus.publish('test', {});
      const e2 = await bus.publish('test', {});
      assert.notStrictEqual(e1.id, e2.id);
    });

    it('should support delayed publishing', async () => {
      const start = Date.now();
      await bus.publish('test', {}, { delay: 100 });
      const elapsed = Date.now() - start;
      assert.strictEqual(elapsed >= 100, true);
    });

    it('should throw on queue overflow', async () => {
      bus = new EventBus({ maxQueueSize: 2 });
      await bus.publish('test1', {});
      await bus.publish('test2', {});
      assert.rejects(() => bus.publish('test3', {}), Error);
    });
  });

  describe('Subscriptions', () => {
    it('should subscribe to exact topic', async () => {
      let received;
      bus.subscribe('orders.created', (event) => {
        received = event;
      });
      await bus.publish('orders.created', { id: 1 });
      await bus.drain();
      assert.strictEqual(received.data.id, 1);
    });

    it('should subscribe to wildcard pattern', async () => {
      let received;
      bus.subscribeWildcard('orders.*', (event) => {
        received = event;
      });
      await bus.publish('orders.created', { id: 1 });
      await bus.drain();
      assert.strictEqual(received.data.id, 1);
    });

    it('should subscribe with filter', async () => {
      let received;
      bus.subscribeWithFilter(
        event => event.data.amount > 100,
        (event) => {
          received = event;
        }
      );
      await bus.publish('payment', { amount: 150 });
      await bus.drain();
      assert.strictEqual(received.data.amount, 150);
    });

    it('should return unsubscribe function', () => {
      const unsub = bus.subscribe('test', () => {});
      assert.strictEqual(typeof unsub, 'function');
    });

    it('should unsubscribe successfully', async () => {
      const handler = () => {};
      const unsub = bus.subscribe('test', handler);
      unsub();
      assert.strictEqual(bus.getSubscriberCount('test'), 0);
    });
  });

  describe('Event Delivery', () => {
    it('should deliver to all subscribers', async () => {
      const results = [];
      bus.subscribe('test', () => results.push(1));
      bus.subscribe('test', () => results.push(2));
      await bus.publish('test', {});
      await bus.drain();
      assert.strictEqual(results.length, 2);
    });

    it('should handle subscriber errors', async () => {
      bus.subscribe('test', () => { throw new Error('Handler error'); });
      bus.subscribe('test', () => {});
      assert.doesNotThrow(async () => {
        await bus.publish('test', {});
        await bus.drain();
      });
    });

    it('should track delivery statistics', async () => {
      bus.subscribe('test', () => {});
      await bus.publish('test', {});
      await bus.drain();
      const stats = bus.getStats();
      assert.strictEqual(stats.published, 1);
      assert.strictEqual(stats.delivered, 1);
    });
  });

  describe('Persistence', () => {
    it('should persist published events', async () => {
      await bus.publish('test', { data: 1 });
      assert.strictEqual(bus.exportEvents().length, 1);
    });

    it('should disable persistence when configured', async () => {
      bus = new EventBus({ persistence: false });
      await bus.publish('test', {});
      assert.strictEqual(bus.exportEvents().length, 0);
    });

    it('should replay persisted events', async () => {
      await bus.publish('orders', { id: 1 });
      await bus.publish('users', { id: 2 });
      const replayed = [];
      await bus.replay(
        { topic: 'orders' },
        event => { replayed.push(event); return Promise.resolve(); }
      );
      assert.strictEqual(replayed.length, 1);
      assert.strictEqual(replayed[0].topic, 'orders');
    });

    it('should enable/disable persistence', async () => {
      bus.setPersistenceEnabled(false);
      await bus.publish('test', {});
      assert.strictEqual(bus.exportEvents().length, 0);
      bus.setPersistenceEnabled(true);
      await bus.publish('test', {});
      assert.strictEqual(bus.exportEvents().length, 1);
    });
  });

  describe('Queue Management', () => {
    it('should process queue asynchronously', async () => {
      let processed = false;
      bus.subscribe('test', () => {
        processed = true;
      });
      await bus.publish('test', {});
      assert.strictEqual(bus.messageQueue.size() > 0, true);
      await bus.drain();
      assert.strictEqual(processed, true);
    });

    it('should drain queue', async () => {
      bus.subscribe('test', () => {});
      await bus.publish('test', {});
      await bus.publish('test', {});
      await bus.drain();
      assert.strictEqual(bus.messageQueue.isEmpty(), true);
    });

    it('should handle synchronous publishing', async () => {
      let processed = false;
      bus.subscribe('test', () => {
        processed = true;
      });
      await bus.publish('test', {}, { async: false });
      assert.strictEqual(processed, true);
    });
  });

  describe('Statistics', () => {
    it('should report event bus stats', async () => {
      bus.subscribe('test', () => {});
      await bus.publish('test', {});
      const stats = bus.getStats();
      assert.strictEqual(typeof stats.published, 'number');
      assert.strictEqual(typeof stats.delivered, 'number');
      assert.strictEqual(typeof stats.queueSize, 'number');
    });

    it('should count subscribers per topic', () => {
      bus.subscribe('test', () => {});
      bus.subscribe('test', () => {});
      assert.strictEqual(bus.getSubscriberCount('test'), 2);
    });
  });

  describe('Configuration', () => {
    it('should validate maxQueueSize', () => {
      assert.throws(() => new EventBus({ maxQueueSize: 0 }), TypeError);
      assert.throws(() => new EventBus({ maxQueueSize: -10 }), TypeError);
    });

    it('should clear subscriptions', () => {
      bus.subscribe('test', () => {});
      bus.subscribeWildcard('pattern.*', () => {});
      bus.clear();
      assert.strictEqual(bus.getSubscriberCount('test'), 0);
    });
  });
});
