/**
 * HELIOS v4.0 Multi-Tenancy Module - Public API
 * Complete multi-tenancy solution with isolation, partitioning, routing, and infrastructure sharing
 * 
 * @module feat-tenancy
 * @version 1.0.0
 * 
 * @description
 * This module provides comprehensive multi-tenancy capabilities:
 * - Tenant Manager (lifecycle, metadata, configuration management)
 * - Data Partitioner (row-level, schema-level, database-level isolation)
 * - Tenant Router (request routing to correct tenant context)
 * - Isolation Manager (cross-tenant access prevention, security logging)
 * 
 * Performance Characteristics:
 * - Tenant lookup: <1ms (cached)
 * - Tenant creation: <10ms
 * - Data routing: <2ms
 * - Isolation checks: <1ms (cached)
 * 
 * Isolation Strategies:
 * - Row-level: Single database, tenant_id column filters
 * - Schema-level: Separate schemas per tenant in one database
 * - Database-level: Dedicated database instance per tenant
 * 
 * @example
 * const { TenantManager, DataPartitioner, TenantRouter } = require('@helios/feat-tenancy');
 * 
 * // Initialize tenant system
 * const manager = new TenantManager();
 * const router = new TenantRouter();
 * const partitioner = new DataPartitioner({ strategy: 'row' });
 * 
 * // Create tenant
 * const tenant = manager.createTenant('acme-corp', {
 *   name: 'ACME Corporation',
 *   email: 'admin@acme.com',
 *   plan: 'enterprise'
 * });
 * 
 * // Route request
 * const tenantId = router.extractTenantId(req);
 * 
 * // Build isolated query
 * const query = partitioner.buildTenantQuery(tenantId, 'articles');
 * 
 * @requires feat-tenancy/implementation
 */

const {
  TenantManager,
  DataPartitioner,
  TenantRouter,
  IsolationManager
} = require('./implementation');

/**
 * TenancyFactory - Create preconfigured tenancy systems
 * @class
 */
class TenancyFactory {
  /**
   * Create complete multi-tenancy system
   * @static
   * @param {string} strategy - Data isolation strategy (row, schema, database)
   * @param {Object} [config] - Configuration options
   * @returns {Object} Complete system with manager, router, partitioner, isolation
   * @example
   * const tenancy = TenancyFactory.createSystem('schema', {
   *   databases: { default: 'postgresql://...' }
   * });
   */
  static createSystem(strategy, config = {}) {
    if (!['row', 'schema', 'database'].includes(strategy)) {
      throw new Error('TenancyFactory: Invalid strategy');
    }

    const manager = new TenantManager(config.manager);
    const partitioner = new DataPartitioner({
      strategy,
      databases: config.databases,
      ...config.partitioner
    });
    const router = new TenantRouter(config.router);
    const isolation = new IsolationManager(config.isolation);

    return {
      manager,
      router,
      partitioner,
      isolation,
      strategy
    };
  }

  /**
   * Create row-level isolation system (single database)
   * @static
   * @param {Object} [config] - Configuration
   * @returns {Object} Tenancy system with row-level isolation
   * @example
   * const tenancy = TenancyFactory.createRowLevelSystem();
   */
  static createRowLevelSystem(config = {}) {
    return this.createSystem('row', config);
  }

  /**
   * Create schema-level isolation system
   * @static
   * @param {Object} [config] - Configuration
   * @returns {Object} Tenancy system with schema-level isolation
   * @example
   * const tenancy = TenancyFactory.createSchemaLevelSystem({
   *   databases: { default: 'postgresql://db.example.com' }
   * });
   */
  static createSchemaLevelSystem(config = {}) {
    return this.createSystem('schema', config);
  }

  /**
   * Create database-level isolation system
   * @static
   * @param {Object} [config] - Configuration
   * @returns {Object} Tenancy system with database-level isolation
   * @example
   * const tenancy = TenancyFactory.createDatabaseLevelSystem({
   *   databases: {
   *     default: 'postgresql://db.example.com',
   *     tenant001: 'postgresql://tenant1.example.com'
   *   }
   * });
   */
  static createDatabaseLevelSystem(config = {}) {
    return this.createSystem('database', config);
  }
}

/**
 * Create tenant extraction middleware
 * @param {TenantRouter} router - Tenant router instance
 * @param {TenantManager} manager - Tenant manager instance
 * @returns {Function} Express middleware
 * @example
 * const middleware = createTenantMiddleware(router, manager);
 * app.use(middleware);
 */
function createTenantMiddleware(router, manager) {
  return (req, res, next) => {
    const tenantId = router.extractTenantId(req);
    
    if (!tenantId) {
      return res.status(400).json({ error: 'Tenant ID required' });
    }

    if (!manager.tenantExists(tenantId)) {
      return res.status(404).json({ error: 'Tenant not found' });
    }

    req.tenantId = tenantId;
    req.tenant = manager.getTenant(tenantId);
    
    next();
  };
}

/**
 * Create isolation enforcement middleware
 * @param {IsolationManager} isolation - Isolation manager instance
 * @returns {Function} Express middleware
 * @example
 * const middleware = createIsolationMiddleware(isolation);
 * app.use(middleware);
 */
function createIsolationMiddleware(isolation) {
  return (req, res, next) => {
    if (!req.tenantId) {
      return res.status(400).json({ error: 'Tenant context required' });
    }

    if (isolation.isBlacklisted(req.tenantId)) {
      return res.status(403).json({ error: 'Tenant access denied' });
    }

    next();
  };
}

/**
 * Create data query isolation wrapper
 * @param {DataPartitioner} partitioner - Data partitioner instance
 * @returns {Function} Query builder function
 * @example
 * const queryBuilder = createQueryIsolation(partitioner);
 * const isolatedQuery = queryBuilder(tenantId, 'articles', { status: 'published' });
 */
function createQueryIsolation(partitioner) {
  return (tenantId, tableName, conditions = {}) => {
    return partitioner.buildTenantQuery(tenantId, tableName, conditions);
  };
}

/**
 * Create tenant context middleware
 * @param {TenantRouter} router - Tenant router instance
 * @returns {Function} Express middleware
 * @example
 * const middleware = createContextMiddleware(router);
 * app.use(middleware);
 */
function createContextMiddleware(router) {
  return (req, res, next) => {
    if (req.tenantId) {
      const context = router.getContext(req.tenantId);
      req.tenantContext = context || {};
    }
    next();
  };
}

/**
 * Export public API
 */
module.exports = {
  // Core classes
  TenantManager,
  DataPartitioner,
  TenantRouter,
  IsolationManager,
  
  // Factory methods
  TenancyFactory,
  
  // Middleware utilities
  createTenantMiddleware,
  createIsolationMiddleware,
  createQueryIsolation,
  createContextMiddleware,
  
  // Version info
  version: '1.0.0',
  name: '@helios/feat-tenancy'
};
