/**
 * Core types and interfaces for the Monado Blade monitoring system
 */

export enum CircuitBreakerState {
  CLOSED = 'CLOSED',
  OPEN = 'OPEN',
  HALF_OPEN = 'HALF_OPEN'
}

export enum HealthStatus {
  HEALTHY = 'HEALTHY',
  DEGRADED = 'DEGRADED',
  UNHEALTHY = 'UNHEALTHY'
}

export enum DegradationMode {
  NORMAL = 'NORMAL',
  REDUCED = 'REDUCED',
  CRITICAL = 'CRITICAL'
}

export interface HealthCheckResult {
  providerId: string;
  timestamp: number;
  status: HealthStatus;
  latency: number;
  errorRate: number;
  successRate: number;
  checksDone: number;
  error?: string;
}

export interface ProviderHealth {
  providerId: string;
  currentStatus: HealthStatus;
  circuitBreakerState: CircuitBreakerState;
  lastCheckTime: number;
  consecutiveFailures: number;
  consecutiveSuccesses: number;
  latency: {
    min: number;
    max: number;
    avg: number;
  };
  errorRate: number;
  availability: number;
  isActive: boolean;
  degradationMode: DegradationMode;
}

export interface CircuitBreakerConfig {
  failureThreshold: number;
  successThreshold: number;
  timeout: number;
  initialBackoff: number;
  maxBackoff: number;
}

export interface HealthCheckConfig {
  interval: number;
  timeout: number;
  adaptiveInterval: boolean;
  degradingIntervalMultiplier: number;
  minInterval: number;
  maxInterval: number;
}

export interface FailoverConfig {
  enabled: boolean;
  maxConcurrentFailovers: number;
  failoverDelay: number;
  cascadePreventionDelay: number;
}

export interface DegradationConfig {
  enabledAt: number;
  timeoutMultiplier: number;
  maxTimeoutMs: number;
  shedNonCriticalTasks: boolean;
}

export interface MetricsSnapshot {
  timestamp: number;
  providers: Map<string, ProviderHealth>;
  systemDegradationMode: DegradationMode;
  activatedFailovers: number;
  recoveredProviders: number;
}

export interface MetricsExport {
  timestamp: number;
  providers: ProviderMetrics[];
  summary: MetricsSummary;
}

export interface ProviderMetrics {
  providerId: string;
  status: string;
  circuitBreakerState: string;
  latencyMs: number;
  errorRate: number;
  availability: number;
  consecutiveFailures: number;
  consecutiveSuccesses: number;
}

export interface MetricsSummary {
  healthyProviders: number;
  degradedProviders: number;
  unhealthyProviders: number;
  systemAvailability: number;
  averageLatency: number;
  averageErrorRate: number;
}

export interface CircuitBreakerStateChange {
  providerId: string;
  oldState: CircuitBreakerState;
  newState: CircuitBreakerState;
  timestamp: number;
  reason: string;
}

export interface FailoverEvent {
  timestamp: number;
  fromProviderId: string;
  toProviderId: string;
  reason: string;
  success: boolean;
}

export interface HealthCheckScheduleConfig {
  baseInterval: number;
  adaptiveScaling: boolean;
  concurrentChecks: number;
  historySize: number;
}

export interface ProviderAdapter {
  providerId: string;
  executeHealthCheck(): Promise<HealthCheckResult>;
  isAvailable(): Promise<boolean>;
}

export interface ProviderRegistry {
  getProvider(id: string): ProviderAdapter | null;
  getAllProviders(): ProviderAdapter[];
  registerProvider(provider: ProviderAdapter): void;
  unregisterProvider(id: string): void;
}

export interface SmartRouter {
  getHealthyProvider(): ProviderAdapter | null;
  getNextProvider(current: ProviderAdapter): ProviderAdapter | null;
  route(request: unknown): Promise<unknown>;
}
