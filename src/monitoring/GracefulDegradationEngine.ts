/**
 * Graceful Degradation Engine
 * Reduces service levels under load to maintain core functionality
 * Implements task prioritization and quality reduction strategies
 */

import {
  DegradationMode,
  DegradationConfig
} from './types';

export interface DegradationStrategy {
  mode: DegradationMode;
  timeoutMultiplier: number;
  shedNonCriticalTasks: boolean;
  qualityReduction: number;
}

export class GracefulDegradationEngine {
  private currentMode: DegradationMode = DegradationMode.NORMAL;
  private readonly config: DegradationConfig;
  private degradationStartTime: number = 0;
  private failedProviderCount: number = 0;
  private totalProviderCount: number = 0;
  private listeners: ((mode: DegradationMode) => void)[] = [];

  constructor(config: Partial<DegradationConfig> = {}) {
    this.config = {
      enabledAt: config.enabledAt ?? 0.5,
      timeoutMultiplier: config.timeoutMultiplier ?? 2,
      maxTimeoutMs: config.maxTimeoutMs ?? 30000,
      shedNonCriticalTasks: config.shedNonCriticalTasks ?? true
    };
  }

  /**
   * Evaluate system health and determine degradation mode
   */
  evaluateSystemHealth(
    healthyProviders: number,
    totalProviders: number,
    averageErrorRate: number
  ): DegradationMode {
    this.totalProviderCount = totalProviders;
    this.failedProviderCount = totalProviders - healthyProviders;

    const healthRatio = totalProviders > 0 ? healthyProviders / totalProviders : 1;

    let newMode: DegradationMode = DegradationMode.NORMAL;

    // Check provider health ratio first (most critical)
    if (healthRatio < this.config.enabledAt * 0.5) {
      newMode = DegradationMode.CRITICAL;
    } else if (healthRatio < this.config.enabledAt) {
      newMode = DegradationMode.REDUCED;
    }

    // Check error rate (can escalate but not downgrade from what health ratio set)
    if (averageErrorRate > 0.3) {
      newMode = DegradationMode.CRITICAL;
    } else if (averageErrorRate > 0.1 && newMode === DegradationMode.NORMAL) {
      newMode = DegradationMode.REDUCED;
    }

    this.updateDegradationMode(newMode);
    return newMode;
  }

  /**
   * Update current degradation mode
   */
  private updateDegradationMode(mode: DegradationMode): void {
    if (mode === this.currentMode) {
      return;
    }

    this.currentMode = mode;

    if (mode !== DegradationMode.NORMAL && this.degradationStartTime === 0) {
      this.degradationStartTime = Date.now();
    } else if (mode === DegradationMode.NORMAL) {
      this.degradationStartTime = 0;
    }

    this.notifyModeChange(mode);
  }

  /**
   * Get current degradation mode
   */
  getCurrentMode(): DegradationMode {
    return this.currentMode;
  }

  /**
   * Get degradation strategy for current mode
   */
  getStrategy(): DegradationStrategy {
    return {
      mode: this.currentMode,
      timeoutMultiplier: this.getTimeoutMultiplier(),
      shedNonCriticalTasks: this.shouldShedNonCriticalTasks(),
      qualityReduction: this.getQualityReduction()
    };
  }

  /**
   * Calculate timeout multiplier based on degradation mode
   */
  private getTimeoutMultiplier(): number {
    switch (this.currentMode) {
      case DegradationMode.NORMAL:
        return 1;
      case DegradationMode.REDUCED:
        return this.config.timeoutMultiplier;
      case DegradationMode.CRITICAL:
        return this.config.timeoutMultiplier * 2;
      default:
        return 1;
    }
  }

  /**
   * Check if non-critical tasks should be shed
   */
  private shouldShedNonCriticalTasks(): boolean {
    if (!this.config.shedNonCriticalTasks) {
      return false;
    }

    return this.currentMode === DegradationMode.REDUCED ||
           this.currentMode === DegradationMode.CRITICAL;
  }

  /**
   * Get quality reduction factor (0-1)
   */
  private getQualityReduction(): number {
    switch (this.currentMode) {
      case DegradationMode.NORMAL:
        return 0;
      case DegradationMode.REDUCED:
        return 0.2;
      case DegradationMode.CRITICAL:
        return 0.5;
      default:
        return 0;
    }
  }

  /**
   * Adjust timeout based on degradation mode
   */
  adjustTimeout(baseTimeout: number): number {
    const multiplier = this.getTimeoutMultiplier();
    const adjusted = baseTimeout * multiplier;
    return Math.min(adjusted, this.config.maxTimeoutMs);
  }

  /**
   * Determine if a task should be executed
   */
  shouldExecuteTask(taskPriority: 'CRITICAL' | 'HIGH' | 'NORMAL' | 'LOW'): boolean {
    if (this.currentMode === DegradationMode.NORMAL) {
      return true;
    }

    if (this.currentMode === DegradationMode.REDUCED) {
      return taskPriority === 'CRITICAL' || taskPriority === 'HIGH';
    }

    if (this.currentMode === DegradationMode.CRITICAL) {
      return taskPriority === 'CRITICAL';
    }

    return true;
  }

  /**
   * Get duration of current degradation (in milliseconds)
   */
  getDegradationDuration(): number {
    if (this.currentMode === DegradationMode.NORMAL || this.degradationStartTime === 0) {
      return 0;
    }

    return Date.now() - this.degradationStartTime;
  }

  /**
   * Get system health status
   */
  getHealthStatus(): {
    mode: DegradationMode;
    healthRatio: number;
    failedProviders: number;
    totalProviders: number;
    duration: number;
    avgErrorRate: number;
  } {
    const healthRatio = this.totalProviderCount > 0
      ? (this.totalProviderCount - this.failedProviderCount) / this.totalProviderCount
      : 1;

    return {
      mode: this.currentMode,
      healthRatio,
      failedProviders: this.failedProviderCount,
      totalProviders: this.totalProviderCount,
      duration: this.getDegradationDuration(),
      avgErrorRate: 0
    };
  }

  /**
   * Recovery plan when transitioning out of degradation
   */
  getRecoveryPlan(): {
    steps: string[];
    estimatedRecoveryTime: number;
  } {
    const steps: string[] = [];
    let estimatedTime = 0;

    if (this.currentMode === DegradationMode.CRITICAL) {
      steps.push('Restore primary provider connection');
      steps.push('Increase timeout values gradually');
      steps.push('Re-enable non-critical task processing');
      steps.push('Monitor error rates for stability');
      estimatedTime = 60000;
    } else if (this.currentMode === DegradationMode.REDUCED) {
      steps.push('Monitor system stability');
      steps.push('Gradually reduce timeout multipliers');
      steps.push('Resume normal task priorities');
      estimatedTime = 30000;
    }

    return {
      steps,
      estimatedRecoveryTime: estimatedTime
    };
  }

  /**
   * Subscribe to degradation mode changes
   */
  onModeChange(listener: (mode: DegradationMode) => void): void {
    this.listeners.push(listener);
  }

  /**
   * Notify listeners of mode change
   */
  private notifyModeChange(mode: DegradationMode): void {
    for (const listener of this.listeners) {
      listener(mode);
    }
  }

  /**
   * Get detailed state information
   */
  getState(): {
    currentMode: DegradationMode;
    strategy: DegradationStrategy;
    healthStatus: ReturnType<GracefulDegradationEngine['getHealthStatus']>;
    recoveryPlan: ReturnType<GracefulDegradationEngine['getRecoveryPlan']>;
  } {
    return {
      currentMode: this.currentMode,
      strategy: this.getStrategy(),
      healthStatus: this.getHealthStatus(),
      recoveryPlan: this.getRecoveryPlan()
    };
  }

  /**
   * Reset degradation state
   */
  reset(): void {
    this.currentMode = DegradationMode.NORMAL;
    this.degradationStartTime = 0;
    this.failedProviderCount = 0;
    this.totalProviderCount = 0;
  }
}
