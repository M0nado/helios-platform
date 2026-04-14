# mod-breaker: Circuit Breaker - HELIOS v4.0

**Circuit Breaker Pattern Implementation** for fault tolerance and cascading failure prevention in distributed systems.

## Overview

`mod-breaker` provides production-ready circuit breaker pattern implementation with intelligent state management, threshold monitoring, and adaptive recovery mechanisms. Prevents cascading failures by isolating problematic services and enabling graceful degradation.

### Key Features

✅ **State Management** - CLOSED → OPEN → HALF_OPEN transitions  
✅ **Threshold Monitoring** - Configurable failure/success thresholds  
✅ **Adaptive Recovery** - Exponential, linear, or fixed backoff strategies  
✅ **Metrics Tracking** - Real-time statistics and failure rate monitoring  
✅ **Event Listeners** - Hooks for open/close/error events  
✅ **100% JSDoc** - Complete documentation for all APIs  
✅ **Production Ready** - Error handling, validation, timeout protection  

## Performance Characteristics

- **State transitions**: O(1)
- **Threshold checks**: O(1) amortized
- **Recovery operations**: O(log n) where n = recovery attempts
- **Memory footprint**: ~2-5 KB per circuit breaker instance

## Installation & Usage

### Basic Usage

```javascript
const { CircuitBreaker } = require('mod-breaker');

// Create a circuit breaker
const breaker = new CircuitBreaker({
  name: 'api-service',
  failureThreshold: 5,
  successThreshold: 3,
  timeout: 5000
});

// Execute protected operations
try {
  const result = await breaker.execute(async () => {
    return await externalService.call();
  });
} catch (error) {
  if (error.code === 'CIRCUIT_BREAKER_OPEN') {
    console.log('Circuit is open, service unavailable');
    console.log('Retry after:', error.retryAfter, 'ms');
  }
}
```

### API Reference

#### CircuitBreaker

Main class for circuit breaker implementation.

```javascript
const breaker = new CircuitBreaker(options);
```

**Constructor Options:**

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `name` | string | 'CircuitBreaker' | Circuit breaker name for logging |
| `failureThreshold` | number | 5 | Failures needed to open circuit |
| `successThreshold` | number | 3 | Successes needed to close circuit |
| `timeout` | number | 5000 | Request timeout in milliseconds |
| `recoveryStrategy` | string | 'exponential' | Recovery strategy: 'exponential', 'linear', 'fixed' |
| `recoveryBaseDelay` | number | 1000 | Base recovery delay in milliseconds |
| `onOpen` | function | null | Callback when circuit opens |
| `onClose` | function | null | Callback when circuit closes |
| `onHalfOpen` | function | null | Callback when circuit enters HALF_OPEN |
| `onError` | function | null | Callback on operation error |

**Methods:**

```javascript
// Execute operation with circuit breaker protection
await breaker.execute(operation, context);

// Get current state and statistics
const state = breaker.getState();

// Manually control circuit
breaker.open();
breaker.close();
breaker.reset();
```

#### StateTransitioner

Manages valid circuit state transitions.

```javascript
const transitioner = new StateTransitioner();

transitioner.canTransition('CLOSED', 'OPEN');        // true
transitioner.transit('CLOSED', 'OPEN');              // { from, to, timestamp }
transitioner.getValidNextStates('OPEN');             // ['HALF_OPEN']
```

#### ThresholdMonitor

Tracks metrics and determines circuit state changes.

```javascript
const monitor = new ThresholdMonitor({
  failureThreshold: 5,
  successThreshold: 3,
  failureRateThreshold: 50,
  windowSize: 60000
});

monitor.recordFailure();
monitor.recordSuccess();
monitor.shouldOpen();                      // true if threshold exceeded
monitor.shouldClose();                     // true if recovery complete
const metrics = monitor.getMetrics();      // Get current metrics
```

**Metrics Object:**

```javascript
{
  currentFailures: 2,
  currentSuccesses: 1,
  totalFailures: 5,
  totalSuccesses: 10,
  failureRate: 33.33,
  totalRequests: 15,
  currentWindow: { failures: 2, successes: 1 }
}
```

#### RecoveryManager

Handles circuit recovery with adaptive timing strategies.

```javascript
const recovery = new RecoveryManager({
  strategy: 'exponential',        // 'exponential', 'linear', 'fixed'
  baseDelay: 1000,                // milliseconds
  maxDelay: 30000,                // milliseconds
  multiplier: 2                   // for exponential strategy
});

recovery.recordAttempt();
recovery.getNextRetryTime();      // milliseconds until next retry
const state = recovery.getRecoveryState();
recovery.reset();                 // Reset after successful recovery
```

## CircuitState Values

```javascript
const { CircuitState } = require('mod-breaker');

CircuitState.CLOSED;              // Normal operation
CircuitState.OPEN;                // Service unavailable
CircuitState.HALF_OPEN;           // Testing recovery
```

## Example: API Service Protection

```javascript
const { createBreaker } = require('mod-breaker');

const breaker = createBreaker({
  name: 'user-api',
  failureThreshold: 5,
  successThreshold: 2,
  timeout: 10000,
  recoveryStrategy: 'exponential',
  onOpen: (state) => {
    console.log('API service circuit opened');
    metrics.recordCircuitOpen();
  },
  onClose: (state) => {
    console.log('API service circuit restored');
    metrics.recordCircuitClose();
  },
  onError: (error, state) => {
    logger.error('Request failed', { error, state });
  }
});

// Usage
app.get('/users/:id', async (req, res) => {
  try {
    const user = await breaker.execute(
      () => userService.getUser(req.params.id),
      { userId: req.params.id }
    );
    res.json(user);
  } catch (error) {
    if (error.code === 'CIRCUIT_BREAKER_OPEN') {
      res.status(503).json({ 
        error: 'Service temporarily unavailable',
        retryAfter: error.retryAfter
      });
    } else {
      res.status(500).json({ error: error.message });
    }
  }
});
```

## Example: Recovery Strategies

### Exponential Backoff

Recovery delay increases exponentially with each failed attempt:

```javascript
const breaker = new CircuitBreaker({
  recoveryStrategy: 'exponential',
  recoveryBaseDelay: 1000,    // 1s
  // Delays: 1s, 2s, 4s, 8s, 16s... (capped at maxDelay)
});
```

Best for: Temporary outages that recover gradually

### Linear Backoff

Recovery delay increases linearly:

```javascript
const breaker = new CircuitBreaker({
  recoveryStrategy: 'linear',
  recoveryBaseDelay: 1000,    // 1s
  // Delays: 1s, 2s, 3s, 4s, 5s... (capped at maxDelay)
});
```

Best for: Predictable degradation patterns

### Fixed Backoff

Recovery delay remains constant:

```javascript
const breaker = new CircuitBreaker({
  recoveryStrategy: 'fixed',
  recoveryBaseDelay: 5000     // Always 5s
});
```

Best for: Simple retry scenarios

## State Machine

```
CLOSED (Normal Operation)
  ↓ (failure threshold exceeded)
OPEN (Requests Rejected)
  ↓ (recovery delay elapsed)
HALF_OPEN (Testing Recovery)
  ↓ (success threshold met)
CLOSED (Recovery Complete)
  OR
  ↓ (first failure)
OPEN (Recovery Failed)
```

## Metrics & Monitoring

Get comprehensive metrics for monitoring and alerting:

```javascript
const state = breaker.getState();

// Statistics
state.statistics.executionCount     // Total operations attempted
state.statistics.successCount       // Successful operations
state.statistics.failureCount       // Failed operations
state.statistics.rejectionCount     // Rejected by circuit
state.statistics.lastError          // Last error details
state.statistics.lastStateChange    // Timestamp of state change

// Threshold Metrics
state.metrics.currentFailures       // Current failure count
state.metrics.currentSuccesses      // Current success count
state.metrics.failureRate           // Failure percentage
state.metrics.totalRequests         // Total requests in window

// Recovery Metrics
state.recovery.attempts             // Recovery attempts
state.recovery.nextRetryIn          // ms until next retry
state.recovery.strategy             // Active recovery strategy
```

## Error Handling

Circuit breaker errors include helpful context:

```javascript
try {
  await breaker.execute(() => operation());
} catch (error) {
  if (error.code === 'CIRCUIT_BREAKER_OPEN') {
    // Circuit is open, provide fallback response
    console.log('Retry after:', error.retryAfter, 'ms');
  } else if (error.message.includes('timeout')) {
    // Operation timed out
    console.log('Request took too long');
  } else {
    // Operation failed
    console.log('Operation error:', error.message);
  }
}
```

## Testing

Run comprehensive test suite:

```bash
npm test tests/test.js
```

**Test Coverage:**
- ✓ State transitions (8 tests)
- ✓ Threshold monitoring (9 tests)
- ✓ Recovery mechanisms (9 tests)
- ✓ Circuit breaker execution (20+ tests)
- **Total: 45+ quality tests**

## Real-World Examples

See `examples.js` for:
1. API service protection
2. Database connection pooling
3. Cascading failure prevention
4. Manual circuit control
5. Threshold configuration
6. Recovery strategies
7. Metrics monitoring

Run examples:
```bash
node examples.js
```

## Best Practices

### 1. Choose Appropriate Thresholds

```javascript
// Strict for critical services
const critical = new CircuitBreaker({
  failureThreshold: 1,        // Open immediately
  successThreshold: 5         // Require proof of recovery
});

// Lenient for non-critical services
const noncritical = new CircuitBreaker({
  failureThreshold: 10,       // Allow multiple failures
  successThreshold: 2         // Quick recovery
});
```

### 2. Implement Fallback Logic

```javascript
async function userService(userId) {
  try {
    return await breaker.execute(() => apiCall(userId));
  } catch (error) {
    if (error.code === 'CIRCUIT_BREAKER_OPEN') {
      // Return cached data or default
      return getCachedUser(userId) || getDefaultUser();
    }
    throw error;
  }
}
```

### 3. Monitor Circuit State

```javascript
setInterval(() => {
  const state = breaker.getState();
  metrics.gauge('circuit.state', {
    service: state.name,
    state: state.state,
    failures: state.statistics.failureCount
  });
}, 5000);
```

### 4. Configure Recovery Appropriately

```javascript
// For temporary network issues
const networkBreaker = new CircuitBreaker({
  recoveryStrategy: 'exponential',
  recoveryBaseDelay: 1000,
  maxDelay: 30000
});

// For rate-limited APIs
const apiBreaker = new CircuitBreaker({
  recoveryStrategy: 'exponential',
  recoveryBaseDelay: 5000,  // Start with longer delay
  maxDelay: 60000
});
```

## License

HELIOS v4.0 Fleet Expansion Module

## Version

4.0.0 - Production Ready
