/**
 * Sample Application: Monado Blade Monitoring System Integration
 * Demonstrates how to integrate the monitoring system with providers
 */

import {
  CircuitBreaker,
  HealthCheckScheduler,
  FailoverController,
  MetricsCollector,
  GracefulDegradationEngine
} from './monitoring';
import {
  ProviderAdapter,
  HealthCheckResult,
  HealthStatus
} from './monitoring/types';

/**
 * Mock Provider for demonstration
 */
class MockProvider implements ProviderAdapter {
  private health: number = 1.0; // 0-1 scale

  constructor(
    public readonly providerId: string,
    private isHealthy: boolean = true
  ) {}

  async executeHealthCheck(): Promise<HealthCheckResult> {
    // Simulate check with occasional failures
    const shouldFail = Math.random() > this.health;

    if (shouldFail) {
      return {
        providerId: this.providerId,
        timestamp: Date.now(),
        status: HealthStatus.UNHEALTHY,
        latency: 0,
        errorRate: 1,
        successRate: 0,
        checksDone: 1,
        error: 'Connection timeout'
      };
    }

    const latency = Math.random() * 200 + 50;

    return {
      providerId: this.providerId,
      timestamp: Date.now(),
      status: HealthStatus.HEALTHY,
      latency,
      errorRate: 0,
      successRate: 1,
      checksDone: 1
    };
  }

  async isAvailable(): Promise<boolean> {
    return this.isHealthy;
  }

  setHealth(healthLevel: number): void {
    this.health = Math.max(0, Math.min(1, healthLevel));
  }
}

/**
 * Application demonstration
 */
async function main(): Promise<void> {
  console.log('=== Monado Blade Monitoring System ===\n');

  // Create mock providers
  const provider1 = new MockProvider('provider-1', true);
  const provider2 = new MockProvider('provider-2', true);
  const provider3 = new MockProvider('provider-3', true);
  const providers: ProviderAdapter[] = [provider1, provider2, provider3];

  // Initialize monitoring components
  const circuitBreakers = new Map<string, CircuitBreaker>();
  const scheduler = new HealthCheckScheduler({
    interval: 5000, // 5 seconds for demo
    adaptiveInterval: true,
    minInterval: 2000,
    maxInterval: 10000
  });

  const failoverController = new FailoverController({
    enabled: true,
    maxConcurrentFailovers: 2,
    failoverDelay: 500,
    cascadePreventionDelay: 2000
  });

  const metricsCollector = new MetricsCollector(60);
  const degradationEngine = new GracefulDegradationEngine({
    enabledAt: 0.5,
    timeoutMultiplier: 2,
    maxTimeoutMs: 10000
  });

  // Setup circuit breakers
  for (const provider of providers) {
    const cb = new CircuitBreaker(provider.providerId, {
      failureThreshold: 3,
      successThreshold: 2,
      timeout: 5000,
      initialBackoff: 1000,
      maxBackoff: 10000
    });

    circuitBreakers.set(provider.providerId, cb);
    scheduler.registerProvider(provider, cb);

    // Listen to state changes
    cb.onStateChange((change) => {
      console.log(
        `[CIRCUIT BREAKER] ${change.providerId}: ${change.oldState} -> ${change.newState}`
      );
      console.log(`  Reason: ${change.reason}\n`);
    });
  }

  // Register providers with failover controller
  failoverController.registerProviders(providers, circuitBreakers);

  // Setup event listeners
  scheduler.on('healthCheckResult', (result: HealthCheckResult) => {
    metricsCollector.recordHealthCheckResult(result);

    const statusStr = result.status === HealthStatus.HEALTHY ? '✓' : '✗';
    console.log(
      `[HEALTH CHECK] ${statusStr} ${result.providerId}: ` +
      `latency=${result.latency.toFixed(0)}ms, error_rate=${result.errorRate}`
    );

    // Check failover
    const health = metricsCollector.getProviderMetrics(result.providerId);
    if (health && failoverController.shouldTriggerFailover(health)) {
      console.log(`[FAILOVER] Triggered for ${result.providerId}\n`);
      const nextProvider = failoverController.getNextHealthyProvider(
        provider1,
        providers
      );
      if (nextProvider) {
        failoverController
          .executeFailover(
            provider1,
            nextProvider,
            `Provider failure: ${result.error}`
          )
          .catch(() => {});
      }
    }
  });

  scheduler.on('degradationDetected', (providerId: string, isDegrading: boolean) => {
    console.log(
      `[DEGRADATION] ${providerId}: ${isDegrading ? 'DEGRADING' : 'RECOVERED'}\n`
    );
  });

  failoverController.onFailover((event) => {
    console.log(
      `[FAILOVER EVENT] ${event.fromProviderId} -> ${event.toProviderId}`
    );
    console.log(`  Reason: ${event.reason}`);
    console.log(`  Success: ${event.success}\n`);
  });

  // Start health checks
  console.log('Starting health checks...\n');
  await scheduler.start(providers);

  // Simulate provider failures
  setTimeout(() => {
    console.log('--- Simulating provider 1 degradation ---\n');
    (provider1 as MockProvider).setHealth(0.4);
  }, 15000);

  setTimeout(() => {
    console.log('--- Simulating provider 1 recovery ---\n');
    (provider1 as MockProvider).setHealth(1.0);
  }, 30000);

  // Periodic metrics collection
  const metricsInterval = setInterval(() => {
    metricsCollector.recordSnapshot();

    const summary = metricsCollector.calculateSummary();
    console.log('[METRICS]');
    console.log(`  Healthy: ${summary.healthyProviders}/${providers.length}`);
    console.log(`  Availability: ${summary.systemAvailability.toFixed(1)}%`);
    console.log(`  Avg Latency: ${summary.averageLatency.toFixed(0)}ms`);
    console.log(`  Avg Error Rate: ${(summary.averageErrorRate * 100).toFixed(1)}%`);

    // Update degradation engine
    degradationEngine.evaluateSystemHealth(
      summary.healthyProviders,
      providers.length,
      summary.averageErrorRate
    );

    const mode = degradationEngine.getCurrentMode();
    console.log(`  Degradation Mode: ${mode}`);
    console.log(`  Strategy: timeout_mult=${degradationEngine.getStrategy().timeoutMultiplier}x\n`);
  }, 8000);

  // Print failover stats periodically
  const statsInterval = setInterval(() => {
    const stats = failoverController.getFailoverStats();
    console.log('[FAILOVER STATS]');
    console.log(`  Total: ${stats.totalFailovers}`);
    console.log(`  Recent: ${stats.recentFailovers}`);
    console.log(`  Active: ${stats.activeFailovers}\n`);
  }, 15000);

  // Run for demo duration
  await new Promise((resolve) => setTimeout(resolve, 60000));

  // Cleanup
  scheduler.stop();
  clearInterval(metricsInterval);
  clearInterval(statsInterval);

  // Print final Prometheus metrics
  console.log('\n--- PROMETHEUS METRICS ---\n');
  console.log(metricsCollector.exportPrometheusMetrics());

  // Print dashboard data
  console.log('\n--- DASHBOARD DATA ---\n');
  const dashboard = metricsCollector.getDashboardData();
  console.log(`Summary: ${JSON.stringify(dashboard.summary, null, 2)}`);
}

// Run the application
main().catch(console.error);
