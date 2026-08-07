#include <cmath>
#include <iostream>
#include <string_view>

#include "helios/monadoblade_profile_optimizer.hpp"

namespace {

int require(bool condition, std::string_view message) {
  if (condition) {
    return 0;
  }
  std::cerr << "monadoblade smoke failure: " << message << '\n';
  return 1;
}

}  // namespace

int main() {
  using namespace helios::monadoblade;

  constexpr RuntimeSignals stable{
      .cpuUtilization = 55.0,
      .gpuUtilization = 47.0,
      .memoryUtilization = 64.0,
      .storageLatencyMs = 8.0,
      .networkLatencyMs = 24.0,
      .thermalPressure = 30.0,
      .securityRisk = 12.0,
      .vmMemoryPressure = 30.0,
      .modelLatencyMs = 95.0,
      .audioXruns = 0.0,
      .frameTimeMs = 13.0,
  };

  constexpr auto recommendation = optimize(Profile::Developer, stable);
  if (require(recommendation.fitness >= 0.0 && recommendation.fitness <= 1.0,
              "fitness must be bounded to [0,1]")) {
    return 1;
  }
  const auto stableRoute = route_label(recommendation);
  if (require(stableRoute != std::string_view{"security-isolation-required"},
              "stable profile should not trigger security isolation")) {
    return 1;
  }

  constexpr auto features = extract_read_only_features(stable);
  static_assert(features.values.size() == 10);
  if (require(features.values[0] > 0.0 && features.values[0] <= 1.0,
              "cpu feature must be normalized")) {
    return 1;
  }
  if (require(std::abs(features.values[9] - 1.0) < 1e-9,
              "zero x-runs should map to max audio confidence")) {
    return 1;
  }

  constexpr RuntimeSignals risky{
      .cpuUtilization = 70.0,
      .gpuUtilization = 45.0,
      .memoryUtilization = 80.0,
      .storageLatencyMs = 20.0,
      .networkLatencyMs = 70.0,
      .thermalPressure = 75.0,
      .securityRisk = 72.0,
      .vmMemoryPressure = 84.0,
      .modelLatencyMs = 160.0,
      .audioXruns = 1.0,
      .frameTimeMs = 16.0,
  };

  constexpr auto admin = optimize(Profile::SysAdmin, risky);
  if (require(admin.requiresApproval, "sysadmin path must always require approval")) {
    return 1;
  }
  if (require(route_label(admin) == std::string_view{"security-isolation-required"},
              "high risk should require security isolation")) {
    return 1;
  }

  return 0;
}
