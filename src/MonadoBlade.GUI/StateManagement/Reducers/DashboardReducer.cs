using System;
using System.Collections.Generic;
using System.Linq;

namespace MonadoBlade.GUI.StateManagement.Reducers
{
    /// <summary>
    /// Reducer for dashboard state mutations (100 LOC)
    /// </summary>
    public static class DashboardReducer
    {
        public static AppState Reduce(object payload, AppState state)
        {
            var newState = state.Clone();

            if (payload is DashboardPayload dashPayload)
            {
                switch (dashPayload.Action)
                {
                    case "LOADING":
                        newState.Dashboard.IsLoading = true;
                        break;

                    case "SET_VIEW":
                        newState.Dashboard.CurrentView = dashPayload.Value?.ToString() ?? "overview";
                        newState.Dashboard.IsLoading = false;
                        break;

                    case "ADD_WIDGET":
                        if (dashPayload.Widget != null)
                        {
                            newState.Dashboard.Widgets.Add(dashPayload.Widget);
                        }
                        break;

                    case "REMOVE_WIDGET":
                        if (dashPayload.Value is string widgetId)
                        {
                            newState.Dashboard.Widgets.RemoveAll(w => w.Id == widgetId);
                        }
                        break;

                    case "UPDATE_METRICS":
                        if (dashPayload.Value is Dictionary<string, object> metrics)
                        {
                            foreach (var kvp in metrics)
                            {
                                newState.Dashboard.Metrics[kvp.Key] = kvp.Value;
                            }
                            newState.Dashboard.LastRefresh = DateTime.UtcNow;
                        }
                        break;

                    case "CLEAR":
                        newState.Dashboard.Widgets.Clear();
                        newState.Dashboard.Metrics.Clear();
                        break;
                }
            }

            return newState;
        }

        public static void RegisterReducer(AppStateManagement stateManager)
        {
            stateManager.RegisterReducer("DASHBOARD", Reduce);
        }
    }

    /// <summary>
    /// Payload for dashboard actions
    /// </summary>
    public class DashboardPayload
    {
        public string Action { get; set; }
        public object Value { get; set; }
        public WidgetState Widget { get; set; }
    }
}
