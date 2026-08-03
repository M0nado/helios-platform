#include <cmath>
#include <iostream>
#include <string_view>

#include "helios/monadoblade_environment_renderer.hpp"

namespace {

int require(const bool condition, const std::string_view message) {
  if (condition) {
    return 0;
  }
  std::cerr << "environment smoke failure: " << message << '\n';
  return 1;
}

}  // namespace

int main() {
  using namespace helios::monadoblade::environment;

  constexpr RuntimeSignals cinematic{
      .frameMilliseconds = 11.0,
      .gpuUtilization = 0.40,
      .memoryPressure = 0.35,
      .batteryLevel = 0.90,
      .thermalPressure = 0.30,
  };
  static_assert(choose_quality(cinematic) == QualityTier::Cinematic);
  static_assert(budget_for(QualityTier::Cinematic).particleCapacity == 8192);

  constexpr RuntimeSignals hidden{.minimized = true};
  static_assert(choose_quality(hidden) == QualityTier::Suspended);
  static_assert(budget_for(QualityTier::Suspended).particleCapacity == 0);

  constexpr RuntimeSignals constrained{
      .frameMilliseconds = 24.0,
      .gpuUtilization = 0.88,
      .memoryPressure = 0.50,
      .batteryLevel = 0.70,
      .thermalPressure = 0.50,
  };
  static_assert(choose_quality(constrained) == QualityTier::Balanced);

  constexpr RuntimeSignals enterBalanced{
      .frameMilliseconds = 20.5,
      .gpuUtilization = 0.70,
      .memoryPressure = 0.60,
      .batteryLevel = 0.80,
      .thermalPressure = 0.40,
  };
  static_assert(choose_quality(enterBalanced, QualityTier::Cinematic) == QualityTier::Balanced);

  constexpr RuntimeSignals remainBalanced{
      .frameMilliseconds = 19.0,
      .gpuUtilization = 0.70,
      .memoryPressure = 0.60,
      .batteryLevel = 0.80,
      .thermalPressure = 0.40,
  };
  static_assert(choose_quality(remainBalanced, QualityTier::Balanced) == QualityTier::Balanced);

  constexpr RuntimeSignals recoverCinematic{
      .frameMilliseconds = 16.0,
      .gpuUtilization = 0.60,
      .memoryPressure = 0.60,
      .batteryLevel = 0.80,
      .thermalPressure = 0.40,
  };
  static_assert(choose_quality(recoverCinematic, QualityTier::Balanced) == QualityTier::Cinematic);

  constexpr RuntimeSignals remainMinimal{
      .frameMilliseconds = 12.0,
      .gpuUtilization = 0.40,
      .memoryPressure = 0.40,
      .batteryLevel = 0.18,
      .thermalPressure = 0.40,
  };
  static_assert(choose_quality(remainMinimal, QualityTier::Minimal) == QualityTier::Minimal);

  static_assert(daylight_for_hour(5.0) == 0.05);
  static_assert(daylight_for_hour(8.0) == 1.0);
  static_assert(daylight_for_hour(17.0) == 1.0);
  static_assert(daylight_for_hour(21.0) == 0.05);

  constexpr EnvironmentSignals sunset{
      .localHour = 19.0,
      .weatherIntensity = 0.40,
      .windSpeed = 0.60,
      .pointerImpulse = 0.75,
      .interactionEnergy = 0.80,
  };
  constexpr auto gamerPlan = build_scene_plan(Identity::Gamer, cinematic, sunset);
  static_assert(gamerPlan.quality == QualityTier::Cinematic);
  static_assert(gamerPlan.budget.horizonCards == 4);
  static_assert(gamerPlan.windAmplitude > 0.50);

  if (require(std::abs(gamerPlan.daylight - 0.525) < 1e-9, "sunset daylight regression") ||
      require(gamerPlan.particleEmission > 0.50, "interaction emission regression")) {
    return 1;
  }

  constexpr auto sysAdminTuning = tuning_for(Identity::SysAdmin);
  constexpr auto gamerTuning = tuning_for(Identity::Gamer);
  if (require(sysAdminTuning.particleGain < gamerTuning.particleGain,
              "SysAdmin must remain visually quiet")) {
    return 1;
  }

  return 0;
}
