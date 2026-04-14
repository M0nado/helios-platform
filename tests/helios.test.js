/**
 * HELIOS V4.0 - Comprehensive Test Suite
 * All tests consolidated into single file for simplicity
 */

const {
  HELIOSV4,
  DatabaseOptimizer,
  GatewayOptimizer,
  CacheStrategy,
  PerformanceMonitor,
  AIOrchestrator,
} = require('./helios-core');

// ============================================================================
// TEST FRAMEWORK HELPERS
// ============================================================================

class TestRunner {
  constructor() {
    this.tests = [];
    this.passed = 0;
    this.failed = 0;
    this.skipped = 0;
  }

  test(name, fn) {
    this.tests.push({ name, fn, type: 'test' });
  }

  describe(name, fn) {
    console.log(`\n📋 ${name}`);
    fn();
  }

  async run() {
    console.log('\n🚀 Running HELIOS V4.0 Test Suite\n');
    console.log('='.repeat(60));

    for (const test of this.tests) {
      try {
        await test.fn();
        this.passed++;
        console.log(`✅ PASS: ${test.name}`);
      } catch (error) {
        this.failed++;
        console.log(`❌ FAIL: ${test.name}`);
        console.log(`   Error: ${error.message}`);
      }
    }

    console.log('='.repeat(60));
    this.printSummary();
  }

  printSummary() {
    const total = this.passed + this.failed + this.skipped;
    console.log(`\n📊 Test Results:`);
    console.log(`   Total: ${total}`);
    console.log(`   ✅ Passed: ${this.passed}`);
    console.log(`   ❌ Failed: ${this.failed}`);
    console.log(`   ⏭️  Skipped: ${this.skipped}`);
    console.log(`   Success Rate: ${((this.passed / total) * 100).toFixed(2)}%\n`);
  }
}

function assertEquals(actual, expected, message) {
  if (JSON.stringify(actual) !== JSON.stringify(expected)) {
    throw new Error(`${message}\nExpected: ${expected}\nActual: ${actual}`);
  }
}

function assertGreaterThan(actual, expected, message) {
  if (actual <= expected) {
    throw new Error(`${message}\nExpected greater than: ${expected}\nActual: ${actual}`);
  }
}

function assertLessThan(actual, expected, message) {
  if (actual >= expected) {
    throw new Error(`${message}\nExpected less than: ${expected}\nActual: ${actual}`);
  }
}

// ============================================================================
// TEST SUITE
// ============================================================================

const runner = new TestRunner();

// DATABASE TESTS
runner.describe('Database Optimizer Tests', () => {
  runner.test('Should initialize database optimizer', () => {
    const db = new DatabaseOptimizer();
    assertEquals(db.stats.queries, 0, 'Initial query count should be 0');
  });

  runner.test('Should execute and cache queries', async () => {
    const db = new DatabaseOptimizer();
    const result = await db.query('SELECT * FROM users', []);
    assertEquals(result.optimized, true, 'Query should be optimized');
  });

  runner.test('Should track cache hits and misses', async () => {
    const db = new DatabaseOptimizer();
    await db.query('SELECT * FROM users', []);
    await db.query('SELECT * FROM users', []); // Same query - should hit cache
    assertEquals(db.stats.cacheHits, 1, 'Should have 1 cache hit');
    assertEquals(db.stats.cacheMisses, 1, 'Should have 1 cache miss');
  });

  runner.test('Should select optimal indices', () => {
    const db = new DatabaseOptimizer();
    const idx1 = db.selectOptimalIndex('SELECT * FROM users WHERE user_id = 1');
    const idx2 = db.selectOptimalIndex('SELECT * FROM events WHERE timestamp > NOW()');
    assertEquals(idx1, 'idx_user_id', 'Should select user_id index');
    assertEquals(idx2, 'idx_timestamp', 'Should select timestamp index');
  });

  runner.test('Database stats should include cache hit rate', async () => {
    const db = new DatabaseOptimizer();
    await db.query('SELECT * FROM users', []);
    const stats = db.getStats();
    assertEquals(typeof stats.cacheHitRate, 'string', 'Cache hit rate should be string');
  });
});

// GATEWAY TESTS
runner.describe('Gateway Optimizer Tests', () => {
  runner.test('Should initialize gateway optimizer', () => {
    const gw = new GatewayOptimizer();
    assertEquals(gw.stats.requests, 0, 'Initial request count should be 0');
  });

  runner.test('Should optimize response with compression', async () => {
    const gw = new GatewayOptimizer();
    const data = { users: [...Array(100)].map((_, i) => ({ id: i, name: `User ${i}` })) };
    const result = await gw.optimizeResponse(data);
    assertEquals(typeof result.metadata.compressionRatio, 'string', 'Should have compression ratio');
  });

  runner.test('Should paginate data correctly', () => {
    const gw = new GatewayOptimizer();
    const data = [...Array(250)].map((_, i) => ({ id: i }));
    const paginated = gw.paginate(data, 1, 50);
    assertEquals(paginated.items.length, 50, 'Should return 50 items per page');
    assertEquals(paginated.pagination.totalPages, 5, 'Should have 5 total pages');
  });

  runner.test('Should track response statistics', async () => {
    const gw = new GatewayOptimizer();
    const data = { test: 'data' };
    await gw.optimizeResponse(data);
    const stats = gw.getStats();
    assertEquals(stats.responses, 1, 'Should have 1 response');
  });
});

// CACHE TESTS
runner.describe('Cache Strategy Tests', () => {
  runner.test('Should initialize cache', () => {
    const cache = new CacheStrategy();
    assertEquals(cache.activeStrategy, 'lru', 'Default strategy should be LRU');
  });

  runner.test('Should cache and retrieve values', () => {
    const cache = new CacheStrategy();
    cache.set('key1', 'value1');
    const result = cache.get('key1');
    assertEquals(result, 'value1', 'Should retrieve cached value');
  });

  runner.test('Should track cache hits', () => {
    const cache = new CacheStrategy();
    cache.set('key1', 'value1');
    cache.get('key1');
    cache.get('key1');
    const stats = cache.getStats();
    assertEquals(stats.hits, 2, 'Should have 2 cache hits');
  });

  runner.test('Should switch cache strategies', () => {
    const cache = new CacheStrategy();
    cache.switchStrategy('lfu');
    assertEquals(cache.activeStrategy, 'lfu', 'Should switch to LFU');
  });

  runner.test('Should calculate hit rate', () => {
    const cache = new CacheStrategy();
    cache.set('key', 'value');
    cache.get('key'); // hit
    cache.get('missing'); // miss
    const stats = cache.getStats();
    assertEquals(typeof stats.hitRate, 'string', 'Hit rate should be formatted string');
  });
});

// MONITORING TESTS
runner.describe('Performance Monitor Tests', () => {
  runner.test('Should initialize monitor', () => {
    const monitor = new PerformanceMonitor();
    assertEquals(monitor.metrics.requests, 0, 'Initial requests should be 0');
  });

  runner.test('Should record metrics', () => {
    const monitor = new PerformanceMonitor();
    monitor.recordMetric('request', 1);
    monitor.recordMetric('response', 1);
    assertEquals(monitor.metrics.requests, 1, 'Should record 1 request');
    assertEquals(monitor.metrics.responses, 1, 'Should record 1 response');
  });

  runner.test('Should calculate latency percentiles', () => {
    const monitor = new PerformanceMonitor();
    const latencies = [10, 20, 30, 40, 50, 100, 150, 200, 250, 500];
    latencies.forEach(l => monitor.recordMetric('latency', l));
    const metrics = monitor.getMetrics();
    assertGreaterThan(metrics.p99Latency, 0, 'P99 latency should be calculated');
  });

  runner.test('Should create dashboards', () => {
    const monitor = new PerformanceMonitor();
    const dashboard = monitor.createDashboard('test-dashboard');
    assertEquals(dashboard.name, 'test-dashboard', 'Dashboard name should match');
  });

  runner.test('Should track error rate', () => {
    const monitor = new PerformanceMonitor();
    monitor.recordMetric('request', 1);
    monitor.recordMetric('error', 1);
    const metrics = monitor.getMetrics();
    assertEquals(typeof metrics.errorRate, 'string', 'Error rate should be string');
  });
});

// AI ORCHESTRATOR TESTS
runner.describe('AI Orchestrator Tests', () => {
  runner.test('Should initialize orchestrator', () => {
    const ai = new AIOrchestrator();
    assertEquals(ai.tasks.length, 0, 'Should start with no tasks');
  });

  runner.test('Should schedule tasks', async () => {
    const ai = new AIOrchestrator();
    const task = {
      name: 'test',
      cpu: 10,
      memory: 50,
      execute: async () => 'result',
    };
    const result = await ai.scheduleTask(task);
    assertEquals(result.status, 'completed', 'Task should complete');
  });

  runner.test('Should manage resources', async () => {
    const ai = new AIOrchestrator({ cpuLimit: 100, memoryLimit: 1000 });
    const task = {
      cpu: 50,
      memory: 500,
      execute: async () => 'result',
    };
    const canAllocate = ai.canAllocate(task);
    assertEquals(canAllocate, true, 'Should allocate resources');
  });

  runner.test('Should report status', () => {
    const ai = new AIOrchestrator();
    const status = ai.getStatus();
    assertEquals(status.totalTasks, 0, 'Should report total tasks');
  });
});

// HELIOS INTEGRATION TESTS
runner.describe('HELIOS V4 Integration Tests', () => {
  runner.test('Should initialize HELIOS', async () => {
    const helios = new HELIOSV4();
    await helios.initialize();
    assertEquals(helios.version, '4.0', 'Version should be 4.0');
  });

  runner.test('Should execute queries through main system', async () => {
    const helios = new HELIOSV4();
    await helios.initialize();
    const result = await helios.query('SELECT * FROM users', []);
    assertEquals(result.optimized, true, 'Query should be optimized');
  });

  runner.test('Should optimize responses', async () => {
    const helios = new HELIOSV4();
    const data = { test: 'data' };
    const result = await helios.optimizeResponse(data);
    assertEquals(typeof result.metadata, 'object', 'Should return metadata');
  });

  runner.test('Should manage cache', () => {
    const helios = new HELIOSV4();
    helios.cacheSet('key', 'value');
    const result = helios.cacheGet('key');
    assertEquals(result, 'value', 'Should cache and retrieve values');
  });

  runner.test('Should provide system status', async () => {
    const helios = new HELIOSV4();
    await helios.initialize();
    const status = helios.getStatus();
    assertEquals(status.version, '4.0', 'Status should include version');
    assertEquals(typeof status.uptime, 'number', 'Status should include uptime');
  });

  runner.test('Should create dashboards', () => {
    const helios = new HELIOSV4();
    const dashboard = helios.createDashboard('main');
    assertEquals(dashboard.name, 'main', 'Dashboard should be created');
  });
});

// ============================================================================
// PERFORMANCE BENCHMARKS
// ============================================================================

async function runBenchmarks() {
  console.log('\n\n🏃 HELIOS V4.0 Performance Benchmarks\n');
  console.log('='.repeat(60));

  const helios = new HELIOSV4();
  await helios.initialize();

  // Database benchmark
  console.log('\n📊 Database Query Performance:');
  const dbStart = performance.now();
  for (let i = 0; i < 1000; i++) {
    await helios.query(`SELECT * FROM users WHERE id = ${i}`, []);
  }
  const dbTime = performance.now() - dbStart;
  console.log(`   1000 queries: ${dbTime.toFixed(2)}ms (${(dbTime / 1000).toFixed(2)}ms/query)`);

  // Cache benchmark
  console.log('\n📊 Cache Performance:');
  const cacheStart = performance.now();
  for (let i = 0; i < 10000; i++) {
    helios.cacheSet(`key${i % 100}`, `value${i}`);
    helios.cacheGet(`key${i % 100}`);
  }
  const cacheTime = performance.now() - cacheStart;
  console.log(`   10000 ops: ${cacheTime.toFixed(2)}ms (${(cacheTime / 10000).toFixed(2)}ms/op)`);

  // Gateway benchmark
  console.log('\n📊 Gateway Response Performance:');
  const gwStart = performance.now();
  for (let i = 0; i < 100; i++) {
    const data = { items: [...Array(100)].map((_, j) => ({ id: j, data: `value${j}` })) };
    await helios.optimizeResponse(data);
  }
  const gwTime = performance.now() - gwStart;
  console.log(`   100 responses: ${gwTime.toFixed(2)}ms (${(gwTime / 100).toFixed(2)}ms/response)`);

  // Final metrics
  console.log('\n📊 Final System Metrics:');
  const metrics = helios.getMetrics();
  console.log(`   Avg Latency: ${metrics.avgLatency.toFixed(2)}ms`);
  console.log(`   P99 Latency: ${metrics.p99Latency.toFixed(2)}ms`);
  console.log(`   Success Rate: ${metrics.successRate}`);
  console.log(`   Error Rate: ${metrics.errorRate}`);

  console.log('\n' + '='.repeat(60) + '\n');
}

// ============================================================================
// MAIN EXECUTION
// ============================================================================

async function main() {
  // Run tests
  await runner.run();

  // Run benchmarks
  await runBenchmarks();

  // Exit with status
  process.exit(runner.failed > 0 ? 1 : 0);
}

// Run if executed directly
if (require.main === module) {
  main().catch(console.error);
}

module.exports = { TestRunner, runner };
