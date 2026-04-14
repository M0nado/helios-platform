# HELIOS v4.0 Rate Limiter Module

Distributed rate limiting with quota enforcement, sliding windows, and comprehensive metric tracking. Production-ready with Redis support, flexible quota dimensions, and real-time monitoring.

## Features

- **Rate Limiting**: Per-IP, per-user, per-endpoint rate limiting
- **Distributed Mode**: Redis-backed for multi-server deployment
- **Quota Enforcement**: Multi-dimensional quota management
- **Metrics**: Prometheus export, real-time statistics
- **Sliding Windows**: Accurate rate limiting over time windows
- **Production-Ready**: Full JSDoc, error handling, validation

## Installation

```javascript
const { createSimpleLimiter, createRateLimiter, createMiddleware } = require('./index');
```

## Quick Start

```javascript
const limiter = createSimpleLimiter({
  maxRequests: 100,
  windowMs: 60000 // 1 minute
});

// Check if request is allowed
const status = await limiter.check(req);

if (status.allowed) {
  // Process request
} else {
  // Reject with 429 Too Many Requests
  res.status(429).json({
    error: 'Too many requests',
    retryAfter: status.resetIn
  });
}

// Get metrics
const summary = limiter.getMetrics();
// { totalAllowed, totalBlocked, blockRate, ... }
```

## API Reference

### DistributedLimiter

Manages distributed rate limiting across multiple nodes.

#### Constructor

```javascript
const limiter = new DistributedLimiter(options);
// options:
//   - maxRequests: number (default: 100) - Max requests per window
//   - windowMs: number (default: 60000) - Time window in ms
//   - distributed: boolean (default: false) - Use distributed mode
//   - backend: Object - Storage backend (Redis/Memory)
//   - identifierFn: Function - Extract identifier from request
//   - keyPrefix: string (default: 'limiter:') - Redis key prefix
```

#### Methods

##### `check(req, options?)`

Check if request is allowed within rate limit.

```javascript
const status = await limiter.check({ ip: '192.168.1.1' });

// Returns:
// {
//   allowed: boolean,
//   limit: number,
//   current: number,
//   remaining: number,
//   resetTime: number,
//   resetIn: number,
//   identifier: string
// }
```

**Parameters:**
- `req` (Object): Request object
- `options` (Object, optional):
  - `increment`: Boolean to increment counter (default: true)

**Returns:** Rate limit status object

**Throws:** `RateLimitError` on backend failure

##### `status(req)`

Get current status without incrementing.

```javascript
const status = await limiter.status({ ip: '192.168.1.1' });
```

##### `reset(identifier)`

Reset rate limit for an identifier.

```javascript
await limiter.reset('192.168.1.1');
```

##### `resetAll()`

Reset all rate limits.

```javascript
await limiter.resetAll();
```

##### `stats()`

Get rate limit statistics.

```javascript
const stats = await limiter.stats();
// { windowMs, maxRequests, activeIdentifiers, totalRequests, ... }
```

##### `setIdentifierFn(fn)`

Set custom identifier function.

```javascript
limiter.setIdentifierFn((req) => req.userId || req.ip);
```

##### `updateConfig(config)`

Update rate limit configuration.

```javascript
limiter.updateConfig({ maxRequests: 50, windowMs: 30000 });
```

### QuotaEnforcer

Manages quota limits and enforcement.

#### Constructor

```javascript
const enforcer = new QuotaEnforcer(options);
// options:
//   - strict: boolean (default: true) - Enforce hard quotas
//   - onQuotaExceeded: Function - Callback on quota exceeded
//   - backend: Object - Storage backend
```

#### Methods

##### `define(name, config)`

Define a quota with limits.

```javascript
enforcer.define('api_calls', {
  limit: 10000,
  resetMs: 86400000, // 24 hours
  warnThreshold: 80,
  dimensions: ['userId']
});

enforcer.define('storage', {
  limit: 1073741824, // 1GB
  resetMs: 2592000000, // 30 days
  warnThreshold: 90,
  dimensions: ['userId', 'bucket']
});
```

**Parameters:**
- `name` (string): Quota name
- `config` (Object):
  - `limit`: Number - Quota limit
  - `resetMs`: Number - Reset interval in milliseconds
  - `warnThreshold`: Number - Warning threshold (0-100)
  - `dimensions`: Array - Context dimensions

##### `enforce(quotaName, context, amount?)`

Check and enforce quota.

```javascript
const status = await enforcer.enforce('api_calls', 
  { userId: 'user-123' }, 
  1
);

// Returns:
// {
//   quotaName: string,
//   context: Object,
//   limit: number,
//   used: number,
//   remaining: number,
//   percentUsed: number,
//   exceeded: boolean,
//   warned: boolean,
//   resetAt: number
// }
```

**Parameters:**
- `quotaName` (string): Quota name
- `context` (Object): Context with dimensions
- `amount` (number, optional): Amount to deduct (default: 1)

**Returns:** Quota status

**Throws:** `QuotaExceededError` if quota exceeded and strict mode

##### `getUsage(quotaName, context)`

Get quota usage.

```javascript
const usage = await enforcer.getUsage('api_calls', { userId: 'user-123' });
// { quotaName, limit, used, remaining, percentUsed }
```

##### `resetUsage(quotaName, context?)`

Reset quota usage.

```javascript
await enforcer.resetUsage('api_calls', { userId: 'user-123' });
await enforcer.resetUsage('api_calls'); // Reset all
```

##### `getQuotas()`

Get all quota definitions.

```javascript
const quotas = enforcer.getQuotas();
```

### MetricsTracker

Tracks and exports rate limiting metrics.

#### Constructor

```javascript
const tracker = new MetricsTracker(options);
// options:
//   - enabled: boolean (default: true)
//   - historySize: number (default: 1000)
//   - labels: Array - Labels to track
```

#### Methods

##### `record(event)`

Record a rate limit event.

```javascript
tracker.record({
  type: 'allowed', // or 'blocked', 'quota_exceeded'
  context: { ip: '192.168.1.1', method: 'GET', path: '/api/users' }
});
```

##### `getMetrics(context)`

Get metrics for a context.

```javascript
const metrics = tracker.getMetrics({ ip: '192.168.1.1' });
// { allowed, blocked, quotaExceeded, totalRequests, lastUpdate }
```

##### `getAllMetrics()`

Get all metrics.

```javascript
const all = tracker.getAllMetrics();
```

##### `exportPrometheus()`

Export metrics in Prometheus format.

```javascript
const prometheus = tracker.exportPrometheus();
// rate_limiter_allowed{ip="..."} 100
// rate_limiter_blocked{ip="..."} 5
```

##### `exportJSON()`

Export metrics as JSON.

```javascript
const json = tracker.exportJSON();
// { timestamp, uptime, metrics, recentEvents }
```

##### `getSummary()`

Get metrics summary.

```javascript
const summary = tracker.getSummary();
// { totalAllowed, totalBlocked, blockRate, activeIdentifiers }
```

##### `clear()`

Clear all metrics.

```javascript
tracker.clear();
```

## Usage Examples

### Simple Per-IP Rate Limiting

```javascript
const { createSimpleLimiter } = require('./index');

const limiter = createSimpleLimiter({
  maxRequests: 100,
  windowMs: 60000
});

// In Express middleware
app.use(async (req, res, next) => {
  const status = await limiter.check(req);

  res.setHeader('X-RateLimit-Limit', status.limit);
  res.setHeader('X-RateLimit-Remaining', status.remaining);

  if (!status.allowed) {
    return res.status(429).json({
      error: 'Too many requests',
      retryAfter: Math.ceil(status.resetIn / 1000)
    });
  }

  next();
});
```

### Distributed Rate Limiting

```javascript
const { DistributedLimiter } = require('./index');

const limiter = new DistributedLimiter({
  maxRequests: 1000,
  windowMs: 60000,
  distributed: true,
  backend: redisClient,
  identifierFn: (req) => req.userId || req.ip
});
```

### Quota Management

```javascript
const { QuotaEnforcer } = require('./index');

const enforcer = new QuotaEnforcer({ strict: false });

// Define quotas
enforcer.define('api_calls', {
  limit: 10000,
  resetMs: 86400000,
  dimensions: ['userId']
});

enforcer.define('storage', {
  limit: 10737418240, // 10GB
  resetMs: 2592000000,
  dimensions: ['userId', 'bucket']
});

// Enforce quotas
const apiStatus = await enforcer.enforce('api_calls', { userId: 'u1' }, 1);
const storageStatus = await enforcer.enforce('storage', 
  { userId: 'u1', bucket: 'docs' }, 
  104857600 // 100MB
);

if (apiStatus.warned) {
  console.log('User approaching API quota');
}

if (storageStatus.exceeded) {
  console.log('User exceeded storage quota');
}
```

### Metrics Export

```javascript
const { MetricsTracker } = require('./index');

const metrics = new MetricsTracker({
  labels: ['ip', 'endpoint', 'method']
});

// Record events
metrics.record({
  type: 'allowed',
  context: { ip: '192.168.1.1', endpoint: '/api/users', method: 'GET' }
});

// Export
console.log(metrics.exportPrometheus()); // For Prometheus
console.log(metrics.exportJSON()); // For logs
console.log(metrics.getSummary()); // Quick overview
```

### Express Middleware

```javascript
const { createMiddleware } = require('./index');

const rateLimitMiddleware = createMiddleware({
  maxRequests: 100,
  windowMs: 60000,
  onLimitExceeded: (req, res, status) => {
    res.status(429).json({
      error: 'Too many requests',
      retryAfter: Math.ceil(status.resetIn / 1000),
      limit: status.limit,
      remaining: status.remaining
    });
  }
});

app.use(rateLimitMiddleware);
```

## Performance Characteristics

- **Rate Check**: O(1) with Redis, O(n) cleanup with in-memory backend
- **Quota Enforcement**: O(1) checking, O(m) where m = quota dimensions
- **Metrics Recording**: O(1) per event
- **Memory**: ~50B per limiter, ~100B per quota, ~1KB per metric

## Distribution

For distributed deployments:

1. Use Redis backend with `distributed: true`
2. Configure key prefix for namespace isolation
3. Redis handles consistency across servers
4. Metrics can be aggregated from multiple servers

## Testing

Run tests with:

```bash
npm test
```

Coverage includes:
- 14 DistributedLimiter tests
- 12 QuotaEnforcer tests
- 11 MetricsTracker tests
- 6 MemoryBackend tests
- 10 Integration tests
- 5 Error handling tests

Total: 58+ comprehensive tests

## License

HELIOS v4.0 - Enterprise Rate Limiter Module
