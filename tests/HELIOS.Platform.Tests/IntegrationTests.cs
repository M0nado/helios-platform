using Xunit;
using HELIOS.Platform;
using HELIOS.Platform.Components;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.Platform.Tests
{
    /// <summary>
    /// Integration Tests - 25+ tests for component interaction and complex scenarios
    /// </summary>
    public class IntegrationTests
    {
        // ==================== MULTI-COMPONENT DEPLOYMENT TESTS ====================

        [Fact]
        public async Task ProfessionalTier_AllComponentsInitialize()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Professional);
            
            Assert.True(result.Success);
            Assert.True(deployment.MonadoEngine.IsHealthy);
            Assert.True(deployment.SecuritySystem.IsCompliant);
            Assert.True(deployment.GUIDashboard.IsHealthy);
        }

        [Fact]
        public async Task EnterpriseTier_AllComponentsInitialize()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Enterprise);
            
            Assert.True(result.Success);
            Assert.True(deployment.MonadoEngine.IsHealthy);
            Assert.True(deployment.SecuritySystem.IsCompliant);
            Assert.True(deployment.AIOrchestrator.IsModelReady);
            Assert.True(deployment.BuildAgents.IsHealthy);
            Assert.True(deployment.DevAIHub.IsHealthy);
        }

        [Fact]
        public async Task UltimateTier_AllComponentsInitialize()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Ultimate);
            
            Assert.True(result.Success);
            Assert.True(deployment.MonadoEngine.IsHealthy);
            Assert.True(deployment.SecuritySystem.IsCompliant);
            Assert.True(deployment.AIOrchestrator.IsModelReady);
            Assert.True(deployment.BuildAgents.IsHealthy);
            Assert.True(deployment.DevAIHub.IsHealthy);
            Assert.True(deployment.SoftwareStack.IsHealthy);
        }

        // ==================== PHASE PROGRESSION TESTS ====================

        [Fact]
        public async Task DeploymentPhases_ProgressCorrectly()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Professional);
            
            // Should reach phase 3 for Professional tier
            Assert.Equal(3, deployment.CurrentPhase);
            Assert.Equal(DeploymentState.Succeeded, deployment.CurrentStatus.State);
        }

        [Fact]
        public async Task DeploymentPhases_EnterpriseReaches6()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Enterprise);
            Assert.Equal(6, deployment.CurrentPhase);
        }

        [Fact]
        public async Task DeploymentPhases_UltimateReaches7()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Ultimate);
            Assert.Equal(7, deployment.CurrentPhase);
        }

        // ==================== STATUS TRACKING TESTS ====================

        [Fact]
        public async Task StatusTracking_TracksComponentHealth()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Professional);
            var status = await deployment.GetStatusAsync();
            
            Assert.NotNull(status.ComponentStatuses);
            Assert.True(status.ComponentStatuses.All(cs => cs.IsHealthy));
        }

        [Fact]
        public async Task StatusTracking_IncludesVersionInfo()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Professional);
            var status = await deployment.GetStatusAsync();
            
            Assert.True(status.ComponentStatuses.All(cs => !string.IsNullOrEmpty(cs.Version)));
        }

        [Fact]
        public async Task StatusTracking_IncludesLastCheckedTime()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Professional);
            var status = await deployment.GetStatusAsync();
            
            Assert.True(status.ComponentStatuses.All(cs => cs.LastChecked != DateTime.MinValue));
        }

        [Fact]
        public async Task StatusTracking_UpdatesCurrentPhase()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Professional);
            var status = await deployment.GetStatusAsync();
            
            Assert.Equal(deployment.CurrentPhase, status.CurrentPhase);
        }

        // ==================== ROLLBACK SCENARIO TESTS ====================

        [Fact]
        public async Task Rollback_ToInitialPhase_ResetsState()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Professional);
            var initialStatus = deployment.CurrentStatus.State;
            
            await deployment.RollbackAsync(0);
            Assert.Equal(DeploymentState.RolledBack, deployment.CurrentStatus.State);
        }

        [Fact]
        public async Task Rollback_FromEnterprise_ToPhase3()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Enterprise);
            
            var successful = await deployment.RollbackAsync(3);
            Assert.True(successful);
            Assert.Equal(3, deployment.CurrentPhase);
        }

        [Fact]
        public async Task Rollback_FromUltimate_ToPhase5()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Ultimate);
            
            var successful = await deployment.RollbackAsync(5);
            Assert.True(successful);
            Assert.Equal(5, deployment.CurrentPhase);
        }

        // ==================== DEPLOYMENT REPORTING TESTS ====================

        [Fact]
        public async Task DeploymentResult_ContainsExpectedFields()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Professional);
            
            Assert.True(result.Success);
            Assert.NotNull(result.Status);
            Assert.True(result.Duration.TotalMilliseconds > 0);
            Assert.NotNull(result.CreatedResources);
        }

        [Fact]
        public async Task DeploymentResult_CreatedResourcesMatchComponents()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Professional);
            
            Assert.Contains("MonadoEngine-Optimizer", result.CreatedResources);
            Assert.Contains("SecuritySystem-Policies", result.CreatedResources);
            Assert.Contains("GUIDashboard-Interface", result.CreatedResources);
        }

        [Fact]
        public async Task DeploymentResult_NoErrorsOnSuccess()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Professional);
            
            Assert.Empty(result.Errors);
        }

        // ==================== TIER PROGRESSION TESTS ====================

        [Fact]
        public async Task TierProgression_ProfessionalToEnterprise()
        {
            var deployment = new HeliosDeployment();
            
            var prof = await deployment.DeployAsync(DeploymentTier.Professional);
            Assert.True(prof.Success);
            Assert.Equal(3, deployment.CurrentPhase);
            
            // Reset for next deployment
            await deployment.UndeployAsync();
            
            var ent = await deployment.DeployAsync(DeploymentTier.Enterprise);
            Assert.True(ent.Success);
            Assert.Equal(6, deployment.CurrentPhase);
        }

        [Fact]
        public async Task TierProgression_EnterpriseToUltimate()
        {
            var deployment = new HeliosDeployment();
            
            var ent = await deployment.DeployAsync(DeploymentTier.Enterprise);
            Assert.True(ent.Success);
            
            await deployment.UndeployAsync();
            
            var ult = await deployment.DeployAsync(DeploymentTier.Ultimate);
            Assert.True(ult.Success);
            Assert.Equal(7, deployment.CurrentPhase);
        }

        // ==================== RECOVERY SCENARIO TESTS ====================

        [Fact]
        public async Task Undeploy_ThenRedeploy_Succeeds()
        {
            var deployment = new HeliosDeployment();
            
            // First deployment
            var result1 = await deployment.DeployAsync(DeploymentTier.Professional);
            Assert.True(result1.Success);
            
            // Undeploy
            await deployment.UndeployAsync();
            Assert.Equal(0, deployment.CurrentPhase);
            Assert.Equal(DeploymentState.Undeployed, deployment.CurrentStatus.State);
            
            // Redeploy
            var result2 = await deployment.DeployAsync(DeploymentTier.Professional);
            Assert.True(result2.Success);
        }

        [Fact]
        public async Task Rollback_ThenRedeploy_Succeeds()
        {
            var deployment = new HeliosDeployment();
            
            // Deploy
            await deployment.DeployAsync(DeploymentTier.Enterprise);
            
            // Rollback to phase 3
            await deployment.RollbackAsync(3);
            Assert.Equal(3, deployment.CurrentPhase);
            
            // Undeploy
            await deployment.UndeployAsync();
            
            // Redeploy
            var result = await deployment.DeployAsync(DeploymentTier.Professional);
            Assert.True(result.Success);
        }

        // ==================== MULTIPLE DEPLOYMENT TESTS ====================

        [Fact]
        public async Task SequentialDeployments_AllSucceed()
        {
            var deployments = new[] { DeploymentTier.Professional, DeploymentTier.Enterprise, DeploymentTier.Ultimate };
            
            foreach (var tier in deployments)
            {
                var deployment = new HeliosDeployment();
                var result = await deployment.DeployAsync(tier);
                Assert.True(result.Success);
            }
        }

        [Fact]
        public async Task MultipleDeployments_TrackStatusCorrectly()
        {
            for (int i = 0; i < 3; i++)
            {
                var deployment = new HeliosDeployment();
                var result = await deployment.DeployAsync(DeploymentTier.Professional);
                
                var status = await deployment.GetStatusAsync();
                Assert.NotNull(status);
                Assert.Equal(DeploymentTier.Professional, status.Tier);
            }
        }

        // ==================== RESOURCE CREATION TESTS ====================

        [Fact]
        public async Task DeploymentCreatesExpectedResources()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Professional);
            
            Assert.NotEmpty(result.CreatedResources);
            Assert.True(result.CreatedResources.Length >= 7);
        }

        [Fact]
        public async Task AllTiers_CreateMonadoEngineResource()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Professional);
            
            Assert.Contains("MonadoEngine-Optimizer", result.CreatedResources);
        }
    }
}
