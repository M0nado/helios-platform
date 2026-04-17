using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HELIOS.Platform.Core.ActionFlow.Models;

namespace HELIOS.Platform.Core.ActionFlow.PageRouter
{
    /// <summary>
    /// Represents a route in the page navigation system
    /// </summary>
    public class PageRoute
    {
        public string Path { get; set; } = string.Empty;
        public ActionFlowId PageId { get; set; }
        public string PageName { get; set; } = string.Empty;
        public Dictionary<string, object> RouteData { get; set; } = new();
        public List<string> RequiredParameters { get; set; } = new();
        public bool IsDefault { get; set; } = false;
    }

    /// <summary>
    /// Represents the navigation history entry
    /// </summary>
    public class NavigationHistoryEntry
    {
        public ActionFlowId PageId { get; set; }
        public string Path { get; set; } = string.Empty;
        public Dictionary<string, object> RouteParameters { get; set; } = new();
        public DateTime NavigatedAt { get; set; } = DateTime.UtcNow;
        public string? PreviousPagePath { get; set; }
    }

    /// <summary>
    /// Page router and navigation manager
    /// </summary>
    public class PageRouter
    {
        private readonly Dictionary<string, PageRoute> _routes = new();
        private readonly Stack<NavigationHistoryEntry> _navigationHistory = new();
        private readonly List<Func<PageRoute, Task<bool>>> _navigationGuards = new();
        private readonly List<Func<ActionFlowId, ActionFlowId, Task>> _navigationListeners = new();

        private ActionFlowId? _currentPageId;
        private string? _currentPath;
        private readonly object _lockObject = new();

        public ActionFlowId? CurrentPageId
        {
            get { lock (_lockObject) { return _currentPageId; } }
        }

        public string? CurrentPath
        {
            get { lock (_lockObject) { return _currentPath; } }
        }

        public int HistoryCount
        {
            get { lock (_lockObject) { return _navigationHistory.Count; } }
        }

        /// <summary>
        /// Registers a page route
        /// </summary>
        public void RegisterRoute(PageRoute route)
        {
            lock (_lockObject)
            {
                _routes[route.Path] = route;
            }
        }

        /// <summary>
        /// Registers multiple routes
        /// </summary>
        public void RegisterRoutes(params PageRoute[] routes)
        {
            lock (_lockObject)
            {
                foreach (var route in routes)
                {
                    _routes[route.Path] = route;
                }
            }
        }

        /// <summary>
        /// Adds a navigation guard
        /// </summary>
        public void AddNavigationGuard(Func<PageRoute, Task<bool>> guard)
        {
            lock (_lockObject)
            {
                _navigationGuards.Add(guard);
            }
        }

        /// <summary>
        /// Navigates to a page by path
        /// </summary>
        public async Task<bool> NavigateToAsync(string path, Dictionary<string, object>? routeParameters = null)
        {
            PageRoute? route;
            lock (_lockObject)
            {
                if (!_routes.TryGetValue(path, out route))
                    return false;
            }

            return await NavigateToPageAsync(route.PageId, path, routeParameters);
        }

        /// <summary>
        /// Navigates to a page by ID
        /// </summary>
        public async Task<bool> NavigateToPageAsync(ActionFlowId pageId, string? path = null, Dictionary<string, object>? routeParameters = null)
        {
            PageRoute? route = null;
            lock (_lockObject)
            {
                route = _routes.Values.FirstOrDefault(r => r.PageId == pageId);
                if (route == null && path != null)
                {
                    // Create implicit route
                    route = new PageRoute
                    {
                        Path = path,
                        PageId = pageId,
                        RouteData = routeParameters ?? new()
                    };
                    _routes[path] = route;
                }

                if (route == null)
                    return false;
            }

            // Check navigation guards
            foreach (var guard in _navigationGuards.ToList())
            {
                if (!await guard(route))
                    return false;
            }

            ActionFlowId? previousPageId;
            lock (_lockObject)
            {
                previousPageId = _currentPageId;

                // Record in history
                if (_currentPath != null && _currentPageId != null)
                {
                    _navigationHistory.Push(new NavigationHistoryEntry
                    {
                        PageId = _currentPageId.Value,
                        Path = _currentPath,
                        RouteParameters = routeParameters ?? new(),
                        PreviousPagePath = path
                    });
                }

                _currentPageId = pageId;
                _currentPath = path ?? route.Path;
            }

            // Notify listeners
            if (previousPageId != null)
            {
                foreach (var listener in _navigationListeners.ToList())
                {
                    await listener(previousPageId.Value, pageId);
                }
            }

            return true;
        }

        /// <summary>
        /// Navigates back in history
        /// </summary>
        public async Task<bool> GoBackAsync()
        {
            NavigationHistoryEntry? previousEntry;
            lock (_lockObject)
            {
                if (_navigationHistory.Count == 0)
                    return false;

                previousEntry = _navigationHistory.Pop();
            }

            return await NavigateToPageAsync(previousEntry.PageId, previousEntry.Path, previousEntry.RouteParameters);
        }

        /// <summary>
        /// Navigates forward (after going back)
        /// </summary>
        public async Task<bool> GoForwardAsync()
        {
            // Note: In a full implementation, you'd maintain a forward stack
            return false;
        }

        /// <summary>
        /// Subscribes to navigation changes
        /// </summary>
        public void SubscribeToNavigation(Func<ActionFlowId, ActionFlowId, Task> listener)
        {
            lock (_lockObject)
            {
                _navigationListeners.Add(listener);
            }
        }

        /// <summary>
        /// Unsubscribes from navigation changes
        /// </summary>
        public void UnsubscribeFromNavigation(Func<ActionFlowId, ActionFlowId, Task> listener)
        {
            lock (_lockObject)
            {
                _navigationListeners.Remove(listener);
            }
        }

        /// <summary>
        /// Gets a route by path
        /// </summary>
        public PageRoute? GetRoute(string path)
        {
            lock (_lockObject)
            {
                _routes.TryGetValue(path, out var route);
                return route;
            }
        }

        /// <summary>
        /// Gets all registered routes
        /// </summary>
        public List<PageRoute> GetAllRoutes()
        {
            lock (_lockObject)
            {
                return _routes.Values.ToList();
            }
        }

        /// <summary>
        /// Gets navigation history
        /// </summary>
        public List<NavigationHistoryEntry> GetHistory()
        {
            lock (_lockObject)
            {
                return _navigationHistory.ToList();
            }
        }

        /// <summary>
        /// Clears navigation history
        /// </summary>
        public void ClearHistory()
        {
            lock (_lockObject)
            {
                _navigationHistory.Clear();
            }
        }
    }

    /// <summary>
    /// Navigation configuration builder
    /// </summary>
    public class NavigationBuilder
    {
        private readonly List<PageRoute> _routes = new();

        public NavigationBuilder AddRoute(string path, ActionFlowId pageId, string pageName)
        {
            _routes.Add(new PageRoute
            {
                Path = path,
                PageId = pageId,
                PageName = pageName
            });
            return this;
        }

        public NavigationBuilder AddRoute(PageRoute route)
        {
            _routes.Add(route);
            return this;
        }

        public List<PageRoute> Build() => _routes;
    }

    /// <summary>
    /// Manages navigation between pages within projects
    /// </summary>
    public class ProjectPageNavigationManager
    {
        private readonly Dictionary<ActionFlowId, PageRouter> _projectRouters = new();
        private readonly Dictionary<ActionFlowId, List<PageNavigation>> _navigationRules = new();
        private readonly object _lockObject = new();

        /// <summary>
        /// Creates a router for a project
        /// </summary>
        public PageRouter CreateProjectRouter(ActionFlowId projectId, ProjectDefinition projectDef)
        {
            var router = new PageRouter();

            // Register all pages as routes
            foreach (var page in projectDef.Pages)
            {
                router.RegisterRoute(new PageRoute
                {
                    Path = $"/project/{projectId}/pages/{page.Id}",
                    PageId = page.Id,
                    PageName = page.Name
                });
            }

            lock (_lockObject)
            {
                _projectRouters[projectId] = router;
                _navigationRules[projectId] = projectDef.Pages
                    .SelectMany(p => p.NavigationRules.Values)
                    .ToList();
            }

            return router;
        }

        /// <summary>
        /// Gets router for a project
        /// </summary>
        public PageRouter? GetProjectRouter(ActionFlowId projectId)
        {
            lock (_lockObject)
            {
                _projectRouters.TryGetValue(projectId, out var router);
                return router;
            }
        }

        /// <summary>
        /// Gets available page navigations from a page
        /// </summary>
        public List<PageNavigation> GetAvailableNavigations(ActionFlowId projectId, ActionFlowId pageId)
        {
            lock (_lockObject)
            {
                if (!_navigationRules.TryGetValue(projectId, out var rules))
                    return new();

                return rules.Where(r => r.TargetPageId == pageId).ToList();
            }
        }
    }
}
