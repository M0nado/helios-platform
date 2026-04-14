using Xunit;
using HELIOS.Platform;
using HELIOS.Platform.Components;
using System;
using System.Threading.Tasks;

namespace HELIOS.Platform.Tests
{
    /// <summary>
    /// End-to-End Tests - 12+ tests for complete scenarios
    /// </summary>
    public class EndToEndTests
    {
        // ==================== FULL DEPLOYMENT SCENARIO TESTS ====================

        [Fact]
        public async Task E2E_ProfessionalDeployment_Complete()
        {
            // Arrange
            var deployment = new HeliosDeployment();
            
            // Act
            var validation = await deployment.ValidateAsync();
            var deployResult = await deployment.DeployAsync(DeploymentTier.Professional);
            var status = await deployment.GetStatusAsync();
            
            // Assert
            Assert.True(validation);
            Assert.True(deployResult.Success);
            Assert.Equal(DeploymentState.Succeeded, status.State);
            Assert.Equal(3, status.CurrentPhase);
        }

        [Fact]
        public async Task E2E_EnterpriseDeployment_Complete()
        {
            // Arrange
            var deployment = new HeliosDeployment();
            
            // Act
            var validation = await deployment.ValidateAsync();
            var deployResult = await deployment.DeployAsync(DeploymentTier.Enterprise);
            var status = await deployment.GetStatusAsync();
            
            // Assert
            Assert.True(validation);
            Assert.True(deployResult.Success);
            Assert.Equal(DeploymentState.Succeeded, status.State);
            Assert.Equal(6, status.CurrentPhase);
            Assert.True(deployment.BuildAgents.IsHealthy);
            Assert.True(deployment.AIOrchestrator.IsModelReady);
        }

        [Fact]
        public async Task E2E_UltimateDeployment_Complete()
        {
            // Arrange
            var deployment = new HeliosDeployment();
            
            // Act
            var validation = await deployment.ValidateAsync();
            var deployResult = await deployment.DeployAsync(DeploymentTier.Ultimate);
            var status = await deployment.GetStatusAsync();
            
            // Assert
            Assert.True(validation);
            Assert.True(deployResult.Success);
            Assert.Equal(DeploymentState.Succeeded, status.State);
            Assert.Equal(7, status.CurrentPhase);
            Assert.True(deployment.SoftwareStack.IsHealthy);
        }

        // ==================== MULTI-PHASE EXECUTION TESTS ====================

        [Fact]
        public async Task E2E_MultiPhaseExecution_AllPhasesComplete()
        {
            var deployment = new HeliosDeployment();
            
            // Phase 0: Validation
            Assert.Equal(0, deployment.CurrentPhase);
            var validation = await deployment.ValidateAsync();
            Assert.True(validation);
            
            // Deploy through all phases
            var result = await deployment.DeployAsync(DeploymentTier.Ultimate);
            Assert.True(result.Success);
            
            // Verify all phases completed
            Assert.Equal(7, deployment.CurrentPhase);
            Assert.Equal(DeploymentState.Succeeded, deployment.CurrentStatus.State);
        }

        [Fact]
        public async Task E2E_ComponentSequence_Correct()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Ultimate);
            
            // Verify all components initialized in sequence
            Assert.True(deployment.MonadoEngine.IsHealthy);
            Assert.True(deployment.SecuritySystem.IsCompliant);
            Assert.True(deployment.GUIDashboard.IsHealthy);
            Assert.True(deployment.BuildAgents.IsHealthy);
            Assert.True(deployment.AIOrchestrator.IsModelReady);
            Assert.True(deployment.DevAIHub.IsHealthy);
            Assert.True(deployment.SoftwareStack.IsHealthy);
        }

        // ==================== COMPLETE ROLLBACK TESTS ====================

        [Fact]
        public async Task E2E_Rollback_FromUltimateToStart()
        {
            var deployment = new HeliosDeployment();
            
            // Deploy to Ultimate
            var deployResult = await deployment.DeployAsync(DeploymentTier.Ultimate);
            Assert.True(deployResult.Success);
            Assert.Equal(7, deployment.CurrentPhase);
            
            // Rollback to phase 0
            var rollbackResult = await deployment.RollbackAsync(0);
            Assert.True(rollbackResult);
            Assert.Equal(0, deployment.CurrentPhase);
            Assert.Equal(DeploymentState.RolledBack, deployment.CurrentStatus.State);
        }

        [Fact]
        public async Task E2E_Rollback_PreservesComponentReferences()
        {
            var deployment = new HeliosDeployment();
            
            // Deploy
            await deployment.DeployAsync(DeploymentTier.Enterprise);
            
            // Rollback
            await deployment.RollbackAsync(3);
            
            // Components should still exist
            Assert.NotNull(deployment.MonadoEngine);
            Assert.NotNull(deployment.BuildAgents);
            Assert.NotNull(deployment.AIOrchestrator);
        }

        [Fact]
        public async Task E2E_PartialRollback_ToIntermediatePhase()
        {
            var deployment = new HeliosDeployment();
            
            // Deploy Ultimate
            await deployment.DeployAsync(DeploymentTier.Ultimate);
            Assert.Equal(7, deployment.CurrentPhase);
            
            // Rollback to phase 4 (Build Agents)
            await deployment.RollbackAsync(4);
            Assert.Equal(4, deployment.CurrentPhase);
            
            // Should still have earlier components
            Assert.True(deployment.MonadoEngine.IsHealthy);
            Assert.True(deployment.SecuritySystem.IsCompliant);
        }

        // ==================== ERROR RECOVERY TESTS ====================

        [Fact]
        public async Task E2E_DeploymentFailure_RollsBackGracefully()
        {
            var deployment = new HeliosDeployment();
            
            // Even if deployment has issues, rollback should work
            try
            {
                await deployment.DeployAsync(DeploymentTier.Professional);
            }
            catch { }
            
            var result = await deployment.RollbackAsync(0);
            Assert.True(result);
        }

        [Fact]
        public async Task E2E_Undeploy_CleansUpState()
        {
            var deployment = new HeliosDeployment();
            
            // Deploy
            await deployment.DeployAsync(DeploymentTier.Ultimate);
            var statusBefore = await deployment.GetStatusAsync();
            Assert.NotEqual(DeploymentState.Undeployed, statusBefore.State);
            
            // Undeploy
            await deployment.UndeployAsync();
            
            // Verify cleanup
            Assert.Equal(0, deployment.CurrentPhase);
            Assert.Equal(DeploymentState.Undeployed, deployment.CurrentStatus.State);
        }

        // ==================== SYSTEM VERIFICATION TESTS ====================

        [Fact]
        public async Task E2E_SystemVerification_AllComponentsHealthy()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Ultimate);
            var status = await deployment.GetStatusAsync();
            
            // Verify all components report healthy
            Assert.True(deployment.MonadoEngine.IsHealthy);
            Assert.True(deployment.SecuritySystem.IsCompliant);
            Assert.True(deployment.AIOrchestrator.IsModelReady);
            Assert.True(deployment.GUIDashboard.IsHealthy);
            Assert.True(deployment.BuildAgents.IsHealthy);
            Assert.True(deployment.DevAIHub.IsHealthy);
            Assert.True(deployment.SoftwareStack.IsHealthy);
        }

        [Fact]
        public async Task E2E_StatusReporting_Complete()
        {
            var deployment = new HeliosDeployment();
            var deployResult = await deployment.DeployAsync(DeploymentTier.Ultimate);
            var status = await deployment.GetStatusAsync();
            
            // Verify comprehensive status information
            Assert.NotNull(status.ComponentStatuses);
            Assert.Equal(7, status.ComponentStatuses.Length);
            Assert.Equal(100.0, status.ProgressPercentage);
            Assert.True(status.CompletionTime.HasValue);
            Assert.True(status.StartTime != DateTime.MinValue);
        }

        // ==================== RESOURCE TRACKING TESTS ====================

        [Fact]
        public async Task E2E_ResourceTracking_AllResourcesCreated()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Ultimate);
            
            // All expected resources should be created
            Assert.Contains("MonadoEngine-Optimizer", result.CreatedResources);
            Assert.Contains("SecuritySystem-Policies", result.CreatedResources);
            Assert.Contains("AIOrchestrator-Models", result.CreatedResources);
            Assert.Contains("GUIDashboard-Interface", result.CreatedResources);
            Assert.Contains("BuildAgents-Pipeline", result.CreatedResources);
            Assert.Contains("DevAIHub-Services", result.CreatedResources);
            Assert.Contains("SoftwareStack-Registry", result.CreatedResources);
        }

        [Fact]
        public async Task E2E_MetricsCollection_TimingAccurate()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Professional);
            
            // Duration should be measurable and reasonable
            Assert.True(result.Duration.TotalMilliseconds > 0);
            Assert.True(result.Duration.TotalMilliseconds < 60000); // Less than 60 seconds
        }

        [Fact]
        public async Task E2E_RecoverySequence_CompleteRedeployment()
        {
            var deployment = new HeliosDeployment();
            
            // First deployment
            var result1 = await deployment.DeployAsync(DeploymentTier.Ultimate);
            Assert.True(result1.Success);
            
            // Undeploy
            await deployment.UndeployAsync();
            
            // Redeploy
            var result2 = await deployment.DeployAsync(DeploymentTier.Ultimate);
            Assert.True(result2.Success);
            
            // Verify complete recovery
            var status = await deployment.GetStatusAsync();
            Assert.Equal(DeploymentState.Succeeded, status.State);
            Assert.Equal(7, status.CurrentPhase);
        }
    }
}
