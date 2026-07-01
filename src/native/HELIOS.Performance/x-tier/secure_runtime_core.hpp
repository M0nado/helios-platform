#pragma once

#include <string>
#include <unordered_map>
#include <vector>

namespace xtier::secure {

struct RuntimeSignal {
  std::string name;
  double value;
};

struct SecurityDecision {
  bool allow;
  std::string reason;
  int score;
};

struct ResourceSample {
  double cpu_util;
  double memory_mb;
  double gpu_util;
  double net_egress_mb_s;
};

class SecureRuntimeCore {
 public:
  SecureRuntimeCore(double cpu_target, double anomaly_threshold);
  std::vector<RuntimeSignal> BuildSignals(double cpu, double memory_mb, double io_wait) const;
  SecurityDecision Evaluate(const std::vector<RuntimeSignal>& signals) const;
  std::unordered_map<std::string, double> BuildOptimizationPlan(bool highSecurity) const;
  std::unordered_map<std::string, double> BuildResourceOptimizationData(const ResourceSample& sample, bool highSecurity) const;
  std::vector<std::string> MajorParallelizationTypes() const;
  std::unordered_map<std::string, double> BuildSecurityWatchPlan(bool highSecurity) const;
  std::unordered_map<std::string, double> BuildFolderGovernancePlan(bool highSecurity) const;
  std::vector<std::string> AlertChannels() const;

 private:
  double cpu_target_;
  double anomaly_threshold_;
};

}  // namespace xtier::secure
