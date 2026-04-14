/**
 * HELIOS v4.0 Multi-Tenancy - Usage Examples
 * Real-world scenarios demonstrating tenant isolation, partitioning, and routing
 * @module feat-tenancy/examples
 */

const {
  TenantManager,
  DataPartitioner,
  TenantRouter,
  IsolationManager,
  TenancyFactory,
  createTenantMiddleware,
  createIsolationMiddleware,
  createQueryIsolation
} = require('./index');

/**
 * Example 1: Tenant Lifecycle Management
 * Create, configure, and manage multiple tenants
 */
function exampleTenantLifecycle() {
  console.log('\n=== Example 1: Tenant Lifecycle Management ===\n');

  const manager = new TenantManager({
    defaults: {
      apiRateLimit: 1000,
      maxUsers: 50,
      storageGB: 10
    }
  });

  // Step 1: Create tenants
  console.log('1. Creating tenants...');
  const acme = manager.createTenant('acme-corp', {
    name: 'ACME Corporation',
    email: 'admin@acme.com',
    plan: 'enterprise',
    config: { apiRateLimit: 10000, maxUsers: 500 }
  });
  console.log(`   ✓ ${acme.name} (${acme.plan})`);

  const startup = manager.createTenant('startup-io', {
    name: 'StartUp.io',
    email: 'hello@startup.io',
    plan: 'pro'
  });
  console.log(`   ✓ ${startup.name} (${startup.plan})`);

  const free = manager.createTenant('free-user', {
    name: 'Free User',
    email: 'user@example.com',
    plan: 'free'
  });
  console.log(`   ✓ ${free.name} (${free.plan})`);

  // Step 2: Manage tenant users
  console.log('\n2. Managing tenant users...');
  manager.addUserToTenant('acme-corp', 'user-001');
  manager.addUserToTenant('acme-corp', 'user-002');
  manager.addUserToTenant('acme-corp', 'user-003');
  
  const acmeUsers = manager.getTenantUsers('acme-corp');
  console.log(`   ACME has ${acmeUsers.length} users: ${acmeUsers.join(', ')}`);

  // Step 3: Enable features
  console.log('\n3. Enabling features per tenant...');
  manager.enableFeature('acme-corp', 'advanced-analytics', { dashboards: true });
  manager.enableFeature('acme-corp', 'api-access', { rateLimit: 10000 });
  manager.enableFeature('startup-io', 'api-access', { rateLimit: 1000 });
  
  console.log('   Enterprise features:');
  console.log(`   - Advanced Analytics: ${manager.isFeatureEnabled('acme-corp', 'advanced-analytics')}`);
  console.log(`   - API Access: ${manager.isFeatureEnabled('acme-corp', 'api-access')}`);
  
  console.log('   Pro features:');
  console.log(`   - API Access: ${manager.isFeatureEnabled('startup-io', 'api-access')}`);
  console.log(`   - Advanced Analytics: ${manager.isFeatureEnabled('startup-io', 'advanced-analytics')}`);

  // Step 4: List and filter tenants
  console.log('\n4. Listing tenants by plan...');
  const enterpriseTenants = manager.listTenants({ plan: 'enterprise' });
  const proTenants = manager.listTenants({ plan: 'pro' });
  
  console.log(`   Enterprise: ${enterpriseTenants.length} tenant(s)`);
  console.log(`   Pro: ${proTenants.length} tenant(s)`);
  console.log(`   Total: ${manager.getTenantCount()} tenant(s)`);

  // Step 5: Update tenant
  console.log('\n5. Updating tenant configuration...');
  manager.updateTenant('startup-io', { plan: 'enterprise' });
  const updated = manager.getTenant('startup-io');
  console.log(`   ${updated.name} upgraded to ${updated.plan}`);
}

/**
 * Example 2: Row-Level Isolation (Shared Database)
 * Single database with tenant_id column for filtering
 */
function exampleRowLevelIsolation() {
  console.log('\n=== Example 2: Row-Level Isolation (Shared Database) ===\n');

  const partitioner = new DataPartitioner({
    strategy: 'row',
    databases: { default: 'postgresql://db.example.com/saas_app' }
  });

  // Register tenants
  console.log('1. Registering tenants...');
  partitioner.registerPartition('acme-corp', 'default');
  partitioner.registerPartition('startup-io', 'default');
  partitioner.registerTenantTable('acme-corp', 'articles', 'tenant_id');
  partitioner.registerTenantTable('startup-io', 'articles', 'tenant_id');
  console.log('   ✓ Tenants registered in shared database');

  // Build tenant-specific queries
  console.log('\n2. Building isolated queries...');
  
  const acmeQuery = partitioner.buildTenantQuery('acme-corp', 'articles', {
    status: 'published'
  });
  console.log('\n   ACME article query:');
  console.log(`   Table: ${acmeQuery.table}`);
  console.log(`   WHERE: `, acmeQuery.where);
  // Result: { table: 'articles', where: { status: 'published', tenant_id: 'acme-corp' } }

  const startupQuery = partitioner.buildTenantQuery('startup-io', 'articles', {
    category: 'tech'
  });
  console.log('\n   StartUp article query:');
  console.log(`   Table: ${startupQuery.table}`);
  console.log(`   WHERE: `, startupQuery.where);
  // Result: { table: 'articles', where: { category: 'tech', tenant_id: 'startup-io' } }

  // Statistics
  console.log('\n3. Partitioning statistics:');
  const stats = partitioner.getStatistics();
  console.log(`   Strategy: ${stats.strategy}`);
  console.log(`   Tenants: ${stats.totalPartitions}`);
  console.log(`   Shared Database: default`);
}

/**
 * Example 3: Schema-Level Isolation
 * Separate schema per tenant in one database
 */
function exampleSchemaLevelIsolation() {
  console.log('\n=== Example 3: Schema-Level Isolation ===\n');

  const partitioner = new DataPartitioner({
    strategy: 'schema',
    databases: { default: 'postgresql://db.example.com/saas_schemas' }
  });

  // Create schemas for tenants
  console.log('1. Creating tenant schemas...');
  const acmeSchema = partitioner.createSchema('acme-corp', 'acme_schema');
  const startupSchema = partitioner.createSchema('startup-io');
  
  console.log(`   ✓ ACME schema: ${acmeSchema}`);
  console.log(`   ✓ StartUp schema: ${startupSchema}`);

  // Build schema-qualified queries
  console.log('\n2. Building schema-qualified queries...');
  
  const acmeQuery = partitioner.buildTenantQuery('acme-corp', 'articles', {
    published: true
  });
  console.log('\n   ACME article query:');
  console.log(`   Table: ${acmeQuery.table}`);
  console.log(`   WHERE: `, acmeQuery.where);
  // Result: { table: 'acme_schema.articles', where: { published: true } }

  const startupQuery = partitioner.buildTenantQuery('startup-io', 'articles');
  console.log('\n   StartUp article query:');
  console.log(`   Table: ${startupQuery.table}`);
  // Result: { table: 'tenant_startup-io.articles', where: {} }

  // Get all partitions
  console.log('\n3. All tenant partitions:');
  const partitions = partitioner.getAllPartitions();
  for (const [tenantId, partition] of Object.entries(partitions)) {
    console.log(`   ${tenantId} → ${partition}`);
  }
}

/**
 * Example 4: Tenant Request Routing
 * Extract and route requests to correct tenant context
 */
function exampleTenantRouting() {
  console.log('\n=== Example 4: Tenant Request Routing ===\n');

  const router = new TenantRouter();

  // Register custom routing strategy
  console.log('1. Registering routing strategies...');
  router.registerStrategy('user', (req) => req.user?.tenantId);
  console.log('   ✓ Header extraction (x-tenant-id)');
  console.log('   ✓ Subdomain extraction');
  console.log('   ✓ Path extraction (/tenants/{tenantId})');
  console.log('   ✓ Query parameter (tenantId)');
  console.log('   ✓ Custom: user tenant ID');

  // Simulate requests
  console.log('\n2. Routing requests...');
  
  const headerReq = {
    headers: { 'x-tenant-id': 'acme-corp' },
    path: '/api/articles',
    hostname: 'app.example.com'
  };
  const headerTenant = router.extractTenantId(headerReq, ['header']);
  console.log(`   Header strategy: ${headerTenant}`);

  const subdomainReq = {
    headers: {},
    subdomains: ['acme'],
    hostname: 'acme.app.example.com'
  };
  const subdomainTenant = router.extractTenantId(subdomainReq, ['subdomain']);
  console.log(`   Subdomain strategy: ${subdomainTenant}`);

  const pathReq = {
    headers: {},
    path: '/tenants/startup-io/api/articles',
    hostname: 'app.example.com'
  };
  const pathTenant = router.extractTenantId(pathReq, ['path']);
  console.log(`   Path strategy: ${pathTenant}`);

  // Set and get context
  console.log('\n3. Managing tenant context...');
  router.setContext('acme-corp', {
    database: 'acme_schema',
    apiKey: 'key-acme-123',
    rateLimit: 10000
  });
  
  const context = router.getContext('acme-corp');
  console.log(`   ACME context:`, context);
  
  router.clearContext('acme-corp');
  console.log('   ✓ Context cleared');
}

/**
 * Example 5: Isolation Enforcement
 * Prevent cross-tenant data access and detect violations
 */
function exampleIsolationEnforcement() {
  console.log('\n=== Example 5: Isolation Enforcement ===\n');

  const isolation = new IsolationManager({ strictMode: true });

  // Valid access
  console.log('1. Validating legitimate access...');
  try {
    isolation.validateAccess('acme-corp', 'article-123', 'acme-corp');
    console.log('   ✓ ACME accessing own article - ALLOWED');
  } catch (error) {
    console.log(`   ✗ ${error.message}`);
  }

  // Cross-tenant access
  console.log('\n2. Detecting cross-tenant access attempts...');
  try {
    isolation.validateAccess('startup-io', 'article-123', 'acme-corp');
    console.log('   ✓ StartUp accessing ACME article - ALLOWED');
  } catch (error) {
    console.log(`   ✗ ${error.message} - BLOCKED`);
  }

  // Query validation
  console.log('\n3. Validating SQL queries...');
  const cleanQuery = { status: 'published', tenant_id: 'acme-corp' };
  const isClean = isolation.validateQuery('acme-corp', cleanQuery);
  console.log(`   Clean query: ${isClean ? 'ALLOWED' : 'BLOCKED'}`);

  const maliciousQuery = {
    search: "'; DROP TABLE articles; --",
    tenant_id: 'acme-corp'
  };
  const isMalicious = isolation.validateQuery('acme-corp', maliciousQuery);
  console.log(`   Malicious query: ${isMalicious ? 'ALLOWED' : 'BLOCKED'}`);

  // Tenant blacklisting
  console.log('\n4. Emergency tenant blacklisting...');
  isolation.blacklistTenant('bad-actor-ltd', 'Suspicious activity detected');
  console.log('   ✓ Tenant blacklisted');
  console.log(`   Is blacklisted: ${isolation.isBlacklisted('bad-actor-ltd')}`);

  // Security report
  console.log('\n5. Security audit report...');
  const report = isolation.getSecurityReport();
  console.log(`   Blacklisted tenants: ${report.blacklistedTenants.length}`);
  console.log(`   Access logs: ${report.totalAccessLogs}`);
  console.log(`   Total violations: ${report.totalViolations}`);
  
  if (report.recentViolations.length > 0) {
    console.log('\n   Recent violations:');
    report.recentViolations.forEach(v => {
      console.log(`   - ${v.type}: ${v.details}`);
    });
  }
}

/**
 * Example 6: Complete Multi-Tenancy System Setup
 * Integrated system with all components
 */
function exampleCompleteSystem() {
  console.log('\n=== Example 6: Complete Multi-Tenancy System Setup ===\n');

  // Create complete system
  console.log('1. Creating complete tenancy system...');
  const tenancy = TenancyFactory.createSystem('row', {
    databases: { default: 'postgresql://localhost/saas' },
    isolation: { strictMode: true }
  });
  console.log(`   ✓ System created (${tenancy.strategy} isolation)`);

  // Setup tenants
  console.log('\n2. Setting up tenants...');
  const tenants = [
    { id: 'acme-corp', name: 'ACME Corporation', plan: 'enterprise' },
    { id: 'tech-startup', name: 'TechStart Inc', plan: 'pro' },
    { id: 'freelancer', name: 'John Doe', plan: 'free' }
  ];

  tenants.forEach(t => {
    tenancy.manager.createTenant(t.id, {
      name: t.name,
      plan: t.plan,
      email: `admin@${t.id}.example.com`
    });
  });
  console.log(`   ✓ ${tenants.length} tenants created`);

  // Setup routing
  console.log('\n3. Setting up request routing...');
  tenancy.router.setContext('acme-corp', {
    database: 'saas',
    rateLimit: 10000,
    features: ['analytics', 'api-access', 'sso']
  });
  console.log('   ✓ Routing configured');

  // Test complete flow
  console.log('\n4. Testing complete request flow...');
  const mockReq = {
    headers: { 'x-tenant-id': 'acme-corp' },
    path: '/api/articles',
    hostname: 'app.example.com'
  };

  const tenantId = tenancy.router.extractTenantId(mockReq);
  const tenant = tenancy.manager.getTenant(tenantId);
  const query = tenancy.partitioner.buildTenantQuery(tenantId, 'articles', { status: 'published' });
  
  console.log(`   Tenant: ${tenant.name}`);
  console.log(`   Plan: ${tenant.plan}`);
  console.log(`   Query: ${JSON.stringify(query.where)}`);
  console.log('   ✓ Complete flow successful');

  // Statistics
  console.log('\n5. System statistics...');
  console.log(`   Total tenants: ${tenancy.manager.getTenantCount()}`);
  console.log(`   Isolation strategy: ${tenancy.strategy}`);
  const stats = tenancy.partitioner.getStatistics();
  console.log(`   Partitions: ${stats.totalPartitions}`);
}

// Export example functions
module.exports = {
  exampleTenantLifecycle,
  exampleRowLevelIsolation,
  exampleSchemaLevelIsolation,
  exampleTenantRouting,
  exampleIsolationEnforcement,
  exampleCompleteSystem
};

// Run examples if executed directly
if (require.main === module) {
  try {
    exampleTenantLifecycle();
    exampleRowLevelIsolation();
    exampleSchemaLevelIsolation();
    exampleTenantRouting();
    exampleIsolationEnforcement();
    exampleCompleteSystem();
    console.log('\n=== All Examples Completed Successfully ===\n');
  } catch (error) {
    console.error('Error running examples:', error);
  }
}
