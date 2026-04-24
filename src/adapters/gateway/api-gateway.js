/**
 * API Gateway - Central entry point for all service requests
 * Handles routing, authentication, rate limiting, and response formatting
 * @module gateway/api-gateway
 */

const crypto = require('crypto');

/**
 * ServiceRouter - Route requests to correct service
 * @class
 */
class ServiceRouter {
  constructor() {
    this.routes = new Map();
    this.middlewares = [];
  }

  /**
   * Register service route
   * @param {string} pattern - URL pattern (e.g., '/api/ai/*')
   * @param {string} service - Service name
   * @param {Function} handler - Handler function
   */
  registerRoute(pattern, service, handler) {
    this.routes.set(pattern, { service, handler });
  }

  /**
   * Match request to service
   * @param {string} path - Request path
   * @returns {Object|null} Route match
   */
  matchRoute(path) {
    for (const [pattern, route] of this.routes.entries()) {
      if (this.patternMatches(pattern, path)) {
        return route;
      }
    }
    return null;
  }

  /**
   * Check if pattern matches path
   * @private
   */
  patternMatches(pattern, path) {
    const regexPattern = pattern.replace(/\*/g, '.*').replace(/\//g, '\\/');
    const regex = new RegExp(`^${regexPattern}$`);
    return regex.test(path);
  }

  /**
   * Add middleware
   * @param {Function} middleware - Middleware function
   */
  use(middleware) {
    this.middlewares.push(middleware);
  }

  /**
   * Route request through middlewares
   * @param {Object} req - Request object
   * @param {Object} res - Response object
   * @returns {Promise} Routing result
   */
  async route(req, res) {
    const route = this.matchRoute(req.path);

    if (!route) {
      return res.status(404).json({ error: 'Route not found' });
    }

    for (const middleware of this.middlewares) {
      const result = await middleware(req, res);
      if (result === false) return;
    }

    return route.handler(req, res);
  }

  /**
   * Get registered routes count
   * @returns {number} Route count
   */
  getRouteCount() {
    return this.routes.size;
  }
}

/**
 * AuthenticationEnforcer - Verify JWT/API keys
 * @class
 */
class AuthenticationEnforcer {
  constructor() {
    this.tokens = new Map();
    this.apiKeys = new Map();
    this.tokenTTL = 3600000; // 1 hour
  }

  /**
   * Register API key
   * @param {string} key - API key
   * @param {Object} metadata - Key metadata
   */
  registerAPIKey(key, metadata = {}) {
    this.apiKeys.set(key, {
      key,
      active: true,
      createdAt: Date.now(),
      lastUsed: null,
      ...metadata,
    });
  }

  /**
   * Validate API key
   * @param {string} key - API key to validate
   * @returns {Object|null} Key data if valid
   */
  validateAPIKey(key) {
    const keyData = this.apiKeys.get(key);
    if (!keyData || !keyData.active) {
      return null;
    }

    keyData.lastUsed = Date.now();
    return keyData;
  }

  /**
   * Issue JWT token
   * @param {string} userId - User ID
   * @param {Object} payload - Token payload
   * @returns {Object} Token info
   */
  issueToken(userId, payload = {}) {
    const token = this.generateToken();
    const expiresAt = Date.now() + this.tokenTTL;

    this.tokens.set(token, {
      token,
      userId,
      payload,
      expiresAt,
      issuedAt: Date.now(),
      active: true,
    });

    return {
      token,
      expiresAt,
      expiresIn: this.tokenTTL / 1000,
    };
  }

  /**
   * Validate JWT token
   * @param {string} token - Token to validate
   * @returns {Object|null} Token data if valid
   */
  validateToken(token) {
    const tokenData = this.tokens.get(token);

    if (!tokenData || !tokenData.active) {
      return null;
    }

    if (Date.now() > tokenData.expiresAt) {
      tokenData.active = false;
      return null;
    }

    return tokenData;
  }

  /**
   * Revoke token
   * @param {string} token - Token to revoke
   * @returns {boolean} Success status
   */
  revokeToken(token) {
    const tokenData = this.tokens.get(token);
    if (tokenData) {
      tokenData.active = false;
      return true;
    }
    return false;
  }

  /**
   * Generate random token
   * @private
   */
  generateToken() {
    return crypto.randomBytes(32).toString('hex');
  }

  /**
   * Authenticate request
   * @param {Object} req - Request object
   * @returns {Object|null} Auth data
   */
  authenticate(req) {
    const apiKey = req.headers['x-api-key'];
    if (apiKey) {
      return { type: 'api-key', data: this.validateAPIKey(apiKey) };
    }

    const authHeader = req.headers.authorization || '';
    const match = authHeader.match(/^Bearer\s+(.+)$/);
    if (match) {
      const token = match[1];
      const tokenData = this.validateToken(token);
      if (tokenData) {
        return { type: 'jwt', data: tokenData };
      }
    }

    return null;
  }
}

/**
 * RateLimitEnforcer - Apply tier-based rate limits
 * @class
 */
class RateLimitEnforcer {
  constructor() {
    this.tiers = new Map();
    this.counters = new Map();
    this.setupDefaultTiers();
  }

  /**
   * Setup default rate limit tiers
   * @private
   */
  setupDefaultTiers() {
    this.registerTier('free', 100, 60000); // 100 requests per minute
    this.registerTier('pro', 1000, 60000); // 1000 requests per minute
    this.registerTier('enterprise', 10000, 60000); // 10000 requests per minute
  }

  /**
   * Register rate limit tier
   * @param {string} tier - Tier name
   * @param {number} limit - Request limit
   * @param {number} windowMs - Time window in milliseconds
   */
  registerTier(tier, limit, windowMs) {
    this.tiers.set(tier, { limit, windowMs });
  }

  /**
   * Check rate limit
   * @param {string} identifier - Unique identifier (IP, API key, etc.)
   * @param {string} tier - Tier name
   * @returns {Object} Rate limit status
   */
  checkLimit(identifier, tier = 'free') {
    const tierConfig = this.tiers.get(tier);
    if (!tierConfig) {
      return { allowed: false, error: `Unknown tier: ${tier}` };
    }

    const key = `${identifier}:${tier}`;
    const counter = this.counters.get(key) || { count: 0, resetAt: Date.now() + tierConfig.windowMs };

    // Check if window expired
    if (Date.now() > counter.resetAt) {
      counter.count = 0;
      counter.resetAt = Date.now() + tierConfig.windowMs;
    }

    const allowed = counter.count < tierConfig.limit;
    counter.count++;

    this.counters.set(key, counter);

    return {
      allowed,
      current: counter.count,
      limit: tierConfig.limit,
      resetAt: counter.resetAt,
      remaining: Math.max(0, tierConfig.limit - counter.count),
    };
  }

  /**
   * Reset counter
   * @param {string} identifier - Unique identifier
   * @param {string} tier - Tier name
   */
  resetCounter(identifier, tier) {
    const key = `${identifier}:${tier}`;
    this.counters.delete(key);
  }

  /**
   * Get stats
   * @returns {Object} Statistics
   */
  getStats() {
    return {
      tiersCount: this.tiers.size,
      activeCounters: this.counters.size,
      tiers: Array.from(this.tiers.entries()).map(([name, config]) => ({
        name,
        ...config,
      })),
    };
  }
}

/**
 * ResponseFormatter - Consistent response shape
 * @class
 */
class ResponseFormatter {
  /**
   * Format successful response
   * @param {*} data - Response data
   * @param {Object} options - Format options
   * @returns {Object} Formatted response
   */
  static success(data, options = {}) {
    return {
      success: true,
      data,
      meta: {
        timestamp: new Date().toISOString(),
        correlationId: options.correlationId,
        version: options.version || '1.0',
      },
      errors: [],
    };
  }

  /**
   * Format error response
   * @param {Error|string} error - Error object or message
   * @param {Object} options - Format options
   * @returns {Object} Formatted error response
   */
  static error(error, options = {}) {
    const message = typeof error === 'string' ? error : error.message;
    const code = options.code || 'INTERNAL_ERROR';

    return {
      success: false,
      data: null,
      meta: {
        timestamp: new Date().toISOString(),
        correlationId: options.correlationId,
        version: options.version || '1.0',
      },
      errors: [
        {
          code,
          message,
          details: options.details,
        },
      ],
    };
  }

  /**
   * Format paginated response
   * @param {Array} items - Data items
   * @param {Object} pagination - Pagination info
   * @param {Object} options - Format options
   * @returns {Object} Formatted response
   */
  static paginated(items, pagination, options = {}) {
    return {
      success: true,
      data: items,
      meta: {
        timestamp: new Date().toISOString(),
        correlationId: options.correlationId,
        version: options.version || '1.0',
        pagination: {
          page: pagination.page || 1,
          pageSize: pagination.pageSize || 20,
          total: pagination.total || 0,
          totalPages: Math.ceil((pagination.total || 0) / (pagination.pageSize || 20)),
        },
      },
      errors: [],
    };
  }
}

/**
 * ErrorResponseFormatter - Standardized error responses
 * @class
 */
class ErrorResponseFormatter {
  static httpStatusCodes = {
    VALIDATION_ERROR: 400,
    UNAUTHORIZED: 401,
    FORBIDDEN: 403,
    NOT_FOUND: 404,
    RATE_LIMIT_EXCEEDED: 429,
    INTERNAL_ERROR: 500,
    SERVICE_UNAVAILABLE: 503,
  };

  /**
   * Format error response with HTTP status
   * @param {string} code - Error code
   * @param {string} message - Error message
   * @param {Object} details - Error details
   * @returns {Object} Error response with status
   */
  static formatError(code, message, details = {}) {
    const status = this.httpStatusCodes[code] || 500;

    return {
      status,
      response: ResponseFormatter.error(message, { code, details }),
    };
  }

  /**
   * Get HTTP status code for error
   * @param {string} code - Error code
   * @returns {number} HTTP status code
   */
  static getStatusCode(code) {
    return this.httpStatusCodes[code] || 500;
  }
}

/**
 * CachingHeaders - Set Cache-Control headers
 * @class
 */
class CachingHeaders {
  static cacheStrategies = {
    no_cache: 'no-cache, no-store, must-revalidate',
    short: 'public, max-age=60',
    medium: 'public, max-age=300',
    long: 'public, max-age=3600',
    private_short: 'private, max-age=60',
    private_medium: 'private, max-age=300',
    private_long: 'private, max-age=3600',
  };

  /**
   * Get cache headers
   * @param {string} strategy - Caching strategy
   * @returns {Object} Cache headers
   */
  static getHeaders(strategy = 'medium') {
    const cacheControl = this.cacheStrategies[strategy] || this.cacheStrategies.medium;

    return {
      'Cache-Control': cacheControl,
      'Pragma': 'cache',
      'ETag': `"${crypto.randomBytes(16).toString('hex')}"`,
    };
  }

  /**
   * Set cache headers on response
   * @param {Object} res - Response object
   * @param {string} strategy - Caching strategy
   */
  static apply(res, strategy = 'medium') {
    const headers = this.getHeaders(strategy);
    for (const [key, value] of Object.entries(headers)) {
      res.setHeader(key, value);
    }
  }
}

/**
 * APIGateway - Main gateway
 * @class
 */
class APIGateway {
  constructor() {
    this.router = new ServiceRouter();
    this.auth = new AuthenticationEnforcer();
    this.rateLimiter = new RateLimitEnforcer();
    this.setupDefaultRoutes();
  }

  /**
   * Setup default routes
   * @private
   */
  setupDefaultRoutes() {
    this.router.use(this.createAuthMiddleware());
    this.router.use(this.createRateLimitMiddleware());
  }

  /**
   * Create authentication middleware
   * @private
   */
  createAuthMiddleware() {
    return (req, res) => {
      const auth = this.auth.authenticate(req);
      if (!auth && !this.isPublicRoute(req.path)) {
        res.status(401).json(ResponseFormatter.error('Unauthorized'));
        return false;
      }
      req.auth = auth;
      return true;
    };
  }

  /**
   * Create rate limit middleware
   * @private
   */
  createRateLimitMiddleware() {
    return (req, res) => {
      const identifier = req.auth?.data?.userId || req.ip;
      const tier = req.auth?.data?.tier || 'free';
      const limit = this.rateLimiter.checkLimit(identifier, tier);

      res.setHeader('X-RateLimit-Limit', limit.limit);
      res.setHeader('X-RateLimit-Remaining', limit.remaining);
      res.setHeader('X-RateLimit-Reset', limit.resetAt);

      if (!limit.allowed) {
        res.status(429).json(ResponseFormatter.error('Rate limit exceeded'));
        return false;
      }

      return true;
    };
  }

  /**
   * Check if route is public
   * @private
   */
  isPublicRoute(path) {
    const publicRoutes = ['/api/auth/login', '/api/health', '/api/version'];
    return publicRoutes.some(route => path.startsWith(route));
  }

  /**
   * Register route
   * @param {string} method - HTTP method
   * @param {string} path - Request path
   * @param {string} service - Service name
   * @param {Function} handler - Handler function
   */
  registerRoute(method, path, service, handler) {
    const pattern = `/api/${method.toLowerCase()}${path}`;
    this.router.registerRoute(pattern, service, handler);
  }

  /**
   * Get gateway stats
   * @returns {Object} Statistics
   */
  getStats() {
    return {
      routes: this.router.getRouteCount(),
      rateLimiter: this.rateLimiter.getStats(),
      tokens: this.auth.tokens.size,
      apiKeys: this.auth.apiKeys.size,
    };
  }
}

module.exports = {
  ServiceRouter,
  AuthenticationEnforcer,
  RateLimitEnforcer,
  ResponseFormatter,
  ErrorResponseFormatter,
  CachingHeaders,
  APIGateway,
};
