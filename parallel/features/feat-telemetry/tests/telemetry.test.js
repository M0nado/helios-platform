/**
 * HELIOS v4.0 Telemetry Module - Test Suite
 * 45+ tests covering RequestTracer, MetricsCollector, EventLogger, CorrelationManager
 */

const {
  RequestTracer,
  MetricsCollector,
  EventLogger,
  CorrelationManager,
  createTelemetrySystem
} = require('../index');

let testCount = 0;
let passCount = 0;
let failCount = 0;

function test(name, fn) {
  testCount++;
  try {
    fn();
    passCount++;
    console.log(`✓ ${name}`);
  } catch (error) {
    failCount++;
    console.log(`✗ ${name}`);
    console.log(`  Error: ${error.message}`);
  }
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message || 'Assertion failed');
  }
}

function assertEquals(actual, expected, message) {
  if (actual !== expected) {
    throw new Error(message || `Expected ${expected}, got ${actual}`);
  }
}

console.log('\n╔════════════════════════════════════════════════════════════╗');
console.log('║        HELIOS v4.0 Telemetry Module - Test Suite          ║');
console.log('╚════════════════════════════════════════════════════════════╝\n');

// ============================================================================
// RequestTracer Tests
// ============================================================================
console.log('📍 RequestTracer Tests:');

test('should create RequestTracer instance', () => {
  const tracer = new RequestTracer();
  assert(tracer !== null);
  assert(tracer.traces instanceof Map);
});

test('should start a trace', () => {
  const tracer = new RequestTracer();
  const result = tracer.startTrace('trace123');
  assert(result.traceId === 'trace123');
  assert(result.sampled === true);
  assert(result.spanId !== undefined);
});

test('should throw on invalid trace ID', () => {
  const tracer = new RequestTracer();
  try {
    tracer.startTrace('');
    throw new Error('Should have thrown');
  } catch (err) {
    assert(err.message.includes('must be a non-empty string'));
  }
});

test('should enforce sampling rate', () => {
  const tracer = new RequestTracer({ samplingRate: 0 });
  const result = tracer.startTrace('trace123');
  assert(result.sampled === false);
});

test('should add span to trace', () => {
  const tracer = new RequestTracer();
  tracer.startTrace('trace123');
  const span = tracer.addSpan('trace123', {
    spanId: 'span1',
    operationName: 'test-op'
  });
  assert(span.operationName === 'test-op');
  assert(span.status === 'active');
});

test('should throw on invalid span data', () => {
  const tracer = new RequestTracer();
  tracer.startTrace('trace123');
  try {
    tracer.addSpan('trace123', { spanId: 'span1' });
    throw new Error('Should have thrown');
  } catch (err) {
    assert(err.message.includes('operationName'));
  }
});

test('should end span with timing', () => {
  const tracer = new RequestTracer();
  tracer.startTrace('trace123');
  const span = tracer.addSpan('trace123', {
    spanId: 'span1',
    operationName: 'test-op'
  });
  const completed = tracer.endSpan('trace123', span.spanId, {
    status: 'success'
  });
  assert(completed.status === 'success');
  assert(completed.duration >= 0);
});

test('should complete trace', () => {
  const tracer = new RequestTracer();
  tracer.startTrace('trace123');
  const trace = tracer.completeTrace('trace123');
  assert(trace.duration >= 0);
  assert(tracer.getActiveTraces().length === 0);
});

test('should retrieve trace by ID', () => {
  const tracer = new RequestTracer();
  tracer.startTrace('trace123');
  const trace = tracer.getTrace('trace123');
  assert(trace !== null);
  assert(trace.traceId === 'trace123');
});

test('should return null for non-existent trace', () => {
  const tracer = new RequestTracer();
  const trace = tracer.getTrace('nonexistent');
  assert(trace === null);
});

test('should get active traces', () => {
  const tracer = new RequestTracer();
  tracer.startTrace('trace1');
  tracer.startTrace('trace2');
  const active = tracer.getActiveTraces();
  assert(active.length === 2);
});

test('should clear old traces', () => {
  const tracer = new RequestTracer();
  tracer.startTrace('trace1');
  tracer.startTrace('trace2');
  tracer.completeTrace('trace1');
  const cleared = tracer.clearOldTraces(0);
  assert(cleared >= 1);
});

test('should enforce rate limit', () => {
  const tracer = new RequestTracer({ maxTracesPerMinute: 1 });
  tracer.startTrace('trace1');
  try {
    tracer.startTrace('trace2');
    throw new Error('Should have thrown');
  } catch (err) {
    assert(err.message.includes('Rate limit'));
  }
});

// ============================================================================
// MetricsCollector Tests
// ============================================================================
console.log('\n📊 MetricsCollector Tests:');

test('should create MetricsCollector instance', () => {
  const collector = new MetricsCollector();
  assert(collector.counters instanceof Map);
  assert(collector.gauges instanceof Map);
  assert(collector.histograms instanceof Map);
});

test('should record counter metric', () => {
  const collector = new MetricsCollector();
  collector.recordCounter('requests.total', 1);
  const metrics = collector.getMetrics();
  assert(metrics.counters.length === 1);
  assert(metrics.counters[0].count === 1);
});

test('should increment counter by value', () => {
  const collector = new MetricsCollector();
  collector.recordCounter('requests.total', 5);
  collector.recordCounter('requests.total', 3);
  const metrics = collector.getMetrics();
  assert(metrics.counters[0].count === 8);
});

test('should throw on invalid counter value', () => {
  const collector = new MetricsCollector();
  try {
    collector.recordCounter('requests.total', 'invalid');
    throw new Error('Should have thrown');
  } catch (err) {
    assert(err.message.includes('numeric'));
  }
});

test('should record gauge metric', () => {
  const collector = new MetricsCollector();
  collector.recordGauge('memory.usage.mb', 256);
  const metrics = collector.getMetrics();
  assert(metrics.gauges.length === 1);
  assert(metrics.gauges[0].value === 256);
});

test('should track gauge min/max', () => {
  const collector = new MetricsCollector();
  collector.recordGauge('memory.usage.mb', 100);
  collector.recordGauge('memory.usage.mb', 200);
  collector.recordGauge('memory.usage.mb', 150);
  const metrics = collector.getMetrics();
  assert(metrics.gauges[0].min === 100);
  assert(metrics.gauges[0].max === 200);
});

test('should record histogram metric', () => {
  const collector = new MetricsCollector();
  collector.recordHistogram('request.latency.ms', 100);
  const metrics = collector.getMetrics();
  assert(metrics.histograms.length === 1);
  assert(metrics.histograms[0].count === 1);
});

test('should calculate histogram percentiles', () => {
  const collector = new MetricsCollector();
  for (let i = 1; i <= 100; i++) {
    collector.recordHistogram('latency.ms', i);
  }
  const metrics = collector.getMetrics();
  const h = metrics.histograms[0];
  assert(h.p50 > 0);
  assert(h.p95 > h.p50);
  assert(h.p99 > h.p95);
});

test('should group metrics by tags', () => {
  const collector = new MetricsCollector();
  collector.recordCounter('requests', 1, { endpoint: '/api/users' });
  collector.recordCounter('requests', 1, { endpoint: '/api/posts' });
  const metrics = collector.getMetrics();
  assert(metrics.counters.length === 2);
});

test('should reset metrics', () => {
  const collector = new MetricsCollector();
  collector.recordCounter('requests.total', 10);
  collector.resetMetrics();
  const metrics = collector.getMetrics();
  assert(metrics.counters.length === 0);
});

test('should enforce rate limit on metrics', () => {
  const collector = new MetricsCollector({ maxMetricsPerWindow: 2 });
  collector.recordCounter('metric1', 1);
  collector.recordCounter('metric2', 1);
  try {
    collector.recordCounter('metric3', 1);
    throw new Error('Should have thrown');
  } catch (err) {
    assert(err.message.includes('Rate limit'));
  }
});

// ============================================================================
// EventLogger Tests
// ============================================================================
console.log('\n📝 EventLogger Tests:');

test('should create EventLogger instance', () => {
  const logger = new EventLogger();
  assert(logger.events instanceof Array);
  assert(logger.levels.length === 5);
});

test('should log event with message', () => {
  const logger = new EventLogger();
  logger.log('INFO', 'Test message');
  assert(logger.getPendingCount() === 1);
  logger.stop();
});

test('should log with different levels', () => {
  const logger = new EventLogger();
  logger.debug('Debug message');
  logger.info('Info message');
  logger.warn('Warn message');
  logger.error('Error message');
  logger.fatal('Fatal message');
  assert(logger.getPendingCount() === 5);
  logger.stop();
});

test('should throw on invalid log level', () => {
  const logger = new EventLogger();
  try {
    logger.log('INVALID', 'message');
    throw new Error('Should have thrown');
  } catch (err) {
    assert(err.message.includes('Invalid level'));
  }
  logger.stop();
});

test('should throw on missing message', () => {
  const logger = new EventLogger();
  try {
    logger.log('INFO', '');
    throw new Error('Should have thrown');
  } catch (err) {
    assert(err.message.includes('required'));
  }
  logger.stop();
});

test('should flush events', () => {
  let flushed = [];
  const logger = new EventLogger({
    onBatchReady: (events) => { flushed.push(...events); }
  });
  logger.info('Message 1');
  logger.info('Message 2');
  const batch = logger.flush();
  assert(batch.length === 2);
  assert(flushed.length === 2);
  logger.stop();
});

test('should auto-flush on batch size', (done) => {
  let callCount = 0;
  const logger = new EventLogger({
    batchSize: 3,
    onBatchReady: () => { callCount++; }
  });
  logger.info('Message 1');
  logger.info('Message 2');
  logger.info('Message 3');
  setTimeout(() => {
    assert(callCount >= 1);
    logger.stop();
  }, 100);
});

test('should include metadata in events', () => {
  const logger = new EventLogger();
  logger.info('Test', { userId: 'user123', traceId: 'trace456' });
  const batch = logger.flush();
  assert(batch[0].metadata.userId === 'user123');
  assert(batch[0].metadata.traceId === 'trace456');
  logger.stop();
});

test('should include timestamp in events', () => {
  const logger = new EventLogger();
  const before = Date.now();
  logger.info('Test');
  const after = Date.now();
  const batch = logger.flush();
  assert(batch[0].timestamp >= before);
  assert(batch[0].timestamp <= after);
  logger.stop();
});

// ============================================================================
// CorrelationManager Tests
// ============================================================================
console.log('\n🔗 CorrelationManager Tests:');

test('should create CorrelationManager instance', () => {
  const manager = new CorrelationManager();
  assert(manager.headerName === 'x-correlation-id');
  assert(manager.contextStack instanceof Array);
});

test('should create correlation context', () => {
  const manager = new CorrelationManager();
  const ctx = manager.createContext('corr123');
  assert(ctx.correlationId === 'corr123');
  assert(ctx.breadcrumbs instanceof Array);
});

test('should auto-generate correlation ID', () => {
  const manager = new CorrelationManager();
  const ctx = manager.createContext();
  assert(ctx.correlationId !== undefined);
  assert(ctx.correlationId !== '');
});

test('should push and pop context', () => {
  const manager = new CorrelationManager();
  const ctx = manager.createContext('corr123');
  manager.pushContext(ctx);
  assert(manager.getCurrentContext() === ctx);
  const popped = manager.popContext();
  assert(popped === ctx);
});

test('should get current context', () => {
  const manager = new CorrelationManager();
  const ctx = manager.createContext('corr123');
  manager.pushContext(ctx);
  assert(manager.getCurrentContext().correlationId === 'corr123');
});

test('should add breadcrumbs to context', () => {
  const manager = new CorrelationManager();
  const ctx = manager.createContext('corr123');
  manager.pushContext(ctx);
  manager.addBreadcrumb('action1', { data: 'value' });
  manager.addBreadcrumb('action2', { data: 'value' });
  assert(ctx.breadcrumbs.length === 2);
});

test('should extract correlation ID from headers', () => {
  const manager = new CorrelationManager({ headerName: 'x-trace-id' });
  const headers = { 'x-trace-id': 'corr123' };
  const id = manager.extractFromHeaders(headers);
  assert(id === 'corr123');
});

test('should format headers with correlation ID', () => {
  const manager = new CorrelationManager();
  const headers = manager.formatHeaders('corr123', { 'content-type': 'json' });
  assert(headers['x-correlation-id'] === 'corr123');
  assert(headers['content-type'] === 'json');
});

test('should retrieve context by ID', () => {
  const manager = new CorrelationManager();
  const ctx = manager.createContext('corr123');
  const retrieved = manager.getContext('corr123');
  assert(retrieved === ctx);
});

test('should clear old contexts', () => {
  const manager = new CorrelationManager();
  manager.createContext('corr1');
  manager.createContext('corr2');
  const cleared = manager.clearOldContexts(0);
  assert(cleared >= 2);
});

test('should handle context stack nesting', () => {
  const manager = new CorrelationManager();
  const ctx1 = manager.createContext('corr1');
  const ctx2 = manager.createContext('corr2');
  manager.pushContext(ctx1);
  manager.pushContext(ctx2);
  assert(manager.contextStack.length === 2);
  manager.popContext();
  assert(manager.getCurrentContext() === ctx1);
});

// ============================================================================
// Integration Tests
// ============================================================================
console.log('\n🔄 Integration Tests:');

test('should create complete telemetry system', () => {
  const telemetry = createTelemetrySystem({
    serviceName: 'test-service'
  });
  assert(telemetry.tracer !== undefined);
  assert(telemetry.metrics !== undefined);
  assert(telemetry.logger !== undefined);
  assert(telemetry.correlator !== undefined);
  telemetry.shutdown();
});

test('should integrate tracer with correlator', () => {
  const tracer = new RequestTracer();
  const correlator = new CorrelationManager();
  const ctx = correlator.createContext('corr123');
  const trace = tracer.startTrace('trace123', { userId: ctx.metadata.userId });
  assert(trace.traceId === 'trace123');
  assert(ctx.correlationId === 'corr123');
});

test('should integrate metrics with logger', () => {
  const metrics = new MetricsCollector();
  const logger = new EventLogger();
  metrics.recordCounter('events.logged', 1);
  logger.info('Metric recorded');
  assert(metrics.getMetrics().counters.length === 1);
  assert(logger.getPendingCount() === 1);
  logger.stop();
});

test('should handle concurrent operations', () => {
  const tracer = new RequestTracer();
  tracer.startTrace('trace1');
  tracer.startTrace('trace2');
  tracer.startTrace('trace3');
  assert(tracer.getActiveTraces().length === 3);
});

test('should maintain trace isolation', () => {
  const tracer = new RequestTracer();
  tracer.startTrace('trace1');
  tracer.startTrace('trace2');
  tracer.addSpan('trace1', { spanId: 'span1', operationName: 'op1' });
  const trace1 = tracer.getTrace('trace1');
  const trace2 = tracer.getTrace('trace2');
  assert(trace1.spans.length === 1);
  assert(trace2.spans.length === 0);
});

// ============================================================================
// Performance Tests
// ============================================================================
console.log('\n⚡ Performance Tests:');

test('startTrace should be fast', () => {
  const tracer = new RequestTracer();
  const start = Date.now();
  for (let i = 0; i < 100; i++) {
    tracer.startTrace(`trace${i}`);
  }
  const duration = Date.now() - start;
  assert(duration < 100, `Too slow: ${duration}ms`);
});

test('recordCounter should be fast', () => {
  const collector = new MetricsCollector();
  const start = Date.now();
  for (let i = 0; i < 1000; i++) {
    collector.recordCounter('test', 1);
  }
  const duration = Date.now() - start;
  assert(duration < 500, `Too slow: ${duration}ms`);
});

test('log should be fast', () => {
  const logger = new EventLogger();
  const start = Date.now();
  for (let i = 0; i < 1000; i++) {
    logger.info(`Message ${i}`);
  }
  const duration = Date.now() - start;
  assert(duration < 500, `Too slow: ${duration}ms`);
  logger.stop();
});

// ============================================================================
// Test Summary
// ============================================================================
console.log('\n╔════════════════════════════════════════════════════════════╗');
console.log(`║              Test Results: ${passCount}/${testCount} passed${' '.repeat(32 - passCount.toString().length - testCount.toString().length)}║`);
console.log(`║              ${failCount > 0 ? `${failCount} failed` : '✓ All tests passed'}${' '.repeat(50 - (failCount > 0 ? `${failCount} failed`.length : '✓ All tests passed'.length))}║`);
console.log('╚════════════════════════════════════════════════════════════╝\n');

process.exit(failCount > 0 ? 1 : 0);
