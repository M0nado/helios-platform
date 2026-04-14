/**
 * HELIOS v4.0 feat-tenancy - Comprehensive Test Suite
 * Tests for Tenant Manager, Data Partitioner, Tenant Router, and Isolation Manager
 * 48 tests covering all functionality and edge cases
 * @module feat-tenancy/tests
 */

const assert = require('assert');
const {
  TenantManager,
  DataPartitioner,
  TenantRouter,
  IsolationManager,
  TenancyFactory,
  createTenantMiddleware,
  createIsolationMiddleware,
  createQueryIsolation
} = require('../index');

// ===== TEST UTILITIES =====

function assertEquals(actual, expected, message) {
  assert.strictEqual(actual, expected, message);
}

function assertThrows(fn, errorMessage) {
  try {
    fn();
    throw new Error('Expected error to be thrown');
  } catch (error) {
    if (errorMessage && !error.message.includes(errorMessage)) {
      throw new Error(`Expected error containing "${errorMessage}", got: ${error.message}`);
    }
  }
}

function assertArrayEquals(actual, expected) {
  assert.deepStrictEqual(actual.sort(), expected.sort());
}

// ===== TENANTMANAGER TESTS =====

function testTenantManagerConstruction() {
  const manager = new TenantManager({
    defaults: { apiLimit: 1000 }
  });
  
  assertEquals(manager.tenants.size, 0, 'Should start with no tenants');
  assertEquals(manager.defaults.apiLimit, 1000, 'Should store defaults');
}

function testCreateTenant() {
  const manager = new TenantManager();
  
  const tenant = manager.createTenant('test-tenant', {
    name: 'Test Tenant',
    email: 'test@example.com',
    plan: 'pro'
  });
  
  assertEquals(tenant.id, 'test-tenant', 'ID should match');
  assertEquals(tenant.name, 'Test Tenant', 'Name should match');
  assertEquals(tenant.plan, 'pro', 'Plan should match');
}

function testCreateTenantValidation() {
  const manager = new TenantManager();
  
  assertThrows(() => manager.createTenant('', { name: 'Test' }), 'Tenant ID required');
  assertThrows(() => manager.createTenant('test', {}), 'Name required');
  assertThrows(() => manager.createTenant('test', { name: null }), 'Valid name required');
}

function testTenantDuplication() {
  const manager = new TenantManager();
  manager.createTenant('test', { name: 'Test' });
  
  assertThrows(() => manager.createTenant('test', { name: 'Duplicate' }), 'Already exists');
}

function testGetTenant() {
  const manager = new TenantManager();
  manager.createTenant('test', { name: 'Test' });
  
  const tenant = manager.getTenant('test');
  assertEquals(tenant.id, 'test', 'Should retrieve tenant');
  assertEquals(tenant.name, 'Test', 'Should have correct name');
}

function testTenantCaching() {
  const manager = new TenantManager();
  manager.createTenant('test', { name: 'Test' });
  
  manager.getTenant('test');
  assert(manager.tenantCache.has('test'), 'Should cache tenant');
}

function testUpdateTenant() {
  const manager = new TenantManager();
  manager.createTenant('test', { name: 'Test', plan: 'free' });
  
  manager.updateTenant('test', { plan: 'enterprise' });
  const tenant = manager.getTenant('test');
  
  assertEquals(tenant.plan, 'enterprise', 'Plan should be updated');
  assertEquals(tenant.name, 'Test', 'Other fields should remain');
}

function testDeleteTenant() {
  const manager = new TenantManager();
  manager.createTenant('test', { name: 'Test' });
  
  const deleted = manager.deleteTenant('test');
  assertEquals(deleted, true, 'Should return true');
  assertEquals(manager.tenantExists('test'), false, 'Should be deleted');
}

function testListTenants() {
  const manager = new TenantManager();
  manager.createTenant('pro-1', { name: 'Pro 1', plan: 'pro' });
  manager.createTenant('free-1', { name: 'Free 1', plan: 'free' });
  manager.createTenant('pro-2', { name: 'Pro 2', plan: 'pro' });
  
  const proPlan = manager.listTenants({ plan: 'pro' });
  assertEquals(proPlan.length, 2, 'Should filter by plan');
}

function testTenantUsers() {
  const manager = new TenantManager();
  manager.createTenant('test', { name: 'Test' });
  
  manager.addUserToTenant('test', 'user-1');
  manager.addUserToTenant('test', 'user-2');
  
  const users = manager.getTenantUsers('test');
  assertArrayEquals(users, ['user-1', 'user-2']);
}

function testRemoveUserFromTenant() {
  const manager = new TenantManager();
  manager.createTenant('test', { name: 'Test' });
  manager.addUserToTenant('test', 'user-1');
  
  const removed = manager.removeUserFromTenant('test', 'user-1');
  assertEquals(removed, true, 'Should remove user');
  assertEquals(manager.getTenantUsers('test').length, 0, 'Should be empty');
}

function testTenantFeatures() {
  const manager = new TenantManager();
  manager.createTenant('test', { name: 'Test' });
  
  manager.enableFeature('test', 'analytics');
  assertEquals(manager.isFeatureEnabled('test', 'analytics'), true, 'Feature enabled');
  assertEquals(manager.isFeatureEnabled('test', 'unknown'), false, 'Unknown feature disabled');
}

function testTenantCount() {
  const manager = new TenantManager();
  assertEquals(manager.getTenantCount(), 0, 'Should start at 0');
  
  manager.createTenant('t1', { name: 'T1' });
  manager.createTenant('t2', { name: 'T2' });
  
  assertEquals(manager.getTenantCount(), 2, 'Should count tenants');
}

// ===== DATAPARTITIONER TESTS =====

function testDataPartitionerConstruction() {
  const partitioner = new DataPartitioner({
    strategy: 'row'
  });
  
  assertEquals(partitioner.strategy, 'row', 'Should store strategy');
}

function testInvalidStrategy() {
  assertThrows(() => new DataPartitioner({
    strategy: 'invalid'
  }), 'Invalid strategy');
}

function testRegisterPartition() {
  const partitioner = new DataPartitioner({ strategy: 'row' });
  
  partitioner.registerPartition('tenant-1', 'partition-1');
  assertEquals(partitioner.getPartition('tenant-1'), 'partition-1', 'Should store partition');
}

function testRegisterPartitionValidation() {
  const partitioner = new DataPartitioner({ strategy: 'row' });
  
  assertThrows(() => partitioner.registerPartition('', 'p1'), 'Tenant ID required');
  assertThrows(() => partitioner.registerPartition('t1', ''), 'Partition required');
}

function testRowLevelQuery() {
  const partitioner = new DataPartitioner({ strategy: 'row' });
  
  const query = partitioner.buildTenantQuery('tenant-1', 'articles', { status: 'published' });
  
  assertEquals(query.table, 'articles', 'Table should be unqualified');
  assertEquals(query.where.tenant_id, 'tenant-1', 'Should add tenant_id filter');
  assertEquals(query.where.status, 'published', 'Should preserve conditions');
}

function testSchemaCreation() {
  const partitioner = new DataPartitioner({ strategy: 'schema' });
  
  const schema = partitioner.createSchema('tenant-1');
  assertEquals(schema, 'tenant_tenant-1', 'Should create schema name');
  assertEquals(partitioner.getSchema('tenant-1'), schema, 'Should store schema');
}

function testSchemaQuery() {
  const partitioner = new DataPartitioner({ strategy: 'schema' });
  partitioner.createSchema('tenant-1');
  
  const query = partitioner.buildTenantQuery('tenant-1', 'articles');
  assertEquals(query.table, 'tenant_tenant-1.articles', 'Should qualify table');
}

function testDatabaseQuery() {
  const partitioner = new DataPartitioner({ strategy: 'database' });
  partitioner.registerPartition('tenant-1', 'tenant1_db');
  
  const query = partitioner.buildTenantQuery('tenant-1', 'articles', { id: 1 });
  assertEquals(query.database, 'tenant1_db', 'Should specify database');
  assertEquals(query.table, 'articles', 'Table should be unqualified');
  assertEquals(query.where.id, 1, 'Should preserve conditions');
}

function testAllPartitions() {
  const partitioner = new DataPartitioner({ strategy: 'row' });
  partitioner.registerPartition('t1', 'p1');
  partitioner.registerPartition('t2', 'p2');
  
  const partitions = partitioner.getAllPartitions();
  assertEquals(partitions.t1, 'p1', 'Should return partitions');
  assertEquals(partitions.t2, 'p2', 'Should return all partitions');
}

function testStatistics() {
  const partitioner = new DataPartitioner({ strategy: 'row' });
  partitioner.registerPartition('t1', 'p1');
  
  const stats = partitioner.getStatistics();
  assertEquals(stats.strategy, 'row', 'Should include strategy');
  assertEquals(stats.totalPartitions, 1, 'Should count partitions');
}

// ===== TENANTROUTER TESTS =====

function testTenantRouterConstruction() {
  const router = new TenantRouter();
  
  assert(router.extractors instanceof Object, 'Should have extractors');
  assert(router.tenantContext instanceof Map, 'Should have context storage');
}

function testHeaderExtraction() {
  const router = new TenantRouter();
  
  const req = {
    headers: { 'x-tenant-id': 'tenant-123' },
    path: '/api',
    hostname: 'app.example.com'
  };
  
  const tenantId = router.extractTenantId(req, ['header']);
  assertEquals(tenantId, 'tenant-123', 'Should extract from header');
}

function testPathExtraction() {
  const router = new TenantRouter();
  
  const req = {
    headers: {},
    path: '/tenants/tenant-456/api',
    hostname: 'app.example.com'
  };
  
  const tenantId = router.extractTenantId(req, ['path']);
  assertEquals(tenantId, 'tenant-456', 'Should extract from path');
}

function testSubdomainExtraction() {
  const router = new TenantRouter();
  
  const req = {
    headers: {},
    path: '/api',
    hostname: 'tenant.app.example.com',
    subdomains: ['tenant']
  };
  
  const tenantId = router.extractTenantId(req, ['subdomain']);
  assertEquals(tenantId, 'tenant', 'Should extract from subdomain');
}

function testStrategyFallback() {
  const router = new TenantRouter();
  
  const req = {
    headers: {},
    path: '/tenants/tenant-789/api',
    hostname: 'app.example.com'
  };
  
  const tenantId = router.extractTenantId(req, ['header', 'path']);
  assertEquals(tenantId, 'tenant-789', 'Should fallback to next strategy');
}

function testCustomStrategy() {
  const router = new TenantRouter();
  router.registerStrategy('custom', (req) => req.customTenant);
  
  const req = { customTenant: 'custom-123' };
  const tenantId = router.extractTenantId(req, ['custom']);
  
  assertEquals(tenantId, 'custom-123', 'Should use custom strategy');
}

function testContextManagement() {
  const router = new TenantRouter();
  
  const context = { database: 'db1', schema: 'schema1' };
  router.setContext('tenant-1', context);
  
  const retrieved = router.getContext('tenant-1');
  assertEquals(retrieved.database, 'db1', 'Should store and retrieve context');
}

function testRouteCache() {
  const router = new TenantRouter();
  
  const req = {
    headers: { 'x-tenant-id': 'tenant-cache-test' },
    path: '/api',
    hostname: 'app.example.com'
  };
  
  router.extractTenantId(req);
  assert(router.routeCache.size > 0, 'Should cache routes');
}

function testMiddlewareCreation() {
  const router = new TenantRouter();
  const middleware = router.middleware((tenantId) => true);
  
  assert(typeof middleware === 'function', 'Should create middleware function');
}

// ===== ISOLATIONMANAGER TESTS =====

function testIsolationManagerConstruction() {
  const isolation = new IsolationManager({ strictMode: true });
  
  assertEquals(isolation.strictMode, true, 'Should store strict mode');
}

function testValidateAccess() {
  const isolation = new IsolationManager({ strictMode: false });
  
  const valid = isolation.validateAccess('tenant-1', 'resource-1', 'tenant-1');
  assertEquals(valid, true, 'Should allow own resource access');
}

function testCrossTenantAccess() {
  const isolation = new IsolationManager({ strictMode: false });
  
  const valid = isolation.validateAccess('tenant-1', 'resource-1', 'tenant-2');
  assertEquals(valid, false, 'Should block cross-tenant access');
}

function testStrictModeThrows() {
  const isolation = new IsolationManager({ strictMode: true });
  
  assertThrows(() => isolation.validateAccess('tenant-1', 'res-1', 'tenant-2'), 'Cross-tenant');
}

function testQueryValidation() {
  const isolation = new IsolationManager();
  
  const valid = isolation.validateQuery('tenant-1', { tenant_id: 'tenant-1' });
  assertEquals(valid, true, 'Should validate clean query');
}

function testSuspiciousQueryPattern() {
  const isolation = new IsolationManager({ strictMode: false });
  
  const valid = isolation.validateQuery('tenant-1', {
    search: "'; DROP TABLE users; --"
  });
  assertEquals(valid, false, 'Should detect SQL injection');
}

function testTenantBlacklisting() {
  const isolation = new IsolationManager();
  
  isolation.blacklistTenant('bad-tenant', 'Security violation');
  assertEquals(isolation.isBlacklisted('bad-tenant'), true, 'Should blacklist tenant');
}

function testTenantWhitelisting() {
  const isolation = new IsolationManager();
  
  isolation.blacklistTenant('tenant-1');
  isolation.whitelistTenant('tenant-1');
  assertEquals(isolation.isBlacklisted('tenant-1'), false, 'Should whitelist tenant');
}

function testAccessLogging() {
  const isolation = new IsolationManager({ strictMode: false });
  
  isolation.validateAccess('tenant-1', 'resource-1');
  const log = isolation.getAccessLog('tenant-1');
  
  assertEquals(log.length > 0, true, 'Should log access');
}

function testViolationLogging() {
  const isolation = new IsolationManager({ strictMode: false });
  
  isolation.validateAccess('tenant-1', 'resource-1', 'tenant-2');
  const violations = isolation.getViolations('tenant-1');
  
  assertEquals(violations.length > 0, true, 'Should log violations');
}

function testSecurityReport() {
  const isolation = new IsolationManager({ strictMode: false });
  
  isolation.blacklistTenant('bad-tenant');
  isolation.validateAccess('tenant-1', 'resource-1');
  
  const report = isolation.getSecurityReport();
  assertEquals(report.blacklistedTenants.length > 0, true, 'Report should include blacklist');
  assertEquals(report.totalAccessLogs > 0, true, 'Report should include logs');
}

// ===== FACTORY TESTS =====

function testFactoryRowLevel() {
  const system = TenancyFactory.createRowLevelSystem();
  
  assertEquals(system.strategy, 'row', 'Should create row-level system');
  assert(system.manager instanceof TenantManager, 'Should include manager');
  assert(system.partitioner instanceof DataPartitioner, 'Should include partitioner');
}

function testFactorySchemaLevel() {
  const system = TenancyFactory.createSchemaLevelSystem();
  
  assertEquals(system.strategy, 'schema', 'Should create schema-level system');
}

function testFactoryDatabaseLevel() {
  const system = TenancyFactory.createDatabaseLevelSystem();
  
  assertEquals(system.strategy, 'database', 'Should create database-level system');
}

function testFactoryInvalidStrategy() {
  assertThrows(() => TenancyFactory.createSystem('invalid'), 'Invalid strategy');
}

// ===== MIDDLEWARE TESTS =====

function testTenantMiddleware() {
  const router = new TenantRouter();
  const manager = new TenantManager();
  
  manager.createTenant('test-tenant', { name: 'Test' });
  
  const middleware = createTenantMiddleware(router, manager);
  assert(typeof middleware === 'function', 'Should return middleware');
}

function testIsolationMiddleware() {
  const isolation = new IsolationManager();
  const middleware = createIsolationMiddleware(isolation);
  
  assert(typeof middleware === 'function', 'Should return middleware');
}

function testQueryIsolation() {
  const partitioner = new DataPartitioner({ strategy: 'row' });
  const builder = createQueryIsolation(partitioner);
  
  const query = builder('tenant-1', 'articles');
  assertEquals(query.where.tenant_id, 'tenant-1', 'Should build isolated query');
}

// ===== RUN ALL TESTS =====

const tests = [
  // TenantManager tests
  { name: 'TenantManager Construction', fn: testTenantManagerConstruction },
  { name: 'Create Tenant', fn: testCreateTenant },
  { name: 'Create Tenant Validation', fn: testCreateTenantValidation },
  { name: 'Tenant Duplication', fn: testTenantDuplication },
  { name: 'Get Tenant', fn: testGetTenant },
  { name: 'Tenant Caching', fn: testTenantCaching },
  { name: 'Update Tenant', fn: testUpdateTenant },
  { name: 'Delete Tenant', fn: testDeleteTenant },
  { name: 'List Tenants', fn: testListTenants },
  { name: 'Tenant Users', fn: testTenantUsers },
  { name: 'Remove User', fn: testRemoveUserFromTenant },
  { name: 'Tenant Features', fn: testTenantFeatures },
  { name: 'Tenant Count', fn: testTenantCount },
  
  // DataPartitioner tests
  { name: 'DataPartitioner Construction', fn: testDataPartitionerConstruction },
  { name: 'Invalid Strategy', fn: testInvalidStrategy },
  { name: 'Register Partition', fn: testRegisterPartition },
  { name: 'Partition Validation', fn: testRegisterPartitionValidation },
  { name: 'Row Level Query', fn: testRowLevelQuery },
  { name: 'Schema Creation', fn: testSchemaCreation },
  { name: 'Schema Query', fn: testSchemaQuery },
  { name: 'Database Query', fn: testDatabaseQuery },
  { name: 'All Partitions', fn: testAllPartitions },
  { name: 'Statistics', fn: testStatistics },
  
  // TenantRouter tests
  { name: 'TenantRouter Construction', fn: testTenantRouterConstruction },
  { name: 'Header Extraction', fn: testHeaderExtraction },
  { name: 'Path Extraction', fn: testPathExtraction },
  { name: 'Subdomain Extraction', fn: testSubdomainExtraction },
  { name: 'Strategy Fallback', fn: testStrategyFallback },
  { name: 'Custom Strategy', fn: testCustomStrategy },
  { name: 'Context Management', fn: testContextManagement },
  { name: 'Route Cache', fn: testRouteCache },
  { name: 'Middleware Creation', fn: testMiddlewareCreation },
  
  // IsolationManager tests
  { name: 'IsolationManager Construction', fn: testIsolationManagerConstruction },
  { name: 'Validate Access', fn: testValidateAccess },
  { name: 'Cross-Tenant Access', fn: testCrossTenantAccess },
  { name: 'Strict Mode', fn: testStrictModeThrows },
  { name: 'Query Validation', fn: testQueryValidation },
  { name: 'Suspicious Query', fn: testSuspiciousQueryPattern },
  { name: 'Tenant Blacklist', fn: testTenantBlacklisting },
  { name: 'Tenant Whitelist', fn: testTenantWhitelisting },
  { name: 'Access Logging', fn: testAccessLogging },
  { name: 'Violation Logging', fn: testViolationLogging },
  { name: 'Security Report', fn: testSecurityReport },
  
  // Factory tests
  { name: 'Factory Row Level', fn: testFactoryRowLevel },
  { name: 'Factory Schema Level', fn: testFactorySchemaLevel },
  { name: 'Factory Database Level', fn: testFactoryDatabaseLevel },
  { name: 'Factory Invalid Strategy', fn: testFactoryInvalidStrategy },
  
  // Middleware tests
  { name: 'Tenant Middleware', fn: testTenantMiddleware },
  { name: 'Isolation Middleware', fn: testIsolationMiddleware },
  { name: 'Query Isolation', fn: testQueryIsolation }
];

function runTests() {
  console.log('\n═══════════════════════════════════════════════════════');
  console.log('HELIOS v4.0 feat-tenancy - Test Suite');
  console.log('═══════════════════════════════════════════════════════\n');
  
  let passed = 0;
  let failed = 0;
  const failures = [];
  
  for (const test of tests) {
    try {
      test.fn();
      console.log(`✓ ${test.name}`);
      passed++;
    } catch (error) {
      console.log(`✗ ${test.name}`);
      console.log(`  Error: ${error.message}`);
      failed++;
      failures.push({ test: test.name, error: error.message });
    }
  }
  
  console.log('\n═══════════════════════════════════════════════════════');
  console.log(`Total: ${tests.length} | Passed: ${passed} | Failed: ${failed}`);
  console.log('═══════════════════════════════════════════════════════\n');
  
  if (failures.length > 0) {
    console.log('FAILURES:');
    failures.forEach(f => {
      console.log(`\n${f.test}:`);
      console.log(`  ${f.error}`);
    });
  }
  
  return failed === 0;
}

module.exports = { runTests, tests };

if (require.main === module) {
  const success = runTests();
  process.exit(success ? 0 : 1);
}
