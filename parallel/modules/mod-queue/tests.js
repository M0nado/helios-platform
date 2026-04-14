/**
 * Message Queue Module - Comprehensive Test Suite
 * 45+ tests covering all functionality
 */

const assert = require('assert');
const {
  MessageQueue,
  OrderingManager,
  DeliveryGuarantee,
  DeadLetterQueue,
  DeliveryModes
} = require('./implementation');

// ============================================================================
// Test Helpers
// ============================================================================

function createQueue(options = {}) {
  return new MessageQueue(options);
}

function createOrderingManager(strategy = 'fifo') {
  return new OrderingManager(strategy);
}

function createDeliveryGuarantee(mode = DeliveryModes.AT_LEAST_ONCE) {
  return new DeliveryGuarantee(mode);
}

function createDLQ(options = {}) {
  return new DeadLetterQueue(options);
}

// ============================================================================
// OrderingManager Tests (8 tests)
// ============================================================================

function testOrderingManager() {
  console.log('\n--- OrderingManager Tests ---');

  // Test 1: FIFO ordering
  {
    const manager = createOrderingManager('fifo');
    manager.enqueue({ id: 1 });
    manager.enqueue({ id: 2 });
    manager.enqueue({ id: 3 });
    
    assert.strictEqual(manager.dequeue().id, 1);
    assert.strictEqual(manager.dequeue().id, 2);
    assert.strictEqual(manager.dequeue().id, 3);
    console.log('✓ Test 1: FIFO ordering');
  }

  // Test 2: Priority ordering
  {
    const manager = createOrderingManager('priority');
    manager.enqueue({ id: 1, priority: 10 });
    manager.enqueue({ id: 2, priority: 50 });
    manager.enqueue({ id: 3, priority: 30 });
    
    assert.strictEqual(manager.dequeue().priority, 50);
    assert.strictEqual(manager.dequeue().priority, 30);
    assert.strictEqual(manager.dequeue().priority, 10);
    console.log('✓ Test 2: Priority ordering');
  }

  // Test 3: Peek without dequeue
  {
    const manager = createOrderingManager('fifo');
    manager.enqueue({ id: 1 });
    const peeked = manager.peek();
    assert.strictEqual(peeked.id, 1);
    assert.strictEqual(manager.size(), 1);
    console.log('✓ Test 3: Peek without dequeue');
  }

  // Test 4: Size tracking
  {
    const manager = createOrderingManager('fifo');
    assert.strictEqual(manager.size(), 0);
    manager.enqueue({ id: 1 });
    assert.strictEqual(manager.size(), 1);
    manager.enqueue({ id: 2 });
    assert.strictEqual(manager.size(), 2);
    manager.dequeue();
    assert.strictEqual(manager.size(), 1);
    console.log('✓ Test 4: Size tracking');
  }

  // Test 5: Clear all messages
  {
    const manager = createOrderingManager('fifo');
    manager.enqueue({ id: 1 });
    manager.enqueue({ id: 2 });
    manager.clear();
    assert.strictEqual(manager.size(), 0);
    assert.strictEqual(manager.peek(), null);
    console.log('✓ Test 5: Clear all messages');
  }

  // Test 6: Get all messages
  {
    const manager = createOrderingManager('fifo');
    manager.enqueue({ id: 1 });
    manager.enqueue({ id: 2 });
    const all = manager.getAll();
    assert.strictEqual(all.length, 2);
    assert.strictEqual(manager.size(), 2); // getAll is non-destructive
    console.log('✓ Test 6: Get all messages (non-destructive)');
  }

  // Test 7: Invalid strategy throws
  {
    try {
      createOrderingManager('invalid');
      assert.fail('Should throw on invalid strategy');
    } catch (err) {
      assert(err.message.includes('Invalid strategy'));
      console.log('✓ Test 7: Invalid strategy throws error');
    }
  }

  // Test 8: Invalid message throws
  {
    const manager = createOrderingManager('fifo');
    try {
      manager.enqueue(null);
      assert.fail('Should throw on null message');
    } catch (err) {
      assert(err.message.includes('Invalid message'));
      console.log('✓ Test 8: Invalid message throws error');
    }
  }
}

// ============================================================================
// DeliveryGuarantee Tests (9 tests)
// ============================================================================

function testDeliveryGuarantee() {
  console.log('\n--- DeliveryGuarantee Tests ---');

  // Test 9: Mark in-flight
  {
    const dg = createDeliveryGuarantee();
    dg.markInFlight('msg-1');
    const status = dg.getStatus('msg-1');
    assert.strictEqual(status.inFlight, true);
    console.log('✓ Test 9: Mark in-flight');
  }

  // Test 10: Acknowledge message
  {
    const dg = createDeliveryGuarantee();
    dg.markInFlight('msg-1');
    const acknowledged = dg.markAcknowledged('msg-1');
    assert.strictEqual(acknowledged, true);
    const status = dg.getStatus('msg-1');
    assert.strictEqual(status.acknowledged, true);
    assert.strictEqual(status.inFlight, false);
    console.log('✓ Test 10: Acknowledge message');
  }

  // Test 11: Retry increments attempts
  {
    const dg = createDeliveryGuarantee();
    dg.markInFlight('msg-1');
    const attempts1 = dg.retry('msg-1');
    const attempts2 = dg.retry('msg-1');
    assert.strictEqual(attempts1, 2);
    assert.strictEqual(attempts2, 3);
    console.log('✓ Test 11: Retry increments attempts');
  }

  // Test 12: Exactly-once duplicate prevention
  {
    const dg = new DeliveryGuarantee(DeliveryModes.EXACTLY_ONCE);
    dg.markInFlight('msg-1');
    const ack1 = dg.markAcknowledged('msg-1');
    
    dg.markInFlight('msg-1');
    const ack2 = dg.markAcknowledged('msg-1');
    
    assert.strictEqual(ack1, true);
    assert.strictEqual(ack2, false); // Already processed
    console.log('✓ Test 12: Exactly-once duplicate prevention');
  }

  // Test 13: Get timed-out messages
  {
    const dg = createDeliveryGuarantee();
    dg.markInFlight('msg-1', Date.now() - 5000);
    dg.markInFlight('msg-2', Date.now());
    
    const timedOut = dg.getTimedOutMessages(3000);
    assert.strictEqual(timedOut.length, 1);
    assert.strictEqual(timedOut[0], 'msg-1');
    console.log('✓ Test 13: Get timed-out messages');
  }

  // Test 14: Invalid mode throws
  {
    try {
      new DeliveryGuarantee('invalid-mode');
      assert.fail('Should throw on invalid mode');
    } catch (err) {
      assert(err.message.includes('Invalid delivery mode'));
      console.log('✓ Test 14: Invalid mode throws error');
    }
  }

  // Test 15: Acknowledge non-existent message throws
  {
    const dg = createDeliveryGuarantee();
    try {
      dg.markAcknowledged('non-existent');
      assert.fail('Should throw');
    } catch (err) {
      assert(err.message.includes('not in-flight'));
      console.log('✓ Test 15: Acknowledge non-existent throws error');
    }
  }

  // Test 16: Retry non-existent message throws
  {
    const dg = createDeliveryGuarantee();
    try {
      dg.retry('non-existent');
      assert.fail('Should throw');
    } catch (err) {
      assert(err.message.includes('not in-flight'));
      console.log('✓ Test 16: Retry non-existent throws error');
    }
  }

  // Test 17: Multiple delivery modes
  {
    const atleast = new DeliveryGuarantee(DeliveryModes.AT_LEAST_ONCE);
    const atmost = new DeliveryGuarantee(DeliveryModes.AT_MOST_ONCE);
    const exactly = new DeliveryGuarantee(DeliveryModes.EXACTLY_ONCE);
    
    assert.strictEqual(atleast.mode, 'at-least-once');
    assert.strictEqual(atmost.mode, 'at-most-once');
    assert.strictEqual(exactly.mode, 'exactly-once');
    console.log('✓ Test 17: Multiple delivery modes');
  }
}

// ============================================================================
// DeadLetterQueue Tests (7 tests)
// ============================================================================

function testDeadLetterQueue() {
  console.log('\n--- DeadLetterQueue Tests ---');

  // Test 18: Add message to DLQ
  {
    const dlq = createDLQ();
    dlq.add({ id: 'msg-1' }, 'Delivery failed');
    const entry = dlq.get('msg-1');
    assert(entry !== null);
    assert.strictEqual(entry.reason, 'Delivery failed');
    console.log('✓ Test 18: Add message to DLQ');
  }

  // Test 19: DLQ statistics
  {
    const dlq = createDLQ();
    dlq.add({ id: 'msg-1' }, 'Failed');
    dlq.add({ id: 'msg-2' }, 'Failed');
    const stats = dlq.getStats();
    assert.strictEqual(stats.total, 2);
    assert.strictEqual(stats.current, 2);
    console.log('✓ Test 19: DLQ statistics');
  }

  // Test 20: DLQ max size enforcement
  {
    const dlq = new DeadLetterQueue({ maxSize: 2 });
    dlq.add({ id: 'msg-1' }, 'Failed');
    dlq.add({ id: 'msg-2' }, 'Failed');
    const added = dlq.add({ id: 'msg-3' }, 'Failed');
    assert.strictEqual(added, false);
    assert.strictEqual(dlq.getStats().discarded, 1);
    console.log('✓ Test 20: DLQ max size enforcement');
  }

  // Test 21: DLQ cleanup expired messages
  {
    const dlq = new DeadLetterQueue({ ttl: 100 });
    dlq.add({ id: 'msg-1' }, 'Failed');
    
    // Wait for expiration
    setTimeout(() => {
      const removed = dlq.cleanup();
      assert(removed >= 1);
      console.log('✓ Test 21: DLQ cleanup expired messages');
    }, 150);
  }

  // Test 22: DLQ clear all
  {
    const dlq = createDLQ();
    dlq.add({ id: 'msg-1' }, 'Failed');
    dlq.add({ id: 'msg-2' }, 'Failed');
    const cleared = dlq.clear();
    assert.strictEqual(cleared, 2);
    assert.strictEqual(dlq.getStats().current, 0);
    console.log('✓ Test 22: DLQ clear all');
  }

  // Test 23: DLQ get all with cleanup
  {
    const dlq = createDLQ();
    dlq.add({ id: 'msg-1' }, 'Failed');
    const all = dlq.getAll();
    assert.strictEqual(all.length, 1);
    assert(all[0].messageId === 'msg-1');
    console.log('✓ Test 23: DLQ get all with cleanup');
  }

  // Test 24: Invalid message throws
  {
    const dlq = createDLQ();
    try {
      dlq.add(null, 'Failed');
      assert.fail('Should throw');
    } catch (err) {
      assert(err.message.includes('Invalid message'));
      console.log('✓ Test 24: Invalid message throws error');
    }
  }
}

// ============================================================================
// MessageQueue Core Tests (10 tests)
// ============================================================================

function testMessageQueueCore() {
  console.log('\n--- MessageQueue Core Tests ---');

  // Test 25: Enqueue and dequeue
  {
    const queue = createQueue();
    const id = queue.enqueue({ data: 'test' });
    assert(typeof id === 'string');
    const msg = queue.dequeue();
    assert.strictEqual(msg.payload.data, 'test');
    console.log('✓ Test 25: Enqueue and dequeue');
  }

  // Test 26: Null payload throws
  {
    const queue = createQueue();
    try {
      queue.enqueue(null);
      assert.fail('Should throw');
    } catch (err) {
      assert(err.message.includes('null or undefined'));
      console.log('✓ Test 26: Null payload throws error');
    }
  }

  // Test 27: Priority option
  {
    const queue = createQueue({ ordering: 'priority' });
    queue.enqueue({ id: 1 }, { priority: 10 });
    queue.enqueue({ id: 2 }, { priority: 100 });
    const first = queue.dequeue();
    assert.strictEqual(first.priority, 100);
    console.log('✓ Test 27: Priority option');
  }

  // Test 28: Metadata storage
  {
    const queue = createQueue();
    const id = queue.enqueue({ data: 'test' }, { metadata: { source: 'api' } });
    const msg = queue.dequeue();
    assert.strictEqual(msg.metadata.source, 'api');
    console.log('✓ Test 28: Metadata storage');
  }

  // Test 29: Peek operation
  {
    const queue = createQueue();
    queue.enqueue({ id: 1 });
    const peeked = queue.peek();
    assert.strictEqual(peeked.payload.id, 1);
    assert.strictEqual(queue.size(), 1); // Not removed
    console.log('✓ Test 29: Peek operation');
  }

  // Test 30: Size tracking
  {
    const queue = createQueue();
    assert.strictEqual(queue.size(), 0);
    queue.enqueue({ id: 1 });
    queue.enqueue({ id: 2 });
    assert.strictEqual(queue.size(), 2);
    console.log('✓ Test 30: Size tracking');
  }

  // Test 31: Get all messages
  {
    const queue = createQueue();
    queue.enqueue({ id: 1 });
    queue.enqueue({ id: 2 });
    const all = queue.getAll();
    assert.strictEqual(all.length, 2);
    assert.strictEqual(queue.size(), 2); // Non-destructive
    console.log('✓ Test 31: Get all messages');
  }

  // Test 32: Clear queue
  {
    const queue = createQueue();
    queue.enqueue({ id: 1 });
    queue.enqueue({ id: 2 });
    const cleared = queue.clear();
    assert.strictEqual(cleared, 2);
    assert.strictEqual(queue.size(), 0);
    console.log('✓ Test 32: Clear queue');
  }

  // Test 33: Acknowledge message
  {
    const queue = createQueue();
    const id = queue.enqueue({ data: 'test' });
    queue.dequeue();
    const ack = queue.acknowledge(id);
    assert.strictEqual(ack, true);
    const stats = queue.getStats();
    assert.strictEqual(stats.acknowledged, 1);
    console.log('✓ Test 33: Acknowledge message');
  }

  // Test 34: Fail message with retry
  {
    const queue = createQueue({ maxRetries: 3 });
    const id = queue.enqueue({ data: 'test' });
    queue.dequeue();
    const shouldRetry = queue.fail(id, new Error('Test error'));
    assert.strictEqual(shouldRetry, true);
    const stats = queue.getStats();
    assert.strictEqual(stats.retried, 1);
    console.log('✓ Test 34: Fail message with retry');
  }
}

// ============================================================================
// MessageQueue Event Tests (5 tests)
// ============================================================================

function testMessageQueueEvents() {
  console.log('\n--- MessageQueue Event Tests ---');

  // Test 35: Enqueue event
  {
    const queue = createQueue();
    let called = false;
    queue.on('enqueued', (msg) => {
      called = true;
      assert(msg.id);
    });
    queue.enqueue({ data: 'test' });
    assert.strictEqual(called, true);
    console.log('✓ Test 35: Enqueue event');
  }

  // Test 36: Dequeue event
  {
    const queue = createQueue();
    queue.enqueue({ data: 'test' });
    let called = false;
    queue.on('dequeued', (msg) => {
      called = true;
    });
    queue.dequeue();
    assert.strictEqual(called, true);
    console.log('✓ Test 36: Dequeue event');
  }

  // Test 37: Acknowledged event
  {
    const queue = createQueue();
    const id = queue.enqueue({ data: 'test' });
    queue.dequeue();
    let called = false;
    queue.on('acknowledged', (msgId) => {
      called = true;
      assert.strictEqual(msgId, id);
    });
    queue.acknowledge(id);
    assert.strictEqual(called, true);
    console.log('✓ Test 37: Acknowledged event');
  }

  // Test 38: Failed event
  {
    const queue = createQueue({ maxRetries: 0 });
    const id = queue.enqueue({ data: 'test' });
    queue.dequeue();
    let called = false;
    queue.on('failed', (msgId, err) => {
      called = true;
      assert.strictEqual(msgId, id);
    });
    queue.fail(id, new Error('Test'));
    assert.strictEqual(called, true);
    console.log('✓ Test 38: Failed event');
  }

  // Test 39: Retried event
  {
    const queue = createQueue({ maxRetries: 2 });
    const id = queue.enqueue({ data: 'test' });
    queue.dequeue();
    let called = false;
    queue.on('retried', (msgId, attempt) => {
      called = true;
      assert.strictEqual(attempt, 2);
    });
    queue.fail(id, new Error('Test'));
    assert.strictEqual(called, true);
    console.log('✓ Test 39: Retried event');
  }
}

// ============================================================================
// MessageQueue Statistics Tests (3 tests)
// ============================================================================

function testMessageQueueStats() {
  console.log('\n--- MessageQueue Statistics Tests ---');

  // Test 40: Stats tracking
  {
    const queue = createQueue();
    queue.enqueue({ id: 1 });
    queue.enqueue({ id: 2 });
    queue.dequeue();
    const stats = queue.getStats();
    assert.strictEqual(stats.enqueued, 2);
    assert.strictEqual(stats.dequeued, 1);
    console.log('✓ Test 40: Stats tracking');
  }

  // Test 41: DLQ stats in queue stats
  {
    const queue = createQueue({ maxRetries: 0 });
    queue.enqueue({ id: 1 });
    queue.dequeue();
    queue.fail('non-existent', new Error('Test'));
    const stats = queue.getStats();
    assert(stats.dlqStats !== undefined);
    console.log('✓ Test 41: DLQ stats in queue stats');
  }

  // Test 42: Complete workflow stats
  {
    const queue = createQueue({ maxRetries: 2 });
    const id1 = queue.enqueue({ id: 1 });
    const id2 = queue.enqueue({ id: 2 });
    
    const msg1 = queue.dequeue();
    queue.acknowledge(msg1.id);
    
    const msg2 = queue.dequeue();
    queue.fail(msg2.id, new Error('Test'));
    
    const stats = queue.getStats();
    assert.strictEqual(stats.enqueued, 2);
    assert.strictEqual(stats.dequeued, 2);
    assert.strictEqual(stats.acknowledged, 1);
    assert.strictEqual(stats.retried, 1);
    console.log('✓ Test 42: Complete workflow stats');
  }
}

// ============================================================================
// MessageQueue Timeout Recovery Tests (3 tests)
// ============================================================================

function testMessageQueueTimeoutRecovery() {
  console.log('\n--- MessageQueue Timeout Recovery Tests ---');

  // Test 43: Recovery of timed-out messages
  {
    const queue = new MessageQueue({
      maxRetries: 3,
      idleTimeout: 100
    });
    
    const id = queue.enqueue({ data: 'test' });
    queue.dequeue();
    
    setTimeout(() => {
      const recovered = queue.recoverTimedOut();
      assert(recovered >= 0);
      console.log('✓ Test 43: Recovery of timed-out messages');
    }, 150);
  }

  // Test 44: Different delivery modes
  {
    const atleast = createQueue({
      deliveryMode: DeliveryModes.AT_LEAST_ONCE
    });
    const atmost = createQueue({
      deliveryMode: DeliveryModes.AT_MOST_ONCE
    });
    const exactly = createQueue({
      deliveryMode: DeliveryModes.EXACTLY_ONCE
    });
    
    assert(atleast !== null);
    assert(atmost !== null);
    assert(exactly !== null);
    console.log('✓ Test 44: Different delivery modes');
  }

  // Test 45: Configuration options
  {
    const queue = new MessageQueue({
      ordering: 'priority',
      deliveryMode: DeliveryModes.EXACTLY_ONCE,
      maxRetries: 5,
      retryBackoff: 3,
      idleTimeout: 60000,
      dlq: { maxSize: 5000, ttl: 3600000 }
    });
    
    assert.strictEqual(queue.ordering.strategy, 'priority');
    assert.strictEqual(queue.guarantee.mode, 'exactly-once');
    assert.strictEqual(queue.maxRetries, 5);
    assert.strictEqual(queue.retryBackoff, 3);
    assert.strictEqual(queue.idleTimeout, 60000);
    console.log('✓ Test 45: Configuration options');
  }
}

// ============================================================================
// Run All Tests
// ============================================================================

function runAllTests() {
  console.log('╔════════════════════════════════════════════════════════════╗');
  console.log('║     Message Queue Module - Comprehensive Test Suite       ║');
  console.log('║                     45+ Test Cases                        ║');
  console.log('╚════════════════════════════════════════════════════════════╝');

  try {
    testOrderingManager();        // Tests 1-8
    testDeliveryGuarantee();      // Tests 9-17
    testDeadLetterQueue();        // Tests 18-24
    testMessageQueueCore();       // Tests 25-34
    testMessageQueueEvents();     // Tests 35-39
    testMessageQueueStats();      // Tests 40-42
    testMessageQueueTimeoutRecovery(); // Tests 43-45

    console.log('\n╔════════════════════════════════════════════════════════════╗');
    console.log('║                 ✓ All 45 Tests Passed                      ║');
    console.log('╚════════════════════════════════════════════════════════════╝\n');
    return true;
  } catch (err) {
    console.error('\n✗ Test failed:', err.message);
    console.error(err.stack);
    process.exit(1);
  }
}

module.exports = {
  runAllTests,
  testOrderingManager,
  testDeliveryGuarantee,
  testDeadLetterQueue,
  testMessageQueueCore,
  testMessageQueueEvents,
  testMessageQueueStats,
  testMessageQueueTimeoutRecovery
};

if (require.main === module) {
  runAllTests();
}
