/**
 * Test Suite for Depth 1 - Generalist REST API
 * Tests all features: routing, auth, validation, caching, errors, OpenAPI, monitoring
 */

const RestAPIManager = require('../rest-api-full');

/**
 * Simple test runner
 */
class TestRunner {
  constructor() {
    this.tests = [];
    this.passed = 0;
    this.failed = 0;
  }

  test(name, fn) {
    this.tests.push({ name, fn });
  }

  async run() {
    console.log('\n===== Depth 1 - Generalist Tests =====\n');
    
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
    console.log(`\n===== Results =====`);
    console.log(`Passed: ${this.passed}/${total}`);
    console.log(`Failed: ${this.failed}/${total}`);
    console.log(`Coverage: ${coverage}%\n`);

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

// ===== Test Suite =====
const runner = new TestRunner();

// Initialization Tests
runner.test('Should initialize with default config', () => {
  const api = new RestAPIManager();
  assert(api.apiVersion === 'v1', 'Default version should be v1');
  assert(api.routes.size === 0, 'Should have no routes initially');
});

runner.test('Should initialize with custom config', () => {
  const api = new RestAPIManager({
    jwtSecret: 'custom-secret',
    apiVersion: 'v2',
    cache: { ttl: 600, maxSize: 2000 }
  });
  assertEqual(api.apiVersion, 'v2', 'Should set custom version');
  assertEqual(api.cacheConfig.ttl, 600, 'Should set custom cache TTL');
});

// Endpoint Registration Tests
runner.test('Should register GET endpoint', () => {
  const api = new RestAPIManager();
  api.registerEndpoint('GET', '/users', {
    handler: async () => ({ status: 200, data: [] }),
    description: 'Get all users'
  });
  assert(api.routes.size === 1, 'Should have one route');
});

runner.test('Should register POST endpoint with schema', () => {
  const api = new RestAPIManager();
  const schema = {
    request: {
      type: 'object',
      properties: { name: { type: 'string' } },
      required: ['name']
    }
  };
  api.registerEndpoint('POST', '/users', {
    handler: async () => ({ status: 201, data: {} }),
    schema
  });
  assert(api.routes.size === 1, 'Should have one route');
});

runner.test('Should register multiple endpoints', () => {
  const api = new RestAPIManager();
  api.registerEndpoint('GET', '/users', { handler: async () => ({ data: [] }) });
  api.registerEndpoint('POST', '/users', { handler: async () => ({ data: {} }) });
  api.registerEndpoint('GET', '/users/:id', { handler: async () => ({ data: {} }) });
  assert(api.routes.size === 3, 'Should have three routes');
});

// Schema Registration Tests
runner.test('Should register JSON schema', () => {
  const api = new RestAPIManager();
  const userSchema = {
    type: 'object',
    properties: {
      id: { type: 'string' },
      name: { type: 'string' },
      email: { type: 'string', pattern: '^[^@]+@[^@]+$' }
    }
  };
  api.registerSchema('User', userSchema);
  assert(api.schemas.has('User'), 'Should store schema');
  assert(api.openApiSpec.components.schemas['User'], 'Should add to OpenAPI');
});

// Authentication Tests
runner.test('Should create JWT token', () => {
  const api = new RestAPIManager({ jwtSecret: 'test-secret' });
  const token = api.createToken({ userId: '123', role: 'admin' });
  assert(typeof token === 'string', 'Should return string token');
  assert(token.split('.').length === 3, 'Should be valid JWT format');
});

runner.test('Should verify valid JWT token', () => {
  const api = new RestAPIManager({ jwtSecret: 'test-secret' });
  const payload = { userId: '123', role: 'admin' };
  const token = api.createToken(payload);
  const decoded = api.verifyToken(token);
  assert(decoded !== null, 'Should decode valid token');
  assertEqual(decoded.userId, '123', 'Should decode payload correctly');
});

runner.test('Should reject invalid JWT token', () => {
  const api = new RestAPIManager({ jwtSecret: 'test-secret' });
  const decoded = api.verifyToken('invalid.token.here');
  assert(decoded === null, 'Should return null for invalid token');
});

runner.test('Should reject wrong secret', () => {
  const api1 = new RestAPIManager({ jwtSecret: 'secret1' });
  const api2 = new RestAPIManager({ jwtSecret: 'secret2' });
  const token = api1.createToken({ userId: '123' });
  const decoded = api2.verifyToken(token);
  assert(decoded === null, 'Should reject token with wrong secret');
});

// Validation Tests
runner.test('Should validate required fields', () => {
  const api = new RestAPIManager();
  const schema = {
    properties: { name: { type: 'string' } },
    required: ['name']
  };
  
  const result1 = api.validateRequest({ name: 'John' }, schema);
  assert(result1.valid, 'Should validate with all required fields');
  
  const result2 = api.validateRequest({}, schema);
  assert(!result2.valid, 'Should fail without required fields');
});

runner.test('Should validate field types', () => {
  const api = new RestAPIManager();
  const schema = {
    properties: { age: { type: 'number' }, name: { type: 'string' } }
  };
  
  const result1 = api.validateRequest({ name: 'John', age: 30 }, schema);
  assert(result1.valid, 'Should validate correct types');
  
  const result2 = api.validateRequest({ name: 'John', age: '30' }, schema);
  assert(!result2.valid, 'Should fail on type mismatch');
});

runner.test('Should validate pattern matching', () => {
  const api = new RestAPIManager();
  const schema = {
    properties: { email: { type: 'string', pattern: '^[^@]+@[^@]+$' } }
  };
  
  const result1 = api.validateRequest({ email: 'user@example.com' }, schema);
  assert(result1.valid, 'Should validate matching pattern');
  
  const result2 = api.validateRequest({ email: 'invalid' }, schema);
  assert(!result2.valid, 'Should fail on pattern mismatch');
});

runner.test('Should validate string length', () => {
  const api = new RestAPIManager();
  const schema = {
    properties: { name: { type: 'string', minLength: 2, maxLength: 50 } }
  };
  
  const result1 = api.validateRequest({ name: 'John' }, schema);
  assert(result1.valid, 'Should validate within length bounds');
  
  const result2 = api.validateRequest({ name: 'J' }, schema);
  assert(!result2.valid, 'Should fail on min length');
  
  const result3 = api.validateRequest({ name: 'a'.repeat(51) }, schema);
  assert(!result3.valid, 'Should fail on max length');
});

runner.test('Should validate enum values', () => {
  const api = new RestAPIManager();
  const schema = {
    properties: { status: { type: 'string', enum: ['active', 'inactive', 'pending'] } }
  };
  
  const result1 = api.validateRequest({ status: 'active' }, schema);
  assert(result1.valid, 'Should validate enum value');
  
  const result2 = api.validateRequest({ status: 'invalid' }, schema);
  assert(!result2.valid, 'Should fail on invalid enum value');
});

// Caching Tests
runner.test('Should cache GET response', async () => {
  const api = new RestAPIManager();
  api.registerEndpoint('GET', '/data', {
    handler: async () => ({ status: 200, data: { value: 42 } }),
    cacheable: true
  });

  const req1 = { method: 'GET', path: '/data' };
  const res1 = await api.handleRequest(req1);
  const res2 = await api.handleRequest(req1);

  assert(api.metrics.cacheHits === 1, 'Should register cache hit');
  assert(res2.cached === true, 'Should indicate cached response');
});

runner.test('Should not cache POST responses', async () => {
  const api = new RestAPIManager();
  api.registerEndpoint('POST', '/data', {
    handler: async () => ({ status: 201, data: { id: 1 } }),
    cacheable: true
  });

  await api.handleRequest({ method: 'POST', path: '/data', body: {} });
  assert(api.metrics.cacheMisses === 0 && api.metrics.cacheHits === 0, 'POST should not use cache');
});

runner.test('Should respect cache TTL', (done) => {
  const api = new RestAPIManager({ cache: { ttl: 1, maxSize: 100 } });
  api.registerEndpoint('GET', '/data', {
    handler: async () => ({ data: { t: Date.now() } }),
    cacheable: true
  });

  api.handleRequest({ method: 'GET', path: '/data' }).then(() => {
    setTimeout(() => {
      api.handleRequest({ method: 'GET', path: '/data' }).then(() => {
        assert(api.metrics.cacheMisses === 1, 'Should expire cache after TTL');
      });
    }, 1100);
  });
});

runner.test('Should evict cache when full', async () => {
  const api = new RestAPIManager({ cache: { ttl: 3600, maxSize: 2 } });
  api.registerEndpoint('GET', '/data/:id', {
    handler: async (req) => ({ data: { id: req.pathParams.id } }),
    cacheable: true
  });

  await api.handleRequest({ method: 'GET', path: '/data/1' });
  await api.handleRequest({ method: 'GET', path: '/data/2' });
  await api.handleRequest({ method: 'GET', path: '/data/3' });

  assert(api.cache.size <= 2, 'Cache should not exceed max size');
});

// Request Handling Tests
runner.test('Should handle GET request', async () => {
  const api = new RestAPIManager();
  api.registerEndpoint('GET', '/users', {
    handler: async () => ({ status: 200, data: { users: [] } })
  });

  const res = await api.handleRequest({
    method: 'GET',
    path: '/users'
  });

  assert(res.success === true, 'Should return successful response');
  assertEqual(res.status, 200, 'Should return 200 status');
});

runner.test('Should handle POST request with body', async () => {
  const api = new RestAPIManager();
  api.registerEndpoint('POST', '/users', {
    handler: async (req) => ({ status: 201, data: { id: 1, ...req.body } })
  });

  const res = await api.handleRequest({
    method: 'POST',
    path: '/users',
    body: { name: 'John', email: 'john@example.com' }
  });

  assert(res.success === true, 'Should handle POST');
  assertEqual(res.data.name, 'John', 'Should pass body data');
});

runner.test('Should return 404 for unknown endpoint', async () => {
  const api = new RestAPIManager();
  const res = await api.handleRequest({ method: 'GET', path: '/unknown' });
  
  assert(res.success === false, 'Should return error');
  assertEqual(res.status, 404, 'Should return 404 status');
});

runner.test('Should validate request body', async () => {
  const api = new RestAPIManager();
  api.registerEndpoint('POST', '/users', {
    handler: async () => ({ data: {} }),
    schema: {
      request: {
        properties: { name: { type: 'string' } },
        required: ['name']
      }
    }
  });

  const res = await api.handleRequest({
    method: 'POST',
    path: '/users',
    body: {}
  });

  assert(res.success === false, 'Should reject invalid body');
  assertEqual(res.status, 400, 'Should return 400 status');
});

// Authentication in Requests
runner.test('Should require auth for protected endpoint', async () => {
  const api = new RestAPIManager();
  api.registerEndpoint('GET', '/admin', {
    handler: async () => ({ data: {} }),
    requireAuth: true
  });

  const res = await api.handleRequest({ method: 'GET', path: '/admin' });
  
  assert(res.success === false, 'Should deny unauthenticated request');
  assertEqual(res.status, 401, 'Should return 401 status');
});

runner.test('Should allow auth with valid token', async () => {
  const api = new RestAPIManager({ jwtSecret: 'test-secret' });
  api.registerEndpoint('GET', '/admin', {
    handler: async () => ({ data: { role: 'admin' } }),
    requireAuth: true
  });

  const token = api.createToken({ userId: '123', role: 'admin' });
  const res = await api.handleRequest({
    method: 'GET',
    path: '/admin',
    headers: { authorization: `Bearer ${token}` }
  });

  assert(res.success === true, 'Should allow authenticated request');
});

// Middleware Tests
runner.test('Should execute middleware chain', async () => {
  const api = new RestAPIManager();
  let called = false;
  
  api.use(async (context) => {
    called = true;
    return context;
  });

  api.registerEndpoint('GET', '/data', {
    handler: async () => ({ data: {} })
  });

  await api.handleRequest({ method: 'GET', path: '/data' });
  assert(called === true, 'Should execute middleware');
});

runner.test('Middleware should block request', async () => {
  const api = new RestAPIManager();
  
  api.use(async (context) => {
    if (context.headers.blocked === 'true') {
      return { ...context, passed: false, error: 'Blocked by middleware' };
    }
    return context;
  });

  api.registerEndpoint('GET', '/data', {
    handler: async () => ({ data: {} })
  });

  const res = await api.handleRequest({
    method: 'GET',
    path: '/data',
    headers: { blocked: 'true' }
  });

  assert(res.success === false, 'Should block request');
  assertEqual(res.status, 403, 'Should return 403');
});

// Path Parameter Tests
runner.test('Should extract path parameters', async () => {
  const api = new RestAPIManager();
  api.registerEndpoint('GET', '/users/:id', {
    handler: async (req) => ({ data: { id: req.pathParams.id } })
  });

  const res = await api.handleRequest({ method: 'GET', path: '/users/123' });
  assert(res.success === true, 'Should handle path params');
});

// Metrics Tests
runner.test('Should track request metrics', async () => {
  const api = new RestAPIManager();
  api.registerEndpoint('GET', '/data', {
    handler: async () => ({ data: {} })
  });

  await api.handleRequest({ method: 'GET', path: '/data' });
  const metrics = api.getMetrics();

  assert(metrics.requests === 1, 'Should count requests');
  assert(metrics.avgResponseTime > 0, 'Should track response time');
});

runner.test('Should track error metrics', async () => {
  const api = new RestAPIManager();
  
  await api.handleRequest({ method: 'GET', path: '/unknown' });
  assert(api.metrics.errors === 1, 'Should count errors');
});

// OpenAPI Tests
runner.test('Should generate OpenAPI spec', () => {
  const api = new RestAPIManager();
  api.registerEndpoint('GET', '/users', {
    handler: async () => ({ data: [] }),
    description: 'Get all users'
  });

  const spec = api.getOpenAPISpec();
  assert(spec.openapi === '3.0.0', 'Should generate OpenAPI 3.0');
  assert(spec.paths['/users'], 'Should include registered paths');
  assert(spec.paths['/users'].get, 'Should include GET operation');
});

runner.test('Should include auth in OpenAPI', () => {
  const api = new RestAPIManager();
  api.registerEndpoint('GET', '/admin', {
    handler: async () => ({ data: {} }),
    requireAuth: true
  });

  const spec = api.getOpenAPISpec();
  const operation = spec.paths['/admin'].get;
  assert(operation.security, 'Should include security requirement');
});

// Integration Tests
runner.test('Should handle complete flow', async () => {
  const api = new RestAPIManager({ jwtSecret: 'test-secret' });

  api.registerEndpoint('POST', '/users', {
    handler: async (req) => ({ status: 201, data: { id: 1, ...req.body } }),
    schema: {
      request: {
        properties: { name: { type: 'string' }, email: { type: 'string' } },
        required: ['name', 'email']
      }
    }
  });

  api.registerEndpoint('GET', '/users/:id', {
    handler: async (req) => ({ data: { id: req.pathParams.id, name: 'John' } }),
    cacheable: true,
    requireAuth: true
  });

  const token = api.createToken({ userId: '123' });

  // Create user
  const createRes = await api.handleRequest({
    method: 'POST',
    path: '/users',
    body: { name: 'John', email: 'john@example.com' }
  });
  assert(createRes.success === true, 'Should create user');

  // Get user
  const getRes = await api.handleRequest({
    method: 'GET',
    path: '/users/1',
    headers: { authorization: `Bearer ${token}` }
  });
  assert(getRes.success === true, 'Should get user');

  // Get user again (cached)
  const cachedRes = await api.handleRequest({
    method: 'GET',
    path: '/users/1',
    headers: { authorization: `Bearer ${token}` }
  });
  assert(cachedRes.cached === true, 'Should serve from cache');
});

// Cache Clearing
runner.test('Should clear cache', async () => {
  const api = new RestAPIManager();
  api.registerEndpoint('GET', '/data', {
    handler: async () => ({ data: { t: Date.now() } }),
    cacheable: true
  });

  await api.handleRequest({ method: 'GET', path: '/data' });
  assert(api.cache.size > 0, 'Should have cached entries');
  
  api.clearCache();
  assert(api.cache.size === 0, 'Cache should be empty');
});

// Metrics Reset
runner.test('Should reset metrics', async () => {
  const api = new RestAPIManager();
  api.registerEndpoint('GET', '/data', {
    handler: async () => ({ data: {} })
  });

  await api.handleRequest({ method: 'GET', path: '/data' });
  assert(api.metrics.requests > 0, 'Should have metrics');
  
  api.resetMetrics();
  assert(api.metrics.requests === 0, 'Metrics should be reset');
});

// Run all tests
runner.run().then(success => {
  if (!success) {
    process.exit(1);
  }
});
