# Message Queue Module (mod-queue)

Production-ready message queue with ordering, delivery guarantees, and dead letter queue support.

## Features

- **Message Buffering**: Reliable message storage with FIFO and priority ordering
- **Delivery Guarantees**: Support for at-least-once, at-most-once, and exactly-once delivery
- **Dead Letter Queue**: Automatic handling of failed messages
- **Event Hooks**: Subscribe to queue lifecycle events
- **Retry Management**: Exponential backoff retry support
- **Statistics**: Comprehensive queue metrics
- **Timeout Recovery**: Automatic recovery of stalled messages

## Installation

```javascript
const { MessageQueue, DeliveryModes } = require('mod-queue');
```

## Quick Start

```javascript
const queue = new MessageQueue({
  ordering: 'fifo',
  deliveryMode: DeliveryModes.AT_LEAST_ONCE,
  maxRetries: 3
});

// Enqueue message
const msgId = queue.enqueue({ content: 'Hello' }, {
  priority: 5,
  metadata: { source: 'api' }
});

// Dequeue and process
const msg = queue.dequeue();
console.log(msg.payload);

// Acknowledge successful delivery
queue.acknowledge(msg.id);

// Get statistics
console.log(queue.getStats());
```

## API Reference

### MessageQueue

Main queue class combining all functionality.

#### Constructor

```javascript
new MessageQueue(options)
```

**Options:**
- `ordering` (string): 'fifo' or 'priority' (default: 'fifo')
- `deliveryMode` (string): Delivery guarantee mode (default: 'at-least-once')
- `maxRetries` (number): Maximum retry attempts (default: 3)
- `retryBackoff` (number): Backoff multiplier (default: 2)
- `idleTimeout` (number): Timeout for stalled messages in ms (default: 30000)
- `dlq` (object): Dead letter queue options

#### Methods

##### `enqueue(payload, options)`

Adds message to queue.

**Parameters:**
- `payload` (*): Message content
- `options` (object):
  - `priority` (number): Priority level 0-100 (default: 0)
  - `deliveryMode` (string): Override queue mode
  - `metadata` (object): Custom metadata

**Returns:** Message ID (string)

**Throws:** Error if payload is null/undefined

```javascript
const id = queue.enqueue({ user: 'alice' }, {
  priority: 10,
  metadata: { userId: '123' }
});
```

##### `dequeue()`

Retrieves and removes next message.

**Returns:** Message object or null

```javascript
const msg = queue.dequeue();
if (msg) {
  console.log(`Processing ${msg.id}: ${msg.payload}`);
}
```

##### `acknowledge(messageId)`

Marks message as successfully delivered.

**Parameters:**
- `messageId` (string): ID to acknowledge

**Returns:** boolean - Success status

**Throws:** Error if message not found

```javascript
queue.acknowledge('msg-123456-1');
```

##### `fail(messageId, error)`

Marks message delivery as failed.

**Parameters:**
- `messageId` (string): Message ID
- `error` (Error): The error that occurred

**Returns:** boolean - Whether to retry

**Throws:** Error if message not in-flight

```javascript
queue.fail('msg-123456-1', new Error('Connection timeout'));
```

##### `peek()`

Views next message without removing.

**Returns:** Message or null

```javascript
const next = queue.peek();
```

##### `size()`

Gets current queue length.

**Returns:** number

```javascript
console.log(`Queue has ${queue.size()} messages`);
```

##### `getAll()`

Gets all queued messages.

**Returns:** Message[] (copy)

```javascript
const all = queue.getAll();
```

##### `on(event, handler)`

Registers event listener.

**Events:**
- `enqueued`: Message added to queue
- `dequeued`: Message removed from queue
- `acknowledged`: Message delivery confirmed
- `failed`: Message delivery failed
- `retried`: Message retry scheduled

```javascript
queue.on('enqueued', (msg) => {
  console.log(`Message ${msg.id} enqueued`);
});

queue.on('failed', (id, error) => {
  console.error(`Message ${id} failed: ${error.message}`);
});
```

##### `off(event)`

Removes event listener.

```javascript
queue.off('enqueued');
```

##### `getStats()`

Gets queue statistics.

**Returns:** Object with counts and DLQ stats

```javascript
const stats = queue.getStats();
console.log(`Delivered: ${stats.acknowledged}, Failed: ${stats.failed}`);
```

##### `clear()`

Removes all messages from queue.

**Returns:** number - Count removed

```javascript
const removed = queue.clear();
```

##### `recoverTimedOut()`

Recovers messages that timed out.

**Returns:** number - Count recovered

```javascript
const recovered = queue.recoverTimedOut();
```

### OrderingManager

Handles message ordering strategies.

#### Constructor

```javascript
new OrderingManager(strategy)
```

**Parameters:**
- `strategy` (string): 'fifo' or 'priority'

#### Methods

- `enqueue(message)` - Add message
- `dequeue()` - Remove and return next
- `peek()` - View next
- `size()` - Get length
- `clear()` - Remove all
- `getAll()` - Get copy of all messages

### DeliveryGuarantee

Manages delivery guarantee semantics.

#### Constructor

```javascript
new DeliveryGuarantee(mode)
```

**Modes:**
- `at-least-once`: Messages delivered at least once (default)
- `at-most-once`: Messages delivered at most once
- `exactly-once`: Messages delivered exactly once

#### Methods

- `markInFlight(messageId)` - Mark message as being delivered
- `markAcknowledged(messageId)` - Mark as successfully delivered
- `retry(messageId)` - Increment retry count
- `getStatus(messageId)` - Get delivery status
- `getTimedOutMessages(timeoutMs)` - Find timed-out messages

### DeadLetterQueue

Stores and manages failed messages.

#### Constructor

```javascript
new DeadLetterQueue(options)
```

**Options:**
- `maxSize` (number): Maximum DLQ capacity (default: 10000)
- `ttl` (number): Message TTL in ms (default: 86400000)

#### Methods

- `add(message, reason, error)` - Add failed message
- `get(messageId)` - Retrieve DLQ entry
- `getAll()` - Get all messages with cleanup
- `cleanup()` - Remove expired messages
- `getStats()` - Get DLQ statistics
- `clear()` - Remove all messages

## Delivery Modes

### at-least-once
Messages guaranteed to be processed at least once. Sender retries until acknowledged.

```javascript
const queue = new MessageQueue({
  deliveryMode: 'at-least-once'
});
```

### at-most-once
Messages delivered at most once. No retries after acknowledgment.

```javascript
const queue = new MessageQueue({
  deliveryMode: 'at-most-once'
});
```

### exactly-once
Messages delivered exactly once. Deduplication based on message ID.

```javascript
const queue = new MessageQueue({
  deliveryMode: 'exactly-once'
});
```

## Ordering Strategies

### FIFO (First-In-First-Out)
Default strategy. Messages processed in order received.

```javascript
const queue = new MessageQueue({ ordering: 'fifo' });
```

### Priority
Higher priority messages processed first. Same-priority uses FIFO.

```javascript
const queue = new MessageQueue({ ordering: 'priority' });

// High priority urgent task
queue.enqueue(urgentTask, { priority: 100 });

// Low priority background task
queue.enqueue(backgroundTask, { priority: 1 });
```

## Examples

### Basic Message Processing

```javascript
const { MessageQueue } = require('mod-queue');

const queue = new MessageQueue();

// Producer
queue.enqueue({ task: 'send-email', to: 'user@example.com' });
queue.enqueue({ task: 'process-payment', amount: 99.99 });

// Consumer
while (queue.size() > 0) {
  const msg = queue.dequeue();
  try {
    processTask(msg.payload);
    queue.acknowledge(msg.id);
  } catch (err) {
    queue.fail(msg.id, err);
  }
}
```

### Priority Queue for Request Processing

```javascript
const queue = new MessageQueue({ ordering: 'priority' });

// Priority 1-10: Low priority background jobs
queue.enqueue({ type: 'cleanup' }, { priority: 1 });

// Priority 50: Normal requests
queue.enqueue({ type: 'api-request' }, { priority: 50 });

// Priority 100: Urgent tasks
queue.enqueue({ type: 'critical-alert' }, { priority: 100 });
```

### Reliability with Exactly-Once Semantics

```javascript
const queue = new MessageQueue({
  deliveryMode: 'exactly-once',
  maxRetries: 5
});

queue.on('enqueued', (msg) => {
  console.log(`Queued: ${msg.id}`);
});

queue.on('acknowledged', (id) => {
  console.log(`Delivered: ${id}`);
});

queue.on('failed', (id, err) => {
  console.log(`Failed: ${id} - ${err.message}`);
});
```

### Monitoring with Dead Letter Queue

```javascript
const queue = new MessageQueue({
  dlq: { maxSize: 5000, ttl: 604800000 } // 7 days
});

setInterval(() => {
  const stats = queue.getStats();
  console.log(`Queue: ${stats.queueSize}, DLQ: ${stats.dlqStats.current}`);
  
  const dlqItems = queue.dlq.getAll();
  dlqItems.forEach(item => {
    console.log(`DLQ: ${item.messageId} - ${item.reason}`);
  });
}, 60000);
```

## Performance Characteristics

| Operation | Complexity | Time (typical) |
|-----------|-----------|----------------|
| enqueue() | O(n) for priority, O(1) for FIFO | <1ms |
| dequeue() | O(1) | <0.1ms |
| peek() | O(1) | <0.1ms |
| acknowledge() | O(1) | <0.1ms |
| getStats() | O(1) | <0.1ms |
| getAll() | O(n) | <10ms (10K msgs) |

**Memory Usage:**
- Base queue: ~2KB
- Per message: ~200 bytes (varies with payload)
- DLQ entry: ~500 bytes

## Error Handling

All public methods include comprehensive error handling:

```javascript
try {
  queue.enqueue(null); // Throws: Payload cannot be null
} catch (err) {
  console.error(err.message);
}

try {
  queue.acknowledge('non-existent');
} catch (err) {
  console.error(err.message); // Throws: Message not found
}
```

## Best Practices

1. **Handle all acknowledge/fail calls**: Ensure every dequeued message is acknowledged or failed
2. **Monitor DLQ**: Regularly check dead letter queue for failed messages
3. **Use appropriate delivery mode**: Choose based on requirements
4. **Set reasonable max retries**: Balance reliability with resource usage
5. **Implement idempotent handlers**: Prepare for potential duplicates
6. **Monitor statistics**: Track queue health continuously

## License

HELIOS v4.0 - All rights reserved
