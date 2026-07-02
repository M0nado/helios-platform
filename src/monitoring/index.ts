/**
 * Monado Blade Monitoring System - Main Export
 * Phase 4D: Failover Monitor & Health Monitoring System
 */

export { CircuitBreaker } from './CircuitBreaker';
export { HealthCheckScheduler } from './HealthCheckScheduler';
export { FailoverController } from './FailoverController';
export { MetricsCollector } from './MetricsCollector';
export { GracefulDegradationEngine } from './GracefulDegradationEngine';

export type {
  CircuitBreakerState,
  HealthStatus,
  DegradationMode,
  HealthCheckResult,
  ProviderHealth,
  CircuitBreakerConfig,
  HealthCheckConfig,
  FailoverConfig,
  DegradationConfig,
  MetricsSnapshot,
  MetricsExport,
  ProviderMetrics,
  MetricsSummary,
  CircuitBreakerStateChange,
  FailoverEvent,
  HealthCheckScheduleConfig,
  ProviderAdapter,
  ProviderRegistry,
  SmartRouter
} from './types';
