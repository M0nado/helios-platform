/**
 * HELIOS v4.0 - Rate Limiting Examples
 * Real-world usage scenarios for rate limiting module
 * @module feat-ratelimit/examples
 */

const {
  TokenBucket,
  SlidingWindow,
  DistributedLimiter,
  QuotaManager
} = require('./implementation');

// ============================================================================
// EXAMPLE 1: Per-User Rate Limiting with Token Bucket
// ============================================================================
console.log('=== Example 1: Per-User Token Bucket Rate Limiting ===\n');

class APIGateway {
  constructor() {
    this.userBuckets = new Map();
    this.defaultCapacity = 100;
    this.defaultRefillRate = 10; // tokens per second
  }

  /**
   * Get or create bucket for user
   */
  getBucket(userId) {
    if (!this.userBuckets.has(userId)) {
      this.userBuckets.set(
        userId,
        new TokenBucket(this.defaultCapacity, this.defaultRefillRate)
      );
    }
    return this.userBuckets.get(userId);
  }

  /**
   * Process API request
   */
  handleRequest(userId, tokensRequired = 1) {
    const bucket = this.getBucket(userId);
    if (bucket.consume(tokensRequired)) {
      return {
        status: 'allowed',
        message: `Request processed. ${Math.floor(bucket.getTokens())} tokens remaining`,
        tokensRemaining: Math.floor(bucket.getTokens())
      };
    } else {
      const waitTime = bucket.getWaitTime(tokensRequired);
      return {
        status: 'throttled',
        message: `Rate limit exceeded. Retry after ${Math.ceil(waitTime / 1000)}s`,
        retryAfter: Math.ceil(waitTime / 1000)
      };
    }
  }

  /**
   * Get user statistics
   */
  getUserStats(userId) {
    const bucket = this.getBucket(userId);
    return bucket.getStats();
  }
}

// Usage
const gateway = new APIGateway();

console.log('User1 Request 1:', gateway.handleRequest('user1', 1));
console.log('User1 Request 2:', gateway.handleRequest('user1', 1));
console.log('User1 Stats:', gateway.getUserStats('user1'));
console.log('User2 Request 1:', gateway.handleRequest('user2', 1));
console.log('User2 is isolated from User1\n');

// ============================================================================
// EXAMPLE 2: Sliding Window for Request Rate Limiting
// ============================================================================
console.log('=== Example 2: Sliding Window Rate Limiting (60s Window) ===\n');

class WebhookRateLimiter {
  constructor(windowMs = 60000, maxRequests = 100) {
    this.limiters = new Map();
    this.windowMs = windowMs;
    this.maxRequests = maxRequests;
  }

  processWebhook(webhookId) {
    if (!this.limiters.has(webhookId)) {
      this.limiters.set(webhookId, new SlidingWindow(this.windowMs, this.maxRequests));
    }

    const limiter = this.limiters.get(webhookId);
    const allowed = limiter.record();

    return {
      webhookId,
      allowed,
      requestCount: limiter.getCount(),
      limit: this.maxRequests,
      remainingCapacity: this.maxRequests - limiter.getCount(),
      waitTimeMs: limiter.getWaitTime()
    };
  }
}

const webhookLimiter = new WebhookRateLimiter(60000, 10);

for (let i = 1; i <= 12; i++) {
  const result = webhookLimiter.processWebhook('webhook-123');
  console.log(`Request ${i}:`, 
    result.allowed ? '✓ ALLOWED' : '✗ BLOCKED', 
    `(${result.requestCount}/${result.limit})`);
}
console.log();

// ============================================================================
// EXAMPLE 3: Distributed Rate Limiting Across Multiple Instances
// ============================================================================
console.log('=== Example 3: Distributed Rate Limiting (Multi-Instance) ===\n');

class DistributedAPILimiter {
  constructor(redisBackend) {
    this.limiter = new DistributedLimiter(
      redisBackend,
      'api-limit:',
      60000, // 60 second window
      100    // 100 requests per window
    );
  }

  /**
   * Check and enforce rate limit
   */
  checkRequest(clientIp) {
    const result = this.limiter.checkLimit(clientIp);

    return {
      allowed: result.allowed,
      remaining: result.remaining,
      total: this.limiter.maxRequests,
      resetTime: new Date(result.resetTime),
      retryAfterSeconds: Math.ceil(result.retryAfter / 1000)
    };
  }

  /**
   * Get all active limiters (for monitoring)
   */
  getMetrics() {
    const all = this.limiter.getAll();
    return {
      totalIPs: all.size,
      limiters: Array.from(all.entries()).map(([key, data]) => ({
        ip: key.replace('api-limit:', ''),
        requests: data.count,
        windowStart: new Date(data.windowStart)
      }))
    };
  }

  /**
   * Reset limit for IP
   */
  whitelistIP(clientIp) {
    this.limiter.reset(clientIp);
    return { message: `Whitelist reset for ${clientIp}` };
  }
}

// Simulated Redis backend (in production, use actual Redis)
const simRedis = new Map();
const distributedLimiter = new DistributedAPILimiter(simRedis);

console.log('Client 192.168.1.1 Request 1:', distributedLimiter.checkRequest('192.168.1.1'));
console.log('Client 192.168.1.1 Request 2:', distributedLimiter.checkRequest('192.168.1.1'));
console.log('Client 192.168.1.2 Request 1:', distributedLimiter.checkRequest('192.168.1.2'));
console.log('\nActive Limiters:', distributedLimiter.getMetrics());
console.log();

// ============================================================================
// EXAMPLE 4: Quota Management with Billing Cycles
// ============================================================================
console.log('=== Example 4: Quota Management (Monthly Billing Cycle) ===\n');

class SubscriptionQuotaManager {
  constructor() {
    this.manager = new QuotaManager(new Map(), 30); // 30-day billing cycle
    
    // Setup subscription tiers
    this.tiers = {
      free: 100,
      pro: 10000,
      enterprise: 1000000
    };
  }

  /**
   * Create subscription
   */
  createSubscription(userId, tier) {
    const limit = this.tiers[tier];
    if (!limit) throw new Error(`Unknown tier: ${tier}`);
    
    const softLimit = Math.ceil(limit * 0.8);
    this.manager.setQuota(userId, limit, softLimit);
    
    return {
      userId,
      tier,
      limit,
      softLimit,
      message: `Subscription created: ${tier} tier (${limit} requests/month)`
    };
  }

  /**
   * Make API call
   */
  makeAPICall(userId, requestUnits = 1) {
    const result = this.manager.consume(userId, requestUnits);
    
    return {
      userId,
      success: result.allowed,
      used: result.used,
      remaining: result.remaining,
      total: result.limit,
      percentageUsed: result.percentage,
      status: result.status,
      resetDate: new Date(result.cycleResetAt),
      message: result.status === 'warning' 
        ? `Warning: ${result.percentage}% of quota used`
        : `OK: ${result.remaining} requests remaining`
    };
  }

  /**
   * Get usage summary
   */
  getUsageSummary(userId) {
    const quota = this.manager.getQuota(userId);
    return {
      used: quota.used,
      limit: quota.limit,
      remaining: quota.remaining,
      percentageUsed: quota.percentage,
      status: quota.status,
      resetDate: new Date(quota.cycleResetAt)
    };
  }

  /**
   * Get all users with quota warnings
   */
  getWarningUsers() {
    return this.manager.getWarnings();
  }
}

const quotaManager = new SubscriptionQuotaManager();

console.log(quotaManager.createSubscription('acme-corp', 'pro'));
console.log(quotaManager.createSubscription('startup-xyz', 'free'));
console.log();

for (let i = 0; i < 3; i++) {
  console.log(`API Call ${i + 1}:`, quotaManager.makeAPICall('acme-corp', 100));
}

console.log('\nUsage Summary for acme-corp:', quotaManager.getUsageSummary('acme-corp'));
console.log('Usage Summary for startup-xyz:', quotaManager.getUsageSummary('startup-xyz'));
console.log('Warning Users:', quotaManager.getWarningUsers());
console.log();

// ============================================================================
// EXAMPLE 5: Quota Reset Scenarios
// ============================================================================
console.log('=== Example 5: Advanced Quota Scenarios ===\n');

const advancedQuota = new QuotaManager();
advancedQuota.setQuota('premium-user', 5000);

// Simulate heavy usage
console.log('Initial quota:', advancedQuota.getQuota('premium-user').remaining);

advancedQuota.consume('premium-user', 4500);
let quota = advancedQuota.getQuota('premium-user');
console.log('After heavy usage:', quota.remaining, 'remaining');
console.log('Status:', quota.status);

// Manual reset (e.g., customer support override)
advancedQuota.resetQuota('premium-user');
quota = advancedQuota.getQuota('premium-user');
console.log('After manual reset:', quota.remaining, 'remaining');
console.log();

// ============================================================================
// EXAMPLE 6: Complex Limiting Scenario
// ============================================================================
console.log('=== Example 6: Complex Limiting (Token Bucket + Quota) ===\n');

class ComplianceAwareRateLimiter {
  constructor() {
    // Token bucket for request rate
    this.requestBucket = new TokenBucket(1000, 100); // 100 tokens/sec
    
    // Quota manager for monthly limits
    this.quotaManager = new QuotaManager();
  }

  /**
   * Process request with dual limiting
   */
  processRequest(userId, requestUnits = 1) {
    // Check quota first
    let quotaResult = this.quotaManager.getQuota(userId);
    if (quotaResult.remaining < requestUnits) {
      return {
        allowed: false,
        reason: 'Monthly quota exceeded',
        monthly: {
          used: quotaResult.used,
          limit: quotaResult.limit,
          remaining: quotaResult.remaining
        }
      };
    }

    // Then check rate limiting
    if (!this.requestBucket.consume(requestUnits)) {
      return {
        allowed: false,
        reason: 'Rate limit exceeded',
        retryAfter: Math.ceil(this.requestBucket.getWaitTime(requestUnits) / 1000)
      };
    }

    // Consume from quota
    this.quotaManager.consume(userId, requestUnits);

    return {
      allowed: true,
      message: 'Request processed successfully'
    };
  }

  /**
   * Setup user
   */
  setupUser(userId, monthlyQuota) {
    this.quotaManager.setQuota(userId, monthlyQuota);
  }
}

const complianceLimiter = new ComplianceAwareRateLimiter();
complianceLimiter.setupUser('user123', 10000);

console.log('Request 1:', complianceLimiter.processRequest('user123', 100));
console.log('Request 2:', complianceLimiter.processRequest('user123', 50));
console.log('\nAll examples completed successfully!\n');
