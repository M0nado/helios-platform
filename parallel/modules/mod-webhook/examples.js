/**
 * Webhook Manager Module - Comprehensive Examples
 * Demonstrates real-world webhook usage patterns and scenarios
 */

const { WebhookManager, SignatureVerifier } = require('./index');

// ============================================================================
// EXAMPLE 1: Basic Webhook Registration and Triggering
// ============================================================================

async function example1_BasicWebhook() {
  console.log('\n=== EXAMPLE 1: Basic Webhook Registration and Triggering ===\n');
  
  const manager = new WebhookManager();

  // Register webhook
  console.log('Registering webhook...');
  const webhook = manager.register(
    'https://api.example.com/webhooks/events',
    ['user.created', 'user.updated', 'user.deleted'],
    {
      metadata: {
        owner: 'alice@example.com',
        team: 'platform'
      }
    }
  );

  console.log(`✓ Webhook registered:`);
  console.log(`  ID: ${webhook.id}`);
  console.log(`  URL: ${webhook.url}`);
  console.log(`  Events: ${webhook.events.join(', ')}`);
  console.log(`  Secret: ${webhook.secret.substring(0, 16)}...`);

  // Trigger webhook
  console.log('\nTriggering webhook...');
  try {
    await manager.trigger(webhook.id, 'user.created', {
      userId: 'user_123',
      email: 'newuser@example.com',
      name: 'Alice Smith',
      createdAt: new Date().toISOString()
    });
    console.log('✓ Webhook triggered successfully');
  } catch (err) {
    console.log(`⚠ Webhook trigger: ${err.message}`);
  }

  const stats = manager.getStats();
  console.log(`\nManager Stats: Delivered: ${stats.delivered}, Failed: ${stats.failed}`);
}

// ============================================================================
// EXAMPLE 2: Signature Verification
// ============================================================================

function example2_SignatureVerification() {
  console.log('\n=== EXAMPLE 2: Signature Verification ===\n');
  
  const verifier = new SignatureVerifier('sha256');

  // Generate secret
  console.log('Generating webhook secret...');
  const secret = verifier.generateSecret(32);
  console.log(`Secret: ${secret.substring(0, 16)}...`);

  // Create payload
  const payload = {
    event: 'user.created',
    data: {
      userId: 'user_456',
      email: 'bob@example.com'
    },
    timestamp: Date.now()
  };

  console.log('\nSigningpayload...');
  const signature = verifier.sign(payload, secret);
  console.log(`Signature: ${signature.substring(0, 20)}...`);

  // Verify signature
  console.log('\nVerifying signature...');
  const isValid = verifier.verify(payload, signature, secret);
  console.log(`✓ Signature valid: ${isValid}`);

  // Try with wrong signature
  console.log('\nVerifying with wrong signature...');
  const wrongSig = 'invalid_signature';
  try {
    const invalid = verifier.verify(payload, wrongSig, secret);
    console.log(`Invalid signature detected: ${!invalid}`);
  } catch (err) {
    console.log(`✓ Correctly rejected invalid signature`);
  }
}

// ============================================================================
// EXAMPLE 3: Multiple Webhooks and Event Broadcasting
// ============================================================================

async function example3_EventBroadcasting() {
  console.log('\n=== EXAMPLE 3: Multiple Webhooks and Event Broadcasting ===\n');
  
  const manager = new WebhookManager();

  // Register multiple webhooks
  console.log('Registering webhooks...');
  const webhooks = [
    manager.register('https://analytics.example.com/events', ['order.created', 'order.completed']),
    manager.register('https://crm.example.com/webhooks', ['user.created', 'user.updated']),
    manager.register('https://notifications.example.com', ['order.created', 'payment.received']),
    manager.register('https://accounting.example.com', ['payment.received', 'refund.issued'])
  ];

  console.log(`✓ Registered ${webhooks.length} webhooks`);

  // Find webhooks for event
  console.log('\nFinding webhooks for "order.created" event...');
  const orderCreatedWebhooks = manager.findForEvent('order.created');
  orderCreatedWebhooks.forEach(w => {
    console.log(`  ✓ ${w.url}`);
  });

  // Broadcast event to all subscribed webhooks
  console.log('\nBroadcasting order.created event...');
  const orderData = {
    orderId: 'ord_789',
    customerId: 'cust_123',
    total: 299.99,
    items: [
      { productId: 'prod_1', quantity: 2, price: 99.99 },
      { productId: 'prod_2', quantity: 1, price: 100.01 }
    ],
    timestamp: new Date().toISOString()
  };

  const results = await Promise.allSettled(
    orderCreatedWebhooks.map(w => manager.trigger(w.id, 'order.created', orderData))
  );

  results.forEach((result, i) => {
    if (result.status === 'fulfilled') {
      console.log(`  ✓ Delivered to webhook ${i + 1}`);
    } else {
      console.log(`  ⚠ Failed to webhook ${i + 1}: ${result.reason.message}`);
    }
  });

  const stats = manager.getStats();
  console.log(`\nBroadcast Stats: Webhooks: ${stats.webhookCount}, Delivered: ${stats.delivered}`);
}

// ============================================================================
// EXAMPLE 4: Webhook Management (Update, List, Delete)
// ============================================================================

function example4_WebhookManagement() {
  console.log('\n=== EXAMPLE 4: Webhook Management ===\n');
  
  const manager = new WebhookManager();

  // Register webhooks
  console.log('Registering webhooks...');
  const webhook1 = manager.register(
    'https://api.example.com/v1/webhooks',
    ['user.created', 'user.updated']
  );
  const webhook2 = manager.register(
    'https://old-api.example.com/webhooks',
    ['order.created']
  );
  console.log(`✓ Registered 2 webhooks`);

  // List all
  console.log('\nListing all webhooks:');
  manager.list().forEach(w => {
    console.log(`  - ${w.id}: ${w.url} (${w.events.join(', ')})`);
  });

  // Update webhook
  console.log('\nUpdating webhook...');
  manager.update(webhook2.id, {
    url: 'https://new-api.example.com/webhooks',
    events: ['order.created', 'order.completed'],
    metadata: { version: '2.0' }
  });
  console.log(`✓ Updated webhook ${webhook2.id}`);

  // Deactivate webhook
  console.log('\nDeactivating webhook...');
  manager.update(webhook1.id, { active: false });
  console.log(`✓ Deactivated webhook ${webhook1.id}`);

  // List active only
  console.log('\nActive webhooks:');
  manager.list({ active: true }).forEach(w => {
    console.log(`  - ${w.url}`);
  });

  // Delete webhook
  console.log('\nDeleting webhook...');
  manager.delete(webhook1.id);
  console.log(`✓ Deleted ${webhook1.id}`);
  console.log(`Total webhooks: ${manager.list().length}`);
}

// ============================================================================
// EXAMPLE 5: Retry Logic with Exponential Backoff
// ============================================================================

async function example5_RetryLogic() {
  console.log('\n=== EXAMPLE 5: Retry Logic with Exponential Backoff ===\n');
  
  const manager = new WebhookManager({
    retry: {
      maxRetries: 4,
      initialDelay: 100,
      maxDelay: 5000,
      backoffMultiplier: 2
    }
  });

  // Track delivery attempts
  let attempts = 0;
  let failCount = 0;

  manager.on('failed', (webhookId, event, error) => {
    failCount++;
    console.log(`  ✗ Failed: ${error.message} (Attempt #${failCount})`);
  });

  manager.on('delivered', (webhookId, event) => {
    console.log(`  ✓ Delivered after retry`);
  });

  const webhook = manager.register(
    'https://flaky-service.example.com/webhooks',
    ['data.processed']
  );

  console.log('Triggering webhook with retry logic...');
  console.log('(Service will fail first 2 times, then succeed)\n');

  // Simulate flaky service
  let callCount = 0;
  const originalSend = manager._send.bind(manager);
  manager._send = async (w, payload, sig) => {
    callCount++;
    if (callCount <= 2) {
      throw new Error('Temporary service unavailable');
    }
    return { status: 200 };
  };

  try {
    await manager.trigger(webhook.id, 'data.processed', { data: 'test' });
  } catch (err) {
    console.log(`Final error: ${err.message}`);
  }

  // Restore original
  manager._send = originalSend;
}

// ============================================================================
// EXAMPLE 6: Rate Limiting
// ============================================================================

function example6_RateLimiting() {
  console.log('\n=== EXAMPLE 6: Rate Limiting ===\n');
  
  const manager = new WebhookManager({
    rateLimit: {
      requestsPerSecond: 5,
      maxBurst: 3
    }
  });

  const webhook = manager.register(
    'https://api.example.com/webhooks',
    ['event.occurred']
  );

  console.log('Testing rate limiter...\n');
  console.log('Attempting 10 rapid deliveries (limit: 5/sec, burst: 3):');

  let delivered = 0;
  let blocked = 0;

  for (let i = 1; i <= 10; i++) {
    if (manager.rateLimiter.canDeliver(webhook.id)) {
      console.log(`  ${i}. ✓ Allowed`);
      delivered++;
    } else {
      console.log(`  ${i}. ✗ Rate limited`);
      blocked++;
    }
  }

  console.log(`\nResults: ${delivered} allowed, ${blocked} blocked`);

  // Check status
  const status = manager.rateLimiter.getStatus(webhook.id);
  console.log(`\nRate limit status:`);
  console.log(`  Webhook tokens: ${status.webhook.tokens.toFixed(2)}/${status.webhook.limit}`);
  console.log(`  Global tokens: ${status.global.tokens.toFixed(2)}/${status.global.limit}`);
}

// ============================================================================
// EXAMPLE 7: Event Hooks and Monitoring
// ============================================================================

async function example7_EventHooks() {
  console.log('\n=== EXAMPLE 7: Event Hooks and Monitoring ===\n');
  
  const manager = new WebhookManager();

  // Setup comprehensive event tracking
  const log = [];

  manager.on('registered', (webhook) => {
    log.push(`[REGISTERED] ${webhook.id} -> ${webhook.url}`);
  });

  manager.on('delivered', (webhookId, event) => {
    log.push(`[DELIVERED] ${event} -> ${webhookId.substring(0, 15)}...`);
  });

  manager.on('failed', (webhookId, event, error) => {
    log.push(`[FAILED] ${event} (${error.message})`);
  });

  manager.on('retried', (webhookId, attempt) => {
    log.push(`[RETRY] Attempt ${attempt}`);
  });

  // Register and trigger
  const webhook = manager.register(
    'https://api.example.com/webhooks',
    ['test.event']
  );

  try {
    await manager.trigger(webhook.id, 'test.event', { data: 'test' });
  } catch (err) {
    console.log(`Trigger error: ${err.message}`);
  }

  // Display log
  console.log('Event log:');
  log.forEach(entry => {
    console.log(`  ${entry}`);
  });

  const stats = manager.getStats();
  console.log(`\nFinal statistics:`);
  console.log(`  Total registered: ${stats.registered}`);
  console.log(`  Delivered: ${stats.delivered}`);
  console.log(`  Failed: ${stats.failed}`);
  console.log(`  Active webhooks: ${stats.activeCount}`);
}

// ============================================================================
// EXAMPLE 8: Complete Order Processing Pipeline
// ============================================================================

async function example8_OrderProcessingPipeline() {
  console.log('\n=== EXAMPLE 8: Complete Order Processing Pipeline ===\n');
  
  const manager = new WebhookManager({
    retry: {
      maxRetries: 3,
      initialDelay: 200
    },
    rateLimit: {
      requestsPerSecond: 20
    }
  });

  // Register webhooks for different services
  console.log('Setting up webhook integrations...\n');

  const services = {
    inventory: manager.register(
      'https://inventory.example.com/webhooks',
      ['order.created']
    ),
    accounting: manager.register(
      'https://accounting.example.com/webhooks',
      ['payment.processed', 'order.completed']
    ),
    shipping: manager.register(
      'https://shipping.example.com/webhooks',
      ['order.completed', 'shipment.created']
    ),
    customer: manager.register(
      'https://customer.example.com/webhooks',
      ['order.created', 'order.completed', 'shipment.created']
    )
  };

  console.log(`✓ Registered ${Object.keys(services).length} services\n`);

  // Simulate order lifecycle
  const order = {
    orderId: 'ORD-2024-001',
    customerId: 'CUST-123',
    total: 449.99,
    items: [
      { sku: 'PROD-001', qty: 2, price: 199.99 },
      { sku: 'PROD-002', qty: 1, price: 50.01 }
    ],
    status: 'created',
    timestamp: new Date().toISOString()
  };

  // Order created
  console.log('Order created - notifying services...');
  const createdWebhooks = manager.findForEvent('order.created');
  await Promise.allSettled(
    createdWebhooks.map(w => manager.trigger(w.id, 'order.created', order))
  );
  console.log(`✓ Notified ${createdWebhooks.length} services\n`);

  // Payment processed
  order.status = 'paid';
  console.log('Payment processed - notifying accounting...');
  await manager.trigger(services.accounting.id, 'payment.processed', {
    orderId: order.orderId,
    amount: order.total,
    timestamp: new Date().toISOString()
  });
  console.log('✓ Accounting notified\n');

  // Order completed
  order.status = 'completed';
  console.log('Order completed - notifying all services...');
  const completedWebhooks = manager.findForEvent('order.completed');
  await Promise.allSettled(
    completedWebhooks.map(w => manager.trigger(w.id, 'order.completed', order))
  );
  console.log(`✓ Notified ${completedWebhooks.length} services\n`);

  // Final stats
  const stats = manager.getStats();
  console.log('Pipeline Summary:');
  console.log(`  Total deliveries: ${stats.delivered + stats.failed}`);
  console.log(`  Successful: ${stats.delivered}`);
  console.log(`  Failed: ${stats.failed}`);
  console.log(`  Active webhooks: ${stats.activeCount}/${stats.webhookCount}`);
}

// ============================================================================
// Run All Examples
// ============================================================================

async function runAllExamples() {
  console.log('╔════════════════════════════════════════════════════════════╗');
  console.log('║   HELIOS Webhook Manager Module - Comprehensive Examples   ║');
  console.log('╚════════════════════════════════════════════════════════════╝');

  try {
    await example1_BasicWebhook();
    example2_SignatureVerification();
    await example3_EventBroadcasting();
    example4_WebhookManagement();
    await example5_RetryLogic();
    example6_RateLimiting();
    await example7_EventHooks();
    await example8_OrderProcessingPipeline();

    console.log('\n╔════════════════════════════════════════════════════════════╗');
    console.log('║              All Examples Completed Successfully            ║');
    console.log('╚════════════════════════════════════════════════════════════╝\n');
  } catch (err) {
    console.error('Error running examples:', err);
  }
}

// Export for testing
module.exports = {
  example1_BasicWebhook,
  example2_SignatureVerification,
  example3_EventBroadcasting,
  example4_WebhookManagement,
  example5_RetryLogic,
  example6_RateLimiting,
  example7_EventHooks,
  example8_OrderProcessingPipeline
};

// Run if executed directly
if (require.main === module) {
  runAllExamples();
}
