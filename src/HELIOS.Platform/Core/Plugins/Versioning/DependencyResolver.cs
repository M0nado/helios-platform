using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.Plugins.Versioning
{
    /// <summary>
    /// Dependency resolver for plugins
    /// </summary>
    public class DependencyResolver
    {
        private readonly Dictionary<string, List<SemanticVersion>> _availableVersions;
        private readonly Dictionary<string, PluginManifest> _manifests;

        public DependencyResolver()
        {
            _availableVersions = new();
            _manifests = new();
        }

        /// <summary>
        /// Register available versions for a plugin
        /// </summary>
        public void RegisterVersions(string pluginId, params string[] versions)
        {
            if (!_availableVersions.ContainsKey(pluginId))
            {
                _availableVersions[pluginId] = new();
            }

            foreach (var version in versions)
            {
                if (SemanticVersion.TryParse(version, out var semVer))
                {
                    if (!_availableVersions[pluginId].Contains(semVer))
                    {
                        _availableVersions[pluginId].Add(semVer);
                    }
                }
            }

            _availableVersions[pluginId] = _availableVersions[pluginId]
                .OrderByDescending(v => v)
                .ToList();
        }

        /// <summary>
        /// Register a plugin manifest with its dependencies
        /// </summary>
        public void RegisterManifest(string pluginId, string version, PluginManifest manifest)
        {
            var key = $"{pluginId}@{version}";
            _manifests[key] = manifest;
        }

        /// <summary>
        /// Resolve dependencies for a plugin
        /// </summary>
        public ResolutionResult ResolveDependencies(string pluginId, string versionConstraint)
        {
            var result = new ResolutionResult();

            // Find matching version
            if (!_availableVersions.TryGetValue(pluginId, out var versions))
            {
                result.AddError($"Plugin '{pluginId}' not found");
                return result;
            }

            var constraint = new VersionConstraint(versionConstraint);
            var selectedVersion = constraint.FindBestMatch(versions);

            if (selectedVersion == null)
            {
                result.AddError($"No version of '{pluginId}' satisfies constraint '{versionConstraint}'");
                return result;
            }

            result.AddResolved(pluginId, selectedVersion);

            // Recursively resolve dependencies
            var manifestKey = $"{pluginId}@{selectedVersion}";
            if (_manifests.TryGetValue(manifestKey, out var manifest) && manifest.Dependencies != null)
            {
                foreach (var dep in manifest.Dependencies)
                {
                    ResolveDependencyRecursive(dep.PluginId, dep.VersionConstraint, result, new HashSet<string>());
                }
            }

            return result;
        }

        private void ResolveDependencyRecursive(string pluginId, string versionConstraint, ResolutionResult result, HashSet<string> visited)
        {
            if (visited.Contains(pluginId))
            {
                result.AddError($"Circular dependency detected: {pluginId}");
                return;
            }

            visited.Add(pluginId);

            if (!_availableVersions.TryGetValue(pluginId, out var versions))
            {
                result.AddError($"Dependency '{pluginId}' not found");
                return;
            }

            var constraint = new VersionConstraint(versionConstraint);
            var selectedVersion = constraint.FindBestMatch(versions);

            if (selectedVersion == null)
            {
                result.AddError($"No version of '{pluginId}' satisfies constraint '{versionConstraint}'");
                return;
            }

            result.AddResolved(pluginId, selectedVersion);

            // Recursively resolve sub-dependencies
            var manifestKey = $"{pluginId}@{selectedVersion}";
            if (_manifests.TryGetValue(manifestKey, out var manifest) && manifest.Dependencies != null)
            {
                foreach (var dep in manifest.Dependencies)
                {
                    ResolveDependencyRecursive(dep.PluginId, dep.VersionConstraint, result, new HashSet<string>(visited));
                }
            }
        }

        /// <summary>
        /// Check if dependencies are satisfied
        /// </summary>
        public List<string> ValidateDependencies(Dictionary<string, string> installedPlugins)
        {
            var errors = new List<string>();

            foreach (var plugin in installedPlugins)
            {
                var manifestKey = $"{plugin.Key}@{plugin.Value}";
                if (_manifests.TryGetValue(manifestKey, out var manifest) && manifest.Dependencies != null)
                {
                    foreach (var dep in manifest.Dependencies)
                    {
                        if (!installedPlugins.TryGetValue(dep.PluginId, out var depVersion))
                        {
                            if (!dep.IsOptional)
                            {
                                errors.Add($"{plugin.Key} requires {dep.PluginId} (not installed)");
                            }
                        }
                        else
                        {
                            var constraint = new VersionConstraint(dep.VersionConstraint);
                            if (!constraint.IsSatisfiedBy(SemanticVersion.Parse(depVersion)))
                            {
                                errors.Add($"{plugin.Key} requires {dep.PluginId}@{dep.VersionConstraint}, but {depVersion} is installed");
                            }
                        }
                    }
                }
            }

            return errors;
        }
    }

    /// <summary>
    /// Dependency resolution result
    /// </summary>
    public class ResolutionResult
    {
        public Dictionary<string, SemanticVersion> Resolved { get; } = new();
        public List<string> Errors { get; } = new();

        public bool IsSuccessful => Errors.Count == 0;

        public void AddResolved(string pluginId, SemanticVersion version)
        {
            Resolved[pluginId] = version;
        }

        public void AddError(string error)
        {
            Errors.Add(error);
        }

        public override string ToString()
        {
            if (IsSuccessful)
            {
                return $"Resolution successful: {string.Join(", ", Resolved.Select(kv => $"{kv.Key}@{kv.Value}"))}";
            }
            else
            {
                return $"Resolution failed: {string.Join("; ", Errors)}";
            }
        }
    }

    /// <summary>
    /// Plugin manifest with metadata and dependencies
    /// </summary>
    public class PluginManifest
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string License { get; set; }
        public string Homepage { get; set; }
        public string RepositoryUrl { get; set; }
        public string MainAssembly { get; set; }
        public string EntryPoint { get; set; }
        public List<PluginDependencyManifest> Dependencies { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public List<string> Capabilities { get; set; } = new();
        public int Priority { get; set; } = 0;
        public bool AutoStart { get; set; } = true;
    }

    /// <summary>
    /// Plugin dependency entry in manifest
    /// </summary>
    public class PluginDependencyManifest
    {
        public string PluginId { get; set; }
        public string VersionConstraint { get; set; } = "*";
        public bool IsOptional { get; set; } = false;
    }
}
