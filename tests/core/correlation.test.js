/**
 * Correlation Tests - Comprehensive test suite for request correlation
 * Tests correlation IDs, trace propagation, lifecycle tracking, and logging
 * @module tests/core/correlation.test
 */

const {
  CorrelationIDGenerator,
  TraceIDPropagation,
  RequestLifecycleTracker,
  DistributedTracingHooks,
  LogCorrelation,
} = require('../../src/core/correlation');

describe('CorrelationIDGenerator', () => {
  let generator;

  beforeEach(() => {
    generator = new CorrelationIDGenerator();
  });

  test('should generate unique correlation IDs', () => {
    const id1 = generator.generate();
    const id2 = generator.generate();

    expect(id1).toBeDefined();
    expect(id2).toBeDefined();
    expect(id1).not.toBe(id2);
  });

  test('should generate IDs with correct format', () => {
    const id = generator.generate();

    expect(id).toMatch(/^corr-[a-z0-9]+-[a-f0-9]+-[a-z0-9]+$/);
  });

  test('should validate correlation ID format', () => {
    const id = generator.generate();

    expect(generator.validate(id)).toBe(true);
    expect(generator.validate('invalid-format')).toBe(false);
  });

  test('should parse correlation ID components', () => {
    const id = generator.generate();
    const parsed = generator.parse(id);

    expect(parsed).toBeDefined();
    expect(parsed.prefix).toBe('corr');
    expect(parsed.timestamp).toBeDefined();
    expect(parsed.random).toBeDefined();
    expect(parsed.counter).toBeDefined();
  });

  test('should extract timestamp from ID', () => {
    const id = generator.generate();
    const parsed = generator.parse(id);
    const now = Date.now();

    expect(parsed.timestamp).toBeLessThanOrEqual(now);
    expect(parsed.timestamp).toBeGreaterThan(now - 10000);
  });

  test('should increment counter', () => {
    const id1 = generator.generate();
    const id2 = generator.generate();

    const parsed1 = generator.parse(id1);
    const parsed2 = generator.parse(id2);

    expect(parsed2.counter).toBeGreaterThan(parsed1.counter);
  });
});

describe('TraceIDPropagation', () => {
  let propagation;

  beforeEach(() => {
    propagation = new TraceIDPropagation();
  });

  test('should create trace context', () => {
    const context = propagation.createContext('corr-123', { userId: 'user-1' });

    expect(context.correlationId).toBe('corr-123');
    expect(context.traceId).toBeDefined();
    expect(context.spanId).toBeDefined();
    expect(context.parentSpanId).toBeNull();
  });

  test('should create child spans', () => {
    const parentContext = propagation.createContext('corr-123');
    const childContext = propagation.createChildSpan('corr-123', 'ai-service');

    expect(childContext.traceId).toBe(parentContext.traceId);
    expect(childContext.spanId).not.toBe(parentContext.spanId);
    expect(childContext.parentSpanId).toBe(parentContext.spanId);
    expect(childContext.service).toBe('ai-service');
  });

  test('should retrieve trace context', () => {
    const context = propagation.createContext('corr-123');
    const retrieved = propagation.getContext('corr-123');

    expect(retrieved).toEqual(context);
  });

  test('should extract trace headers', () => {
    const context = propagation.createContext('corr-123');
    const headers = propagation.extractHeaders('corr-123');

    expect(headers['x-correlation-id']).toBe('corr-123');
    expect(headers['x-trace-id']).toBe(context.traceId);
    expect(headers['x-span-id']).toBe(context.spanId);
  });

  test('should inject headers from incoming request', () => {
    const headers = {
      'x-correlation-id': 'corr-456',
      'x-trace-id': 'trace-789',
      'x-span-id': 'span-012',
      'x-parent-span-id': 'parent-345',
    };

    const injected = propagation.injectHeaders(headers);

    expect(injected.correlationId).toBe('corr-456');
    expect(injected.traceId).toBe('trace-789');
    expect(injected.spanId).toBe('span-012');
    expect(injected.parentSpanId).toBe('parent-345');
  });

  test('should clean up old trace contexts', () => {
    propagation.createContext('corr-1');
    propagation.createContext('corr-2');

    const cleaned = propagation.cleanup(0); // Clean all

    expect(cleaned).toBe(2);
    expect(propagation.traceBag.size).toBe(0);
  });

  test('should not clean up recent contexts', () => {
    propagation.createContext('corr-1');
    propagation.createContext('corr-2');

    const cleaned = propagation.cleanup(360000000); // Very long TTL

    expect(cleaned).toBe(0);
    expect(propagation.traceBag.size).toBe(2);
  });
});

describe('RequestLifecycleTracker', () => {
  let tracker;

  beforeEach(() => {
    tracker = new RequestLifecycleTracker();
  });

  test('should start tracking request', () => {
    const lifecycle = tracker.startTracking('corr-123');

    expect(lifecycle.correlationId).toBe('corr-123');
    expect(lifecycle.stages.length).toBe(1);
    expect(lifecycle.stages[0].stage).toBe(RequestLifecycleTracker.STAGES.RECEIVED);
  });

  test('should mark lifecycle stages', () => {
    tracker.startTracking('corr-123');
    tracker.markStage('corr-123', RequestLifecycleTracker.STAGES.VALIDATED);
    tracker.markStage('corr-123', RequestLifecycleTracker.STAGES.PROCESSING);

    const lifecycle = tracker.getLifecycle('corr-123');

    expect(lifecycle.stages.length).toBe(3);
    expect(lifecycle.stages[1].stage).toBe(RequestLifecycleTracker.STAGES.VALIDATED);
    expect(lifecycle.stages[2].stage).toBe(RequestLifecycleTracker.STAGES.PROCESSING);
  });

  test('should record errors in lifecycle', () => {
    tracker.startTracking('corr-123');
    const error = new Error('Test error');

    tracker.recordError('corr-123', error);

    const lifecycle = tracker.getLifecycle('corr-123');

    expect(lifecycle.errors.length).toBe(1);
    expect(lifecycle.errors[0].message).toBe('Test error');
  });

  test('should calculate request duration', () => {
    tracker.startTracking('corr-123');
    tracker.markStage('corr-123', RequestLifecycleTracker.STAGES.VALIDATED);

    const lifecycle = tracker.completeTracking('corr-123');

    expect(lifecycle.duration).toBeGreaterThanOrEqual(0);
  });

  test('should get request statistics', () => {
    tracker.startTracking('corr-1');
    tracker.startTracking('corr-2');
    tracker.startTracking('corr-3');

    const stats = tracker.getStats();

    expect(stats.total).toBe(3);
    expect(stats.avgDuration).toBeDefined();
    expect(stats.maxDuration).toBeDefined();
    expect(stats.minDuration).toBeDefined();
  });

  test('should clean up old requests', () => {
    tracker.startTracking('corr-1');
    tracker.startTracking('corr-2');

    const cleaned = tracker.cleanup(0); // Clean all

    expect(cleaned).toBeGreaterThan(0);
  });
});

describe('DistributedTracingHooks', () => {
  let hooks;

  beforeEach(() => {
    hooks = new DistributedTracingHooks();
  });

  test('should enable and disable tracing', () => {
    expect(hooks.tracingEnabled).toBe(false);

    hooks.enable();
    expect(hooks.tracingEnabled).toBe(true);

    hooks.disable();
    expect(hooks.tracingEnabled).toBe(false);
  });

  test('should register hooks', () => {
    const callback = jest.fn();

    hooks.registerHook('span:start', callback);

    expect(hooks.hooks.has('span:start')).toBe(true);
  });

  test('should execute hooks', async () => {
    const callback = jest.fn();

    hooks.enable();
    hooks.registerHook('span:start', callback);

    await hooks.executeHooks('span:start', { operationName: 'test' });

    expect(callback).toHaveBeenCalled();
  });

  test('should not execute hooks when disabled', async () => {
    const callback = jest.fn();

    hooks.registerHook('span:start', callback);

    await hooks.executeHooks('span:start', { operationName: 'test' });

    expect(callback).not.toHaveBeenCalled();
  });

  test('should create spans', async () => {
    hooks.enable();

    const startCallback = jest.fn();
    const endCallback = jest.fn();

    hooks.registerHook('span:start', startCallback);
    hooks.registerHook('span:end', endCallback);

    const result = await hooks.createSpan('test-operation', async () => 'result', 'corr-123');

    expect(result).toBe('result');
    expect(startCallback).toHaveBeenCalled();
    expect(endCallback).toHaveBeenCalled();
  });

  test('should handle span errors', async () => {
    hooks.enable();

    const endCallback = jest.fn();
    hooks.registerHook('span:end', endCallback);

    try {
      await hooks.createSpan('test-operation', async () => {
        throw new Error('Operation failed');
      }, 'corr-123');
    } catch (error) {
      // Expected
    }

    expect(endCallback).toHaveBeenCalled();
    const call = endCallback.mock.calls[0][0];
    expect(call.status).toBe('error');
  });
});

describe('LogCorrelation', () => {
  let logCorrelation;

  beforeEach(() => {
    logCorrelation = new LogCorrelation();
  });

  test('should correlate logs with request', () => {
    logCorrelation.correlateLog('corr-123', {
      level: 'info',
      message: 'Test log',
    });

    const logs = logCorrelation.getLogsForCorrelation('corr-123');

    expect(logs.length).toBe(1);
    expect(logs[0].message).toBe('Test log');
    expect(logs[0].correlationId).toBe('corr-123');
  });

  test('should maintain log buffer size', () => {
    logCorrelation.maxBufferSize = 5;

    for (let i = 0; i < 10; i++) {
      logCorrelation.correlateLog('corr-123', {
        level: 'info',
        message: `Log ${i}`,
      });
    }

    expect(logCorrelation.logBuffer.length).toBe(5);
  });

  test('should export logs for analysis', () => {
    logCorrelation.correlateLog('corr-123', { level: 'info', message: 'Log 1' });
    logCorrelation.correlateLog('corr-123', { level: 'info', message: 'Log 2' });

    const exported = logCorrelation.exportLogs('corr-123');

    expect(exported.correlationId).toBe('corr-123');
    expect(exported.logCount).toBe(2);
    expect(exported.duration).toBeGreaterThanOrEqual(0);
  });

  test('should clear correlation logs', () => {
    logCorrelation.correlateLog('corr-123', { level: 'info', message: 'Log' });
    logCorrelation.correlateLog('corr-124', { level: 'info', message: 'Log' });

    logCorrelation.clearCorrelation('corr-123');

    expect(logCorrelation.getLogsForCorrelation('corr-123').length).toBe(0);
    expect(logCorrelation.getLogsForCorrelation('corr-124').length).toBe(1);
  });

  test('should create middleware', () => {
    const middleware = logCorrelation.middleware('x-correlation-id');

    expect(typeof middleware).toBe('function');
  });
});
