# HELIOS v4.0 feat-tenancy Module

## Overview

`feat-tenancy` provides enterprise-grade multi-tenancy for HELIOS v4.0 applications. It implements tenant isolation, data partitioning, request routing, and shared infrastructure management.

**Module Size:** ~70 KB | **Test Coverage:** 48 tests

### Key Features

- **Tenant Manager** - Lifecycle management, configuration, feature enablement
- **Data Partitioner** - Row-level, schema-level, and database-level isolation
- **Tenant Router** - Smart request routing with multiple extraction strategies
- **Isolation Manager** - Cross-tenant access prevention, security logging, violation detection

### Isolation Strategies

| Strategy | Isolation Level | Scalability | Cost | Complexity |
|----------|-----------------|-------------|------|-----------|
| Row-level | Shared database + tenant_id column | High | Low | Low |
| Schema-level | Separate schemas in one database | High | Medium | Medium |
| Database-level | Dedicated database per tenant | Medium | High | High |

### Performance Characteristics

| Operation | Latency | Notes |
|-----------|---------|-------|
| Tenant lookup | <1ms | Cached |
| Tenant creation | <10ms | Metadata initialization |
| Data routing | <2ms | Query building |
| Isolation checks | <1ms | Cached validation |
| Request routing | <2ms | Cached context |

## Installation

```bash
npm install @helios/feat-tenancy
```

## Quick Start

### Row-Level Isolation (Recommended for SaaS)

```javascript
const { TenancyFactory } = require('@helios/feat-tenancy');

// Create system with row-level isolation
const tenancy = TenancyFactory.createRowLevelSystem({
  databases: { default: 'postgresql://localhost/app' }
});

// Create tenant
const tenant = tenancy.manager.createTenant('acme-corp', {
  name: 'ACME Corporation',
  email: 'admin@acme.com',
  plan: 'enterprise'
});

// Build isolated queries
const query = tenancy.partitioner.buildTenantQuery(
  'acme-corp',
  'articles',
  { status: 'published' }
);
// Result: { table: 'articles', where: { status: 'published', tenant_id: 'acme-corp' } }
```

### Schema-Level Isolation

```javascript
const tenancy = TenancyFactory.createSchemaLevelSystem({
  databases: { default: 'postgresql://localhost/app' }
});

// Create tenant schema
const schema = tenancy.partitioner.createSchema('acme-corp');
// Creates schema 'tenant_acme-corp' in database

const query = tenancy.partitioner.buildTenantQuery('acme-corp', 'articles');
// Result: { table: 'tenant_acme-corp.articles', where: {} }
```

### Tenant Request Routing

```javascript
const { TenantRouter } = require('@helios/feat-tenancy');

const router = new TenantRouter();

// Extract tenant from request (header, subdomain, path, query, or custom)
const tenantId = router.extractTenantId(req);
// Tries: x-tenant-id header → subdomain → /tenants/{id} path → tenantId query

// Set tenant context
router.setContext('acme-corp', {
  database: 'acme_db',
  schema: 'acme_schema',
  apiLimit: 10000
});

// Get context in subsequent operations
const context = router.getContext('acme-corp');
```

### Express Middleware Integration

```javascript
const express = require('express');
const {
  createTenantMiddleware,
  createIsolationMiddleware,
  createQueryIsolation
} = require('@helios/feat-tenancy');

const app = express();
const tenancy = TenancyFactory.createRowLevelSystem();

// Apply middleware stack
app.use(createTenantMiddleware(tenancy.router, tenancy.manager));
app.use(createIsolationMiddleware(tenancy.isolation));

// Use in routes
const buildQuery = createQueryIsolation(tenancy.partitioner);

app.get('/api/articles', (req, res) => {
  // req.tenantId = extracted tenant
  // req.tenant = tenant object
  const query = buildQuery(req.tenantId, 'articles', { status: 'published' });
  
  // Execute query with isolation
  db.query(query).then(articles => res.json(articles));
});
```

### Security & Isolation

```javascript
const { IsolationManager } = require('@helios/feat-tenancy');

const isolation = new IsolationManager({ strictMode: true });

// Validate access
try {
  isolation.validateAccess('tenant-001', 'article-123', 'tenant-001');
  // Access granted
} catch (error) {
  // Cross-tenant access blocked
}

// Validate queries for SQL injection
const isSafe = isolation.validateQuery('tenant-001', {
  search: 'safe-input',
  tenant_id: 'tenant-001'
});

// Emergency tenant blacklist
isolation.blacklistTenant('malicious-tenant', 'Suspicious activity');

// Security audit
const report = isolation.getSecurityReport();
```

## API Reference

### TenantManager

#### Constructor
```javascript
new TenantManager({
  defaults: Object,      // Default tenant configuration
  tenants: Map          // Existing tenants
})
```

#### Methods

- **createTenant(tenantId, metadata)** → Object  
  Create new tenant with metadata

- **getTenant(tenantId)** → Object|null  
  Get tenant by ID (cached)

- **updateTenant(tenantId, updates)** → Object  
  Update tenant configuration

- **deleteTenant(tenantId)** → boolean  
  Delete tenant (removes all data associations)

- **listTenants(filter)** → Object[]  
  List tenants with optional filtering by plan/status

- **tenantExists(tenantId)** → boolean  
  Check if tenant exists

- **getTenantCount()** → number  
  Get total number of tenants

- **addUserToTenant(tenantId, userId)** → void  
  Add user to tenant

- **removeUserFromTenant(tenantId, userId)** → boolean  
  Remove user from tenant

- **getTenantUsers(tenantId)** → string[]  
  Get all users in tenant

- **enableFeature(tenantId, featureName, config)** → void  
  Enable feature for tenant with configuration

- **isFeatureEnabled(tenantId, featureName)** → boolean  
  Check if feature is enabled for tenant

### DataPartitioner

#### Constructor
```javascript
new DataPartitioner({
  strategy: 'row'|'schema'|'database',  // Isolation strategy
  databases: Object,                     // Database configs
  partitionMap: Map                      // Existing mappings
})
```

#### Methods

- **registerPartition(tenantId, partitionKey)** → void  
  Register tenant to partition

- **getPartition(tenantId)** → string|null  
  Get partition for tenant

- **createSchema(tenantId, schemaName)** → string  
  Create schema for tenant (schema strategy only)

- **getSchema(tenantId)** → string|null  
  Get schema name for tenant

- **registerTenantTable(tenantId, tableName, tenantColumn)** → void  
  Register table for row-level isolation

- **buildTenantQuery(tenantId, tableName, conditions)** → Object  
  Build isolated query with automatic filtering

- **getAllPartitions()** → Object  
  Get all tenant-to-partition mappings

- **getStatistics()** → Object  
  Get partitioning statistics

### TenantRouter

#### Constructor
```javascript
new TenantRouter({
  // Configuration options
})
```

#### Methods

- **registerStrategy(name, extractor)** → void  
  Register custom tenant extraction strategy

- **extractTenantId(req, strategies)** → string|null  
  Extract tenant ID from request using strategies

- **setContext(tenantId, context)** → void  
  Set tenant context for request

- **getContext(tenantId)** → Object|null  
  Get tenant context

- **clearContext(tenantId)** → void  
  Clear tenant context

- **middleware(tenantValidator)** → Function  
  Create Express middleware for extraction

#### Built-in Extraction Strategies

- `header` - From `x-tenant-id` header
- `subdomain` - From first subdomain (tenant.app.com)
- `path` - From path pattern /tenants/{tenantId}
- `query` - From `tenantId` query parameter
- `hostname` - From hostname pattern

### IsolationManager

#### Constructor
```javascript
new IsolationManager({
  strictMode: boolean  // Throw errors on violations (default: true)
})
```

#### Methods

- **validateAccess(tenantId, resourceId, resourceTenant)** → boolean  
  Check if tenant can access resource

- **validateQuery(tenantId, query)** → boolean  
  Validate query is safe from SQL injection

- **blacklistTenant(tenantId, reason)** → void  
  Emergency blacklist tenant

- **whitelistTenant(tenantId)** → void  
  Remove tenant from blacklist

- **isBlacklisted(tenantId)** → boolean  
  Check if tenant is blacklisted

- **getAccessLog(tenantId)** → Object[]  
  Get access log for tenant

- **getViolations(tenantId)** → Object[]  
  Get security violations for tenant

- **getSecurityReport()** → Object  
  Get complete security audit report

## Choosing Isolation Strategy

### Row-Level (Best for: SaaS, Multi-tenant)
```javascript
const tenancy = TenancyFactory.createRowLevelSystem();
// All tenants share one database
// Filtered by tenant_id column
// Lowest cost, highest scalability
// Requires careful query filtering
```

### Schema-Level (Best for: High-isolation SaaS)
```javascript
const tenancy = TenancyFactory.createSchemaLevelSystem();
// Each tenant has own schema in one database
// Complete data isolation per tenant
// Moderate cost, good scalability
// Schema-level permissions
```

### Database-Level (Best for: Enterprise, Compliance)
```javascript
const tenancy = TenancyFactory.createDatabaseLevelSystem();
// Each tenant has dedicated database
// Maximum isolation and security
// Higher cost, excellent performance per tenant
// Regulatory compliance simplicity
```

## Multi-Tenant Query Example

```javascript
// Without isolation (UNSAFE!)
const articles = db.query('SELECT * FROM articles WHERE status = ?', ['published']);
// Returns articles from all tenants!

// With row-level isolation
const query = partitioner.buildTenantQuery(tenantId, 'articles', { status: 'published' });
// Builds: { table: 'articles', where: { status: 'published', tenant_id: tenantId } }
const articles = db.query(query);
// Returns only articles for specific tenant
```

## Security Best Practices

1. **Always Filter by Tenant**
   ```javascript
   // Use buildTenantQuery for every data access
   const query = partitioner.buildTenantQuery(tenantId, tableName, filters);
   ```

2. **Validate Tenant Context**
   ```javascript
   if (!manager.tenantExists(req.tenantId)) {
     return res.status(403).json({ error: 'Invalid tenant' });
   }
   ```

3. **Monitor Access Logs**
   ```javascript
   const violations = isolation.getViolations(tenantId);
   if (violations.length > threshold) {
     isolation.blacklistTenant(tenantId, 'Suspicious activity');
   }
   ```

4. **Cross-Tenant Join Prevention**
   ```javascript
   // Each tenant query builds WHERE tenant_id = X
   // Prevents accidental joins across tenants
   ```

5. **Feature-Based Access**
   ```javascript
   if (!manager.isFeatureEnabled(tenantId, 'advanced-analytics')) {
     return res.status(403).json({ error: 'Feature not enabled' });
   }
   ```

## Testing

Run comprehensive test suite:

```bash
npm test
```

Test categories:
- Tenant Manager (10 tests)
- Data Partitioner (12 tests)
- Tenant Router (12 tests)
- Isolation Manager (8 tests)
- Integration (6 tests)

## Examples

Run included examples:

```bash
node examples.js
```

Covers:
1. Tenant lifecycle management
2. Row-level isolation
3. Schema-level isolation
4. Tenant request routing
5. Isolation enforcement and violations
6. Complete system setup

## Troubleshooting

### "Tenant not found"
```javascript
// Ensure tenant exists before operations
if (!manager.tenantExists(tenantId)) {
  manager.createTenant(tenantId, { name: 'Tenant Name', ... });
}
```

### "Cross-tenant access denied"
```javascript
// Data is isolated - ensure tenant_id matches
const query = partitioner.buildTenantQuery(tenantId, 'articles');
// WHERE will include tenant_id filter
```

### "Tenant ID not extracted"
```javascript
// Check extraction strategies
const tenantId = router.extractTenantId(req, ['header', 'subdomain', 'path']);
// Add custom strategy if needed
router.registerStrategy('custom', (req) => req.user.tenantId);
```

### Performance slow for large tables
```javascript
// Use schema-level or database-level isolation
// Row-level requires filtering by tenant_id on every query
// Add index: CREATE INDEX idx_articles_tenant ON articles(tenant_id)
```

## License

HELIOS v4.0 - Proprietary

## Support

For issues and feature requests: support@helios.example.com
