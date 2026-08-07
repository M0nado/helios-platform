#pragma once

#include <algorithm>
#include <array>
#include <limits>
#include <string_view>

namespace helios::monado_enterprise {

struct RuntimeSignals {
  double cpuUtilization{};
  double gpuUtilization{};
  double memoryUtilization{};
  double storageLatencyMs{};
  double networkLatencyMs{};
  double thermalPressure{};
  double securityRisk{};
  double vmMemoryPressure{};
  double modelLatencyMs{};
};

[[nodiscard]] constexpr bool is_finite(const double value) noexcept {
  return value == value &&
         value != std::numeric_limits<double>::infinity() &&
         value != -std::numeric_limits<double>::infinity();
}

[[nodiscard]] constexpr double clamp01(const double value) noexcept {
  if (!is_finite(value)) {
    return 1.0;
  }
  return std::clamp(value, 0.0, 1.0);
}

[[nodiscard]] constexpr double normalized_percent(const double value) noexcept {
  if (!is_finite(value)) {
    return 1.0;
  }
  return clamp01(value / 100.0);
}

[[nodiscard]] constexpr double inverse_normalized(const double maximum, const double value) noexcept {
  if (maximum <= 0.0 || !is_finite(maximum) || !is_finite(value)) {
    return 0.0;
  }
  return 1.0 - clamp01(value / maximum);
}

/// Read-only feature extraction for profile-scoring and routing.
[[nodiscard]] constexpr std::array<double, 9> extract_read_only_features(const RuntimeSignals& signals) noexcept {
  return {
      normalized_percent(signals.cpuUtilization),
      normalized_percent(signals.gpuUtilization),
      normalized_percent(signals.memoryUtilization),
      inverse_normalized(30.0, signals.storageLatencyMs),
      inverse_normalized(150.0, signals.networkLatencyMs),
      normalized_percent(signals.thermalPressure),
      normalized_percent(signals.securityRisk),
      normalized_percent(signals.vmMemoryPressure),
      inverse_normalized(250.0, signals.modelLatencyMs),
  };
}

[[nodiscard]] constexpr double profile_activation_pressure(const RuntimeSignals& signals) noexcept {
  const auto features = extract_read_only_features(signals);
  return clamp01(
      (features[0] * 0.10) +  // CPU utilization
      (features[1] * 0.10) +  // GPU utilization
      (features[2] * 0.18) +  // memory utilization
      ((1.0 - features[3]) * 0.14) +  // storage latency pressure
      ((1.0 - features[4]) * 0.12) +  // network latency pressure
      (features[5] * 0.12) +          // thermal pressure
      (features[6] * 0.16) +          // security risk
      (features[7] * 0.08));          // VM memory pressure
}

[[nodiscard]] constexpr bool requires_operator_review(const RuntimeSignals& signals) noexcept {
  const auto pressure = profile_activation_pressure(signals);
  if (!is_finite(signals.securityRisk) || !is_finite(signals.thermalPressure) || !is_finite(pressure)) {
    return true;
  }
  return signals.securityRisk >= 60.0 || signals.thermalPressure >= 85.0 || pressure >= 0.70;
}

[[nodiscard]] constexpr std::string_view activation_label(const RuntimeSignals& signals) noexcept {
  const auto pressure = profile_activation_pressure(signals);
  if (requires_operator_review(signals)) {
    return "proposal-only-review-required";
  }
  if (pressure >= 0.45) {
    return "proposal-only-optimization-recommended";
  }
  return "proposal-only-stable";
}

}  // namespace helios::monado_enterprise
