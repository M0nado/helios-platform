using System;
using System.Collections.Generic;

namespace MonadoBlade.GUI.StateManagement.Reducers
{
    /// <summary>
    /// Reducer for settings state mutations (80 LOC)
    /// </summary>
    public static class SettingsReducer
    {
        public static AppState Reduce(object payload, AppState state)
        {
            var newState = state.Clone();

            if (payload is SettingsPayload settingsPayload)
            {
                switch (settingsPayload.Action)
                {
                    case "SET_THEME":
                        if (settingsPayload.Value is string theme)
                        {
                            newState.Settings.Theme = theme;
                        }
                        break;

                    case "SET_LANGUAGE":
                        if (settingsPayload.Value is string language)
                        {
                            newState.Settings.Language = language;
                        }
                        break;

                    case "TOGGLE_AUTOSAVE":
                        newState.Settings.AutoSave = !newState.Settings.AutoSave;
                        break;

                    case "TOGGLE_NOTIFICATIONS":
                        newState.Settings.NotificationsEnabled = !newState.Settings.NotificationsEnabled;
                        break;

                    case "UPDATE_PREFERENCES":
                        if (settingsPayload.Value is Dictionary<string, object> prefs)
                        {
                            foreach (var kvp in prefs)
                            {
                                newState.Settings.UserPreferences[kvp.Key] = kvp.Value;
                            }
                        }
                        break;

                    case "RESET":
                        newState.Settings = new SettingsState();
                        break;
                }
            }

            return newState;
        }

        public static void RegisterReducer(AppStateManagement stateManager)
        {
            stateManager.RegisterReducer("SETTINGS", Reduce);
        }
    }

    /// <summary>
    /// Payload for settings actions
    /// </summary>
    public class SettingsPayload
    {
        public string Action { get; set; }
        public object Value { get; set; }
    }
}
