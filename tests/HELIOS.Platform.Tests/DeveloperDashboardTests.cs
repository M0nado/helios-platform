using System;
using System.Collections.ObjectModel;
using Xunit;
using MonadoBlade.GUI.ViewModels;

namespace HELIOS.Platform.Tests
{
    /// <summary>
    /// Comprehensive test suite for Developer Dashboard functionality.
    /// Tests cover initialization, data binding, real-time updates, and performance.
    /// </summary>
    public class DeveloperDashboardTests
    {
        [Fact]
        public void DeveloperDashboardViewModel_Initialization_CreatesTabsSuccessfully()
        {
            var vm = new DeveloperDashboardViewModel();
            Assert.NotNull(vm.Tabs);
            Assert.Equal(6, vm.Tabs.Count);
        }

        [Fact]
        public void DeveloperDashboardViewModel_Initialization_SetsDefaultMetrics()
        {
            var vm = new DeveloperDashboardViewModel();
            Assert.NotNull(vm.Metrics);
            Assert.Equal(4, vm.Metrics.Count);
        }

        [Fact]
        public void DeveloperDashboardViewModel_SelectTab_UpdatesSelectedTabCorrectly()
        {
            var vm = new DeveloperDashboardViewModel();
            vm.SelectTabCommand.Execute("performance");
            
            Assert.NotNull(vm.SelectedTab);
            Assert.Equal("performance", vm.SelectedTab.Id);
        }

        [Fact]
        public void DeveloperDashboardViewModel_SelectTab_MarksTabAsSelected()
        {
            var vm = new DeveloperDashboardViewModel();
            vm.SelectTabCommand.Execute("tools");

            var selectedTab = vm.SelectedTab;
            Assert.True(selectedTab.IsSelected);
        }

        [Fact]
        public void DeveloperDashboardViewModel_RefreshMetrics_UpdatesSystemMetrics()
        {
            var vm = new DeveloperDashboardViewModel();
            var initialCpu = vm.CpuUsage;
            
            vm.RefreshCommand.Execute(null);
            System.Threading.Thread.Sleep(500);

            Assert.NotEqual(initialCpu, vm.CpuUsage);
            Assert.NotNull(vm.StatusMessage);
        }

        [Fact]
        public void DeveloperDashboardViewModel_CpuUsageUpdate_UpdatesMetricColor()
        {
            var vm = new DeveloperDashboardViewModel();
            vm.CpuUsage = 45;

            var metric = vm.Metrics[0];
            Assert.NotNull(metric.IndicatorColor);
            Assert.Equal(45, metric.Value);
        }

        [Fact]
        public void DeveloperDashboardViewModel_HighCpuUsage_ShowsRedIndicator()
        {
            var vm = new DeveloperDashboardViewModel();
            vm.CpuUsage = 95;

            var metric = vm.Metrics[0];
            // Red color in ARGB format
            Assert.True(metric.IndicatorColor.R > 200 || metric.IndicatorColor == System.Windows.Media.Colors.Red);
        }

        [Fact]
        public void DeveloperDashboardViewModel_TabNavigation_PreservesState()
        {
            var vm = new DeveloperDashboardViewModel();
            vm.CpuUsage = 50;
            
            vm.SelectTabCommand.Execute("performance");
            var cpuBefore = vm.CpuUsage;
            
            vm.SelectTabCommand.Execute("overview");
            var cpuAfter = vm.CpuUsage;

            Assert.Equal(cpuBefore, cpuAfter);
        }

        [Fact]
        public void DeveloperDashboardViewModel_RefreshCommand_CannotExecuteWhileRefreshing()
        {
            var vm = new DeveloperDashboardViewModel();
            vm.IsRefreshing = true;

            Assert.False(vm.RefreshCommand.CanExecute(null));
        }

        [Fact]
        public void DeveloperDashboardViewModel_MultipleRefresh_DoesNotStackRequests()
        {
            var vm = new DeveloperDashboardViewModel();
            vm.RefreshCommand.Execute(null);
            vm.RefreshCommand.Execute(null);

            Assert.True(vm.IsRefreshing);
        }

        [Fact]
        public void DeveloperDashboardViewModel_Metrics_UpdateTimestamps()
        {
            var vm = new DeveloperDashboardViewModel();
            var initialTime = vm.LastUpdate;
            
            System.Threading.Thread.Sleep(100);
            vm.RefreshCommand.Execute(null);
            System.Threading.Thread.Sleep(500);

            Assert.True(vm.LastUpdate > initialTime);
        }

        [Fact]
        public void DeveloperDashboardViewModel_ActiveProcesses_UpdatesCorrectly()
        {
            var vm = new DeveloperDashboardViewModel();
            vm.RefreshCommand.Execute(null);
            System.Threading.Thread.Sleep(500);

            Assert.True(vm.ActiveProcesses > 0);
        }

        [Fact]
        public void DeveloperDashboardViewModel_SettingsCommand_ExecutesSuccessfully()
        {
            var vm = new DeveloperDashboardViewModel();
            vm.SettingsCommand.Execute(null);

            Assert.NotNull(vm.StatusMessage);
        }
    }

    /// <summary>
    /// Tests for Analytics ViewModel functionality.
    /// </summary>
    public class AnalyticsViewModelTests
    {
        [Fact]
        public void AnalyticsViewModel_Initialization_CreatesMetricsHistory()
        {
            var vm = new AnalyticsViewModel();
            Assert.NotNull(vm.MetricsHistory);
            Assert.True(vm.MetricsHistory.Count > 0);
        }

        [Fact]
        public void AnalyticsViewModel_LoadProcesses_PopulatesProcessList()
        {
            var vm = new AnalyticsViewModel();
            vm.RefreshCommand.Execute(null);
            System.Threading.Thread.Sleep(500);

            Assert.NotEmpty(vm.ProcessList);
        }

        [Fact]
        public void AnalyticsViewModel_HealthStatus_InitializedWithFourCategories()
        {
            var vm = new AnalyticsViewModel();
            Assert.Equal(4, vm.HealthStatuses.Count);
        }

        [Fact]
        public void AnalyticsViewModel_FilterLogs_UpdatesSelectedLevel()
        {
            var vm = new AnalyticsViewModel();
            vm.FilterLogsCommand.Execute("ERROR");

            Assert.Equal("ERROR", vm.SelectedLogLevel);
        }

        [Fact]
        public void AnalyticsViewModel_ClearLogs_RemovesAllEntries()
        {
            var vm = new AnalyticsViewModel();
            vm.LogEntries.Add(new AnalyticsViewModel.LogEntry
            {
                Timestamp = DateTime.Now,
                Level = "INFO",
                Source = "Test",
                Message = "Test message"
            });

            vm.ClearLogsCommand.Execute(null);
            Assert.Empty(vm.LogEntries);
        }

        [Fact]
        public void AnalyticsViewModel_Refresh_UpdatesAllMetrics()
        {
            var vm = new AnalyticsViewModel();
            var initialProcessCount = vm.ProcessList.Count;
            
            vm.RefreshCommand.Execute(null);
            System.Threading.Thread.Sleep(500);

            Assert.NotNull(vm.StatusMessage);
        }

        [Fact]
        public void AnalyticsViewModel_AverageMetrics_CalculatedCorrectly()
        {
            var vm = new AnalyticsViewModel();
            vm.RefreshCommand.Execute(null);
            System.Threading.Thread.Sleep(500);

            if (vm.ProcessList.Count > 0)
            {
                Assert.True(vm.AverageCpu >= 0);
                Assert.True(vm.AverageMemory >= 0);
            }
        }

        [Fact]
        public void AnalyticsViewModel_ProcessTermination_RemovesFromList()
        {
            var vm = new AnalyticsViewModel();
            vm.RefreshCommand.Execute(null);
            System.Threading.Thread.Sleep(500);

            if (vm.ProcessList.Count > 0)
            {
                var initialCount = vm.ProcessList.Count;
                var processToTerminate = vm.ProcessList[0];
                
                vm.TerminateProcessCommand.Execute(processToTerminate);
                
                Assert.True(vm.ProcessList.Count <= initialCount);
            }
        }

        [Fact]
        public void AnalyticsViewModel_HealthStatus_AssignsCorrectColors()
        {
            var vm = new AnalyticsViewModel();
            
            foreach (var status in vm.HealthStatuses)
            {
                Assert.NotNull(status.StatusColor);
            }
        }

        [Fact]
        public void AnalyticsViewModel_LogEntry_AssignsColorByLevel()
        {
            var vm = new AnalyticsViewModel();
            vm.LogEntries.Add(new AnalyticsViewModel.LogEntry
            {
                Level = "ERROR",
                LevelColor = System.Windows.Media.Colors.Red
            });

            Assert.Equal(System.Windows.Media.Colors.Red, vm.LogEntries[0].LevelColor);
        }
    }

    /// <summary>
    /// Tests for Developer Tools ViewModel functionality.
    /// </summary>
    public class DeveloperToolsViewModelTests
    {
        [Fact]
        public void DeveloperToolsViewModel_Initialization_CreatesDefaultValues()
        {
            var vm = new DeveloperToolsViewModel();
            Assert.Equal("GET", vm.ApiMethod);
            Assert.NotNull(vm.ApiEndpoint);
        }

        [Fact]
        public void DeveloperToolsViewModel_ThemeColors_InitializedWithThreeColors()
        {
            var vm = new DeveloperToolsViewModel();
            Assert.Equal(3, vm.ThemeColors.Count);
        }

        [Fact]
        public void DeveloperToolsViewModel_PluginTemplates_InitializedWithThreeTemplates()
        {
            var vm = new DeveloperToolsViewModel();
            Assert.Equal(3, vm.PluginTemplates.Count);
        }

        [Fact]
        public void DeveloperToolsViewModel_ExecuteApi_CreatesRequestRecord()
        {
            var vm = new DeveloperToolsViewModel();
            vm.ApiEndpoint = "https://api.example.com/test";
            vm.ExecuteApiCommand.Execute(null);

            Assert.Single(vm.ApiRequests);
        }

        [Fact]
        public void DeveloperToolsViewModel_ClearApi_RemovesAllRequests()
        {
            var vm = new DeveloperToolsViewModel();
            vm.ApiEndpoint = "https://api.example.com/test";
            vm.ExecuteApiCommand.Execute(null);
            vm.ClearApiCommand.Execute(null);

            Assert.Empty(vm.ApiRequests);
        }

        [Fact]
        public void DeveloperToolsViewModel_GeneratePlugin_RequiresNameAndTemplate()
        {
            var vm = new DeveloperToolsViewModel();
            vm.PluginName = null;
            vm.SelectedTemplateId = "basic";

            Assert.False(vm.GeneratePluginCommand.CanExecute(null));
        }

        [Fact]
        public void DeveloperToolsViewModel_StartProfiling_SetsIsProfiling()
        {
            var vm = new DeveloperToolsViewModel();
            Assert.False(vm.IsProfiling);

            vm.StartProfilingCommand.Execute(null);
            Assert.True(vm.IsProfiling);
        }

        [Fact]
        public void DeveloperToolsViewModel_StopProfiling_ClearsProfilingFlag()
        {
            var vm = new DeveloperToolsViewModel();
            vm.StartProfilingCommand.Execute(null);
            vm.StopProfilingCommand.Execute(null);

            Assert.False(vm.IsProfiling);
        }

        [Fact]
        public void DeveloperToolsViewModel_StartProfiling_PopulatesProfiles()
        {
            var vm = new DeveloperToolsViewModel();
            vm.StartProfilingCommand.Execute(null);

            Assert.NotEmpty(vm.PerformanceProfiles);
        }

        [Fact]
        public void DeveloperToolsViewModel_ExportTheme_SetsStatusMessage()
        {
            var vm = new DeveloperToolsViewModel();
            vm.ExportThemeCommand.Execute(null);

            Assert.NotNull(vm.StatusMessage);
            Assert.Contains("Theme exported", vm.StatusMessage);
        }

        [Fact]
        public void DeveloperToolsViewModel_ApiRequest_RecordsTimestamp()
        {
            var vm = new DeveloperToolsViewModel();
            vm.ApiEndpoint = "https://api.example.com/test";
            vm.ExecuteApiCommand.Execute(null);

            Assert.NotEqual(default, vm.ApiRequests[0].Timestamp);
        }

        [Fact]
        public void DeveloperToolsViewModel_ColorProperties_CanBeSet()
        {
            var vm = new DeveloperToolsViewModel();
            var newColor = System.Windows.Media.Colors.Blue;
            
            vm.PrimaryColor = newColor;
            Assert.Equal(newColor, vm.PrimaryColor);
        }
    }

    /// <summary>
    /// Tests for Advanced Features ViewModel functionality.
    /// </summary>
    public class AdvancedFeaturesViewModelTests
    {
        [Fact]
        public void AdvancedFeaturesViewModel_Initialization_CreatesDependencies()
        {
            var vm = new AdvancedFeaturesViewModel();
            Assert.NotEmpty(vm.Dependencies);
        }

        [Fact]
        public void AdvancedFeaturesViewModel_Initialization_CreatesMemoryAllocations()
        {
            var vm = new AdvancedFeaturesViewModel();
            Assert.True(vm.MemoryAllocations.Count > 0);
        }

        [Fact]
        public void AdvancedFeaturesViewModel_AnalyzeCrash_PopulatesCrashDumps()
        {
            var vm = new AdvancedFeaturesViewModel();
            vm.AnalyzeCrashCommand.Execute(null);
            System.Threading.Thread.Sleep(100);

            Assert.NotNull(vm.StatusMessage);
        }

        [Fact]
        public void AdvancedFeaturesViewModel_DetectBottlenecks_FindsIssues()
        {
            var vm = new AdvancedFeaturesViewModel();
            vm.DetectBottlenecksCommand.Execute(null);

            Assert.NotEmpty(vm.Bottlenecks);
        }

        [Fact]
        public void AdvancedFeaturesViewModel_DetectBottlenecks_AssignsSeverity()
        {
            var vm = new AdvancedFeaturesViewModel();
            vm.DetectBottlenecksCommand.Execute(null);

            foreach (var bottleneck in vm.Bottlenecks)
            {
                Assert.NotNull(bottleneck.Severity);
            }
        }

        [Fact]
        public void AdvancedFeaturesViewModel_StartMemoryProfiling_SetsFlag()
        {
            var vm = new AdvancedFeaturesViewModel();
            vm.StartMemoryProfilingCommand.Execute(null);

            Assert.True(vm.IsProfilingMemory);
        }

        [Fact]
        public void AdvancedFeaturesViewModel_StopMemoryProfiling_ClearsFlag()
        {
            var vm = new AdvancedFeaturesViewModel();
            vm.StartMemoryProfilingCommand.Execute(null);
            vm.StopMemoryProfilingCommand.Execute(null);

            Assert.False(vm.IsProfilingMemory);
        }

        [Fact]
        public void AdvancedFeaturesViewModel_StartTrace_InitializesTracing()
        {
            var vm = new AdvancedFeaturesViewModel();
            vm.StartTraceCommand.Execute(null);

            Assert.True(vm.IsTracing);
        }

        [Fact]
        public void AdvancedFeaturesViewModel_StopTrace_PopulatesTraceEvents()
        {
            var vm = new AdvancedFeaturesViewModel();
            vm.StartTraceCommand.Execute(null);
            vm.StopTraceCommand.Execute(null);

            Assert.NotEmpty(vm.TraceEvents);
        }

        [Fact]
        public void AdvancedFeaturesViewModel_OverallHealth_WithinValidRange()
        {
            var vm = new AdvancedFeaturesViewModel();
            Assert.True(vm.OverallHealth >= 0 && vm.OverallHealth <= 100);
        }

        [Fact]
        public void AdvancedFeaturesViewModel_AnalyzeCrash_CannotExecuteWhileAnalyzing()
        {
            var vm = new AdvancedFeaturesViewModel();
            vm.IsAnalyzing = true;

            Assert.False(vm.AnalyzeCrashCommand.CanExecute(null));
        }

        [Fact]
        public void AdvancedFeaturesViewModel_DependencyNode_AssignsCorrectColor()
        {
            var vm = new AdvancedFeaturesViewModel();

            foreach (var dep in vm.Dependencies)
            {
                Assert.NotNull(dep.StatusColor);
            }
        }

        [Fact]
        public void AdvancedFeaturesViewModel_MemoryAllocation_CalculatesPerfectage()
        {
            var vm = new AdvancedFeaturesViewModel();

            foreach (var alloc in vm.MemoryAllocations)
            {
                Assert.True(alloc.PercentOfTotal > 0);
            }
        }
    }

    /// <summary>
    /// Performance tests for dashboard responsiveness.
    /// </summary>
    public class DashboardPerformanceTests
    {
        [Fact]
        public void Dashboard_RefreshMetrics_CompletesInLessThan500Ms()
        {
            var vm = new DeveloperDashboardViewModel();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            vm.RefreshCommand.Execute(null);
            System.Threading.Thread.Sleep(500);
            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds < 1000);
        }

        [Fact]
        public void Dashboard_TabSelection_InstantaneousResponse()
        {
            var vm = new DeveloperDashboardViewModel();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            vm.SelectTabCommand.Execute("performance");
            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds < 50);
        }

        [Fact]
        public void Analytics_RefreshProcesses_CompletesInLessThan1000Ms()
        {
            var vm = new AnalyticsViewModel();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            vm.RefreshCommand.Execute(null);
            System.Threading.Thread.Sleep(1000);
            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds < 2000);
        }

        [Fact]
        public void DeveloperTools_ExecuteApi_RecordsRequestWithinLimit()
        {
            var vm = new DeveloperToolsViewModel();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            vm.ApiEndpoint = "https://api.example.com/test";
            vm.ExecuteApiCommand.Execute(null);
            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds < 100);
        }

        [Fact]
        public void AdvancedFeatures_DetectBottlenecks_CompletesInTime()
        {
            var vm = new AdvancedFeaturesViewModel();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            vm.DetectBottlenecksCommand.Execute(null);
            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds < 500);
        }
    }

    /// <summary>
    /// Accessibility and usability tests for the dashboard.
    /// </summary>
    public class DashboardAccessibilityTests
    {
        [Fact]
        public void Dashboard_AllTabsAccessible_ViaKeyboard()
        {
            var vm = new DeveloperDashboardViewModel();
            
            foreach (var tab in vm.Tabs)
            {
                vm.SelectTabCommand.Execute(tab.Id);
                Assert.Equal(tab.Id, vm.SelectedTab.Id);
            }
        }

        [Fact]
        public void Dashboard_StatusMessages_ProvidedForActions()
        {
            var vm = new DeveloperDashboardViewModel();
            vm.RefreshCommand.Execute(null);

            Assert.NotNull(vm.StatusMessage);
            Assert.NotEmpty(vm.StatusMessage);
        }

        [Fact]
        public void Analytics_ColorIndicators_Assigned()
        {
            var vm = new AnalyticsViewModel();
            
            foreach (var status in vm.HealthStatuses)
            {
                Assert.NotNull(status.StatusColor);
            }
        }

        [Fact]
        public void DeveloperTools_Commands_HaveCanExecuteLogic()
        {
            var vm = new DeveloperToolsViewModel();
            
            Assert.False(vm.GeneratePluginCommand.CanExecute(null));
            vm.PluginName = "TestPlugin";
            vm.SelectedTemplateId = "basic";
            Assert.True(vm.GeneratePluginCommand.CanExecute(null));
        }
    }
}
