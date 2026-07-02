namespace MonadoBlade.Core.Services;

/// <summary>
/// Configuration options for AI/ML queries.
/// </summary>
public class AIOptions
{
    /// <summary>Gets or sets the model to use (e.g., "gpt-4", "claude-3").</summary>
    public string? Model { get; set; }

    /// <summary>Gets or sets the maximum tokens in the response.</summary>
    public int? MaxTokens { get; set; }

    /// <summary>Gets or sets the sampling temperature (0.0-2.0).</summary>
    public float? Temperature { get; set; }

    /// <summary>Gets or sets the top-p value for nucleus sampling.</summary>
    public float? TopP { get; set; }

    /// <summary>Gets or sets the response format (json, text, etc).</summary>
    public string? ResponseFormat { get; set; }

    /// <summary>Gets or sets the timeout for the request.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Gets or sets whether to stream the response.</summary>
    public bool Stream { get; set; }

    /// <summary>Gets or sets the system prompt context.</summary>
    public string? SystemPrompt { get; set; }
}

/// <summary>
/// Response from an AI query operation.
/// </summary>
public class AIResponse
{
    /// <summary>Gets or sets the response text.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the model that generated the response.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of input tokens used.</summary>
    public int InputTokens { get; set; }

    /// <summary>Gets or sets the number of output tokens generated.</summary>
    public int OutputTokens { get; set; }

    /// <summary>Gets or sets the processing latency.</summary>
    public TimeSpan Latency { get; set; }

    /// <summary>Gets or sets the cost of the query in USD.</summary>
    public decimal Cost { get; set; }

    /// <summary>Gets or sets the confidence score (0-1).</summary>
    public float Confidence { get; set; }

    /// <summary>Gets or sets usage metrics.</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Contextual information for routing ML queries to the best provider.
/// </summary>
public class QueryContext
{
    /// <summary>Gets or sets the user query.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>Gets or sets the query type (chat, analysis, generation, etc).</summary>
    public string QueryType { get; set; } = string.Empty;

    /// <summary>Gets or sets the required latency in milliseconds.</summary>
    public int? MaxLatencyMs { get; set; }

    /// <summary>Gets or sets the budget in USD.</summary>
    public decimal? BudgetUsd { get; set; }

    /// <summary>Gets or sets the conversation history for context.</summary>
    public List<(string Role, string Content)> ConversationHistory { get; set; } = new();

    /// <summary>Gets or sets required capabilities (reasoning, vision, etc).</summary>
    public List<string> RequiredCapabilities { get; set; } = new();
}

/// <summary>
/// Decision about which provider to route a query to.
/// </summary>
public class RoutingDecision
{
    /// <summary>Gets or sets the selected provider name.</summary>
    public string SelectedProvider { get; set; } = string.Empty;

    /// <summary>Gets or sets the model to use from the provider.</summary>
    public string SelectedModel { get; set; } = string.Empty;

    /// <summary>Gets or sets the confidence in this routing decision (0-1).</summary>
    public float Confidence { get; set; }

    /// <summary>Gets or sets the estimated cost of using this provider.</summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>Gets or sets the estimated latency.</summary>
    public TimeSpan EstimatedLatency { get; set; }

    /// <summary>Gets or sets the fallback provider if primary fails.</summary>
    public string? FallbackProvider { get; set; }

    /// <summary>Gets or sets the reasoning for this selection.</summary>
    public string Reasoning { get; set; } = string.Empty;
}

/// <summary>
/// Machine Learning and AI service providing advanced query processing and provider routing.
/// Responsible for embedding generation, multi-provider query execution, and cost optimization.
/// </summary>
public interface IMLService : IService
{
    /// <summary>
    /// Executes a query against AI providers with streaming support.
    /// </summary>
    /// <param name="query">The user query.</param>
    /// <param name="options">Query configuration options.</param>
    /// <returns>The AI response.</returns>
    /// <exception cref="ServiceUnavailableException">Thrown when all providers are unavailable.</exception>
    /// <exception cref="OperationFailedException">Thrown when query execution fails.</exception>
    Task<AIResponse> QueryAsync(string query, AIOptions? options = null);

    /// <summary>
    /// Generates embeddings for text (vector representation for semantic search).
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <returns>A float array representing the embedding.</returns>
    /// <exception cref="ServiceUnavailableException">Thrown when embedding service is unavailable.</exception>
    Task<float[]> GetEmbeddingAsync(string text);

    /// <summary>
    /// Selects the best provider for a given query based on context and constraints.
    /// </summary>
    /// <param name="context">The query context with requirements.</param>
    /// <returns>A routing decision with selected provider and model.</returns>
    Task<RoutingDecision> SelectBestProviderAsync(QueryContext context);

    /// <summary>
    /// Streams a response token by token.
    /// </summary>
    /// <param name="query">The user query.</param>
    /// <param name="options">Query configuration options.</param>
    /// <returns>An async enumerable of response tokens.</returns>
    IAsyncEnumerable<string> StreamAsync(string query, AIOptions? options = null);

    /// <summary>
    /// Analyzes sentiment of text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>Sentiment score (-1 to 1) and confidence.</returns>
    Task<(float Sentiment, float Confidence)> AnalyzeSentimentAsync(string text);

    /// <summary>
    /// Extracts entities (people, places, organizations, etc) from text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>A collection of extracted entities with types.</returns>
    Task<List<(string Entity, string Type)>> ExtractEntitiesAsync(string text);

    /// <summary>
    /// Summarizes long text into a concise summary.
    /// </summary>
    /// <param name="text">The text to summarize.</param>
    /// <param name="maxLength">Maximum length of the summary.</param>
    /// <returns>The summarized text.</returns>
    Task<string> SummarizeAsync(string text, int maxLength = 150);

    /// <summary>
    /// Gets metrics about provider availability and performance.
    /// </summary>
    /// <returns>Provider health and performance metrics.</returns>
    Task<Dictionary<string, object>> GetProviderMetricsAsync();
}
