#include <cmath>
#include <iostream>
#include <limits>
#include <string_view>

#include "helios/monado_enterprise_feature_extractor.hpp"

namespace {

int require(bool condition, std::string_view message) {
  if (condition) {
    return 0;
  }
  std::cerr << "monado enterprise native smoke failure: " << message << '\n';
  return 1;
}

}  // namespace

int main() {
  using namespace helios::monado_enterprise;

  constexpr RuntimeSignals stable{
      .cpuUtilization = 42.0,
      .gpuUtilization = 38.0,
      .memoryUtilization = 45.0,
      .storageLatencyMs = 9.0,
      .networkLatencyMs = 24.0,
      .thermalPressure = 35.0,
      .securityRisk = 12.0,
      .vmMemoryPressure = 20.0,
      .modelLatencyMs = 80.0,
  };

  constexpr auto stableFeatures = extract_read_only_features(stable);
  static_assert(stableFeatures.size() == 9);
  if (require(stableFeatures[0] > 0.0 && stableFeatures[0] < 1.0, "cpu feature must be normalized")) {
    return 1;
  }
  if (require(stableFeatures[3] > 0.0 && stableFeatures[3] <= 1.0, "storage feature must be inverse normalized")) {
    return 1;
  }
  if (require(!requires_operator_review(stable), "stable profile should not require operator review")) {
    return 1;
  }
  if (require(activation_label(stable) == std::string_view{"proposal-only-stable"}, "stable activation label mismatch")) {
    return 1;
  }

  constexpr RuntimeSignals stressed{
      .cpuUtilization = 95.0,
      .gpuUtilization = 90.0,
      .memoryUtilization = 93.0,
      .storageLatencyMs = 33.0,
      .networkLatencyMs = 190.0,
      .thermalPressure = 88.0,
      .securityRisk = 72.0,
      .vmMemoryPressure = 91.0,
      .modelLatencyMs = 280.0,
  };

  constexpr auto pressure = profile_activation_pressure(stressed);
  static_assert(pressure >= 0.70);
  if (require(requires_operator_review(stressed), "stressed profile must require operator review")) {
    return 1;
  }
  if (require(
          activation_label(stressed) == std::string_view{"proposal-only-review-required"},
          "stressed activation label mismatch")) {
    return 1;
  }

  RuntimeSignals nonFinite = stable;
  nonFinite.securityRisk = std::numeric_limits<double>::quiet_NaN();
  if (require(requires_operator_review(nonFinite), "non-finite telemetry must fail closed to review-required")) {
    return 1;
  }
  if (require(
          activation_label(nonFinite) == std::string_view{"proposal-only-review-required"},
          "non-finite telemetry label must be review-required")) {
    return 1;
  }
  return 0;
}
