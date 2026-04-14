# HELIOS v4.0 Rate Limiting Module (65 KB)

Production-ready rate limiting with multiple algorithms for comprehensive traffic control.

## Features

- **Token Bucket**: Burst-friendly rate limiting with configurable capacity and refill rate
- **Sliding Window**: Precise request counting within rolling time windows
- **Distributed Limiting**: Multi-instance coordination for distributed systems
- **Quota Management**: Monthly/configurable billing cycle quota tracking
- **100% JSDoc**: Complete API documentation with examples
- **54 Quality Tests**: Comprehensive test coverage for all scenarios
- **Performance**: O(1) operations for all core algorithms

## Installation

```javascript
const {
  TokenBucket,
  SlidingWindow,
  DistributedLimiter,
  QuotaManager
} = require('./implementation');
```

## API Documentation

### TokenBucket

```javascript
const bucket = new TokenBucket(capacity, refillRate, [initialTokens]);
```

**Parameters:**
- `capacity` (number): Maximum tokens the bucket can hold
- `refillRate` (number): Tokens added per second
- `initialTokens` (number, optional): Starting token count (default: capacity)

**Methods:**

```javascript
// Consume tokens (returns true if successful)
bucket.consume(count = 1) // boolean

// Get available tokens without consuming
bucket.getTokens() // number

// Get wait time until tokens available
bucket.getWaitTime(count = 1) // number (ms)

// Reset to initial state
bucket.reset() // void

// Get statistics
bucket.getStats() // { capacity, tokens, refillRate, usage }
```

**Example:**
```javascript
const bucket = new TokenBucket(100, 10); // 100 capacity, 10 tokens/sec
if (bucket.consume(5)) {
  // Process request
} else {
  const wait = bucket.getWaitTime(5);
  // Retry after 'wait' milliseconds
}
```

### SlidingWindow

```javascript
const window = new SlidingWindow(windowSize, maxRequests);
```

**Parameters:**
- `windowSize` (number): Time window in milliseconds
- `maxRequests` (number): Maximum requests in window

**Methods:**

```javascript
// Check if request allowed without recording
window.isAllowed() // boolean

// Record a request (returns true if allowed)
window.record() // boolean

// Get current request count
window.getCount() // number

// Get wait time until next request allowed
window.getWaitTime() // number (ms)

// Reset window
window.reset() // void

// Get statistics
window.getStats() // { windowSize, maxRequests, currentCount, remaining, waitTime }
```

**Example:**
```javascript
const window = new SlidingWindow(60000, 100); // 60s window, 100 requests
if (window.record()) {
  // Process request
} else {
  // Wait for window expiry
}
```

### DistributedLimiter

```javascript
const limiter = new DistributedLimiter(backend, keyPrefix, windowSize, maxRequests);
```

**Parameters:**
- `backend` (Object, optional): Storage backend (Map by default)
- `keyPrefix` (string): Prefix for storage keys
- `windowSize` (number): Window size in milliseconds
- `maxRequests` (number): Max requests per window

**Methods:**

```javascript
// Check and increment limit
limiter.checkLimit(identifier) // { allowed, remaining, resetTime, retryAfter }

// Reset limit for identifier
limiter.reset(identifier) // void

// Get current status
limiter.getStatus(identifier) // { count, remaining, resetTime }

// Cleanup expired entries
limiter.cleanup() // number (cleaned count)

// Get all active limiters
limiter.getAll() // Map
```

**Example:**
```javascript
const limiter = new DistributedLimiter(redisBackend, 'api:', 60000, 100);
const result = limiter.checkLimit(clientIP);
if (!result.allowed) {
  response.status(429).json({
    error: 'Rate limit exceeded',
    retryAfter: Math.ceil(result.retryAfter / 1000)
  });
}
```

### QuotaManager

```javascript
const manager = new QuotaManager([storage], [billingCycleDays]);
```

**Parameters:**
- `storage` (Object, optional): Storage backend
- `billingCycleDays` (number): Days in billing cycle (default: 30)

**Methods:**

```javascript
// Set quota for user
manager.setQuota(userId, limit, [softLimit]) // void

// Consume quota
manager.consume(userId, amount = 1) // { allowed, used, remaining, limit, status, cycleResetAt }

// Get quota information
manager.getQuota(userId) // { limit, used, remaining, percentage, softLimit, status, ... }

// Reset quota mid-cycle
manager.resetQuota(userId) // void

// Get all quotas
manager.getAllQuotas() // Map

// Get users with warnings
manager.getWarnings() // Array<{ userId, used, limit, percentage }>
```

**Example:**
```javascript
const manager = new QuotaManager();
manager.setQuota('user123', 10000, 8000); // 10k limit, 8k soft limit

const result = manager.consume('user123', 100);
if (!result.allowed) {
  console.log('Quota exceeded');
} else if (result.status === 'warning') {
  console.log(`Warning: ${result.percentage}% of quota used`);
}
```

## Performance Characteristics

| Algorithm | Check Time | Memory | Use Case |
|-----------|-----------|--------|----------|
| TokenBucket | O(1) | O(1) | Bursty traffic, API rate limiting |
| SlidingWindow | O(n) | O(n) | Precise counting, webhook limiting |
| DistributedLimiter | O(1) | O(k) | Multi-instance coordination |
| QuotaManager | O(1) | O(u) | Monthly billing, usage tracking |

**Note:** n = requests in window, k = active identifiers, u = users

## Error Handling

All modules include comprehensive error handling:

```javascript
try {
  const bucket = new TokenBucket(0, 1); // Invalid
} catch (error) {
  console.error(error.message); // "Capacity must be a positive finite number"
}
```

## Real-World Scenarios

### 1. Per-User API Rate Limiting
```javascript
const buckets = new Map();
function getLimit(userId) {
  if (!buckets.has(userId)) {
    buckets.set(userId, new TokenBucket(1000, 100));
  }
  return buckets.get(userId);
}

const bucket = getLimit('user123');
if (bucket.consume(1)) {
  // Process API call
}
```

### 2. Webhook Delivery with Sliding Window
```javascript
const webhook = new SlidingWindow(60000, 50); // 50/minute
if (webhook.record()) {
  deliverWebhook();
} else {
  scheduleRetry(webhook.getWaitTime());
}
```

### 3. Multi-Instance Load Balancer
```javascript
const limiter = new DistributedLimiter(redisStore);
const result = limiter.checkLimit(request.clientIP);
response.setHeader('X-RateLimit-Remaining', result.remaining);
if (!result.allowed) {
  return response.status(429).send('Rate Limited');
}
```

### 4. SaaS Usage Tracking
```javascript
const quota = new QuotaManager();
quota.setQuota('org-123', 100000); // 100k monthly

const result = quota.consume('org-123', 150);
if (!result.allowed) {
  return { error: 'Upgrade required' };
}
```

## Testing

The module includes 54 comprehensive tests:

- **TokenBucket**: 15 tests covering capacity, refill, consumption, reset
- **SlidingWindow**: 13 tests for window expiry, cleanup, limits
- **DistributedLimiter**: 11 tests for distributed scenarios
- **QuotaManager**: 15 tests for quotas, billing cycles, warnings

Run tests with your preferred test framework:

```bash
mocha tests/tests.js
```

## Best Practices

1. **Choose the Right Algorithm**
   - TokenBucket: For APIs with burst allowance
   - SlidingWindow: For strict per-minute/hour limits
   - DistributedLimiter: For multiple servers
   - QuotaManager: For billing/subscription tracking

2. **Monitor and Alert**
   ```javascript
   const stats = bucket.getStats();
   if (stats.usage > 0.9) {
    alertHighUsage();
   }
   ```

3. **Cleanup Distributed Entries**
   ```javascript
   // Periodically cleanup expired entries
   setInterval(() => limiter.cleanup(), 300000); // Every 5 minutes
   ```

4. **Handle Edge Cases**
   - Always validate identifier/userId parameters
   - Check return values before processing
   - Set appropriate timeout/retry logic

## License

HELIOS v4.0 - Proprietary

## Support

For issues or feature requests, contact the HELIOS development team.
