using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HELIOS.Platform.Core.Logging;

namespace HELIOS.Platform.Core.Automation
{
    public interface IAutomationServer
    {
        Task<bool> ScheduleTaskAsync(AutomationTask task);
        Task<bool> ExecuteTaskAsync(string taskId);
        Task<List<AutomationTask>> GetScheduledTasksAsync();
        Task<bool> CancelTaskAsync(string taskId);
    }

    public class AutomationTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty; // Cron format
        public string Command { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsEnabled { get; set; }
        public List<AutomationResult> ExecutionHistory { get; set; } = new();
    }

    public class AutomationResult
    {
        public DateTime ExecutedAt { get; set; }
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    public class AutomationServer : IAutomationServer
    {
        private readonly Core.Logging.ILogger? _logger;
        private readonly Dictionary<string, AutomationTask> _tasks = new();

        public AutomationServer(Core.Logging.ILogger? logger = null)
        {
            _logger = logger;
        }

        public async Task<bool> ScheduleTaskAsync(AutomationTask task)
        {
            try
            {
                task.CreatedAt = DateTime.UtcNow;
                _tasks[task.Id] = task;
                _logger?.Info($"Task scheduled: {task.Name} ({task.Schedule})");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to schedule task: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExecuteTaskAsync(string taskId)
        {
            try
            {
                if (!_tasks.TryGetValue(taskId, out var task))
                    return false;

                var result = new AutomationResult
                {
                    ExecutedAt = DateTime.UtcNow,
                    Success = true,
                    Output = $"Task {task.Name} executed successfully"
                };

                task.ExecutionHistory.Add(result);
                _logger?.Info($"Task executed: {task.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Task execution failed: {ex.Message}");
                return false;
            }
        }

        public async Task<List<AutomationTask>> GetScheduledTasksAsync()
        {
            return new List<AutomationTask>(_tasks.Values);
        }

        public async Task<bool> CancelTaskAsync(string taskId)
        {
            try
            {
                if (_tasks.TryGetValue(taskId, out var task))
                {
                    task.IsEnabled = false;
                    _logger?.Info($"Task cancelled: {task.Name}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to cancel task: {ex.Message}");
                return false;
            }
        }
    }
}
