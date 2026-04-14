# HELIOS v4.0 Telemetry Module

Request tracing, metrics collection, event logging, and correlation ID management for distributed systems.

## Features

- **Request Tracing**: Distributed tracing with context propagation
- **Metrics Collection**: Counters, gauges, histograms with aggregation
- **Event Logging**: Structured logging with batching
- **Correlation Management**: Correlation ID propagation across services

## Installation

```javascript
const { 
  RequestTracer, 
  MetricsCollector, 
  EventLogger, 
  CorrelationManager,
  createTelemetrySystem 
} = require('./index');
```

## API Documentation

### RequestTracer

Distributed request tracing with automatic span management.

**Performance**: <1ms overhead per trace operation

```javascript
const tracer = new RequestTracer({
  serviceName: 'api-gateway',
  samplingRate: 0.1, // 10% sampling
  onTraceComplete: (trace) => console.log(trace)
});

// Start a trace
const { traceId, spanId } = tracer.startTrace('trace123', {
  userId: 'user456',
  requestId: 'req789'
});

// Add spans
const span = tracer.addSpan(traceId, {
  spanId: spanId,
  operationName: 'database-query',
  tags: { database: 'postgres' }
});

// End span
tracer.endSpan(traceId, spanId, { 
  status: 'success', 
  data: { rows: 42 } 
});

// Complete trace
tracer.completeTrace(traceId, { 
  status: 'success' 
});
```

**Methods**:
- `startTrace(traceId, context)` - Begin trace
- `addSpan(traceId, spanData)` - Add span to trace
- `endSpan(traceId, spanId, result)` - Mark span complete
- `completeTrace(traceId, result)` - Finish trace
- `getTrace(traceId)` - Retrieve trace
- `getActiveTraces()` - List active traces
- `clearOldTraces(olderThanMs)` - Cleanup

### MetricsCollector

Aggregate metrics across multiple dimensions.

**Performance**: <0.5ms per metric operation

```javascript
const metrics = new MetricsCollector({
  aggregationWindowMs: 60000,
  onMetricReady: (agg) => console.log(agg)
});

// Counter metric
metrics.recordCounter('requests.total', 1, { 
  endpoint: '/api/users',
  method: 'POST'
});

// Gauge metric
metrics.recordGauge('memory.usage.mb', 256, { 
  instance: 'server-1' 
});

// Histogram metric
metrics.recordHistogram('http.request.duration.ms', 142, {
  endpoint: '/api/users',
  status: '200'
});

// Get aggregated metrics
const report = metrics.getMetrics();
```

**Methods**:
- `recordCounter(name, value, tags)` - Increment counter
- `recordGauge(name, value, tags)` - Set gauge
- `recordHistogram(name, value, tags)` - Record histogram
- `getMetrics()` - Get aggregated metrics
- `resetMetrics()` - Clear metrics

### EventLogger

Structured event logging with batch processing.

**Performance**: <0.3ms per event

```javascript
const logger = new EventLogger({
  batchSize: 100,
  flushIntervalMs: 5000,
  onBatchReady: (events) => sendToBackend(events)
});

logger.info('User login', {
  userId: 'user123',
  traceId: 'trace456'
});

logger.error('Database connection failed', {
  database: 'postgres',
  error: 'ECONNREFUSED'
});

// Manual flush
const batch = logger.flush();

// Graceful shutdown
logger.stop();
```

**Methods**:
- `log(level, message, metadata)` - Log event
- `debug(message, metadata)` - Debug level
- `info(message, metadata)` - Info level
- `warn(message, metadata)` - Warn level
- `error(message, metadata)` - Error level
- `fatal(message, metadata)` - Fatal level
- `flush()` - Flush pending events
- `getPendingCount()` - Queue length
- `stop()` - Stop logging

### CorrelationManager

Manage correlation context across distributed systems.

**Performance**: <0.1ms per operation

```javascript
const correlator = new CorrelationManager({
  headerName: 'x-correlation-id'
});

// Create context
const ctx = correlator.createContext('corr123', {
  userId: 'user456'
});

// Push to stack
correlator.pushContext(ctx);

// Add breadcrumbs
correlator.addBreadcrumb('called-auth-service', {
  latencyMs: 45
});

// Current context
const current = correlator.getCurrentContext();

// Extract from HTTP headers
const id = correlator.extractFromHeaders(req.headers);

// Format outgoing headers
const headers = correlator.formatHeaders('corr123', {
  'content-type': 'application/json'
});
```

**Methods**:
- `createContext(correlationId, metadata)` - Create context
- `pushContext(context)` - Push to stack
- `popContext()` - Pop from stack
- `getCurrentContext()` - Get current
- `addBreadcrumb(action, data)` - Add breadcrumb
- `extractFromHeaders(headers)` - Extract ID
- `formatHeaders(id, existing)` - Format headers
- `getContext(id)` - Get by ID
- `clearOldContexts(olderThanMs)` - Cleanup

## Real-World Examples

### Example 1: Request Tracing with Express

```javascript
const express = require('express');
const { createTelemetrySystem } = require('./index');

const app = express();
const telemetry = createTelemetrySystem({
  serviceName: 'api-gateway',
  samplingRate: 0.1
});

app.use((req, res, next) => {
  const traceId = req.headers['x-trace-id'] || `trace_${Date.now()}`;
  const { spanId } = telemetry.tracer.startTrace(traceId, {
    requestId: req.id,
    userId: req.user?.id
  });

  res.on('finish', () => {
    telemetry.tracer.completeTrace(traceId, {
      status: res.statusCode < 400 ? 'success' : 'error'
    });
  });

  next();
});

app.get('/api/users/:id', (req, res) => {
  const trace = telemetry.tracer.getTrace(req.traceId);
  const span = telemetry.tracer.addSpan(req.traceId, {
    spanId: `span_${Date.now()}`,
    operationName: 'fetch-user',
    tags: { userId: req.params.id }
  });

  telemetry.metrics.recordCounter('users.fetch', 1, {
    userId: req.params.id
  });

  res.json({ id: req.params.id, name: 'John' });
  
  telemetry.tracer.endSpan(req.traceId, span.spanId, {
    status: 'success'
  });
});
```

### Example 2: Metrics Aggregation

```javascript
const { MetricsCollector } = require('./index');

const metrics = new MetricsCollector({
  aggregationWindowMs: 60000,
  onMetricReady: (agg) => {
    console.log('Metrics Report:');
    console.log('Counters:', agg.counters);
    console.log('Gauges:', agg.gauges);
    agg.histograms.forEach(h => {
      console.log(`${h.name}: p50=${h.p50}ms, p95=${h.p95}ms, p99=${h.p99}ms`);
    });
  }
});

// Track request metrics
setInterval(() => {
  metrics.recordCounter('http.requests', Math.floor(Math.random() * 100), {
    endpoint: '/api/users',
    method: 'GET'
  });

  metrics.recordHistogram('http.request.duration.ms', Math.random() * 500, {
    endpoint: '/api/users',
    status: '200'
  });

  metrics.recordGauge('system.cpu.usage', Math.random() * 100, {
    host: 'server-1'
  });
}, 100);
```

### Example 3: Distributed Tracing

```javascript
const http = require('http');
const { createTelemetrySystem } = require('./index');

const telemetry = createTelemetrySystem({
  serviceName: 'service-a'
});

// Service A makes request to Service B
async function callServiceB(correlationId) {
  const context = telemetry.correlator.createContext(correlationId);
  telemetry.correlator.pushContext(context);

  const { traceId, spanId } = telemetry.tracer.startTrace(
    `trace_${correlationId}`,
    { userId: 'user123' }
  );

  return new Promise((resolve, reject) => {
    const options = {
      hostname: 'service-b.local',
      path: '/api/data',
      headers: telemetry.correlator.formatHeaders(correlationId, {
        'x-trace-id': traceId,
        'x-span-id': spanId
      })
    };

    const req = http.request(options, (res) => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        telemetry.tracer.endSpan(traceId, spanId, {
          status: 'success'
        });
        resolve(JSON.parse(data));
      });
    });

    req.on('error', (err) => {
      telemetry.tracer.endSpan(traceId, spanId, {
        status: 'error',
        data: err.message
      });
      reject(err);
    });

    req.end();
  });
}
```

## Performance Characteristics

| Operation | Latency | Notes |
|-----------|---------|-------|
| startTrace | <0.2ms | O(1) hash map insertion |
| addSpan | <0.3ms | Array push operation |
| endSpan | <0.1ms | Hash map lookup + update |
| recordCounter | <0.1ms | Hash map operation |
| recordGauge | <0.3ms | Hash map + array operations |
| recordHistogram | <0.4ms | Array push + math operations |
| log | <0.1ms | Array push |
| createContext | <0.2ms | Object creation + map insert |

## Configuration

### RequestTracer Options
- `serviceName` (string): Service identifier
- `samplingRate` (0-1): Trace sampling percentage
- `maxTracesPerMinute` (number): Rate limit
- `onTraceComplete` (function): Completion callback

### MetricsCollector Options
- `aggregationWindowMs` (number): Window size
- `maxMetricsPerWindow` (number): Rate limit
- `onMetricReady` (function): Window flush callback

### EventLogger Options
- `batchSize` (number): Events per batch
- `flushIntervalMs` (number): Auto-flush interval
- `onBatchReady` (function): Batch callback

### CorrelationManager Options
- `headerName` (string): HTTP header name
- `idGenerator` (function): Custom ID generator

## Best Practices

1. **Rate Limiting**: Use sampling for high-volume systems
2. **Cleanup**: Call `clearOldTraces()` and `clearOldContexts()` periodically
3. **Batching**: Process event batches efficiently
4. **Context Propagation**: Always include correlation IDs in cross-service calls
5. **Error Handling**: Log spans with error status for debugging

## Error Handling

All classes validate inputs and throw descriptive errors:

```javascript
try {
  tracer.startTrace('', {}); // Throws: traceId must be non-empty
} catch (err) {
  console.error(err.message);
}
```

## License

HELIOS v4.0
