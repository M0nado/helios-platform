# HELIOS v4.0 Request Router Module

Advanced HTTP request routing engine with dynamic parameters, middleware composition, and comprehensive error handling.

## Features

- **Route Matching**: Static routes, dynamic parameters, wildcards, regex patterns
- **Parameter Extraction**: Type validation, transformation, URI decoding
- **Middleware Composition**: Global and route-specific middleware chains
- **Error Handling**: Centralized error handling with custom handlers
- **Performance**: Route caching, optimized pattern matching
- **Production-Ready**: Full JSDoc, error handling, validation

## Installation

```javascript
const { createRouter, createSimpleRouter } = require('./index');
```

## Quick Start

```javascript
const router = createSimpleRouter();

// Register routes
router.register('/users/:id', 'GET', (req, res) => {
  return { userId: req.params.id };
});

// Match routes
const match = router.match('/users/123', 'GET');
// { params: { id: '123' }, handler: Function, ... }

// Use middleware
router.use(async (req, res) => {
  console.log(`${req.method} ${req.path}`);
  return true;
});

// Execute middleware
await router.executeMiddleware(req, res, 'route-key');

// Handle errors
await router.handleError(error, { route: 'get-user' });
```

## API Reference

### RouteTable

Manages route definitions and matching logic.

#### Constructor

```javascript
const table = new RouteTable(options);
// options:
//   - caseSensitive: boolean (default: false)
//   - trailingSlash: boolean (default: false)
//   - maxRoutes: number (default: 10000)
```

#### Methods

##### `register(path, method, handler?)`

Register a route with optional handler.

```javascript
table.register('/users/:id', 'GET', (req, res) => {});
table.register('/api/*/data', 'POST');
```

**Parameters:**
- `path` (string): Route path with parameters (`:param`) and wildcards (`*`)
- `method` (string): HTTP method (GET, POST, PUT, DELETE, PATCH, etc.)
- `handler` (Function, optional): Route handler function

**Returns:** Route definition object

**Throws:**
- Invalid path (must start with `/`)
- Invalid method (must be uppercase)
- Duplicate route
- Route limit exceeded

##### `match(path, method)`

Find a route matching the given path and method.

```javascript
const match = table.match('/users/123', 'GET');
if (match) {
  console.log(match.params.id); // '123'
  match.handler(req, res);
}
```

**Parameters:**
- `path` (string): Request path
- `method` (string): HTTP method

**Returns:** Match object with `route`, `params`, `handler`, or `null`

##### `getRoutes()`

Get all registered routes.

```javascript
const routes = table.getRoutes();
// [{ path, method, handler, paramNames, ... }, ...]
```

##### `clear()`

Clear all routes and cache.

```javascript
table.clear();
```

### ParameterExtractor

Extracts, validates, and transforms route parameters.

#### Constructor

```javascript
const extractor = new ParameterExtractor(options);
// options:
//   - strict: boolean (default: true) - Throw on validation failure
//   - decodeUri: boolean (default: true) - Auto-decode URI components
```

#### Methods

##### `define(name, schema)`

Define a parameter validator.

```javascript
extractor.define('id', {
  type: 'integer',
  required: true
});

extractor.define('email', {
  type: 'email',
  required: true,
  validate: (v) => v.includes('@'),
  transform: (v) => v.toLowerCase()
});
```

**Parameters:**
- `name` (string): Parameter name
- `schema` (Object):
  - `type`: Parameter type (string, number, integer, boolean, email, uuid, slug)
  - `required`: Must be present (default: true)
  - `default`: Default value if missing
  - `validate`: Custom validation function
  - `transform`: Custom transformation function

##### `extract(params, schemas)`

Extract and validate parameters.

```javascript
const extracted = extractor.extract(
  { id: '123', email: 'user@example.com' },
  {
    id: { type: 'integer' },
    email: { type: 'email' }
  }
);
// { id: 123, email: 'user@example.com' }
```

**Parameters:**
- `params` (Object): Raw parameters
- `schemas` (Object): Parameter schemas

**Returns:** Extracted parameters object

**Throws:** `ParameterValidationError` if strict mode and validation fails

### MiddlewareComposer

Manages middleware chains with error handling and async support.

#### Constructor

```javascript
const composer = new MiddlewareComposer(options);
// options:
//   - maxMiddleware: number (default: 100)
//   - timeout: number (default: 30000) - Timeout in ms
```

#### Methods

##### `use(fn, options?)`

Register global middleware.

```javascript
composer.use(async (req, res) => {
  console.log('Global middleware');
  return true; // Continue, false to stop
});
```

**Parameters:**
- `fn` (Function): Middleware function
- `options` (Object, optional):
  - `mode`: 'serial' or 'parallel' (default: 'serial')

##### `register(route, middleware)`

Register route-specific middleware.

```javascript
composer.register('get-user', [authMiddleware, validateMiddleware]);
composer.register('create-user', permissionMiddleware);
```

**Parameters:**
- `route` (string): Route identifier
- `middleware` (Function or Array<Function>): Middleware function(s)

##### `execute(req, res, route)`

Execute middleware chain for a request.

```javascript
const passed = await composer.execute(req, res, 'get-user');
if (!passed) {
  // Middleware halted execution
}
```

**Parameters:**
- `req` (Object): Request object
- `res` (Object): Response object
- `route` (string): Route identifier

**Returns:** Promise<boolean> - True if passed, false if halted

**Throws:** `MiddlewareExecutionError` on failure or timeout

##### `getChain(route)`

Get middleware chain for a route.

```javascript
const chain = composer.getChain('get-user');
```

##### `clear()`

Clear all middleware.

```javascript
composer.clear();
```

### ErrorHandler

Centralized error handling and error routing.

#### Constructor

```javascript
const handler = new ErrorHandler(options);
// options:
//   - includeStack: boolean (default: false) - Include stack traces
//   - maxErrorHandlers: number (default: 50)
```

#### Methods

##### `on(type, handler)`

Register an error handler.

```javascript
handler.on('ValidationError', (error, context) => ({
  status: 400,
  code: 'VALIDATION_ERROR',
  message: error.message,
  field: error.field
}));

handler.on(CustomErrorClass, (error, context) => ({
  status: 500,
  code: 'CUSTOM_ERROR',
  message: error.message
}));
```

**Parameters:**
- `type` (string or Function): Error type name or constructor
- `handler` (Function): Error handler function

##### `setFallback(handler)`

Set fallback error handler.

```javascript
handler.setFallback((error, context) => ({
  status: 500,
  code: 'INTERNAL_ERROR',
  message: 'An error occurred'
}));
```

##### `handle(error, context?)`

Handle an error.

```javascript
const response = await handler.handle(error, {
  route: 'get-user',
  req: request,
  res: response
});
```

**Parameters:**
- `error` (Error): Error object
- `context` (Object, optional): Error context

**Returns:** Promise<Object> - Error response

##### `getResponse(error)`

Get error response for error type.

```javascript
const response = handler.getResponse(error);
```

## Usage Examples

### Simple REST API

```javascript
const { createSimpleRouter } = require('./index');

const router = createSimpleRouter();

// Register CRUD routes
router.register('/users', 'GET', listUsers);
router.register('/users/:id', 'GET', getUser);
router.register('/users', 'POST', createUser);
router.register('/users/:id', 'PUT', updateUser);
router.register('/users/:id', 'DELETE', deleteUser);

// Match and execute
const match = router.match(req.path, req.method);
if (match) {
  const result = await match.handler(req, res);
  res.json(result);
}
```

### Parameter Validation

```javascript
const { createRouter } = require('./index');

const router = createRouter();

router.params.define('userId', {
  type: 'integer',
  required: true
});

router.params.define('email', {
  type: 'email',
  required: true
});

const validated = router.params.extract(
  { userId: '123', email: 'user@example.com' },
  {
    userId: { type: 'integer' },
    email: { type: 'email' }
  }
);
```

### Middleware Chain

```javascript
const router = createSimpleRouter();

// Global middleware
router.use(async (req, res) => {
  req.startTime = Date.now();
  return true;
});

router.use(async (req, res) => {
  if (!req.headers.authorization) {
    res.status = 401;
    return false; // Stop chain
  }
  return true;
});

// Route-specific middleware
const authMiddleware = async (req, res) => {
  const token = req.headers.authorization.replace('Bearer ', '');
  req.user = validateToken(token);
  return req.user !== null;
};

router.registerMiddleware('get-user', authMiddleware);

// Execute
const passed = await router.executeMiddleware(req, res, 'get-user');
```

### Error Handling

```javascript
const router = createSimpleRouter();

router.errors.on('ValidationError', (error) => ({
  status: 400,
  code: 'INVALID_INPUT',
  message: error.message
}));

router.errors.on('NotFoundError', (error) => ({
  status: 404,
  code: 'NOT_FOUND',
  message: 'Resource not found'
}));

// Handle errors
try {
  // ... route execution
} catch (error) {
  const response = await router.errors.handle(error, { route: 'get-user' });
  res.status(response.status).json(response);
}
```

## Performance Characteristics

- **Route Matching**: O(n) worst case, O(1) amortized with caching
- **Parameter Extraction**: O(m) where m = number of parameters
- **Middleware Execution**: O(k) where k = number of middleware functions
- **Memory**: ~2KB per route, ~1KB per parameter, ~500B per middleware

## Error Handling

The module provides several custom error classes:

- **ParameterValidationError**: Parameter validation failed
- **MiddlewareExecutionError**: Middleware execution failed

All errors include context and stack traces for debugging.

## Testing

Run tests with:

```bash
npm test
```

Coverage includes:
- 12 RouteTable tests
- 13 ParameterExtractor tests
- 12 MiddlewareComposer tests
- 8 ErrorHandler tests
- 10 Integration tests
- 10 Edge case tests

Total: 65+ comprehensive tests

## License

HELIOS v4.0 - Enterprise Request Router Module
