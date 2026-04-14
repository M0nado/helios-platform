# feat-recovery - Error Recovery Module

Production-ready error recovery implementation with retry logic, exponential backoff, fallback strategies, and graceful degradation. Prevents cascading failures and enables resilient systems.

## Features

- **RetryPolicy**: Configurable retry logic with exponential backoff
- **ExponentialBackoff**: Multiple backoff strategies (standard, full jitter, equal jitter)
- **FallbackStrategy**: Chain fallback strategies for failover
- **GracefulDegradation**: Maintain partial functionality under load
- **100% JSDoc**: Full API documentation on every function
- **Production Ready**: Comprehensive error handling and validation

## Installation

```bash
npm install @helios/feat-recovery
```

## Quick Start

```javascript
const { RetryPolicy, ExponentialBackoff, FallbackStrategy, GracefulDegradation } = require('@helios/feat-recovery');

// Simple retry with exponential backoff
const policy = new RetryPolicy({ maxRetries: 3, initialDelay: 1000 });

const result = await policy.execute(async () => {
  const response = await fetch('https://api.example.com/data');
  if (!response.ok) {
    throw new Error(`HTTP ${response.status}`);
  }
  return response.json();
});

// Fallback to alternative services
const fallback = new FallbackStrategy([
  async () => fetch('https://primary.example.com/data'),
  async () => fetch('https://secondary.example.com/data'),
  async () => ({ cached: true, data: getCachedData() })
]);

const data = await fallback.execute();

// Graceful degradation under load
const degradation = new GracefulDegradation({
  full: { includeDetails: true, timeout: 5000 },
  partial: { includeDetails: false, timeout: 2000 },
  minimal: { onlyEssentials: true, timeout: 1000 }
});

const result = await degradation.execute('full,partial,minimal', async (mode) => {
  return await fetchUserProfile(userId, mode);
});
```

## API Reference

### RetryPolicy

Configurable retry logic with exponential backoff and custom conditions.

#### Constructor

```javascript
new RetryPolicy(options)
```

**Options:**
- `maxRetries` (number): Maximum retry attempts. Default: 3
- `initialDelay` (number): Initial delay in milliseconds. Default: 1000
- `maxDelay` (number): Maximum delay cap in milliseconds. Default: 30000
- `retryableStatuses` (array): HTTP status codes to retry. Default: [408,429,500,502,503,504]
- `shouldRetry` (function): Custom retry condition function. Default: built-in detection

**Performance:** O(1) per retry decision, O(n) total where n = maxRetries

#### Methods

**execute(fn, args) → Promise**

Execute function with retry logic.

```javascript
try {
  const result = await policy.execute(
    async (url) => {
      const response = await fetch(url);
      if (!response.ok) throw new Error(`Status ${response.status}`);
      return response.json();
    },
    ['https://api.example.com/data']
  );
  console.log('Success:', result);
} catch (error) {
  console.error('Failed after retries:', error.message);
}
```

**Retryable Errors:**
- HTTP status 408 (Request Timeout)
- HTTP status 429 (Too Many Requests)
- HTTP status 500 (Internal Server Error)
- HTTP status 502 (Bad Gateway)
- HTTP status 503 (Service Unavailable)
- HTTP status 504 (Gateway Timeout)
- Connection errors: ECONNREFUSED, ECONNRESET, ETIMEDOUT, EHOSTUNREACH

### ExponentialBackoff

Multiple exponential backoff strategies for distributed systems.

#### Constructor

```javascript
new ExponentialBackoff(options)
```

**Options:**
- `baseDelay` (number): Base delay in milliseconds. Default: 1000
- `maxDelay` (number): Maximum delay cap in milliseconds. Default: 60000
- `multiplier` (number): Backoff multiplier. Default: 2
- `jitterFactor` (number): Jitter as fraction 0-1. Default: 0.1

**Performance:** O(1) backoff calculation

#### Methods

**calculateDelay(attempt) → number**

Calculate delay with standard exponential backoff + jitter.

```javascript
const backoff = new ExponentialBackoff();
const delay = backoff.calculateDelay(0); // ~1000ms
const delay = backoff.calculateDelay(1); // ~2000ms
const delay = backoff.calculateDelay(2); // ~4000ms
```

**calculateDelayFullJitter(attempt) → number**

Calculate delay with full jitter strategy (better for concurrent retries).

```javascript
const backoff = new ExponentialBackoff();
const delay = backoff.calculateDelayFullJitter(attempt);
// Returns random value between 0 and capped exponential delay
```

**calculateDelayEqualJitter(attempt) → number**

Calculate delay with equal jitter strategy (balanced approach).

```javascript
const backoff = new ExponentialBackoff();
const delay = backoff.calculateDelayEqualJitter(attempt);
// Returns value = jitterRange/2 + random(jitterRange/2)
```

**execute(fn, maxAttempts) → Promise**

Execute function with exponential backoff.

```javascript
const result = await backoff.execute(
  async () => {
    const response = await fetch('https://api.example.com/data');
    if (!response.ok) throw new Error('Request failed');
    return response.json();
  },
  3 // Try 3 times
);
```

**Backoff Examples:**
```
Attempt 0: ~1000ms (1000 + jitter)
Attempt 1: ~2000ms (2000 + jitter)
Attempt 2: ~4000ms (4000 + jitter)
Attempt 3: ~8000ms (capped at 60000)
```

### FallbackStrategy

Chain multiple fallback strategies for resilient failover.

#### Constructor

```javascript
new FallbackStrategy(strategies, options)
```

**Parameters:**
- `strategies` (array): Array of async functions to try
- `options.allMustFail` (boolean): If true, throw only after all fail. Default: true

**Performance:** O(n) where n = number of strategies tried

#### Methods

**execute() → Promise**

Execute fallback chain, trying strategies in order.

```javascript
const strategies = [
  async () => fetch('https://primary.example.com/data'),
  async () => fetch('https://secondary.example.com/data'),
  async () => ({ cached: true, data: cache.get('last-response') })
];

const fallback = new FallbackStrategy(strategies);

try {
  const result = await fallback.execute();
  console.log('Succeeded with strategy:', fallback.getExecutionLog().find(e => e.success).strategyIndex);
} catch (error) {
  console.error('All strategies failed:', error.causes);
}
```

**executeWithTimeout(timeout) → Promise**

Execute with per-strategy timeout.

```javascript
const fallback = new FallbackStrategy([
  slowServiceA,
  slowServiceB,
  fastServiceC
]);

try {
  const result = await fallback.executeWithTimeout(5000);
  console.log('Result:', result);
} catch (error) {
  console.error('Timeout or all failed:', error.message);
}
```

**getExecutionLog() → Array**

Get log of all strategy executions.

```javascript
const log = fallback.getExecutionLog();
// [
//   { strategyIndex: 0, success: false, error: 'Connection refused' },
//   { strategyIndex: 1, success: false, error: 'Timeout' },
//   { strategyIndex: 2, success: true, result: {...} }
// ]
```

### GracefulDegradation

Degrade functionality gracefully when primary mode fails.

#### Constructor

```javascript
new GracefulDegradation(modes, options)
```

**Parameters:**
- `modes` (object): Named degradation modes with configurations
- `options.defaultMode` (string): Starting mode. Default: 'full'

**Performance:** O(1) degradation check

#### Methods

**execute(modeChain, fn) → Promise**

Execute with degradation fallback.

```javascript
const degradation = new GracefulDegradation({
  full: { includeDetails: true, timeout: 5000 },
  partial: { includeDetails: false, timeout: 2000 },
  minimal: { onlyEssentials: true, timeout: 1000 }
});

const result = await degradation.execute('full,partial,minimal', async (mode) => {
  return await fetchUserData(userId, {
    timeout: mode.timeout,
    detailed: mode.includeDetails
  });
});
```

**isDegraded() → boolean**

Check if system is operating in degraded mode.

```javascript
if (degradation.isDegraded()) {
  console.log('System is degraded, some features limited');
}
```

**getStatus() → Object**

Get current degradation status.

```javascript
const status = degradation.getStatus();
// {
//   currentMode: 'partial',
//   isDegraded: true,
//   failureCount: 5,
//   successCount: 100,
//   healthRatio: 0.95
// }
```

**reset() → void**

Reset degradation state.

```javascript
degradation.reset(); // Back to 'full' mode
```

**getLog() → Array**

Get degradation event log (max 1000 entries).

```javascript
const log = degradation.getLog();
// [
//   { timestamp: 1234567890, mode: 'full', success: true },
//   { timestamp: 1234567891, mode: 'full', success: false, error: 'Timeout' },
//   { timestamp: 1234567892, mode: 'partial', success: true }
// ]
```

## Usage Examples

### Transient Failure Retry Example

```javascript
const policy = new RetryPolicy({
  maxRetries: 5,
  initialDelay: 500,
  maxDelay: 10000
});

const fetchUserData = async (userId) => {
  return await policy.execute(async () => {
    const response = await fetch(`/api/users/${userId}`);
    if (!response.ok) {
      throw { status: response.status, message: `HTTP ${response.status}` };
    }
    return response.json();
  });
};

// Usage
try {
  const user = await fetchUserData(123);
  console.log('User:', user);
} catch (error) {
  console.error('Failed after retries:', error);
}
```

### Circuit Breaking Example

```javascript
class CircuitBreaker {
  constructor(policy) {
    this.policy = policy;
    this.failureCount = 0;
    this.successCount = 0;
    this.isOpen = false;
    this.lastFailureTime = null;
  }

  async execute(fn) {
    if (this.isOpen) {
      const elapsed = Date.now() - this.lastFailureTime;
      if (elapsed > 60000) { // 1 minute
        this.isOpen = false;
        this.failureCount = 0;
      } else {
        throw new Error('Circuit breaker is open');
      }
    }

    try {
      const result = await this.policy.execute(fn);
      this.successCount++;
      if (this.failureCount > 0) this.failureCount--;
      return result;
    } catch (error) {
      this.failureCount++;
      this.lastFailureTime = Date.now();
      if (this.failureCount >= 5) {
        this.isOpen = true;
      }
      throw error;
    }
  }
}

// Usage
const breaker = new CircuitBreaker(new RetryPolicy());
try {
  const result = await breaker.execute(() => riskyOperation());
} catch (error) {
  console.error('Operation failed:', error);
}
```

### Fallback Services Example

```javascript
const fallback = new FallbackStrategy([
  async () => {
    console.log('Trying primary API...');
    return await fetch('https://api.primary.com/data').then(r => r.json());
  },
  async () => {
    console.log('Trying secondary API...');
    return await fetch('https://api.secondary.com/data').then(r => r.json());
  },
  async () => {
    console.log('Using cached data...');
    return cache.get('api-data') || { message: 'No data available' };
  }
]);

try {
  const data = await fallback.execute();
  console.log('Data retrieved:', data);
  const log = fallback.getExecutionLog();
  console.log(`Used strategy #${log.find(e => e.success).strategyIndex}`);
} catch (error) {
  console.error('All strategies failed:', error.message);
}
```

### Degraded Modes Example

```javascript
const degradation = new GracefulDegradation({
  full: {
    includeDetails: true,
    includeComments: true,
    timeout: 5000
  },
  partial: {
    includeDetails: true,
    includeComments: false,
    timeout: 2000
  },
  minimal: {
    includeDetails: false,
    includeComments: false,
    timeout: 1000
  }
});

const fetchPost = async (postId) => {
  return await degradation.execute('full,partial,minimal', async (mode) => {
    const response = await fetch(`/api/posts/${postId}`, {
      timeout: mode.timeout,
      params: {
        details: mode.includeDetails ? 'full' : 'minimal',
        comments: mode.includeComments
      }
    });
    if (!response.ok) throw new Error('Request failed');
    return response.json();
  });
};

// Usage
const post = await fetchPost(42);
if (degradation.isDegraded()) {
  console.log('Note: Some features are limited due to high load');
}
console.log(degradation.getStatus());
// { currentMode: 'partial', isDegraded: true, healthRatio: 0.92 }
```

## Performance Characteristics

- **Retry Execution**: O(n) where n = number of retries
- **Backoff Calculation**: O(1) constant time
- **Fallback Execution**: O(n) where n = number of strategies tried
- **Degradation Check**: O(1) constant time

## Error Handling

All methods include comprehensive error handling:

```javascript
try {
  const policy = new RetryPolicy({ maxRetries: -1 }); // Invalid
} catch (error) {
  console.error('Configuration error:', error.message);
}

try {
  await policy.execute('not-a-function'); // Invalid
} catch (error) {
  console.error('Type error:', error.message);
}

try {
  const fallback = new FallbackStrategy([]); // Empty
} catch (error) {
  console.error('Strategy error:', error.message);
}
```

## License

MIT
