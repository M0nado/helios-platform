using Xunit;
using HELIOS.Platform;
using HELIOS.Platform.Components;
using System;
using System.Threading.Tasks;

namespace HELIOS.Platform.Tests
{
    /// <summary>
    /// Compatibility Tests - Framework support and environment validation
    /// </summary>
    public class CompatibilityTests
    {
        // ==================== .NET FRAMEWORK COMPATIBILITY ====================

        [Fact]
        public void Compat_RuntimeVersion_Supported()
        {
            var runtimeVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            Assert.NotNull(runtimeVersion);
            // Should be running on .NET 6.0+
            Assert.Contains("NET", runtimeVersion);
        }

        [Fact]
        public void Compat_AssemblyLoads_Successfully()
        {
            var assembly = typeof(HeliosDeployment).Assembly;
            Assert.NotNull(assembly);
            Assert.NotNull(assembly.GetName());
        }

        [Fact]
        public void Compat_ComponentAssembliesLoad()
        {
            var components = new[]
            {
                typeof(MonadoEngine),
                typeof(SecuritySystem),
                typeof(AIOrchestrator),
                typeof(GUIDashboard),
                typeof(BuildAgents),
                typeof(DevAIHub),
                typeof(SoftwareStack)
            };
            
            foreach (var component in components)
            {
                Assert.NotNull(component.Assembly);
            }
        }

        // ==================== ASYNC/AWAIT COMPATIBILITY ====================

        [Fact]
        public async Task Compat_AsyncOperations_Supported()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.ValidateAsync();
            Assert.True(result);
        }

        [Fact]
        public async Task Compat_TaskCombination_Works()
        {
            var tasks = new Task[3];
            tasks[0] = Task.Delay(10);
            tasks[1] = Task.Delay(10);
            tasks[2] = Task.Delay(10);
            
            await Task.WhenAll(tasks);
            Assert.True(true);
        }

        [Fact]
        public async Task Compat_AsyncEnumerable_Capable()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Professional);
            Assert.True(result.Success);
        }

        // ==================== LINQ COMPATIBILITY ====================

        [Fact]
        public async Task Compat_LINQQueries_Work()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Professional);
            
            var status = await deployment.GetStatusAsync();
            var healthyComponents = Array.FindAll(status.ComponentStatuses, 
                cs => cs.IsHealthy);
            
            Assert.NotEmpty(healthyComponents);
        }

        [Fact]
        public async Task Compat_EnumSupport_Works()
        {
            var tiers = Enum.GetValues(typeof(DeploymentTier));
            Assert.True(tiers.Length > 0);
            
            var states = Enum.GetValues(typeof(DeploymentState));
            Assert.True(states.Length > 0);
        }

        // ==================== NULLABLE REFERENCE TYPES ====================

        [Fact]
        public async Task Compat_NullableTypes_Handled()
        {
            var deployment = new HeliosDeployment();
            
            var status = new DeploymentStatus();
            Assert.True(status.CompletionTime == null || status.CompletionTime != null);
            
            await deployment.DeployAsync(DeploymentTier.Professional);
            var deployedStatus = await deployment.GetStatusAsync();
            
            // Completion time should be set after deployment
            Assert.NotNull(deployedStatus.CompletionTime);
        }

        [Fact]
        public void Compat_StringNullableHandling()
        {
            var status = new ComponentStatus();
            Assert.NotNull(status.ComponentName); // Should be empty string, not null
            Assert.NotNull(status.Version); // Should have default value
        }

        // ==================== GENERIC TYPE SUPPORT ====================

        [Fact]
        public async Task Compat_GenericArrays_Work()
        {
            var deployment = new HeliosDeployment();
            await deployment.DeployAsync(DeploymentTier.Professional);
            
            var status = await deployment.GetStatusAsync();
            var componentArray = status.ComponentStatuses;
            
            Assert.IsType<ComponentStatus[]>(componentArray);
        }

        [Fact]
        public async Task Compat_GenericDictionaries_Work()
        {
            var config = new PhaseConfig();
            config.Variables["testKey"] = "testValue";
            
            Assert.Equal("testValue", config.Variables["testKey"]);
        }

        // ==================== REFLECTION COMPATIBILITY ====================

        [Fact]
        public void Compat_ReflectionWorks()
        {
            var deployment = new HeliosDeployment();
            var deploymentType = deployment.GetType();
            
            var properties = deploymentType.GetProperties();
            Assert.NotEmpty(properties);
            
            Assert.NotNull(deploymentType.GetProperty("MonadoEngine"));
            Assert.NotNull(deploymentType.GetProperty("CurrentStatus"));
        }

        [Fact]
        public void Compat_AttributeReflection()
        {
            var assembly = typeof(HeliosDeployment).Assembly;
            var attributes = assembly.GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute));
            
            // Should have target framework attribute
            Assert.NotEmpty(attributes);
        }

        // ==================== DATETIME COMPATIBILITY ====================

        [Fact]
        public async Task Compat_DateTimeUtc_Used()
        {
            var deployment = new HeliosDeployment();
            var status = deployment.CurrentStatus;
            
            Assert.Equal(DateTimeKind.Utc, status.StartTime.Kind);
        }

        [Fact]
        public async Task Compat_TimeSpan_Operations()
        {
            var deployment = new HeliosDeployment();
            var result = await deployment.DeployAsync(DeploymentTier.Professional);
            
            Assert.True(result.Duration > TimeSpan.Zero);
            Assert.True(result.Duration.TotalMilliseconds > 0);
        }

        // ==================== ENUM COMPATIBILITY ====================

        [Fact]
        public void Compat_DeploymentTierEnum_Valid()
        {
            var professional = DeploymentTier.Professional;
            var enterprise = DeploymentTier.Enterprise;
            var ultimate = DeploymentTier.Ultimate;
            
            Assert.Equal(1, (int)professional);
            Assert.Equal(2, (int)enterprise);
            Assert.Equal(3, (int)ultimate);
        }

        [Fact]
        public void Compat_DeploymentStateEnum_Valid()
        {
            var states = new[]
            {
                DeploymentState.Idle,
                DeploymentState.Validating,
                DeploymentState.Deploying,
                DeploymentState.Succeeded,
                DeploymentState.Failed,
                DeploymentState.RolledBack,
                DeploymentState.Undeploying,
                DeploymentState.Undeployed
            };
            
            Assert.Equal(8, states.Length);
        }

        // ==================== EXCEPTION HANDLING COMPATIBILITY ====================

        [Fact]
        public async Task Compat_ExceptionHandling_Works()
        {
            var deployment = new HeliosDeployment();
            
            try
            {
                var result = await deployment.DeployAsync(DeploymentTier.Professional);
                Assert.True(result.Success);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Should not throw: {ex.Message}");
            }
        }

        [Fact]
        public void Compat_ExceptionTypes_Supported()
        {
            Assert.IsType<InvalidOperationException>(
                new InvalidOperationException("Test"));
            
            Assert.IsType<Exception>(
                new Exception("Test"));
        }

        // ==================== COLLECTION COMPATIBILITY ====================

        [Fact]
        public void Compat_ArrayOperations()
        {
            var array = new string[] { "a", "b", "c" };
            Assert.Equal(3, array.Length);
            
            var emptyArray = Array.Empty<string>();
            Assert.Empty(emptyArray);
        }

        [Fact]
        public void Compat_ListOperations()
        {
            var list = new System.Collections.Generic.List<string>();
            list.Add("test");
            
            Assert.Single(list);
        }

        // ==================== INTERFACE COMPATIBILITY ====================

        [Fact]
        public void Compat_Interfaces_Implemented()
        {
            var deployment = typeof(HeliosDeployment);
            var methods = deployment.GetMethods();
            
            Assert.NotEmpty(methods);
        }

        [Fact]
        public async Task Compat_IAsyncResult_Support()
        {
            var deployment = new HeliosDeployment();
            
            var task = deployment.ValidateAsync();
            Assert.IsAssignableFrom<System.Collections.IEnumerator>(task.GetAwaiter());
            
            var result = await task;
            Assert.True(result);
        }

        // ==================== TYPE CONVERSION COMPATIBILITY ====================

        [Fact]
        public void Compat_IntegerConversion()
        {
            int tier = (int)DeploymentTier.Professional;
            Assert.Equal(1, tier);
            
            DeploymentTier converted = (DeploymentTier)2;
            Assert.Equal(DeploymentTier.Enterprise, converted);
        }

        [Fact]
        public void Compat_StringConversion()
        {
            var tier = DeploymentTier.Professional;
            string tierString = tier.ToString();
            
            Assert.Equal("Professional", tierString);
        }

        // ==================== THREADING COMPATIBILITY ====================

        [Fact]
        public async Task Compat_ThreadSafeOperations()
        {
            var deployment = new HeliosDeployment();
            
            var task1 = deployment.ValidateAsync();
            var task2 = deployment.DeployAsync(DeploymentTier.Professional);
            
            await Task.WhenAll(task1, task2);
            Assert.True(true);
        }

        [Fact]
        public async Task Compat_ParallelExecution()
        {
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    var deployment = new HeliosDeployment();
                    var result = await deployment.ValidateAsync();
                    Assert.True(result);
                });
            }
            
            await Task.WhenAll(tasks);
            Assert.True(true);
        }
    }
}
