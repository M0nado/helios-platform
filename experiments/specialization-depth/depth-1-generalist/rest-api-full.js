/**
 * HELIOS REST API - Generalist (Depth 1)
 * 
 * A comprehensive, single-module REST API implementation covering all features:
 * - Endpoint design and routing
 * - API versioning
 * - Authentication & authorization
 * - Request/response validation
 * - Caching strategies
 * - Error handling
 * - OpenAPI documentation
 * - Monitoring & telemetry
 * 
 * @module rest-api-full
 * @version 1.0.0
 */

const crypto = require('crypto');
const jwt = require('jsonwebtoken');

/**
 * REST API Manager - Handles all REST API features
 * @class RestAPIManager
 */
class RestAPIManager {
  /**
   * Initialize REST API Manager
   * @param {Object} config - Configuration
   * @param {string} config.jwtSecret - JWT signing secret
   * @param {number} config.apiVersion - Default API version
   * @param {Object} config.cache - Cache configuration
   * @param {number} config.cache.ttl - Cache TTL in seconds
   * @param {number} config.cache.maxSize - Max cache size
   * @param {Object} config.monitoring - Monitoring config
   * @param {string} config.monitoring.serviceName - Service name
   */
  constructor(config = {}) {
    this.jwtSecret = config.jwtSecret || 'default-secret-key';
    this.apiVersion = config.apiVersion || 'v1';
    this.cacheConfig = config.cache || { ttl: 300, maxSize: 1000 };
    this.monitoringConfig = config.monitoring || { serviceName: 'rest-api' };
    
    // Internal state
    this.routes = new Map();
    this.cache = new Map();
    this.schemas = new Map();
    this.middlewares = [];
    this.metrics = {
      requests: 0,
      errors: 0,
      cacheHits: 0,
      cacheMisses: 0,
      avgResponseTime: 0,
      responseTimes: []
    };
    this.openApiSpec = this._initializeOpenAPI();
  }

  /**
   * Initialize OpenAPI specification
   * @private
   * @returns {Object} OpenAPI spec
   */
  _initializeOpenAPI() {
    return {
      openapi: '3.0.0',
      info: {
        title: 'HELIOS REST API',
        version: this.apiVersion,
        description: 'Comprehensive REST API with all features'
      },
      servers: [{ url: `/api/${this.apiVersion}` }],
      paths: {},
      components: {
        schemas: {},
        securitySchemes: {
          bearerAuth: {
            type: 'http',
            scheme: 'bearer',
            bearerFormat: 'JWT'
          }
        }
      }
    };
  }

  /**
   * Register a REST endpoint
   * @param {string} method - HTTP method (GET, POST, PUT, DELETE)
   * @param {string} path - Endpoint path
   * @param {Object} options - Endpoint options
   * @param {Function} options.handler - Request handler
   * @param {Object} options.schema - Input/output schema
   * @param {boolean} options.requireAuth - Requires authentication
   * @param {boolean} options.cacheable - Response is cacheable
   * @param {string} options.description - Endpoint description
   * @returns {void}
   */
  registerEndpoint(method, path, options = {}) {
    const {
      handler,
      schema = {},
      requireAuth = false,
      cacheable = false,
      description = ''
    } = options;

    const routeKey = `${method} ${path}`;
    const route = {
      method: method.toUpperCase(),
      path,
      handler,
      schema,
      requireAuth,
      cacheable,
      description,
      version: this.apiVersion,
      registered: new Date().toISOString()
    };

    this.routes.set(routeKey, route);

    // Add to OpenAPI spec
    this._addToOpenAPI(method, path, route);
  }

  /**
   * Add route to OpenAPI specification
   * @private
   * @param {string} method - HTTP method
   * @param {string} path - Endpoint path
   * @param {Object} route - Route definition
   */
  _addToOpenAPI(method, path, route) {
    if (!this.openApiSpec.paths[path]) {
      this.openApiSpec.paths[path] = {};
    }

    this.openApiSpec.paths[path][method.toLowerCase()] = {
      summary: route.description,
      operationId: `${method.toLowerCase()}${path.replace(/\//g, '_').replace(/{|}/g, '')}`,
      security: route.requireAuth ? [{ bearerAuth: [] }] : [],
      requestBody: route.schema.request ? {
        required: true,
        content: {
          'application/json': {
            schema: route.schema.request
          }
        }
      } : undefined,
      responses: {
        200: {
          description: 'Success',
          content: {
            'application/json': {
              schema: route.schema.response || { type: 'object' }
            }
          }
        },
        400: { description: 'Bad Request' },
        401: { description: 'Unauthorized' },
        404: { description: 'Not Found' },
        500: { description: 'Internal Server Error' }
      }
    };
  }

  /**
   * Register validation schema
   * @param {string} name - Schema name
   * @param {Object} schema - JSON Schema
   */
  registerSchema(name, schema) {
    this.schemas.set(name, schema);
    this.openApiSpec.components.schemas[name] = schema;
  }

  /**
   * Register middleware
   * @param {Function} middlewareFn - Middleware function
   */
  use(middlewareFn) {
    this.middlewares.push(middlewareFn);
  }

  /**
   * Create JWT token
   * @param {Object} payload - Token payload
   * @param {Object} options - JWT options
   * @returns {string} JWT token
   */
  createToken(payload, options = {}) {
    return jwt.sign(payload, this.jwtSecret, {
      expiresIn: options.expiresIn || '24h',
      ...options
    });
  }

  /**
   * Verify JWT token
   * @param {string} token - JWT token to verify
   * @returns {Object|null} Decoded token or null if invalid
   */
  verifyToken(token) {
    try {
      return jwt.verify(token, this.jwtSecret);
    } catch (error) {
      return null;
    }
  }

  /**
   * Validate request data against schema
   * @param {Object} data - Data to validate
   * @param {Object} schema - JSON Schema
   * @returns {Object} Validation result { valid: boolean, errors: Array }
   */
  validateRequest(data, schema) {
    const errors = [];

    // Basic required field validation
    if (schema.required) {
      for (const field of schema.required) {
        if (!(field in data)) {
          errors.push(`Missing required field: ${field}`);
        }
      }
    }

    // Type validation
    if (schema.properties) {
      for (const [field, fieldSchema] of Object.entries(schema.properties)) {
        if (field in data) {
          const value = data[field];
          const expectedType = fieldSchema.type;

          if (expectedType && typeof value !== expectedType) {
            errors.push(
              `Field '${field}' expected type '${expectedType}', got '${typeof value}'`
            );
          }

          // Pattern validation
          if (fieldSchema.pattern && typeof value === 'string') {
            const regex = new RegExp(fieldSchema.pattern);
            if (!regex.test(value)) {
              errors.push(
                `Field '${field}' does not match pattern: ${fieldSchema.pattern}`
              );
            }
          }

          // Min/Max length
          if (fieldSchema.minLength && value.length < fieldSchema.minLength) {
            errors.push(
              `Field '${field}' must be at least ${fieldSchema.minLength} characters`
            );
          }

          if (fieldSchema.maxLength && value.length > fieldSchema.maxLength) {
            errors.push(
              `Field '${field}' must be at most ${fieldSchema.maxLength} characters`
            );
          }

          // Enum validation
          if (fieldSchema.enum && !fieldSchema.enum.includes(value)) {
            errors.push(
              `Field '${field}' must be one of: ${fieldSchema.enum.join(', ')}`
            );
          }
        }
      }
    }

    return {
      valid: errors.length === 0,
      errors
    };
  }

  /**
   * Cache response data
   * @private
   * @param {string} key - Cache key
   * @param {any} value - Value to cache
   */
  _setCacheEntry(key, value) {
    if (this.cache.size >= this.cacheConfig.maxSize) {
      // Simple eviction: remove first entry
      const firstKey = this.cache.keys().next().value;
      this.cache.delete(firstKey);
    }

    this.cache.set(key, {
      value,
      timestamp: Date.now(),
      ttl: this.cacheConfig.ttl
    });
  }

  /**
   * Get cached response
   * @private
   * @param {string} key - Cache key
   * @returns {any|null} Cached value or null if expired/missing
   */
  _getCacheEntry(key) {
    const entry = this.cache.get(key);
    if (!entry) {
      this.metrics.cacheMisses++;
      return null;
    }

    const age = (Date.now() - entry.timestamp) / 1000;
    if (age > entry.ttl) {
      this.cache.delete(key);
      this.metrics.cacheMisses++;
      return null;
    }

    this.metrics.cacheHits++;
    return entry.value;
  }

  /**
   * Generate cache key from request
   * @private
   * @param {string} method - HTTP method
   * @param {string} path - Request path
   * @param {Object} query - Query parameters
   * @returns {string} Cache key
   */
  _generateCacheKey(method, path, query = {}) {
    const queryStr = Object.entries(query)
      .sort()
      .map(([k, v]) => `${k}=${v}`)
      .join('&');
    return `${method}:${path}:${queryStr}`;
  }

  /**
   * Execute middleware chain
   * @private
   * @param {Object} request - Request context
   * @returns {Promise<Object>} Middleware result
   */
  async _executeMiddlewares(request) {
    let context = { ...request, passed: true };

    for (const middleware of this.middlewares) {
      context = await middleware(context);
      if (context.passed === false) {
        break;
      }
    }

    return context;
  }

  /**
   * Process incoming request
   * @param {Object} request - HTTP request
   * @param {string} request.method - HTTP method
   * @param {string} request.path - Request path
   * @param {Object} request.query - Query parameters
   * @param {Object} request.headers - Request headers
   * @param {any} request.body - Request body
   * @returns {Promise<Object>} Response
   */
  async handleRequest(request) {
    const startTime = Date.now();
    const { method, path, query = {}, headers = {}, body = null } = request;

    try {
      this.metrics.requests++;

      // Extract path parameters
      const pathParams = this._extractPathParams(path);
      const cacheKey = this._generateCacheKey(method, path, query);

      // Check authentication
      if (headers.authorization) {
        const token = headers.authorization.replace('Bearer ', '');
        const decoded = this.verifyToken(token);
        if (!decoded) {
          return this._errorResponse(401, 'Invalid or expired token');
        }
      }

      // Find matching route
      const route = this._findRoute(method, path);
      if (!route) {
        return this._errorResponse(404, 'Endpoint not found');
      }

      // Check auth requirement
      if (route.requireAuth && !this.verifyToken(headers.authorization?.replace('Bearer ', ''))) {
        return this._errorResponse(401, 'Authentication required');
      }

      // Check cache for GET requests
      if (method === 'GET' && route.cacheable) {
        const cached = this._getCacheEntry(cacheKey);
        if (cached) {
          return this._successResponse(cached, { cached: true });
        }
      }

      // Validate request
      if (route.schema.request && body) {
        const validation = this.validateRequest(body, route.schema.request);
        if (!validation.valid) {
          return this._errorResponse(400, 'Validation failed', { errors: validation.errors });
        }
      }

      // Execute middlewares
      const context = await this._executeMiddlewares({
        method,
        path,
        pathParams,
        query,
        body,
        headers
      });

      if (context.passed === false) {
        return this._errorResponse(403, 'Middleware rejected request', context.error);
      }

      // Call handler
      const result = await route.handler({
        pathParams,
        query,
        body,
        headers,
        context
      });

      // Validate response
      if (route.schema.response && result.data) {
        const validation = this.validateRequest(result.data, route.schema.response);
        if (!validation.valid) {
          console.warn('Response validation failed:', validation.errors);
        }
      }

      // Cache successful response if cacheable
      if (method === 'GET' && route.cacheable && result.status === 200) {
        this._setCacheEntry(cacheKey, result.data);
      }

      return this._successResponse(result.data, { status: result.status || 200 });
    } catch (error) {
      this.metrics.errors++;
      return this._errorResponse(500, 'Internal server error', { message: error.message });
    } finally {
      const duration = Date.now() - startTime;
      this.metrics.responseTimes.push(duration);
      this.metrics.avgResponseTime = this.metrics.responseTimes.reduce((a, b) => a + b, 0) / 
                                      this.metrics.responseTimes.length;
    }
  }

  /**
   * Find matching route for path
   * @private
   * @param {string} method - HTTP method
   * @param {string} path - Request path
   * @returns {Object|null} Matched route
   */
  _findRoute(method, path) {
    // Exact match first
    const routeKey = `${method} ${path}`;
    if (this.routes.has(routeKey)) {
      return this.routes.get(routeKey);
    }

    // Pattern match
    for (const [key, route] of this.routes) {
      if (route.method === method.toUpperCase()) {
        const pattern = this._pathToRegex(route.path);
        if (pattern.test(path)) {
          return route;
        }
      }
    }

    return null;
  }

  /**
   * Extract path parameters from request path
   * @private
   * @param {string} path - Request path
   * @returns {Object} Path parameters
   */
  _extractPathParams(path) {
    const params = {};
    const segments = path.split('/');
    
    for (const [, route] of this.routes) {
      const routeSegments = route.path.split('/');
      if (routeSegments.length === segments.length) {
        let match = true;
        for (let i = 0; i < segments.length; i++) {
          if (routeSegments[i].startsWith(':')) {
            const paramName = routeSegments[i].substring(1);
            params[paramName] = segments[i];
          } else if (routeSegments[i] !== segments[i]) {
            match = false;
            break;
          }
        }
        if (match) break;
      }
    }

    return params;
  }

  /**
   * Convert path pattern to regex
   * @private
   * @param {string} path - Path pattern
   * @returns {RegExp} Regex pattern
   */
  _pathToRegex(path) {
    const pattern = path
      .replace(/:[^/]+/g, '[^/]+')
      .replace(/\//g, '\\/')
      .replace(/\*/g, '.*');
    return new RegExp(`^${pattern}$`);
  }

  /**
   * Format success response
   * @private
   * @param {any} data - Response data
   * @param {Object} options - Response options
   * @returns {Object} Formatted response
   */
  _successResponse(data, options = {}) {
    return {
      success: true,
      status: options.status || 200,
      data,
      timestamp: new Date().toISOString(),
      cached: options.cached || false
    };
  }

  /**
   * Format error response
   * @private
   * @param {number} status - HTTP status code
   * @param {string} message - Error message
   * @param {Object} details - Error details
   * @returns {Object} Formatted error response
   */
  _errorResponse(status, message, details = {}) {
    return {
      success: false,
      status,
      error: {
        message,
        code: `ERR_${status}`,
        details
      },
      timestamp: new Date().toISOString()
    };
  }

  /**
   * Get current metrics
   * @returns {Object} Current metrics snapshot
   */
  getMetrics() {
    return {
      ...this.metrics,
      cacheSize: this.cache.size,
      routeCount: this.routes.size
    };
  }

  /**
   * Get OpenAPI specification
   * @returns {Object} OpenAPI spec
   */
  getOpenAPISpec() {
    return this.openApiSpec;
  }

  /**
   * Get all registered routes
   * @returns {Array} Route list
   */
  getRoutes() {
    return Array.from(this.routes.values());
  }

  /**
   * Clear cache
   */
  clearCache() {
    this.cache.clear();
    this.metrics.cacheHits = 0;
    this.metrics.cacheMisses = 0;
  }

  /**
   * Reset metrics
   */
  resetMetrics() {
    this.metrics = {
      requests: 0,
      errors: 0,
      cacheHits: 0,
      cacheMisses: 0,
      avgResponseTime: 0,
      responseTimes: []
    };
  }
}

module.exports = RestAPIManager;
