/**
 * HELIOS v4.0 Request Router Module - Public API
 * @module mod-router
 * @version 4.0.0
 */

const {
  RouteTable,
  ParameterExtractor,
  MiddlewareComposer,
  ErrorHandler,
  ParameterValidationError,
  MiddlewareExecutionError
} = require('./implementation');

/**
 * Create a new Router instance with all components integrated
 * @param {Object} options - Router configuration
 * @returns {Object} Router instance with RouteTable, ParameterExtractor, MiddlewareComposer, ErrorHandler
 */
function createRouter(options = {}) {
  return {
    routes: new RouteTable(options.routes),
    params: new ParameterExtractor(options.params),
    middleware: new MiddlewareComposer(options.middleware),
    errors: new ErrorHandler(options.errors)
  };
}

/**
 * Create a request router with convenience methods
 * @param {Object} options - Configuration options
 * @returns {Object} Router with register, match, execute methods
 */
function createSimpleRouter(options = {}) {
  const router = createRouter(options);

  return {
    /**
     * Register a route
     * @param {string} path - Route path
     * @param {string} method - HTTP method
     * @param {Function} handler - Route handler
     * @returns {Object} Route definition
     */
    register: (path, method, handler) => router.routes.register(path, method, handler),

    /**
     * Match a route
     * @param {string} path - Request path
     * @param {string} method - HTTP method
     * @returns {Object|null} Route match or null
     */
    match: (path, method) => router.routes.match(path, method),

    /**
     * Use middleware
     * @param {Function} fn - Middleware function
     * @returns {void}
     */
    use: (fn) => router.middleware.use(fn),

    /**
     * Register route middleware
     * @param {string} route - Route identifier
     * @param {Function|Array} middleware - Middleware
     * @returns {void}
     */
    registerMiddleware: (route, middleware) => router.middleware.register(route, middleware),

    /**
     * Execute middleware for a route
     * @async
     * @param {Object} req - Request
     * @param {Object} res - Response
     * @param {string} route - Route identifier
     * @returns {Promise<boolean>}
     */
    executeMiddleware: (req, res, route) => router.middleware.execute(req, res, route),

    /**
     * Handle an error
     * @async
     * @param {Error} error - Error object
     * @param {Object} context - Error context
     * @returns {Promise<Object>}
     */
    handleError: (error, context) => router.errors.handle(error, context),

    /**
     * Get all registered routes
     * @returns {Array}
     */
    getRoutes: () => router.routes.getRoutes(),

    /**
     * Clear all routes and middleware
     * @returns {void}
     */
    clear: () => {
      router.routes.clear();
      router.middleware.clear();
    }
  };
}

module.exports = {
  RouteTable,
  ParameterExtractor,
  MiddlewareComposer,
  ErrorHandler,
  ParameterValidationError,
  MiddlewareExecutionError,
  createRouter,
  createSimpleRouter,
  version: '4.0.0'
};
