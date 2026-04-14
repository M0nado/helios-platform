/**
 * HELIOS v4.0 Multi-Tenancy Implementation Module
 * Provides tenant isolation, data partitioning, routing, and shared infrastructure
 * @module feat-tenancy/implementation
 * @version 1.0.0
 */

/**
 * TenantManager - Manages tenant lifecycle, metadata, and configuration
 * Performance: Tenant lookup <1ms (cached), creation <10ms
 */
class TenantManager {
  /**
   * Initialize Tenant Manager
   * @param {Object} [config] - Configuration object
   * @param {Map<string, Object>} [config.tenants] - Existing tenants
   * @param {Object} [config.defaults] - Default tenant configuration
   */
  constructor(config = {}) {
    this.tenants = new Map(config.tenants || []);
    this.defaults = config.defaults || {};
    this.tenantCache = new Map();
    this.subscriptions = new Map();
  }

  /**
   * Create new tenant
   * @param {string} tenantId - Unique tenant identifier
   * @param {Object} metadata - Tenant metadata
   * @param {string} metadata.name - Tenant display name
   * @param {string} metadata.email - Tenant contact email
   * @param {string} [metadata.plan] - Subscription plan (free, pro, enterprise)
   * @param {Object} [metadata.config] - Custom configuration
   * @throws {Error} If tenant already exists or parameters invalid
   * @returns {Object} Created tenant object
   * @example
   * const tenant = manager.createTenant('tenant-001', {
   *   name: 'Acme Corporation',
   *   email: 'admin@acme.com',
   *   plan: 'enterprise'
   * });
   */
  createTenant(tenantId, metadata = {}) {
    if (!tenantId || typeof tenantId !== 'string') {
      throw new Error('TenantManager: Tenant ID must be non-empty string');
    }
    if (this.tenants.has(tenantId)) {
      throw new Error(`TenantManager: Tenant '${tenantId}' already exists`);
    }
    if (!metadata.name) {
      throw new Error('TenantManager: Tenant name required');
    }

    const tenant = {
      id: tenantId,
      name: metadata.name,
      email: metadata.email || '',
      plan: metadata.plan || 'free',
      config: { ...this.defaults, ...metadata.config },
      status: 'active',
      createdAt: new Date(),
      updatedAt: new Date(),
      dataPartitions: [],
      users: new Set(),
      features: new Map()
    };

    this.tenants.set(tenantId, tenant);
    this.tenantCache.set(tenantId, tenant);
    
    return {
      id: tenant.id,
      name: tenant.name,
      plan: tenant.plan,
      createdAt: tenant.createdAt
    };
  }

  /**
   * Get tenant by ID
   * @param {string} tenantId - Tenant identifier
   * @returns {Object|null} Tenant object or null if not found
   * @example
   * const tenant = manager.getTenant('tenant-001');
   */
  getTenant(tenantId) {
    if (this.tenantCache.has(tenantId)) {
      return this.tenantCache.get(tenantId);
    }
    
    const tenant = this.tenants.get(tenantId);
    if (tenant) {
      this.tenantCache.set(tenantId, tenant);
    }
    return tenant || null;
  }

  /**
   * Update tenant metadata
   * @param {string} tenantId - Tenant identifier
   * @param {Object} updates - Fields to update
   * @returns {Object} Updated tenant
   * @throws {Error} If tenant not found
   * @example
   * manager.updateTenant('tenant-001', { plan: 'pro' });
   */
  updateTenant(tenantId, updates = {}) {
    const tenant = this.getTenant(tenantId);
    if (!tenant) {
      throw new Error(`TenantManager: Tenant '${tenantId}' not found`);
    }

    Object.assign(tenant, updates, { updatedAt: new Date() });
    this.tenantCache.set(tenantId, tenant);
    
    return tenant;
  }

  /**
   * Delete tenant
   * @param {string} tenantId - Tenant identifier
   * @returns {boolean} True if deleted
   * @example
   * manager.deleteTenant('tenant-001');
   */
  deleteTenant(tenantId) {
    const deleted = this.tenants.delete(tenantId);
    this.tenantCache.delete(tenantId);
    return deleted;
  }

  /**
   * List all tenants
   * @param {Object} [filter] - Filter options
   * @param {string} [filter.plan] - Filter by plan
   * @param {string} [filter.status] - Filter by status
   * @returns {Object[]} Array of tenant objects
   * @example
   * const enterprises = manager.listTenants({ plan: 'enterprise' });
   */
  listTenants(filter = {}) {
    let tenants = Array.from(this.tenants.values());
    
    if (filter.plan) {
      tenants = tenants.filter(t => t.plan === filter.plan);
    }
    if (filter.status) {
      tenants = tenants.filter(t => t.status === filter.status);
    }
    
    return tenants.map(t => ({
      id: t.id,
      name: t.name,
      plan: t.plan,
      status: t.status,
      createdAt: t.createdAt
    }));
  }

  /**
   * Check if tenant exists
   * @param {string} tenantId - Tenant identifier
   * @returns {boolean} True if tenant exists
   */
  tenantExists(tenantId) {
    return this.tenants.has(tenantId);
  }

  /**
   * Get tenant count
   * @returns {number} Total number of tenants
   */
  getTenantCount() {
    return this.tenants.size;
  }

  /**
   * Add user to tenant
   * @param {string} tenantId - Tenant identifier
   * @param {string} userId - User identifier
   * @throws {Error} If tenant not found
   */
  addUserToTenant(tenantId, userId) {
    const tenant = this.getTenant(tenantId);
    if (!tenant) throw new Error(`TenantManager: Tenant '${tenantId}' not found`);
    
    tenant.users.add(userId);
  }

  /**
   * Remove user from tenant
   * @param {string} tenantId - Tenant identifier
   * @param {string} userId - User identifier
   * @returns {boolean} True if removed
   */
  removeUserFromTenant(tenantId, userId) {
    const tenant = this.getTenant(tenantId);
    if (!tenant) return false;
    
    return tenant.users.delete(userId);
  }

  /**
   * Get tenant users
   * @param {string} tenantId - Tenant identifier
   * @returns {string[]} Array of user IDs
   */
  getTenantUsers(tenantId) {
    const tenant = this.getTenant(tenantId);
    return tenant ? Array.from(tenant.users) : [];
  }

  /**
   * Enable feature for tenant
   * @param {string} tenantId - Tenant identifier
   * @param {string} featureName - Feature name
   * @param {Object} [config] - Feature configuration
   */
  enableFeature(tenantId, featureName, config = {}) {
    const tenant = this.getTenant(tenantId);
    if (!tenant) throw new Error(`TenantManager: Tenant '${tenantId}' not found`);
    
    tenant.features.set(featureName, { enabled: true, config, enabledAt: new Date() });
  }

  /**
   * Check if feature enabled for tenant
   * @param {string} tenantId - Tenant identifier
   * @param {string} featureName - Feature name
   * @returns {boolean} Feature enabled status
   */
  isFeatureEnabled(tenantId, featureName) {
    const tenant = this.getTenant(tenantId);
    if (!tenant) return false;
    
    const feature = tenant.features.get(featureName);
    return feature ? feature.enabled : false;
  }
}

/**
 * DataPartitioner - Handles tenant data isolation and partitioning strategies
 * Performance: Partition lookup <1ms, data routing <2ms
 */
class DataPartitioner {
  /**
   * Initialize Data Partitioner
   * @param {Object} config - Partitioning configuration
   * @param {string} config.strategy - Partitioning strategy (row, schema, database)
   * @param {Object} [config.databases] - Database configurations
   * @param {Map<string, string>} [config.partitionMap] - Existing tenant-to-partition mappings
   */
  constructor(config = {}) {
    const validStrategies = ['row', 'schema', 'database'];
    if (!validStrategies.includes(config.strategy)) {
      throw new Error(`DataPartitioner: Invalid strategy. Must be one of ${validStrategies.join(', ')}`);
    }
    
    this.strategy = config.strategy;
    this.databases = config.databases || {};
    this.partitionMap = new Map(config.partitionMap || []);
    this.schemaMap = new Map();
    this.tenantTables = new Map();
  }

  /**
   * Register tenant partition
   * @param {string} tenantId - Tenant identifier
   * @param {string} partitionKey - Partition identifier
   * @throws {Error} If partition key invalid
   * @example
   * partitioner.registerPartition('tenant-001', 'acme_db');
   */
  registerPartition(tenantId, partitionKey) {
    if (!tenantId || !partitionKey) {
      throw new Error('DataPartitioner: Tenant ID and partition key required');
    }
    
    this.partitionMap.set(tenantId, partitionKey);
  }

  /**
   * Get partition for tenant
   * @param {string} tenantId - Tenant identifier
   * @returns {string|null} Partition key or null
   * @example
   * const partition = partitioner.getPartition('tenant-001');
   */
  getPartition(tenantId) {
    return this.partitionMap.get(tenantId) || null;
  }

  /**
   * Create schema for tenant (schema isolation strategy)
   * @param {string} tenantId - Tenant identifier
   * @param {string} [schemaName] - Custom schema name
   * @returns {string} Schema name
   * @example
   * const schema = partitioner.createSchema('tenant-001');
   */
  createSchema(tenantId, schemaName = null) {
    if (this.strategy !== 'schema') {
      throw new Error('DataPartitioner: Schema creation only for schema strategy');
    }
    
    const schema = schemaName || `tenant_${tenantId}`;
    this.schemaMap.set(tenantId, schema);
    this.registerPartition(tenantId, schema);
    
    return schema;
  }

  /**
   * Get schema for tenant
   * @param {string} tenantId - Tenant identifier
   * @returns {string|null} Schema name or null
   */
  getSchema(tenantId) {
    return this.schemaMap.get(tenantId) || null;
  }

  /**
   * Add tenant table for row-level isolation
   * @param {string} tenantId - Tenant identifier
   * @param {string} tableName - Table name
   * @param {string} tenantColumn - Column storing tenant ID
   * @example
   * partitioner.registerTenantTable('tenant-001', 'articles', 'tenant_id');
   */
  registerTenantTable(tenantId, tableName, tenantColumn = 'tenant_id') {
    if (!this.tenantTables.has(tableName)) {
      this.tenantTables.set(tableName, new Map());
    }
    
    this.tenantTables.get(tableName).set(tenantId, tenantColumn);
  }

  /**
   * Build filtered query for tenant data
   * @param {string} tenantId - Tenant identifier
   * @param {string} tableName - Table name
   * @param {Object} [conditions] - Additional WHERE conditions
   * @returns {Object} Query object {table, where}
   * @example
   * const query = partitioner.buildTenantQuery('tenant-001', 'articles');
   * // { table: 'articles', where: { tenant_id: 'tenant-001' } }
   */
  buildTenantQuery(tenantId, tableName, conditions = {}) {
    if (this.strategy === 'schema') {
      const schema = this.getSchema(tenantId);
      return {
        table: `${schema}.${tableName}`,
        where: conditions
      };
    }
    
    if (this.strategy === 'database') {
      const partition = this.getPartition(tenantId);
      return {
        database: partition,
        table: tableName,
        where: conditions
      };
    }
    
    // Row-level isolation
    return {
      table: tableName,
      where: { ...conditions, tenant_id: tenantId }
    };
  }

  /**
   * Get all partitions
   * @returns {Object} Map of tenant IDs to partitions
   */
  getAllPartitions() {
    const result = {};
    for (const [tenantId, partition] of this.partitionMap) {
      result[tenantId] = partition;
    }
    return result;
  }

  /**
   * Get partitioning statistics
   * @returns {Object} Statistics about partitioning
   */
  getStatistics() {
    return {
      strategy: this.strategy,
      totalPartitions: this.partitionMap.size,
      totalSchemas: this.schemaMap.size,
      totalTables: this.tenantTables.size
    };
  }
}

/**
 * TenantRouter - Routes requests to correct tenant context
 * Performance: Route determination <2ms
 */
class TenantRouter {
  /**
   * Initialize Tenant Router
   * @param {Object} [config] - Router configuration
   */
  constructor(config = {}) {
    this.routingStrategies = new Map();
    this.tenantContext = new Map();
    this.routeCache = new Map();
    this.extractors = {
      header: (req) => req.headers['x-tenant-id'],
      subdomain: (req) => req.subdomains ? req.subdomains[0] : null,
      path: (req) => this.extractPathTenant(req.path),
      query: (req) => req.query ? req.query.tenantId : null,
      hostname: (req) => this.extractHostnameTenant(req.hostname)
    };
  }

  /**
   * Register routing strategy
   * @param {string} name - Strategy name
   * @param {Function} extractor - Function to extract tenant ID from request
   * @example
   * router.registerStrategy('custom', (req) => req.user.tenantId);
   */
  registerStrategy(name, extractor) {
    if (typeof extractor !== 'function') {
      throw new Error('TenantRouter: Extractor must be function');
    }
    this.routingStrategies.set(name, extractor);
  }

  /**
   * Extract tenant ID from request
   * @param {Object} req - Request object
   * @param {string[]} [strategies=['header','subdomain','path']] - Strategies to try
   * @returns {string|null} Tenant ID or null
   * @example
   * const tenantId = router.extractTenantId(req);
   */
  extractTenantId(req, strategies = ['header', 'subdomain', 'path']) {
    const cacheKey = `${req.path}:${req.headers.host}`;
    if (this.routeCache.has(cacheKey)) {
      return this.routeCache.get(cacheKey);
    }

    for (const strategy of strategies) {
      const extractor = this.extractors[strategy] || this.routingStrategies.get(strategy);
      if (!extractor) continue;
      
      try {
        const tenantId = extractor(req);
        if (tenantId) {
          this.routeCache.set(cacheKey, tenantId);
          return tenantId;
        }
      } catch (error) {
        // Strategy failed, try next
      }
    }
    
    return null;
  }

  /**
   * Extract tenant ID from path
   * @param {string} path - URL path
   * @returns {string|null} Tenant ID or null
   * @private
   */
  extractPathTenant(path) {
    const match = path.match(/^\/tenants\/([^\/]+)/);
    return match ? match[1] : null;
  }

  /**
   * Extract tenant ID from hostname
   * @param {string} hostname - Request hostname
   * @returns {string|null} Tenant ID or null
   * @private
   */
  extractHostnameTenant(hostname) {
    const parts = hostname.split('.');
    if (parts.length > 2) {
      return parts[0];
    }
    return null;
  }

  /**
   * Set tenant context for request
   * @param {string} tenantId - Tenant identifier
   * @param {Object} context - Context data
   * @example
   * router.setContext('tenant-001', { database: 'acme_db', schema: 'tenant_001' });
   */
  setContext(tenantId, context) {
    this.tenantContext.set(tenantId, { ...context, setAt: new Date() });
  }

  /**
   * Get tenant context
   * @param {string} tenantId - Tenant identifier
   * @returns {Object|null} Context object or null
   */
  getContext(tenantId) {
    return this.tenantContext.get(tenantId) || null;
  }

  /**
   * Clear context for tenant
   * @param {string} tenantId - Tenant identifier
   */
  clearContext(tenantId) {
    this.tenantContext.delete(tenantId);
    this.routeCache.clear();
  }

  /**
   * Create routing middleware
   * @param {Function} tenantValidator - Function to validate tenant exists
   * @returns {Function} Express middleware
   * @example
   * app.use(router.middleware((tenantId) => manager.tenantExists(tenantId)));
   */
  middleware(tenantValidator) {
    const self = this;
    
    return (req, res, next) => {
      const tenantId = self.extractTenantId(req);
      
      if (!tenantId) {
        return res.status(400).json({ error: 'Tenant ID not found' });
      }
      
      if (tenantValidator && !tenantValidator(tenantId)) {
        return res.status(403).json({ error: 'Invalid tenant' });
      }
      
      req.tenantId = tenantId;
      req.tenantContext = self.getContext(tenantId);
      
      next();
    };
  }
}

/**
 * IsolationManager - Enforces tenant isolation and prevents data leakage
 * Performance: Isolation checks <1ms
 */
class IsolationManager {
  /**
   * Initialize Isolation Manager
   * @param {Object} [config] - Configuration
   * @param {boolean} [config.strictMode=true] - Strict isolation enforcement
   */
  constructor(config = {}) {
    this.strictMode = config.strictMode !== false;
    this.tenantBlacklist = new Set();
    this.accessLog = [];
    this.violations = [];
  }

  /**
   * Validate tenant access to resource
   * @param {string} tenantId - Tenant identifier
   * @param {string} resourceId - Resource identifier
   * @param {string} [resourceTenant] - Tenant that owns resource
   * @returns {boolean} True if access allowed
   * @throws {Error} If strict mode and access denied
   * @example
   * isolationManager.validateAccess('tenant-001', 'article-123', 'tenant-001');
   */
  validateAccess(tenantId, resourceId, resourceTenant) {
    this.logAccess(tenantId, resourceId);
    
    if (!tenantId || !resourceId) {
      throw new Error('IsolationManager: Tenant ID and resource ID required');
    }

    if (this.tenantBlacklist.has(tenantId)) {
      this.recordViolation(tenantId, 'BLACKLISTED_TENANT', resourceId);
      if (this.strictMode) throw new Error('Tenant is blacklisted');
      return false;
    }

    if (resourceTenant && tenantId !== resourceTenant) {
      this.recordViolation(tenantId, 'CROSS_TENANT_ACCESS', resourceId);
      if (this.strictMode) throw new Error('Cross-tenant access denied');
      return false;
    }

    return true;
  }

  /**
   * Validate multi-tenant query
   * @param {string} tenantId - Tenant identifier
   * @param {Object} query - Query to validate
   * @param {string} [query.tenant_id] - Expected tenant ID in query
   * @returns {boolean} True if query is safe
   * @example
   * isolationManager.validateQuery('tenant-001', { tenant_id: 'tenant-001' });
   */
  validateQuery(tenantId, query) {
    if (!query) return true;
    
    // Check that tenant_id in query matches request tenant
    if (query.tenant_id && query.tenant_id !== tenantId) {
      this.recordViolation(tenantId, 'INVALID_QUERY', 'tenant mismatch');
      return false;
    }

    // Check for SQL injection patterns in string fields
    for (const [key, value] of Object.entries(query)) {
      if (typeof value === 'string') {
        if (this.isSuspiciousPattern(value)) {
          this.recordViolation(tenantId, 'SUSPICIOUS_QUERY', key);
          return false;
        }
      }
    }

    return true;
  }

  /**
   * Check for suspicious query patterns
   * @param {string} value - Value to check
   * @returns {boolean} True if suspicious
   * @private
   */
  isSuspiciousPattern(value) {
    const suspiciousPatterns = [
      /(\bUNION\b.*\bSELECT\b)/i,
      /(\bDROP\b|\bDELETE\b|\bTRUNCATE\b)/i,
      /(\bOR\b\s+['"]?\d+['"]?\s*=\s*['"]?\d+['"]?)/i,
      /(-{2}|\/\*)/
    ];
    
    return suspiciousPatterns.some(pattern => pattern.test(value));
  }

  /**
   * Blacklist tenant (emergency)
   * @param {string} tenantId - Tenant identifier
   * @param {string} [reason] - Blacklist reason
   * @example
   * isolationManager.blacklistTenant('tenant-001', 'Security violation');
   */
  blacklistTenant(tenantId, reason = '') {
    this.tenantBlacklist.add(tenantId);
    this.recordViolation(tenantId, 'BLACKLISTED', reason);
  }

  /**
   * Whitelist tenant
   * @param {string} tenantId - Tenant identifier
   */
  whitelistTenant(tenantId) {
    this.tenantBlacklist.delete(tenantId);
  }

  /**
   * Check if tenant is blacklisted
   * @param {string} tenantId - Tenant identifier
   * @returns {boolean} True if blacklisted
   */
  isBlacklisted(tenantId) {
    return this.tenantBlacklist.has(tenantId);
  }

  /**
   * Log access attempt
   * @param {string} tenantId - Tenant identifier
   * @param {string} resourceId - Resource identifier
   * @private
   */
  logAccess(tenantId, resourceId) {
    this.accessLog.push({
      tenantId,
      resourceId,
      timestamp: new Date(),
      type: 'access'
    });
    
    // Keep last 10000 logs
    if (this.accessLog.length > 10000) {
      this.accessLog = this.accessLog.slice(-10000);
    }
  }

  /**
   * Record security violation
   * @param {string} tenantId - Tenant identifier
   * @param {string} type - Violation type
   * @param {string} [details] - Additional details
   * @private
   */
  recordViolation(tenantId, type, details = '') {
    this.violations.push({
      tenantId,
      type,
      details,
      timestamp: new Date()
    });
    
    // Keep last 1000 violations
    if (this.violations.length > 1000) {
      this.violations = this.violations.slice(-1000);
    }
  }

  /**
   * Get access log for tenant
   * @param {string} tenantId - Tenant identifier
   * @returns {Object[]} Access log entries
   */
  getAccessLog(tenantId) {
    return this.accessLog.filter(entry => entry.tenantId === tenantId);
  }

  /**
   * Get violations for tenant
   * @param {string} tenantId - Tenant identifier
   * @returns {Object[]} Violation entries
   */
  getViolations(tenantId) {
    return this.violations.filter(entry => entry.tenantId === tenantId);
  }

  /**
   * Get security report
   * @returns {Object} Security report
   */
  getSecurityReport() {
    return {
      blacklistedTenants: Array.from(this.tenantBlacklist),
      totalAccessLogs: this.accessLog.length,
      totalViolations: this.violations.length,
      recentViolations: this.violations.slice(-10)
    };
  }
}

module.exports = {
  TenantManager,
  DataPartitioner,
  TenantRouter,
  IsolationManager
};
