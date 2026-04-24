using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HELIOS.Platform.Core.API.Interfaces;
using HELIOS.Platform.Core.Performance;

namespace HELIOS.Platform.Core.API.Services;

/// <summary>
/// High-performance Web UI server with caching and rendering optimization.
/// </summary>
public class WebUIServer : IWebUIServer
{
    private readonly ILogger<WebUIServer> _logger;
    private readonly IL1CacheService? _cache;
    private readonly Dictionary<string, string> _layouts = new();
    private readonly Dictionary<string, string> _pages = new();
    private readonly Dictionary<string, string> _components = new();
    private readonly Dictionary<string, string> _themes = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _isRunning = false;
    private int _activeConnections = 0;
    private long _rendersCompleted = 0;
    private long _renderErrors = 0;
    private long _cacheHits = 0;
    private long _cacheMisses = 0;

    public WebUIServer(ILogger<WebUIServer> logger, IL1CacheService? cache = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache;
        _logger.LogInformation("Web UI Server initialized");
    }

    /// <summary>
    /// Renders a page with optional model data.
    /// </summary>
    public async Task<string> RenderPageAsync(string pageName, Dictionary<string, object>? model = null)
    {
        if (string.IsNullOrWhiteSpace(pageName))
            throw new ArgumentException("Page name cannot be empty", nameof(pageName));

        var startTime = DateTime.UtcNow;
        var cacheKey = $"page:{pageName}:{model?.GetHashCode() ?? 0}";

        try
        {
            Interlocked.Increment(ref _activeConnections);

            // Try cache
            if (_cache != null && _cache.TryGet(cacheKey, out string cachedHtml))
            {
                Interlocked.Increment(ref _cacheHits);
                _logger.LogDebug("Page cache hit: {PageName}", pageName);
                return cachedHtml;
            }

            Interlocked.Increment(ref _cacheMisses);

            await _semaphore.WaitAsync();
            try
            {
                if (!_pages.ContainsKey(pageName))
                {
                    _logger.LogWarning("Page not found: {PageName}", pageName);
                    return $"<html><body>Page not found: {pageName}</body></html>";
                }

                var pageHtml = _pages[pageName];
                var renderedHtml = await RenderWithModelAsync(pageHtml, model);

                // Cache the result
                if (_cache != null)
                    _cache.Set(cacheKey, renderedHtml, TimeSpan.FromMinutes(10));

                Interlocked.Increment(ref _rendersCompleted);
                var elapsed = DateTime.UtcNow - startTime;

                if (elapsed.TotalMilliseconds > 100)
                    _logger.LogWarning("Slow page render detected ({Latency}ms): {PageName}",
                        elapsed.TotalMilliseconds, pageName);

                _logger.LogDebug("Page rendered: {PageName} ({Latency}ms)", pageName, elapsed.TotalMilliseconds);
                return renderedHtml;
            }
            finally
            {
                _semaphore.Release();
                Interlocked.Decrement(ref _activeConnections);
            }
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _renderErrors);
            _logger.LogError(ex, "Error rendering page: {PageName}", pageName);
            throw;
        }
    }

    /// <summary>
    /// Renders a component with optional props.
    /// </summary>
    public async Task<string> RenderComponentAsync(string componentName, Dictionary<string, object>? props = null)
    {
        if (string.IsNullOrWhiteSpace(componentName))
            throw new ArgumentException("Component name cannot be empty", nameof(componentName));

        var startTime = DateTime.UtcNow;
        var cacheKey = $"component:{componentName}:{props?.GetHashCode() ?? 0}";

        try
        {
            // Try cache
            if (_cache != null && _cache.TryGet(cacheKey, out string cachedHtml))
            {
                Interlocked.Increment(ref _cacheHits);
                return cachedHtml;
            }

            Interlocked.Increment(ref _cacheMisses);

            await _semaphore.WaitAsync();
            try
            {
                if (!_components.ContainsKey(componentName))
                {
                    _logger.LogWarning("Component not found: {ComponentName}", componentName);
                    return $"<div>Component not found: {componentName}</div>";
                }

                var componentHtml = _components[componentName];
                var renderedHtml = await RenderWithModelAsync(componentHtml, props);

                // Cache the result
                if (_cache != null)
                    _cache.Set(cacheKey, renderedHtml, TimeSpan.FromMinutes(5));

                Interlocked.Increment(ref _rendersCompleted);
                var elapsed = DateTime.UtcNow - startTime;

                _logger.LogDebug("Component rendered: {ComponentName} ({Latency}ms)", componentName, elapsed.TotalMilliseconds);
                return renderedHtml;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _renderErrors);
            _logger.LogError(ex, "Error rendering component: {ComponentName}", componentName);
            throw;
        }
    }

    private async Task<string> RenderWithModelAsync(string template, Dictionary<string, object>? model)
    {
        var result = template;

        if (model != null)
        {
            foreach (var kvp in model)
            {
                var placeholder = "{{" + kvp.Key + "}}";
                result = result.Replace(placeholder, kvp.Value?.ToString() ?? "");
            }
        }

        return await Task.FromResult(result);
    }

    /// <summary>
    /// Registers a layout template.
    /// </summary>
    public async Task<bool> RegisterLayoutAsync(string layoutName, string html)
    {
        if (string.IsNullOrWhiteSpace(layoutName))
            throw new ArgumentException("Layout name cannot be empty", nameof(layoutName));
        if (html == null)
            throw new ArgumentNullException(nameof(html));

        await _semaphore.WaitAsync();
        try
        {
            _layouts[layoutName] = html;
            _logger.LogInformation("Layout registered: {LayoutName}", layoutName);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Registers a page template.
    /// </summary>
    public async Task<bool> RegisterPageAsync(string pageName, string html)
    {
        if (string.IsNullOrWhiteSpace(pageName))
            throw new ArgumentException("Page name cannot be empty", nameof(pageName));
        if (html == null)
            throw new ArgumentNullException(nameof(html));

        await _semaphore.WaitAsync();
        try
        {
            _pages[pageName] = html;
            _logger.LogInformation("Page registered: {PageName}", pageName);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets a theme by name.
    /// </summary>
    public async Task<string> GetThemeAsync(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
            throw new ArgumentException("Theme name cannot be empty", nameof(themeName));

        await _semaphore.WaitAsync();
        try
        {
            return _themes.TryGetValue(themeName, out var theme)
                ? theme
                : "<style>/* Default theme */</style>";
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Starts the Web UI server.
    /// </summary>
    public async Task<bool> StartServerAsync(int port)
    {
        if (port < 1 || port > 65535)
            throw new ArgumentException("Port must be between 1 and 65535", nameof(port));

        await _semaphore.WaitAsync();
        try
        {
            if (_isRunning)
            {
                _logger.LogWarning("Server is already running on port {Port}", port);
                return false;
            }

            _isRunning = true;
            _logger.LogInformation("Web UI Server started on port {Port}", port);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Stops the Web UI server.
    /// </summary>
    public async Task<bool> StopServerAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_isRunning)
            {
                _logger.LogWarning("Server is not running");
                return false;
            }

            _isRunning = false;
            _logger.LogInformation("Web UI Server stopped");
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets server health status.
    /// </summary>
    public async Task<WebServerHealthStatus> GetHealthAsync()
    {
        return await Task.FromResult(new WebServerHealthStatus
        {
            IsHealthy = _isRunning,
            ActiveConnections = _activeConnections,
            LastHealthCheck = DateTime.UtcNow,
            Status = _isRunning ? "Running" : "Stopped"
        });
    }

    /// <summary>
    /// Gets Web UI server metrics.
    /// </summary>
    public WebUIMetrics GetMetrics()
    {
        return new WebUIMetrics
        {
            TotalRendersCompleted = Interlocked.Read(ref _rendersCompleted),
            TotalRenderErrors = Interlocked.Read(ref _renderErrors),
            ActiveConnections = _activeConnections,
            RegisteredPages = _pages.Count,
            RegisteredComponents = _components.Count,
            RegisteredLayouts = _layouts.Count,
            CacheHits = Interlocked.Read(ref _cacheHits),
            CacheMisses = Interlocked.Read(ref _cacheMisses),
            IsRunning = _isRunning
        };
    }
}

/// <summary>
/// Web UI server metrics.
/// </summary>
public class WebUIMetrics
{
    public long TotalRendersCompleted { get; set; }
    public long TotalRenderErrors { get; set; }
    public int ActiveConnections { get; set; }
    public int RegisteredPages { get; set; }
    public int RegisteredComponents { get; set; }
    public int RegisteredLayouts { get; set; }
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    public double CacheHitRate => (CacheHits + CacheMisses) > 0 ? (double)CacheHits / (CacheHits + CacheMisses) : 0;
    public bool IsRunning { get; set; }
}
