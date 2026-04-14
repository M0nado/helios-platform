# HELIOS v7.0 - Complete API Reference

**Date:** April 14, 2026  
**Version:** 7.0  
**Status:** Production Ready

---

## 📖 TABLE OF CONTENTS

1. [Shared Utils API](#shared-utils-api)
2. [Security System API](#security-system-api)
3. [Pattern Learning API](#pattern-learning-api)
4. [AI Orchestrator API](#ai-orchestrator-api)
5. [USB Installer API](#usb-installer-api)
6. [Build Agents API](#build-agents-api)
7. [GUI Dashboard API](#gui-dashboard-api)
8. [Main HELIOS Orchestrator](#main-helios-orchestrator)

---

## SHARED UTILS API

### Logger

```javascript
const { Logger } = require('./modules/utils');
const logger = new Logger('ModuleName');

// Logging methods
logger.info(message, context)     // Info level
logger.warn(message, context)     // Warning level
logger.error(message, context)    // Error level
logger.debug(message, context)    // Debug level

// Retrieval
logger.getLogs()                   // Get all logs
logger.getLogs('error')            // Get logs by level
logger.clearLogs()                 // Clear all logs

// Example
logger.info('Operation started', { operation: 'deploy' });
```

### Validator

```javascript
const { Validator } = require('./modules/utils');

// String validation
Validator.validateString(value, 'fieldName', minLength, maxLength)

// Number validation
Validator.validateNumber(value, 'fieldName', min, max)

// Object validation
Validator.validateObject(value, 'fieldName', ['required', 'keys'])

// Array validation
Validator.validateArray(value, 'fieldName', minLength)

// Throws ValidationError on failure
try {
  Validator.validateString('test', 'name', 5);  // Too short
} catch (error) {
  console.error(error.message);  // "Field 'name' must be at least 5 chars"
}
```

### ValidationError

```javascript
const { ValidationError } = require('./modules/utils');

try {
  throw new ValidationError('Invalid input');
} catch (error) {
  console.error(error.message);
  console.error(error.field);
  console.error(error.fields);  // Map of all field errors
}
```

### EventEmitter

```javascript
const { EventEmitter } = require('./modules/utils');
const emitter = new EventEmitter();

// Listen to events
emitter.on('event-name', (data) => {
  console.log('Event fired:', data);
});

// Listen once
emitter.once('event-name', (data) => {
  console.log('Event fired once');
});

// Emit events
emitter.emit('event-name', { key: 'value' });

// Remove listener
emitter.off('event-name', handler);
```

### CircuitBreaker

```javascript
const { CircuitBreaker } = require('./modules/utils');

const breaker = new CircuitBreaker(async () => {
  // Operation that might fail
}, { 
  failureThreshold: 5,
  resetTimeout: 60000 
});

try {
  const result = await breaker.execute();
} catch (error) {
  // Circuit is open or operation failed
}

// States: closed, open, half-open
const state = breaker.getState();
```

### Cache

```javascript
const { Cache } = require('./modules/utils');

const cache = new Cache(3600000);  // 1 hour TTL

// Store value
cache.set('key', 'value');

// Retrieve value
const value = cache.get('key');   // Returns value or null

// Check existence
if (cache.has('key')) { }

// Delete entry
cache.delete('key');

// Clear all
cache.clear();

// Get size
const size = cache.getSize();     // Number of entries
```

### RetryHandler

```javascript
const { RetryHandler } = require('./modules/utils');

const retrier = new RetryHandler({
  maxRetries: 3,
  initialDelay: 100,
  maxDelay: 10000,
  backoffMultiplier: 2
});

try {
  const result = await retrier.execute(async () => {
    // Operation to retry
    return await someAsyncOperation();
  });
} catch (error) {
  // Failed after all retries
}
```

---

## SECURITY SYSTEM API

```javascript
const { SecuritySystem } = require('./modules/security-system');
const security = new SecuritySystem();

// APP LOCK RULES
security.addAppLockRule({ path: 'C:\\Program Files' })
security.removeRule('rule-id')

// FIREWALL RULES
security.addFirewallRule({
  action: 'block',         // 'block' or 'allow'
  direction: 'inbound',    // 'inbound' or 'outbound'
  protocol: 'tcp'          // 'tcp', 'udp', or 'all'
})

// SECRET MANAGEMENT
security.storeSecret('api-key', 'secret-value')
const secret = security.retrieveSecret('api-key')

// VALIDATION
const validation = security.validateSecurity()
// Returns: { passed: boolean, issues: [...] }

// AUDIT LOG
const logs = security.getAuditLog(startTime, endTime)
security.clearAuditLog()

// METRICS
const metrics = security.getMetrics()

// EVENTS
security.on('applock-rule-added', (rule) => {})
security.on('firewall-rule-added', (rule) => {})
security.on('secret-stored', (secret) => {})
security.on('error', (error) => {})
```

---

## PATTERN LEARNING API

```javascript
const { PatternLearning } = require('./modules/pattern-learning');
const patterns = new PatternLearning();

// LEARN PATTERNS
patterns.learnPattern({
  type: 'web_app',
  cpu: 75,
  memory: 60,
  disk: 40,
  network: 25
})

// GENERATE PROFILES
const profile = patterns.generateProfile('web_app')
// Returns: { cpu, memory, disk, network, recommendations }

// CLASSIFY WORKLOAD
const classification = patterns.classifyWorkload({ cpu: 90, memory: 75 })
// Returns: 'compute_intensive', 'io_intensive', 'cpu_bound', 'memory_intensive', 'balanced'

// GET RECOMMENDATIONS
const resources = patterns.recommendResources(workload)

// QUERY DATA
const history = patterns.getPatternHistory(limit)
const profile = patterns.getProfileByType('web_app')

// CACHE
patterns.clearCache()

// METRICS
const metrics = patterns.getMetrics()

// EVENTS
patterns.on('pattern-learned', (pattern) => {})
patterns.on('profile-generated', (profile) => {})
patterns.on('resources-recommended', (data) => {})
```

---

## AI ORCHESTRATOR API

```javascript
const { AIOrchestrator } = require('./modules/ai-orchestrator');
const ai = new AIOrchestrator({ maxConcurrentTasks: 10 });

// SCHEDULE TASKS
const task = ai.scheduleTask({
  name: 'Optimize database',
  priority: 'high',        // 'critical', 'high', 'normal', 'low'
  cpu: 4,
  memory: 4096,
  maxRetries: 3
})

// ALLOCATE RESOURCES
ai.allocateResources({
  id: taskId,
  cpu: 8,
  memory: 8192,
  disk: 102400,
  network: 1000
})

// QUERY TASKS
const task = ai.getTaskStatus(taskId)
const pending = ai.getTasksByStatus('pending')
const schedule = ai.getSchedule()

// OPTIMIZE
const optimization = ai.optimizeResources()

// CANCEL
ai.cancelTask(taskId)

// METRICS
const metrics = ai.getMetrics()

// EVENTS
ai.on('task-scheduled', (task) => {})
ai.on('task-started', (task) => {})
ai.on('task-completed', (task) => {})
ai.on('task-failed', (task) => {})
ai.on('task-retry', ({ task, attempt }) => {})
ai.on('resources-allocated', (allocation) => {})
ai.on('resources-optimized', (optimization) => {})
```

---

## USB INSTALLER API

```javascript
const { USBInstaller } = require('./modules/usb-installer');
const usb = new USBInstaller();

// DETECT DEVICES
const devices = usb.detectUSBDevices()
// Returns: [{ id, name, size, status, detected }]

// FORMAT USB
const result = usb.formatUSB(deviceId, 'NTFS')
// Supports: 'NTFS', 'FAT32', 'exFAT'

// FLASH IMAGE
const result = usb.flashImage(deviceId, 'path/to/image.iso', 'ISO')
// Supports: 'ISO', 'IMG', 'WIM', 'VHD', 'ESD'

// INSTALL TOOLS
const result = await usb.installTool('node.js')
const result = await usb.installAll()  // Install all 40 tools

// QUERY
const progress = usb.getProgress()                    // 0-100
const device = usb.getDeviceStatus(deviceId)
const logs = usb.getInstallationLog(limit)

// METRICS
const metrics = usb.getMetrics()

// EVENTS
usb.on('devices-detected', (devices) => {})
usb.on('format-started', (data) => {})
usb.on('format-progress', ({ deviceId, progress }) => {})
usb.on('format-completed', (data) => {})
usb.on('flash-started', (data) => {})
usb.on('flash-progress', ({ deviceId, progress }) => {})
usb.on('flash-completed', (data) => {})
usb.on('install-started', ({ tool }) => {})
usb.on('install-completed', ({ tool }) => {})
usb.on('install-all-started', ({ toolCount }) => {})
usb.on('install-all-progress', ({ progress, tool, completed, total }) => {})
usb.on('install-all-completed', ({ results }) => {})
```

---

## BUILD AGENTS API

```javascript
const { BuildAgents } = require('./modules/build-agents');
const agents = new BuildAgents();

// EXECUTE TASKS
const results = await agents.executeParallel([
  { name: 'compile', priority: 'high' },
  { name: 'test', priority: 'normal' }
])

// QUERY AGENTS
const statuses = agents.getAllStatus()
const agent = agents.getAgent(agentId)

// BUILD RESULTS
const results = agents.getBuildResults(limit)

// AUTOMATIONS
agents.createAutomation('build-start', 'run-tests', {})
agents.executeAutomationByTrigger('build-start')
agents.disableAutomation(automationId)
const automations = agents.getAutomations()

// CUSTOMIZATIONS
agents.createCustomization('gaming-build', { 
  excludeTools: ['node'],
  includeTools: ['cuda']
})
const customizations = agents.getCustomizations()

// METRICS
const metrics = agents.getMetrics()

// EVENTS
agents.on('execution-started', (data) => {})
agents.on('execution-progress', ({ progress, completed, total }) => {})
agents.on('execution-completed', (result) => {})
agents.on('agent-task-completed', (agent) => {})
agents.on('agent-task-failed', (agent) => {})
agents.on('automation-created', (automation) => {})
agents.on('automation-triggered', (data) => {})
agents.on('automation-disabled', (automation) => {})
```

---

## GUI DASHBOARD API

```javascript
const { GUIDashboard } = require('./modules/gui-dashboard');
const dashboard = new GUIDashboard({ theme: 'dark' });

// TABS (8 available)
const tab = dashboard.getTab(tabId)  // 1-8
dashboard.updateTabData(tabId, { key: 'value' })

// WIDGETS
const widget = dashboard.addWidget(tabId, {
  type: 'chart',
  title: 'CPU Usage',
  data: { cpu: 45 }
})
dashboard.updateWidget(widgetId, { cpu: 65 })
const widgets = dashboard.getWidgets(tabId)

// ALERTS
dashboard.addAlert('error', 'High CPU', { cpu: 95 })
// Levels: 'info', 'warning', 'error', 'critical'

const alerts = dashboard.getAlerts()
const errors = dashboard.getAlerts('error')
dashboard.acknowledgeAlert(alertId)

// ALERT QUEUE
const queue = dashboard.getAlertQueue()
dashboard.clearAlertQueue()

// THEME
dashboard.setTheme('dark')  // 'dark', 'light', 'auto'

// RENDER
const rendered = dashboard.renderDashboard()

// WIDGET STATUS
const status = dashboard.getWidgetStatus()

// METRICS
const metrics = dashboard.getMetrics()

// EVENTS
dashboard.on('dashboard-initialized', (data) => {})
dashboard.on('tab-updated', (data) => {})
dashboard.on('widget-added', (data) => {})
dashboard.on('widget-updated', (widget) => {})
dashboard.on('alert-added', (alert) => {})
dashboard.on('alert-acknowledged', (alert) => {})
dashboard.on('alert-queue-cleared', (data) => {})
dashboard.on('theme-changed', (data) => {})
dashboard.on('dashboard-rendered', (dashboard) => {})
```

---

## MAIN HELIOS ORCHESTRATOR

```javascript
const { HELIOS } = require('./modules');

const helios = new HELIOS();

// INITIALIZE
await helios.initialize()

// ACCESS MODULES
helios.security          // SecuritySystem instance
helios.patterns          // PatternLearning instance
helios.ai                // AIOrchestrator instance
helios.usb               // USBInstaller instance
helios.agents            // BuildAgents instance
helios.dashboard         // GUIDashboard instance

// DEPLOYMENT
await helios.deploy()
await helios.shutdown()

// STATUS
const status = helios.getSystemStatus()

// METRICS
const metrics = helios.getMetrics()

// EXAMPLE WORKFLOW
async function deploy() {
  await helios.initialize();
  
  // Security
  helios.security.addAppLockRule({ path: 'C:\\Program Files' });
  
  // Patterns
  helios.patterns.learnPattern({ type: 'production', cpu: 80 });
  
  // AI
  helios.ai.scheduleTask({ name: 'optimize', priority: 'high' });
  
  // Get status
  console.log(helios.getSystemStatus());
  
  // Deploy
  await helios.deploy();
}

deploy().catch(console.error);
```

---

## ERROR HANDLING PATTERNS

### Try-Catch
```javascript
try {
  security.addAppLockRule({ path: 'invalid' });
} catch (error) {
  console.error('Operation failed:', error.message);
}
```

### Event-Based
```javascript
security.on('error', ({ action, error }) => {
  console.error(`${action} failed: ${error.message}`);
});
```

### Promise-Based
```javascript
usb.installTool('node.js')
  .then(result => console.log('Success'))
  .catch(error => console.error('Failed:', error.message));
```

---

## BEST PRACTICES

1. **Always validate input** - Let Validator do the work
2. **Handle events** - Don't just use return values
3. **Use error events** - Catch all errors, not just exceptions
4. **Log important operations** - Use logger at each module
5. **Check metrics** - Monitor module health
6. **Use retry handler** - For operations that might fail
7. **Cache results** - Avoid unnecessary recomputation
8. **Event-driven** - Build loose coupling with events

---

## PERFORMANCE TIPS

1. **Parallel execution** - Use `executeParallel` for Build Agents
2. **Resource limits** - Set `maxConcurrentTasks` appropriately
3. **Caching** - Profiles are cached for 1 hour
4. **Batch operations** - USB `installAll` is more efficient
5. **Async operations** - Use `await` for better performance
6. **Circuit breaker** - Prevents cascading failures

---

## TESTING

```javascript
// Run all tests
node run-tests.js

// Test output includes:
// ✅ Pass/Fail status
// 📊 Success rate
// ⏱️ Performance metrics
// 📝 Detailed error messages
```

---

## DEPLOYMENT CHECKLIST

- [ ] All modules initialized
- [ ] Error handlers registered
- [ ] Event listeners attached
- [ ] Logging enabled
- [ ] Metrics collecting
- [ ] Tests passing (40+)
- [ ] Validation working
- [ ] Caching active
- [ ] Retry logic enabled
- [ ] Dashboard rendering

---

## SUPPORT & DOCUMENTATION

- **GitHub:** https://github.com/M0nado/helios-platform
- **Issues:** Report via GitHub Issues
- **PR:** Submit changes via pull requests
- **Discussions:** Use GitHub Discussions

---

**All APIs are production-ready and fully tested! 🚀**

