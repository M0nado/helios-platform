/**
 * HELIOS v4.0 Request Router - Usage Examples
 * Real-world scenarios and best practices
 */

const { createRouter, createSimpleRouter, RouteTable, ParameterExtractor, MiddlewareComposer, ErrorHandler } = require('./index');

// ============================================================================
// Example 1: Simple REST API Router
// ============================================================================
console.log('\n=== Example 1: Simple REST API ===\n');

const router1 = createSimpleRouter();

// Register routes
router1.register('/users', 'GET', (req, res) => ({ action: 'list users' }));
router1.register('/users/:id', 'GET', (req, res) => ({ action: 'get user', id: req.params.id }));
router1.register('/users', 'POST', (req, res) => ({ action: 'create user' }));
router1.register('/users/:id', 'PUT', (req, res) => ({ action: 'update user', id: req.params.id }));
router1.register('/users/:id', 'DELETE', (req, res) => ({ action: 'delete user', id: req.params.id }));

// Match routes
const match1 = router1.match('/users/123', 'GET');
console.log('Route match:', match1);
// Output: Route match info with params: { id: '123' }

// ============================================================================
// Example 2: Dynamic Routes with Multiple Parameters
// ============================================================================
console.log('\n=== Example 2: Dynamic Routes ===\n');

const router2 = createSimpleRouter();

router2.register('/api/:version/users/:id/posts/:postId', 'GET');
router2.register('/files/:path', 'GET');
router2.register('/search/:query', 'GET');

const match2a = router2.match('/api/v1/users/42/posts/99', 'GET');
console.log('Multi-param match:', match2a?.route.paramNames);
// Output: ['version', 'id', 'postId']

const match2b = router2.match('/search/nodejs+tutorial', 'GET');
console.log('Search match params:', match2b?.params);

// ============================================================================
// Example 3: Parameter Validation and Transformation
// ============================================================================
console.log('\n=== Example 3: Parameter Validation ===\n');

const router3 = createRouter();
const extractor = router3.params;

// Define parameter validators
extractor.define('userId', {
  type: 'integer',
  required: true
});

extractor.define('email', {
  type: 'email',
  required: true
});

extractor.define('slug', {
  type: 'slug',
  required: true
});

extractor.define('uuid', {
  type: 'uuid',
  required: false,
  default: null
});

// Extract parameters with validation
try {
  const params = extractor.extract(
    { userId: '123', email: 'user@example.com', slug: 'hello-world', uuid: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890' },
    {
      userId: { type: 'integer' },
      email: { type: 'email' },
      slug: { type: 'slug' },
      uuid: { type: 'uuid', required: false }
    }
  );
  console.log('Extracted params:', params);
} catch (error) {
  console.error('Validation error:', error.errors);
}

// ============================================================================
// Example 4: Global and Route-Specific Middleware
// ============================================================================
console.log('\n=== Example 4: Middleware Chains ===\n');

const router4 = createSimpleRouter();

// Global middleware (runs for all routes)
router4.use(async (req, res) => {
  console.log(`[${new Date().toISOString()}] ${req.method} ${req.path}`);
  return true;
});

router4.use(async (req, res) => {
  if (!req.headers.authorization) {
    res.status = 401;
    res.body = { error: 'Unauthorized' };
    return false; // Stop execution
  }
  return true;
});

// Route-specific middleware
const authMiddleware = async (req, res) => {
  if (req.headers.authorization.startsWith('Bearer ')) {
    req.user = { id: 1, name: 'John' };
    return true;
  }
  return false;
};

router4.registerMiddleware('get-user', authMiddleware);

// Execute middleware chain
(async () => {
  const req = {
    method: 'GET',
    path: '/users/1',
    headers: { authorization: 'Bearer token123' }
  };
  const res = { status: 200 };

  const middlewarePassed = await router4.executeMiddleware(req, res, 'get-user');
  console.log('Middleware passed:', middlewarePassed);
  console.log('Request after middleware:', req.user);
})();

// ============================================================================
// Example 5: Error Handling
// ============================================================================
console.log('\n=== Example 5: Error Handling ===\n');

const router5 = createRouter();
const errorHandler = router5.errors;

// Register custom error handlers
errorHandler.on('ValidationError', (error, context) => ({
  status: 400,
  code: 'VALIDATION_ERROR',
  message: error.message,
  field: error.field
}));

errorHandler.on('NotFoundError', (error, context) => ({
  status: 404,
  code: 'RESOURCE_NOT_FOUND',
  message: `${context.resource || 'Resource'} not found`
}));

errorHandler.on('AuthorizationError', (error, context) => ({
  status: 403,
  code: 'FORBIDDEN',
  message: 'You do not have permission to access this resource'
}));

// Handle errors
(async () => {
  const error1 = new Error('Invalid email format');
  error1.name = 'ValidationError';
  const response1 = await errorHandler.handle(error1, { field: 'email' });
  console.log('Validation error response:', response1);

  const error2 = new Error('User not found');
  error2.name = 'NotFoundError';
  const response2 = await errorHandler.handle(error2, { resource: 'User' });
  console.log('Not found error response:', response2);
})();

// ============================================================================
// Example 6: Complete Request Handling Pipeline
// ============================================================================
console.log('\n=== Example 6: Complete Pipeline ===\n');

const router6 = createRouter();

router6.routes.register('/api/users/:id/profile', 'GET', (req, res) => ({
  action: 'get user profile',
  userId: req.params.id
}));

// Define parameter validators
router6.params.define('id', { type: 'integer', required: true });

// Global middleware
router6.middleware.use(async (req, res) => {
  req.startTime = Date.now();
  return true;
});

// Error handler
router6.errors.on('Error', (error) => ({
  status: 500,
  code: 'INTERNAL_ERROR',
  message: 'An error occurred while processing your request'
}));

// Execute complete pipeline
(async () => {
  const path = '/api/users/42/profile';
  const method = 'GET';

  // 1. Match route
  const match = router6.routes.match(path, method);
  if (!match) {
    console.log('Route not found');
    return;
  }

  // 2. Extract and validate parameters
  const params = router6.params.extract(match.params, {
    id: { type: 'integer', required: true }
  });

  // 3. Execute middleware
  const req = { path, method, params, headers: {} };
  const res = { status: 200 };
  const middlewarePassed = await router6.middleware.execute(req, res, match.route.key);

  if (!middlewarePassed) {
    console.log('Middleware blocked request');
    return;
  }

  // 4. Execute handler
  const responseData = match.handler(req, res);
  console.log('Pipeline result:', responseData);
})();

// ============================================================================
// Example 7: Wildcard and Pattern Routes
// ============================================================================
console.log('\n=== Example 7: Advanced Routing Patterns ===\n');

const router7 = createSimpleRouter();

router7.register('/api/*', 'GET');
router7.register('/static/:filepath*', 'GET');
router7.register('/uploads/:year/:month/:filename', 'GET');

const match7a = router7.match('/api/v1/users/123/posts', 'GET');
console.log('Wildcard match (api):', match7a !== null);

const match7b = router7.match('/uploads/2024/01/image.jpg', 'GET');
console.log('Date-based match:', match7b?.params);

// ============================================================================
// Example 8: Case Sensitivity and Trailing Slashes
// ============================================================================
console.log('\n=== Example 8: Route Normalization ===\n');

const caseSensitiveRouter = new RouteTable({ caseSensitive: true });
const caseInsensitiveRouter = new RouteTable({ caseSensitive: false });
const trailingSlashRouter = new RouteTable({ trailingSlash: true });

caseSensitiveRouter.register('/Users', 'GET');
caseInsensitiveRouter.register('/Users', 'GET');

console.log('Case sensitive /users match:', caseSensitiveRouter.match('/users', 'GET') !== null);
console.log('Case insensitive /users match:', caseInsensitiveRouter.match('/users', 'GET') !== null);

trailingSlashRouter.register('/api/users/', 'GET');
console.log('Trailing slash /api/users match:', trailingSlashRouter.match('/api/users', 'GET') !== null);

// ============================================================================
// Example 9: Route Cache Performance
// ============================================================================
console.log('\n=== Example 9: Performance ===\n');

const perfRouter = createSimpleRouter();
const routes = [
  ['/users', 'GET'],
  ['/users/:id', 'GET'],
  ['/users/:id/posts', 'GET'],
  ['/users/:id/posts/:postId', 'GET'],
  ['/posts', 'GET'],
  ['/posts/:id', 'GET']
];

routes.forEach(([path, method]) => perfRouter.register(path, method));

console.time('First match (cold cache)');
perfRouter.match('/users/123/posts/456', 'GET');
console.timeEnd('First match (cold cache)');

console.time('Second match (warm cache)');
perfRouter.match('/users/123/posts/456', 'GET');
console.timeEnd('Second match (warm cache)');

// ============================================================================
// Example 10: Real-world Microservice Router
// ============================================================================
console.log('\n=== Example 10: Microservice Router ===\n');

function createMicroserviceRouter() {
  const router = createRouter();

  // Setup auth middleware
  const checkAuth = async (req, res) => {
    const token = req.headers.authorization?.replace('Bearer ', '');
    if (!token) {
      res.status = 401;
      return false;
    }
    req.user = { id: 1, token };
    return true;
  };

  // Setup rate limiting
  let requestCount = 0;
  const checkRateLimit = async (req, res) => {
    requestCount++;
    if (requestCount > 100) {
      res.status = 429;
      return false;
    }
    return true;
  };

  // Setup error handlers
  router.errors.on('NotFoundError', () => ({
    status: 404,
    code: 'NOT_FOUND',
    message: 'Resource not found'
  }));

  return { router, checkAuth, checkRateLimit };
}

const { router: msRouter } = createMicroserviceRouter();
msRouter.routes.register('/api/v1/users/:id', 'GET');
const msMatch = msRouter.routes.match('/api/v1/users/123', 'GET');
console.log('Microservice match:', msMatch?.params);

console.log('\n=== All examples completed ===\n');
