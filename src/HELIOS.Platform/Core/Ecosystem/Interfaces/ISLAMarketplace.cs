using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Ecosystem.Interfaces
{
    /// <summary>
    /// Manages SLA (Service Level Agreement) trading and negotiation.
    /// Allows buyers and sellers to negotiate custom SLAs with dynamic pricing.
    /// </summary>
    public interface ISLAMarketplace
    {
        /// <summary>
        /// Creates a new SLA template for trading.
        /// </summary>
        /// <param name="template">SLA template definition</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Creation result</returns>
        Task<SLACreationResult> CreateSLATemplateAsync(SLATemplate template, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes an SLA for buyers to browse and negotiate.
        /// </summary>
        /// <param name="slaId">SLA identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Publication result</returns>
        Task<SLAPublicationResult> PublishSLAAsync(string slaId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Discovers available SLAs in the marketplace.
        /// </summary>
        /// <param name="filter">Search and filter criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available SLAs</returns>
        Task<IEnumerable<SLAListing>> DiscoverSLAsAsync(SLADiscoveryFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets detailed information about a specific SLA.
        /// </summary>
        /// <param name="slaId">SLA identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>SLA details or null if not found</returns>
        Task<SLADetails?> GetSLADetailsAsync(string slaId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a negotiation request for an SLA.
        /// </summary>
        /// <param name="slaId">SLA identifier</param>
        /// <param name="negotiation">Negotiation parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Negotiation result</returns>
        Task<NegotiationResult> InitiateNegotiationAsync(string slaId, SLANegotiation negotiation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Responds to an SLA negotiation request.
        /// </summary>
        /// <param name="negotiationId">Negotiation identifier</param>
        /// <param name="response">Response to the negotiation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response result</returns>
        Task<NegotiationResponseResult> RespondToNegotiationAsync(string negotiationId, NegotiationResponse response, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets active negotiations for an organization.
        /// </summary>
        /// <param name="organizationId">Organization identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active negotiations</returns>
        Task<IEnumerable<NegotiationStatus>> GetActiveNegotiationsAsync(string organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finalizes and accepts an SLA agreement.
        /// </summary>
        /// <param name="negotiationId">Negotiation identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Agreement result</returns>
        Task<AgreementResult> FinalizeAgreementAsync(string negotiationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active SLA agreements for an organization.
        /// </summary>
        /// <param name="organizationId">Organization identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active agreements</returns>
        Task<IEnumerable<SLAAgreement>> GetActiveAgreementsAsync(string organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates the cost for a custom SLA configuration.
        /// </summary>
        /// <param name="basePrice">Base price of the SLA</param>
        /// <param name="customConfig">Custom configuration parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cost calculation result</returns>
        Task<CostCalculationResult> CalculateSLACostAsync(decimal basePrice, SLACustomConfiguration customConfig, CancellationToken cancellationToken = default);

        /// <summary>
        /// Terminates an active SLA agreement.
        /// </summary>
        /// <param name="agreementId">Agreement identifier</param>
        /// <param name="reason">Reason for termination</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Termination result</returns>
        Task<TerminationResult> TerminateAgreementAsync(string agreementId, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets performance metrics against an SLA agreement.
        /// </summary>
        /// <param name="agreementId">Agreement identifier</param>
        /// <param name="startDate">Start date for metrics</param>
        /// <param name="endDate">End date for metrics</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Performance metrics</returns>
        Task<SLAPerformanceMetrics> GetPerformanceMetricsAsync(string agreementId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// SLA template definition.
    /// </summary>
    public class SLATemplate
    {
        /// <summary>Gets or sets the SLA identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the SLA name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the SLA description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the seller organization identifier.</summary>
        public string SellerOrgId { get; set; } = string.Empty;

        /// <summary>Gets or sets the service being offered.</summary>
        public string Service { get; set; } = string.Empty;

        /// <summary>Gets or sets the base price per month.</summary>
        public decimal BasePricePerMonth { get; set; }

        /// <summary>Gets or sets the uptime guarantee percentage.</summary>
        public double UptimePercentage { get; set; }

        /// <summary>Gets or sets the response time SLA in milliseconds.</summary>
        public int ResponseTimeSlaMs { get; set; }

        /// <summary>Gets or sets the maximum allowed downtime per month in minutes.</summary>
        public int MaxDowntimePerMonthMinutes { get; set; }

        /// <summary>Gets or sets the credit for each percentage point SLA breach.</summary>
        public decimal CreditPerPercentageBreachUpto { get; set; }

        /// <summary>Gets or sets the maximum support response time.</summary>
        public int SupportResponseTimeHours { get; set; }

        /// <summary>Gets or sets whether the SLA is customizable.</summary>
        public bool IsCustomizable { get; set; }

        /// <summary>Gets or sets the available customization parameters.</summary>
        public IEnumerable<string> CustomizationParameters { get; set; } = Array.Empty<string>();

        /// <summary>Gets or sets the minimum term in months.</summary>
        public int MinimumTermMonths { get; set; }
    }

    /// <summary>
    /// Filter criteria for SLA discovery.
    /// </summary>
    public class SLADiscoveryFilter
    {
        /// <summary>Gets or sets the search query.</summary>
        public string? SearchQuery { get; set; }

        /// <summary>Gets or sets the service category filter.</summary>
        public string? ServiceCategory { get; set; }

        /// <summary>Gets or sets the minimum uptime percentage.</summary>
        public double? MinimumUptimePercentage { get; set; }

        /// <summary>Gets or sets the maximum price filter.</summary>
        public decimal? MaximumPrice { get; set; }

        /// <summary>Gets or sets whether to include only customizable SLAs.</summary>
        public bool? CustomizableOnly { get; set; }

        /// <summary>Gets or sets the sort order.</summary>
        public string SortBy { get; set; } = "Price";

        /// <summary>Gets or sets pagination page number.</summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>Gets or sets pagination page size.</summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// SLA listing in the marketplace.
    /// </summary>
    public class SLAListing
    {
        /// <summary>Gets or sets the SLA identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the SLA name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the short description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the seller organization name.</summary>
        public string SellerOrganization { get; set; } = string.Empty;

        /// <summary>Gets or sets the service type.</summary>
        public string ServiceType { get; set; } = string.Empty;

        /// <summary>Gets or sets the base price per month.</summary>
        public decimal BasePrice { get; set; }

        /// <summary>Gets or sets the uptime guarantee percentage.</summary>
        public double UptimePercentage { get; set; }

        /// <summary>Gets or sets the number of active subscribers.</summary>
        public int ActiveSubscribers { get; set; }

        /// <summary>Gets or sets whether the SLA is customizable.</summary>
        public bool IsCustomizable { get; set; }

        /// <summary>Gets or sets the average rating (0-5).</summary>
        public double AverageRating { get; set; }
    }

    /// <summary>
    /// Detailed information about an SLA.
    /// </summary>
    public class SLADetails
    {
        /// <summary>Gets or sets the SLA identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the SLA name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the full description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the base price per month.</summary>
        public decimal BasePricePerMonth { get; set; }

        /// <summary>Gets or sets the uptime guarantee percentage.</summary>
        public double UptimePercentage { get; set; }

        /// <summary>Gets or sets the response time SLA in milliseconds.</summary>
        public int ResponseTimeSlaMs { get; set; }

        /// <summary>Gets or sets the maximum downtime per month.</summary>
        public int MaxDowntimePerMonthMinutes { get; set; }

        /// <summary>Gets or sets the support response time.</summary>
        public int SupportResponseTimeHours { get; set; }

        /// <summary>Gets or sets whether customizable.</summary>
        public bool IsCustomizable { get; set; }

        /// <summary>Gets or sets the customization parameters available.</summary>
        public IEnumerable<CustomizationParameter> CustomizationParameters { get; set; } = Array.Empty<CustomizationParameter>();

        /// <summary>Gets or sets recent performance reviews.</summary>
        public IEnumerable<PerformanceReview> PerformanceReviews { get; set; } = Array.Empty<PerformanceReview>();
    }

    /// <summary>
    /// Customization parameter for an SLA.
    /// </summary>
    public class CustomizationParameter
    {
        /// <summary>Gets or sets the parameter name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the parameter description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the minimum value.</summary>
        public string? MinValue { get; set; }

        /// <summary>Gets or sets the maximum value.</summary>
        public string? MaxValue { get; set; }

        /// <summary>Gets or sets the default value.</summary>
        public string? DefaultValue { get; set; }

        /// <summary>Gets or sets the price adjustment per unit change.</summary>
        public decimal? PriceAdjustmentPerUnit { get; set; }
    }

    /// <summary>
    /// Performance review for an SLA.
    /// </summary>
    public class PerformanceReview
    {
        /// <summary>Gets or sets the reviewer organization.</summary>
        public string ReviewerOrganization { get; set; } = string.Empty;

        /// <summary>Gets or sets the review date.</summary>
        public DateTime ReviewDate { get; set; }

        /// <summary>Gets or sets the rating (0-5).</summary>
        public double Rating { get; set; }

        /// <summary>Gets or sets the review comment.</summary>
        public string Comment { get; set; } = string.Empty;
    }

    /// <summary>
    /// SLA negotiation request.
    /// </summary>
    public class SLANegotiation
    {
        /// <summary>Gets or sets the buyer organization identifier.</summary>
        public string BuyerOrgId { get; set; } = string.Empty;

        /// <summary>Gets or sets the buyer organization name.</summary>
        public string BuyerOrgName { get; set; } = string.Empty;

        /// <summary>Gets or sets the desired customizations.</summary>
        public SLACustomConfiguration DesiredConfiguration { get; set; } = new();

        /// <summary>Gets or sets the budget constraint.</summary>
        public decimal? MaximumBudget { get; set; }

        /// <summary>Gets or sets any special requirements.</summary>
        public string? SpecialRequirements { get; set; }

        /// <summary>Gets or sets the contract start date.</summary>
        public DateTime ContractStartDate { get; set; }

        /// <summary>Gets or sets the contract duration in months.</summary>
        public int ContractDurationMonths { get; set; }
    }

    /// <summary>
    /// Custom SLA configuration parameters.
    /// </summary>
    public class SLACustomConfiguration
    {
        /// <summary>Gets or sets the desired uptime percentage.</summary>
        public double? UptimePercentage { get; set; }

        /// <summary>Gets or sets the desired response time in milliseconds.</summary>
        public int? ResponseTimeSlaMs { get; set; }

        /// <summary>Gets or sets the desired maximum downtime per month.</summary>
        public int? MaxDowntimePerMonthMinutes { get; set; }

        /// <summary>Gets or sets the desired support response time.</summary>
        public int? SupportResponseTimeHours { get; set; }

        /// <summary>Gets or sets custom parameters as key-value pairs.</summary>
        public Dictionary<string, string> CustomParameters { get; set; } = new();
    }

    /// <summary>
    /// Response to an SLA negotiation.
    /// </summary>
    public class NegotiationResponse
    {
        /// <summary>Gets or sets whether the seller accepts the negotiation.</summary>
        public bool IsAccepted { get; set; }

        /// <summary>Gets or sets the counter-offer configuration (if not accepting as-is).</summary>
        public SLACustomConfiguration? CounterOfferConfiguration { get; set; }

        /// <summary>Gets or sets the proposed price.</summary>
        public decimal ProposedPrice { get; set; }

        /// <summary>Gets or sets any seller comments.</summary>
        public string? SellerComments { get; set; }

        /// <summary>Gets or sets the counter-offer expiration date.</summary>
        public DateTime? CounterOfferExpirationDate { get; set; }
    }

    /// <summary>
    /// Status of an active negotiation.
    /// </summary>
    public class NegotiationStatus
    {
        /// <summary>Gets or sets the negotiation identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the SLA identifier.</summary>
        public string SLAId { get; set; } = string.Empty;

        /// <summary>Gets or sets the SLA name.</summary>
        public string SLAName { get; set; } = string.Empty;

        /// <summary>Gets or sets the negotiation initiator organization.</summary>
        public string InitiatorOrganization { get; set; } = string.Empty;

        /// <summary>Gets or sets the negotiation responder organization.</summary>
        public string ResponderOrganization { get; set; } = string.Empty;

        /// <summary>Gets or sets the current status.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Gets or sets the number of negotiation rounds.</summary>
        public int NegotiationRounds { get; set; }

        /// <summary>Gets or sets the creation date.</summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>Gets or sets the last update date.</summary>
        public DateTime LastUpdatedDate { get; set; }

        /// <summary>Gets or sets the proposed monthly cost.</summary>
        public decimal ProposedMonthlyCost { get; set; }
    }

    /// <summary>
    /// SLA agreement contract.
    /// </summary>
    public class SLAAgreement
    {
        /// <summary>Gets or sets the agreement identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the SLA template identifier.</summary>
        public string TemplateId { get; set; } = string.Empty;

        /// <summary>Gets or sets the SLA name.</summary>
        public string SLAName { get; set; } = string.Empty;

        /// <summary>Gets or sets the buyer organization identifier.</summary>
        public string BuyerOrgId { get; set; } = string.Empty;

        /// <summary>Gets or sets the seller organization identifier.</summary>
        public string SellerOrgId { get; set; } = string.Empty;

        /// <summary>Gets or sets the agreed configuration.</summary>
        public SLACustomConfiguration AgreedConfiguration { get; set; } = new();

        /// <summary>Gets or sets the agreed monthly price.</summary>
        public decimal AgreedMonthlyCost { get; set; }

        /// <summary>Gets or sets the contract start date.</summary>
        public DateTime ContractStartDate { get; set; }

        /// <summary>Gets or sets the contract end date.</summary>
        public DateTime ContractEndDate { get; set; }

        /// <summary>Gets or sets the agreement status.</summary>
        public string Status { get; set; } = "Active";

        /// <summary>Gets or sets the agreement creation date.</summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>Gets or sets whether automatic renewal is enabled.</summary>
        public bool AutoRenewal { get; set; }
    }

    /// <summary>
    /// Result of SLA creation.
    /// </summary>
    public class SLACreationResult
    {
        /// <summary>Gets or sets whether creation succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the created SLA identifier.</summary>
        public string SLAId { get; set; } = string.Empty;

        /// <summary>Gets or sets any validation issues.</summary>
        public IEnumerable<string> ValidationIssues { get; set; } = Array.Empty<string>();

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of SLA publication.
    /// </summary>
    public class SLAPublicationResult
    {
        /// <summary>Gets or sets whether publication succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the SLA identifier.</summary>
        public string SLAId { get; set; } = string.Empty;

        /// <summary>Gets or sets the publication timestamp.</summary>
        public DateTime PublishedDate { get; set; }

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of initiating an SLA negotiation.
    /// </summary>
    public class NegotiationResult
    {
        /// <summary>Gets or sets whether initiation succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the negotiation identifier.</summary>
        public string NegotiationId { get; set; } = string.Empty;

        /// <summary>Gets or sets the initial cost estimate.</summary>
        public decimal InitialCostEstimate { get; set; }

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of negotiation response.
    /// </summary>
    public class NegotiationResponseResult
    {
        /// <summary>Gets or sets whether the response was accepted.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the negotiation status.</summary>
        public string NegotiationStatus { get; set; } = string.Empty;

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of finalizing an SLA agreement.
    /// </summary>
    public class AgreementResult
    {
        /// <summary>Gets or sets whether finalization succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the agreement identifier.</summary>
        public string AgreementId { get; set; } = string.Empty;

        /// <summary>Gets or sets the agreement details.</summary>
        public SLAAgreement? Agreement { get; set; }

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of SLA cost calculation.
    /// </summary>
    public class CostCalculationResult
    {
        /// <summary>Gets or sets whether calculation succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the calculated monthly cost.</summary>
        public decimal MonthlyCost { get; set; }

        /// <summary>Gets or sets the cost breakdown by component.</summary>
        public Dictionary<string, decimal> CostBreakdown { get; set; } = new();

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of SLA agreement termination.
    /// </summary>
    public class TerminationResult
    {
        /// <summary>Gets or sets whether termination succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the agreement identifier.</summary>
        public string AgreementId { get; set; } = string.Empty;

        /// <summary>Gets or sets the termination date.</summary>
        public DateTime TerminationDate { get; set; }

        /// <summary>Gets or sets any refunds or charges.</summary>
        public decimal? AdjustedCost { get; set; }

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Performance metrics for an SLA agreement.
    /// </summary>
    public class SLAPerformanceMetrics
    {
        /// <summary>Gets or sets the agreement identifier.</summary>
        public string AgreementId { get; set; } = string.Empty;

        /// <summary>Gets or sets the measurement period start date.</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Gets or sets the measurement period end date.</summary>
        public DateTime EndDate { get; set; }

        /// <summary>Gets or sets the actual uptime percentage.</summary>
        public double ActualUptimePercentage { get; set; }

        /// <summary>Gets or sets the SLA target uptime percentage.</summary>
        public double TargetUptimePercentage { get; set; }

        /// <summary>Gets or sets the total downtime minutes.</summary>
        public int TotalDowntimeMinutes { get; set; }

        /// <summary>Gets or sets the average response time in milliseconds.</summary>
        public double AverageResponseTimeMs { get; set; }

        /// <summary>Gets or sets the target response time in milliseconds.</summary>
        public int TargetResponseTimeMs { get; set; }

        /// <summary>Gets or sets whether SLA targets were met.</summary>
        public bool SLATargetsMet { get; set; }

        /// <summary>Gets or sets the credit amount owed for SLA breach.</summary>
        public decimal CreditOwed { get; set; }

        /// <summary>Gets or sets the incident count during the period.</summary>
        public int IncidentCount { get; set; }
    }
}
