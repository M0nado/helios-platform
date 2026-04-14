/**
 * HELIOS v4.0 Rate Limiter - Usage Examples
 * Real-world scenarios and best practices
 */

const { createRateLimiter, createSimpleLimiter, createMiddleware, DistributedLimiter, QuotaEnforcer, MetricsTracker } = require('./index');

// ============================================================================
// Example 1: Simple Per-IP Rate Limiting
// ============================================================================
console.log('\n=== Example 1: Simple Per-IP Rate Limiting ===\n');

const simpleLimiter = createSimpleLimiter({
  maxRequests: 100,
  windowMs: 60000 // 1 minute
});

(async () => {
  // Simulate requests
  const req = { ip: '192.168.1.1' };

  for (let i = 0; i < 3; i++) {
    const status = await simpleLimiter.check(req);
    console.log(`Request ${i + 1}:`, {
      allowed: status.allowed,
      remaining: status.remaining,
      resetIn: `${status.resetIn}ms`
    });
  }
})();

// ============================================================================
// Example 2: Distributed Rate Limiting with Redis
// ============================================================================
console.log('\n=== Example 2: Distributed Rate Limiting ===\n');

const redisLimiter = new DistributedLimiter({
  maxRequests: 50,
  windowMs: 60000,
  distributed: true,
  identifierFn: (req) => req.userId || req.ip
});

(async () => {
  const req = { ip: '192.168.1.2', userId: 'user-123' };

  // First request
  let status = await redisLimiter.check(req);
  console.log('First request - Allowed:', status.allowed);

  // Check stats
  const stats = await redisLimiter.stats();
  console.log('Limiter stats:', {
    maxRequests: stats.maxRequests,
    activeIdentifiers: stats.activeIdentifiers,
    distributedMode: stats.distributedMode
  });
})();

// ============================================================================
// Example 3: Express Middleware Integration
// ============================================================================
console.log('\n=== Example 3: Express Middleware ===\n');

// Create rate limit middleware
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

console.log('Middleware created - can be used with Express app.use()');

// Example usage:
// app.use(rateLimitMiddleware);
// app.get('/api/data', (req, res) => { ... });

// ============================================================================
// Example 4: Per-Endpoint Rate Limiting
// ============================================================================
console.log('\n=== Example 4: Per-Endpoint Rate Limiting ===\n');

const apiLimiter = new DistributedLimiter({
  maxRequests: 1000,
  windowMs: 3600000 // 1 hour
});

const strictLimiter = new DistributedLimiter({
  maxRequests: 10,
  windowMs: 60000
});

(async () => {
  const req = { ip: '192.168.1.3' };

  // General API endpoint - relaxed limits
  const apiStatus = await apiLimiter.check(req);
  console.log('General API endpoint:', { allowed: apiStatus.allowed, remaining: apiStatus.remaining });

  // Login endpoint - strict limits
  const loginStatus = await strictLimiter.check(req);
  console.log('Login endpoint:', { allowed: loginStatus.allowed, remaining: loginStatus.remaining });
})();

// ============================================================================
// Example 5: Quota Management
// ============================================================================
console.log('\n=== Example 5: Quota Management ===\n');

const enforcer = new QuotaEnforcer({
  strict: false,
  onQuotaExceeded: (status) => {
    console.log(`Quota warning: ${status.quotaName} exceeded!`);
  }
});

// Define quotas
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

(async () => {
  const context = { userId: 'user-123' };

  // Check API quota
  let status = await enforcer.enforce('api_calls', context, 1);
  console.log('API calls quota:', {
    limit: status.limit,
    used: status.used,
    remaining: status.remaining,
    percentUsed: status.percentUsed.toFixed(1) + '%'
  });

  // Check storage quota
  context.bucket = 'documents';
  status = await enforcer.enforce('storage', context, 104857600); // 100MB
  console.log('Storage quota:', {
    limit: status.limit,
    used: status.used,
    remaining: status.remaining,
    percentUsed: status.percentUsed.toFixed(1) + '%'
  });
})();

// ============================================================================
// Example 6: Complete Rate Limiting System
// ============================================================================
console.log('\n=== Example 6: Complete System ===\n');

const system = createRateLimiter({
  limiter: {
    maxRequests: 1000,
    windowMs: 60000,
    identifierFn: (req) => req.headers.authorization ? req.userId : req.ip
  },
  quota: {
    strict: false
  },
  metrics: {
    enabled: true,
    labels: ['userId', 'endpoint', 'method']
  }
});

// Define quotas
system.quota.define('requests_per_hour', {
  limit: 5000,
  resetMs: 3600000,
  dimensions: ['userId']
});

(async () => {
  const req = { ip: '192.168.1.4', userId: 'user-456', headers: {} };

  // Check rate limit
  const limitStatus = await system.limiter.check(req);
  console.log('Rate limit check:', {
    allowed: limitStatus.allowed,
    remaining: limitStatus.remaining
  });

  // Enforce quota
  const quotaStatus = await system.quota.enforce('requests_per_hour', { userId: req.userId });
  console.log('Quota enforcement:', {
    limit: quotaStatus.limit,
    used: quotaStatus.used,
    remaining: quotaStatus.remaining
  });

  // Record metrics
  system.metrics.record({
    type: limitStatus.allowed ? 'allowed' : 'blocked',
    context: { userId: req.userId, endpoint: '/api/data', method: 'GET' }
  });

  // Get summary
  console.log('Metrics summary:', system.metrics.getSummary());
})();

// ============================================================================
// Example 7: Sliding Window Rate Limiting
// ============================================================================
console.log('\n=== Example 7: Sliding Window Rate Limiting ===\n');

const slidingWindowLimiter = new DistributedLimiter({
  maxRequests: 100,
  windowMs: 60000, // 1 minute window
  identifierFn: (req) => req.ip
});

(async () => {
  const req = { ip: '192.168.1.5' };

  // Simulate requests over time
  for (let i = 0; i < 5; i++) {
    const status = await slidingWindowLimiter.check(req);
    console.log(`Sliding window request ${i + 1}:`, {
      current: status.current,
      remaining: status.remaining,
      resetIn: `${status.resetIn}ms`
    });

    if (!status.allowed) {
      console.log('Rate limit exceeded!');
      break;
    }

    // Wait 100ms between requests
    await new Promise(resolve => setTimeout(resolve, 100));
  }
})();

// ============================================================================
// Example 8: Metrics Export
// ============================================================================
console.log('\n=== Example 8: Metrics Export ===\n');

const metricsTracker = new MetricsTracker({
  enabled: true,
  labels: ['ip', 'endpoint']
});

// Record some events
metricsTracker.record({
  type: 'allowed',
  context: { ip: '192.168.1.6', endpoint: '/api/users' }
});

metricsTracker.record({
  type: 'blocked',
  context: { ip: '192.168.1.6', endpoint: '/api/users' }
});

// Export formats
console.log('JSON Export:', metricsTracker.exportJSON());
console.log('\nPrometheus Export Sample:');
console.log(metricsTracker.exportPrometheus().substring(0, 300));

// ============================================================================
// Example 9: Custom Identifier Function
// ============================================================================
console.log('\n=== Example 9: Custom Identifier ===\n');

const customLimiter = new DistributedLimiter({
  maxRequests: 100,
  windowMs: 60000,
  identifierFn: (req) => {
    // Use JWT token ID or fall back to IP
    if (req.user && req.user.id) {
      return `user:${req.user.id}`;
    }
    if (req.apiKey) {
      return `api:${req.apiKey}`;
    }
    return `ip:${req.ip}`;
  }
});

(async () => {
  // Authenticated request
  const authReq = { 
    ip: '192.168.1.7',
    user: { id: 'user-789' }
  };

  const authStatus = await customLimiter.check(authReq);
  console.log('Authenticated user limit:', { allowed: authStatus.allowed });

  // API key request
  const apiReq = {
    ip: '192.168.1.8',
    apiKey: 'sk-12345'
  };

  const apiStatus = await customLimiter.check(apiReq);
  console.log('API key limit:', { allowed: apiStatus.allowed });
})();

// ============================================================================
// Example 10: Quota with Multiple Dimensions
// ============================================================================
console.log('\n=== Example 10: Multi-Dimensional Quotas ===\n');

const multiEnforcer = new QuotaEnforcer({ strict: false });

multiEnforcer.define('bandwidth', {
  limit: 10737418240, // 10GB
  resetMs: 2592000000, // 30 days
  dimensions: ['userId', 'month']
});

(async () => {
  // User 1 - January
  let status = await multiEnforcer.enforce('bandwidth', 
    { userId: 'user-100', month: '2024-01' },
    1073741824 // 1GB
  );
  console.log('User 100 Jan bandwidth:', {
    used: `${(status.used / 1073741824).toFixed(2)}GB`,
    remaining: `${(status.remaining / 1073741824).toFixed(2)}GB`
  });

  // User 1 - February (separate quota)
  status = await multiEnforcer.enforce('bandwidth',
    { userId: 'user-100', month: '2024-02' },
    536870912 // 500MB
  );
  console.log('User 100 Feb bandwidth:', {
    used: `${(status.used / 1073741824).toFixed(2)}GB`,
    remaining: `${(status.remaining / 1073741824).toFixed(2)}GB`
  });
})();

// ============================================================================
// Example 11: Reset and Cleanup
// ============================================================================
console.log('\n=== Example 11: Reset and Cleanup ===\n');

const resetLimiter = new DistributedLimiter({
  maxRequests: 10,
  windowMs: 60000
});

(async () => {
  const req = { ip: '192.168.1.9' };

  // Make a request
  let status = await resetLimiter.check(req);
  console.log('Before reset:', { remaining: status.remaining });

  // Reset for this IP
  await resetLimiter.reset('192.168.1.9');

  // Check status
  status = await resetLimiter.check(req, { increment: false });
  console.log('After reset:', { remaining: status.remaining });

  // Reset all
  await resetLimiter.resetAll();
  console.log('All limits reset');
})();

console.log('\n=== All examples completed ===\n');
