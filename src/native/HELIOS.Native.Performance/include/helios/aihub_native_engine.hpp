#pragma once

#include <algorithm>
#include <array>
#include <numeric>
#include <span>
#include <string_view>

namespace helios::aihub {

struct NativeRouteSignals {
  double memoryPressure{};
  double vectorizationPotential{};
  double latencySensitivity{};
  double dataLocality{};
  double riskPenalty{};
};

constexpr double clamp01(double value) noexcept {
  return std::max(0.0, std::min(1.0, value));
}

constexpr double performance_route_score(const NativeRouteSignals& signals) noexcept {
  return clamp01((signals.memoryPressure * 0.22) +
                 (signals.vectorizationPotential * 0.28) +
                 (signals.latencySensitivity * 0.25) +
                 (signals.dataLocality * 0.18) -
                 (signals.riskPenalty * 0.20));
}

constexpr std::string_view route_label(double score) noexcept {
  return score >= 0.85 ? "native-hot-path" :
         score >= 0.65 ? "native-accelerated-submodule" :
         score >= 0.45 ? "managed-or-fsharp-partner" :
                         "avoid-native-until-profiled";
}

inline double weighted_similarity(std::span<const double> left,
                                  std::span<const double> right,
                                  std::span<const double> weights) noexcept {
  const auto count = std::min({left.size(), right.size(), weights.size()});
  if (count == 0) {
    return 0.0;
  }
  double weightedDistance = 0.0;
  double totalWeight = 0.0;
  for (std::size_t index = 0; index < count; ++index) {
    weightedDistance += std::abs(left[index] - right[index]) * weights[index];
    totalWeight += weights[index];
  }
  if (totalWeight <= 0.0) {
    return 0.0;
  }
  return clamp01(1.0 - (weightedDistance / totalWeight));
}

inline std::array<double, 4> normalize_priority_cluster(std::span<const double, 4> values) noexcept {
  const auto minmax = std::minmax_element(values.begin(), values.end());
  const auto range = *minmax.second - *minmax.first;
  std::array<double, 4> result{};
  for (std::size_t index = 0; index < result.size(); ++index) {
    result[index] = range <= 0.0 ? 0.0 : clamp01((values[index] - *minmax.first) / range);
  }
  return result;
}

}  // namespace helios::aihub

namespace helios::aihub {

struct GuiRenderBudget {
  double frameMilliseconds{};
  double nodeCount{};
  double edgeCount{};
  double memoryMegabytes{};
};

constexpr double gui_render_pressure(const GuiRenderBudget& budget) noexcept {
  return clamp01((budget.frameMilliseconds / 16.67 * 0.35) +
                 (budget.nodeCount / 1000.0 * 0.25) +
                 (budget.edgeCount / 4000.0 * 0.20) +
                 (budget.memoryMegabytes / 512.0 * 0.20));
}

constexpr bool should_use_native_rendering(const GuiRenderBudget& budget) noexcept {
  return gui_render_pressure(budget) >= 0.55;
}

struct UsbWizardNativeCheck {
  double deviceCount{};
  double partitionCount{};
  double checksumGigabytes{};
  double securitySensitivity{};
};

constexpr double usbwizard_native_priority(const UsbWizardNativeCheck& check) noexcept {
  return clamp01((check.deviceCount / 16.0 * 0.18) +
                 (check.partitionCount / 12.0 * 0.22) +
                 (check.checksumGigabytes / 128.0 * 0.25) +
                 (check.securitySensitivity * 0.35));
}

}  // namespace helios::aihub

namespace helios::aihub {

struct BranchGraphSignals {
  double commitCount{};
  double touchedFiles{};
  double duplicatePaths{};
  double hotPathFiles{};
  double failingChecks{};
};

constexpr double branch_graph_complexity(const BranchGraphSignals& graph) noexcept {
  return clamp01((graph.commitCount / 120.0 * 0.20) +
                 (graph.touchedFiles / 500.0 * 0.22) +
                 (graph.duplicatePaths / 120.0 * 0.18) +
                 (graph.hotPathFiles / 80.0 * 0.25) +
                 (graph.failingChecks / 40.0 * 0.15));
}

constexpr bool should_use_native_branch_compare(const BranchGraphSignals& graph) noexcept {
  return branch_graph_complexity(graph) >= 0.50;
}

}  // namespace helios::aihub

namespace helios::aihub {

struct BranchFinishSignals {
  double absorptionScore{};
  double graphComplexity{};
  double nativeHotPathPressure{};
  double failingChecks{};
  double rollbackCoverage{};
};

constexpr double branch_finish_priority(const BranchFinishSignals& signals) noexcept {
  return clamp01((signals.absorptionScore * 0.30) +
                 (signals.graphComplexity * 0.20) +
                 (signals.nativeHotPathPressure * 0.22) -
                 (signals.failingChecks / 40.0 * 0.16) +
                 (signals.rollbackCoverage * 0.12));
}

constexpr bool should_finish_with_native_assist(const BranchFinishSignals& signals) noexcept {
  return branch_finish_priority(signals) >= 0.58;
}

}  // namespace helios::aihub

namespace helios::aihub {

struct ComplexGradeNativeSignals {
  double lineCount{};
  double duplicateLineRatio{};
  double complexityScore{};
  double nativeTokenPressure{};
  double performanceTokenPressure{};
};

constexpr double complex_grade_native_boost(const ComplexGradeNativeSignals& signals) noexcept {
  return clamp01((signals.lineCount / 2000.0 * 0.16) +
                 (signals.duplicateLineRatio * 0.18) +
                 (signals.complexityScore * 0.22) +
                 (signals.nativeTokenPressure * 0.22) +
                 (signals.performanceTokenPressure * 0.22));
}

constexpr bool should_route_complex_grade_to_native(const ComplexGradeNativeSignals& signals) noexcept {
  return complex_grade_native_boost(signals) >= 0.52;
}

}  // namespace helios::aihub

namespace helios::aihub {

struct SelfLearningNativeSignals {
  double situationVariablePressure{};
  double connectionIdeaPressure{};
  double compositePartPressure{};
  double smallFileCount{};
  double hotPathUrgency{};
  double deleteReviewNeed{};
};

constexpr double self_learning_native_assist_score(const SelfLearningNativeSignals& signals) noexcept {
  return clamp01((signals.situationVariablePressure * 0.18) +
                 (signals.connectionIdeaPressure * 0.20) +
                 (signals.compositePartPressure * 0.18) +
                 (signals.smallFileCount / 64.0 * 0.12) +
                 (signals.hotPathUrgency * 0.22) -
                 (signals.deleteReviewNeed * 0.10));
}

constexpr bool should_native_assist_self_learning(const SelfLearningNativeSignals& signals) noexcept {
  return self_learning_native_assist_score(signals) >= 0.56;
}

constexpr std::string_view self_learning_native_label(const SelfLearningNativeSignals& signals) noexcept {
  const auto score = self_learning_native_assist_score(signals);
  if (score >= 0.82) {
    return "native-self-learning-hot-path";
  }
  if (score >= 0.56) {
    return "native-assisted-learning-compare";
  }
  return "managed-fsharp-learning-first";
}

}  // namespace helios::aihub
namespace helios::aihub {

struct KnowledgeVectorSignals {
  double sqlConfidence{};
  double vectorSimilarity{};
  double graphNeighborhood{};
  double freshness{};
  double failingTestPressure{};
  double safetyPenalty{};
};

constexpr double knowledge_baked_fix_priority(const KnowledgeVectorSignals& signals) noexcept {
  return clamp01((signals.sqlConfidence * 0.20) +
                 (signals.vectorSimilarity * 0.28) +
                 (signals.graphNeighborhood * 0.18) +
                 (signals.freshness * 0.14) +
                 (signals.failingTestPressure * 0.16) -
                 (signals.safetyPenalty * 0.18));
}

constexpr bool should_use_knowledge_baked_native_fix(const KnowledgeVectorSignals& signals) noexcept {
  return knowledge_baked_fix_priority(signals) >= 0.60;
}

constexpr std::string_view knowledge_baked_fix_label(const KnowledgeVectorSignals& signals) noexcept {
  const auto score = knowledge_baked_fix_priority(signals);
  if (score >= 0.84) {
    return "native-vector-sql-fix-hot-path";
  }
  if (score >= 0.60) {
    return "native-assisted-knowledge-fix";
  }
  return "managed-knowledge-fix-first";
}

}  // namespace helios::aihub
