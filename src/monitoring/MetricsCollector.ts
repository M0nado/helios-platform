/**
 * Metrics Collector
 * Collects and aggregates health metrics from providers
 * Generates dashboards and exports metrics in Prometheus format
 */

import {
  MetricsSnapshot,
  MetricsExport,
  ProviderMetrics,
  MetricsSummary,
  ProviderHealth,
  HealthCheckResult,
  DegradationMode,
  CircuitBreakerState,
  HealthStatus
} from './types';

export class MetricsCollector {
  private providerMetrics: Map<string, ProviderHealth> = new Map();
  private metricsHistory: MetricsSnapshot[] = [];
  private readonly maxHistorySize: number;
  private systemDegradationMode: DegradationMode = DegradationMode.NORMAL;
  private activatedFailovers: number = 0;
  private recoveredProviders: number = 0;

  constructor(maxHistorySize: number = 1440) {
    this.maxHistorySize = maxHistorySize;
  }

  /**
   * Initialize metrics for a provider
   */
  initializeProvider(providerId: string): void {
    if (!this.providerMetrics.has(providerId)) {
      this.providerMetrics.set(providerId, {
        providerId,
        currentStatus: HealthStatus.HEALTHY,
        circuitBreakerState: CircuitBreakerState.CLOSED,
        lastCheckTime: 0,
        consecutiveFailures: 0,
        consecutiveSuccesses: 0,
        latency: {
          min: Number.MAX_VALUE,
          max: 0,
          avg: 0
        },
        errorRate: 0,
        availability: 100,
        isActive: true,
        degradationMode: DegradationMode.NORMAL
      });
    }
  }

  /**
   * Record a health check result
   */
  recordHealthCheckResult(result: HealthCheckResult): void {
    this.initializeProvider(result.providerId);
    const metrics = this.providerMetrics.get(result.providerId)!;

    metrics.lastCheckTime = result.timestamp;
    metrics.currentStatus = result.status;
    metrics.errorRate = result.errorRate;

    // Update latency metrics
    if (result.latency > 0) {
      metrics.latency.min = Math.min(metrics.latency.min, result.latency);
      metrics.latency.max = Math.max(metrics.latency.max, result.latency);
      metrics.latency.avg = (metrics.latency.avg + result.latency) / 2;
    }

    // Update availability
    if (result.successRate > 0) {
      metrics.availability = result.successRate * 100;
    }

    // Update consecutive success/failure counts
    if (result.status === HealthStatus.HEALTHY) {
      metrics.consecutiveSuccesses++;
      metrics.consecutiveFailures = 0;
    } else {
      metrics.consecutiveFailures++;
      metrics.consecutiveSuccesses = 0;
    }
  }

  /**
   * Update circuit breaker state for a provider
   */
  updateCircuitBreakerState(providerId: string, state: CircuitBreakerState): void {
    this.initializeProvider(providerId);
    const metrics = this.providerMetrics.get(providerId)!;
    metrics.circuitBreakerState = state;

    if (state === CircuitBreakerState.OPEN) {
      this.activatedFailovers++;
    } else if (state === CircuitBreakerState.CLOSED) {
      this.recoveredProviders++;
    }
  }

  /**
   * Update degradation mode for a provider
   */
  updateDegradationMode(providerId: string, mode: DegradationMode): void {
    this.initializeProvider(providerId);
    const metrics = this.providerMetrics.get(providerId)!;
    metrics.degradationMode = mode;
  }

  /**
   * Update system degradation mode
   */
  setSystemDegradationMode(mode: DegradationMode): void {
    this.systemDegradationMode = mode;
  }

  /**
   * Get current metrics snapshot
   */
  getCurrentSnapshot(): MetricsSnapshot {
    return {
      timestamp: Date.now(),
      providers: new Map(this.providerMetrics),
      systemDegradationMode: this.systemDegradationMode,
      activatedFailovers: this.activatedFailovers,
      recoveredProviders: this.recoveredProviders
    };
  }

  /**
   * Record a metrics snapshot
   */
  recordSnapshot(): void {
    const snapshot = this.getCurrentSnapshot();
    this.metricsHistory.push(snapshot);

    if (this.metricsHistory.length > this.maxHistorySize) {
      this.metricsHistory.shift();
    }
  }

  /**
   * Get metrics history
   */
  getHistory(maxAge: number = 3600000): MetricsSnapshot[] {
    const cutoffTime = Date.now() - maxAge;
    return this.metricsHistory.filter(snapshot => snapshot.timestamp >= cutoffTime);
  }

  /**
   * Get metrics export in Prometheus format
   */
  exportMetrics(): MetricsExport {
    const providers: ProviderMetrics[] = [];

    for (const [, health] of this.providerMetrics) {
      providers.push({
        providerId: health.providerId,
        status: health.currentStatus,
        circuitBreakerState: health.circuitBreakerState,
        latencyMs: health.latency.avg,
        errorRate: health.errorRate,
        availability: health.availability,
        consecutiveFailures: health.consecutiveFailures,
        consecutiveSuccesses: health.consecutiveSuccesses
      });
    }

    const summary = this.calculateSummary();

    return {
      timestamp: Date.now(),
      providers,
      summary
    };
  }

  /**
   * Export metrics as Prometheus text format
   */
  exportPrometheusMetrics(): string {
    let output = '# HELP monado_blade_provider_health Provider health status\n';
    output += '# TYPE monado_blade_provider_health gauge\n';

    for (const [providerId, health] of this.providerMetrics) {
      const statusValue = health.currentStatus === HealthStatus.HEALTHY ? 1 : 0;
      output += `monado_blade_provider_health{provider="${providerId}"} ${statusValue}\n`;

      output += `monado_blade_provider_latency_ms{provider="${providerId}"} ${health.latency.avg}\n`;
      output += `monado_blade_provider_error_rate{provider="${providerId}"} ${health.errorRate}\n`;
      output += `monado_blade_provider_availability{provider="${providerId}"} ${health.availability}\n`;
      output += `monado_blade_provider_consecutive_failures{provider="${providerId}"} ${health.consecutiveFailures}\n`;
      output += `monado_blade_provider_consecutive_successes{provider="${providerId}"} ${health.consecutiveSuccesses}\n`;

      const cbStateValue = health.circuitBreakerState === CircuitBreakerState.CLOSED ? 0 :
        health.circuitBreakerState === CircuitBreakerState.HALF_OPEN ? 1 : 2;
      output += `monado_blade_circuit_breaker_state{provider="${providerId}"} ${cbStateValue}\n`;
    }

    output += '\n# HELP monado_blade_system_degradation_mode System degradation mode\n';
    output += '# TYPE monado_blade_system_degradation_mode gauge\n';
    const modeValue = this.systemDegradationMode === DegradationMode.NORMAL ? 0 :
      this.systemDegradationMode === DegradationMode.REDUCED ? 1 : 2;
    output += `monado_blade_system_degradation_mode ${modeValue}\n`;

    output += '\n# HELP monado_blade_activated_failovers Total activated failovers\n';
    output += '# TYPE monado_blade_activated_failovers counter\n';
    output += `monado_blade_activated_failovers ${this.activatedFailovers}\n`;

    output += '\n# HELP monado_blade_recovered_providers Total recovered providers\n';
    output += '# TYPE monado_blade_recovered_providers counter\n';
    output += `monado_blade_recovered_providers ${this.recoveredProviders}\n`;

    return output;
  }

  /**
   * Calculate summary metrics
   */
  calculateSummary(): MetricsSummary {
    let healthyCount = 0;
    let degradedCount = 0;
    let unhealthyCount = 0;
    let totalLatency = 0;
    let totalErrorRate = 0;
    let totalAvailability = 0;

    for (const health of this.providerMetrics.values()) {
      if (health.currentStatus === HealthStatus.HEALTHY) {
        healthyCount++;
      } else if (health.currentStatus === HealthStatus.DEGRADED) {
        degradedCount++;
      } else {
        unhealthyCount++;
      }

      totalLatency += health.latency.avg;
      totalErrorRate += health.errorRate;
      totalAvailability += health.availability;
    }

    const providerCount = this.providerMetrics.size;

    return {
      healthyProviders: healthyCount,
      degradedProviders: degradedCount,
      unhealthyProviders: unhealthyCount,
      systemAvailability: providerCount > 0 ? totalAvailability / providerCount : 0,
      averageLatency: providerCount > 0 ? totalLatency / providerCount : 0,
      averageErrorRate: providerCount > 0 ? totalErrorRate / providerCount : 0
    };
  }

  /**
   * Get metrics for a specific provider
   */
  getProviderMetrics(providerId: string): ProviderHealth | null {
    return this.providerMetrics.get(providerId) ?? null;
  }

  /**
   * Get all provider metrics
   */
  getAllMetrics(): ProviderHealth[] {
    return Array.from(this.providerMetrics.values());
  }

  /**
   * Get dashboard data
   */
  getDashboardData(): {
    snapshot: MetricsSnapshot;
    summary: MetricsSummary;
    healthTrends: Map<string, number[]>;
    latencyTrends: Map<string, number[]>;
  } {
    const snapshot = this.getCurrentSnapshot();
    const summary = this.calculateSummary();

    // Calculate trends from history
    const healthTrends = new Map<string, number[]>();
    const latencyTrends = new Map<string, number[]>();

    const recentHistory = this.metricsHistory.slice(-60);
    for (const snap of recentHistory) {
      for (const [providerId, health] of snap.providers) {
        if (!healthTrends.has(providerId)) {
          healthTrends.set(providerId, []);
          latencyTrends.set(providerId, []);
        }

        const healthValue = health.currentStatus === HealthStatus.HEALTHY ? 1 : 0;
        healthTrends.get(providerId)!.push(healthValue);
        latencyTrends.get(providerId)!.push(health.latency.avg);
      }
    }

    return {
      snapshot,
      summary,
      healthTrends,
      latencyTrends
    };
  }

  /**
   * Reset all metrics
   */
  reset(): void {
    this.providerMetrics.clear();
    this.metricsHistory = [];
    this.systemDegradationMode = DegradationMode.NORMAL;
    this.activatedFailovers = 0;
    this.recoveredProviders = 0;
  }

  /**
   * Get statistics
   */
  getStatistics(): {
    totalProviders: number;
    metricsAge: number;
    historySize: number;
  } {
    const oldestSnapshot = this.metricsHistory[0];
    const metricsAge = oldestSnapshot
      ? Date.now() - oldestSnapshot.timestamp
      : 0;

    return {
      totalProviders: this.providerMetrics.size,
      metricsAge,
      historySize: this.metricsHistory.length
    };
  }
}
