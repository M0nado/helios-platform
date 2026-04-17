using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HELIOS.Platform.Core.ActionFlow.Models;
using HELIOS.Platform.Core.ActionFlow.StateManagement;

namespace HELIOS.Platform.Core.ActionFlow.AutoSave
{
    /// <summary>
    /// Represents a conflict when merging changes
    /// </summary>
    public class MergeConflict
    {
        public ActionFlowId ResourceId { get; set; }
        public string ResourceType { get; set; } = string.Empty;
        public object? LocalVersion { get; set; }
        public object? RemoteVersion { get; set; }
        public DateTime ConflictTime { get; set; } = DateTime.UtcNow;
        public string PropertyPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Conflict resolution strategy
    /// </summary>
    public enum ConflictResolutionStrategy
    {
        UseLocal,
        UseRemote,
        Merge,
        Manual
    }

    /// <summary>
    /// Represents a save state checkpoint
    /// </summary>
    public class SaveCheckpoint
    {
        public ActionFlowId Id { get; set; } = ActionFlowId.New();
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<ActionFlowId, string> SerializedWorkflows { get; set; } = new();
        public Dictionary<ActionFlowId, string> SerializedProjects { get; set; } = new();
        public Dictionary<ActionFlowId, string> SerializedPages { get; set; } = new();
        public string? SavedBy { get; set; }
        public bool IsAutoSave { get; set; }
    }

    /// <summary>
    /// Auto-save persistence interface
    /// </summary>
    public interface IAutoSavePersistence
    {
        Task SaveCheckpointAsync(SaveCheckpoint checkpoint);
        Task<SaveCheckpoint?> LoadCheckpointAsync(ActionFlowId id);
        Task<List<SaveCheckpoint>> GetCheckpointsAsync(int limit = 10);
        Task DeleteCheckpointAsync(ActionFlowId id);
    }

    /// <summary>
    /// Auto-save manager with conflict detection and resolution
    /// </summary>
    public class AutoSaveManager
    {
        private readonly StateStore _stateStore;
        private readonly IAutoSavePersistence _persistence;

        private Timer? _autoSaveTimer;
        private int _autoSaveIntervalSeconds = 30;
        private bool _isAutoSaveEnabled = true;

        private readonly Dictionary<ActionFlowId, SaveCheckpoint> _lastSavedStates = new();
        private readonly List<MergeConflict> _detectedConflicts = new();
        private readonly List<Func<List<MergeConflict>, Task<ConflictResolutionStrategy>>> _conflictResolvers = new();

        private readonly object _lockObject = new();

        public event EventHandler<List<MergeConflict>>? ConflictsDetected;
        public event EventHandler<SaveCheckpoint>? SaveCompleted;

        public AutoSaveManager(StateStore stateStore, IAutoSavePersistence persistence)
        {
            _stateStore = stateStore;
            _persistence = persistence;
        }

        /// <summary>
        /// Starts auto-save with specified interval
        /// </summary>
        public void StartAutoSave(int intervalSeconds = 30, bool enableConflictDetection = true)
        {
            lock (_lockObject)
            {
                _autoSaveIntervalSeconds = intervalSeconds;
                _isAutoSaveEnabled = true;

                _autoSaveTimer = new Timer(
                    async _ => await PerformAutoSaveAsync(enableConflictDetection),
                    null,
                    TimeSpan.FromSeconds(intervalSeconds),
                    TimeSpan.FromSeconds(intervalSeconds));
            }
        }

        /// <summary>
        /// Stops auto-save
        /// </summary>
        public void StopAutoSave()
        {
            lock (_lockObject)
            {
                _isAutoSaveEnabled = false;
                _autoSaveTimer?.Dispose();
                _autoSaveTimer = null;
            }
        }

        /// <summary>
        /// Performs manual save
        /// </summary>
        public async Task<SaveCheckpoint> SaveAsync(bool detectConflicts = true, string? savedBy = null)
        {
            var checkpoint = new SaveCheckpoint
            {
                SavedAt = DateTime.UtcNow,
                SavedBy = savedBy,
                IsAutoSave = false
            };

            var state = _stateStore.CurrentState;

            foreach (var workflow in state.Workflows.Values)
            {
                checkpoint.SerializedWorkflows[workflow.Id] = workflow.Serialize();
            }

            foreach (var project in state.Projects.Values)
            {
                checkpoint.SerializedProjects[project.Id] = project.Serialize();
            }

            foreach (var page in state.Pages.Values)
            {
                checkpoint.SerializedPages[page.Id] = page.Serialize();
            }

            if (detectConflicts)
            {
                var conflicts = await DetectConflictsAsync(checkpoint);
                if (conflicts.Count > 0)
                {
                    ConflictsDetected?.Invoke(this, conflicts);
                    var strategy = await ResolveConflictsAsync(conflicts);
                    await ApplyConflictResolution(checkpoint, conflicts, strategy);
                }
            }

            await _persistence.SaveCheckpointAsync(checkpoint);

            lock (_lockObject)
            {
                foreach (var workflow in state.Workflows.Values)
                {
                    _lastSavedStates[workflow.Id] = checkpoint;
                }
            }

            SaveCompleted?.Invoke(this, checkpoint);
            return checkpoint;
        }

        /// <summary>
        /// Performs auto-save
        /// </summary>
        private async Task PerformAutoSaveAsync(bool detectConflicts)
        {
            try
            {
                await SaveAsync(detectConflicts, "system");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-save error: {ex.Message}");
            }
        }

        /// <summary>
        /// Detects conflicts between current state and last saved state
        /// </summary>
        private async Task<List<MergeConflict>> DetectConflictsAsync(SaveCheckpoint currentCheckpoint)
        {
            var conflicts = new List<MergeConflict>();

            foreach (var workflow in _stateStore.CurrentState.Workflows.Values)
            {
                if (_lastSavedStates.TryGetValue(workflow.Id, out var lastCheckpoint))
                {
                    if (lastCheckpoint.SerializedWorkflows.TryGetValue(workflow.Id, out var lastSerialized))
                    {
                        var currentSerialized = workflow.Serialize();
                        if (lastSerialized != currentSerialized)
                        {
                            conflicts.Add(new MergeConflict
                            {
                                ResourceId = workflow.Id,
                                ResourceType = "Workflow",
                                LocalVersion = currentSerialized,
                                RemoteVersion = lastSerialized,
                                PropertyPath = "*"
                            });
                        }
                    }
                }
            }

            await Task.CompletedTask;
            return conflicts;
        }

        /// <summary>
        /// Registers a conflict resolver
        /// </summary>
        public void RegisterConflictResolver(
            Func<List<MergeConflict>, Task<ConflictResolutionStrategy>> resolver)
        {
            lock (_lockObject)
            {
                _conflictResolvers.Add(resolver);
            }
        }

        /// <summary>
        /// Resolves conflicts using registered resolvers
        /// </summary>
        private async Task<ConflictResolutionStrategy> ResolveConflictsAsync(List<MergeConflict> conflicts)
        {
            List<Func<List<MergeConflict>, Task<ConflictResolutionStrategy>>> resolvers;
            lock (_lockObject)
            {
                resolvers = new List<Func<List<MergeConflict>, Task<ConflictResolutionStrategy>>>(_conflictResolvers);
            }

            foreach (var resolver in resolvers)
            {
                var strategy = await resolver(conflicts);
                if (strategy != ConflictResolutionStrategy.Manual)
                    return strategy;
            }

            return ConflictResolutionStrategy.UseLocal;
        }

        /// <summary>
        /// Applies conflict resolution strategy
        /// </summary>
        private async Task ApplyConflictResolution(
            SaveCheckpoint checkpoint,
            List<MergeConflict> conflicts,
            ConflictResolutionStrategy strategy)
        {
            switch (strategy)
            {
                case ConflictResolutionStrategy.UseLocal:
                    break;
                case ConflictResolutionStrategy.UseRemote:
                    foreach (var conflict in conflicts)
                    {
                        if (conflict.RemoteVersion is string serialized)
                        {
                            if (conflict.ResourceType == "Workflow")
                            {
                                var workflow = WorkflowDefinition.Deserialize(serialized);
                                if (workflow != null)
                                    checkpoint.SerializedWorkflows[conflict.ResourceId] = serialized;
                            }
                        }
                    }
                    break;
                case ConflictResolutionStrategy.Merge:
                    await MergeConflictsAsync(checkpoint, conflicts);
                    break;
                case ConflictResolutionStrategy.Manual:
                    lock (_lockObject)
                    {
                        _detectedConflicts.AddRange(conflicts);
                    }
                    break;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Merges conflicting changes
        /// </summary>
        private async Task MergeConflictsAsync(SaveCheckpoint checkpoint, List<MergeConflict> conflicts)
        {
            foreach (var conflict in conflicts)
            {
                if (conflict.ResourceType == "Workflow" &&
                    conflict.LocalVersion is string localSerialized &&
                    conflict.RemoteVersion is string remoteSerialized)
                {
                    var local = WorkflowDefinition.Deserialize(localSerialized);
                    var remote = WorkflowDefinition.Deserialize(remoteSerialized);

                    if (local != null && remote != null)
                    {
                        var merged = MergeWorkflows(local, remote);
                        checkpoint.SerializedWorkflows[conflict.ResourceId] = merged.Serialize();
                    }
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Merges two workflow definitions
        /// </summary>
        private WorkflowDefinition MergeWorkflows(WorkflowDefinition local, WorkflowDefinition remote)
        {
            var merged = new WorkflowDefinition
            {
                Id = local.Id,
                Name = local.Name,
                Description = local.Description,
                Version = Math.Max(local.Version, remote.Version) + 1,
                Steps = new List<ActionStep>(local.Steps),
                Transitions = new List<WorkflowTransition>(local.Transitions),
                GlobalVariables = new Dictionary<string, object>(local.GlobalVariables)
            };

            foreach (var kvp in remote.GlobalVariables)
            {
                if (!merged.GlobalVariables.ContainsKey(kvp.Key))
                    merged.GlobalVariables[kvp.Key] = kvp.Value;
            }

            return merged;
        }

        /// <summary>
        /// Gets detected conflicts
        /// </summary>
        public List<MergeConflict> GetDetectedConflicts()
        {
            lock (_lockObject)
            {
                return new List<MergeConflict>(_detectedConflicts);
            }
        }

        /// <summary>
        /// Clears detected conflicts
        /// </summary>
        public void ClearConflicts()
        {
            lock (_lockObject)
            {
                _detectedConflicts.Clear();
            }
        }

        public void Dispose()
        {
            StopAutoSave();
            _autoSaveTimer?.Dispose();
        }
    }
}
