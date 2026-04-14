# HELIOS v4.0 Health Checks Module

Liveness probes, readiness checks, and circuit breaker integration for resilient systems.

## Features

- **Liveness Probes**: Monitor if service is still running
- **Readiness Checks**: Verify dependencies before accepting traffic
- **Circuit Breaker Integration**: Handle cascading failures gracefully

## Installation

```javascript
const { 
  LivenessProbe, 
  ReadinessCheck, 
  CircuitBreakerIntegration,
  createHealthSystem 
} = require('./index');
```

## API Documentation

### LivenessProbe

Determine if service process is alive and functioning.

**Performance**: <0.5ms per probe check

```javascript
const probe = new LivenessProbe({
  unhealthyThreshold: 3,
  healthyThreshold: 2,
  onStateChange: (event) => console.log(event)
});

// Register health checks
probe.registerCheck('memory', async () => {
  const usage = process.memoryUsage();
  return usage.heapUsed < 500 * 1024 * 1024; // Less than 500MB
});

probe.registerCheck('event-loop', async () => {
  const start = Date.now();
  await new Promise(resolve => setImmediate(resolve));
  return Date.now() - start < 50; // Should respond quickly
});

// Run probes
const result = await probe.runChecks();
console.log(result.overallStatus); // 'healthy' or 'unhealthy'

// Check state
console.log(probe.getState()); // 'healthy', 'unhealthy', or 'unknown'
```

**Methods**:
- `registerCheck(name, checkFn)` - Register check
- `runChecks()` - Execute all checks
- `getState()` - Get current state
- `getLastCheckResult()` - Get last result
- `getHistory()` - Get historical results
- `getStatus()` - Get detailed status
- `reset()` - Reset state

### ReadinessCheck

Verify dependencies are initialized before accepting requests.

**Performance**: <0.5ms per check

```javascript
const readiness = new ReadinessCheck({
  timeoutMs: 10000,
  onReadinessChange: (event) => console.log(event)
});

// Register dependencies
readiness.registerDependency('database', async () => {
  try {
    await db.query('SELECT 1');
    return true;
  } catch (err) {
    return false;
  }
});

readiness.registerDependency('cache', async () => {
  try {
    await redis.ping();
    return true;
  } catch (err) {
    return false;
  }
});

// Check readiness
const result = await readiness.checkReadiness();
console.log(result.ready); // true or false

// Get status
if (readiness.isServiceReady()) {
  // Accept traffic
}

// Failed dependencies
const failed = readiness.getFailedDependencies();
```

**Methods**:
- `registerDependency(name, checkFn)` - Register dependency
- `checkReadiness()` - Run all checks
- `isServiceReady()` - Get ready state
- `getFailedDependencies()` - Get failed deps
- `getStatus()` - Get detailed status
- `getHistory()` - Get historical results
- `markNotReady(reason)` - Manual mark not ready
- `reset()` - Reset state

### CircuitBreakerIntegration

Protect services from cascading failures.

**Performance**: <0.1ms per state check

```javascript
const circuitBreaker = new CircuitBreakerIntegration({
  failureThreshold: 5,
  successThreshold: 2,
  timeoutMs: 30000,
  onStateChange: (event) => console.log(event)
});

async function callExternalService(data) {
  try {
    // Check if circuit allows requests
    if (!circuitBreaker.canMakeRequest()) {
      throw new Error('Circuit breaker is OPEN');
    }

    const response = await externalAPI.call(data);
    circuitBreaker.recordSuccess();
    return response;
  } catch (error) {
    circuitBreaker.recordFailure();
    throw error;
  }
}

// Monitor state
console.log(circuitBreaker.getState()); // 'closed', 'open', or 'half-open'

// Reset if needed
circuitBreaker.reset();
```

**States**:
- **CLOSED**: Normal operation, requests allowed
- **OPEN**: Too many failures, requests blocked
- **HALF-OPEN**: Testing recovery, limited requests

**Methods**:
- `recordSuccess()` - Record success
- `recordFailure()` - Record failure
- `getState()` - Get current state
- `canMakeRequest()` - Check if request allowed
- `getStatus()` - Get detailed status
- `getEventHistory()` - Get event history
- `reset()` - Reset to closed

## Real-World Examples

### Example 1: Express Health Endpoints

```javascript
const express = require('express');
const { createHealthSystem } = require('./index');

const app = express();
const health = createHealthSystem();

// Register liveness checks
health.liveness.registerCheck('memory', async () => {
  const usage = process.memoryUsage();
  return usage.heapUsed < 500 * 1024 * 1024;
});

// Register readiness checks
health.readiness.registerDependency('database', async () => {
  try {
    await db.query('SELECT 1');
    return true;
  } catch {
    return false;
  }
});

// Liveness endpoint
app.get('/health/live', async (req, res) => {
  const result = await health.liveness.runChecks();
  const statusCode = result.overallStatus === 'healthy' ? 200 : 503;
  res.status(statusCode).json(result);
});

// Readiness endpoint
app.get('/health/ready', async (req, res) => {
  const result = await health.readiness.checkReadiness();
  const statusCode = result.ready ? 200 : 503;
  res.status(statusCode).json(result);
});
```

### Example 2: Kubernetes Integration

```javascript
const health = createHealthSystem();

// Configure for K8s liveness probe
health.liveness.registerCheck('process', async () => {
  return process.uptime() > 0;
});

// Configure for K8s readiness probe
health.readiness.registerDependency('api-gateway', async () => {
  try {
    const response = await fetch('http://api-gateway:3000/health');
    return response.ok;
  } catch {
    return false;
  }
});

// K8s will call these endpoints
// GET /health/live - every 10 seconds
// GET /health/ready - every 5 seconds
```

### Example 3: Circuit Breaker Pattern

```javascript
const { CircuitBreakerIntegration } = require('./index');

const breaker = new CircuitBreakerIntegration({
  failureThreshold: 3,
  successThreshold: 2,
  timeoutMs: 60000
});

async function callPaymentService(paymentData) {
  // Check circuit state first
  if (!breaker.canMakeRequest()) {
    // Return cached response or fail gracefully
    return { status: 'service-unavailable', cached: true };
  }

  try {
    const response = await paymentAPI.charge(paymentData);
    breaker.recordSuccess();
    return response;
  } catch (error) {
    try {
      breaker.recordFailure();
    } catch (breaker_error) {
      // Circuit is now open
      console.log('Circuit breaker opened, rejecting requests');
    }
    throw error;
  }
}

// Usage
try {
  const result = await callPaymentService({ amount: 99.99 });
} catch (err) {
  console.log('Payment failed:', err.message);
}
```

### Example 4: Dependency Cascade Check

```javascript
const readiness = new ReadinessCheck();

// Register service dependencies in order
readiness.registerDependency('postgres', async () => {
  try {
    await pool.query('SELECT 1');
    return true;
  } catch {
    return false;
  }
});

readiness.registerDependency('redis', async () => {
  try {
    await redis.ping();
    return true;
  } catch {
    return false;
  }
});

readiness.registerDependency('kafka', async () => {
  try {
    await kafka.admin().connect();
    await kafka.admin().disconnect();
    return true;
  } catch {
    return false;
  }
});

// Check readiness
const result = await readiness.checkReadiness();

if (!result.ready) {
  console.log('Failed dependencies:', result.dependencies
    .filter(d => d.status === 'not-ready')
    .map(d => d.name));
  process.exit(1);
}

console.log('All dependencies ready!');
startServer();
```

### Example 5: Combined Health System

```javascript
const { createHealthSystem } = require('./index');

const health = createHealthSystem({
  onLivenessChange: (evt) => console.log('🫀 Liveness:', evt),
  onReadinessChange: (evt) => console.log('🚀 Readiness:', evt),
  onCircuitChange: (evt) => console.log('🔌 Circuit:', evt)
});

// Setup liveness
health.liveness.registerCheck('process', async () => {
  return process.uptime() > 0;
});

health.liveness.registerCheck('memory', async () => {
  const usage = process.memoryUsage();
  return usage.heapUsed < 500 * 1024 * 1024;
});

// Setup readiness
health.readiness.registerDependency('database', async () => {
  return await checkDatabase();
});

health.readiness.registerDependency('cache', async () => {
  return await checkRedis();
});

// Setup circuit breaker
const breaker = health.circuitBreaker;

// Periodic health checks
setInterval(async () => {
  const livenessResult = await health.liveness.runChecks();
  const readinessResult = await health.readiness.checkReadiness();
  
  const status = health.getStatus();
  console.log('Health Status:', status);
}, 30000);

// API endpoint
app.get('/health', (req, res) => {
  const status = health.getStatus();
  const statusCode = health.readiness.isServiceReady() ? 200 : 503;
  res.status(statusCode).json(status);
});
```

## Performance Characteristics

| Operation | Latency | Notes |
|-----------|---------|-------|
| registerCheck | <0.1ms | Hash map insertion |
| runChecks | variable | Depends on check functions |
| recordSuccess | <0.1ms | Counter increment |
| recordFailure | <0.1ms | Counter increment |
| canMakeRequest | <0.05ms | State check |
| getState | <0.05ms | Simple property read |

## Configuration

### LivenessProbe Options
- `unhealthyThreshold` (number): Failures before unhealthy
- `healthyThreshold` (number): Successes before healthy
- `timeoutMs` (number): Check timeout
- `onStateChange` (function): State change callback

### ReadinessCheck Options
- `timeoutMs` (number): Check timeout
- `onReadinessChange` (function): Change callback

### CircuitBreakerIntegration Options
- `failureThreshold` (number): Failures before open
- `successThreshold` (number): Successes before closed
- `timeoutMs` (number): Open state duration
- `onStateChange` (function): Change callback

## Best Practices

1. **Liveness**: Quick checks that verify process is alive
2. **Readiness**: Comprehensive checks that verify all dependencies
3. **Circuit Breaker**: Use for external service calls
4. **Timeouts**: Set appropriate timeouts for all checks
5. **Callbacks**: Use state change callbacks for monitoring

## Error Handling

All classes include proper error handling:

```javascript
try {
  circuitBreaker.recordFailure();
} catch (err) {
  console.error('Circuit breaker opened:', err.message);
}
```

## License

HELIOS v4.0
