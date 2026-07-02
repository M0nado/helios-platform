using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.Core.AI.Interfaces;
using HELIOS.Platform.Core.AIHub;
using Xunit;

namespace HELIOS.Platform.Tests.AIHub
{
    public class AIHubFleetServiceTests
    {
        [Theory]
        [InlineData("azure")]
        [InlineData("docker")]
        [InlineData("github")]
        [InlineData("security")]
        [InlineData("analytics")]
        [InlineData("frontend")]
        [InlineData("backend")]
        public async Task RecommendFleetAsync_UsesRouterAndReturnsMissionSpecialist(string mission)
        {
            var service = new AIHubFleetService();

            var recommendation = await service.RecommendFleetAsync(mission);

            Assert.NotEmpty(recommendation.Units);
            Assert.Contains(recommendation.Units, unit => unit.Capabilities.Contains(mission));
            Assert.NotEmpty(recommendation.BalanceTips);
            Assert.NotEmpty(recommendation.MissionRecommendations);
        }

        [Fact]
        public async Task TrainUnitsAsync_TrainsAndDeploysLearningModelsForRegisteredUnits()
        {
            var service = new AIHubFleetService();

            var result = await service.TrainUnitsAsync();

            Assert.True(result.LearningStarted);
            Assert.Equal(service.Units.Count, result.Models.Count);
            Assert.All(result.Models, model => Assert.True(model.IsDeployed));
        }

        [Fact]
        public async Task AIHubUnitAgentAdapter_MapsUnitToUnifiedAgentContract()
        {
            var unit = new AIHubUnit { UnitId = "azure-test", Name = "Azure Test", MissionFamily = "azure", Capabilities = new[] { "azure" } };
            IAgent agent = new AIHubUnitAgentAdapter(unit);

            await agent.InitializeAsync();
            var result = await agent.ExecuteAsync(new AgentRequest { Operation = "recommend", Parameters = { ["mission"] = "azure" } });

            Assert.Equal(unit.UnitId, agent.AgentId);
            Assert.True(agent.HasCapability("azure"));
            Assert.True(result.Success);
            Assert.Same(unit, result.ResultData);
        }

        [Fact]
        public async Task ScriptOperations_AreBehindAdapterInsteadOfUiShellingScripts()
        {
            var service = new AIHubFleetService(scriptAdapter: new NoOpAIHubScriptAdapter());

            var result = await service.InvokeScriptAdapterAsync("ai-integration/status", new Dictionary<string, object>());

            Assert.Contains("adapter", result);
        }
    }
}
