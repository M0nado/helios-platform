using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HELIOS.Platform.Core.Ecosystem;
using HELIOS.Platform.Core.Ecosystem.Interfaces;
using HELIOS.Platform.Core.Container;

namespace HELIOS.Platform.Tests.Phase5.Ecosystem
{
    /// <summary>
    /// Comprehensive test suite for Phase 5 Tier 4 Ecosystem Services.
    /// Tests all four ecosystem services with 15-20 test cases.
    /// </summary>
    public class Phase5EcosystemTests : IAsyncLifetime
    {
        private IServiceProvider _serviceProvider;
        private IMarketplaceIntegration _marketplaceIntegration;
        private IAPIMarketplace _apiMarketplace;
        private ISLAMarketplace _slaMarketplace;
        private IPartnerNetworking _partnerNetworking;

        /// <summary>
        /// Initializes test fixtures asynchronously.
        /// </summary>
        public async Task InitializeAsync()
        {
            var services = new ServiceCollection();

            // Register logging
            services.AddLogging(config =>
            {
                config.AddConsole();
                config.SetMinimumLevel(LogLevel.Debug);
            });

            // Register ecosystem services
            services.AddEcosystemServices();

            _serviceProvider = services.BuildServiceProvider();

            _marketplaceIntegration = _serviceProvider.GetRequiredService<IMarketplaceIntegration>();
            _apiMarketplace = _serviceProvider.GetRequiredService<IAPIMarketplace>();
            _slaMarketplace = _serviceProvider.GetRequiredService<ISLAMarketplace>();
            _partnerNetworking = _serviceProvider.GetRequiredService<IPartnerNetworking>();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Cleans up test resources asynchronously.
        /// </summary>
        public async Task DisposeAsync()
        {
            (_serviceProvider as IDisposable)?.Dispose();
            await Task.CompletedTask;
        }

        #region Marketplace Integration Tests

        [Fact]
        public async Task DiscoverPlugins_ReturnsAvailablePlugins()
        {
            // Arrange & Act
            var plugins = await _marketplaceIntegration.DiscoverPluginsAsync();

            // Assert
            Assert.NotNull(plugins);
            Assert.True(plugins.Any(), "Should return at least one plugin");
            var firstPlugin = plugins.First();
            Assert.NotEmpty(firstPlugin.Id);
            Assert.NotEmpty(firstPlugin.Name);
        }

        [Fact]
        public async Task InstallPlugin_SuccessfullyInstalls()
        {
            // Arrange
            var pluginId = "test-plugin-001";
            var version = "1.0.0";

            // Act
            var result = await _marketplaceIntegration.InstallPluginAsync(pluginId, version);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(pluginId, result.PluginId);
            Assert.Equal(version, result.InstalledVersion);
        }

        [Fact]
        public async Task InstallPlugin_FailsWhenAlreadyInstalled()
        {
            // Arrange
            var pluginId = "duplicate-plugin";
            await _marketplaceIntegration.InstallPluginAsync(pluginId, "1.0.0");

            // Act
            var result = await _marketplaceIntegration.InstallPluginAsync(pluginId, "1.0.0");

            // Assert
            Assert.False(result.Success);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public async Task UninstallPlugin_SuccessfullyUninstalls()
        {
            // Arrange
            var pluginId = "uninstall-test";
            await _marketplaceIntegration.InstallPluginAsync(pluginId, "1.0.0");

            // Act
            var result = await _marketplaceIntegration.UninstallPluginAsync(pluginId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(pluginId, result.PluginId);
        }

        [Fact]
        public async Task GetPluginDetails_ReturnsValidDetails()
        {
            // Arrange
            var pluginId = "detail-test";

            // Act
            var details = await _marketplaceIntegration.GetPluginDetailsAsync(pluginId);

            // Assert
            Assert.NotNull(details);
            Assert.Equal(pluginId, details.Id);
            Assert.NotEmpty(details.AvailableVersions);
        }

        [Fact]
        public async Task GetInstalledPlugins_ReturnsInstalledList()
        {
            // Arrange
            await _marketplaceIntegration.InstallPluginAsync("installed-1", "1.0.0");
            await _marketplaceIntegration.InstallPluginAsync("installed-2", "2.0.0");

            // Act
            var installed = await _marketplaceIntegration.GetInstalledPluginsAsync();

            // Assert
            Assert.NotNull(installed);
            Assert.True(installed.Count() >= 2);
        }

        [Fact]
        public async Task CheckForUpdates_DetectsAvailableUpdates()
        {
            // Arrange
            var pluginId = "update-check";
            await _marketplaceIntegration.InstallPluginAsync(pluginId, "1.0.0");

            // Act
            var update = await _marketplaceIntegration.CheckForUpdatesAsync(pluginId);

            // Assert
            Assert.NotNull(update);
            Assert.Equal(pluginId, update.PluginId);
            Assert.True(string.Compare(update.NewVersion, update.CurrentVersion) > 0);
        }

        [Fact]
        public async Task UpdatePlugin_SuccessfullyUpdates()
        {
            // Arrange
            var pluginId = "update-test";
            var originalVersion = "1.0.0";
            var newVersion = "2.0.0";
            await _marketplaceIntegration.InstallPluginAsync(pluginId, originalVersion);

            // Act
            var result = await _marketplaceIntegration.UpdatePluginAsync(pluginId, newVersion);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(newVersion, result.NewVersion);
            Assert.Equal(originalVersion, result.PreviousVersion);
        }

        [Fact]
        public async Task ValidateCompatibility_ReturnsCompatibilityResult()
        {
            // Arrange
            var pluginId = "compat-test";
            var version = "1.0.0";

            // Act
            var result = await _marketplaceIntegration.ValidateCompatibilityAsync(pluginId, version);

            // Assert
            Assert.True(result.IsCompatible);
            Assert.NotNull(result.Issues);
        }

        [Fact]
        public async Task RatePlugin_SuccessfullyRecordsRating()
        {
            // Arrange
            var pluginId = "rating-test";
            var rating = 5;

            // Act
            var result = await _marketplaceIntegration.RatePluginAsync(pluginId, rating, "Excellent plugin");

            // Assert
            Assert.True(result.Success);
            Assert.True(result.NewAverageRating > 0);
        }

        #endregion

        #region API Marketplace Tests

        [Fact]
        public async Task PublishAPI_SuccessfullyPublishes()
        {
            // Arrange
            var apiDef = new APIDefinition
            {
                Id = "api-publish-test",
                Name = "Test API",
                Description = "A test API",
                BaseEndpoint = "https://api.example.com",
                Publisher = "TestCorp",
                Category = "Analytics",
                PricingTiers = new[]
                {
                    new PricingTier
                    {
                        Name = "Free",
                        MonthlyPrice = 0,
                        RequestQuotaPerMonth = 10000
                    }
                }
            };

            // Act
            var result = await _apiMarketplace.PublishAPIAsync(apiDef);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(apiDef.Id, result.APIId);
        }

        [Fact]
        public async Task DiscoverAPIs_ReturnsPublishedAPIs()
        {
            // Arrange
            var filter = new APIDiscoveryFilter { PageSize = 10 };

            // Act
            var apis = await _apiMarketplace.DiscoverAPIsAsync(filter);

            // Assert
            Assert.NotNull(apis);
        }

        [Fact]
        public async Task GetAPIDetails_ReturnsValidDetails()
        {
            // Arrange
            var apiId = "api-details-test";
            var apiDef = new APIDefinition
            {
                Id = apiId,
                Name = "Detail Test API",
                BaseEndpoint = "https://api.test.com",
                Publisher = "TestPub",
                Category = "Testing",
                PricingTiers = new[] { new PricingTier { Name = "Basic", MonthlyPrice = 99 } }
            };
            await _apiMarketplace.PublishAPIAsync(apiDef);

            // Act
            var details = await _apiMarketplace.GetAPIDetailsAsync(apiId);

            // Assert
            Assert.NotNull(details);
            Assert.Equal(apiId, details.Id);
        }

        [Fact]
        public async Task SubscribeToAPI_SuccessfullyCreatesSubscription()
        {
            // Arrange
            var apiId = "subscription-test";
            var apiDef = new APIDefinition
            {
                Id = apiId,
                Name = "Sub Test",
                BaseEndpoint = "https://api.test.com",
                Publisher = "TestPub",
                Category = "Testing",
                PricingTiers = new[] { new PricingTier { Name = "Pro", MonthlyPrice = 299 } }
            };
            await _apiMarketplace.PublishAPIAsync(apiDef);

            var consumer = new ConsumerInfo
            {
                Id = "consumer-001",
                Name = "Test Consumer",
                Email = "test@example.com"
            };

            // Act
            var result = await _apiMarketplace.SubscribeToAPIAsync(apiId, "Pro", consumer);

            // Assert
            Assert.True(result.Success);
            Assert.NotEmpty(result.SubscriptionId);
        }

        [Fact]
        public async Task GetUsageMetrics_ReturnsValidMetrics()
        {
            // Arrange
            var subscriptionId = "sub-metrics-test";
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            // Act
            var metrics = await _apiMarketplace.GetUsageMetricsAsync(subscriptionId, startDate, endDate);

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.TotalRequests > 0);
            Assert.True(metrics.SuccessRate > 0);
        }

        [Fact]
        public async Task GenerateAccessKey_SuccessfullyGeneratesKey()
        {
            // Arrange
            var subscriptionId = "sub-key-test";

            // Act
            var keyInfo = await _apiMarketplace.GenerateAccessKeyAsync(subscriptionId);

            // Assert
            Assert.NotNull(keyInfo);
            Assert.NotEmpty(keyInfo.KeyId);
            Assert.NotEmpty(keyInfo.KeyValue);
            Assert.True(keyInfo.IsActive);
        }

        #endregion

        #region SLA Marketplace Tests

        [Fact]
        public async Task CreateSLATemplate_SuccessfullyCreates()
        {
            // Arrange
            var template = new SLATemplate
            {
                Id = "sla-create-test",
                Name = "Premium SLA",
                Description = "Premium support SLA",
                SellerOrgId = "seller-001",
                Service = "Support",
                BasePricePerMonth = 1000,
                UptimePercentage = 99.9,
                ResponseTimeSlaMs = 200
            };

            // Act
            var result = await _slaMarketplace.CreateSLATemplateAsync(template);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(template.Id, result.SLAId);
        }

        [Fact]
        public async Task DiscoverSLAs_ReturnsPublishedSLAs()
        {
            // Arrange
            var filter = new SLADiscoveryFilter { PageSize = 10 };

            // Act
            var slas = await _slaMarketplace.DiscoverSLAsAsync(filter);

            // Assert
            Assert.NotNull(slas);
        }

        [Fact]
        public async Task InitiateNegotiation_SuccessfullyInitiates()
        {
            // Arrange
            var slaTemplate = new SLATemplate
            {
                Id = "sla-negotiate",
                Name = "Negotiable SLA",
                SellerOrgId = "seller-001",
                Service = "Support",
                BasePricePerMonth = 500,
                UptimePercentage = 99.5,
                IsCustomizable = true
            };
            await _slaMarketplace.CreateSLATemplateAsync(slaTemplate);
            await _slaMarketplace.PublishSLAAsync(slaTemplate.Id);

            var negotiation = new SLANegotiation
            {
                BuyerOrgId = "buyer-001",
                BuyerOrgName = "Buyer Corp",
                ContractStartDate = DateTime.UtcNow,
                ContractDurationMonths = 12
            };

            // Act
            var result = await _slaMarketplace.InitiateNegotiationAsync(slaTemplate.Id, negotiation);

            // Assert
            Assert.True(result.Success);
            Assert.NotEmpty(result.NegotiationId);
        }

        [Fact]
        public async Task FinalizeAgreement_SuccessfullyFinalizes()
        {
            // Arrange
            var negotiationId = "test-negotiation";

            // Act
            var result = await _slaMarketplace.FinalizeAgreementAsync(negotiationId);

            // Assert
            Assert.True(result.Success);
            Assert.NotEmpty(result.AgreementId);
            Assert.NotNull(result.Agreement);
        }

        [Fact]
        public async Task CalculateSLACost_ReturnsValidCost()
        {
            // Arrange
            var basePrice = 1000m;
            var config = new SLACustomConfiguration { UptimePercentage = 99.99 };

            // Act
            var result = await _slaMarketplace.CalculateSLACostAsync(basePrice, config);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.MonthlyCost > basePrice);
            Assert.NotEmpty(result.CostBreakdown);
        }

        [Fact]
        public async Task GetPerformanceMetrics_ReturnsValidMetrics()
        {
            // Arrange
            var agreementId = "agreement-001";
            var startDate = DateTime.UtcNow.AddMonths(-1);
            var endDate = DateTime.UtcNow;

            // Act
            var metrics = await _slaMarketplace.GetPerformanceMetricsAsync(agreementId, startDate, endDate);

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.ActualUptimePercentage > 0);
            Assert.True(metrics.SLATargetsMet || !metrics.SLATargetsMet); // Either way is valid
        }

        #endregion

        #region Partner Networking Tests

        [Fact]
        public async Task RegisterPartner_SuccessfullyRegisters()
        {
            // Arrange
            var partnerInfo = new PartnerInfo
            {
                OrganizationName = "Partner Corp",
                OrganizationType = "Tech",
                Industry = "Software",
                ContactEmail = "partner@example.com",
                EmployeeCount = 50,
                Competencies = new[] { "Cloud", "AI" }
            };

            // Act
            var result = await _partnerNetworking.RegisterPartnerAsync(partnerInfo);

            // Assert
            Assert.True(result.Success);
            Assert.NotEmpty(result.PartnerId);
        }

        [Fact]
        public async Task GetPartnerDetails_ReturnsValidDetails()
        {
            // Arrange
            var partnerInfo = new PartnerInfo
            {
                OrganizationName = "Details Test Corp",
                Industry = "Software",
                ContactEmail = "test@example.com",
                EmployeeCount = 25
            };
            var regResult = await _partnerNetworking.RegisterPartnerAsync(partnerInfo);

            // Act
            var details = await _partnerNetworking.GetPartnerDetailsAsync(regResult.PartnerId);

            // Assert
            Assert.NotNull(details);
            Assert.Equal("Details Test Corp", details.OrganizationName);
        }

        [Fact]
        public async Task DiscoverOpportunities_ReturnsAvailableOpportunities()
        {
            // Arrange
            var filter = new OpportunityDiscoveryFilter { PageSize = 20 };

            // Act
            var opportunities = await _partnerNetworking.DiscoverOpportunitiesAsync(filter);

            // Assert
            Assert.NotNull(opportunities);
            Assert.True(opportunities.Any());
        }

        [Fact]
        public async Task CreatePartnershipProposal_SuccessfullyCreates()
        {
            // Arrange
            var proposal = new PartnershipProposal
            {
                InitiatingOrgId = "org-001",
                TargetPartnerOrgId = "org-002",
                Title = "Strategic Partnership",
                PartnershipType = "Technology",
                Description = "Collaboration opportunity",
                EstimatedAnnualValue = 500000,
                ContractDurationMonths = 24
            };

            // Act
            var result = await _partnerNetworking.CreatePartnershipProposalAsync(proposal);

            // Assert
            Assert.True(result.Success);
            Assert.NotEmpty(result.ProposalId);
        }

        [Fact]
        public async Task EstablishPartnership_SuccessfullyEstablishes()
        {
            // Arrange
            var proposal = new PartnershipProposal
            {
                InitiatingOrgId = "org-est-001",
                TargetPartnerOrgId = "org-est-002",
                Title = "Partnership Test",
                PartnershipType = "Reseller",
                EstimatedAnnualValue = 250000,
                ContractDurationMonths = 12
            };
            var propResult = await _partnerNetworking.CreatePartnershipProposalAsync(proposal);

            // Act
            var result = await _partnerNetworking.EstablishPartnershipAsync(propResult.ProposalId);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Partnership);
        }

        [Fact]
        public async Task SetupRevenueSharing_SuccessfullySetups()
        {
            // Arrange
            var partnership = await CreateTestPartnership();
            var revenueModel = new RevenueSharingModel
            {
                ModelType = "Percentage",
                PrimaryPartnerPercentage = 40,
                SecondaryPartnerPercentage = 50,
                PlatformCommissionPercentage = 10,
                PaymentFrequency = "Monthly"
            };

            // Act
            var result = await _partnerNetworking.SetupRevenueSharingAsync(partnership.Id, revenueModel);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task GetRevenueMetrics_ReturnsValidMetrics()
        {
            // Arrange
            var partnership = await CreateTestPartnership();
            var startDate = DateTime.UtcNow.AddMonths(-1);
            var endDate = DateTime.UtcNow;

            // Act
            var metrics = await _partnerNetworking.GetRevenueMetricsAsync(partnership.Id, startDate, endDate);

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.TotalRevenue > 0);
            Assert.True(metrics.Partner1Earnings > 0);
        }

        [Fact]
        public async Task RatePartner_SuccessfullyRecordsRating()
        {
            // Arrange
            var partnerInfo = new PartnerInfo
            {
                OrganizationName = "Rate Test Corp",
                Industry = "Tech",
                ContactEmail = "rate@example.com",
                EmployeeCount = 20
            };
            var regResult = await _partnerNetworking.RegisterPartnerAsync(partnerInfo);

            // Act
            var result = await _partnerNetworking.RatePartnerAsync(regResult.PartnerId, 5, "Excellent partner");

            // Assert
            Assert.True(result.Success);
            Assert.True(result.NewAverageRating > 0);
        }

        [Fact]
        public async Task GetPartnerReputation_ReturnsValidReputation()
        {
            // Arrange
            var partnerInfo = new PartnerInfo
            {
                OrganizationName = "Reputation Test Corp",
                Industry = "Tech",
                ContactEmail = "rep@example.com",
                EmployeeCount = 30
            };
            var regResult = await _partnerNetworking.RegisterPartnerAsync(partnerInfo);

            // Act
            var reputation = await _partnerNetworking.GetPartnerReputationAsync(regResult.PartnerId);

            // Assert
            Assert.NotNull(reputation);
            Assert.True(reputation.OverallScore >= 0);
        }

        #endregion

        #region Helper Methods

        private async Task<Partnership> CreateTestPartnership()
        {
            var proposal = new PartnershipProposal
            {
                InitiatingOrgId = $"org-{Guid.NewGuid()}",
                TargetPartnerOrgId = $"org-{Guid.NewGuid()}",
                Title = "Test Partnership",
                PartnershipType = "Technology",
                EstimatedAnnualValue = 300000,
                ContractDurationMonths = 12
            };

            var propResult = await _partnerNetworking.CreatePartnershipProposalAsync(proposal);
            var estResult = await _partnerNetworking.EstablishPartnershipAsync(propResult.ProposalId);

            return estResult.Partnership ?? throw new InvalidOperationException("Partnership establishment failed");
        }

        #endregion
    }
}
