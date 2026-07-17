#pragma once

#include <algorithm>
#include <array>
#include <cstddef>
#include <string_view>

namespace helios::monadoblade {

enum class Profile : std::size_t {
  Developer,
  SysAdmin,
  SysOps,
  Gamer,
  Studio,
  Personal,
  ServerBackground,
};

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
  double audioXruns{};
  double frameTimeMs{};
};

struct OptimizationResult {
  double fitness{};
  bool reduceBackgroundCompute{};
  bool reduceVmOrModelMemory{};
  bool isolateRiskyWorkload{};
  bool requiresApproval{};
};

[[nodiscard]] constexpr double clamp01(const double value) noexcept {
  return std::clamp(value, 0.0, 1.0);
}

[[nodiscard]] constexpr double normalized_percent(const double value) noexcept {
  return clamp01(value / 100.0);
}

[[nodiscard]] constexpr double inverse_normalized(const double maximum,
                                                  const double value) noexcept {
  return maximum <= 0.0 ? 0.0 : 1.0 - clamp01(value / maximum);
}

[[nodiscard]] constexpr double safety_floor(const RuntimeSignals& signals) noexcept {
  const auto thermal = normalized_percent(signals.thermalPressure);
  const auto risk = normalized_percent(signals.securityRisk);
  return clamp01(1.0 - ((thermal * 0.35) + (risk * 0.65)));
}

[[nodiscard]] constexpr double profile_fitness(const Profile profile,
                                               const RuntimeSignals& signals) noexcept {
  const auto cpuHeadroom = 1.0 - normalized_percent(signals.cpuUtilization);
  const auto gpuLoad = normalized_percent(signals.gpuUtilization);
  const auto memoryHeadroom = 1.0 - normalized_percent(signals.memoryUtilization);
  const auto storage = inverse_normalized(30.0, signals.storageLatencyMs);
  const auto network = inverse_normalized(150.0, signals.networkLatencyMs);
  const auto thermal = 1.0 - normalized_percent(signals.thermalPressure);
  const auto risk = 1.0 - normalized_percent(signals.securityRisk);
  const auto vm = 1.0 - normalized_percent(signals.vmMemoryPressure);
  const auto model = inverse_normalized(250.0, signals.modelLatencyMs);
  const auto audio = inverse_normalized(10.0, signals.audioXruns);
  const auto frame = inverse_normalized(33.4, signals.frameTimeMs);

  double profileScore = 0.0;
  switch (profile) {
    case Profile::Developer:
      profileScore = (cpuHeadroom * 0.25) + (memoryHeadroom * 0.25) +
                     (storage * 0.25) + (model * 0.25);
      break;
    case Profile::SysAdmin:
      profileScore = (risk * 0.60) + (thermal * 0.20) + (memoryHeadroom * 0.20);
      break;
    case Profile::SysOps:
      profileScore = (network * 0.25) + (storage * 0.20) + (vm * 0.20) +
                     (risk * 0.35);
      break;
    case Profile::Gamer:
      profileScore = (frame * 0.40) + (network * 0.20) + (gpuLoad * 0.20) +
                     (thermal * 0.20);
      break;
    case Profile::Studio:
      profileScore = (audio * 0.45) + (storage * 0.25) +
                     (memoryHeadroom * 0.15) + (thermal * 0.15);
      break;
    case Profile::Personal:
      profileScore = (cpuHeadroom * 0.20) + (memoryHeadroom * 0.20) +
                     (thermal * 0.25) + (risk * 0.35);
      break;
    case Profile::ServerBackground:
      profileScore = (cpuHeadroom * 0.15) + (memoryHeadroom * 0.20) +
                     (vm * 0.20) + (network * 0.15) + (risk * 0.30);
      break;
  }

  return clamp01((profileScore * 0.80) + (safety_floor(signals) * 0.20));
}

[[nodiscard]] constexpr OptimizationResult optimize(const Profile profile,
                                                    const RuntimeSignals& signals) noexcept {
  return OptimizationResult{
      .fitness = profile_fitness(profile, signals),
      .reduceBackgroundCompute = signals.thermalPressure >= 80.0,
      .reduceVmOrModelMemory =
          signals.memoryUtilization >= 88.0 || signals.vmMemoryPressure >= 85.0,
      .isolateRiskyWorkload = signals.securityRisk >= 60.0,
      .requiresApproval = profile == Profile::SysAdmin || signals.securityRisk >= 60.0,
  };
}

[[nodiscard]] constexpr std::string_view route_label(
    const OptimizationResult& result) noexcept {
  if (result.isolateRiskyWorkload) {
    return "security-isolation-required";
  }
  if (result.reduceVmOrModelMemory) {
    return "memory-pressure-rebalance";
  }
  if (result.reduceBackgroundCompute) {
    return "thermal-background-throttle";
  }
  return result.fitness >= 0.70 ? "profile-optimized" : "profile-review-recommended";
}

}  // namespace helios::monadoblade
