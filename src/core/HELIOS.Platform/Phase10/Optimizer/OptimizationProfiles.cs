using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Phase10.Optimizer
{
    /// Predefined optimization profiles for different use cases
    public static class OptimizationProfiles
    {
        /// Gaming optimization profile
        public static OptimizationProfile GamingProfile => new OptimizationProfile
        {
            Name = "Gaming",
            Description = "Optimized for gaming performance - Maximum FPS and minimal latency",
            EnableRegistry = true,
            EnableProcessManagement = true,
            EnableNetworkTuning = true,
            EnableGPUOptimization = true,
            EnablePowerOptimization = true,
            MaxExecutionTimeSeconds = 300,
            Settings = new Dictionary<string, object>
            {
                ["CPUPriority"] = "High",
                ["GPUClockSpeed"] = "Maximum",
                ["NetworkCompression"] = false,
                ["VisualEffects"] = "Maximum",
                ["RefreshRate"] = 144,
                ["TargetFPS"] = 120,
                ["PowerMode"] = "HighPerformance"
            }
        };

        /// Work profile
        public static OptimizationProfile WorkProfile => new OptimizationProfile
        {
            Name = "Work",
            Description = "Optimized for productivity - Balanced performance and power",
            EnableRegistry = true,
            EnableProcessManagement = true,
            EnableNetworkTuning = true,
            EnableGPUOptimization = false,
            EnablePowerOptimization = true,
            MaxExecutionTimeSeconds = 240,
            Settings = new Dictionary<string, object>
            {
                ["CPUPriority"] = "Normal",
                ["PowerMode"] = "Balanced"
            }
        };

        /// Development profile
        public static OptimizationProfile DevelopmentProfile => new OptimizationProfile
        {
            Name = "Development",
            Description = "Optimized for development - Focus on compilation",
            EnableRegistry = true,
            EnableProcessManagement = true,
            EnableNetworkTuning = true,
            EnableGPUOptimization = false,
            EnablePowerOptimization = true,
            MaxExecutionTimeSeconds = 300,
            Settings = new Dictionary<string, object>
            {
                ["CPUPriority"] = "High",
                ["CompilationBoost"] = true
            }
        };

        /// Server profile
        public static OptimizationProfile ServerProfile => new OptimizationProfile
        {
            Name = "Server",
            Description = "Optimized for server operations - Maximum uptime",
            EnableRegistry = true,
            EnableProcessManagement = true,
            EnableNetworkTuning = true,
            EnableGPUOptimization = false,
            EnablePowerOptimization = true,
            MaxExecutionTimeSeconds = 500,
            Settings = new Dictionary<string, object>
            {
                ["UptimeMode"] = true,
                ["PowerMode"] = "PowerSaver"
            }
        };

        /// Balanced profile
        public static OptimizationProfile BalancedProfile => new OptimizationProfile
        {
            Name = "Balanced",
            Description = "Balanced optimization",
            EnableRegistry = true,
            EnableProcessManagement = true,
            EnableNetworkTuning = true,
            EnableGPUOptimization = true,
            EnablePowerOptimization = true,
            MaxExecutionTimeSeconds = 300,
            Settings = new Dictionary<string, object>()
        };

        /// Get all profiles
        public static List<OptimizationProfile> GetAllProfiles()
        {
            return new List<OptimizationProfile>
            {
                GamingProfile,
                WorkProfile,
                DevelopmentProfile,
                ServerProfile,
                BalancedProfile
            };
        }

        /// Get profile by name
        public static OptimizationProfile GetProfileByName(string name)
        {
            return name?.ToLower() switch
            {
                "gaming" => GamingProfile,
                "work" => WorkProfile,
                "development" => DevelopmentProfile,
                "server" => ServerProfile,
                _ => BalancedProfile
            };
        }
    }
}
