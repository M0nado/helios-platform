/**
 * HELIOS REST API - Deep Specialist (Depth 3)
 * Agent 1: Endpoint Routing
 * 
 * Responsible for:
 * - Route registration and matching
 * - API versioning
 * - Path parameter extraction
 * 
 * @module routing
 * @version 1.0.0
 */

/**
 * Routing Manager
 * Handles all routing concerns
 * @class Routing
 */
class Routing {
  /**
   * Initialize Routing
   * @param {Object} config - Configuration
   * @param {string} config.apiVersion - API version
   */
  constructor(config = {}) {
    this.apiVersion = config.apiVersion || 'v1';
    this.routes = new Map();
  }

  /**
   * Register endpoint
   * @param {string} method - HTTP method
   * @param {string} path - Endpoint path
   * @param {Object} options - Options
   */
  registerEndpoint(method, path, options = {}) {
    const routeKey = `${method} ${path}`;
    const route = {
      method: method.toUpperCase(),
      path,
      handler: options.handler,
      requireAuth: options.requireAuth || false,
      cacheable: options.cacheable || false,
      description: options.description || '',
      schema: options.schema || {},
      version: this.apiVersion,
      registered: new Date().toISOString()
    };

    this.routes.set(routeKey, route);
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

    for (const [, route] of this.routes) {
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
   * @returns {RegExp} Regex
   */
  _pathToRegex(path) {
    const pattern = path
      .replace(/:[^/]+/g, '[^/]+')
      .replace(/\//g, '\\/')
      .replace(/\*/g, '.*');
    return new RegExp(`^${pattern}$`);
  }

  /**
   * Get all routes
   * @returns {Array} Routes
   */
  getRoutes() {
    return Array.from(this.routes.values());
  }
}

module.exports = Routing;
