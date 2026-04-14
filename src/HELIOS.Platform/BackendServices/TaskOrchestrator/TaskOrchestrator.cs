using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HELIOS.Platform.BackendServices.TaskOrchestrator
{
    /// <summary>
    /// Task types supported by the orchestrator
    /// </summary>
    public enum TaskType
    {
        Immediate,
        Scheduled,
        Recurring,
        Workflow
    }

    /// <summary>
    /// Task execution status
    /// </summary>
    public enum TaskStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Task priority levels
    /// </summary>
    public enum TaskPriority
    {
        Critical = 1,
        High = 2,
        Normal = 3,
        Low = 4,
        Deferred = 5
    }

    public class TaskModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public TaskType Type { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ScheduledFor { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string CronExpression { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Task execution engine
    /// Manages task scheduling, execution, and monitoring
    /// </summary>
    public interface ITaskOrchestrator
    {
        Task<TaskModel> CreateTaskAsync(TaskModel task);
        Task<TaskModel> GetTaskAsync(Guid taskId);
        Task<IEnumerable<TaskModel>> GetUserTasksAsync(Guid userId);
        Task<bool> ExecuteTaskAsync(Guid taskId);
        Task<bool> CancelTaskAsync(Guid taskId);
        Task<bool> DeleteTaskAsync(Guid taskId);
    }

    public class TaskOrchestrator : ITaskOrchestrator
    {
        private readonly ILogger<TaskOrchestrator> _logger;
        private readonly Dictionary<Guid, TaskModel> _tasks = new();
        private readonly System.Timers.Timer _scheduler;

        public TaskOrchestrator(ILogger<TaskOrchestrator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize scheduler
            _scheduler = new System.Timers.Timer(1000); // Check every second
            _scheduler.Elapsed += ProcessScheduledTasks;
            _scheduler.AutoReset = true;
            _scheduler.Start();

            _logger.LogInformation("Task orchestrator initialized");
        }

        public async Task<TaskModel> CreateTaskAsync(TaskModel task)
        {
            try
            {
                if (task == null)
                    throw new ArgumentNullException(nameof(task));

                _tasks[task.Id] = task;
                _logger.LogInformation($"Task created: {task.Id} ({task.Name})");
                return await Task.FromResult(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                throw;
            }
        }

        public async Task<TaskModel> GetTaskAsync(Guid taskId)
        {
            try
            {
                if (_tasks.TryGetValue(taskId, out var task))
                {
                    return await Task.FromResult(task);
                }
                
                _logger.LogWarning($"Task not found: {taskId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving task {taskId}");
                throw;
            }
        }

        public async Task<IEnumerable<TaskModel>> GetUserTasksAsync(Guid userId)
        {
            try
            {
                var userTasks = _tasks.Values
                    .Where(t => t.UserId == userId)
                    .ToList();

                return await Task.FromResult<IEnumerable<TaskModel>>(userTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving tasks for user {userId}");
                throw;
            }
        }

        public async Task<bool> ExecuteTaskAsync(Guid taskId)
        {
            try
            {
                if (!_tasks.TryGetValue(taskId, out var task))
                {
                    _logger.LogWarning($"Cannot execute: task not found {taskId}");
                    return false;
                }

                task.Status = TaskStatus.Running;
                task.StartedAt = DateTime.UtcNow;

                // Simulate task execution
                await Task.Delay(100);

                task.Status = TaskStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation($"Task executed successfully: {taskId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing task {taskId}");
                if (_tasks.TryGetValue(taskId, out var task))
                {
                    task.Status = TaskStatus.Failed;
                }
                return false;
            }
        }

        public async Task<bool> CancelTaskAsync(Guid taskId)
        {
            try
            {
                if (!_tasks.TryGetValue(taskId, out var task))
                {
                    _logger.LogWarning($"Cannot cancel: task not found {taskId}");
                    return false;
                }

                task.Status = TaskStatus.Cancelled;
                _logger.LogInformation($"Task cancelled: {taskId}");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling task {taskId}");
                return false;
            }
        }

        public async Task<bool> DeleteTaskAsync(Guid taskId)
        {
            try
            {
                var removed = _tasks.Remove(taskId);
                if (removed)
                {
                    _logger.LogInformation($"Task deleted: {taskId}");
                }
                return await Task.FromResult(removed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting task {taskId}");
                return false;
            }
        }

        private void ProcessScheduledTasks(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                var now = DateTime.UtcNow;
                var scheduledTasks = _tasks.Values
                    .Where(t => t.Status == TaskStatus.Pending && 
                                t.ScheduledFor.HasValue && 
                                t.ScheduledFor <= now)
                    .ToList();

                foreach (var task in scheduledTasks)
                {
                    _ = ExecuteTaskAsync(task.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled tasks");
            }
        }

        public void Dispose()
        {
            _scheduler?.Dispose();
        }
    }
}
