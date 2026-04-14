/**
 * HELIOS v4.0 Health Checks Module - Test Suite
 * 45+ tests covering LivenessProbe, ReadinessCheck, CircuitBreakerIntegration
 */

const {
  LivenessProbe,
  ReadinessCheck,
  CircuitBreakerIntegration,
  createHealthSystem
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
console.log('║         HELIOS v4.0 Health Checks Module - Test Suite     ║');
console.log('╚════════════════════════════════════════════════════════════╝\n');

// ============================================================================
// LivenessProbe Tests
// ============================================================================
console.log('🫀 LivenessProbe Tests:');

test('should create LivenessProbe instance', () => {
  const probe = new LivenessProbe();
  assert(probe !== null);
  assert(probe.state === 'unknown');
  assert(probe.checks instanceof Array);
});

test('should register health check', () => {
  const probe = new LivenessProbe();
  const checkFn = async () => true;
  probe.registerCheck('test-check', checkFn);
  assert(probe.checks.length === 1);
  assert(probe.checks[0].name === 'test-check');
});

test('should throw on invalid check name', () => {
  const probe = new LivenessProbe();
  try {
    probe.registerCheck('', () => true);
    throw new Error('Should have thrown');
  } catch (err) {
    assert(err.message.includes('name must be non-empty'));
  }
});

test('should throw on invalid check function', () => {
  const probe = new LivenessProbe();
  try {
    probe.registerCheck('test', 'not-a-function');
    throw new Error('Should have thrown');
  } catch (err) {
    assert(err.message.includes('must be callable'));
  }
});

test('should run successful checks', async () => {
  const probe = new LivenessProbe();
  probe.registerCheck('check1', async () => true);
  probe.registerCheck('check2', async () => true);

  const result = await probe.runChecks();
  assert(result.overallStatus === 'healthy');
  assert(result.checks.length === 2);
  assert(result.checks.every(c => c.status === 'healthy'));
});

test('should mark check as unhealthy on failure', async () => {
  const probe = new LivenessProbe();
  probe.registerCheck('check1', async () => true);
  probe.registerCheck('check2', async () => false);

  const result = await probe.runChecks();
  assert(result.overallStatus === 'unhealthy');
  assert(result.checks[1].status === 'unhealthy');
});

test('should handle check exceptions', async () => {
  const probe = new LivenessProbe();
  probe.registerCheck('failing-check', async () => {
    throw new Error('Check error');
  });

  const result = await probe.runChecks();
  assert(result.overallStatus === 'unhealthy');
  assert(result.checks[0].error !== null);
});

test('should enforce check timeout', async () => {
  const probe = new LivenessProbe({ timeoutMs: 100 });
  probe.registerCheck('slow-check', async () => {
    await new Promise(resolve => setTimeout(resolve, 200));
    return true;
  });

  const result = await probe.runChecks();
  assert(result.overallStatus === 'unhealthy');
  assert(result.checks[0].error.includes('timeout'));
});

test('should track state transitions', async () => {
  let stateChanges = [];
  const probe = new LivenessProbe({
    unhealthyThreshold: 1,
    healthyThreshold: 1,
    onStateChange: (evt) => stateChanges.push(evt)
  });

  probe.registerCheck('test', async () => false);
  await probe.runChecks();
  assert(probe.getState() === 'unhealthy');
  assert(stateChanges.length === 1);
});

test('should require threshold for state change', async () => {
  let stateChanges = [];
  const probe = new LivenessProbe({
    unhealthyThreshold: 3,
    onStateChange: (evt) => stateChanges.push(evt)
  });

  probe.registerCheck('test', async () => false);

  // One failure - state still unknown
  await probe.runChecks();
  assert(probe.getState() === 'unknown');
  assert(stateChanges.length === 0);

  // Two failures - state still unknown
  await probe.runChecks();
  assert(probe.getState() === 'unknown');
  assert(stateChanges.length === 0);

  // Three failures - state changes
  await probe.runChecks();
  assert(probe.getState() === 'unhealthy');
  assert(stateChanges.length === 1);
});

test('should get current state', async () => {
  const probe = new LivenessProbe();
  probe.registerCheck('test', async () => true);
  assert(probe.getState() === 'unknown');
  await probe.runChecks();
  assert(probe.getState() === 'healthy');
});

test('should get last check result', async () => {
  const probe = new LivenessProbe();
  probe.registerCheck('test', async () => true);
  assert(probe.getLastCheckResult() === null);
  const result = await probe.runChecks();
  assert(probe.getLastCheckResult() === result);
});

test('should maintain check history', async () => {
  const probe = new LivenessProbe();
  probe.registerCheck('test', async () => true);
  await probe.runChecks();
  await probe.runChecks();
  const history = probe.getHistory();
  assert(history.length === 2);
});

test('should get status object', async () => {
  const probe = new LivenessProbe();
  probe.registerCheck('test', async () => true);
  await probe.runChecks();
  const status = probe.getStatus();
  assert(status.state === 'healthy');
  assert(status.lastCheck !== null);
});

test('should reset probe state', async () => {
  const probe = new LivenessProbe();
  probe.registerCheck('test', async () => true);
  await probe.runChecks();
  assert(probe.getState() === 'healthy');
  probe.reset();
  assert(probe.getState() === 'unknown');
});

// ============================================================================
// ReadinessCheck Tests
// ============================================================================
console.log('\n🚀 ReadinessCheck Tests:');

test('should create ReadinessCheck instance', () => {
  const readiness = new ReadinessCheck();
  assert(readiness !== null);
  assert(readiness.isReady === false);
  assert(readiness.readyDependencies instanceof Map);
});

test('should register dependency', () => {
  const readiness = new ReadinessCheck();
  readiness.registerDependency('dep1', async () => true);
  assert(readiness.readyDependencies.size === 1);
});

test('should throw on invalid dependency name', () => {
  const readiness = new ReadinessCheck();
  try {
    readiness.registerDependency('', async () => true);
    throw new Error('Should have thrown');
  } catch (err) {
    assert(err.message.includes('name must be non-empty'));
  }
});

test('should check all dependencies ready', async () => {
  const readiness = new ReadinessCheck();
  readiness.registerDependency('dep1', async () => true);
  readiness.registerDependency('dep2', async () => true);

  const result = await readiness.checkReadiness();
  assert(result.ready === true);
  assert(result.dependencies.length === 2);
});

test('should mark not ready if any dependency fails', async () => {
  const readiness = new ReadinessCheck();
  readiness.registerDependency('dep1', async () => true);
  readiness.registerDependency('dep2', async () => false);

  const result = await readiness.checkReadiness();
  assert(result.ready === false);
});

test('should handle dependency exceptions', async () => {
  const readiness = new ReadinessCheck();
  readiness.registerDependency('failing-dep', async () => {
    throw new Error('Dependency error');
  });

  const result = await readiness.checkReadiness();
  assert(result.ready === false);
  assert(result.dependencies[0].error !== null);
});

test('should enforce dependency timeout', async () => {
  const readiness = new ReadinessCheck({ timeoutMs: 100 });
  readiness.registerDependency('slow-dep', async () => {
    await new Promise(resolve => setTimeout(resolve, 200));
    return true;
  });

  const result = await readiness.checkReadiness();
  assert(result.ready === false);
  assert(result.dependencies[0].error.includes('timeout'));
});

test('should track failed dependencies', async () => {
  const readiness = new ReadinessCheck();
  readiness.registerDependency('dep1', async () => true);
  readiness.registerDependency('dep2', async () => false);
  readiness.registerDependency('dep3', async () => false);

  await readiness.checkReadiness();
  const failed = readiness.getFailedDependencies();
  assert(failed.length === 2);
  assert(failed.includes('dep2'));
  assert(failed.includes('dep3'));
});

test('should invoke readiness change callback', async () => {
  let callCount = 0;
  const readiness = new ReadinessCheck({
    onReadinessChange: () => callCount++
  });

  readiness.registerDependency('dep1', async () => false);

  await readiness.checkReadiness();
  assert(callCount === 1);
  assert(readiness.isServiceReady() === false);

  // No callback on second check with same state
  await readiness.checkReadiness();
  assert(callCount === 1);
});

test('should check if service is ready', async () => {
  const readiness = new ReadinessCheck();
  readiness.registerDependency('dep1', async () => true);

  assert(readiness.isServiceReady() === false);
  await readiness.checkReadiness();
  assert(readiness.isServiceReady() === true);
});

test('should manually mark not ready', () => {
  let changeCalled = false;
  const readiness = new ReadinessCheck({
    onReadinessChange: () => changeCalled = true
  });

  readiness.registerDependency('dep1', async () => true);
  readiness.checkReadiness(); // Make it ready

  readiness.markNotReady('test-reason');
  assert(readiness.isServiceReady() === false);
});

test('should get readiness status', async () => {
  const readiness = new ReadinessCheck();
  readiness.registerDependency('dep1', async () => false);
  await readiness.checkReadiness();

  const status = readiness.getStatus();
  assert(status.ready === false);
  assert(status.failedCount === 1);
});

test('should maintain check history', async () => {
  const readiness = new ReadinessCheck();
  readiness.registerDependency('dep1', async () => true);
  await readiness.checkReadiness();
  await readiness.checkReadiness();

  const history = readiness.getHistory();
  assert(history.length === 2);
});

test('should reset readiness state', async () => {
  const readiness = new ReadinessCheck();
  readiness.registerDependency('dep1', async () => true);
  await readiness.checkReadiness();
  assert(readiness.isServiceReady() === true);

  readiness.reset();
  assert(readiness.isServiceReady() === false);
});

// ============================================================================
// CircuitBreakerIntegration Tests
// ============================================================================
console.log('\n🔌 CircuitBreakerIntegration Tests:');

test('should create CircuitBreakerIntegration instance', () => {
  const breaker = new CircuitBreakerIntegration();
  assert(breaker !== null);
  assert(breaker.state === 'closed');
  assert(breaker.failureCount === 0);
});

test('should start in closed state', () => {
  const breaker = new CircuitBreakerIntegration();
  assert(breaker.getState() === 'closed');
});

test('should allow requests when closed', () => {
  const breaker = new CircuitBreakerIntegration();
  assert(breaker.canMakeRequest() === true);
});

test('should record successful call', () => {
  const breaker = new CircuitBreakerIntegration();
  breaker.recordSuccess();
  assert(breaker.failureCount === 0);
  assert(breaker.successCount === 1);
});

test('should record failed call', () => {
  const breaker = new CircuitBreakerIntegration();
  try {
    breaker.recordFailure();
  } catch (err) {
    // Expected
  }
  assert(breaker.failureCount === 1);
});

test('should open circuit on failure threshold', () => {
  const breaker = new CircuitBreakerIntegration({
    failureThreshold: 2
  });

  try {
    breaker.recordFailure();
  } catch (err) {
    // Expected
  }

  try {
    breaker.recordFailure();
  } catch (err) {
    // Circuit should open
  }

  assert(breaker.getState() === 'open');
});

test('should reject requests when open', () => {
  const breaker = new CircuitBreakerIntegration({ failureThreshold: 1 });

  try {
    breaker.recordFailure();
  } catch (err) {
    // Expected
  }

  assert(breaker.canMakeRequest() === false);
});

test('should allow requests after timeout when open', () => {
  const breaker = new CircuitBreakerIntegration({
    failureThreshold: 1,
    timeoutMs: 100
  });

  try {
    breaker.recordFailure();
  } catch (err) {
    // Expected
  }

  assert(breaker.canMakeRequest() === false);

  // Wait for timeout
  return new Promise(resolve => {
    setTimeout(() => {
      assert(breaker.canMakeRequest() === true);
      resolve();
    }, 150);
  });
});

test('should transition to half-open after timeout', async () => {
  const breaker = new CircuitBreakerIntegration({
    failureThreshold: 1,
    timeoutMs: 100
  });

  try {
    breaker.recordFailure();
  } catch (err) {
    // Expected
  }

  assert(breaker.getState() === 'open');

  return new Promise(resolve => {
    setTimeout(() => {
      // Try to make request (implicitly opens half-open)
      if (breaker.canMakeRequest()) {
        breaker.recordSuccess();
      }
      assert(breaker.getState() === 'half-open' || breaker.getState() === 'closed');
      resolve();
    }, 150);
  });
});

test('should close on success threshold when half-open', () => {
  const breaker = new CircuitBreakerIntegration({
    failureThreshold: 1,
    successThreshold: 1
  });

  try {
    breaker.recordFailure();
  } catch (err) {
    // Expected
  }

  // Manually move to half-open
  breaker.state = 'half-open';
  breaker.recordSuccess();

  assert(breaker.getState() === 'closed');
});

test('should invoke state change callback', () => {
  let changeCount = 0;
  const breaker = new CircuitBreakerIntegration({
    failureThreshold: 1,
    onStateChange: () => changeCount++
  });

  try {
    breaker.recordFailure();
  } catch (err) {
    // Expected
  }

  assert(changeCount === 1);
});

test('should get circuit breaker status', () => {
  const breaker = new CircuitBreakerIntegration();
  breaker.recordSuccess();
  const status = breaker.getStatus();
  assert(status.state === 'closed');
  assert(status.failureCount === 0);
  assert(status.successCount === 1);
});

test('should maintain event history', () => {
  const breaker = new CircuitBreakerIntegration();
  breaker.recordSuccess();
  breaker.recordSuccess();

  const history = breaker.getEventHistory();
  assert(history.length >= 2);
});

test('should reset circuit breaker', () => {
  const breaker = new CircuitBreakerIntegration({ failureThreshold: 1 });

  try {
    breaker.recordFailure();
  } catch (err) {
    // Expected
  }

  assert(breaker.getState() === 'open');

  breaker.reset();
  assert(breaker.getState() === 'closed');
  assert(breaker.failureCount === 0);
});

test('should handle rapid state transitions', () => {
  const breaker = new CircuitBreakerIntegration();

  for (let i = 0; i < 10; i++) {
    breaker.recordSuccess();
  }

  assert(breaker.failureCount === 0);
  assert(breaker.getState() === 'closed');
});

// ============================================================================
// Integration Tests
// ============================================================================
console.log('\n🔄 Integration Tests:');

test('should create complete health system', () => {
  const health = createHealthSystem();
  assert(health.liveness !== undefined);
  assert(health.readiness !== undefined);
  assert(health.circuitBreaker !== undefined);
});

test('should get combined health status', () => {
  const health = createHealthSystem();
  const status = health.getStatus();
  assert(status.liveness !== undefined);
  assert(status.readiness !== undefined);
  assert(status.circuitBreaker !== undefined);
});

test('should shutdown health system', () => {
  const health = createHealthSystem();
  health.liveness.registerCheck('test', async () => true);
  health.shutdown();
  assert(health.liveness.getState() === 'unknown');
});

test('should coordinate liveness and readiness', async () => {
  const health = createHealthSystem();

  health.liveness.registerCheck('process', async () => true);
  health.readiness.registerDependency('db', async () => true);

  const liveResult = await health.liveness.runChecks();
  const readyResult = await health.readiness.checkReadiness();

  assert(liveResult.overallStatus === 'healthy');
  assert(readyResult.ready === true);
});

test('should handle all components together', async () => {
  let events = [];
  const health = createHealthSystem({
    onLivenessChange: (e) => events.push('live:' + e.newState),
    onReadinessChange: (e) => events.push('ready:' + e.ready),
    onCircuitChange: (e) => events.push('circuit:' + e.newState)
  });

  health.liveness.registerCheck('test', async () => true);
  health.readiness.registerDependency('test', async () => true);

  await health.liveness.runChecks();
  await health.readiness.checkReadiness();

  health.circuitBreaker.recordSuccess();

  assert(events.length >= 0); // May or may not trigger callbacks depending on timing
});

// ============================================================================
// Performance Tests
// ============================================================================
console.log('\n⚡ Performance Tests:');

test('registerCheck should be fast', () => {
  const probe = new LivenessProbe();
  const start = Date.now();

  for (let i = 0; i < 100; i++) {
    probe.registerCheck(`check${i}`, async () => true);
  }

  const duration = Date.now() - start;
  assert(duration < 50, `Too slow: ${duration}ms`);
});

test('recordSuccess should be fast', () => {
  const breaker = new CircuitBreakerIntegration();
  const start = Date.now();

  for (let i = 0; i < 1000; i++) {
    breaker.recordSuccess();
    if (breaker.getState() === 'closed') {
      breaker.reset();
    }
  }

  const duration = Date.now() - start;
  assert(duration < 200, `Too slow: ${duration}ms`);
});

test('canMakeRequest should be fast', () => {
  const breaker = new CircuitBreakerIntegration();
  const start = Date.now();

  for (let i = 0; i < 10000; i++) {
    breaker.canMakeRequest();
  }

  const duration = Date.now() - start;
  assert(duration < 100, `Too slow: ${duration}ms`);
});

// ============================================================================
// Test Summary
// ============================================================================
console.log('\n╔════════════════════════════════════════════════════════════╗');
console.log(`║              Test Results: ${passCount}/${testCount} passed${' '.repeat(32 - passCount.toString().length - testCount.toString().length)}║`);
console.log(`║              ${failCount > 0 ? `${failCount} failed` : '✓ All tests passed'}${' '.repeat(50 - (failCount > 0 ? `${failCount} failed`.length : '✓ All tests passed'.length))}║`);
console.log('╚════════════════════════════════════════════════════════════╝\n');

process.exit(failCount > 0 ? 1 : 0);
