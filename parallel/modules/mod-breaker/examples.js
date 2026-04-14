/**
 * @fileoverview Real-world usage examples for mod-breaker
 * Demonstrates circuit breaker patterns for fault tolerance
 * 
 * Scenarios covered:
 * - API service protection
 * - Database connection pooling
 * - Cascading failure prevention
 * - Metrics monitoring
 * - Recovery strategies
 */

const { CircuitBreaker, CircuitState } = require('./index');

// ============================================================================
// EXAMPLE 1: Protecting API Service Calls
// ============================================================================
console.log('\n=== EXAMPLE 1: API Service Protection ===\n');

const apiBreaker = new CircuitBreaker({
  name: 'api-service',
  failureThreshold: 5,      // Open after 5 failures
  successThreshold: 2,      // Close after 2 successes
  timeout: 5000,            // 5 second timeout per request
  recoveryStrategy: 'exponential',
  recoveryBaseDelay: 2000,  // Start with 2s, exponentially increase
  onOpen: (state) => {
    console.log(`🔴 Circuit OPENED - API Service Unavailable`);
    console.log(`   Metrics: ${state.metrics.currentFailures} failures`);
  },
  onClose: (state) => {
    console.log(`🟢 Circuit CLOSED - API Service Restored`);
  },
  onHalfOpen: (state) => {
    console.log(`🟡 Circuit HALF_OPEN - Testing Recovery`);
  },
  onError: (error, state) => {
    console.log(`⚠️  Error in ${state}: ${error.message}`);
  }
});

// Simulated async API call
async function callExternalAPI(endpoint) {
  return new Promise((resolve, reject) => {
    setTimeout(() => {
      if (Math.random() > 0.3) {
        resolve({ status: 200, data: { endpoint, result: 'success' } });
      } else {
        reject(new Error('API service temporarily unavailable'));
      }
    }, 100);
  });
}

// Usage pattern for API calls
(async function exampleAPIUsage() {
  for (let i = 0; i < 10; i++) {
    try {
      const result = await apiBreaker.execute(
        () => callExternalAPI('/api/users'),
        { attempt: i + 1 }
      );
      console.log(`✓ Request ${i + 1} succeeded`);
    } catch (error) {
      if (error.code === 'CIRCUIT_BREAKER_OPEN') {
        console.log(`✗ Request ${i + 1} rejected - Circuit open, retry after ${error.retryAfter}ms`);
      } else {
        console.log(`✗ Request ${i + 1} failed - ${error.message}`);
      }
    }
  }
  console.log('\nAPI Service Circuit State:', apiBreaker.getState());
})();

// ============================================================================
// EXAMPLE 2: Database Connection Protection
// ============================================================================
setTimeout(() => {
  console.log('\n=== EXAMPLE 2: Database Connection Protection ===\n');

  const dbBreaker = new CircuitBreaker({
    name: 'database-connection',
    failureThreshold: 3,
    timeout: 2000,
    recoveryStrategy: 'linear',    // Linear recovery strategy
    recoveryBaseDelay: 1000
  });

  // Simulated database query
  async function queryDatabase(sql) {
    return new Promise((resolve, reject) => {
      setTimeout(() => {
        if (Math.random() > 0.4) {
          resolve({ rows: [{ id: 1, name: 'User 1' }] });
        } else {
          reject(new Error('Connection timeout'));
        }
      }, 150);
    });
  }

  (async function exampleDBUsage() {
    const results = [];
    for (let i = 0; i < 5; i++) {
      try {
        const result = await dbBreaker.execute(
          () => queryDatabase('SELECT * FROM users'),
          { queryId: `q${i}` }
        );
        results.push(`Query ${i}: SUCCESS`);
      } catch (error) {
        results.push(`Query ${i}: FAILED (${error.message})`);
      }
    }
    results.forEach(r => console.log(r));
    console.log('\nDatabase Circuit Metrics:', dbBreaker.thresholdMonitor.getMetrics());
  })();
}, 1000);

// ============================================================================
// EXAMPLE 3: Cascading Failure Prevention
// ============================================================================
setTimeout(() => {
  console.log('\n=== EXAMPLE 3: Cascading Failure Prevention ===\n');

  const breaker1 = new CircuitBreaker({ name: 'service-1', failureThreshold: 2 });
  const breaker2 = new CircuitBreaker({ name: 'service-2', failureThreshold: 2 });
  const breaker3 = new CircuitBreaker({ name: 'service-3', failureThreshold: 2 });

  async function chainedServiceCall() {
    try {
      const res1 = await breaker1.execute(() => 
        Promise.reject(new Error('Service 1 failed'))
      );
      const res2 = await breaker2.execute(() => Promise.resolve({ data: res1 }));
      const res3 = await breaker3.execute(() => Promise.resolve({ data: res2 }));
      return res3;
    } catch (error) {
      console.log(`Cascade stopped: ${error.message}`);
      return null;
    }
  }

  (async function exampleCascade() {
    for (let i = 0; i < 5; i++) {
      await chainedServiceCall();
    }
    console.log('\nCircuit States:');
    console.log(`  Service 1: ${breaker1.state}`);
    console.log(`  Service 2: ${breaker2.state}`);
    console.log(`  Service 3: ${breaker3.state}`);
  })();
}, 2000);

// ============================================================================
// EXAMPLE 4: Manual Circuit Control
// ============================================================================
setTimeout(() => {
  console.log('\n=== EXAMPLE 4: Manual Circuit Control ===\n');

  const breaker = new CircuitBreaker({
    name: 'maintenance-service',
    failureThreshold: 10
  });

  console.log(`Initial state: ${breaker.state}`);

  // Simulate maintenance: manually open circuit
  const openState = breaker.open();
  console.log(`After manual open: ${openState.state}`);
  console.log(`Last state change: ${new Date(openState.statistics.lastStateChange).toISOString()}`);

  // Simulate maintenance complete: manually close circuit
  const closeState = breaker.close();
  console.log(`After manual close: ${closeState.state}`);
  console.log(`Statistics reset: Success count = ${closeState.statistics.successCount}`);

  // Reset for next cycle
  breaker.reset();
  console.log(`After reset: ${JSON.stringify(breaker.getState().statistics)}`);
}, 3000);

// ============================================================================
// EXAMPLE 5: Threshold Configuration for Different Scenarios
// ============================================================================
setTimeout(() => {
  console.log('\n=== EXAMPLE 5: Threshold Configuration ===\n');

  // Strict threshold - for critical services
  const criticalBreaker = new CircuitBreaker({
    name: 'payment-service',
    failureThreshold: 1,        // Open immediately on first failure
    successThreshold: 5,        // Require 5 successes to close
    timeout: 1000
  });

  // Lenient threshold - for non-critical services
  const lenientBreaker = new CircuitBreaker({
    name: 'cache-service',
    failureThreshold: 10,       // Allow 10 failures
    successThreshold: 2,        // Quick recovery
    timeout: 10000
  });

  console.log('Critical Service Config:');
  console.log(`  Failure Threshold: ${criticalBreaker.thresholdMonitor.failureThreshold}`);
  console.log(`  Success Threshold: ${criticalBreaker.thresholdMonitor.successThreshold}`);

  console.log('\nLenient Service Config:');
  console.log(`  Failure Threshold: ${lenientBreaker.thresholdMonitor.failureThreshold}`);
  console.log(`  Success Threshold: ${lenientBreaker.thresholdMonitor.successThreshold}`);
}, 4000);

// ============================================================================
// EXAMPLE 6: Recovery Strategies
// ============================================================================
setTimeout(() => {
  console.log('\n=== EXAMPLE 6: Recovery Strategies ===\n');

  const exponentialBreaker = new CircuitBreaker({
    name: 'exp-recovery',
    recoveryStrategy: 'exponential',
    recoveryBaseDelay: 100,
    failureThreshold: 1
  });

  const linearBreaker = new CircuitBreaker({
    name: 'linear-recovery',
    recoveryStrategy: 'linear',
    recoveryBaseDelay: 100,
    failureThreshold: 1
  });

  (async function exampleRecovery() {
    // Open exponential breaker
    try {
      await exponentialBreaker.execute(() => Promise.reject(new Error('Fail')));
    } catch (e) { /* expected */ }
    exponentialBreaker.open();

    // Open linear breaker
    try {
      await linearBreaker.execute(() => Promise.reject(new Error('Fail')));
    } catch (e) { /* expected */ }
    linearBreaker.open();

    console.log('Recovery Strategy Comparison:');
    const expState = exponentialBreaker.getState();
    const linState = linearBreaker.getState();

    console.log(`\nExponential: Next retry in ${expState.recovery.nextRetryIn}ms`);
    console.log(`Linear:      Next retry in ${linState.recovery.nextRetryIn}ms`);
  })();
}, 5000);

// ============================================================================
// EXAMPLE 7: Metrics and Monitoring
// ============================================================================
setTimeout(() => {
  console.log('\n=== EXAMPLE 7: Metrics and Monitoring ===\n');

  const monitoredBreaker = new CircuitBreaker({
    name: 'monitored-service',
    failureThreshold: 5
  });

  (async function exampleMonitoring() {
    // Generate some traffic
    for (let i = 0; i < 8; i++) {
      try {
        await monitoredBreaker.execute(
          () => Math.random() > 0.6 ? Promise.resolve() : Promise.reject(new Error('Error'))
        );
      } catch (e) { /* expected */ }
    }

    // Get metrics
    const state = monitoredBreaker.getState();
    console.log('Service Metrics:');
    console.log(`  State: ${state.state}`);
    console.log(`  Total Executions: ${state.statistics.executionCount}`);
    console.log(`  Successes: ${state.statistics.successCount}`);
    console.log(`  Failures: ${state.statistics.failureCount}`);
    console.log(`  Failure Rate: ${state.metrics.failureRate.toFixed(2)}%`);
    console.log(`  Recovery Attempts: ${state.recovery.attempts}`);
  })();
}, 6000);

console.log('✓ All examples scheduled');
