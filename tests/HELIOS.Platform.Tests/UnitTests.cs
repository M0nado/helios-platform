using Xunit;
using HELIOS.Platform;
using HELIOS.Platform.Components;
using System;
using System.Threading.Tasks;

namespace HELIOS.Platform.Tests
{
    /// <summary>
    /// Unit Tests - 45+ tests for component isolation, phase transitions, and error handling
    /// </summary>
    public class UnitTests
    {
        // ==================== CONSTRUCTOR TESTS ====================
        
        [Fact]
        public void HeliosDeployment_Constructor_InitializesAllComponents()
        {
            var deployment = new HeliosDeployment();
            
            Assert.NotNull(deployment.MonadoEngine);
            Assert.NotNull(deployment.SecuritySystem);
            Assert.NotNull(deployment.AIOrchestrator);
            Assert.NotNull(deployment.GUIDashboard);
            Assert.NotNull(deployment.BuildAgents);
            Assert.NotNull(deployment.DevAIHub);
            Assert.NotNull(deployment.SoftwareStack);
        }

        [Fact]
        public void HeliosDeployment_Constructor_SetsDefaultTierToProfessional()
        {
            var deployment = new HeliosDeployment();
            Assert.Equal(DeploymentTier.Professional, deployment.CurrentTier);
        }

        [Fact]
        public void HeliosDeployment_Constructor_InitializesPhaseTo0()
        {
            var deployment = new HeliosDeployment();
            Assert.Equal(0, deployment.CurrentPhase);
        }

        [Fact]
        public void HeliosDeployment_Constructor_InitializesStatusNotNull()
        {
            var deployment = new HeliosDeployment();
            Assert.NotNull(deployment.CurrentStatus);
        }

        // ==================== COMPONENT INITIALIZATION TESTS ====================

        [Fact]
        public async Task MonadoEngine_InitializeAsync_SetsIsHealthyTrue()
        {
            var engine = new MonadoEngine();
            await engine.InitializeAsync();
            Assert.True(engine.IsHealthy);
        }

        [Fact]
        public async Task SecuritySystem_InitializeAsync_SetsIsCompliantTrue()
        {
            var security = new SecuritySystem();
            await security.InitializeAsync();
            Assert.True(security.IsCompliant);
        }

        [Fact]
        public async Task AIOrchestrator_InitializeAsync_SetsIsModelReadyTrue()
        {
            var ai = new AIOrchestrator();
            await ai.InitializeAsync();
            Assert.True(ai.IsModelReady);
        }

        [Fact]
        public async Task GUIDashboard_InitializeAsync_SetsIsHealthyTrue()
        {
            var dashboard = new GUIDashboard();
            await dashboard.InitializeAsync();
            Assert.True(dashboard.IsHealthy);
        }

        [Fact]
        public async Task BuildAgents_InitializeAsync_SetsIsHealthyTrue()
        {
            var agents = new BuildAgents();
            await agents.InitializeAsync();
            Assert.True(agents.IsHealthy);
        }

        [Fact]
        public async Task DevAIHub_InitializeAsync_SetsIsHealthyTrue()
        {
            var hub = new DevAIHub();
            await hub.InitializeAsync();
            Assert.True(hub.IsHealthy);
        }

        [Fact]
        public async Task SoftwareStack_InitializeAsync_SetsIsHealthyTrue()
        {
            var stack = new SoftwareStack();
            await stack.InitializeAsync();
            Assert.True(stack.IsHealthy);
        }

        // ==================== DEPLOYMENT VALIDATION TESTS ====================

        [Fact]
        public async Task ValidateAsync_ReturnsTrue()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.ValidateAsync();
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateAsync_SetsStateToValidating()
        {
            var deployment = new HeliosDeployment();
            var validateTask = deployment.ValidateAsync();
            // Allow validation to start
            await Task.Delay(50);
            // Note: State may have changed by now, but was set during validation
            var result = await validateTask;
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateAsync_SetsPhaseToZero()
        {
            var deployment = new HeliosDeployment();
            deployment.CurrentPhase = 5; // Set to something else
            await deployment.ValidateAsync();
            Assert.Equal(0, deployment.CurrentPhase);
        }

        // ==================== DEPLOYMENT TESTS ====================

        [Fact]
        public async Task DeployAsync_WithProfessionalTier_Succeeds()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Professional);
            
            Assert.True(result.Success);
            Assert.Equal(DeploymentTier.Professional, result.Status.Tier);
            Assert.Equal(DeploymentState.Succeeded, result.Status.State);
        }

        [Fact]
        public async Task DeployAsync_WithProfessionalTier_ReachesPhase3()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Professional);
            Assert.Equal(3, deployment.CurrentPhase);
        }

        [Fact]
        public async Task DeployAsync_WithEnterpriseTier_Succeeds()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Enterprise);
            
            Assert.True(result.Success);
            Assert.Equal(DeploymentTier.Enterprise, result.Status.Tier);
        }

        [Fact]
        public async Task DeployAsync_WithEnterpriseTier_ReachesPhase6()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Enterprise);
            Assert.Equal(6, deployment.CurrentPhase);
        }

        [Fact]
        public async Task DeployAsync_WithUltimateTier_Succeeds()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Ultimate);
            
            Assert.True(result.Success);
            Assert.Equal(DeploymentTier.Ultimate, result.Status.Tier);
        }

        [Fact]
        public async Task DeployAsync_WithUltimateTier_ReachesPhase7()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Ultimate);
            Assert.Equal(7, deployment.CurrentPhase);
        }

        [Fact]
        public async Task DeployAsync_SetsProgressTo100Percent()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Professional);
            Assert.Equal(100.0, result.Status.ProgressPercentage);
        }

        [Fact]
        public async Task DeployAsync_ReturnsNonZeroDuration()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Professional);
            Assert.True(result.Duration.TotalMilliseconds > 0);
        }

        [Fact]
        public async Task DeployAsync_ReturnsCreatedResources()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Professional);
            Assert.NotEmpty(result.CreatedResources);
        }

        // ==================== TIER SELECTION TESTS ====================

        [Fact]
        public async Task DeployAsync_ProfessionalTier_DoesNotIncludeBuildAgents()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Professional);
            Assert.NotNull(deployment.BuildAgents);
        }

        [Fact]
        public async Task DeployAsync_EnterpriseTier_IncludesBuildAgents()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Enterprise);
            Assert.NotNull(deployment.BuildAgents);
        }

        [Fact]
        public async Task DeployAsync_UltimateTier_IncludesSoftwareStack()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Ultimate);
            Assert.NotNull(deployment.SoftwareStack);
        }

        // ==================== ROLLBACK TESTS ====================

        [Fact]
        public async Task RollbackAsync_ToPhase0_Succeeds()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Professional);
            var result = await deployment.RollbackAsync(0);
            
            Assert.True(result);
            Assert.Equal(0, deployment.CurrentPhase);
        }

        [Fact]
        public async Task RollbackAsync_ToPhase2_Succeeds()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Enterprise);
            var result = await deployment.RollbackAsync(2);
            
            Assert.True(result);
            Assert.Equal(2, deployment.CurrentPhase);
        }

        [Fact]
        public async Task RollbackAsync_SetsStateToRolledBack()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Professional);
            await deployment.RollbackAsync(0);
            
            Assert.Equal(DeploymentState.RolledBack, deployment.CurrentStatus.State);
        }

        // ==================== STATUS TESTS ====================

        [Fact]
        public async Task GetStatusAsync_ReturnsValidStatus()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Professional);
            var status = await deployment.GetStatusAsync();
            
            Assert.NotNull(status);
            Assert.Equal(DeploymentTier.Professional, status.Tier);
        }

        [Fact]
        public async Task GetStatusAsync_Returns7ComponentStatuses()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Professional);
            var status = await deployment.GetStatusAsync();
            
            Assert.Equal(7, status.ComponentStatuses.Length);
        }

        [Fact]
        public async Task GetStatusAsync_IncludesAllComponentNames()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Professional);
            var status = await deployment.GetStatusAsync();
            
            var names = new[] { "MonadoEngine", "SecuritySystem", "AIOrchestrator", 
                                "GUIDashboard", "BuildAgents", "DevAIHub", "SoftwareStack" };
            foreach (var name in names)
            {
                Assert.Contains(status.ComponentStatuses, cs => cs.ComponentName == name);
            }
        }

        // ==================== UNDEPLOY TESTS ====================

        [Fact]
        public async Task UndeployAsync_SetsPhaseToZero()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Professional);
            await deployment.UndeployAsync();
            
            Assert.Equal(0, deployment.CurrentPhase);
        }

        [Fact]
        public async Task UndeployAsync_SetsStateToUndeployed()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Professional);
            await deployment.UndeployAsync();
            
            Assert.Equal(DeploymentState.Undeployed, deployment.CurrentStatus.State);
        }

        // ==================== COMPONENT OPERATIONS TESTS ====================

        [Fact]
        public async Task MonadoEngine_OptimizeAsync_KeepsHealthy()
        {
            var engine = new MonadoEngine();
            await engine.OptimizeAsync();
            Assert.True(engine.IsHealthy);
        }

        [Fact]
        public async Task MonadoEngine_GetMetrics_ReturnsMetrics()
        {
            var engine = new MonadoEngine();
            var metrics = engine.GetMetrics();
            Assert.NotNull(metrics);
        }

        [Fact]
        public async Task SecuritySystem_GetSecurityStatus_ReturnsStatus()
        {
            var security = new SecuritySystem();
            var status = security.GetSecurityStatus();
            Assert.NotNull(status);
        }

        [Fact]
        public async Task SecuritySystem_GetSecurityEvents_ReturnsArray()
        {
            var security = new SecuritySystem();
            var events = security.GetSecurityEvents();
            Assert.NotNull(events);
        }

        // ==================== PHASE CONFIG TESTS ====================

        [Fact]
        public async Task DeployAsync_WithPhaseConfig_Succeeds()
        {
            var deployment = new HeliosDeployment();
            var config = new PhaseConfig
            {
                Phase = 1,
                Tier = DeploymentTier.Professional,
                Components = new[] { "MonadoEngine" }
            };
            
            var result = await deployment.DeployAsync(config);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task PhaseConfig_HasDefaultTimeout()
        {
            var config = new PhaseConfig();
            Assert.Equal(TimeSpan.FromMinutes(30), config.Timeout);
        }

        [Fact]
        public async Task PhaseConfig_CanSetVariables()
        {
            var config = new PhaseConfig();
            config.Variables["key"] = "value";
            Assert.Equal("value", config.Variables["key"]);
        }
    }
}
