/**
 * HELIOS v4.0 Health Checks Module - Real-World Examples
 * @module feat-health/examples
 */

const {
  LivenessProbe,
  ReadinessCheck,
  CircuitBreakerIntegration,
  createHealthSystem
} = require('./index');

/**
 * Example 1: Liveness Probes
 * Demonstrates monitoring if service is still running
 */
async function example1_LivenessProbes() {
  console.log('\n=== Example 1: Liveness Probes ===\n');

  const probe = new LivenessProbe({
    unhealthyThreshold: 3,
    healthyThreshold: 2,
    onStateChange: (event) => {
      console.log(`🫀 State changed: ${event.oldState} → ${event.newState}`);
    }
  });

  // Register basic checks
  probe.registerCheck('process-alive', async () => {
    return process.uptime() > 0;
  });

  probe.registerCheck('memory-usage', async () => {
    const usage = process.memoryUsage();
    const limit = 500 * 1024 * 1024; // 500MB
    const healthy = usage.heapUsed < limit;
    console.log(`  Memory: ${Math.round(usage.heapUsed / 1024 / 1024)}MB / ${Math.round(limit / 1024 / 1024)}MB`);
    return healthy;
  });

  probe.registerCheck('event-loop-responsive', async () => {
    const start = Date.now();
    await new Promise(resolve => setImmediate(resolve));
    const latency = Date.now() - start;
    const responsive = latency < 50;
    console.log(`  Event loop latency: ${latency}ms`);
    return responsive;
  });

  // Run checks multiple times
  for (let i = 1; i <= 3; i++) {
    console.log(`\nCheck run ${i}:`);
    const result = await probe.runChecks();
    console.log(`  Overall: ${result.overallStatus}`);
    console.log(`  Duration: ${result.duration}ms`);
  }

  console.log(`\nFinal state: ${probe.getState()}`);
  console.log(`Status:`, probe.getStatus());
}

/**
 * Example 2: Readiness Checks
 * Demonstrates verifying dependencies before accepting traffic
 */
async function example2_ReadinessChecks() {
  console.log('\n=== Example 2: Readiness Checks ===\n');

  let dbReady = false;
  let cacheReady = false;

  const readiness = new ReadinessCheck({
    timeoutMs: 5000,
    onReadinessChange: (event) => {
      console.log(`🚀 Readiness changed: ${event.ready}`);
      if (!event.ready) {
        console.log(`  Failed: ${event.failedDependencies.join(', ')}`);
      }
    }
  });

  // Register dependencies
  readiness.registerDependency('database', async () => {
    console.log('  Checking database...');
    // Simulate database connection
    if (!dbReady) {
      console.log('    Database not ready yet');
      return false;
    }
    console.log('    Database OK');
    return true;
  });

  readiness.registerDependency('cache', async () => {
    console.log('  Checking cache...');
    // Simulate cache connection
    if (!cacheReady) {
      console.log('    Cache not ready yet');
      return false;
    }
    console.log('    Cache OK');
    return true;
  });

  readiness.registerDependency('message-queue', async () => {
    console.log('  Checking message queue...');
    console.log('    Message queue OK');
    return true;
  });

  // Check 1: Not ready
  console.log('Check 1 (dependencies not ready):');
  let result = await readiness.checkReadiness();
  console.log(`Ready: ${result.ready}`);

  // Simulate dependencies coming online
  console.log('\nSimulating dependencies coming online...');
  dbReady = true;
  cacheReady = true;

  // Check 2: Ready
  console.log('\nCheck 2 (dependencies ready):');
  result = await readiness.checkReadiness();
  console.log(`Ready: ${result.ready}`);

  // Check 3: Dependency fails
  console.log('\nSimulating database failure...');
  dbReady = false;

  console.log('\nCheck 3 (database failed):');
  result = await readiness.checkReadiness();
  console.log(`Ready: ${result.ready}`);
  console.log(`Failed: ${readiness.getFailedDependencies().join(', ')}`);
}

/**
 * Example 3: Circuit Breaker Pattern
 * Demonstrates protecting against cascading failures
 */
async function example3_CircuitBreakerPattern() {
  console.log('\n=== Example 3: Circuit Breaker Pattern ===\n');

  const breaker = new CircuitBreakerIntegration({
    failureThreshold: 3,
    successThreshold: 2,
    timeoutMs: 5000,
    onStateChange: (event) => {
      console.log(`🔌 Circuit state: ${event.oldState} → ${event.newState}`);
    }
  });

  // Simulate external service calls
  let failureSimulation = false;

  async function callExternalService() {
    if (!breaker.canMakeRequest()) {
      console.log('❌ Request blocked: Circuit is OPEN');
      throw new Error('Circuit breaker OPEN');
    }

    try {
      if (failureSimulation) {
        throw new Error('Service error');
      }
      console.log('✓ Request succeeded');
      breaker.recordSuccess();
      return { success: true };
    } catch (error) {
      console.log('✗ Request failed');
      try {
        breaker.recordFailure();
      } catch (cbError) {
        console.log('⚠️  Circuit opened due to failures');
      }
      throw error;
    }
  }

  // Test successful calls
  console.log('Scenario 1: Successful calls');
  for (let i = 1; i <= 3; i++) {
    try {
      await callExternalService();
    } catch (err) {
      console.log(`  Error: ${err.message}`);
    }
  }

  // Test failing calls
  console.log('\nScenario 2: Service failures');
  failureSimulation = true;
  for (let i = 1; i <= 4; i++) {
    try {
      await callExternalService();
    } catch (err) {
      console.log(`  Error: ${err.message}`);
    }
  }

  console.log('\nCircuit breaker status:');
  console.log(`  State: ${breaker.getState()}`);
  console.log(`  Can make request: ${breaker.canMakeRequest()}`);

  // Test recovery
  console.log('\nScenario 3: Recovery (service comes back)');
  failureSimulation = false;

  for (let i = 1; i <= 3; i++) {
    try {
      await callExternalService();
    } catch (err) {
      console.log(`  Error: ${err.message}`);
    }
  }

  console.log('\nFinal circuit state:');
  console.log(`  State: ${breaker.getState()}`);
}

/**
 * Example 4: Health Endpoint Simulation
 * Demonstrates exposing health endpoints like Kubernetes expects
 */
async function example4_HealthEndpoints() {
  console.log('\n=== Example 4: Health Endpoints ===\n');

  const health = createHealthSystem();

  // Configure liveness (quick checks)
  health.liveness.registerCheck('process', async () => {
    return process.uptime() > 0;
  });

  health.liveness.registerCheck('memory', async () => {
    const usage = process.memoryUsage();
    return usage.heapUsed < 500 * 1024 * 1024;
  });

  // Configure readiness (comprehensive checks)
  health.readiness.registerDependency('database', async () => {
    console.log('  Checking database connection...');
    // In real scenario, query database
    return true;
  });

  health.readiness.registerDependency('cache', async () => {
    console.log('  Checking cache connection...');
    // In real scenario, ping cache
    return true;
  });

  // Simulate Kubernetes liveness probe
  console.log('GET /health/live');
  const liveResult = await health.liveness.runChecks();
  const liveStatus = liveResult.overallStatus === 'healthy' ? 200 : 503;
  console.log(`Response: ${liveStatus}`);
  console.log(`Body: ${JSON.stringify(liveResult, null, 2)}`);

  // Simulate Kubernetes readiness probe
  console.log('\nGET /health/ready');
  const readyResult = await health.readiness.checkReadiness();
  const readyStatus = readyResult.ready ? 200 : 503;
  console.log(`Response: ${readyStatus}`);
  console.log(`Body: ${JSON.stringify(readyResult, null, 2)}`);

  // Full health status
  console.log('\nGET /health');
  const fullStatus = health.getStatus();
  console.log(`Body: ${JSON.stringify(fullStatus, null, 2)}`);
}

/**
 * Example 5: Integrated Health System
 * Demonstrates all components working together
 */
async function example5_IntegratedHealthSystem() {
  console.log('\n=== Example 5: Integrated Health System ===\n');

  const health = createHealthSystem({
    onLivenessChange: (evt) => {
      console.log(`  🫀 Liveness: ${evt.oldState} → ${evt.newState}`);
    },
    onReadinessChange: (evt) => {
      console.log(`  🚀 Readiness: ${evt.ready}`);
    },
    onCircuitChange: (evt) => {
      console.log(`  🔌 Circuit: ${evt.oldState} → ${evt.newState}`);
    }
  });

  // Setup liveness
  health.liveness.registerCheck('process', async () => process.uptime() > 0);
  health.liveness.registerCheck('memory', async () => {
    const usage = process.memoryUsage();
    return usage.heapUsed < 500 * 1024 * 1024;
  });

  // Setup readiness
  health.readiness.registerDependency('primary-db', async () => true);
  health.readiness.registerDependency('cache-layer', async () => true);
  health.readiness.registerDependency('message-bus', async () => true);

  // Simulate application lifecycle
  console.log('Starting application...');

  // Phase 1: Starting up
  console.log('\nPhase 1: Startup checks');
  let liveResult = await health.liveness.runChecks();
  console.log(`Liveness: ${liveResult.overallStatus}`);

  let readyResult = await health.readiness.checkReadiness();
  console.log(`Readiness: ${readyResult.ready}`);

  // Phase 2: Running normally
  console.log('\nPhase 2: Normal operation');
  console.log(`Overall status: ${JSON.stringify(health.getStatus(), null, 2)}`);

  // Phase 3: Simulating failure
  console.log('\nPhase 3: Simulating failures');
  health.readiness.markNotReady('Database connection lost');
  console.log(`Service ready: ${health.readiness.isServiceReady()}`);

  // Phase 4: Recovery
  console.log('\nPhase 4: Recovery');
  health.readiness.reset();
  readyResult = await health.readiness.checkReadiness();
  console.log(`Service ready: ${health.readiness.isServiceReady()}`);

  // Cleanup
  health.shutdown();
  console.log('\nApplication shut down cleanly');
}

/**
 * Example 6: Advanced Dependency Checking
 * Demonstrates complex dependency patterns
 */
async function example6_AdvancedDependencies() {
  console.log('\n=== Example 6: Advanced Dependency Checking ===\n');

  const readiness = new ReadinessCheck({ timeoutMs: 5000 });

  // Database with failover
  readiness.registerDependency('database-primary', async () => {
    console.log('  Checking primary database...');
    return true; // Healthy
  });

  readiness.registerDependency('database-replica', async () => {
    console.log('  Checking replica database...');
    return true; // Healthy
  });

  // Cache cluster
  readiness.registerDependency('cache-node-1', async () => {
    console.log('  Checking cache node 1...');
    return true;
  });

  readiness.registerDependency('cache-node-2', async () => {
    console.log('  Checking cache node 2...');
    return true;
  });

  // External services
  readiness.registerDependency('auth-service', async () => {
    console.log('  Checking auth service...');
    return true;
  });

  readiness.registerDependency('payment-gateway', async () => {
    console.log('  Checking payment gateway...');
    return true;
  });

  // Run checks
  console.log('Checking all dependencies:\n');
  const result = await readiness.checkReadiness();

  // Display results
  console.log(`\nReadiness: ${result.ready}`);
  console.log(`Checks performed: ${result.dependencies.length}`);
  console.log(`Healthy: ${result.dependencies.filter(d => d.status === 'ready').length}`);
  console.log(`Failed: ${result.dependencies.filter(d => d.status === 'not-ready').length}`);

  // Performance summary
  const totalDuration = result.dependencies.reduce((sum, d) => sum + d.duration, 0);
  console.log(`Total duration: ${result.duration}ms`);
  console.log(`Sum of checks: ${totalDuration}ms`);
}

/**
 * Run all examples
 */
async function runAllExamples() {
  console.log('\n╔════════════════════════════════════════════════════════════╗');
  console.log('║      HELIOS v4.0 Health Checks Module - Real-World Examples ║');
  console.log('╚════════════════════════════════════════════════════════════╝');

  try {
    await example1_LivenessProbes();
    await new Promise(resolve => setTimeout(resolve, 100));

    await example2_ReadinessChecks();
    await new Promise(resolve => setTimeout(resolve, 100));

    await example3_CircuitBreakerPattern();
    await new Promise(resolve => setTimeout(resolve, 100));

    await example4_HealthEndpoints();
    await new Promise(resolve => setTimeout(resolve, 100));

    await example5_IntegratedHealthSystem();
    await new Promise(resolve => setTimeout(resolve, 100));

    await example6_AdvancedDependencies();

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
  example1_LivenessProbes,
  example2_ReadinessChecks,
  example3_CircuitBreakerPattern,
  example4_HealthEndpoints,
  example5_IntegratedHealthSystem,
  example6_AdvancedDependencies,
  runAllExamples
};

// Run if executed directly
if (require.main === module) {
  runAllExamples().catch(console.error);
}
