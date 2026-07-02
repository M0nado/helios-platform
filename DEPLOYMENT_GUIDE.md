# Phase 4D: Deployment & Integration Guide

## Quick Start

### Installation

```bash
cd MonadoBlade
npm install
npm run build
npm test
```

### Verify All Tests Pass

```bash
npm run build
# Expected: No errors
npm test
# Expected: All 40/40 tests passing
```

### Generate Production Build

```bash
npm run build
# Output: dist/ directory with compiled JavaScript
```

## Component Usage

### 1. Initialize Monitoring System

```typescript
import {
  CircuitBreaker,
  HealthCheckScheduler,
  FailoverController,
  MetricsCollector,
  GracefulDegradationEngine
} from '@monado/blade-monitoring';

// Create instances
const scheduler = new HealthCheckScheduler({
  interval: 30000,           // 30-second base interval
  timeout: 10000,            // 10-second check timeout
  adaptiveInterval: true,    // Enable adaptive scaling
  minInterval: 5000,
  maxInterval: 120000
});

const circuitBreakers = new Map();
const failoverController = new FailoverController({
  enabled: true,
  maxConcurrentFailovers: 3,
  failoverDelay: 1000,
  cascadePreventionDelay: 5000
});

const metricsCollector = new MetricsCollector();
const degradationEngine = new GracefulDegradationEngine({
  enabledAt: 0.5,
  timeoutMultiplier: 2,
  maxTimeoutMs: 30000
});
```

### 2. Register Providers

```typescript
for (const provider of providers) {
  // Create circuit breaker for each provider
  const cb = new CircuitBreaker(provider.providerId, {
    failureThreshold: 5,
    successThreshold: 2,
    timeout: 60000,
    initialBackoff: 1000,
    maxBackoff: 60000
  });
  
  circuitBreakers.set(provider.providerId, cb);
  scheduler.registerProvider(provider, cb);
  metricsCollector.initializeProvider(provider.providerId);
}
```

### 3. Setup Event Listeners

```typescript
// Health check results
scheduler.on('healthCheckResult', (result) => {
  metricsCollector.recordHealthCheckResult(result);
  
  // Update circuit breaker
  const cb = circuitBreakers.get(result.providerId);
  if (cb) {
    cb.recordResult(result);
  }
});

// Degradation detection
scheduler.on('degradationDetected', (providerId, isDegrading) => {
  console.log(`Provider ${providerId} degradation: ${isDegrading}`);
});

// Circuit breaker state changes
for (const [, cb] of circuitBreakers) {
  cb.onStateChange((change) => {
    console.log(`CB State: ${change.oldState} → ${change.newState}`);
    metricsCollector.updateCircuitBreakerState(
      change.providerId,
      change.newState
    );
  });
}

// Failover events
failoverController.onFailover((event) => {
  console.log(`Failover: ${event.fromProviderId} → ${event.toProviderId}`);
});
```

### 4. Start Monitoring

```typescript
// Start health checks
await scheduler.start(providers);

// Failover controller is ready (register providers first)
failoverController.registerProviders(providers, circuitBreakers);

// Periodically evaluate system health
setInterval(() => {
  const snapshot = metricsCollector.getCurrentSnapshot();
  const summary = metricsCollector.calculateSummary();
  
  // Update degradation mode
  degradationEngine.evaluateSystemHealth(
    summary.healthyProviders,
    providers.length,
    summary.averageErrorRate
  );
  
  // Record metrics
  metricsCollector.recordSnapshot();
}, 60000);
```

### 5. Monitor & Export Metrics

```typescript
// Get current snapshot
const snapshot = metricsCollector.getCurrentSnapshot();
console.log(`Healthy providers: ${snapshot.providers.size}`);

// Export Prometheus metrics
const prometheusMetrics = metricsCollector.exportPrometheusMetrics();
res.header('Content-Type', 'text/plain');
res.send(prometheusMetrics);

// Get dashboard data
const dashboard = metricsCollector.getDashboardData();
res.json({
  summary: dashboard.summary,
  healthTrends: Object.fromEntries(dashboard.healthTrends),
  latencyTrends: Object.fromEntries(dashboard.latencyTrends)
});
```

## Configuration Recommendations

### Development Environment
```typescript
const config = {
  HealthCheckScheduler: {
    interval: 10000,           // 10 seconds for faster iteration
    timeout: 5000,
    adaptiveInterval: true,
    minInterval: 2000,
    maxInterval: 30000
  },
  CircuitBreaker: {
    failureThreshold: 2,       // Lower threshold for faster detection
    successThreshold: 1,
    timeout: 5000
  },
  FailoverController: {
    cascadePreventionDelay: 1000
  }
};
```

### Staging Environment
```typescript
const config = {
  HealthCheckScheduler: {
    interval: 30000,
    timeout: 10000,
    adaptiveInterval: true,
    minInterval: 5000,
    maxInterval: 120000
  },
  CircuitBreaker: {
    failureThreshold: 3,
    successThreshold: 2,
    timeout: 30000,
    initialBackoff: 2000
  },
  FailoverController: {
    cascadePreventionDelay: 3000
  }
};
```

### Production Environment
```typescript
const config = {
  HealthCheckScheduler: {
    interval: 30000,           // Standard 30-second interval
    timeout: 10000,
    adaptiveInterval: true,
    minInterval: 5000,
    maxInterval: 120000
  },
  CircuitBreaker: {
    failureThreshold: 5,       // Fewer false positives
    successThreshold: 3,
    timeout: 60000,
    initialBackoff: 1000,
    maxBackoff: 60000
  },
  FailoverController: {
    maxConcurrentFailovers: 3,
    cascadePreventionDelay: 5000
  },
  GracefulDegradation: {
    enabledAt: 0.5,            // Activate at 50% provider loss
    timeoutMultiplier: 2
  }
};
```

## Monitoring & Alerting Setup

### Prometheus Configuration

```yaml
scrape_configs:
  - job_name: 'monado-blade'
    static_configs:
      - targets: ['localhost:3000']
    metrics_path: '/metrics'
    scrape_interval: 15s
```

### Alert Rules

```yaml
groups:
  - name: monado_blade
    rules:
      - alert: ProvidersUnhealthy
        expr: (monado_blade_provider_health == 0)
        for: 5m
        annotations:
          summary: "Provider unhealthy"
      
      - alert: HighErrorRate
        expr: (monado_blade_provider_error_rate > 0.1)
        for: 2m
        annotations:
          summary: "High error rate detected"
      
      - alert: SystemDegraded
        expr: (monado_blade_system_degradation_mode > 0)
        for: 5m
        annotations:
          summary: "System in degradation mode"
```

## Testing Scenarios

### Test 1: Provider Failure
```typescript
// Simulate provider failure
(provider as MockProvider).setHealth(0);

// Verify:
// - Health checks report UNHEALTHY
// - Circuit breaker transitions to OPEN
// - Failover triggers
// - Metrics recorded correctly
```

### Test 2: Partial System Failure
```typescript
// Fail 50% of providers
provider1.setHealth(0);
provider2.setHealth(0);

// Verify:
// - Degradation mode shifts to REDUCED
// - Timeouts doubled
// - Non-critical tasks shed
// - System remains available
```

### Test 3: Recovery
```typescript
// Restore provider after failure
provider1.setHealth(1);

// Verify:
// - Health checks return to HEALTHY
// - Circuit breaker transitions to HALF_OPEN then CLOSED
// - Failover reverses if needed
// - Degradation mode normalizes
```

## Troubleshooting

### Circuit Breaker Stuck OPEN

```typescript
// Check backoff calculation
const cb = circuitBreakers.get(providerId);
console.log(cb.getDetailedState());
// Check if nextRetryTime is very large

// Solutions:
// 1. Verify provider is actually healthy
// 2. Reduce initialBackoff
// 3. Manually reset: cb.reset()
```

### Excessive Failovers

```typescript
// Check failover statistics
const stats = failoverController.getFailoverStats();
console.log(`Recent failovers: ${stats.recentFailovers}`);

// Solutions:
// 1. Increase cascadePreventionDelay
// 2. Reduce maxConcurrentFailovers
// 3. Investigate root cause
// 4. Check degradation mode appropriate
```

### Metrics Not Updating

```typescript
// Verify scheduler is running
const stats = scheduler.getStatistics();
console.log(`Running: ${stats.isRunning}`);

// Check provider registration
console.log(`Registered: ${stats.registeredProviders}`);

// Verify listeners attached
// Check no errors in health check callbacks
```

## Deployment Checklist

- [ ] All tests passing (npm test)
- [ ] Build succeeds (npm run build)
- [ ] No TypeScript errors
- [ ] Health endpoints configured
- [ ] Monitoring/alerting setup
- [ ] Log aggregation configured
- [ ] Failover procedures documented
- [ ] Team trained on monitoring
- [ ] Thresholds tuned for environment
- [ ] Dashboard created
- [ ] Runbooks written
- [ ] On-call alerts configured
- [ ] Backup providers available
- [ ] Failover tests passed
- [ ] Recovery procedures tested

## Production Monitoring Dashboard

Recommended metrics to display:

```
System Health:
  - Healthy Providers: {value}/10
  - System Availability: {percentage}%
  - Current Degradation: {mode}
  - Active Failovers: {count}

Per-Provider:
  - Status (Healthy/Degraded/Unhealthy)
  - Latency (p50/p95/p99)
  - Error Rate (%)
  - Availability (%)
  - Circuit Breaker State

Trends (30 minute):
  - Health Status Line Chart
  - Error Rate Line Chart
  - Latency Percentiles
  - Failover Count Bar Chart

Recent Events:
  - Last 10 failovers
  - Last 10 state changes
  - Current degradation mode
  - Recovery time estimates
```

## Support & Documentation

- **API Documentation**: See `docs/FAILOVER_STRATEGY_GUIDE.md`
- **Code Examples**: See `src/index.ts`
- **Type Definitions**: See `src/monitoring/types.ts`
- **Test Examples**: See `src/monitoring/__tests__/failover.test.ts`

## Contact

For questions or issues with Phase 4D implementation, refer to:
1. FAILOVER_STRATEGY_GUIDE.md for detailed documentation
2. Test suite for usage examples
3. Type definitions for API reference

---

**Status**: ✅ Production Ready
**Last Updated**: 2026-04-23
**Version**: 1.0.0
