/**
 * Integration Test Suite for Depth 2 & 3
 * Tests compatibility and identical functionality
 */

const RoutingManager = require('../routing-middleware');
const ValidationFeaturesManager = require('../validation-features');

/**
 * Simple test runner
 */
class TestRunner {
  constructor(name) {
    this.name = name;
    this.tests = [];
    this.passed = 0;
    this.failed = 0;
  }

  test(name, fn) {
    this.tests.push({ name, fn });
  }

  async run() {
    console.log(`\n===== ${this.name} =====\n`);
    
    for (const { name, fn } of this.tests) {
      try {
        await fn();
        this.passed++;
        console.log(`✓ ${name}`);
      } catch (error) {
        this.failed++;
        console.log(`✗ ${name}`);
        console.log(`  Error: ${error.message}`);
      }
    }

    const total = this.tests.length;
    const coverage = ((this.passed / total) * 100).toFixed(1);
    console.log(`\nResults: ${this.passed}/${total} (${coverage}%)\n`);

    return this.failed === 0;
  }
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message || 'Assertion failed');
  }
}

function assertEqual(actual, expected, message) {
  if (actual !== expected) {
    throw new Error(message || `Expected ${expected}, got ${actual}`);
  }
}

// ===== Depth 2 Tests =====
const depth2 = new TestRunner('Depth 2 - Medium Specialist Tests');

depth2.test('Should initialize routing manager', () => {
  const routing = new RoutingManager({ apiVersion: 'v1' });
  assert(routing.apiVersion === 'v1', 'Should set version');
  assert(routing.routes.size === 0, 'Should have no routes initially');
});

depth2.test('Should initialize validation manager', () => {
  const validation = new ValidationFeaturesManager({
    jwtSecret: 'test-secret',
    cache: { ttl: 300, maxSize: 1000 }
  });
  assert(validation.jwtSecret === 'test-secret', 'Should set secret');
  assertEqual(validation.cacheConfig.ttl, 300, 'Should set TTL');
});

depth2.test('Should register endpoint in routing', () => {
  const routing = new RoutingManager();
  routing.registerEndpoint('GET', '/users', {
    handler: async () => ({ data: [] }),
    description: 'Get users'
  });
  assert(routing.routes.size === 1, 'Should register route');
});

depth2.test('Should find exact route match', () => {
  const routing = new RoutingManager();
  routing.registerEndpoint('GET', '/users', {
    handler: async () => ({ data: [] })
  });
  
  const route = routing.findRoute('GET', '/users');
  assert(route !== null, 'Should find route');
  assertEqual(route.path, '/users', 'Should match exact path');
});

depth2.test('Should find parameterized route', () => {
  const routing = new RoutingManager();
  routing.registerEndpoint('GET', '/users/:id', {
    handler: async () => ({ data: {} })
  });
  
  const route = routing.findRoute('GET', '/users/123');
  assert(route !== null, 'Should find parameterized route');
});

depth2.test('Should extract path parameters', () => {
  const routing = new RoutingManager();
  routing.registerEndpoint('GET', '/users/:id/posts/:postId', {
    handler: async () => ({})
  });
  
  const params = routing.extractPathParams('/users/123/posts/456');
  assertEqual(params.id, '123', 'Should extract user ID');
  assertEqual(params.postId, '456', 'Should extract post ID');
});

depth2.test('Should validate required fields', () => {
  const validation = new ValidationFeaturesManager();
  const schema = {
    properties: { name: { type: 'string' } },
    required: ['name']
  };
  
  const result = validation.validateRequest({}, schema);
  assert(!result.valid, 'Should fail validation');
  assert(result.errors.length > 0, 'Should have errors');
});

depth2.test('Should validate field types', () => {
  const validation = new ValidationFeaturesManager();
  const schema = {
    properties: { age: { type: 'number' }, name: { type: 'string' } }
  };
  
  const result = validation.validateRequest({ name: 'John', age: 30 }, schema);
  assert(result.valid, 'Should validate correct types');
});

depth2.test('Should validate patterns', () => {
  const validation = new ValidationFeaturesManager();
  const schema = {
    properties: { email: { type: 'string', pattern: '^[^@]+@[^@]+$' } }
  };
  
  const result1 = validation.validateRequest({ email: 'test@example.com' }, schema);
  assert(result1.valid, 'Should validate matching pattern');
  
  const result2 = validation.validateRequest({ email: 'invalid' }, schema);
  assert(!result2.valid, 'Should reject non-matching pattern');
});

depth2.test('Should create JWT token', () => {
  const validation = new ValidationFeaturesManager({ jwtSecret: 'secret' });
  const token = validation.createToken({ userId: '123' });
  assert(typeof token === 'string', 'Should return token');
  assert(token.split('.').length === 3, 'Should be valid JWT');
});

depth2.test('Should verify JWT token', () => {
  const validation = new ValidationFeaturesManager({ jwtSecret: 'secret' });
  const token = validation.createToken({ userId: '123' });
  const decoded = validation.verifyToken(token);
  assert(decoded !== null, 'Should decode token');
  assertEqual(decoded.userId, '123', 'Should decode payload');
});

depth2.test('Should reject invalid token', () => {
  const validation = new ValidationFeaturesManager({ jwtSecret: 'secret' });
  const decoded = validation.verifyToken('invalid.token.here');
  assert(decoded === null, 'Should return null for invalid token');
});

depth2.test('Should cache response', () => {
  const validation = new ValidationFeaturesManager();
  validation.storeInCache('GET', '/data', {}, { value: 42 });
  const cached = validation.getFromCache('GET', '/data', {});
  assert(cached !== null, 'Should retrieve cached value');
});

depth2.test('Should not cache non-GET requests', () => {
  const validation = new ValidationFeaturesManager();
  validation.storeInCache('POST', '/data', {}, { value: 42 });
  const cached = validation.getFromCache('POST', '/data', {});
  assert(cached === null, 'Should not cache POST');
});

depth2.test('Should format success response', () => {
  const validation = new ValidationFeaturesManager();
  const response = validation.formatSuccessResponse({ id: 1 }, { status: 200 });
  assert(response.success === true, 'Should indicate success');
  assertEqual(response.status, 200, 'Should set status');
  assert(response.timestamp, 'Should include timestamp');
});

depth2.test('Should format error response', () => {
  const validation = new ValidationFeaturesManager();
  const response = validation.formatErrorResponse(404, 'Not found');
  assert(response.success === false, 'Should indicate error');
  assertEqual(response.status, 404, 'Should set status');
  assert(response.error.code === 'ERR_404', 'Should set error code');
});

depth2.test('Should track metrics', () => {
  const validation = new ValidationFeaturesManager();
  validation.trackMetrics(100, false);
  validation.trackMetrics(150, true);
  
  const metrics = validation.getMetrics();
  assertEqual(metrics.requests, 2, 'Should count requests');
  assertEqual(metrics.errors, 1, 'Should count errors');
});

depth2.test('Should execute middleware', async () => {
  const validation = new ValidationFeaturesManager();
  let called = false;
  
  validation.use(async (ctx) => {
    called = true;
    return ctx;
  });

  await validation.executeMiddlewares({});
  assert(called === true, 'Should execute middleware');
});

depth2.test('Should register and use OpenAPI schema', () => {
  const routing = new RoutingManager();
  routing.registerSchema('User', {
    type: 'object',
    properties: {
      id: { type: 'string' },
      name: { type: 'string' }
    }
  });

  const spec = routing.getOpenAPISpec();
  assert(spec.components.schemas['User'], 'Should include schema');
});

depth2.test('Should generate OpenAPI spec', () => {
  const routing = new RoutingManager();
  routing.registerEndpoint('GET', '/users', {
    handler: async () => ({}),
    description: 'Get users'
  });

  const spec = routing.getOpenAPISpec();
  assert(spec.openapi === '3.0.0', 'Should be OpenAPI 3.0');
  assert(spec.paths['/users'].get, 'Should include GET operation');
});

depth2.test('Should clear cache', () => {
  const validation = new ValidationFeaturesManager();
  validation.storeInCache('GET', '/data', {}, { value: 42 });
  assert(validation.getMetrics().cacheSize > 0, 'Should have cache');
  
  validation.clearCache();
  assert(validation.getMetrics().cacheSize === 0, 'Cache should be empty');
});

// ===== Run Tests =====
(async () => {
  const success = await depth2.run();
  process.exit(success ? 0 : 1);
})();
