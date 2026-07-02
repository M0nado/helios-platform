using System;
using System.Collections.Generic;
using System.Linq;

namespace MonadoBlade.GUI.StateManagement.Reducers
{
    /// <summary>
    /// Reducer for plugin system state mutations (90 LOC)
    /// </summary>
    public static class PluginReducer
    {
        public static AppState Reduce(object payload, AppState state)
        {
            var newState = state.Clone();

            if (payload is PluginPayload pluginPayload)
            {
                switch (pluginPayload.Action)
                {
                    case "LOADING":
                        newState.Plugins.IsLoadingPlugins = true;
                        break;

                    case "INSTALL":
                        if (pluginPayload.Plugin != null)
                        {
                            var existing = newState.Plugins.InstalledPlugins
                                .FirstOrDefault(p => p.Id == pluginPayload.Plugin.Id);
                            
                            if (existing == null)
                            {
                                pluginPayload.Plugin.InstalledDate = DateTime.UtcNow;
                                newState.Plugins.InstalledPlugins.Add(pluginPayload.Plugin);
                            }
                            else
                            {
                                existing.Version = pluginPayload.Plugin.Version;
                            }
                        }
                        newState.Plugins.IsLoadingPlugins = false;
                        break;

                    case "UNINSTALL":
                        if (pluginPayload.Value is string pluginId)
                        {
                            newState.Plugins.InstalledPlugins
                                .RemoveAll(p => p.Id == pluginId);
                        }
                        break;

                    case "ENABLE":
                        if (pluginPayload.Value is string enableId)
                        {
                            var plugin = newState.Plugins.InstalledPlugins
                                .FirstOrDefault(p => p.Id == enableId);
                            if (plugin != null)
                                plugin.IsEnabled = true;
                        }
                        break;

                    case "DISABLE":
                        if (pluginPayload.Value is string disableId)
                        {
                            var plugin = newState.Plugins.InstalledPlugins
                                .FirstOrDefault(p => p.Id == disableId);
                            if (plugin != null)
                                plugin.IsEnabled = false;
                        }
                        break;

                    case "SET_CURRENT":
                        newState.Plugins.CurrentPlugin = pluginPayload.Value?.ToString();
                        break;

                    case "UPDATE_DATA":
                        if (pluginPayload.Value is Dictionary<string, object> data)
                        {
                            foreach (var kvp in data)
                            {
                                newState.Plugins.PluginData[kvp.Key] = kvp.Value;
                            }
                        }
                        break;
                }
            }

            return newState;
        }

        public static void RegisterReducer(AppStateManagement stateManager)
        {
            stateManager.RegisterReducer("PLUGIN", Reduce);
        }
    }

    /// <summary>
    /// Payload for plugin actions
    /// </summary>
    public class PluginPayload
    {
        public string Action { get; set; }
        public object Value { get; set; }
        public PluginInfo Plugin { get; set; }
    }
}
