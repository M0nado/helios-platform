/**
 * Message Queue Module - Comprehensive Examples
 * Demonstrates real-world usage patterns and scenarios
 */

const { MessageQueue, DeliveryModes } = require('./index');

// ============================================================================
// EXAMPLE 1: Basic FIFO Queue for Task Processing
// ============================================================================

function example1_BasicFIFO() {
  console.log('\n=== EXAMPLE 1: Basic FIFO Queue ===\n');
  
  const queue = new MessageQueue({
    ordering: 'fifo',
    deliveryMode: DeliveryModes.AT_LEAST_ONCE,
    maxRetries: 3
  });

  // Simulate task producer
  const tasks = [
    { type: 'email', to: 'alice@example.com', subject: 'Welcome' },
    { type: 'sms', phone: '+1234567890', message: 'Code: 123456' },
    { type: 'database', action: 'backup', timestamp: Date.now() }
  ];

  console.log('Enqueueing tasks...');
  tasks.forEach(task => {
    const id = queue.enqueue(task);
    console.log(`  ✓ Enqueued ${task.type}: ${id}`);
  });

  // Simulate task consumer
  console.log('\nProcessing tasks...');
  let processed = 0;
  while (queue.size() > 0) {
    const msg = queue.dequeue();
    console.log(`  Processing: ${msg.payload.type}`);
    try {
      // Simulate task execution
      if (msg.payload.type === 'sms') {
        throw new Error('SMS service unavailable');
      }
      queue.acknowledge(msg.id);
      processed++;
      console.log(`  ✓ Completed: ${msg.payload.type}`);
    } catch (err) {
      const shouldRetry = queue.fail(msg.id, err);
      if (shouldRetry) {
        console.log(`  ⚠ Failed, will retry: ${err.message}`);
      } else {
        console.log(`  ✗ Exhausted retries: ${err.message}`);
      }
    }
  }

  const stats = queue.getStats();
  console.log(`\nFinal Stats: ${processed} processed, DLQ: ${stats.dlqStats.current}`);
}

// ============================================================================
// EXAMPLE 2: Priority Queue for Mixed Workloads
// ============================================================================

function example2_PriorityQueue() {
  console.log('\n=== EXAMPLE 2: Priority Queue ===\n');
  
  const queue = new MessageQueue({
    ordering: 'priority'
  });

  // Enqueue tasks with different priorities
  const tasks = [
    { priority: 1, task: 'Log rotation', type: 'maintenance' },
    { priority: 50, task: 'API request', type: 'normal' },
    { priority: 100, task: 'Critical alert', type: 'urgent' },
    { priority: 50, task: 'Cache update', type: 'normal' },
    { priority: 75, task: 'Report generation', type: 'high' }
  ];

  console.log('Enqueueing tasks with priorities...');
  tasks.forEach(({ priority, task, type }) => {
    queue.enqueue({ task, type }, { priority });
    console.log(`  ✓ Priority ${priority}: ${task}`);
  });

  console.log('\nProcessing in priority order...');
  while (queue.size() > 0) {
    const msg = queue.dequeue();
    console.log(`  [P${msg.priority}] ${msg.payload.task}`);
    queue.acknowledge(msg.id);
  }
}

// ============================================================================
// EXAMPLE 3: Exactly-Once Delivery Semantics
// ============================================================================

function example3_ExactlyOnce() {
  console.log('\n=== EXAMPLE 3: Exactly-Once Delivery ===\n');
  
  const queue = new MessageQueue({
    deliveryMode: DeliveryModes.EXACTLY_ONCE,
    maxRetries: 5
  });

  // Subscribe to all events
  queue.on('enqueued', (msg) => {
    console.log(`  📥 Enqueued: ${msg.id}`);
  });

  queue.on('dequeued', (msg) => {
    console.log(`  ⬆️  Dequeued: ${msg.id}`);
  });

  queue.on('acknowledged', (id) => {
    console.log(`  ✓ Acknowledged: ${id}`);
  });

  queue.on('failed', (id, err) => {
    console.log(`  ✗ Failed DLQ: ${id} - ${err.message}`);
  });

  queue.on('retried', (id, attempt) => {
    console.log(`  🔄 Retry attempt ${attempt}: ${id}`);
  });

  // Simulate duplicate delivery attempt
  console.log('Processing with exactly-once semantics...\n');
  
  const msgId = queue.enqueue({ payment: 100 });
  const msg = queue.dequeue();
  
  queue.acknowledge(msg.id);
  console.log('\nTrying to acknowledge same message again...');
  
  const secondAck = queue.acknowledge(msg.id);
  console.log(`Second acknowledge successful: ${secondAck} (should be false)`);
}

// ============================================================================
// EXAMPLE 4: Dead Letter Queue Handling
// ============================================================================

function example4_DeadLetterQueue() {
  console.log('\n=== EXAMPLE 4: Dead Letter Queue (DLQ) ===\n');
  
  const queue = new MessageQueue({
    maxRetries: 2,
    dlq: { maxSize: 1000, ttl: 86400000 }
  });

  queue.on('failed', (id, err) => {
    const dlqEntry = queue.dlq.get(id);
    if (dlqEntry) {
      console.log(`  💀 Added to DLQ: ${id}`);
      console.log(`     Reason: ${dlqEntry.reason}`);
    }
  });

  // Simulate failing messages
  const messages = [
    { orderId: '001', amount: 99.99 },
    { orderId: '002', amount: -10 },      // Will fail validation
    { orderId: '003', amount: 199.99 }
  ];

  console.log('Enqueueing orders...');
  const ids = messages.map(msg => queue.enqueue(msg));

  console.log('\nProcessing with deliberate failures...');
  let attempts = 0;
  while (queue.size() > 0) {
    const msg = queue.dequeue();
    attempts++;
    
    // Validation error on negative amounts
    if (msg.payload.amount < 0) {
      const err = new Error('Invalid order amount');
      queue.fail(msg.id, err);
    } else {
      queue.acknowledge(msg.id);
      console.log(`  ✓ Order ${msg.payload.orderId} processed`);
    }
  }

  const stats = queue.getStats();
  console.log(`\nQueue Stats:`);
  console.log(`  Enqueued: ${stats.enqueued}`);
  console.log(`  Processed: ${stats.acknowledged}`);
  console.log(`  Dead Letters: ${stats.dlqStats.current}`);

  console.log('\nDLQ Contents:');
  queue.dlq.getAll().forEach(entry => {
    console.log(`  - ${entry.messageId}: ${entry.reason}`);
  });
}

// ============================================================================
// EXAMPLE 5: Timeout Recovery and Self-Healing
// ============================================================================

function example5_TimeoutRecovery() {
  console.log('\n=== EXAMPLE 5: Timeout Recovery ===\n');
  
  const queue = new MessageQueue({
    maxRetries: 3,
    idleTimeout: 2000  // 2 second timeout for demo
  });

  console.log('Enqueueing message...');
  const msgId = queue.enqueue({ task: 'process' });
  
  console.log('Dequeuing message...');
  const msg = queue.dequeue();
  console.log(`Message in-flight: ${msg.id}`);

  console.log('\nWaiting for timeout...');
  setTimeout(() => {
    console.log('Checking for timed-out messages...');
    const recovered = queue.recoverTimedOut();
    console.log(`Recovered: ${recovered} message(s)`);
    
    if (recovered > 0) {
      const retry = queue.dequeue();
      console.log(`Retry attempt ${retry.attempts} for ${retry.id}`);
      queue.acknowledge(retry.id);
    }
  }, 2500);
}

// ============================================================================
// EXAMPLE 6: Real-World Email Queue
// ============================================================================

function example6_EmailQueue() {
  console.log('\n=== EXAMPLE 6: Real-World Email Queue ===\n');
  
  const queue = new MessageQueue({
    ordering: 'priority',
    deliveryMode: DeliveryModes.AT_LEAST_ONCE,
    maxRetries: 3
  });

  // Track email stats
  const stats = {
    sent: 0,
    failed: 0,
    logs: []
  };

  queue.on('acknowledged', (id) => {
    stats.sent++;
  });

  queue.on('failed', (id, err) => {
    stats.failed++;
  });

  // Simulate email sending service
  async function sendEmail(email) {
    return new Promise((resolve, reject) => {
      setTimeout(() => {
        // Simulate occasional failures
        if (Math.random() < 0.3 && email.attempts < 2) {
          reject(new Error('SMTP timeout'));
        } else {
          resolve({ messageId: email.id });
        }
      }, 100);
    });
  }

  // Queue emails
  const emails = [
    { to: 'user@example.com', subject: 'Welcome', priority: 50 },
    { to: 'admin@example.com', subject: 'Alert', priority: 100 },
    { to: 'newsletter@example.com', subject: 'Weekly', priority: 10 },
    { to: 'payment@example.com', subject: 'Receipt', priority: 75 }
  ];

  console.log('Queueing emails...');
  emails.forEach(email => {
    queue.enqueue(email, { priority: email.priority });
    console.log(`  ✓ ${email.to} (P${email.priority})`);
  });

  // Process queue
  console.log('\nSending emails...');
  const processQueue = setInterval(async () => {
    if (queue.size() === 0) {
      clearInterval(processQueue);
      console.log(`\nEmail Queue Summary:`);
      console.log(`  Sent: ${stats.sent}`);
      console.log(`  Failed: ${stats.failed}`);
      console.log(`  DLQ: ${queue.dlq.getStats().current}`);
      return;
    }

    const msg = queue.dequeue();
    try {
      await sendEmail(msg.payload);
      queue.acknowledge(msg.id);
      console.log(`  ✓ Sent to ${msg.payload.to}`);
    } catch (err) {
      const shouldRetry = queue.fail(msg.id, err);
      if (shouldRetry) {
        console.log(`  ⚠ Failed (retry): ${msg.payload.to} - ${err.message}`);
      } else {
        console.log(`  ✗ Failed (DLQ): ${msg.payload.to}`);
      }
    }
  }, 100);
}

// ============================================================================
// EXAMPLE 7: Batch Processing with Statistics
// ============================================================================

function example7_BatchProcessing() {
  console.log('\n=== EXAMPLE 7: Batch Processing ===\n');
  
  const queue = new MessageQueue();
  
  // Enqueue batch of items
  console.log('Loading batch data...');
  const itemCount = 100;
  for (let i = 1; i <= itemCount; i++) {
    queue.enqueue({
      itemId: `item-${i}`,
      value: Math.random() * 1000,
      timestamp: Date.now()
    });
  }

  // Process in batches
  const batchSize = 10;
  let batchNum = 1;
  const results = [];

  console.log(`\nProcessing ${itemCount} items in batches of ${batchSize}...\n`);

  while (queue.size() > 0) {
    const batch = [];
    for (let i = 0; i < batchSize && queue.size() > 0; i++) {
      const msg = queue.dequeue();
      batch.push(msg);
    }

    // Process batch
    const batchTotal = batch.reduce((sum, msg) => sum + msg.payload.value, 0);
    const batchAvg = batchTotal / batch.length;

    console.log(`Batch ${batchNum}: ${batch.length} items, Total: $${batchTotal.toFixed(2)}, Avg: $${batchAvg.toFixed(2)}`);

    // Acknowledge all in batch
    batch.forEach(msg => {
      queue.acknowledge(msg.id);
      results.push(msg.payload);
    });

    batchNum++;
  }

  const allStats = queue.getStats();
  console.log(`\nProcessing Complete:`);
  console.log(`  Total Processed: ${allStats.acknowledged}`);
  console.log(`  Total Enqueued: ${allStats.enqueued}`);
  console.log(`  Queue Empty: ${queue.size() === 0}`);
}

// ============================================================================
// Run All Examples
// ============================================================================

async function runAllExamples() {
  console.log('╔════════════════════════════════════════════════════════════╗');
  console.log('║   HELIOS Message Queue Module - Comprehensive Examples    ║');
  console.log('╚════════════════════════════════════════════════════════════╝');

  try {
    example1_BasicFIFO();
    example2_PriorityQueue();
    example3_ExactlyOnce();
    example4_DeadLetterQueue();
    // Note: Example 5 uses setTimeout, so we skip it in sequential run
    // example5_TimeoutRecovery();
    
    // Run email queue async
    await new Promise(resolve => {
      example6_EmailQueue();
      setTimeout(resolve, 2000);
    });
    
    example7_BatchProcessing();

    console.log('\n╔════════════════════════════════════════════════════════════╗');
    console.log('║              All Examples Completed Successfully            ║');
    console.log('╚════════════════════════════════════════════════════════════╝\n');
  } catch (err) {
    console.error('Error running examples:', err);
  }
}

// Export for testing
module.exports = {
  example1_BasicFIFO,
  example2_PriorityQueue,
  example3_ExactlyOnce,
  example4_DeadLetterQueue,
  example5_TimeoutRecovery,
  example6_EmailQueue,
  example7_BatchProcessing
};

// Run if executed directly
if (require.main === module) {
  runAllExamples();
}
