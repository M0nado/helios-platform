/**
 * HELIOS v4.0 Telemetry Module - Real-World Examples
 * @module feat-telemetry/examples
 */

const {
  RequestTracer,
  MetricsCollector,
  EventLogger,
  CorrelationManager,
  createTelemetrySystem
} = require('./index');

/**
 * Example 1: Request Tracing
 * Demonstrates distributed request tracing with span management
 */
async function example1_RequestTracing() {
  console.log('\n=== Example 1: Request Tracing ===\n');

  const tracer = new RequestTracer({
    serviceName: 'api-gateway',
    samplingRate: 1.0,
    onTraceComplete: (trace) => {
      console.log(`✓ Trace completed: ${trace.traceId} (${trace.duration}ms)`);
      console.log(`  Spans: ${trace.spans.length}, Status: ${trace.status}`);
    }
  });

  // Simulate HTTP request processing
  const traceId = 'trace_user_123_456';
  const { spanId } = tracer.startTrace(traceId, {
    userId: 'user_789',
    requestId: 'req_abc_def'
  });

  console.log(`Started trace: ${traceId}`);

  // Database query span
  const dbSpan = tracer.addSpan(traceId, {
    spanId: 'span_db_query',
    operationName: 'database-query',
    tags: { database: 'postgres', table: 'users' }
  });

  await new Promise(resolve => setTimeout(resolve, 50));
  tracer.endSpan(traceId, dbSpan.spanId, {
    status: 'success',
    data: { rowsAffected: 1 }
  });

  // Cache lookup span
  const cacheSpan = tracer.addSpan(traceId, {
    spanId: 'span_cache_lookup',
    operationName: 'cache-lookup',
    tags: { cache: 'redis', key: 'user:789' }
  });

  await new Promise(resolve => setTimeout(resolve, 10));
  tracer.endSpan(traceId, cacheSpan.spanId, {
    status: 'success',
    data: { hit: true }
  });

  // Complete trace
  const trace = tracer.completeTrace(traceId, {
    status: 'success'
  });

  console.log(`Active traces: ${tracer.getActiveTraces().length}`);
}

/**
 * Example 2: Metrics Collection with Aggregation
 * Demonstrates recording and aggregating different metric types
 */
async function example2_MetricsCollection() {
  console.log('\n=== Example 2: Metrics Collection ===\n');

  const metrics = new MetricsCollector({
    aggregationWindowMs: 10000,
    onMetricReady: (aggregated) => {
      console.log('📊 Metrics Report Ready:');
      console.log(`  Counters: ${aggregated.counters.length}`);
      aggregated.counters.slice(0, 3).forEach(c => {
        console.log(`    - ${c.name}: ${c.count}`);
      });
      console.log(`  Histograms: ${aggregated.histograms.length}`);
      aggregated.histograms.slice(0, 2).forEach(h => {
        console.log(`    - ${h.name}: p50=${h.p50}ms, p95=${h.p95}ms`);
      });
    }
  });

  // Simulate request metrics
  for (let i = 0; i < 50; i++) {
    const latency = Math.random() * 500;
    const endpoint = ['/api/users', '/api/posts', '/api/comments'][
      Math.floor(Math.random() * 3)
    ];

    metrics.recordCounter('http.requests.total', 1, { endpoint });
    metrics.recordHistogram('http.request.duration.ms', latency, { endpoint });

    if (i % 10 === 0) {
      metrics.recordGauge('system.memory.mb', 256 + Math.random() * 100);
      metrics.recordGauge('system.cpu.usage', Math.random() * 80);
    }
  }

  const report = metrics.getMetrics();
  console.log('\n📈 Current Metrics:');
  console.log(`  Total counters: ${report.counters.length}`);
  console.log(`  Total gauges: ${report.gauges.length}`);
  console.log(`  Total histograms: ${report.histograms.length}`);
}

/**
 * Example 3: Event Logging with Batching
 * Demonstrates structured logging and batch processing
 */
async function example3_EventLogging() {
  console.log('\n=== Example 3: Event Logging ===\n');

  let batchCount = 0;

  const logger = new EventLogger({
    batchSize: 20,
    flushIntervalMs: 5000,
    onBatchReady: (events) => {
      batchCount++;
      console.log(`✓ Batch ${batchCount} ready (${events.length} events)`);
      console.log(`  Sample: "${events[0].message}"`);
    }
  });

  // Simulate application events
  const events = [
    { level: 'INFO', msg: 'Server started on port 3000' },
    { level: 'DEBUG', msg: 'Database connection initialized' },
    { level: 'INFO', msg: 'User logged in', userId: 'user_123' },
    { level: 'WARN', msg: 'High memory usage detected', memory: '85%' },
    { level: 'INFO', msg: 'Cache warmed up with 1000 entries' },
    { level: 'ERROR', msg: 'Failed to connect to external API', api: 'stripe' },
    { level: 'INFO', msg: 'Background job completed', jobId: 'job_456' },
    { level: 'DEBUG', msg: 'Request trace completed', duration: '145ms' }
  ];

  events.forEach((e, i) => {
    logger[e.level.toLowerCase()](e.msg, { ...e, eventIndex: i });
  });

  console.log(`📝 Pending events: ${logger.getPendingCount()}`);

  // Manual flush
  const flushed = logger.flush();
  console.log(`✓ Manually flushed ${flushed.length} events`);

  logger.stop();
}

/**
 * Example 4: Correlation ID Propagation
 * Demonstrates context management across service calls
 */
async function example4_CorrelationPropagation() {
  console.log('\n=== Example 4: Correlation Propagation ===\n');

  const correlator = new CorrelationManager({
    headerName: 'x-correlation-id'
  });

  // Service A receives request
  const correlationId = 'corr_req_123_456';
  const contextA = correlator.createContext(correlationId, {
    userId: 'user_789',
    service: 'service-a'
  });

  correlator.pushContext(contextA);
  console.log(`🔗 Context created: ${correlationId}`);

  correlator.addBreadcrumb('received-request', {
    endpoint: '/api/users/789',
    method: 'GET'
  });

  // Service A calls Service B
  const outgoingHeaders = correlator.formatHeaders(correlationId, {
    'content-type': 'application/json',
    'authorization': 'Bearer token_xyz'
  });

  console.log(`📤 Outgoing headers:`);
  console.log(`  ${Object.keys(outgoingHeaders).join(', ')}`);

  // Service B receives request
  const incomingId = correlator.extractFromHeaders(outgoingHeaders);
  console.log(`📥 Extracted correlation ID: ${incomingId}`);

  const contextB = correlator.createContext(incomingId, {
    service: 'service-b'
  });

  correlator.pushContext(contextB);

  correlator.addBreadcrumb('called-auth-service', { latencyMs: 45 });
  correlator.addBreadcrumb('called-db-service', { latencyMs: 120 });
  correlator.addBreadcrumb('returned-response', { statusCode: 200 });

  // Retrieve full context with breadcrumbs
  const context = correlator.getCurrentContext();
  console.log(`\n📍 Context breadcrumbs (${context.breadcrumbs.length}):`);
  context.breadcrumbs.forEach((bc, i) => {
    console.log(`  ${i + 1}. ${bc.action} (+${bc.data.latencyMs || bc.data.statusCode})`);
  });

  correlator.popContext();
  console.log(`✓ Context popped`);
}

/**
 * Example 5: Integrated Telemetry System
 * Demonstrates all components working together
 */
async function example5_IntegratedTelemetrySystem() {
  console.log('\n=== Example 5: Integrated Telemetry System ===\n');

  const telemetry = createTelemetrySystem({
    serviceName: 'order-service',
    samplingRate: 1.0,
    onTraceComplete: (trace) => {
      console.log(`✓ Trace: ${trace.traceId} completed in ${trace.duration}ms`);
    },
    onMetricReady: (metrics) => {
      console.log(`📊 Metrics: ${metrics.counters.length} counters, ${metrics.histograms.length} histograms`);
    },
    onEventBatch: (events) => {
      console.log(`📝 Event batch: ${events.length} events`);
    }
  });

  // Simulate order processing
  const correlationId = 'order_12345';
  const traceId = `trace_${correlationId}`;

  // Create correlation context
  const ctx = telemetry.correlator.createContext(correlationId, {
    orderId: 'order_12345',
    customerId: 'cust_6789'
  });
  telemetry.correlator.pushContext(ctx);

  // Start trace
  const { spanId } = telemetry.tracer.startTrace(traceId, {
    userId: 'cust_6789'
  });

  telemetry.logger.info('Order processing started', {
    orderId: 'order_12345',
    traceId: traceId,
    correlationId: correlationId
  });

  // Payment processing
  const paymentSpan = telemetry.tracer.addSpan(traceId, {
    spanId: 'span_payment',
    operationName: 'process-payment',
    tags: { gateway: 'stripe' }
  });

  await new Promise(resolve => setTimeout(resolve, 100));
  const paymentLatency = 100;

  telemetry.tracer.endSpan(traceId, paymentSpan.spanId, {
    status: 'success'
  });

  telemetry.metrics.recordHistogram('payment.latency.ms', paymentLatency, {
    gateway: 'stripe'
  });

  telemetry.logger.info('Payment processed successfully', {
    amount: 99.99,
    currency: 'USD'
  });

  // Inventory update
  const inventorySpan = telemetry.tracer.addSpan(traceId, {
    spanId: 'span_inventory',
    operationName: 'update-inventory',
    tags: { warehouse: 'us-west' }
  });

  await new Promise(resolve => setTimeout(resolve, 50));

  telemetry.tracer.endSpan(traceId, inventorySpan.spanId, {
    status: 'success'
  });

  telemetry.metrics.recordCounter('inventory.updated', 3, {
    warehouse: 'us-west'
  });

  telemetry.logger.info('Inventory updated', {
    itemsUpdated: 3
  });

  // Complete trace
  telemetry.tracer.completeTrace(traceId, { status: 'success' });

  telemetry.metrics.recordCounter('orders.completed', 1, {
    status: 'success'
  });

  telemetry.logger.info('Order processing completed', {
    orderId: 'order_12345',
    totalLatency: 'ms'
  });

  telemetry.correlator.addBreadcrumb('order-completed', {
    orderId: 'order_12345'
  });

  // Shutdown
  telemetry.shutdown();
  console.log('\n✓ Telemetry system shutdown complete');
}

/**
 * Example 6: Error Handling and Recovery
 * Demonstrates error scenarios and recovery patterns
 */
async function example6_ErrorHandling() {
  console.log('\n=== Example 6: Error Handling ===\n');

  const tracer = new RequestTracer({
    serviceName: 'api-gateway',
    onTraceComplete: (trace) => {
      console.log(`Trace: ${trace.traceId}, Status: ${trace.status}`);
    }
  });

  const traceId = 'trace_error_scenario';
  const { spanId } = tracer.startTrace(traceId);

  // Simulate failed operation
  const errorSpan = tracer.addSpan(traceId, {
    spanId: 'span_failed_op',
    operationName: 'external-api-call',
    tags: { api: 'third-party' }
  });

  await new Promise(resolve => setTimeout(resolve, 50));

  tracer.endSpan(traceId, errorSpan.spanId, {
    status: 'error',
    data: { error: 'ECONNREFUSED', message: 'Connection refused' }
  });

  console.log('✓ Recorded failed operation span');

  // Retry span
  const retrySpan = tracer.addSpan(traceId, {
    spanId: 'span_retry',
    operationName: 'external-api-call-retry',
    tags: { api: 'third-party', attempt: 2 }
  });

  await new Promise(resolve => setTimeout(resolve, 100));

  tracer.endSpan(traceId, retrySpan.spanId, {
    status: 'success',
    data: { result: 'recovered' }
  });

  console.log('✓ Recorded successful retry');

  tracer.completeTrace(traceId, {
    status: 'success'
  });

  console.log('✓ Trace completed with successful recovery');
}

/**
 * Run all examples
 */
async function runAllExamples() {
  console.log('\n╔════════════════════════════════════════════════════════════╗');
  console.log('║   HELIOS v4.0 Telemetry Module - Real-World Examples      ║');
  console.log('╚════════════════════════════════════════════════════════════╝');

  try {
    await example1_RequestTracing();
    await new Promise(resolve => setTimeout(resolve, 100));

    await example2_MetricsCollection();
    await new Promise(resolve => setTimeout(resolve, 100));

    await example3_EventLogging();
    await new Promise(resolve => setTimeout(resolve, 100));

    await example4_CorrelationPropagation();
    await new Promise(resolve => setTimeout(resolve, 100));

    await example5_IntegratedTelemetrySystem();
    await new Promise(resolve => setTimeout(resolve, 100));

    await example6_ErrorHandling();

    console.log('\n╔════════════════════════════════════════════════════════════╗');
    console.log('║              All examples completed successfully            ║');
    console.log('╚════════════════════════════════════════════════════════════╝\n');
  } catch (error) {
    console.error('Example failed:', error.message);
    process.exit(1);
  }
}

// Export examples for testing
module.exports = {
  example1_RequestTracing,
  example2_MetricsCollection,
  example3_EventLogging,
  example4_CorrelationPropagation,
  example5_IntegratedTelemetrySystem,
  example6_ErrorHandling,
  runAllExamples
};

// Run if executed directly
if (require.main === module) {
  runAllExamples().catch(console.error);
}
