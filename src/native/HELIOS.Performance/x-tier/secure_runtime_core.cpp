#include "secure_runtime_core.hpp"

#include <algorithm>

namespace xtier::secure {

SecureRuntimeCore::SecureRuntimeCore(double cpu_target, double anomaly_threshold)
    : cpu_target_(cpu_target), anomaly_threshold_(anomaly_threshold) {}

std::vector<RuntimeSignal> SecureRuntimeCore::BuildSignals(double cpu, double memory_mb, double io_wait) const {
  return {
      {"cpu_ratio_to_target", cpu_target_ > 0.0 ? cpu / cpu_target_ : cpu},
      {"memory_pressure", memory_mb / 65536.0},
      {"io_wait", io_wait},
      {"risk_index", std::min(1.0, (cpu * 0.5) + (memory_mb / 131072.0 * 0.3) + (io_wait * 0.2))},
  };
}

SecurityDecision SecureRuntimeCore::Evaluate(const std::vector<RuntimeSignal>& signals) const {
  double risk = 0.0;
  for (const auto& s : signals) {
    if (s.name == "risk_index") risk = s.value;
  }
  if (risk >= anomaly_threshold_) {
    return {false, "risk threshold exceeded, lock high-risk egress and throttle non-critical workloads", static_cast<int>(risk * 100)};
  }
  return {true, "runtime health acceptable", static_cast<int>(risk * 100)};
}

std::unordered_map<std::string, double> SecureRuntimeCore::BuildOptimizationPlan(bool highSecurity) const {
  return {
      {"cpu_scheduler_boost", highSecurity ? 0.75 : 1.0},
      {"memory_gc_pressure", highSecurity ? 0.65 : 0.82},
      {"gpu_queue_depth", highSecurity ? 2.0 : 3.0},
      {"network_egress_factor", highSecurity ? 0.5 : 1.0},
      {"model_hotset_prefetch", highSecurity ? 0.6 : 0.95},
  };
}

std::unordered_map<std::string, double> SecureRuntimeCore::BuildResourceOptimizationData(
    const ResourceSample& sample, bool highSecurity) const {
  const double cpu_norm = std::min(1.0, std::max(0.0, sample.cpu_util / 100.0));
  const double gpu_norm = std::min(1.0, std::max(0.0, sample.gpu_util / 100.0));
  const double mem_norm = std::min(1.0, std::max(0.0, sample.memory_mb / 65536.0));
  const double net_norm = std::min(1.0, std::max(0.0, sample.net_egress_mb_s / 1000.0));

  const double compression_pressure = std::min(1.0, (mem_norm * 0.55) + (net_norm * 0.45));
  const double multitask_pressure = std::min(1.0, (cpu_norm * 0.45) + (gpu_norm * 0.35) + (mem_norm * 0.20));
  const double lightweight_score = std::max(0.0, 1.0 - ((cpu_norm * 0.30) + (mem_norm * 0.45) + (gpu_norm * 0.25)));

  return {
      {"cpu_utilization", sample.cpu_util},
      {"memory_mb", sample.memory_mb},
      {"gpu_utilization", sample.gpu_util},
      {"net_egress_mb_s", sample.net_egress_mb_s},
      {"compression_pressure", compression_pressure},
      {"multitask_pressure", multitask_pressure},
      {"lightweight_score", lightweight_score},
      {"kernel_cpu_clamp", highSecurity ? 0.82 : 0.94},
      {"kernel_memory_compaction", highSecurity ? 0.90 : 0.78},
      {"kernel_gpu_scheduling", highSecurity ? 0.74 : 0.92},
      {"kernel_network_throttle", highSecurity ? 0.58 : 0.90},
      {"kernel_parallel_lane_utilization", highSecurity ? 0.68 : 0.96},
  };
}

std::vector<std::string> SecureRuntimeCore::MajorParallelizationTypes() const {
  return {
      "task-parallel",
      "data-parallel",
      "pipeline-parallel",
      "tensor-parallel",
      "model-parallel",
      "fleet-swarm-parallel",
      "multi-llm-routing-parallel",
      "subagent-specialist-parallel",
      "hybrid-mesh-parallel",
      "async-event-parallel",
  };
}

std::unordered_map<std::string, double> SecureRuntimeCore::BuildSecurityWatchPlan(bool highSecurity) const {
  return {
      {"internet_watch_interval_seconds", highSecurity ? 8.0 : 15.0},
      {"port_watch_interval_seconds", highSecurity ? 5.0 : 12.0},
      {"permission_drift_scan_seconds", highSecurity ? 45.0 : 120.0},
      {"quarantine_trigger_score", highSecurity ? 58.0 : 72.0},
      {"alert_escalation_threshold", highSecurity ? 52.0 : 68.0},
      {"auto_isolation_level", highSecurity ? 0.92 : 0.75},
      {"egress_lockdown_factor", highSecurity ? 0.82 : 0.58},
  };
}

std::unordered_map<std::string, double> SecureRuntimeCore::BuildFolderGovernancePlan(bool highSecurity) const {
  return {
      {"sort_priority_score", highSecurity ? 0.84 : 0.76},
      {"compression_level", highSecurity ? 0.78 : 0.66},
      {"dedupe_intensity", highSecurity ? 0.88 : 0.72},
      {"permission_enforcement_level", highSecurity ? 0.96 : 0.82},
      {"suspicious_folder_quarantine_level", highSecurity ? 0.93 : 0.74},
      {"readonly_guard_ratio", highSecurity ? 0.91 : 0.70},
      {"filesystem_watch_depth", highSecurity ? 0.95 : 0.80},
  };
}

std::vector<std::string> SecureRuntimeCore::AlertChannels() const {
  return {
      "cpp_runtime_watchdog",
      "fleet_security_bus",
      "aihub_knowledge_alerts",
      "quarantine_signal_stream",
      "folder_permission_guard",
  };
}

}  // namespace xtier::secure
