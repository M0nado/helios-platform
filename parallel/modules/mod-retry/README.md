# mod-retry: Retry Handler - HELIOS v4.0

**Intelligent Retry Manager** with configurable policies, jitter strategies, and exponential backoff for transient failure recovery.

## Overview

`mod-retry` provides production-ready retry mechanisms with multiple backoff policies (exponential, linear, fibonacci, fixed), adaptive jitter strategies, and comprehensive attempt tracking. Prevents thundering herd problems and optimizes recovery from transient failures.

### Key Features

✅ **Multiple Policies** - Exponential, linear, fibonacci, fixed backoff  
✅ **Jitter Strategies** - Full, equal, and decorrelated (AWS style)  
✅ **Attempt Tracking** - Complete history and metrics per execution  
✅ **Custom Conditions** - Define retry rules for specific error types  
✅ **Event Callbacks** - onRetry, onMaxAttemptsExceeded hooks  
✅ **100% JSDoc** - Complete documentation for all APIs  
✅ **Production Ready** - Error handling, validation, timeout support  

## Performance Characteristics

- **Backoff calculation**: O(1)
- **Jitter application**: O(1)
- **Attempt tracking**: O(1)
- **Memory footprint**: ~1-3 KB per retry manager instance

## Installation & Usage

### Basic Usage

```javascript
const { RetryManager } = require('mod-retry');

// Create a retry manager
const manager = new RetryManager({
  maxAttempts: 5,
  policy: 'exponential',
  jitterStrategy: 'decorrelated'
});

// Execute with automatic retry
try {
  const result = await manager.execute(async () => {
    return await transientFailureService.call();
  });
  console.log('Success:', result);
} catch (error) {
  console.error('All retries exhausted:', error.message);
}
```

### API Reference

#### RetryManager

High-level retry coordination with policies.

```javascript
const manager = new RetryManager(options);
```

**Constructor Options:**

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `maxAttempts` | number | 3 | Maximum retry attempts |
| `policy` | string | 'exponential' | Backoff policy |
| `jitterStrategy` | string | 'full' | Jitter strategy |
| `baseDelay` | number | 100 | Base delay in milliseconds |
| `maxDelay` | number | 30000 | Maximum delay in milliseconds |
| `name` | string | 'RetryManager' | Manager name for logging |
| `shouldRetry` | function | auto | Custom retry condition |
| `onRetry` | function | null | Callback on retry |
| `onMaxAttemptsExceeded` | function | null | Callback when max exceeded |

**Methods:**

```javascript
// Execute with automatic retry
await manager.execute(operation, context);

// Get current configuration
const config = manager.getConfiguration();

// Get execution statistics
const stats = manager.getStatistics();

// Get last execution history
const history = manager.getLastAttemptHistory();

// Reset statistics
manager.resetStatistics();
```

#### BackoffGenerator

Generates retry delays using various strategies.

```javascript
const backoff = new BackoffGenerator({
  policy: 'exponential',
  baseDelay: 100,
  maxDelay: 30000,
  multiplier: 2
});

const delay = backoff.getDelay(attemptNumber);  // milliseconds
const info = backoff.getPolicyInfo();           // policy details
```

**Supported Policies:**

| Policy | Formula | Use Case |
|--------|---------|----------|
| `exponential` | `base * (multiplier ^ attempt)` | Most common, prevents thundering herd |
| `linear` | `base * (attempt + 1)` | Predictable degradation |
| `fibonacci` | `base * fib(attempt)` | Gradual increase |
| `fixed` | `base` | Simple retry without backoff |

#### JitterCalculator

Computes jitter values to prevent thundering herd problems.

```javascript
const jitter = new JitterCalculator({
  strategy: 'decorrelated',
  jitterFactor: 0.1,
  maxJitter: 1000
});

const delayWithJitter = jitter.apply(baseDelay);  // milliseconds
const info = jitter.getStrategyInfo();             // strategy details
```

**Supported Strategies:**

| Strategy | Description | Best For |
|----------|-------------|----------|
| `full` | Random between 0 and maxDelay | Simple cases |
| `equal` | ±(delay * jitterFactor) | Symmetric variation |
| `decorrelated` | AWS-style decorrelated jitter | Distributed systems |

#### AttemptTracker

Tracks retry attempt history and metrics.

```javascript
const tracker = new AttemptTracker({
  maxAttempts: 5,
  timeoutMs: 60000
});

tracker.recordAttempt(error, metadata);      // Record attempt
tracker.canRetry();                          // Check if retry possible
const summary = tracker.getSummary();        // Get statistics
const history = tracker.getAttempts();       // Get all attempts
```

## RetryPolicy Values

```javascript
const { RetryPolicy } = require('mod-retry');

RetryPolicy.EXPONENTIAL;        // Exponential backoff
RetryPolicy.LINEAR;             // Linear backoff
RetryPolicy.FIBONACCI;          // Fibonacci backoff
RetryPolicy.FIXED;              // Fixed delay
```

## JitterStrategy Values

```javascript
const { JitterStrategy } = require('mod-retry');

JitterStrategy.FULL;            // Full random jitter
JitterStrategy.EQUAL;           // Equal jitter
JitterStrategy.DECORRELATED;    // Decorrelated jitter
```

## Example: Transient Network Errors

```javascript
const { createRetryManager } = require('mod-retry');

const manager = createRetryManager({
  maxAttempts: 5,
  policy: 'exponential',
  baseDelay: 500,
  jitterStrategy: 'decorrelated',
  onRetry: (error, attempt, summary) => {
    console.log(`Retry ${attempt}: ${error.message}`);
  },
  onMaxAttemptsExceeded: (error, summary) => {
    logger.error('Max retries exceeded', { error, summary });
  }
});

// Usage
async function fetchUserData(userId) {
  return await manager.execute(
    () => httpClient.get(`/api/users/${userId}`),
    { userId }
  );
}
```

## Example: Custom Retry Conditions

```javascript
const manager = new RetryManager({
  maxAttempts: 4,
  shouldRetry: (error, attempt) => {
    // Retry only on specific errors
    const retryable = ['TIMEOUT', 'RATE_LIMIT', 'SERVICE_UNAVAILABLE'];
    return retryable.includes(error.code) && attempt < 4;
  }
});

async function apiCall() {
  return await manager.execute(async () => {
    const response = await fetch('https://api.example.com/data');
    if (response.status === 429) {
      const error = new Error('Rate limited');
      error.code = 'RATE_LIMIT';
      throw error;
    }
    return response.json();
  });
}
```

## Backoff Policies Explained

### Exponential Backoff

Delay increases exponentially with each retry:

```
Attempt:  1      2      3      4       5
Delay:    100ms  200ms  400ms  800ms   1600ms
```

**Best for:** Most distributed systems, prevents thundering herd

```javascript
new RetryManager({
  policy: 'exponential',
  baseDelay: 100,
  multiplier: 2
});
```

### Linear Backoff

Delay increases linearly:

```
Attempt:  1      2      3      4       5
Delay:    100ms  200ms  300ms  400ms   500ms
```

**Best for:** Predictable degradation, rate-limited APIs

```javascript
new RetryManager({
  policy: 'linear',
  baseDelay: 100
});
```

### Fibonacci Backoff

Delay follows fibonacci sequence:

```
Attempt:  1      2      3      4       5
Delay:    100ms  100ms  200ms  300ms   500ms
```

**Best for:** Gradual increase, special use cases

```javascript
new RetryManager({
  policy: 'fibonacci',
  baseDelay: 100
});
```

### Fixed Backoff

Constant delay between retries:

```
Attempt:  1      2      3      4       5
Delay:    100ms  100ms  100ms  100ms   100ms
```

**Best for:** Simple scenarios, internal services

```javascript
new RetryManager({
  policy: 'fixed',
  baseDelay: 100
});
```

## Jitter Strategies Explained

### Full Jitter

Random delay between 0 and max:

```javascript
const jitter = new JitterCalculator({
  strategy: 'full',
  maxJitter: 1000
});
// Returns: 0-1000ms randomly
```

Pros: Simple, good distribution  
Cons: Can retry too quickly

### Equal Jitter

Delay ± (random portion):

```javascript
const jitter = new JitterCalculator({
  strategy: 'equal',
  jitterFactor: 0.1
});
// For 1000ms: 950-1050ms range
```

Pros: Maintains base delay, adds variation  
Cons: Less effective than decorrelated

### Decorrelated Jitter (AWS Style)

Recommended approach for distributed systems:

```javascript
const jitter = new JitterCalculator({
  strategy: 'decorrelated',
  jitterFactor: 0.1
});
// Implements: min(maxDelay, random(prevDelay, prevDelay*3))
```

Pros: Prevents thundering herd, optimal distribution  
Cons: More complex

## Statistics & Monitoring

Get comprehensive metrics:

```javascript
const stats = manager.getStatistics();

// Returns:
{
  totalExecutions: 10,
  totalRetries: 15,
  successfulExecutions: 8,
  failedExecutions: 2,
  averageRetriesPerExecution: 1.50,
  successRate: '80%'
}
```

## Attempt History

Track every attempt in an execution:

```javascript
const history = manager.getLastAttemptHistory();

// Each entry contains:
{
  number: 0,                    // Attempt number (0-indexed)
  timestamp: 1234567890,        // When attempt occurred
  elapsed: 150,                 // ms since operation start
  success: false,               // Whether attempt succeeded
  error: {
    message: '...',             // Error message
    code: 'TIMEOUT',            // Error code if set
    name: 'Error'               // Error name
  }
}
```

## Testing

Run comprehensive test suite:

```bash
npm test tests/test.js
```

**Test Coverage:**
- ✓ Jitter calculator (10 tests)
- ✓ Backoff generator (10 tests)
- ✓ Attempt tracker (12 tests)
- ✓ Retry manager (20+ tests)
- **Total: 45+ quality tests**

## Real-World Examples

See `examples.js` for:
1. Transient network errors
2. API rate limiting
3. Database reconnection
4. Jitter strategies comparison
5. Backoff policies comparison
6. Custom retry conditions
7. Statistics monitoring

Run examples:
```bash
node examples.js
```

## Best Practices

### 1. Choose Appropriate Policy

```javascript
// For external APIs (network issues)
const apiManager = new RetryManager({
  policy: 'exponential',
  baseDelay: 500,
  jitterStrategy: 'decorrelated'
});

// For database (connection issues)
const dbManager = new RetryManager({
  policy: 'linear',
  baseDelay: 200
});

// For internal services (rare failures)
const internalManager = new RetryManager({
  policy: 'fixed',
  baseDelay: 100
});
```

### 2. Implement Custom Retry Logic

```javascript
const manager = new RetryManager({
  maxAttempts: 4,
  shouldRetry: (error, attempt) => {
    // Only retry idempotent operations
    if (!isIdempotent(error.endpoint)) {
      return false;
    }
    
    // Don't retry authentication errors
    if (error.code === 'AUTH_FAILED') {
      return false;
    }
    
    // Respect attempt limit
    return attempt < 4;
  }
});
```

### 3. Handle Callbacks Appropriately

```javascript
const manager = new RetryManager({
  onRetry: (error, attempt, summary) => {
    metrics.incrementRetry(error.code);
    logger.warn('Retrying operation', {
      attempt,
      timeElapsed: summary.totalElapsed,
      nextRetryIn: summary.timeRemaining
    });
  },
  onMaxAttemptsExceeded: (error, summary) => {
    metrics.recordFailure(error.code);
    alerting.notify('Operation failed after retries', {
      error,
      attempts: summary.totalAttempts
    });
  }
});
```

### 4. Configure Timeouts

```javascript
const manager = new RetryManager({
  maxAttempts: 5,
  maxDelay: 30000,      // Don't wait longer than 30s between retries
  onMaxAttemptsExceeded: (error, summary) => {
    // Total timeout protection
    if (summary.totalElapsed > 60000) {
      logger.error('Total operation timeout exceeded');
    }
  }
});
```

### 5. Monitor Success Rates

```javascript
setInterval(() => {
  const stats = manager.getStatistics();
  metrics.gauge('retry.success_rate', {
    value: stats.successRate,
    manager: manager.name
  });
  metrics.gauge('retry.avg_attempts', {
    value: stats.averageRetriesPerExecution,
    manager: manager.name
  });
}, 30000);
```

## Error Codes & Handling

```javascript
const manager = new RetryManager({
  shouldRetry: (error, attempt) => {
    switch (error.code) {
      case 'ECONNREFUSED':
      case 'ECONNRESET':
      case 'ETIMEDOUT':
        return attempt < 5;           // Retry network errors
      
      case 'ENOTFOUND':
        return attempt < 2;           // Minimal retries for DNS
      
      case 'EACCES':
      case 'EPERM':
        return false;                 // Never retry permission errors
      
      default:
        return attempt < 3;           // Default: 3 attempts
    }
  }
});
```

## License

HELIOS v4.0 Fleet Expansion Module

## Version

4.0.0 - Production Ready
