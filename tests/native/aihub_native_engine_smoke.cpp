#include <array>
#include <cassert>
#include <cmath>
#include <string_view>

#include "helios/aihub_native_engine.hpp"

int main() {
  using namespace helios::aihub;

  constexpr NativeRouteSignals route{
      .memoryPressure = 0.9,
      .vectorizationPotential = 0.9,
      .latencySensitivity = 0.8,
      .dataLocality = 0.8,
      .riskPenalty = 0.1,
  };
  constexpr auto routeScore = performance_route_score(route);
  static_assert(routeScore > 0.70 && routeScore <= 1.0);
  assert(route_label(routeScore) == std::string_view{"native-accelerated-submodule"});

  constexpr FourEngineNativeSignals fusion{
      .csharpSafetyScore = 0.90,
      .fsharpLearningScore = 0.85,
      .cppNativeScore = 0.80,
      .pythonGlueScore = 0.75,
      .vectorPressure = 0.70,
      .integrationRisk = 0.10,
  };
  static_assert(should_integrate_four_engine_parallel(fusion));

  const std::array<double, 3> left{0.1, 0.5, 0.9};
  const std::array<double, 3> right{0.1, 0.4, 0.8};
  const std::array<double, 3> weights{1.0, 1.0, 2.0};
  const auto similarity = weighted_similarity(left, right, weights);
  assert(similarity > 0.90 && similarity <= 1.0);

  const std::array<double, 4> priorities{4.0, 2.0, 8.0, 6.0};
  const auto normalized = normalize_priority_cluster(priorities);
  assert(std::abs(normalized[0] - (1.0 / 3.0)) < 1e-9);
  assert(normalized[1] == 0.0);
  assert(normalized[2] == 1.0);
  return 0;
}
