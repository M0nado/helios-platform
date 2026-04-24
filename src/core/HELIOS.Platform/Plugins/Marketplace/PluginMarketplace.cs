namespace HELIOS.Platform.Plugins;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

/// <summary>
/// Plugin marketplace for discovering, downloading, and publishing plugins.
/// </summary>
public class PluginMarketplace : IPluginMarketplace
{
    private readonly string _cacheDirectory;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, PluginMetadata> _pluginCatalog;
    private readonly Dictionary<string, List<(int Rating, string Review)>> _pluginRatings;

    public PluginMarketplace(string cacheDirectory)
    {
        _cacheDirectory = cacheDirectory;
        _httpClient = new HttpClient();
        _pluginCatalog = new Dictionary<string, PluginMetadata>();
        _pluginRatings = new Dictionary<string, List<(int, string)>>();
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<List<PluginMetadata>> SearchPluginsAsync(string query)
    {
        if (string.IsNullOrEmpty(query))
            return _pluginCatalog.Values.ToList();

        var results = _pluginCatalog.Values
            .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       p.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       p.Author.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return await Task.FromResult(results);
    }

    public async Task<PluginMetadata> GetPluginDetailsAsync(string pluginId)
    {
        if (!_pluginCatalog.TryGetValue(pluginId, out var plugin))
            throw new PluginException(pluginId, "Plugin not found in marketplace");

        return await Task.FromResult(plugin);
    }

    public async Task<byte[]> DownloadPluginAsync(string pluginId)
    {
        var plugin = await GetPluginDetailsAsync(pluginId);
        var cachePath = Path.Combine(_cacheDirectory, $"{pluginId}.dll");

        if (File.Exists(cachePath))
            return File.ReadAllBytes(cachePath);

        // In real implementation, would download from marketplace server
        // For now, return empty byte array
        return await Task.FromResult(Array.Empty<byte>());
    }

    public async Task<bool> PublishPluginAsync(IPlugin plugin, string marketplaceKey)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        if (string.IsNullOrEmpty(marketplaceKey)) throw new ArgumentNullException(nameof(marketplaceKey));

        var metadata = plugin.Metadata;
        _pluginCatalog[metadata.Id] = metadata;

        return await Task.FromResult(true);
    }

    public async Task RatePluginAsync(string pluginId, int rating, string review)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5");

        if (!_pluginRatings.ContainsKey(pluginId))
            _pluginRatings[pluginId] = new List<(int, string)>();

        _pluginRatings[pluginId].Add((rating, review));
        await Task.CompletedTask;
    }

    public async Task<List<(PluginMetadata Plugin, double Rating)>> GetTopPluginsAsync(int count)
    {
        var topPlugins = _pluginCatalog.Values
            .Select(p => (
                Plugin: p,
                AverageRating: _pluginRatings.ContainsKey(p.Id)
                    ? _pluginRatings[p.Id].Average(r => r.Rating)
                    : 0.0
            ))
            .OrderByDescending(x => x.AverageRating)
            .Take(count)
            .Select(x => (x.Plugin, x.AverageRating))
            .ToList();

        return await Task.FromResult(topPlugins);
    }
}
