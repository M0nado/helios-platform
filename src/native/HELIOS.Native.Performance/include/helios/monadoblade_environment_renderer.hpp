#pragma once

#include <algorithm>
#include <cstdint>

namespace helios::monadoblade::environment {

enum class Identity : std::uint8_t {
  Core,
  Developer,
  Studio,
  Gamer,
  AiServer,
  SysAdmin,
};

enum class QualityTier : std::uint8_t {
  Suspended,
  Minimal,
  Balanced,
  Cinematic,
};

struct RuntimeSignals {
  double frameMilliseconds{};
  double gpuUtilization{};
  double memoryPressure{};
  double batteryLevel{1.0};
  double thermalPressure{};
  bool remoteSession{};
  bool reducedMotion{};
  bool minimized{};
  bool occluded{};
};

struct EnvironmentSignals {
  double localHour{12.0};
  double weatherIntensity{};
  double windSpeed{};
  double pointerImpulse{};
  double interactionEnergy{};
};

struct SceneBudget {
  std::uint32_t particleCapacity{};
  std::uint32_t horizonCards{};
  std::uint32_t grassInstances{};
  std::uint32_t targetUpdatesPerSecond{};
  double fogResolutionScale{};
  bool animateParticles{};
  bool animateGrass{};
};

struct IdentityTuning {
  double particleGain{};
  double windGain{};
  double fogGain{};
  double energyGain{};
};

struct ScenePlan {
  QualityTier quality{};
  SceneBudget budget{};
  double daylight{};
  double weatherIntensity{};
  double windAmplitude{};
  double fogDensity{};
  double pointerImpulse{};
  double particleEmission{};
};

[[nodiscard]] constexpr double clamp01(const double value) noexcept {
  return std::clamp(value, 0.0, 1.0);
}

[[nodiscard]] constexpr double daylight_for_hour(double localHour) noexcept {
  while (localHour < 0.0) {
    localHour += 24.0;
  }
  while (localHour >= 24.0) {
    localHour -= 24.0;
  }

  if (localHour < 5.0 || localHour >= 21.0) {
    return 0.05;
  }
  if (localHour < 8.0) {
    return clamp01((localHour - 5.0) / 3.0);
  }
  if (localHour < 17.0) {
    return 1.0;
  }
  return clamp01((21.0 - localHour) / 4.0);
}

[[nodiscard]] constexpr QualityTier choose_quality(const RuntimeSignals& signals) noexcept {
  if (signals.minimized || signals.occluded) {
    return QualityTier::Suspended;
  }
  if (signals.reducedMotion || signals.remoteSession || signals.batteryLevel <= 0.15 ||
      signals.thermalPressure >= 0.90) {
    return QualityTier::Minimal;
  }
  if (signals.frameMilliseconds >= 20.0 || signals.gpuUtilization >= 0.85 ||
      signals.memoryPressure >= 0.85 || signals.batteryLevel <= 0.35 ||
      signals.thermalPressure >= 0.72) {
    return QualityTier::Balanced;
  }
  return QualityTier::Cinematic;
}

[[nodiscard]] constexpr SceneBudget budget_for(const QualityTier tier) noexcept {
  switch (tier) {
    case QualityTier::Suspended:
      return SceneBudget{};
    case QualityTier::Minimal:
      return SceneBudget{
          .particleCapacity = 384,
          .horizonCards = 2,
          .grassInstances = 96,
          .targetUpdatesPerSecond = 15,
          .fogResolutionScale = 0.25,
          .animateParticles = false,
          .animateGrass = false,
      };
    case QualityTier::Balanced:
      return SceneBudget{
          .particleCapacity = 3072,
          .horizonCards = 4,
          .grassInstances = 1024,
          .targetUpdatesPerSecond = 30,
          .fogResolutionScale = 0.50,
          .animateParticles = true,
          .animateGrass = true,
      };
    case QualityTier::Cinematic:
      return SceneBudget{
          .particleCapacity = 8192,
          .horizonCards = 4,
          .grassInstances = 4096,
          .targetUpdatesPerSecond = 60,
          .fogResolutionScale = 0.67,
          .animateParticles = true,
          .animateGrass = true,
      };
  }
  return SceneBudget{};
}

[[nodiscard]] constexpr IdentityTuning tuning_for(const Identity identity) noexcept {
  switch (identity) {
    case Identity::Core:
      return IdentityTuning{.particleGain = 0.65, .windGain = 0.70, .fogGain = 0.55, .energyGain = 0.70};
    case Identity::Developer:
      return IdentityTuning{.particleGain = 0.80, .windGain = 0.55, .fogGain = 0.35, .energyGain = 0.90};
    case Identity::Studio:
      return IdentityTuning{.particleGain = 0.75, .windGain = 0.65, .fogGain = 0.70, .energyGain = 0.85};
    case Identity::Gamer:
      return IdentityTuning{.particleGain = 0.55, .windGain = 0.95, .fogGain = 0.25, .energyGain = 1.00};
    case Identity::AiServer:
      return IdentityTuning{.particleGain = 0.90, .windGain = 0.45, .fogGain = 0.50, .energyGain = 0.95};
    case Identity::SysAdmin:
      return IdentityTuning{.particleGain = 0.15, .windGain = 0.20, .fogGain = 0.15, .energyGain = 0.35};
  }
  return IdentityTuning{};
}

[[nodiscard]] constexpr ScenePlan build_scene_plan(
    const Identity identity,
    const RuntimeSignals& runtime,
    const EnvironmentSignals& environment) noexcept {
  const auto quality = choose_quality(runtime);
  const auto budget = budget_for(quality);
  const auto tuning = tuning_for(identity);
  const auto qualityGain = quality == QualityTier::Cinematic ? 1.0 :
                           quality == QualityTier::Balanced ? 0.62 :
                           quality == QualityTier::Minimal ? 0.18 : 0.0;

  return ScenePlan{
      .quality = quality,
      .budget = budget,
      .daylight = daylight_for_hour(environment.localHour),
      .weatherIntensity = clamp01(environment.weatherIntensity),
      .windAmplitude = clamp01(environment.windSpeed * tuning.windGain),
      .fogDensity = clamp01((0.15 + environment.weatherIntensity * 0.55) * tuning.fogGain * qualityGain),
      .pointerImpulse = clamp01(environment.pointerImpulse * qualityGain),
      .particleEmission = clamp01(
          (0.20 + environment.weatherIntensity * 0.35 + environment.interactionEnergy * tuning.energyGain) *
          tuning.particleGain * qualityGain),
  };
}

}  // namespace helios::monadoblade::environment
