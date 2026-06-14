using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MonadoBlade.GUI.Windows
{
    /// <summary>
    /// AI Hub Window - Manages AI providers, analytics, and smart routing.
    /// Supports: 6 built-in providers + unlimited cloud providers + Hyper-V/WSL2 hub
    /// </summary>
    public partial class AIHubWindow : Window
    {
        private ObservableCollection<AIProvider> _providers = new ObservableCollection<AIProvider>();
        private AIProvider _selectedProvider;

        public AIHubWindow()
        {
            InitializeComponent();
            LoadProviders();
        }

        /// <summary>
        /// Load all available AI providers.
        /// </summary>
        private void LoadProviders()
        {
            // Built-in providers
            _providers.Add(new AIProvider
            {
                Name = "Claude (Anthropic)",
                Type = ProviderType.BuiltIn,
                Status = ProviderStatus.Online,
                AverageLatency = 245,
                SuccessRate = 99.8,
                TokensUsed = 1250000,
                CostPerMillion = 3.00,
                Description = "Advanced reasoning with extended context (200K tokens)"
            });

            _providers.Add(new AIProvider
            {
                Name = "GPT-4 (OpenAI)",
                Type = ProviderType.BuiltIn,
                Status = ProviderStatus.Online,
                AverageLatency = 320,
                SuccessRate = 99.5,
                TokensUsed = 890000,
                CostPerMillion = 15.00,
                Description = "Multimodal reasoning and analysis"
            });

            _providers.Add(new AIProvider
            {
                Name = "Hermes (Local)",
                Type = ProviderType.BuiltIn,
                Status = ProviderStatus.Online,
                AverageLatency = 150,
                SuccessRate = 99.9,
                TokensUsed = 2100000,
                CostPerMillion = 0.00,
                Description = "Local HERMES fleet learning system"
            });

            _providers.Add(new AIProvider
            {
                Name = "Local LLM",
                Type = ProviderType.BuiltIn,
                Status = ProviderStatus.Online,
                AverageLatency = 120,
                SuccessRate = 98.5,
                TokensUsed = 450000,
                CostPerMillion = 0.00,
                Description = "On-device inference (Mistral, Llama, etc.)"
            });

            _providers.Add(new AIProvider
            {
                Name = "Custom Provider",
                Type = ProviderType.BuiltIn,
                Status = ProviderStatus.Idle,
                AverageLatency = 0,
                SuccessRate = 0,
                TokensUsed = 0,
                CostPerMillion = 0,
                Description = "User-configured custom API endpoint"
            });

            _providers.Add(new AIProvider
            {
                Name = "GitHub Copilot",
                Type = ProviderType.BuiltIn,
                Status = ProviderStatus.Online,
                AverageLatency = 280,
                SuccessRate = 99.7,
                TokensUsed = 620000,
                CostPerMillion = 0.00,
                Description = "Code generation and completion"
            });

            // Cloud providers (expandable)
            _providers.Add(new AIProvider
            {
                Name = "Azure OpenAI",
                Type = ProviderType.Cloud,
                Status = ProviderStatus.Online,
                AverageLatency = 350,
                SuccessRate = 99.6,
                TokensUsed = 450000,
                CostPerMillion = 12.00,
                Description = "Enterprise Azure deployment"
            });

            _providers.Add(new AIProvider
            {
                Name = "AWS Bedrock",
                Type = ProviderType.Cloud,
                Status = ProviderStatus.Online,
                AverageLatency = 300,
                SuccessRate = 99.4,
                TokensUsed = 320000,
                CostPerMillion = 8.00,
                Description = "AWS-hosted multi-model service"
            });

            _providers.Add(new AIProvider
            {
                Name = "Google PaLM",
                Type = ProviderType.Cloud,
                Status = ProviderStatus.Online,
                AverageLatency = 280,
                SuccessRate = 99.3,
                TokensUsed = 280000,
                CostPerMillion = 10.00,
                Description = "Google's pathways language model"
            });

            // Local Hyper-V/WSL2 hub
            _providers.Add(new AIProvider
            {
                Name = "WSL2 AI Hub",
                Type = ProviderType.LocalHub,
                Status = ProviderStatus.Online,
                AverageLatency = 80,
                SuccessRate = 99.8,
                TokensUsed = 1800000,
                CostPerMillion = 0.00,
                Description = "Local containerized LLM cluster (Hyper-V/WSL2)"
            });

            _selectedProvider = _providers[0];
        }

        /// <summary>
        /// Get provider statistics.
        /// </summary>
        public class AIProvider
        {
            public string Name { get; set; }
            public ProviderType Type { get; set; }
            public ProviderStatus Status { get; set; }
            public double AverageLatency { get; set; } // milliseconds
            public double SuccessRate { get; set; } // percentage
            public long TokensUsed { get; set; }
            public double CostPerMillion { get; set; } // dollars
            public string Description { get; set; }

            public string StatusColor => Status switch
            {
                ProviderStatus.Online => "Lime",
                ProviderStatus.Offline => "Red",
                ProviderStatus.Idle => "Gray",
                ProviderStatus.Busy => "Yellow",
                _ => "White"
            };

            public double DailyCost => (TokensUsed / 1000000.0) * CostPerMillion;
        }

        public enum ProviderType
        {
            BuiltIn,
            Cloud,
            LocalHub
        }

        public enum ProviderStatus
        {
            Online,
            Offline,
            Idle,
            Busy
        }
    }

    /// <summary>
    /// AI Hub integration with smart routing and load balancing.
    /// </summary>
    public class AIProviderManager
    {
        private readonly List<ProviderRegistration> _providers = new List<ProviderRegistration>();

        public void RegisterProvider(string name, string endpoint, AIHubWindow.ProviderType type)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Provider name is required.", nameof(name));

            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("Provider endpoint is required.", nameof(endpoint));

            var provider = new AIHubWindow.AIProvider
            {
                Name = name.Trim(),
                Type = type,
                Status = AIHubWindow.ProviderStatus.Idle,
                AverageLatency = type == AIHubWindow.ProviderType.LocalHub ? 100 : 300,
                SuccessRate = 99.0,
                TokensUsed = 0,
                CostPerMillion = type == AIHubWindow.ProviderType.LocalHub ? 0.0 : 10.0,
                Description = $"Registered endpoint: {SanitizeEndpoint(endpoint)}"
            };

            var existing = _providers.FirstOrDefault(p =>
                string.Equals(p.Provider.Name, provider.Name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.Endpoint = endpoint;
                existing.Provider.Status = provider.Status;
                existing.Provider.Description = provider.Description;
                return;
            }

            _providers.Add(new ProviderRegistration(provider, endpoint));
        }

        public AIHubWindow.AIProvider SelectOptimalProvider(AIRequestType requestType)
        {
            if (_providers.Count == 0)
                throw new InvalidOperationException("No AI providers are registered.");

            return _providers
                .Where(p => p.Provider.Status != AIHubWindow.ProviderStatus.Offline)
                .OrderByDescending(p => ScoreProvider(p.Provider, requestType))
                .ThenBy(p => p.Provider.AverageLatency)
                .Select(p => p.Provider)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("No online or idle AI providers are available.");
        }

        public void MonitorProviderHealth()
        {
            foreach (var registration in _providers)
            {
                registration.Provider.Status = string.IsNullOrWhiteSpace(registration.Endpoint)
                    ? AIHubWindow.ProviderStatus.Offline
                    : AIHubWindow.ProviderStatus.Idle;
            }
        }

        public void RebalanceLoad()
        {
            foreach (var registration in _providers.Where(p => p.Provider.Status == AIHubWindow.ProviderStatus.Busy))
            {
                registration.Provider.Status = AIHubWindow.ProviderStatus.Idle;
            }
        }

        private static double ScoreProvider(AIHubWindow.AIProvider provider, AIRequestType requestType)
        {
            var latencyScore = 1.0 / Math.Max(1.0, provider.AverageLatency);
            var reliabilityScore = provider.SuccessRate / 100.0;
            var costScore = provider.CostPerMillion <= 0 ? 1.0 : 1.0 / provider.CostPerMillion;
            var statusScore = provider.Status == AIHubWindow.ProviderStatus.Online ? 1.0 : 0.8;
            var localBoost = provider.Type == AIHubWindow.ProviderType.LocalHub ? 0.15 : 0.0;
            var reasoningBoost = requestType == AIRequestType.Reasoning && provider.Name.Contains("GPT", StringComparison.OrdinalIgnoreCase) ? 0.1 : 0.0;
            var codeBoost = requestType == AIRequestType.CodeGeneration && provider.Name.Contains("Copilot", StringComparison.OrdinalIgnoreCase) ? 0.1 : 0.0;

            return (reliabilityScore * 0.45)
                + (latencyScore * 50 * 0.25)
                + (costScore * 0.20)
                + (statusScore * 0.10)
                + localBoost
                + reasoningBoost
                + codeBoost;
        }

        private static string SanitizeEndpoint(string endpoint)
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
                return "custom endpoint";

            return uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
        }

        private sealed class ProviderRegistration
        {
            public ProviderRegistration(AIHubWindow.AIProvider provider, string endpoint)
            {
                Provider = provider;
                Endpoint = endpoint;
            }

            public AIHubWindow.AIProvider Provider { get; }
            public string Endpoint { get; set; }
        }
    }

    public enum AIRequestType
    {
        CodeGeneration,
        Reasoning,
        Analysis,
        Summarization,
        Translation,
        Custom
    }
}
