/**
 * API Gateway Tests - Test suite for API gateway
 * Tests routing, authentication, rate limiting, response formatting
 * @module tests/gateway/api-gateway.test
 */

const {
  ServiceRouter,
  AuthenticationEnforcer,
  RateLimitEnforcer,
  ResponseFormatter,
  ErrorResponseFormatter,
  CachingHeaders,
  APIGateway,
} = require('../../src/gateway/api-gateway');

describe('ServiceRouter', () => {
  let router;

  beforeEach(() => {
    router = new ServiceRouter();
  });

  test('should register routes', () => {
    const handler = jest.fn();

    router.registerRoute('/api/ai/*', 'ai-service', handler);

    expect(router.getRouteCount()).toBe(1);
  });

  test('should match route patterns', () => {
    const handler = jest.fn();

    router.registerRoute('/api/ai/*', 'ai-service', handler);

    const route = router.matchRoute('/api/ai/suggest');

    expect(route).toBeDefined();
    expect(route.service).toBe('ai-service');
  });

  test('should return null for unmatched routes', () => {
    const route = router.matchRoute('/unknown/path');

    expect(route).toBeNull();
  });
});

describe('AuthenticationEnforcer', () => {
  let enforcer;

  beforeEach(() => {
    enforcer = new AuthenticationEnforcer();
  });

  test('should register API keys', () => {
    enforcer.registerAPIKey('key-123', { tier: 'pro' });

    const keyData = enforcer.validateAPIKey('key-123');

    expect(keyData).toBeDefined();
    expect(keyData.active).toBe(true);
  });

  test('should issue JWT tokens', () => {
    const { token, expiresAt } = enforcer.issueToken('user-123');

    expect(token).toBeDefined();
    expect(expiresAt).toBeGreaterThan(Date.now());
  });

  test('should validate JWT tokens', () => {
    const { token } = enforcer.issueToken('user-123');

    const tokenData = enforcer.validateToken(token);

    expect(tokenData).toBeDefined();
    expect(tokenData.userId).toBe('user-123');
  });

  test('should reject expired tokens', () => {
    enforcer.tokenTTL = 1;

    const { token } = enforcer.issueToken('user-123');

    jest.useFakeTimers();
    jest.advanceTimersByTime(100);

    const tokenData = enforcer.validateToken(token);

    expect(tokenData).toBeNull();

    jest.useRealTimers();
  });

  test('should revoke tokens', () => {
    const { token } = enforcer.issueToken('user-123');

    enforcer.revokeToken(token);

    const tokenData = enforcer.validateToken(token);

    expect(tokenData).toBeNull();
  });

  test('should authenticate requests', () => {
    const { token } = enforcer.issueToken('user-123');

    const req = {
      headers: {
        authorization: `Bearer ${token}`,
      },
    };

    const auth = enforcer.authenticate(req);

    expect(auth).toBeDefined();
    expect(auth.type).toBe('jwt');
  });
});

describe('RateLimitEnforcer', () => {
  let limiter;

  beforeEach(() => {
    limiter = new RateLimitEnforcer();
  });

  test('should check rate limits', () => {
    const limit = limiter.checkLimit('user-1', 'free');

    expect(limit.allowed).toBe(true);
    expect(limit.remaining).toBeGreaterThan(0);
  });

  test('should track requests', () => {
    let limit;

    for (let i = 0; i < 5; i++) {
      limit = limiter.checkLimit('user-1', 'free');
    }

    expect(limit.current).toBe(5);
  });

  test('should exceed rate limit', () => {
    limiter.registerTier('test', 2, 60000);

    limiter.checkLimit('user-1', 'test');
    limiter.checkLimit('user-1', 'test');
    const limit = limiter.checkLimit('user-1', 'test');

    expect(limit.allowed).toBe(false);
  });

  test('should reset counters', () => {
    limiter.checkLimit('user-1', 'free');
    limiter.resetCounter('user-1', 'free');

    const limit = limiter.checkLimit('user-1', 'free');

    expect(limit.current).toBe(1);
  });

  test('should get statistics', () => {
    const stats = limiter.getStats();

    expect(stats.tiersCount).toBeGreaterThan(0);
    expect(stats.tiers).toBeDefined();
  });
});

describe('ResponseFormatter', () => {
  test('should format success response', () => {
    const response = ResponseFormatter.success({ id: 1, name: 'test' });

    expect(response.success).toBe(true);
    expect(response.data).toBeDefined();
    expect(response.meta).toBeDefined();
    expect(response.errors).toEqual([]);
  });

  test('should format error response', () => {
    const response = ResponseFormatter.error(new Error('Test error'), { code: 'TEST_ERROR' });

    expect(response.success).toBe(false);
    expect(response.data).toBeNull();
    expect(response.errors.length).toBe(1);
    expect(response.errors[0].code).toBe('TEST_ERROR');
  });

  test('should format paginated response', () => {
    const items = [{ id: 1 }, { id: 2 }];
    const pagination = { page: 1, pageSize: 20, total: 100 };

    const response = ResponseFormatter.paginated(items, pagination);

    expect(response.success).toBe(true);
    expect(response.data).toEqual(items);
    expect(response.meta.pagination).toBeDefined();
    expect(response.meta.pagination.totalPages).toBe(5);
  });
});

describe('ErrorResponseFormatter', () => {
  test('should format validation error', () => {
    const error = ErrorResponseFormatter.formatError('VALIDATION_ERROR', 'Invalid input');

    expect(error.status).toBe(400);
    expect(error.response.success).toBe(false);
  });

  test('should format unauthorized error', () => {
    const error = ErrorResponseFormatter.formatError('UNAUTHORIZED', 'Not authenticated');

    expect(error.status).toBe(401);
  });

  test('should format rate limit error', () => {
    const error = ErrorResponseFormatter.formatError('RATE_LIMIT_EXCEEDED', 'Too many requests');

    expect(error.status).toBe(429);
  });

  test('should get status code for error', () => {
    expect(ErrorResponseFormatter.getStatusCode('FORBIDDEN')).toBe(403);
    expect(ErrorResponseFormatter.getStatusCode('NOT_FOUND')).toBe(404);
    expect(ErrorResponseFormatter.getStatusCode('INTERNAL_ERROR')).toBe(500);
  });
});

describe('CachingHeaders', () => {
  test('should get cache headers for strategy', () => {
    const headers = CachingHeaders.getHeaders('short');

    expect(headers['Cache-Control']).toContain('public');
    expect(headers['Cache-Control']).toContain('max-age=60');
    expect(headers['ETag']).toBeDefined();
  });

  test('should support different cache strategies', () => {
    const shortHeaders = CachingHeaders.getHeaders('short');
    const longHeaders = CachingHeaders.getHeaders('long');

    expect(shortHeaders['Cache-Control']).not.toBe(longHeaders['Cache-Control']);
  });

  test('should apply headers to response', () => {
    const res = {
      setHeader: jest.fn(),
    };

    CachingHeaders.apply(res, 'medium');

    expect(res.setHeader).toHaveBeenCalledWith('Cache-Control', expect.any(String));
    expect(res.setHeader).toHaveBeenCalledWith('Pragma', 'cache');
    expect(res.setHeader).toHaveBeenCalledWith('ETag', expect.any(String));
  });
});

describe('APIGateway', () => {
  let gateway;

  beforeEach(() => {
    gateway = new APIGateway();
  });

  test('should initialize with default routes', () => {
    expect(gateway.router.middlewares.length).toBeGreaterThan(0);
  });

  test('should register routes', () => {
    const handler = jest.fn();

    gateway.registerRoute('ai', '/suggest', 'ai-service', handler);

    expect(gateway.router.getRouteCount()).toBeGreaterThan(0);
  });

  test('should handle authentication', () => {
    const { token } = gateway.auth.issueToken('user-1');

    const req = {
      headers: { authorization: `Bearer ${token}` },
      path: '/api/ai/suggest',
    };

    const auth = gateway.auth.authenticate(req);

    expect(auth).toBeDefined();
  });

  test('should get gateway statistics', () => {
    const stats = gateway.getStats();

    expect(stats.routes).toBeDefined();
    expect(stats.rateLimiter).toBeDefined();
    expect(stats.tokens).toBeGreaterThanOrEqual(0);
    expect(stats.apiKeys).toBeGreaterThanOrEqual(0);
  });
});
