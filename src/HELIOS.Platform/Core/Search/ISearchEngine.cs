using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Search
{
    /// <summary>
    /// Interface for advanced search and discovery system
    /// </summary>
    public interface ISearchEngine
    {
        // Global Search
        Task<SearchResults> SearchAsync(string query, SearchOptions options = null);
        Task<SearchResults> SearchGlobalAsync(string query);

        // Full-text Search
        Task<SearchResults> FullTextSearchAsync(string query);
        Task<bool> IndexContentAsync(string contentId, string content);
        Task<bool> RemoveFromIndexAsync(string contentId);

        // Advanced Features
        Task<SearchResults> FilterAsync(SearchResults results, SearchFilter filter);
        Task<SearchResults> SortAsync(SearchResults results, SortOrder sortOrder);
        Task<SearchResults> FuzzySearchAsync(string query, int fuzzyThreshold = 80);

        // Category-based Search
        Task<SearchResults> SearchCategoryAsync(string category, string query);
        Task<IEnumerable<SearchCategory>> GetCategoriesAsync();

        // Search History and Suggestions
        Task<IEnumerable<SearchHistory>> GetSearchHistoryAsync(int count = 10);
        Task<IEnumerable<string>> GetSearchSuggestionsAsync(string query);
        Task ClearSearchHistoryAsync();

        // Search Configuration
        Task SetupKeyboardShortcutsAsync(Dictionary<string, string> shortcuts);
        Task<Dictionary<string, string>> GetKeyboardShortcutsAsync();

        // Search Optimization
        Task OptimizeIndexAsync();
        Task<IndexStats> GetIndexStatsAsync();
        Task RebuildIndexAsync();

        // Real-time Search
        Task<SearchResults> RealTimeSearchAsync(string query);
    }

    /// <summary>
    /// Search results container
    /// </summary>
    public class SearchResults
    {
        public List<SearchResult> Items { get; set; } = new List<SearchResult>();
        public int TotalCount { get; set; }
        public double ExecutionTimeMs { get; set; }
        public string Query { get; set; }
        public bool HasMoreResults { get; set; }
    }

    /// <summary>
    /// Individual search result
    /// </summary>
    public class SearchResult
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public double Relevance { get; set; }
        public string[] Highlights { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public DateTime LastModified { get; set; }
    }

    /// <summary>
    /// Search options
    /// </summary>
    public class SearchOptions
    {
        public int MaxResults { get; set; } = 100;
        public int PageSize { get; set; } = 20;
        public int PageNumber { get; set; } = 1;
        public bool IncludeMetadata { get; set; } = true;
        public bool UseFullText { get; set; } = true;
        public bool HighlightMatches { get; set; } = true;
        public string[] Categories { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    /// <summary>
    /// Search filter
    /// </summary>
    public class SearchFilter
    {
        public string Category { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string[] ExcludedKeywords { get; set; }
        public Dictionary<string, object> CustomFilters { get; set; }
    }

    /// <summary>
    /// Sort order
    /// </summary>
    public class SortOrder
    {
        public string Field { get; set; }
        public bool Ascending { get; set; } = true;
    }

    /// <summary>
    /// Search history entry
    /// </summary>
    public class SearchHistory
    {
        public string Query { get; set; }
        public DateTime SearchTime { get; set; }
        public int ResultCount { get; set; }
        public int SelectionCount { get; set; }
    }

    /// <summary>
    /// Search category
    /// </summary>
    public class SearchCategory
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int ItemCount { get; set; }
        public string Icon { get; set; }
    }

    /// <summary>
    /// Index statistics
    /// </summary>
    public class IndexStats
    {
        public int TotalIndexedItems { get; set; }
        public long IndexSizeBytes { get; set; }
        public DateTime LastIndexTime { get; set; }
        public double IndexUpdateFrequencyMs { get; set; }
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
    }
}
