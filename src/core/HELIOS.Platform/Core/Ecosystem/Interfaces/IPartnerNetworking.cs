using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Ecosystem.Interfaces
{
    /// <summary>
    /// Manages partner ecosystem and networking.
    /// Enables partner discovery, collaboration, and revenue sharing.
    /// </summary>
    public interface IPartnerNetworking
    {
        /// <summary>
        /// Registers a new partner organization.
        /// </summary>
        /// <param name="partnerInfo">Partner information</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Registration result</returns>
        Task<PartnerRegistrationResult> RegisterPartnerAsync(PartnerInfo partnerInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets partner details.
        /// </summary>
        /// <param name="partnerId">Partner identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Partner details or null if not found</returns>
        Task<PartnerDetails?> GetPartnerDetailsAsync(string partnerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Discovers partner opportunities.
        /// </summary>
        /// <param name="filter">Discovery filter criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of partner opportunities</returns>
        Task<IEnumerable<PartnerOpportunity>> DiscoverOpportunitiesAsync(OpportunityDiscoveryFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a partnership proposal.
        /// </summary>
        /// <param name="proposal">Partnership proposal</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Proposal result</returns>
        Task<ProposalResult> CreatePartnershipProposalAsync(PartnershipProposal proposal, CancellationToken cancellationToken = default);

        /// <summary>
        /// Responds to a partnership proposal.
        /// </summary>
        /// <param name="proposalId">Proposal identifier</param>
        /// <param name="response">Response to the proposal</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response result</returns>
        Task<ProposalResponseResult> RespondToProposalAsync(string proposalId, ProposalResponse response, CancellationToken cancellationToken = default);

        /// <summary>
        /// Establishes a partnership agreement.
        /// </summary>
        /// <param name="proposalId">Proposal identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Partnership result</returns>
        Task<PartnershipEstablishmentResult> EstablishPartnershipAsync(string proposalId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets active partnerships for an organization.
        /// </summary>
        /// <param name="organizationId">Organization identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active partnerships</returns>
        Task<IEnumerable<Partnership>> GetActivePartnershipsAsync(string organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets up revenue sharing arrangement for a partnership.
        /// </summary>
        /// <param name="partnershipId">Partnership identifier</param>
        /// <param name="revenueSharingModel">Revenue sharing model configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Setup result</returns>
        Task<RevenueSharingSetupResult> SetupRevenueSharingAsync(string partnershipId, RevenueSharingModel revenueSharingModel, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets revenue sharing metrics for a partnership.
        /// </summary>
        /// <param name="partnershipId">Partnership identifier</param>
        /// <param name="startDate">Start date for metrics</param>
        /// <param name="endDate">End date for metrics</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Revenue metrics</returns>
        Task<RevenueMetrics> GetRevenueMetricsAsync(string partnershipId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a collaboration project between partners.
        /// </summary>
        /// <param name="collaboration">Collaboration details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collaboration result</returns>
        Task<CollaborationResult> CreateCollaborationProjectAsync(PartnerCollaboration collaboration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Shares resources with a partner.
        /// </summary>
        /// <param name="partnershipId">Partnership identifier</param>
        /// <param name="resourceShare">Resource sharing details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Share result</returns>
        Task<ResourceShareResult> ShareResourceAsync(string partnershipId, ResourceShare resourceShare, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets marketplace reputation and ratings for a partner.
        /// </summary>
        /// <param name="partnerId">Partner identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Reputation information</returns>
        Task<PartnerReputation> GetPartnerReputationAsync(string partnerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rates a partner organization.
        /// </summary>
        /// <param name="partnerId">Partner identifier</param>
        /// <param name="rating">Rating (1-5)</param>
        /// <param name="review">Optional review text</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rating result</returns>
        Task<PartnerRatingResult> RatePartnerAsync(string partnerId, int rating, string? review = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Terminates a partnership agreement.
        /// </summary>
        /// <param name="partnershipId">Partnership identifier</param>
        /// <param name="reason">Reason for termination</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Termination result</returns>
        Task<PartnershipTerminationResult> TerminatePartnershipAsync(string partnershipId, string reason, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Partner organization information.
    /// </summary>
    public class PartnerInfo
    {
        /// <summary>Gets or sets the organization name.</summary>
        public string OrganizationName { get; set; } = string.Empty;

        /// <summary>Gets or sets the organization type.</summary>
        public string OrganizationType { get; set; } = string.Empty;

        /// <summary>Gets or sets the industry vertical.</summary>
        public string Industry { get; set; } = string.Empty;

        /// <summary>Gets or sets the primary contact email.</summary>
        public string ContactEmail { get; set; } = string.Empty;

        /// <summary>Gets or sets the organization size (employee count).</summary>
        public int EmployeeCount { get; set; }

        /// <summary>Gets or sets the organization website URL.</summary>
        public string WebsiteUrl { get; set; } = string.Empty;

        /// <summary>Gets or sets the partnership competencies.</summary>
        public IEnumerable<string> Competencies { get; set; } = Array.Empty<string>();

        /// <summary>Gets or sets the geographic regions served.</summary>
        public IEnumerable<string> GeographicRegions { get; set; } = Array.Empty<string>();

        /// <summary>Gets or sets certifications held.</summary>
        public IEnumerable<string> Certifications { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Detailed partner information.
    /// </summary>
    public class PartnerDetails
    {
        /// <summary>Gets or sets the partner identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the organization name.</summary>
        public string OrganizationName { get; set; } = string.Empty;

        /// <summary>Gets or sets the organization description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the registration date.</summary>
        public DateTime RegistrationDate { get; set; }

        /// <summary>Gets or sets the partnership tier level.</summary>
        public string TierLevel { get; set; } = string.Empty;

        /// <summary>Gets or sets the current active partnerships count.</summary>
        public int ActivePartnershipsCount { get; set; }

        /// <summary>Gets or sets the organization reputation score (0-100).</summary>
        public double ReputationScore { get; set; }

        /// <summary>Gets or sets the total annual revenue through partnerships.</summary>
        public decimal AnnualRevenueFromPartnerships { get; set; }

        /// <summary>Gets or sets the partner's available services.</summary>
        public IEnumerable<AvailableService> AvailableServices { get; set; } = Array.Empty<AvailableService>();

        /// <summary>Gets or sets recent success stories.</summary>
        public IEnumerable<SuccessStory> SuccessStories { get; set; } = Array.Empty<SuccessStory>();
    }

    /// <summary>
    /// Service available from a partner.
    /// </summary>
    public class AvailableService
    {
        /// <summary>Gets or sets the service name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the service category.</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Gets or sets the service description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the base price per unit.</summary>
        public decimal BasePrice { get; set; }
    }

    /// <summary>
    /// Partner success story.
    /// </summary>
    public class SuccessStory
    {
        /// <summary>Gets or sets the story title.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Gets or sets the case study description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the client organization.</summary>
        public string ClientOrganization { get; set; } = string.Empty;

        /// <summary>Gets or sets the business impact achieved.</summary>
        public string BusinessImpact { get; set; } = string.Empty;
    }

    /// <summary>
    /// Partnership opportunity discovered in the ecosystem.
    /// </summary>
    public class PartnerOpportunity
    {
        /// <summary>Gets or sets the opportunity identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the partner organization name.</summary>
        public string PartnerOrganization { get; set; } = string.Empty;

        /// <summary>Gets or sets the opportunity title.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Gets or sets the opportunity description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the partnership type (e.g., Technology, Reseller, Co-marketing).</summary>
        public string PartnershipType { get; set; } = string.Empty;

        /// <summary>Gets or sets the revenue sharing potential.</summary>
        public string RevenueSharingPotential { get; set; } = string.Empty;

        /// <summary>Gets or sets the estimated annual opportunity value.</summary>
        public decimal EstimatedOpportunityValue { get; set; }

        /// <summary>Gets or sets the partner's reputation score.</summary>
        public double PartnerReputationScore { get; set; }

        /// <summary>Gets or sets when the opportunity was posted.</summary>
        public DateTime PostedDate { get; set; }
    }

    /// <summary>
    /// Filter for opportunity discovery.
    /// </summary>
    public class OpportunityDiscoveryFilter
    {
        /// <summary>Gets or sets the partnership type filter.</summary>
        public string? PartnershipType { get; set; }

        /// <summary>Gets or sets the industry filter.</summary>
        public string? Industry { get; set; }

        /// <summary>Gets or sets the minimum partner reputation score.</summary>
        public double? MinimumReputationScore { get; set; }

        /// <summary>Gets or sets the minimum estimated opportunity value.</summary>
        public decimal? MinimumOpportunityValue { get; set; }

        /// <summary>Gets or sets the geographic region filter.</summary>
        public string? GeographicRegion { get; set; }

        /// <summary>Gets or sets pagination page number.</summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>Gets or sets pagination page size.</summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Partnership proposal.
    /// </summary>
    public class PartnershipProposal
    {
        /// <summary>Gets or sets the initiating organization identifier.</summary>
        public string InitiatingOrgId { get; set; } = string.Empty;

        /// <summary>Gets or sets the target partner organization identifier.</summary>
        public string TargetPartnerOrgId { get; set; } = string.Empty;

        /// <summary>Gets or sets the proposal title.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Gets or sets the partnership type.</summary>
        public string PartnershipType { get; set; } = string.Empty;

        /// <summary>Gets or sets the proposal description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the proposed revenue sharing model.</summary>
        public string RevenueShareModel { get; set; } = string.Empty;

        /// <summary>Gets or sets the estimated annual value.</summary>
        public decimal EstimatedAnnualValue { get; set; }

        /// <summary>Gets or sets the proposed contract duration in months.</summary>
        public int ContractDurationMonths { get; set; }

        /// <summary>Gets or sets any special terms or conditions.</summary>
        public string? SpecialTerms { get; set; }
    }

    /// <summary>
    /// Response to a partnership proposal.
    /// </summary>
    public class ProposalResponse
    {
        /// <summary>Gets or sets whether the proposal is accepted.</summary>
        public bool IsAccepted { get; set; }

        /// <summary>Gets or sets counter-proposal details if not accepting as-is.</summary>
        public string? CounterProposal { get; set; }

        /// <summary>Gets or sets the responder's comments.</summary>
        public string? Comments { get; set; }

        /// <summary>Gets or sets alternative revenue share model if applicable.</summary>
        public string? AlternativeRevenueShareModel { get; set; }

        /// <summary>Gets or sets the counter-offer expiration date.</summary>
        public DateTime? CounterOfferExpirationDate { get; set; }
    }

    /// <summary>
    /// Active partnership agreement.
    /// </summary>
    public class Partnership
    {
        /// <summary>Gets or sets the partnership identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the first partner organization identifier.</summary>
        public string Partner1OrgId { get; set; } = string.Empty;

        /// <summary>Gets or sets the second partner organization identifier.</summary>
        public string Partner2OrgId { get; set; } = string.Empty;

        /// <summary>Gets or sets the first partner organization name.</summary>
        public string Partner1Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the second partner organization name.</summary>
        public string Partner2Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the partnership type.</summary>
        public string PartnershipType { get; set; } = string.Empty;

        /// <summary>Gets or sets the partnership status.</summary>
        public string Status { get; set; } = "Active";

        /// <summary>Gets or sets the establishment date.</summary>
        public DateTime EstablishedDate { get; set; }

        /// <summary>Gets or sets the contract end date.</summary>
        public DateTime ContractEndDate { get; set; }

        /// <summary>Gets or sets the agreed revenue sharing model.</summary>
        public string RevenueShareModel { get; set; } = string.Empty;

        /// <summary>Gets or sets the annual revenue generated.</summary>
        public decimal AnnualRevenue { get; set; }

        /// <summary>Gets or sets whether automatic renewal is enabled.</summary>
        public bool AutoRenewal { get; set; }
    }

    /// <summary>
    /// Revenue sharing model configuration.
    /// </summary>
    public class RevenueSharingModel
    {
        /// <summary>Gets or sets the model type (e.g., "Percentage", "FixedMargin").</summary>
        public string ModelType { get; set; } = string.Empty;

        /// <summary>Gets or sets the primary partner's revenue share percentage.</summary>
        public double PrimaryPartnerPercentage { get; set; }

        /// <summary>Gets or sets the secondary partner's revenue share percentage.</summary>
        public double SecondaryPartnerPercentage { get; set; }

        /// <summary>Gets or sets the platform/mediator commission percentage.</summary>
        public double PlatformCommissionPercentage { get; set; }

        /// <summary>Gets or sets the minimum revenue threshold for payments.</summary>
        public decimal MinimumRevenueThreshold { get; set; }

        /// <summary>Gets or sets the payment frequency (e.g., "Monthly", "Quarterly").</summary>
        public string PaymentFrequency { get; set; } = "Monthly";

        /// <summary>Gets or sets any additional terms or conditions.</summary>
        public string? AdditionalTerms { get; set; }
    }

    /// <summary>
    /// Revenue metrics for a partnership.
    /// </summary>
    public class RevenueMetrics
    {
        /// <summary>Gets or sets the partnership identifier.</summary>
        public string PartnershipId { get; set; } = string.Empty;

        /// <summary>Gets or sets the measurement period start date.</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Gets or sets the measurement period end date.</summary>
        public DateTime EndDate { get; set; }

        /// <summary>Gets or sets the total revenue generated.</summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>Gets or sets the first partner's earnings.</summary>
        public decimal Partner1Earnings { get; set; }

        /// <summary>Gets or sets the second partner's earnings.</summary>
        public decimal Partner2Earnings { get; set; }

        /// <summary>Gets or sets the platform commission.</summary>
        public decimal PlatformCommission { get; set; }

        /// <summary>Gets or sets the transaction count.</summary>
        public int TransactionCount { get; set; }

        /// <summary>Gets or sets the average transaction value.</summary>
        public decimal AverageTransactionValue { get; set; }

        /// <summary>Gets or sets the trend compared to previous period (percentage).</summary>
        public double GrowthPercentage { get; set; }
    }

    /// <summary>
    /// Partner collaboration project.
    /// </summary>
    public class PartnerCollaboration
    {
        /// <summary>Gets or sets the project identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the partnership identifier.</summary>
        public string PartnershipId { get; set; } = string.Empty;

        /// <summary>Gets or sets the project name.</summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>Gets or sets the project description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the project status.</summary>
        public string Status { get; set; } = "Planning";

        /// <summary>Gets or sets the expected start date.</summary>
        public DateTime ExpectedStartDate { get; set; }

        /// <summary>Gets or sets the expected completion date.</summary>
        public DateTime ExpectedCompletionDate { get; set; }

        /// <summary>Gets or sets the project budget.</summary>
        public decimal ProjectBudget { get; set; }

        /// <summary>Gets or sets the collaborating partners' roles.</summary>
        public Dictionary<string, string> PartnerRoles { get; set; } = new();
    }

    /// <summary>
    /// Resource sharing details between partners.
    /// </summary>
    public class ResourceShare
    {
        /// <summary>Gets or sets the resource type (e.g., "API", "Data", "Infrastructure").</summary>
        public string ResourceType { get; set; } = string.Empty;

        /// <summary>Gets or sets the resource description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gets or sets the resource access level.</summary>
        public string AccessLevel { get; set; } = "ReadOnly";

        /// <summary>Gets or sets the sharing start date.</summary>
        public DateTime SharingStartDate { get; set; }

        /// <summary>Gets or sets the sharing end date (if applicable).</summary>
        public DateTime? SharingEndDate { get; set; }

        /// <summary>Gets or sets any associated costs.</summary>
        public decimal? Cost { get; set; }

        /// <summary>Gets or sets the resource usage terms.</summary>
        public string UsageTerms { get; set; } = string.Empty;
    }

    /// <summary>
    /// Partner reputation information.
    /// </summary>
    public class PartnerReputation
    {
        /// <summary>Gets or sets the partner identifier.</summary>
        public string PartnerId { get; set; } = string.Empty;

        /// <summary>Gets or sets the overall reputation score (0-100).</summary>
        public double OverallScore { get; set; }

        /// <summary>Gets or sets the average rating from partnerships (0-5).</summary>
        public double AverageRating { get; set; }

        /// <summary>Gets or sets the number of ratings received.</summary>
        public int RatingCount { get; set; }

        /// <summary>Gets or sets the reliability score based on contract compliance.</summary>
        public double ReliabilityScore { get; set; }

        /// <summary>Gets or sets the communication quality score.</summary>
        public double CommunicationScore { get; set; }

        /// <summary>Gets or sets the delivery quality score.</summary>
        public double DeliveryQualityScore { get; set; }

        /// <summary>Gets or sets recent reviews and feedback.</summary>
        public IEnumerable<PartnerReview> RecentReviews { get; set; } = Array.Empty<PartnerReview>();
    }

    /// <summary>
    /// Partner review.
    /// </summary>
    public class PartnerReview
    {
        /// <summary>Gets or sets the reviewer organization.</summary>
        public string ReviewerOrganization { get; set; } = string.Empty;

        /// <summary>Gets or sets the review date.</summary>
        public DateTime ReviewDate { get; set; }

        /// <summary>Gets or sets the rating (1-5).</summary>
        public double Rating { get; set; }

        /// <summary>Gets or sets the review text.</summary>
        public string ReviewText { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of partner registration.
    /// </summary>
    public class PartnerRegistrationResult
    {
        /// <summary>Gets or sets whether registration succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the registered partner identifier.</summary>
        public string PartnerId { get; set; } = string.Empty;

        /// <summary>Gets or sets any validation issues found.</summary>
        public IEnumerable<string> ValidationIssues { get; set; } = Array.Empty<string>();

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of partnership proposal creation.
    /// </summary>
    public class ProposalResult
    {
        /// <summary>Gets or sets whether proposal creation succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the proposal identifier.</summary>
        public string ProposalId { get; set; } = string.Empty;

        /// <summary>Gets or sets the proposal creation timestamp.</summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of responding to a proposal.
    /// </summary>
    public class ProposalResponseResult
    {
        /// <summary>Gets or sets whether response was accepted.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the current proposal status.</summary>
        public string ProposalStatus { get; set; } = string.Empty;

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of partnership establishment.
    /// </summary>
    public class PartnershipEstablishmentResult
    {
        /// <summary>Gets or sets whether establishment succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the partnership identifier.</summary>
        public string PartnershipId { get; set; } = string.Empty;

        /// <summary>Gets or sets the partnership details.</summary>
        public Partnership? Partnership { get; set; }

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of revenue sharing setup.
    /// </summary>
    public class RevenueSharingSetupResult
    {
        /// <summary>Gets or sets whether setup succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the partnership identifier.</summary>
        public string PartnershipId { get; set; } = string.Empty;

        /// <summary>Gets or sets the next payment date.</summary>
        public DateTime NextPaymentDate { get; set; }

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of collaboration project creation.
    /// </summary>
    public class CollaborationResult
    {
        /// <summary>Gets or sets whether creation succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the collaboration project identifier.</summary>
        public string CollaborationId { get; set; } = string.Empty;

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of resource sharing setup.
    /// </summary>
    public class ResourceShareResult
    {
        /// <summary>Gets or sets whether sharing was successfully established.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the share identifier.</summary>
        public string ShareId { get; set; } = string.Empty;

        /// <summary>Gets or sets access details for the shared resource.</summary>
        public string AccessDetails { get; set; } = string.Empty;

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of rating a partner.
    /// </summary>
    public class PartnerRatingResult
    {
        /// <summary>Gets or sets whether rating was successful.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the new average rating.</summary>
        public double NewAverageRating { get; set; }

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of partnership termination.
    /// </summary>
    public class PartnershipTerminationResult
    {
        /// <summary>Gets or sets whether termination succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the partnership identifier.</summary>
        public string PartnershipId { get; set; } = string.Empty;

        /// <summary>Gets or sets the termination date.</summary>
        public DateTime TerminationDate { get; set; }

        /// <summary>Gets or sets final settlement amount if applicable.</summary>
        public decimal? FinalSettlementAmount { get; set; }

        /// <summary>Gets or sets any error messages.</summary>
        public string? ErrorMessage { get; set; }
    }
}
