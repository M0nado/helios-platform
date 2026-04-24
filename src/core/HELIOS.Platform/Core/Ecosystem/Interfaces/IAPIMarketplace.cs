using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Ecosystem.Interfaces
{
    /// <summary>
    /// Manages the public API marketplace for exposing services.
    /// Handles API publishing, versioning, access control, and monetization.
    /// </summary>
    public interface IAPIMarketplace
    {
        /// <summary>
        /// Publishes an API to the marketplace.
        /// </summary>
        /// <param name="apiDefinition">API definition to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Publication result</returns>
        Task<PublicationResult> PublishAPIAsync(APIDefinition apiDefinition, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unpublishes an API from the marketplace.
        /// </summary>
        /// <param name="apiId">API identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Unpublication result</returns>
        Task<UnpublicationResult> UnpublishAPIAsync(string apiId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Discovers available APIs in the marketplace.
        /// </summary>
        /// <param name="filter">Search and filter criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of discoverable APIs</returns>
        Task<IEnumerable<APIListing>> DiscoverAPIsAsync(APIDiscoveryFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets detailed information about a specific API.
        /// </summary>
        /// <param name="apiId">API identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>API details or null if not found</returns>
        Task<APIDetails?> GetAPIDetailsAsync(string apiId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribes to an API.
        /// </summary>
        /// <param name="apiId">API identifier</param>
        /// <param name="subscriptionTier">Subscription tier to purchase</param>
        /// <param name="consumerInfo">Consumer information</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Subscription result</returns>
        Task<SubscriptionResult> SubscribeToAPIAsync(string apiId, string subscriptionTier, ConsumerInfo consumerInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels an API subscription.
        /// </summary>
        /// <param name="subscriptionId">Subscription identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cancellation result</returns>
        Task<CancellationResult> CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets active subscriptions for a consumer.
        /// </summary>
        /// <param name="consumerId">Consumer identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active subscriptions</returns>
        Task<IEnumerable<APISubscription>> GetConsumerSubscriptionsAsync(string consumerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets usage metrics for an API subscription.
        /// </summary>
        /// <param name="subscriptionId">Subscription identifier</param>
        /// <param name="startDate">Start date for metrics</param>
        /// <param name="endDate">End date for metrics</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Usage metrics</returns>
        Task<UsageMetrics> GetUsageMetricsAsync(string subscriptionId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates an API access key for a subscription.
        /// </summary>
        /// <param name="subscriptionId">Subscription identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Access key information</returns>
        Task<AccessKeyInfo> GenerateAccessKeyAsync(string subscriptionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes an API access key.
        /// </summary>
        /// <param name="keyId">Access key identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Revocation result</returns>
        Task<RevocationResult> RevokeAccessKeyAsync(string keyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates API pricing.
        /// </summary>
        /// <param name="apiId">API identifier</param>
        /// <param name="pricingTiers">New pricing tier configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Update result</returns>
        Task<PricingUpdateResult> UpdatePricingAsync(string apiId, IEnumerable<PricingTier> pricingTiers, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Definition for an API to be published to the marketplace.
    /// </summary>
    public class APIDefinition
    {
        /// <summary>Gets or sets the API identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the API name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the API description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the API version.</summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>Gets or sets the base endpoint URL.</summary>
        public string BaseEndpoint { get; set; } = string.Empty;

        /// <summary>Gets or sets the OpenAPI/Swagger specification.</summary>
        public string OpenAPISpec { get; set; } = string.Empty;

        /// <summary>Gets or sets the publisher organization.</summary>
        public string Publisher { get; set; } = string.Empty;

        /// <summary>Gets or sets the API category.</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Gets or sets the pricing tiers.</summary>
        public IEnumerable<PricingTier> PricingTiers { get; set; } = Array.Empty<PricingTier>();

        /// <summary>Gets or sets the rate limits.</summary>
        public RateLimits RateLimits { get; set; } = new();

        /// <summary>Gets or sets whether the API requires authentication.</summary>
        public bool RequiresAuthentication { get; set; } = true;

        /// <summary>Gets or sets the supported authentication methods.</summary>
        public IEnumerable<string> AuthenticationMethods { get; set; } = new[] { "ApiKey", "OAuth2" };
    }

    /// <summary>
    /// Filter criteria for API discovery.
    /// </summary>
    public class APIDiscoveryFilter
    {
        /// <summary>Gets or sets the search query.</summary>
        public string? SearchQuery { get; set; }

        /// <summary>Gets or sets the category filter.</summary>
        public string? Category { get; set; }

        /// <summary>Gets or sets the minimum rating threshold.</summary>
        public double? MinimumRating { get; set; }

        /// <summary>Gets or sets the supported authentication types.</summary>
        public IEnumerable<string>? AuthenticationTypes { get; set; }

        /// <summary>Gets or sets whether to include free APIs only.</summary>
        public bool? FreeOnly { get; set; }

        /// <summary>Gets or sets the sort order.</summary>
        public string SortBy { get; set; } = "Popularity";

        /// <summary>Gets or sets pagination page number.</summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>Gets or sets pagination page size.</summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// API listing in the marketplace.
    /// </summary>
    public class APIListing
    {
        /// <summary>Gets or sets the API identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the API name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the short description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the publisher.</summary>
        public string Publisher { get; set; } = string.Empty;

        /// <summary>Gets or sets the category.</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Gets or sets the average rating (0-5).</summary>
        public double Rating { get; set; }

        /// <summary>Gets or sets the number of subscribers.</summary>
        public int Subscribers { get; set; }

        /// <summary>Gets or sets the starting price.</summary>
        public decimal StartingPrice { get; set; }

        /// <summary>Gets or sets whether the API is verified.</summary>
        public bool IsVerified { get; set; }
    }

    /// <summary>
    /// Detailed information about an API.
    /// </summary>
    public class APIDetails
    {
        /// <summary>Gets or sets the API identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the API name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the full description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the base endpoint URL.</summary>
        public string BaseEndpoint { get; set; } = string.Empty;

        /// <summary>Gets or sets available API versions.</summary>
        public IEnumerable<string> AvailableVersions { get; set; } = Array.Empty<string>();

        /// <summary>Gets or sets the OpenAPI specification URL.</summary>
        public string OpenAPISpecUrl { get; set; } = string.Empty;

        /// <summary>Gets or sets the documentation URL.</summary>
        public string DocumentationUrl { get; set; } = string.Empty;

        /// <summary>Gets or sets the pricing tiers.</summary>
        public IEnumerable<PricingTier> PricingTiers { get; set; } = Array.Empty<PricingTier>();

        /// <summary>Gets or sets the rate limits per tier.</summary>
        public Dictionary<string, RateLimits> RateLimitsByTier { get; set; } = new();

        /// <summary>Gets or sets the service level agreement.</summary>
        public ServiceLevelAgreement SLA { get; set; } = new();
    }

    /// <summary>
    /// Pricing tier for an API.
    /// </summary>
    public class PricingTier
    {
        /// <summary>Gets or sets the tier name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the monthly price.</summary>
        public decimal MonthlyPrice { get; set; }

        /// <summary>Gets or sets the included request quota per month.</summary>
        public long RequestQuotaPerMonth { get; set; }

        /// <summary>Gets or sets the price per additional request.</summary>
        public decimal PricePerAdditionalRequest { get; set; }

        /// <summary>Gets or sets additional features included in this tier.</summary>
        public IEnumerable<string> Features { get; set; } = Array.Empty<string>();

        /// <summary>Gets or sets the rate limit for this tier.</summary>
        public RateLimits RateLimit { get; set; } = new();
    }

    /// <summary>
    /// Rate limiting configuration.
    /// </summary>
    public class RateLimits
    {
        /// <summary>Gets or sets the maximum requests per second.</summary>
        public int RequestsPerSecond { get; set; }

        /// <summary>Gets or sets the maximum concurrent connections.</summary>
        public int MaxConcurrentConnections { get; set; }

        /// <summary>Gets or sets the maximum request size in bytes.</summary>
        public long MaxRequestSizeBytes { get; set; }

        /// <summary>Gets or sets the request timeout in seconds.</summary>
        public int TimeoutSeconds { get; set; }
    }

    /// <summary>
    /// Service level agreement details.
    /// </summary>
    public class ServiceLevelAgreement
    {
        /// <summary>Gets or sets the uptime percentage guarantee.</summary>
        public double UptimePercentage { get; set; } = 99.9;

        /// <summary>Gets or sets the average response time in milliseconds.</summary>
        public int AverageResponseTimeMs { get; set; }

        /// <summary>Gets or sets the maximum response time in milliseconds.</summary>
        public int MaxResponseTimeMs { get; set; }

        /// <summary>Gets or sets the support level.</summary>
        public string SupportLevel { get; set; } = "Standard";
    }

    /// <summary>
    /// Consumer information for API subscriptions.
    /// </summary>
    public class ConsumerInfo
    {
        /// <summary>Gets or sets the consumer identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the consumer name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the consumer email.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Gets or sets the organization name.</summary>
        public string Organization { get; set; } = string.Empty;
    }

    /// <summary>
    /// API subscription information.
    /// </summary>
    public class APISubscription
    {
        /// <summary>Gets or sets the subscription identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the API identifier.</summary>
        public string APIId { get; set; } = string.Empty;

        /// <summary>Gets or sets the consumer identifier.</summary>
        public string ConsumerId { get; set; } = string.Empty;

        /// <summary>Gets or sets the subscription tier.</summary>
        public string Tier { get; set; } = string.Empty;

        /// <summary>Gets or sets the subscription status.</summary>
        public string Status { get; set; } = "Active";

        /// <summary>Gets or sets the subscription start date.</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Gets or sets the subscription renewal date.</summary>
        public DateTime RenewalDate { get; set; }

        /// <summary>Gets or sets the monthly cost.</summary>
        public decimal MonthlyCost { get; set; }

        /// <summary>Gets or sets the access keys for this subscription.</summary>
        public IEnumerable<string> AccessKeys { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Usage metrics for an API subscription.
    /// </summary>
    public class UsageMetrics
    {
        /// <summary>Gets or sets the subscription identifier.</summary>
        public string SubscriptionId { get; set; } = string.Empty;

        /// <summary>Gets or sets the measurement period start date.</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Gets or sets the measurement period end date.</summary>
        public DateTime EndDate { get; set; }

        /// <summary>Gets or sets the total API requests made.</summary>
        public long TotalRequests { get; set; }

        /// <summary>Gets or sets the total data transferred in bytes.</summary>
        public long TotalDataTransferBytes { get; set; }

        /// <summary>Gets or sets the average response time in milliseconds.</summary>
        public double AverageResponseTimeMs { get; set; }

        /// <summary>Gets or sets the percentage of successful requests.</summary>
        public double SuccessRate { get; set; }

        /// <summary>Gets or sets the cost for this period.</summary>
        public decimal CostForPeriod { get; set; }
    }

    /// <summary>
    /// API access key information.
    /// </summary>
    public class AccessKeyInfo
    {
        /// <summary>Gets or sets the access key identifier.</summary>
        public string KeyId { get; set; } = string.Empty;

        /// <summary>Gets or sets the access key value.</summary>
        public string KeyValue { get; set; } = string.Empty;

        /// <summary>Gets or sets the creation date.</summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>Gets or sets the expiration date (if applicable).</summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>Gets or sets whether the key is active.</summary>
        public bool IsActive { get; set; }

        /// <summary>Gets or sets the last used date.</summary>
        public DateTime? LastUsedDate { get; set; }
    }

    /// <summary>
    /// Result of an API publication operation.
    /// </summary>
    public class PublicationResult
    {
        /// <summary>Gets or sets whether publication succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the API identifier.</summary>
        public string APIId { get; set; } = string.Empty;

        /// <summary>Gets or sets the publication timestamp.</summary>
        public DateTime PublishedDate { get; set; }

        /// <summary>Gets or sets any validation issues found.</summary>
        public IEnumerable<string> ValidationIssues { get; set; } = Array.Empty<string>();

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of an API unpublication operation.
    /// </summary>
    public class UnpublicationResult
    {
        /// <summary>Gets or sets whether unpublication succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the API identifier.</summary>
        public string APIId { get; set; } = string.Empty;

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of an API subscription operation.
    /// </summary>
    public class SubscriptionResult
    {
        /// <summary>Gets or sets whether subscription succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the subscription identifier.</summary>
        public string SubscriptionId { get; set; } = string.Empty;

        /// <summary>Gets or sets the subscription details.</summary>
        public APISubscription? Subscription { get; set; }

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of a subscription cancellation operation.
    /// </summary>
    public class CancellationResult
    {
        /// <summary>Gets or sets whether cancellation succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the subscription identifier.</summary>
        public string SubscriptionId { get; set; } = string.Empty;

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of an access key revocation operation.
    /// </summary>
    public class RevocationResult
    {
        /// <summary>Gets or sets whether revocation succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the key identifier.</summary>
        public string KeyId { get; set; } = string.Empty;

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of a pricing update operation.
    /// </summary>
    public class PricingUpdateResult
    {
        /// <summary>Gets or sets whether the update succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the API identifier.</summary>
        public string APIId { get; set; } = string.Empty;

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }
}
