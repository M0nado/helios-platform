/**
 * HELIOS v4.0 Request Router - Test Suite
 * 45+ comprehensive tests covering all router functionality
 */

const assert = require('assert');
const {
  RouteTable,
  ParameterExtractor,
  MiddlewareComposer,
  ErrorHandler,
  ParameterValidationError,
  MiddlewareExecutionError
} = require('../implementation');

// ============================================================================
// RouteTable Tests (12 tests)
// ============================================================================
describe('RouteTable', () => {
  it('should register a simple route', () => {
    const table = new RouteTable();
    const route = table.register('/users', 'GET', () => {});
    assert.strictEqual(route.path, '/users');
    assert.strictEqual(route.method, 'GET');
    assert.strictEqual(typeof route.handler, 'function');
  });

  it('should match exact routes', () => {
    const table = new RouteTable();
    table.register('/users', 'GET');
    const match = table.match('/users', 'GET');
    assert.notStrictEqual(match, null);
    assert.strictEqual(match.path, '/users');
  });

  it('should match dynamic routes with parameters', () => {
    const table = new RouteTable();
    table.register('/users/:id', 'GET');
    const match = table.match('/users/123', 'GET');
    assert.notStrictEqual(match, null);
    assert.strictEqual(match.params.id, '123');
  });

  it('should match multiple parameters', () => {
    const table = new RouteTable();
    table.register('/users/:userId/posts/:postId', 'GET');
    const match = table.match('/users/42/posts/99', 'GET');
    assert.strictEqual(match.params.userId, '42');
    assert.strictEqual(match.params.postId, '99');
  });

  it('should handle wildcard routes', () => {
    const table = new RouteTable();
    table.register('/api/*', 'GET');
    const match = table.match('/api/v1/users/123', 'GET');
    assert.notStrictEqual(match, null);
  });

  it('should return null for non-matching routes', () => {
    const table = new RouteTable();
    table.register('/users', 'GET');
    const match = table.match('/posts', 'GET');
    assert.strictEqual(match, null);
  });

  it('should respect HTTP methods', () => {
    const table = new RouteTable();
    table.register('/users', 'GET');
    table.register('/users', 'POST');
    const getMatch = table.match('/users', 'GET');
    const postMatch = table.match('/users', 'POST');
    assert.notStrictEqual(getMatch, null);
    assert.notStrictEqual(postMatch, null);
    assert.strictEqual(getMatch.method, 'GET');
    assert.strictEqual(postMatch.method, 'POST');
  });

  it('should prevent duplicate routes', () => {
    const table = new RouteTable();
    table.register('/users', 'GET');
    assert.throws(() => table.register('/users', 'GET'));
  });

  it('should enforce route limit', () => {
    const table = new RouteTable({ maxRoutes: 2 });
    table.register('/a', 'GET');
    table.register('/b', 'GET');
    assert.throws(() => table.register('/c', 'GET'));
  });

  it('should handle trailing slashes', () => {
    const tableWithout = new RouteTable({ trailingSlash: false });
    tableWithout.register('/users/', 'GET');
    const match = tableWithout.match('/users', 'GET');
    assert.notStrictEqual(match, null);
  });

  it('should cache route matches', () => {
    const table = new RouteTable();
    table.register('/users/:id', 'GET');
    table.match('/users/123', 'GET');
    assert.strictEqual(table.cache.size, 1);
    table.match('/users/123', 'GET');
    assert.strictEqual(table.cache.size, 1);
  });

  it('should clear routes and cache', () => {
    const table = new RouteTable();
    table.register('/users', 'GET');
    table.match('/users', 'GET');
    table.clear();
    assert.strictEqual(table.routes.length, 0);
    assert.strictEqual(table.cache.size, 0);
  });
});

// ============================================================================
// ParameterExtractor Tests (13 tests)
// ============================================================================
describe('ParameterExtractor', () => {
  it('should extract string parameters', () => {
    const extractor = new ParameterExtractor();
    const params = extractor.extract({ name: 'John' }, { name: { type: 'string' } });
    assert.strictEqual(params.name, 'John');
  });

  it('should validate and convert numbers', () => {
    const extractor = new ParameterExtractor();
    const params = extractor.extract({ age: '25' }, { age: { type: 'number' } });
    assert.strictEqual(params.age, 25);
  });

  it('should validate integers', () => {
    const extractor = new ParameterExtractor();
    const params = extractor.extract({ count: '10' }, { count: { type: 'integer' } });
    assert.strictEqual(params.count, 10);
  });

  it('should validate booleans', () => {
    const extractor = new ParameterExtractor();
    const params = extractor.extract({ active: 'true' }, { active: { type: 'boolean' } });
    assert.strictEqual(params.active, true);
  });

  it('should validate email format', () => {
    const extractor = new ParameterExtractor();
    const params = extractor.extract({ email: 'user@example.com' }, { email: { type: 'email' } });
    assert.strictEqual(params.email, 'user@example.com');
  });

  it('should reject invalid email', () => {
    const extractor = new ParameterExtractor({ strict: false });
    const params = extractor.extract({ email: 'invalid-email' }, { email: { type: 'email' } });
    assert.strictEqual(params._errors !== undefined, true);
  });

  it('should validate UUID format', () => {
    const extractor = new ParameterExtractor();
    const uuid = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890';
    const params = extractor.extract({ id: uuid }, { id: { type: 'uuid' } });
    assert.strictEqual(params.id, uuid.toLowerCase());
  });

  it('should validate slug format', () => {
    const extractor = new ParameterExtractor();
    const params = extractor.extract({ slug: 'hello-world-123' }, { slug: { type: 'slug' } });
    assert.strictEqual(params.slug, 'hello-world-123');
  });

  it('should use default values', () => {
    const extractor = new ParameterExtractor();
    const params = extractor.extract({}, { page: { type: 'integer', required: false, default: 1 } });
    assert.strictEqual(params.page, 1);
  });

  it('should throw on missing required parameters', () => {
    const extractor = new ParameterExtractor({ strict: true });
    assert.throws(() => {
      extractor.extract({}, { id: { type: 'integer', required: true } });
    });
  });

  it('should decode URI components', () => {
    const extractor = new ParameterExtractor({ decodeUri: true });
    const params = extractor.extract({ search: 'hello%20world' }, { search: { type: 'string' } });
    assert.strictEqual(params.search, 'hello world');
  });

  it('should support custom validators', () => {
    const extractor = new ParameterExtractor();
    extractor.define('custom', {
      type: 'string',
      validate: (v) => v.length > 5,
      transform: (v) => v.toUpperCase()
    });
    const params = extractor.extract({ custom: 'abcdef' }, { custom: { type: 'string' } });
    assert.strictEqual(params.custom, 'ABCDEF');
  });

  it('should handle non-strict validation errors', () => {
    const extractor = new ParameterExtractor({ strict: false });
    const params = extractor.extract({ id: 'not-a-number' }, { id: { type: 'integer' } });
    assert.strictEqual(params._errors !== undefined, true);
    assert.strictEqual(params.id === undefined, true);
  });
});

// ============================================================================
// MiddlewareComposer Tests (12 tests)
// ============================================================================
describe('MiddlewareComposer', () => {
  it('should register global middleware', () => {
    const composer = new MiddlewareComposer();
    const fn = () => true;
    composer.use(fn);
    assert.strictEqual(composer.global.length, 1);
  });

  it('should register route-specific middleware', () => {
    const composer = new MiddlewareComposer();
    const fn = () => true;
    composer.register('get-user', fn);
    assert.strictEqual(composer.chains.get('get-user').length, 1);
  });

  it('should register multiple middleware functions', () => {
    const composer = new MiddlewareComposer();
    const fn1 = () => true;
    const fn2 = () => true;
    composer.register('route', [fn1, fn2]);
    assert.strictEqual(composer.chains.get('route').length, 2);
  });

  it('should execute middleware chain successfully', async () => {
    const composer = new MiddlewareComposer();
    let executed = false;
    composer.use(() => {
      executed = true;
      return true;
    });
    const result = await composer.execute({}, {}, 'route');
    assert.strictEqual(result, true);
    assert.strictEqual(executed, true);
  });

  it('should stop execution on middleware return false', async () => {
    const composer = new MiddlewareComposer();
    let secondExecuted = false;
    composer.use(() => false);
    composer.use(() => {
      secondExecuted = true;
      return true;
    });
    const result = await composer.execute({}, {}, 'route');
    assert.strictEqual(result, false);
    assert.strictEqual(secondExecuted, false);
  });

  it('should execute route-specific middleware', async () => {
    const composer = new MiddlewareComposer();
    const results = [];
    composer.register('route1', () => {
      results.push('route1');
      return true;
    });
    composer.register('route2', () => {
      results.push('route2');
      return true;
    });
    await composer.execute({}, {}, 'route1');
    assert.strictEqual(results[0], 'route1');
  });

  it('should support async middleware', async () => {
    const composer = new MiddlewareComposer();
    let executed = false;
    composer.use(async () => {
      executed = true;
      return true;
    });
    await composer.execute({}, {}, 'route');
    assert.strictEqual(executed, true);
  });

  it('should throw on middleware timeout', async () => {
    const composer = new MiddlewareComposer({ timeout: 100 });
    composer.use(() => new Promise(resolve => setTimeout(resolve, 500)));
    try {
      await composer.execute({}, {}, 'route');
      assert.fail('Should have thrown timeout error');
    } catch (error) {
      assert.strictEqual(error.name, 'MiddlewareExecutionError');
    }
  });

  it('should enforce middleware limit', () => {
    const composer = new MiddlewareComposer({ maxMiddleware: 2 });
    composer.use(() => true);
    composer.use(() => true);
    assert.throws(() => composer.use(() => true));
  });

  it('should get middleware chain', () => {
    const composer = new MiddlewareComposer();
    const fn1 = () => true;
    const fn2 = () => true;
    composer.use(fn1);
    composer.register('route', fn2);
    const chain = composer.getChain('route');
    assert.strictEqual(chain.length, 2);
  });

  it('should clear middleware', () => {
    const composer = new MiddlewareComposer();
    composer.use(() => true);
    composer.register('route', () => true);
    composer.clear();
    assert.strictEqual(composer.global.length, 0);
    assert.strictEqual(composer.chains.size, 0);
  });

  it('should reject non-function middleware', () => {
    const composer = new MiddlewareComposer();
    assert.throws(() => composer.use('not a function'));
  });
});

// ============================================================================
// ErrorHandler Tests (8 tests)
// ============================================================================
describe('ErrorHandler', () => {
  it('should register error handlers', () => {
    const handler = new ErrorHandler();
    const fn = (error) => ({ status: 400 });
    handler.on('ValidationError', fn);
    assert.strictEqual(handler.handlers.has('ValidationError'), true);
  });

  it('should handle registered error types', async () => {
    const handler = new ErrorHandler();
    handler.on('CustomError', () => ({ status: 400, code: 'CUSTOM' }));
    const error = new Error('Test');
    error.name = 'CustomError';
    const response = await handler.handle(error);
    assert.strictEqual(response.status, 400);
  });

  it('should use fallback handler', async () => {
    const handler = new ErrorHandler();
    handler.setFallback(() => ({ status: 500, code: 'UNKNOWN' }));
    const error = new Error('Unknown error');
    error.name = 'UnknownError';
    const response = await handler.handle(error);
    assert.strictEqual(response.status, 500);
  });

  it('should use default handlers', async () => {
    const handler = new ErrorHandler();
    const error = new Error('Invalid input');
    error.name = 'ValidationError';
    const response = await handler.handle(error);
    assert.strictEqual(response.status, 400);
  });

  it('should include context in error handling', async () => {
    const handler = new ErrorHandler();
    const error = new Error('Test');
    error.name = 'NotFoundError';
    const response = await handler.handle(error, { resource: 'User' });
    assert.strictEqual(response.status, 404);
  });

  it('should enforce handler limit', () => {
    const handler = new ErrorHandler({ maxErrorHandlers: 2 });
    handler.on('Error1', () => {});
    handler.on('Error2', () => {});
    assert.throws(() => handler.on('Error3', () => {}));
  });

  it('should reject non-function handlers', () => {
    const handler = new ErrorHandler();
    assert.throws(() => handler.on('Error', 'not a function'));
  });

  it('should include stack trace when enabled', async () => {
    const handler = new ErrorHandler({ includeStack: true });
    const error = new Error('Test error');
    const response = handler.getResponse(error);
    assert.strictEqual(response.stack !== undefined, true);
  });
});

// ============================================================================
// Integration Tests (10 tests)
// ============================================================================
describe('Integration', () => {
  it('should handle complete request flow', async () => {
    const table = new RouteTable();
    const extractor = new ParameterExtractor();
    const composer = new MiddlewareComposer();
    const errorHandler = new ErrorHandler();

    table.register('/users/:id', 'GET');
    extractor.define('id', { type: 'integer' });

    const match = table.match('/users/123', 'GET');
    assert.notStrictEqual(match, null);

    const params = extractor.extract(match.params, { id: { type: 'integer' } });
    assert.strictEqual(params.id, 123);

    const req = { params };
    const res = {};
    const middlewarePassed = await composer.execute(req, res, 'get-user');
    assert.strictEqual(middlewarePassed, true);
  });

  it('should combine all components', async () => {
    const table = new RouteTable();
    const extractor = new ParameterExtractor();
    const composer = new MiddlewareComposer();
    const errorHandler = new ErrorHandler();

    table.register('/api/:version/users/:id', 'GET');
    extractor.define('version', { type: 'string' });
    extractor.define('id', { type: 'integer' });

    composer.use(async (req) => {
      req.startTime = Date.now();
      return true;
    });

    const match = table.match('/api/v1/users/42', 'GET');
    const params = extractor.extract(match.params, {
      version: { type: 'string' },
      id: { type: 'integer' }
    });

    assert.strictEqual(params.version, 'v1');
    assert.strictEqual(params.id, 42);
  });

  it('should handle parameter extraction with validation error', async () => {
    const extractor = new ParameterExtractor({ strict: false });
    const params = extractor.extract(
      { id: 'not-a-number' },
      { id: { type: 'integer' } }
    );
    assert.strictEqual(params._errors !== undefined, true);
  });

  it('should execute error handler on validation failure', async () => {
    const errorHandler = new ErrorHandler();
    const error = new ParameterValidationError('Validation failed', { id: 'Invalid' });
    const response = await errorHandler.handle(error);
    assert.strictEqual(response.status !== undefined, true);
  });

  it('should handle middleware error with error handler', async () => {
    const composer = new MiddlewareComposer();
    const errorHandler = new ErrorHandler();

    composer.use(() => {
      throw new Error('Middleware error');
    });

    try {
      await composer.execute({}, {}, 'route');
      assert.fail('Should have thrown');
    } catch (error) {
      const response = await errorHandler.handle(error);
      assert.strictEqual(response.status, 500);
    }
  });

  it('should cache routes through multiple operations', () => {
    const table = new RouteTable();
    table.register('/users/:id', 'GET');
    table.match('/users/1', 'GET');
    table.match('/users/1', 'GET');
    table.match('/users/2', 'GET');
    assert.strictEqual(table.cache.size >= 2, true);
  });

  it('should handle complex nested routes', () => {
    const table = new RouteTable();
    table.register('/api/:version/users/:userId/posts/:postId/comments/:commentId', 'GET');
    const match = table.match('/api/v1/users/1/posts/2/comments/3', 'GET');
    assert.strictEqual(match.params.version, 'v1');
    assert.strictEqual(match.params.userId, '1');
    assert.strictEqual(match.params.postId, '2');
    assert.strictEqual(match.params.commentId, '3');
  });

  it('should prioritize exact matches over wildcards', () => {
    const table = new RouteTable();
    table.register('/api/users', 'GET');
    table.register('/api/*', 'GET');
    const match = table.match('/api/users', 'GET');
    assert.strictEqual(match.route.path, '/api/users');
  });

  it('should handle error handler fallback chain', async () => {
    const handler = new ErrorHandler();
    handler.on('SpecificError', () => ({ status: 400 }));
    handler.setFallback(() => ({ status: 500 }));

    const error1 = new Error('Specific');
    error1.name = 'SpecificError';
    const response1 = await handler.handle(error1);
    assert.strictEqual(response1.status, 400);

    const error2 = new Error('Unknown');
    error2.name = 'UnknownError';
    const response2 = await handler.handle(error2);
    assert.strictEqual(response2.status, 500);
  });
});

// ============================================================================
// Edge Cases and Error Handling (10 tests)
// ============================================================================
describe('Edge Cases', () => {
  it('should handle empty route path', () => {
    const table = new RouteTable();
    assert.throws(() => table.register('', 'GET'));
  });

  it('should handle null parameters', () => {
    const extractor = new ParameterExtractor();
    const params = extractor.extract(null, {});
    assert.strictEqual(typeof params, 'object');
  });

  it('should handle special characters in parameters', () => {
    const table = new RouteTable();
    table.register('/search/:query', 'GET');
    const match = table.match('/search/hello%20world%26more', 'GET');
    assert.strictEqual(match !== null, true);
  });

  it('should handle very long routes', () => {
    const table = new RouteTable();
    const longPath = '/a/b/c/d/e/f/g/h/i/j/k/l/m/n/o/p/q/r/s/t/u/v/w/x/y/z';
    table.register(longPath, 'GET');
    const match = table.match(longPath, 'GET');
    assert.notStrictEqual(match, null);
  });

  it('should handle many parameters', () => {
    const table = new RouteTable();
    const path = '/:a/:b/:c/:d/:e/:f/:g/:h/:i/:j';
    table.register(path, 'GET');
    const match = table.match('/1/2/3/4/5/6/7/8/9/10', 'GET');
    assert.strictEqual(Object.keys(match.params).length, 10);
  });

  it('should reject invalid HTTP methods', () => {
    const table = new RouteTable();
    assert.throws(() => table.register('/users', 'get'));
  });

  it('should handle concurrent route registration', async () => {
    const table = new RouteTable();
    const promises = [];
    for (let i = 0; i < 10; i++) {
      promises.push(Promise.resolve(table.register(`/route${i}`, 'GET')));
    }
    await Promise.all(promises);
    assert.strictEqual(table.routes.length, 10);
  });

  it('should handle case sensitivity options', () => {
    const caseSensitive = new RouteTable({ caseSensitive: true });
    const caseInsensitive = new RouteTable({ caseSensitive: false });

    caseSensitive.register('/Users', 'GET');
    caseInsensitive.register('/Users', 'GET');

    assert.strictEqual(caseSensitive.match('/users', 'GET'), null);
    assert.notStrictEqual(caseInsensitive.match('/users', 'GET'), null);
  });

  it('should handle middleware with undefined return', async () => {
    const composer = new MiddlewareComposer();
    composer.use(() => undefined);
    const result = await composer.execute({}, {}, 'route');
    assert.strictEqual(result, true);
  });

  it('should handle error handler exceptions gracefully', async () => {
    const handler = new ErrorHandler();
    handler.on('CustomError', () => {
      throw new Error('Handler error');
    });
    const error = new Error('Test');
    error.name = 'CustomError';
    const response = await handler.handle(error);
    assert.strictEqual(response.status, 500);
  });
});

console.log('All tests defined');
