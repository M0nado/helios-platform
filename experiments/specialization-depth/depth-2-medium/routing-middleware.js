/**
 * HELIOS REST API - Medium Specialist (Depth 2)
 * Agent 1: Endpoint Design & OpenAPI
 * 
 * Responsible for:
 * - Route registration and matching
 * - API versioning
 * - Parameter extraction
 * - OpenAPI specification generation
 * 
 * @module routing-middleware
 * @version 1.0.0
 */

const crypto = require('crypto');

/**
 * Routing & OpenAPI Manager
 * Handles endpoint design, versioning, and API documentation
 * @class RoutingManager
 */
class RoutingManager {
  /**
   * Initialize Routing Manager
   * @param {Object} config - Configuration
   * @param {string} config.apiVersion - API version
   */
  constructor(config = {}) {
    this.apiVersion = config.apiVersion || 'v1';
    this.routes = new Map();
    this.schemas = new Map();
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
        description: 'REST API with medium specialization (Routing + Validation)'
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
   * @param {string} method - HTTP method
   * @param {string} path - Endpoint path
   * @param {Object} options - Endpoint options
   * @param {Function} options.handler - Request handler
   * @param {Object} options.schema - Input/output schema
   * @param {boolean} options.requireAuth - Requires authentication
   * @param {boolean} options.cacheable - Response is cacheable
   * @param {string} options.description - Endpoint description
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
        200: { description: 'Success' },
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
   * Find matching route
   * @param {string} method - HTTP method
   * @param {string} path - Request path
   * @returns {Object|null} Matched route
   */
  findRoute(method, path) {
    const routeKey = `${method} ${path}`;
    if (this.routes.has(routeKey)) {
      return this.routes.get(routeKey);
    }

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
   * Extract path parameters
   * @param {string} path - Request path
   * @returns {Object} Path parameters
   */
  extractPathParams(path) {
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
   * Get OpenAPI specification
   * @returns {Object} OpenAPI spec
   */
  getOpenAPISpec() {
    return this.openApiSpec;
  }

  /**
   * Get all routes
   * @returns {Array} Routes array
   */
  getRoutes() {
    return Array.from(this.routes.values());
  }
}

module.exports = RoutingManager;
