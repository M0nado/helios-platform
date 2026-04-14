/**
 * HELIOS v4.0 Event Bus - Usage Examples
 */

const { EventBus } = require('./index');

// ============================================================================
// Example 1: Basic Pub/Sub
// ============================================================================
console.log('=== Example 1: Basic Pub/Sub ===\n');

const bus = new EventBus();

// Subscribe to events
bus.subscribe('user.created', (event) => {
  console.log(`User created: ${event.data.name} (${event.data.email})`);
});

bus.subscribe('user.created', (event) => {
  console.log(`  → Sending welcome email to ${event.data.email}`);
});

// Publish an event
(async () => {
  await bus.publish('user.created', {
    userId: 1001,
    name: 'Alice Johnson',
    email: 'alice@example.com'
  });

  await bus.drain();
})();

// ============================================================================
// Example 2: Wildcard Subscriptions
// ============================================================================
console.log('\n=== Example 2: Wildcard Subscriptions ===\n');

const orderBus = new EventBus();

// Subscribe to all order events
orderBus.subscribeWildcard('order.*', (event) => {
  console.log(`Order event: ${event.topic} - Order ${event.data.orderId}`);
});

// Subscribe to specific order lifecycle events
orderBus.subscribeWildcard('order.status.*', (event) => {
  console.log(`  → Status change: ${event.topic}`);
});

(async () => {
  await orderBus.publish('order.created', { orderId: 'ORD-001', total: 99.99 });
  await orderBus.publish('order.status.confirmed', { orderId: 'ORD-001' });
  await orderBus.publish('order.status.shipped', { orderId: 'ORD-001' });
  await orderBus.publish('order.status.delivered', { orderId: 'ORD-001' });
  await orderBus.drain();
})();

// ============================================================================
// Example 3: Filter-Based Subscriptions
// ============================================================================
console.log('\n=== Example 3: Filter-Based Subscriptions ===\n');

const paymentBus = new EventBus();

// Only react to high-value payments
paymentBus.subscribeWithFilter(
  (event) => event.data.amount > 1000,
  (event) => {
    console.log(`⚠️  High-value payment detected: $${event.data.amount}`);
    console.log(`   → Triggering fraud checks for payment ${event.data.paymentId}`);
  }
);

// React to all payments from certain region
paymentBus.subscribeWithFilter(
  (event) => event.data.region === 'EU',
  (event) => {
    console.log(`🌍 EU Payment: ${event.data.paymentId} - $${event.data.amount}`);
  }
);

(async () => {
  await paymentBus.publish('payment.processed', {
    paymentId: 'PAY-001',
    amount: 500,
    region: 'US'
  });
  await paymentBus.publish('payment.processed', {
    paymentId: 'PAY-002',
    amount: 1500,
    region: 'EU'
  });
  await paymentBus.drain();
})();

// ============================================================================
// Example 4: Event Ordering and Sequential Processing
// ============================================================================
console.log('\n=== Example 4: Event Ordering ===\n');

const inventoryBus = new EventBus();
let inventory = 100;

inventoryBus.subscribe('inventory.purchase', (event) => {
  inventory -= event.data.quantity;
  console.log(`Purchase: -${event.data.quantity}, Remaining: ${inventory}`);
});

inventoryBus.subscribe('inventory.restock', (event) => {
  inventory += event.data.quantity;
  console.log(`Restock: +${event.data.quantity}, Remaining: ${inventory}`);
});

(async () => {
  await inventoryBus.publish('inventory.purchase', { orderId: 'ORD-001', quantity: 10 });
  await inventoryBus.publish('inventory.purchase', { orderId: 'ORD-002', quantity: 5 });
  await inventoryBus.publish('inventory.restock', { batchId: 'BATCH-001', quantity: 50 });
  await inventoryBus.drain();
})();

// ============================================================================
// Example 5: Event Persistence and Replay
// ============================================================================
console.log('\n=== Example 5: Event Persistence and Replay ===\n');

const persistentBus = new EventBus({ persistence: true });

// Subscribe to events
persistentBus.subscribe('action', (event) => {
  console.log(`Action performed: ${event.data.action}`);
});

(async () => {
  // Publish some events
  await persistentBus.publish('action', { action: 'login' });
  await persistentBus.publish('action', { action: 'upload_file' });
  await persistentBus.publish('action', { action: 'logout' });
  await persistentBus.drain();

  // Later, replay the events
  console.log('\nReplaying events:');
  const replayed = await persistentBus.replay(
    { topic: 'action' },
    (event) => {
      console.log(`  Replayed: ${event.data.action}`);
      return Promise.resolve();
    }
  );

  console.log(`Total events replayed: ${replayed}`);
  console.log(`Events exported: ${persistentBus.exportEvents().length}`);
})();

// ============================================================================
// Example 6: Error Handling in Subscribers
// ============================================================================
console.log('\n=== Example 6: Error Handling ===\n');

const robustBus = new EventBus();

robustBus.subscribe('process', (event) => {
  console.log(`Processing item ${event.data.id}...`);
});

robustBus.subscribe('process', (event) => {
  if (event.data.id === 2) {
    throw new Error('Processing failed for item 2');
  }
  console.log(`  ✓ Item ${event.data.id} processed successfully`);
});

robustBus.subscribe('process', (event) => {
  console.log(`  → Logged to audit trail`);
});

(async () => {
  // Even if subscriber 2 fails, subscribers 1 and 3 still execute
  for (let i = 1; i <= 3; i++) {
    await robustBus.publish('process', { id: i });
  }
  await robustBus.drain();
})();

// ============================================================================
// Example 7: Complex Event Workflows
// ============================================================================
console.log('\n=== Example 7: Event Workflows ===\n');

const workflowBus = new EventBus();

// User signup workflow
let user = null;

workflowBus.subscribe('user.signup', async (event) => {
  user = { id: event.data.userId, email: event.data.email };
  console.log(`1. User registered: ${user.email}`);
  // Trigger next event
  await workflowBus.publish('user.verified', { userId: user.id });
});

workflowBus.subscribe('user.verified', async (event) => {
  console.log(`2. Email verified for user ${event.data.userId}`);
  await workflowBus.publish('user.profile.created', { userId: event.data.userId });
});

workflowBus.subscribe('user.profile.created', (event) => {
  console.log(`3. Profile created for user ${event.data.userId}`);
  console.log(`4. Workflow complete!`);
});

(async () => {
  await workflowBus.publish('user.signup', {
    userId: 1,
    email: 'user@example.com'
  });
  await workflowBus.drain();
})();

// ============================================================================
// Example 8: Statistics and Monitoring
// ============================================================================
console.log('\n=== Example 8: Statistics ===\n');

const statsBus = new EventBus();

// Subscribe to various events
statsBus.subscribe('test', () => {});
statsBus.subscribe('test', () => {});
statsBus.subscribeWildcard('test.*', () => {});

console.log(`Subscribers for 'test': ${statsBus.getSubscriberCount('test')}`);

(async () => {
  await statsBus.publish('test', { data: 1 });
  await statsBus.publish('test', { data: 2 });
  await statsBus.publish('test.sub', { data: 3 });

  const stats = statsBus.getStats();
  console.log('\nEvent Bus Statistics:');
  console.log(`  Published: ${stats.published}`);
  console.log(`  Delivered: ${stats.delivered}`);
  console.log(`  Failed: ${stats.failed}`);
  console.log(`  Queue Size: ${stats.queueSize}`);
  console.log(`  Persisted Events: ${stats.persistedEvents}`);
})();

// ============================================================================
// Example 9: Real-World: E-Commerce Order Processing
// ============================================================================
console.log('\n=== Example 9: E-Commerce Order Processing ===\n');

const ecommerceBus = new EventBus({ persistence: true });

const orders = {};

// Order service
ecommerceBus.subscribe('order.created', async (event) => {
  orders[event.data.orderId] = {
    id: event.data.orderId,
    status: 'created',
    total: event.data.total,
    items: event.data.items
  };
  console.log(`📦 Order created: ${event.data.orderId}`);
  await ecommerceBus.publish('payment.requested', {
    orderId: event.data.orderId,
    amount: event.data.total
  });
});

// Payment service
ecommerceBus.subscribe('payment.requested', async (event) => {
  console.log(`💳 Processing payment for order ${event.data.orderId}...`);
  setTimeout(async () => {
    await ecommerceBus.publish('payment.completed', {
      orderId: event.data.orderId,
      amount: event.data.amount
    });
  }, 100);
});

// Fulfillment service
ecommerceBus.subscribe('payment.completed', async (event) => {
  orders[event.data.orderId].status = 'paid';
  console.log(`✅ Payment confirmed for order ${event.data.orderId}`);
  await ecommerceBus.publish('order.ready', {
    orderId: event.data.orderId
  });
});

// Shipping service
ecommerceBus.subscribe('order.ready', async (event) => {
  console.log(`🚚 Shipping order ${event.data.orderId}`);
  await ecommerceBus.publish('order.shipped', {
    orderId: event.data.orderId,
    trackingNumber: `TRACK-${Date.now()}`
  });
});

// Notification service
ecommerceBus.subscribeWildcard('order.*', (event) => {
  const orderId = event.data.orderId;
  const status = event.topic.split('.')[1];
  console.log(`📧 Sending email: Order ${orderId} is ${status}`);
});

(async () => {
  await ecommerceBus.publish('order.created', {
    orderId: 'ORD-2024-001',
    total: 199.99,
    items: ['ITEM-001', 'ITEM-002']
  });
  await ecommerceBus.drain();
})();

// ============================================================================
// Example 10: Delayed and Scheduled Events
// ============================================================================
console.log('\n=== Example 10: Delayed Events ===\n');

const delayedBus = new EventBus();

delayedBus.subscribe('reminder', (event) => {
  console.log(`🔔 Reminder: ${event.data.message}`);
});

(async () => {
  console.log('Setting up delayed event (500ms delay)...');
  const start = Date.now();
  await delayedBus.publish(
    'reminder',
    { message: 'It\'s time to update the cache' },
    { delay: 500 }
  );
  const elapsed = Date.now() - start;
  console.log(`Event triggered after ${elapsed}ms`);
})();

// ============================================================================
// Example 11: Unsubscribing from Events
// ============================================================================
console.log('\n=== Example 11: Unsubscribing ===\n');

const dynamicBus = new EventBus();

const tempHandler = (event) => {
  console.log(`Temporary handler: ${event.data.message}`);
};

// Subscribe
const unsubscribe = dynamicBus.subscribe('temp', tempHandler);

console.log(`Subscribers: ${dynamicBus.getSubscriberCount('temp')}`);

// Unsubscribe
unsubscribe();

console.log(`After unsubscribe: ${dynamicBus.getSubscriberCount('temp')}`);

// ============================================================================
// Example 12: Queue Management
// ============================================================================
console.log('\n=== Example 12: Queue Management ===\n');

const queueBus = new EventBus({ maxQueueSize: 10 });

let processed = 0;

queueBus.subscribe('item', async (event) => {
  processed++;
  // Simulate processing
  await new Promise(r => setTimeout(r, 50));
});

(async () => {
  // Publish multiple events
  for (let i = 0; i < 5; i++) {
    await queueBus.publish('item', { id: i });
  }

  console.log(`Queue size before drain: ${queueBus.getStats().queueSize}`);

  // Wait for all events to be processed
  await queueBus.drain();

  console.log(`Processed ${processed} items`);
  console.log('Queue is now empty');
})();
