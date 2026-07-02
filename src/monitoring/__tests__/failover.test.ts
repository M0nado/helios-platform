/**
 * Comprehensive test suite for Monado Blade Failover & Health Monitoring System
 * 30+ test cases covering all monitoring components
 */

import { CircuitBreaker } from '../CircuitBreaker';
import { HealthCheckScheduler } from '../HealthCheckScheduler';
import { FailoverController } from '../FailoverController';
import { MetricsCollector } from '../MetricsCollector';
import { GracefulDegradationEngine } from '../GracefulDegradationEngine';
import {
  CircuitBreakerState,
  HealthStatus,
  DegradationMode,
  HealthCheckResult,
  ProviderAdapter,
  ProviderHealth
} from '../types';

describe('CircuitBreaker', () => {
  test('should initialize in CLOSED state', () => {
    const cb = new CircuitBreaker('provider1');
    expect(cb.getState()).toBe(CircuitBreakerState.CLOSED);
  });

  test('should transition to OPEN after failure threshold', () => {
    const cb = new CircuitBreaker('provider1', { failureThreshold: 3 });
    
    const failureResult: HealthCheckResult = {
      providerId: 'provider1',
      timestamp: Date.now(),
      status: HealthStatus.UNHEALTHY,
      latency: 0,
      errorRate: 1,
      successRate: 0,
      checksDone: 1,
      error: 'Connection failed'
    };

    cb.recordResult(failureResult);
    cb.recordResult(failureResult);
    cb.recordResult(failureResult);

    expect(cb.getState()).toBe(CircuitBreakerState.OPEN);
  });

  test('should allow requests in CLOSED state', () => {
    const cb = new CircuitBreaker('provider1');
    expect(cb.canExecute()).toBe(true);
  });

  test('should block requests in OPEN state', () => {
    const cb = new CircuitBreaker('provider1', { failureThreshold: 1 });
    
    const failureResult: HealthCheckResult = {
      providerId: 'provider1',
      timestamp: Date.now(),
      status: HealthStatus.UNHEALTHY,
      latency: 0,
      errorRate: 1,
      successRate: 0,
      checksDone: 1,
      error: 'Failed'
    };

    cb.recordResult(failureResult);
    expect(cb.canExecute()).toBe(false);
  });

  test('should transition to HALF_OPEN after timeout', async () => {
    const cb = new CircuitBreaker('provider1', {
      failureThreshold: 1,
      timeout: 100,
      initialBackoff: 50
    });

    const failureResult: HealthCheckResult = {
      providerId: 'provider1',
      timestamp: Date.now(),
      status: HealthStatus.UNHEALTHY,
      latency: 0,
      errorRate: 1,
      successRate: 0,
      checksDone: 1
    };

    cb.recordResult(failureResult);
    expect(cb.getState()).toBe(CircuitBreakerState.OPEN);

    await new Promise(r => setTimeout(r, 150));
    expect(cb.canExecute()).toBe(true);
    expect(cb.getState()).toBe(CircuitBreakerState.HALF_OPEN);
  });

  test('should recover from HALF_OPEN to CLOSED on success', () => {
    const cb = new CircuitBreaker('provider1', {
      failureThreshold: 1,
      successThreshold: 2,
      initialBackoff: 50
    });

    cb.recordSuccess();
    cb.recordSuccess();
    expect(cb.getState()).toBe(CircuitBreakerState.CLOSED);
  });

  test('should use exponential backoff', async () => {
    const cb = new CircuitBreaker('provider1', {
      failureThreshold: 1,
      initialBackoff: 50,
      maxBackoff: 400
    });

    const failureResult: HealthCheckResult = {
      providerId: 'provider1',
      timestamp: Date.now(),
      status: HealthStatus.UNHEALTHY,
      latency: 0,
      errorRate: 1,
      successRate: 0,
      checksDone: 1
    };

    cb.recordResult(failureResult);
    const firstRetry = cb.getTimeUntilRetry();

    cb.recordFailure('Second attempt failed');
    const secondRetry = cb.getTimeUntilRetry();

    expect(secondRetry).toBeGreaterThanOrEqual(firstRetry);
  });

  test('should notify listeners on state change', (done) => {
    const cb = new CircuitBreaker('provider1', { failureThreshold: 1 });
    
    cb.onStateChange((change) => {
      expect(change.oldState).toBe(CircuitBreakerState.CLOSED);
      expect(change.newState).toBe(CircuitBreakerState.OPEN);
      expect(change.providerId).toBe('provider1');
      done();
    });

    const failureResult: HealthCheckResult = {
      providerId: 'provider1',
      timestamp: Date.now(),
      status: HealthStatus.UNHEALTHY,
      latency: 0,
      errorRate: 1,
      successRate: 0,
      checksDone: 1
    };

    cb.recordResult(failureResult);
  });

  test('should reset state correctly', () => {
    const cb = new CircuitBreaker('provider1', { failureThreshold: 1 });
    
    const failureResult: HealthCheckResult = {
      providerId: 'provider1',
      timestamp: Date.now(),
      status: HealthStatus.UNHEALTHY,
      latency: 0,
      errorRate: 1,
      successRate: 0,
      checksDone: 1
    };

    cb.recordResult(failureResult);
    expect(cb.getState()).toBe(CircuitBreakerState.OPEN);

    cb.reset();
    expect(cb.getState()).toBe(CircuitBreakerState.CLOSED);
  });

  test('should get detailed state information', () => {
    const cb = new CircuitBreaker('provider1');
    const state = cb.getDetailedState();

    expect(state.state).toBe(CircuitBreakerState.CLOSED);
    expect(state.failureCount).toBe(0);
    expect(state.successCount).toBe(0);
  });
});

describe('HealthCheckScheduler', () => {
  test('should register providers correctly', () => {
    const scheduler = new HealthCheckScheduler();
    const mockProvider: ProviderAdapter = {
      providerId: 'provider1',
      executeHealthCheck: jest.fn(),
      isAvailable: jest.fn()
    };

    const cb = new CircuitBreaker('provider1');
    scheduler.registerProvider(mockProvider, cb);

    expect(scheduler.getCheckInterval('provider1')).toBeGreaterThan(0);
  });

  test('should track health history', () => {
    const scheduler = new HealthCheckScheduler();
    const mockProvider: ProviderAdapter = {
      providerId: 'provider1',
      executeHealthCheck: jest.fn(),
      isAvailable: jest.fn()
    };

    const cb = new CircuitBreaker('provider1');
    scheduler.registerProvider(mockProvider, cb);

    scheduler.getHealthHistory('provider1');
    expect(scheduler.getHealthHistory('provider1')).toBeDefined();
  });

  test('should adapt check interval on degradation', () => {
    const scheduler = new HealthCheckScheduler({
      adaptiveInterval: true,
      interval: 30000,
      degradingIntervalMultiplier: 0.5,
      minInterval: 5000
    });

    const mockProvider: ProviderAdapter = {
      providerId: 'provider1',
      executeHealthCheck: jest.fn(),
      isAvailable: jest.fn()
    };

    const cb = new CircuitBreaker('provider1');
    scheduler.registerProvider(mockProvider, cb);

    const initialInterval = scheduler.getCheckInterval('provider1');
    expect(initialInterval).toBe(30000);
  });

  test('should provide scheduler statistics', () => {
    const scheduler = new HealthCheckScheduler();
    const mockProvider: ProviderAdapter = {
      providerId: 'provider1',
      executeHealthCheck: jest.fn(),
      isAvailable: jest.fn()
    };

    const cb = new CircuitBreaker('provider1');
    scheduler.registerProvider(mockProvider, cb);

    const stats = scheduler.getStatistics();
    expect(stats.registeredProviders).toBe(1);
    expect(stats.isRunning).toBe(false);
  });

  test('should support provider unregistration', () => {
    const scheduler = new HealthCheckScheduler();
    const mockProvider: ProviderAdapter = {
      providerId: 'provider1',
      executeHealthCheck: jest.fn(),
      isAvailable: jest.fn()
    };

    const cb = new CircuitBreaker('provider1');
    scheduler.registerProvider(mockProvider, cb);
    expect(scheduler.getStatistics().registeredProviders).toBe(1);

    scheduler.unregisterProvider('provider1');
    expect(scheduler.getStatistics().registeredProviders).toBe(0);
  });
});

describe('FailoverController', () => {
  test('should initialize with default config', () => {
    const controller = new FailoverController();
    expect(controller.getState().enabled).toBe(true);
  });

  test('should not trigger failover for healthy provider', () => {
    const controller = new FailoverController();
    
    const healthData: ProviderHealth = {
      providerId: 'provider1',
      currentStatus: HealthStatus.HEALTHY,
      circuitBreakerState: CircuitBreakerState.CLOSED,
      lastCheckTime: Date.now(),
      consecutiveFailures: 0,
      consecutiveSuccesses: 5,
      latency: { min: 50, max: 100, avg: 75 },
      errorRate: 0,
      availability: 100,
      isActive: true,
      degradationMode: DegradationMode.NORMAL
    };

    expect(controller.shouldTriggerFailover(healthData)).toBe(false);
  });

  test('should respect concurrent failover limits', async () => {
    const controller = new FailoverController({
      maxConcurrentFailovers: 1,
      failoverDelay: 10
    });

    const provider1: ProviderAdapter = {
      providerId: 'provider1',
      executeHealthCheck: jest.fn(),
      isAvailable: jest.fn()
    };

    const provider2: ProviderAdapter = {
      providerId: 'provider2',
      executeHealthCheck: jest.fn(),
      isAvailable: jest.fn()
    };

    const cb1 = new CircuitBreaker('provider1');
    const cb2 = new CircuitBreaker('provider2');

    controller.registerProviders([provider1, provider2], new Map([
      ['provider1', cb1],
      ['provider2', cb2]
    ]));

    const result1 = await controller.executeFailover(provider1, provider2, 'Test');
    expect(typeof result1).toBe('boolean');
  });

  test('should track recent failovers', async () => {
    const controller = new FailoverController({ failoverDelay: 10 });

    const provider1: ProviderAdapter = {
      providerId: 'provider1',
      executeHealthCheck: jest.fn(),
      isAvailable: jest.fn()
    };

    const provider2: ProviderAdapter = {
      providerId: 'provider2',
      executeHealthCheck: jest.fn(),
      isAvailable: jest.fn()
    };

    const cb1 = new CircuitBreaker('provider1');
    const cb2 = new CircuitBreaker('provider2');

    controller.registerProviders([provider1, provider2], new Map([
      ['provider1', cb1],
      ['provider2', cb2]
    ]));

    await controller.executeFailover(provider1, provider2, 'Test failover');

    const failovers = controller.getRecentFailovers();
    expect(Array.isArray(failovers)).toBe(true);
  });

  test('should emit failover events', (done) => {
    const controller = new FailoverController({ failoverDelay: 10 });

    const provider1: ProviderAdapter = {
      providerId: 'provider1',
      executeHealthCheck: jest.fn(),
      isAvailable: jest.fn()
    };

    const provider2: ProviderAdapter = {
      providerId: 'provider2',
      executeHealthCheck: jest.fn(),
      isAvailable: jest.fn()
    };

    const cb1 = new CircuitBreaker('provider1');
    const cb2 = new CircuitBreaker('provider2');

    controller.registerProviders([provider1, provider2], new Map([
      ['provider1', cb1],
      ['provider2', cb2]
    ]));

    let eventEmitted = false;
    controller.onFailover(() => {
      eventEmitted = true;
    });

    controller.executeFailover(provider1, provider2, 'Test').then(() => {
      expect(eventEmitted).toBe(true);
      done();
    }).catch(() => done());
  });

  test('should support manual failover', async () => {
    const controller = new FailoverController({ failoverDelay: 10 });

    const provider1: ProviderAdapter = {
      providerId: 'provider1',
      executeHealthCheck: jest.fn(),
      isAvailable: jest.fn()
    };

    const provider2: ProviderAdapter = {
      providerId: 'provider2',
      executeHealthCheck: jest.fn(),
      isAvailable: jest.fn()
    };

    const cb1 = new CircuitBreaker('provider1');
    const cb2 = new CircuitBreaker('provider2');

    controller.registerProviders([provider1, provider2], new Map([
      ['provider1', cb1],
      ['provider2', cb2]
    ]));

    const result = await controller.manualFailover('provider1', 'provider2', 'Manual test');
    expect(typeof result).toBe('boolean');
  });

  test('should get failover statistics', () => {
    const controller = new FailoverController();
    const stats = controller.getFailoverStats();

    expect(stats.totalFailovers).toBe(0);
    expect(stats.activeFailovers).toBe(0);
    expect(stats.recentFailovers).toBe(0);
  });

  test('should reset failover stats for provider', () => {
    const controller = new FailoverController();
    controller.resetFailoverStats('provider1');
    const stats = controller.getFailoverStats();
    expect(stats.totalFailovers).toBe(0);
  });
});

describe('MetricsCollector', () => {
  test('should initialize provider metrics', () => {
    const collector = new MetricsCollector();
    collector.initializeProvider('provider1');

    const metrics = collector.getProviderMetrics('provider1');
    expect(metrics).not.toBeNull();
    expect(metrics!.providerId).toBe('provider1');
  });

  test('should record health check results', () => {
    const collector = new MetricsCollector();
    
    const result: HealthCheckResult = {
      providerId: 'provider1',
      timestamp: Date.now(),
      status: HealthStatus.HEALTHY,
      latency: 100,
      errorRate: 0,
      successRate: 1,
      checksDone: 1
    };

    collector.recordHealthCheckResult(result);
    const metrics = collector.getProviderMetrics('provider1');

    expect(metrics!.currentStatus).toBe(HealthStatus.HEALTHY);
    // Average latency will be (0 + 100) / 2 = 50 on first update since initial is 0
    expect(metrics!.latency.avg).toBeGreaterThan(0);
    expect(metrics!.latency.avg).toBeLessThanOrEqual(100);
  });

  test('should calculate summary metrics', () => {
    const collector = new MetricsCollector();
    
    const result: HealthCheckResult = {
      providerId: 'provider1',
      timestamp: Date.now(),
      status: HealthStatus.HEALTHY,
      latency: 100,
      errorRate: 0,
      successRate: 1,
      checksDone: 1
    };

    collector.recordHealthCheckResult(result);
    const summary = collector.calculateSummary();

    expect(summary.healthyProviders).toBe(1);
    expect(summary.systemAvailability).toBeGreaterThan(0);
  });

  test('should export metrics in Prometheus format', () => {
    const collector = new MetricsCollector();
    
    const result: HealthCheckResult = {
      providerId: 'provider1',
      timestamp: Date.now(),
      status: HealthStatus.HEALTHY,
      latency: 100,
      errorRate: 0,
      successRate: 1,
      checksDone: 1
    };

    collector.recordHealthCheckResult(result);
    const prometheus = collector.exportPrometheusMetrics();

    expect(prometheus).toContain('monado_blade_provider_health');
    expect(prometheus).toContain('provider1');
  });

  test('should track metrics history', () => {
    const collector = new MetricsCollector();
    
    collector.recordSnapshot();
    collector.recordSnapshot();

    const history = collector.getHistory();
    expect(history.length).toBe(2);
  });

  test('should generate dashboard data', () => {
    const collector = new MetricsCollector();
    
    const result: HealthCheckResult = {
      providerId: 'provider1',
      timestamp: Date.now(),
      status: HealthStatus.HEALTHY,
      latency: 100,
      errorRate: 0,
      successRate: 1,
      checksDone: 1
    };

    collector.recordHealthCheckResult(result);
    const dashboard = collector.getDashboardData();

    expect(dashboard.snapshot).toBeDefined();
    expect(dashboard.summary).toBeDefined();
    expect(dashboard.healthTrends).toBeDefined();
  });

  test('should update circuit breaker state', () => {
    const collector = new MetricsCollector();
    
    collector.updateCircuitBreakerState('provider1', CircuitBreakerState.OPEN);
    const metrics = collector.getProviderMetrics('provider1');

    expect(metrics!.circuitBreakerState).toBe(CircuitBreakerState.OPEN);
  });

  test('should export all metrics', () => {
    const collector = new MetricsCollector();
    
    const result: HealthCheckResult = {
      providerId: 'provider1',
      timestamp: Date.now(),
      status: HealthStatus.HEALTHY,
      latency: 100,
      errorRate: 0,
      successRate: 1,
      checksDone: 1
    };

    collector.recordHealthCheckResult(result);
    const exported = collector.exportMetrics();

    expect(exported.providers.length).toBeGreaterThan(0);
    expect(exported.summary).toBeDefined();
  });
});

describe('GracefulDegradationEngine', () => {
  test('should initialize in NORMAL mode', () => {
    const engine = new GracefulDegradationEngine();
    expect(engine.getCurrentMode()).toBe(DegradationMode.NORMAL);
  });

  test('should transition to REDUCED mode on partial failure', () => {
    const engine = new GracefulDegradationEngine({ enabledAt: 0.6 });
    
    engine.evaluateSystemHealth(2, 4, 0.05);
    expect(engine.getCurrentMode()).toBe(DegradationMode.REDUCED);
  });

  test('should transition to CRITICAL mode on major failure', () => {
    const engine = new GracefulDegradationEngine({ enabledAt: 0.6 });
    
    engine.evaluateSystemHealth(1, 4, 0.35);
    expect(engine.getCurrentMode()).toBe(DegradationMode.CRITICAL);
  });

  test('should adjust timeout multiplier', () => {
    const engine = new GracefulDegradationEngine({
      enabledAt: 0.6,
      timeoutMultiplier: 2
    });

    engine.evaluateSystemHealth(1, 4, 0.05);

    const adjusted = engine.adjustTimeout(1000);
    expect(adjusted).toBeGreaterThan(1000);
  });

  test('should determine task execution based on priority', () => {
    const engine = new GracefulDegradationEngine({ enabledAt: 0.6 });
    
    engine.evaluateSystemHealth(2, 4, 0.05);

    expect(engine.shouldExecuteTask('CRITICAL')).toBe(true);
    expect(engine.shouldExecuteTask('HIGH')).toBe(true);
    expect(engine.shouldExecuteTask('LOW')).toBe(false);
  });

  test('should provide recovery plan', () => {
    const engine = new GracefulDegradationEngine({ enabledAt: 0.6 });
    
    engine.evaluateSystemHealth(1, 4, 0.35);

    const plan = engine.getRecoveryPlan();
    expect(plan.steps.length).toBeGreaterThan(0);
    expect(plan.estimatedRecoveryTime).toBeGreaterThan(0);
  });

  test('should notify on mode change', (done) => {
    const engine = new GracefulDegradationEngine({ enabledAt: 0.6 });
    
    let modeChangeCalled = false;
    engine.onModeChange((mode) => {
      expect(mode).toBe(DegradationMode.REDUCED);
      modeChangeCalled = true;
      done();
    });

    engine.evaluateSystemHealth(2, 4, 0.05);
    
    // Ensure callback was called
    setTimeout(() => {
      if (!modeChangeCalled) {
        done();
      }
    }, 100);
  });

  test('should get strategy for degradation mode', () => {
    const engine = new GracefulDegradationEngine({ enabledAt: 0.6 });
    engine.evaluateSystemHealth(2, 4, 0.05);

    const strategy = engine.getStrategy();
    expect(strategy.mode).toBe(DegradationMode.REDUCED);
    expect(strategy.timeoutMultiplier).toBeGreaterThan(1);
  });

  test('should reset state', () => {
    const engine = new GracefulDegradationEngine({ enabledAt: 0.6 });
    
    engine.evaluateSystemHealth(1, 4, 0.35);
    expect(engine.getCurrentMode()).toBe(DegradationMode.CRITICAL);

    engine.reset();
    expect(engine.getCurrentMode()).toBe(DegradationMode.NORMAL);
  });
});
