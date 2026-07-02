using System;
using System.Collections.Generic;

namespace MonadoBlade.GUI.StateManagement.Reducers
{
    /// <summary>
    /// Reducer for cloud sync state mutations (80 LOC)
    /// </summary>
    public static class CloudSyncReducer
    {
        public static AppState Reduce(object payload, AppState state)
        {
            var newState = state.Clone();

            if (payload is CloudSyncPayload syncPayload)
            {
                switch (syncPayload.Action)
                {
                    case "START":
                        newState.CloudSync.IsSyncing = true;
                        newState.CloudSync.SyncStatus = "syncing";
                        newState.CloudSync.SyncProgress = 0;
                        break;

                    case "PROGRESS":
                        if (syncPayload.Value is int progress)
                        {
                            newState.CloudSync.SyncProgress = 
                                Math.Min(100, Math.Max(0, progress));
                        }
                        break;

                    case "COMPLETE":
                        newState.CloudSync.IsSyncing = false;
                        newState.CloudSync.SyncStatus = "idle";
                        newState.CloudSync.SyncProgress = 100;
                        newState.CloudSync.LastSyncTime = DateTime.UtcNow;
                        newState.CloudSync.PendingChanges.Clear();
                        break;

                    case "ERROR":
                        newState.CloudSync.IsSyncing = false;
                        newState.CloudSync.SyncStatus = "error";
                        break;

                    case "CONNECTION_CHANGED":
                        if (syncPayload.Value is bool isConnected)
                        {
                            newState.CloudSync.IsConnected = isConnected;
                            if (!isConnected)
                            {
                                newState.CloudSync.IsSyncing = false;
                                newState.CloudSync.SyncStatus = "offline";
                            }
                        }
                        break;

                    case "ADD_PENDING":
                        if (syncPayload.Value is string change)
                        {
                            if (!newState.CloudSync.PendingChanges.Contains(change))
                            {
                                newState.CloudSync.PendingChanges.Add(change);
                            }
                        }
                        break;

                    case "CLEAR_PENDING":
                        newState.CloudSync.PendingChanges.Clear();
                        break;
                }
            }

            return newState;
        }

        public static void RegisterReducer(AppStateManagement stateManager)
        {
            stateManager.RegisterReducer("CLOUDSYNC", Reduce);
        }
    }

    /// <summary>
    /// Payload for cloud sync actions
    /// </summary>
    public class CloudSyncPayload
    {
        public string Action { get; set; }
        public object Value { get; set; }
    }
}
