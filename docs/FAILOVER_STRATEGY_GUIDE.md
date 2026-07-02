# Monado Blade Phase 4D: Failover Monitor & Health Monitoring System

## Overview

Phase 4D implements a comprehensive health monitoring and failover system for the Monado Blade platform. This system ensures 99.9% uptime by automatically detecting provider failures, triggering failovers, and gracefully degrading service levels under stress.

## Architecture

### Core Components

#### 1. **Circuit Breaker Pattern** (`CircuitBreaker.ts`)
Prevents cascading failures by monitoring provider health and controlling request flow.

**States:**
- **CLOSED**: Normal operation, all requests allowed
- **OPEN**: Provider failed, all requests blocked
- **HALF_OPEN**: Recovery test, limited requests to verify recovery

**Features:**
- Automatic state transitions based on failure thresholds
- Exponential backoff for recovery attempts
- Configurable failure/success thresholds
- State change event notifications
- Per-provider circuit breakers

**Configuration:**
```typescript
{
  failureThreshold: 5,      // Failures before opening circuit
  successThreshold: 3,      // Successes needed to close after half-open
  timeout: 60000,           // Time before attempting recovery
  initialBackoff: 1000,     // Initial backoff in milliseconds
  maxBackoff: 60000         // Maximum backoff timeout
}
```

#### 2. **Health Check Scheduler** (`HealthCheckScheduler.ts`)
Manages periodic health checks across all providers with adaptive intervals.

**Features:**
- 30-second base health check interval
- Concurrent health checks across all providers
- Adaptive interval adjustment based on provider health
- Health history tracking (up to 100 recent checks)
- Degradation pattern detection
- Automatic interval tuning (more frequent for degrading providers)

**Adaptive Intervals:**
- **Normal**: 30s
- **Degrading**: 15s (0.5x multiplier)
- **Unhealthy**: 10s-15s (combined with circuit breaker state)
- **Min interval**: 5s
- **Max interval**: 120s

**Configuration:**
```typescript
{
  interval: 30000,                      // Base health check interval
  timeout: 10000,                       // Individual check timeout
  adaptiveInterval: true,               // Enable adaptive intervals
  degradingIntervalMultiplier: 0.5,     // Multiplier for degrading providers
  minInterval: 5000,                    // Minimum interval
  maxInterval: 120000                   // Maximum interval
}
```

#### 3. **Failover Controller** (`FailoverController.ts`)
Automatically detects failures and triggers provider failovers.

**Features:**
- Automatic failover detection based on circuit breaker state
- Manual failover support
- Concurrent failover limiting (prevents cascade)
- Cascade failure prevention with configurable delays
- Failover event tracking and history
- Recent failover detection window

**Failover Workflow:**
1. Health check detects failure
2. Circuit breaker transitions to OPEN
3. Failover controller evaluates failover conditions
4. Next healthy provider is selected
5. Failover delay applied (prevents cascade)
6. Failover executed and tracked

**Configuration:**
```typescript
{
  enabled: true,
  maxConcurrentFailovers: 3,
  failoverDelay: 1000,
  cascadePreventionDelay: 5000
}
```

#### 4. **Metrics Collector** (`MetricsCollector.ts`)
Collects and aggregates health metrics for monitoring and dashboard purposes.

**Metrics Tracked:**
- Provider status (HEALTHY, DEGRADED, UNHEALTHY)
- Circuit breaker state
- Latency (min, max, average)
- Error rate
- Availability percentage
- Consecutive failures/successes
- System degradation mode

**Export Formats:**
- **JSON**: Full metrics export
- **Prometheus**: Standard metrics format for monitoring systems
- **Dashboard**: Data optimized for UI visualization

**Prometheus Metrics:**
```
monado_blade_provider_health{provider="..."}
monado_blade_provider_latency_ms{provider="..."}
monado_blade_provider_error_rate{provider="..."}
monado_blade_provider_availability{provider="..."}
monado_blade_circuit_breaker_state{provider="..."}
monado_blade_system_degradation_mode
monado_blade_activated_failovers
monado_blade_recovered_providers
```

#### 5. **Graceful Degradation Engine** (`GracefulDegradationEngine.ts`)
Reduces service levels under load to maintain core functionality.

**Degradation Modes:**
- **NORMAL**: All features enabled, standard timeouts
- **REDUCED**: Non-critical tasks shed, 2x timeout multiplier
- **CRITICAL**: Only critical/high priority tasks, 4x timeout multiplier

**Degradation Triggers:**
- Error rate > 10% → REDUCED
- Error rate > 30% → CRITICAL
- Health ratio < 50% → REDUCED
- Health ratio < 25% → CRITICAL

**Features:**
- Automatic mode transitions
- Task priority-based execution
- Timeout adjustment
- Recovery planning
- Degradation duration tracking

**Configuration:**
```typescript
{
  enabledAt: 0.5,           // Health ratio threshold
  timeoutMultiplier: 2,     // Timeout multiplier for REDUCED
  maxTimeoutMs: 30000,      // Absolute maximum timeout
  shedNonCriticalTasks: true
}
```

## Integration Points

### With ProviderRegistry (Phase 4A)
- Registers providers for health monitoring
- Gets provider information
- Updates provider status in registry

### With SmartRouter (Phase 4C)
- Provides health data for routing decisions
- Gets healthy providers for failover
- Integrates degradation mode for timeout adjustments

### Health Check Flow
```
HealthCheckScheduler
    ↓
Provider Health Check
    ↓
Circuit Breaker Update
    ↓
Metrics Collector Record
    ↓
Degradation Engine Evaluate
    ↓
Failover Check
    ↓
Event Notifications
```

## Usage Examples

### Basic Setup

```typescript
import {
  CircuitBreaker,
  HealthCheckScheduler,
  FailoverController,
  MetricsCollector,
  GracefulDegradationEngine
} from '@monado/blade-monitoring';

// Initialize components
const circuitBreakers = new Map();
const scheduler = new HealthCheckScheduler();
const failoverController = new FailoverController();
const metricsCollector = new MetricsCollector();
const degradationEngine = new GracefulDegradationEngine();

// Register providers
for (const provider of providers) {
  const cb = new CircuitBreaker(provider.providerId);
  circuitBreakers.set(provider.providerId, cb);
  scheduler.registerProvider(provider, cb);
}

// Start health checks
await scheduler.start(providers);

// Listen for health check results
scheduler.on('healthCheckResult', (result) => {
  metricsCollector.recordHealthCheckResult(result);
  
  // Check for failover
  if (failoverController.shouldTriggerFailover(result)) {
    const nextProvider = failoverController.getNextHealthyProvider(
      currentProvider,
      providers
    );
    if (nextProvider) {
      failoverController.executeFailover(
        currentProvider,
        nextProvider,
        `Provider failure: ${result.error}`
      );
    }
  }
});

// Monitor degradation
scheduler.on('degradationDetected', (providerId, isDegrading) => {
  console.log(`Provider ${providerId} degradation detected: ${isDegrading}`);
});

// Listen for failover events
failoverController.onFailover((event) => {
  console.log(`Failover: ${event.fromProviderId} -> ${event.toProviderId}`);
  metricsCollector.updateCircuitBreakerState(
    event.fromProviderId,
    CircuitBreakerState.OPEN
  );
});

// Evaluate degradation regularly
setInterval(() => {
  const summary = metricsCollector.calculateSummary();
  degradationEngine.evaluateSystemHealth(
    summary.healthyProviders,
    metricsCollector.getAllMetrics().length,
    summary.averageErrorRate
  );
}, 30000);
```

### Querying Metrics

```typescript
// Get current metrics
const snapshot = metricsCollector.getCurrentSnapshot();
console.log(`Healthy providers: ${snapshot.providers.size}`);

// Export for monitoring
const prometheusMetrics = metricsCollector.exportPrometheusMetrics();

// Get dashboard data
const dashboard = metricsCollector.getDashboardData();
console.log('Health trends:', dashboard.healthTrends);
console.log('Latency trends:', dashboard.latencyTrends);

// Get degradation state
const degradationState = degradationEngine.getState();
console.log(`Current mode: ${degradationState.currentMode}`);
console.log(`Timeout multiplier: ${degradationState.strategy.timeoutMultiplier}`);

// Get failover statistics
const failoverStats = failoverController.getFailoverStats();
console.log(`Total failovers: ${failoverStats.totalFailovers}`);
console.log(`Recent failovers: ${failoverStats.recentFailovers}`);
```

### Task Priority Under Degradation

```typescript
const strategy = degradationEngine.getStrategy();

async function processTask(task, priority) {
  // Check if task should execute
  if (!degradationEngine.shouldExecuteTask(priority)) {
    console.log(`Task skipped: ${priority} task in ${strategy.mode} mode`);
    return;
  }

  // Adjust timeout based on degradation
  const timeout = degradationEngine.adjustTimeout(task.baseTimeout);

  // Process with adjusted timeout
  return executeWithTimeout(task, timeout);
}

// Usage
await processTask(criticalTask, 'CRITICAL');  // Always runs
await processTask(normalTask, 'NORMAL');      // Runs if not critical mode
await processTask(lowPriorityTask, 'LOW');    // Only in normal mode
```

## Monitoring Strategy

### Health Check Frequency

The system uses a 30-second base interval for health checks with adaptive adjustment:

- **Healthy state**: Checks every 30-120 seconds
- **Degrading**: Checks every 15-30 seconds
- **Circuit breaker HALF_OPEN**: Checks every 5-15 seconds
- **Circuit breaker OPEN**: Checks every 5-10 seconds

### Alerting Thresholds

**WARNING** (Degradation):
- Error rate > 5%
- Latency p95 > 2x baseline
- Provider availability < 95%
- 2-3 consecutive failures

**CRITICAL** (Intervention):
- Error rate > 10%
- Provider unavailable
- Circuit breaker OPEN
- Cascading failures detected

### Dashboard Metrics

Display these metrics on monitoring dashboard:

1. **System Health**
   - Number of healthy/degraded/unhealthy providers
   - Overall system availability
   - Current degradation mode
   - Number of active failovers

2. **Per-Provider Metrics**
   - Status and circuit breaker state
   - Latency (min/max/avg)
   - Error rate
   - Availability percentage
   - Recent failover status

3. **Trends** (30-minute window)
   - Health trend per provider
   - Latency trend per provider
   - Error rate trend
   - Degradation mode changes

## Troubleshooting

### Provider Stuck in OPEN State

**Symptoms:** Provider never recovers even though it's healthy

**Causes:**
- Backoff timeout too long
- Success threshold too high
- Continued failures

**Solutions:**
1. Check recent health check results: `scheduler.getHealthHistory(providerId)`
2. Verify provider is actually healthy
3. Manually reset if necessary: `circuitBreaker.reset()`
4. Reduce initial backoff if frequently failing

### Cascade Failures

**Symptoms:** Multiple providers fail shortly after first failure

**Causes:**
- Shared dependency failing
- Load spike causing timeouts
- Cascade prevention delay too short

**Solutions:**
1. Increase `cascadePreventionDelay` (default 5s)
2. Reduce `maxConcurrentFailovers` (default 3)
3. Investigate root cause of initial failure
4. Check metrics for related errors

### Excessive Failovers

**Symptoms:** Frequent failovers between same providers

**Causes:**
- All providers unhealthy
- Failing provider not actually fixed
- Too low failure threshold

**Solutions:**
1. Investigate why all providers unhealthy
2. Check degradation mode appropriately set to CRITICAL
3. Manually trigger investigation/rollback
4. Review failover history: `failoverController.getRecentFailovers()`

### High Latency in Degraded Mode

**Symptoms:** Requests timing out despite increased timeout

**Causes:**
- Max timeout too low
- Degradation too aggressive
- Underlying provider issue

**Solutions:**
1. Increase `maxTimeoutMs` in degradation config
2. Adjust degradation thresholds
3. Investigate actual provider response times
4. Review logs for specific errors

## Performance Considerations

### Health Check Overhead
- Base interval: 30 seconds
- Per check: ~100-500ms
- Memory: ~1KB per check history entry
- Total memory for 100 providers: ~100MB

### Metrics Collection
- Snapshots every 60 seconds by default
- History retention: 1 day (1440 snapshots)
- Memory: ~50-100MB for full history

### CPU Impact
- Health checks: <1% CPU at 30s interval
- Metrics collection: <0.5% CPU
- Total: <2% CPU with normal operation

## Best Practices

1. **Set Realistic Thresholds**
   - Failure threshold: 5-10 consecutive failures
   - Success threshold: 2-3 successes
   - Match to provider's normal failure rate

2. **Monitor from Multiple Angles**
   - Track trends, not just current values
   - Compare against baseline metrics
   - Alert on rate of change, not just absolute values

3. **Test Failover Scenarios**
   - Regularly test manual failovers
   - Simulate provider failures
   - Verify degradation works as expected

4. **Maintain Health Check Stability**
   - Keep checks lightweight (< 1s ideally)
   - Avoid checks that depend on other providers
   - Use independent health endpoints

5. **Document Custom Providers**
   - Clearly define success/failure criteria
   - Document acceptable latency ranges
   - Specify recovery procedures

## API Reference

### CircuitBreaker
- `recordResult(result: HealthCheckResult): void`
- `canExecute(): boolean`
- `getState(): CircuitBreakerState`
- `reset(): void`
- `getTimeUntilRetry(): number`
- `onStateChange(listener): void`

### HealthCheckScheduler
- `registerProvider(provider, circuitBreaker): void`
- `unregisterProvider(providerId): void`
- `start(providers): Promise<void>`
- `stop(): void`
- `getHealthHistory(providerId): HealthCheckResult[]`
- `on(event, listener): void`

### FailoverController
- `shouldTriggerFailover(health): boolean`
- `executeFailover(from, to, reason): Promise<boolean>`
- `getNextHealthyProvider(current, all): ProviderAdapter | null`
- `manualFailover(fromId, toId, reason): Promise<boolean>`
- `getRecentFailovers(maxAge): FailoverEvent[]`
- `onFailover(listener): void`

### MetricsCollector
- `recordHealthCheckResult(result): void`
- `updateCircuitBreakerState(providerId, state): void`
- `getCurrentSnapshot(): MetricsSnapshot`
- `exportMetrics(): MetricsExport`
- `exportPrometheusMetrics(): string`
- `getDashboardData(): Object`

### GracefulDegradationEngine
- `evaluateSystemHealth(healthy, total, errorRate): DegradationMode`
- `getCurrentMode(): DegradationMode`
- `adjustTimeout(baseTimeout): number`
- `shouldExecuteTask(priority): boolean`
- `getRecoveryPlan(): Object`
- `onModeChange(listener): void`

## Future Enhancements

1. **Machine Learning**: Predictive failure detection
2. **Multi-region Failover**: Geographic failover support
3. **Custom Health Checks**: Plugin system for provider-specific checks
4. **Automatic Scaling**: Trigger scale-up on provider failures
5. **Distributed Tracing**: Integration with tracing systems
6. **Advanced Analytics**: Anomaly detection, forecasting
