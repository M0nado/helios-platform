# HELIOS v4.0 Modules - Quick Start Guide

## Module Locations

```
C:\helios-v4\parallel\modules\
├── mod-queue\           (Message Queue - 60.4 KB)
└── mod-webhook\         (Webhook Manager - 75.8 KB)
```

---

## mod-queue Quick Start

### Basic Usage

```javascript
const { MessageQueue, DeliveryModes } = require('./mod-queue');

// Create queue
const queue = new MessageQueue({
  ordering: 'fifo',              // or 'priority'
  deliveryMode: DeliveryModes.AT_LEAST_ONCE,
  maxRetries: 3
});

// Enqueue message
const msgId = queue.enqueue(
  { task: 'send-email', to: 'user@example.com' },
  { priority: 50, metadata: { source: 'api' } }
);

// Dequeue message
const msg = queue.dequeue();
console.log(`Processing: ${msg.payload.task}`);

// Acknowledge success
queue.acknowledge(msg.id);

// Or mark as failed
queue.fail(msg.id, new Error('Network timeout'));

// Get statistics
const stats = queue.getStats();
console.log(`Processed: ${stats.acknowledged}, Failed: ${stats.failed}`);
```

### Event Handling

```javascript
queue.on('enqueued', (msg) => {
  console.log(`Message enqueued: ${msg.id}`);
});

queue.on('acknowledged', (msgId) => {
  console.log(`Message acknowledged: ${msgId}`);
});

queue.on('failed', (msgId, error) => {
  console.log(`Message failed: ${msgId} - ${error.message}`);
});
```

### Delivery Modes

```javascript
// At-least-once: Messages delivered at least once (allows duplicates)
new MessageQueue({ deliveryMode: 'at-least-once' });

// At-most-once: Messages delivered at most once (may lose messages)
new MessageQueue({ deliveryMode: 'at-most-once' });

// Exactly-once: Messages delivered exactly once (prevents duplicates)
new MessageQueue({ deliveryMode: 'exactly-once' });
```

### Priority Queue

```javascript
const queue = new MessageQueue({ ordering: 'priority' });

// Lower priority (background tasks)
queue.enqueue({ task: 'cleanup' }, { priority: 1 });

// Normal priority
queue.enqueue({ task: 'process' }, { priority: 50 });

// High priority (urgent)
queue.enqueue({ task: 'alert' }, { priority: 100 });

// Messages dequeued in priority order (highest first)
const msg = queue.dequeue(); // Gets alert
```

### Dead Letter Queue

```javascript
const queue = new MessageQueue({
  dlq: { maxSize: 5000, ttl: 86400000 } // 24 hours
});

// Failed messages automatically moved to DLQ
queue.fail(msgId, error);

// Inspect DLQ
const dlqItems = queue.dlq.getAll();
dlqItems.forEach(item => {
  console.log(`DLQ: ${item.messageId} - ${item.reason}`);
});
```

---

## mod-webhook Quick Start

### Basic Usage

```javascript
const { WebhookManager } = require('./mod-webhook');

// Create manager
const manager = new WebhookManager();

// Register webhook
const webhook = manager.register(
  'https://api.example.com/webhooks',
  ['user.created', 'user.updated'],
  {
    metadata: { owner: 'alice@example.com' }
  }
);

console.log(`Webhook registered: ${webhook.id}`);
console.log(`Secret: ${webhook.secret}`);

// Trigger webhook
await manager.trigger(webhook.id, 'user.created', {
  userId: 'user_123',
  email: 'newuser@example.com',
  createdAt: new Date().toISOString()
});

// Get statistics
const stats = manager.getStats();
console.log(`Delivered: ${stats.delivered}, Failed: ${stats.failed}`);
```

### Signature Verification

```javascript
const { SignatureVerifier } = require('./mod-webhook');

const verifier = new SignatureVerifier('sha256');

// Generate signature
const payload = { event: 'user.created', id: 123 };
const secret = webhook.secret;
const signature = verifier.sign(payload, secret);

// Verify signature
const isValid = verifier.verify(payload, signature, secret);
console.log(`Valid: ${isValid}`);

// Generate secure secret
const newSecret = verifier.generateSecret(32);
```

### Event Broadcasting

```javascript
// Find webhooks for event
const webhooks = manager.findForEvent('order.created');

// Broadcast to all
const results = await Promise.allSettled(
  webhooks.map(w => manager.trigger(w.id, 'order.created', orderData))
);

results.forEach((result, i) => {
  if (result.status === 'fulfilled') {
    console.log(`✓ Delivered to webhook ${i}`);
  } else {
    console.log(`✗ Failed to webhook ${i}: ${result.reason.message}`);
  }
});
```

### Webhook Management

```javascript
// List all webhooks
const all = manager.list();

// List active only
const active = manager.list({ active: true });

// Get specific webhook
const webhook = manager.get(webhookId);

// Update webhook
manager.update(webhookId, {
  url: 'https://new-api.example.com/webhooks',
  events: ['order.created', 'order.completed'],
  active: true
});

// Delete webhook
manager.delete(webhookId);
```

### Event Handling

```javascript
manager.on('registered', (webhook) => {
  console.log(`Webhook registered: ${webhook.id}`);
});

manager.on('delivered', (webhookId, event) => {
  console.log(`Event ${event} delivered to ${webhookId}`);
});

manager.on('failed', (webhookId, event, error) => {
  console.log(`Event ${event} failed: ${error.message}`);
});

manager.on('retried', (webhookId, attempt) => {
  console.log(`Retry attempt ${attempt} for ${webhookId}`);
});
```

### Rate Limiting

```javascript
const manager = new WebhookManager({
  rateLimit: {
    requestsPerSecond: 50,  // Per webhook
    maxBurst: 5             // Global burst
  }
});

// Check before delivery
if (manager.rateLimiter.canDeliver(webhookId)) {
  await manager.trigger(webhookId, event, data);
}

// Check status
const status = manager.rateLimiter.getStatus(webhookId);
console.log(`Tokens: ${status.webhook.tokens}/${status.webhook.limit}`);
```

---

## Files Overview

### mod-queue Files

| File | Purpose |
|------|---------|
| `implementation.js` | Core implementation (14.5 KB) |
| `index.js` | Public API exports |
| `tests.js` | 45+ test cases |
| `examples.js` | 7 usage examples |
| `README.md` | Complete documentation |

### mod-webhook Files

| File | Purpose |
|------|---------|
| `implementation.js` | Core implementation (18.5 KB) |
| `index.js` | Public API exports |
| `tests.js` | 50+ test cases |
| `examples.js` | 8 usage examples |
| `README.md` | Complete documentation |

---

## API Reference Quick Links

### mod-queue Classes
- **MessageQueue** - Main queue orchestrator
  - enqueue(), dequeue(), acknowledge(), fail(), peek(), size()
  - getAll(), on(), off(), getStats(), clear(), recoverTimedOut()

- **OrderingManager** - Message ordering strategies
  - enqueue(), dequeue(), peek(), size(), clear(), getAll()

- **DeliveryGuarantee** - Delivery guarantee semantics
  - markInFlight(), markAcknowledged(), retry(), getStatus(), getTimedOutMessages()

- **DeadLetterQueue** - Failed message storage
  - add(), get(), getAll(), cleanup(), getStats(), clear()

### mod-webhook Classes
- **WebhookManager** - Webhook orchestration
  - register(), get(), list(), findForEvent(), update(), delete()
  - trigger(), verifySignature(), on(), off(), getStats()

- **SignatureVerifier** - HMAC signature operations
  - sign(), verify(), generateSecret()

- **RetryManager** - Exponential backoff
  - getNextDelay(), recordAttempt(), shouldRetry(), scheduleRetry()

- **WebhookRateLimiter** - Rate limiting
  - canDeliver(), getStatus(), reset(), resetAll()

---

## Testing

### Run Tests

```bash
# mod-queue tests
node C:\helios-v4\parallel\modules\mod-queue\tests.js

# mod-webhook tests
node C:\helios-v4\parallel\modules\mod-webhook\tests.js
```

### Run Examples

```bash
# mod-queue examples
node C:\helios-v4\parallel\modules\mod-queue\examples.js

# mod-webhook examples
node C:\helios-v4\parallel\modules\mod-webhook\examples.js
```

---

## Configuration Examples

### mod-queue Configuration

```javascript
new MessageQueue({
  ordering: 'priority',           // 'fifo' or 'priority'
  deliveryMode: 'exactly-once',   // delivery guarantee mode
  maxRetries: 5,                  // max retry attempts
  retryBackoff: 2,                // exponential backoff multiplier
  idleTimeout: 30000,             // stalled message timeout (ms)
  dlq: {
    maxSize: 10000,               // max DLQ entries
    ttl: 86400000                 // message TTL (24h)
  }
});
```

### mod-webhook Configuration

```javascript
new WebhookManager({
  algorithm: 'sha256',            // hash algorithm
  retry: {
    maxRetries: 5,
    initialDelay: 1000,           // 1 second
    maxDelay: 300000,             // 5 minutes
    backoffMultiplier: 2
  },
  rateLimit: {
    requestsPerSecond: 100,
    maxBurst: 10
  }
});
```

---

## Common Patterns

### Pattern 1: Email Queue

```javascript
const queue = new MessageQueue({ ordering: 'priority' });

// High priority: Password reset
queue.enqueue(email, { priority: 100 });

// Medium priority: Notifications
queue.enqueue(email, { priority: 50 });

// Low priority: Marketing
queue.enqueue(email, { priority: 10 });
```

### Pattern 2: Event Broadcasting

```javascript
async function publishEvent(eventName, data) {
  const webhooks = manager.findForEvent(eventName);
  
  return Promise.allSettled(
    webhooks.map(w => manager.trigger(w.id, eventName, data))
  );
}
```

### Pattern 3: Reliable Processing

```javascript
while (queue.size() > 0) {
  const msg = queue.dequeue();
  
  try {
    await processMessage(msg.payload);
    queue.acknowledge(msg.id);
  } catch (error) {
    if (!queue.fail(msg.id, error)) {
      // Max retries exhausted, moved to DLQ
      console.error(`Message moved to DLQ: ${msg.id}`);
    }
  }
}
```

---

## Best Practices

### mod-queue
1. **Always acknowledge or fail** - Every dequeued message must be acknowledged or failed
2. **Monitor DLQ** - Regularly check dead letter queue
3. **Choose right delivery mode** - Match your requirements
4. **Handle timeouts** - Call recoverTimedOut() periodically
5. **Implement idempotency** - Prepare for retries

### mod-webhook
1. **Verify signatures** - Always verify incoming webhook signatures
2. **Store secrets securely** - Use secure configuration management
3. **Implement retries** - Use exponential backoff
4. **Rate limit** - Protect downstream services
5. **Log deliveries** - Track all webhook activity

---

## Troubleshooting

### Messages stuck in queue?
- Check `queue.size()` and `queue.getStats()`
- Call `queue.recoverTimedOut()` to recover stalled messages
- Check `queue.dlq.getAll()` for failed messages

### Webhooks not delivering?
- Check webhook is active: `manager.get(webhookId).active`
- Verify signature: `manager.verifySignature(payload, sig, webhookId)`
- Check rate limits: `manager.rateLimiter.getStatus(webhookId)`
- Review events: `manager.findForEvent(eventName)`

---

## Support & Documentation

- **Detailed API Docs**: See `README.md` in each module
- **Full Examples**: See `examples.js` in each module
- **Test Coverage**: See `tests.js` in each module

---

**HELIOS v4.0 Fleet Expansion** | Ready for Production
