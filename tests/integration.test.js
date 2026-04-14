/**
 * HELIOS Integration Tests
 * Tests all 6 modules working together
 * v7.0
 */

const assert = require('assert');
const { HELIOS, GUIDashboard, SecuritySystem, PatternLearning, AIOrchestrator, USBInstaller, BuildAgents } = require('../modules');
const { Logger, Validator, Cache, RetryHandler, CircuitBreaker } = require('../modules/utils');

// Test Suite
const tests = [];
let passed = 0;
let failed = 0;

function test(name, fn) {
  tests.push({ name, fn });
}

async function runTests() {
  console.log('\n🧪 HELIOS Integration Test Suite v7.0\n');
  console.log('═'.repeat(60));

  for (const { name, fn } of tests) {
    try {
      await fn();
      console.log(`✅ PASS: ${name}`);
      passed++;
    } catch (error) {
      console.log(`❌ FAIL: ${name}`);
      console.log(`   Error: ${error.message}`);
      failed++;
    }
  }

  console.log('═'.repeat(60));
  console.log(`\n📊 Results: ${passed} passed, ${failed} failed (${tests.length} total)\n`);
  return failed === 0;
}

// ============================================================================
// UTILITY TESTS
// ============================================================================

test('Logger: Can log messages with different levels', () => {
  const logger = new Logger('TestModule');
  logger.info('Info message');
  logger.warn('Warning message');
  logger.error('Error message');
  assert.strictEqual(logger.getLogs().length, 3);
  assert.strictEqual(logger.getLogs('error').length, 1);
});

test('Validator: Can validate strings', () => {
  const valid = Validator.validateString('hello', 'name');
  assert.strictEqual(valid, 'hello');
  
  try {
    Validator.validateString('', 'name', 1);
    assert.fail('Should have thrown');
  } catch (e) {
    assert(e.name === 'ValidationError');
  }
});

test('Validator: Can validate numbers', () => {
  const valid = Validator.validateNumber(42, 'count', 0, 100);
  assert.strictEqual(valid, 42);
  
  try {
    Validator.validateNumber(150, 'count', 0, 100);
    assert.fail('Should have thrown');
  } catch (e) {
    assert(e.name === 'ValidationError');
  }
});

test('Cache: Can store and retrieve values', () => {
  const cache = new Cache(1000);
  cache.set('key1', 'value1');
  assert.strictEqual(cache.get('key1'), 'value1');
  assert.strictEqual(cache.has('key1'), true);
  cache.delete('key1');
  assert.strictEqual(cache.get('key1'), null);
});

test('RetryHandler: Can retry failed operations', async () => {
  let attempts = 0;
  const retrier = new RetryHandler({ maxRetries: 3, initialDelay: 10 });
  
  try {
    await retrier.execute(async () => {
      attempts++;
      if (attempts < 3) throw new Error('Not yet');
      return 'success';
    });
  } catch (e) {
    // Expected to succeed on 3rd attempt
  }
  assert(attempts >= 3);
});

// ============================================================================
// MODULE TESTS
// ============================================================================

test('SecuritySystem: Can add and validate AppLock rules', () => {
  const security = new SecuritySystem();
  const rule = security.addAppLockRule({ path: 'C:\\Program Files' });
  assert(rule.id);
  assert.strictEqual(rule.path, 'C:\\Program Files');
  assert.strictEqual(rule.enabled, true);
});

test('SecuritySystem: Can add and validate Firewall rules', () => {
  const security = new SecuritySystem();
  const rule = security.addFirewallRule({ action: 'block', direction: 'inbound' });
  assert(rule.id);
  assert.strictEqual(rule.action, 'block');
  assert.strictEqual(rule.direction, 'inbound');
});

test('SecuritySystem: Can store and retrieve secrets', () => {
  const security = new SecuritySystem();
  security.storeSecret('api-key', 'secret123');
  const value = security.retrieveSecret('api-key');
  assert.strictEqual(value, 'secret123');
});

test('SecuritySystem: Validates input before storing rules', () => {
  const security = new SecuritySystem();
  try {
    security.addAppLockRule({});  // Missing required 'path'
    assert.fail('Should have thrown');
  } catch (e) {
    assert(e.name === 'ValidationError');
  }
});

test('PatternLearning: Can learn patterns and generate profiles', () => {
  const patterns = new PatternLearning();
  const learned = patterns.learnPattern({ type: 'test', cpu: 80, memory: 70 });
  assert(learned.id);
  
  const profile = patterns.generateProfile('test');
  assert(profile.recommendations);
  assert.strictEqual(profile.recommendations.length > 0, true);
});

test('PatternLearning: Can classify workloads', () => {
  const patterns = new PatternLearning();
  const compute = patterns.classifyWorkload({ cpu: 90, memory: 75 });
  assert.strictEqual(compute, 'compute_intensive');
  
  const balanced = patterns.classifyWorkload({ cpu: 40, memory: 45 });
  assert.strictEqual(balanced, 'balanced');
});

test('AIOrchestrator: Can schedule and track tasks', () => {
  const ai = new AIOrchestrator();
  const task = ai.scheduleTask('test-task', { priority: 'high' });
  assert(task.id);
  assert.strictEqual(task.priority, 'high');
  
  const status = ai.getTaskStatus(task.id);
  assert.strictEqual(status.name, 'test-task');
});

test('AIOrchestrator: Can update resource metrics', () => {
  const ai = new AIOrchestrator();
  ai.updateResourceMetrics(75, 60, 40);
  const status = ai.getResourceStatus();
  assert.strictEqual(status.cpu, '75%');
  assert.strictEqual(status.memory, '60%');
});

test('USBInstaller: Can detect USB devices', () => {
  const usb = new USBInstaller();
  const devices = usb.detectUSBDevices();
  assert(Array.isArray(devices));
  assert(devices.length >= 0);
});

test('USBInstaller: Can format USB devices', () => {
  const usb = new USBInstaller();
  usb.detectUSBDevices();
  const result = usb.formatUSB('USB001', 'NTFS');
  assert.strictEqual(result.status, 'success');
});

test('USBInstaller: Can flash images', () => {
  const usb = new USBInstaller();
  usb.detectUSBDevices();
  const result = usb.flashImage('USB001', './windows.iso', 'ISO');
  assert.strictEqual(result.status, 'success');
});

test('BuildAgents: Can execute tasks in parallel', () => {
  const build = new BuildAgents();
  const result = build.executeParallel(['task1', 'task2', 'task3']);
  assert.strictEqual(result.status, 'parallel_execution_started');
  assert.strictEqual(result.tasksCount, 3);
});

test('BuildAgents: Can create automations', () => {
  const build = new BuildAgents();
  const auto = build.createAutomation('on-push', 'run-tests');
  assert(auto.id);
  assert.strictEqual(auto.trigger, 'on-push');
  assert.strictEqual(auto.action, 'run-tests');
});

test('BuildAgents: Can create customizations', () => {
  const build = new BuildAgents();
  const custom = build.createCustomization('dev-profile', { env: 'development' });
  assert(custom.id);
  assert.strictEqual(custom.name, 'dev-profile');
});

// ============================================================================
// ORCHESTRATOR TESTS
// ============================================================================

test('HELIOS: Can initialize all modules', async () => {
  const helios = new HELIOS();
  const init = await helios.initialize();
  assert.strictEqual(init.modules, 6);
  assert(Array.isArray(init.core_modules));
});

test('HELIOS: Can get system status from all modules', () => {
  const helios = new HELIOS();
  const status = helios.getSystemStatus();
  assert.strictEqual(status.version, '7.0');
  assert.strictEqual(status.modules, 6);
  assert(status.status.gui);
  assert(status.status.security);
  assert(status.status.patterns);
  assert(status.status.ai);
  assert(status.status.usb);
  assert(status.status.build);
});

test('HELIOS: Can deploy all components', async () => {
  const helios = new HELIOS();
  const deploy = await helios.deploy();
  assert.strictEqual(deploy.status, 'deployment_complete');
  assert.strictEqual(deploy.total_modules, 6);
});

// ============================================================================
// INTEGRATION WORKFLOW TESTS
// ============================================================================

test('Complete Workflow: Security + AI + Build', async () => {
  const helios = new HELIOS();
  
  // Initialize
  await helios.initialize();
  
  // Add security rules
  helios.security.addAppLockRule({ path: 'C:\\Program Files' });
  helios.security.addFirewallRule({ action: 'block' });
  helios.security.storeSecret('api-key', 'secret123');
  
  // Learn patterns
  helios.patterns.learnPattern({ cpu: 80, memory: 70 });
  helios.patterns.generateProfile('production');
  
  // Schedule tasks
  helios.ai.scheduleTask('build-and-test', { priority: 'high' });
  
  // Execute builds
  helios.build.executeParallel(['compile', 'test', 'deploy']);
  
  // Get status
  const status = helios.getSystemStatus();
  assert(status.status.security.appLockRules > 0);
  assert(status.status.patterns.patternsLearned > 0);
  assert(status.status.ai.tasksScheduled > 0);
});

// ============================================================================
// ERROR HANDLING TESTS
// ============================================================================

test('Error Handling: SecuritySystem handles invalid rules gracefully', () => {
  const security = new SecuritySystem();
  try {
    security.addFirewallRule({ action: 'invalid' });
    assert.fail('Should have thrown');
  } catch (error) {
    assert(error.message.includes('Action must be'));
  }
});

test('Error Handling: Duplicate AppLock rules are rejected', () => {
  const security = new SecuritySystem();
  security.addAppLockRule({ path: 'C:\\Test' });
  try {
    security.addAppLockRule({ path: 'C:\\Test' });
    assert.fail('Should have thrown');
  } catch (error) {
    assert(error.message.includes('already exists'));
  }
});

test('Error Handling: Missing secrets return null safely', () => {
  const security = new SecuritySystem();
  const value = security.retrieveSecret('nonexistent');
  assert.strictEqual(value, null);
});

// ============================================================================
// PERFORMANCE TESTS
// ============================================================================

test('Performance: Can handle 100 security rules', () => {
  const security = new SecuritySystem();
  for (let i = 0; i < 100; i++) {
    security.addAppLockRule({ path: `C:\\Path${i}` });
  }
  assert.strictEqual(security.getMetrics().appLockRules, 100);
});

test('Performance: Can handle 50 parallel tasks', () => {
  const build = new BuildAgents();
  const tasks = Array.from({ length: 50 }, (_, i) => `task_${i}`);
  const result = build.executeParallel(tasks);
  assert.strictEqual(result.tasksCount, 50);
});

// ============================================================================
// RUN TESTS
// ============================================================================

runTests().then(allPassed => {
  process.exit(allPassed ? 0 : 1);
}).catch(error => {
  console.error('Test runner error:', error);
  process.exit(1);
});
