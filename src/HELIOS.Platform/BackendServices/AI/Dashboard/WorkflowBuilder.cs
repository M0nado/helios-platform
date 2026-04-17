using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;

namespace HELIOS.Platform.BackendServices.AI.Dashboard
{
    /// <summary>
    /// Visual workflow builder enabling drag-drop automation configuration
    /// </summary>
    public interface IWorkflowBuilder
    {
        Task<WorkflowDefinition> CreateWorkflowAsync(string name);
        Task<WorkflowDefinition> GetWorkflowAsync(string workflowId);
        Task AddStepAsync(string workflowId, WorkflowStep step);
        Task ConnectStepsAsync(string workflowId, string fromStepId, string toStepId);
        Task RemoveStepAsync(string workflowId, string stepId);
        Task<WorkflowValidationResult> ValidateWorkflowAsync(string workflowId);
        Task<WorkflowExecutionResult> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> inputs);
        Task SaveWorkflowAsync(string workflowId, string filePath);
        Task<WorkflowDefinition> LoadWorkflowAsync(string filePath);
    }

    public class WorkflowBuilder : IWorkflowBuilder
    {
        private readonly ConcurrentDictionary<string, WorkflowDefinition> _workflows;
        private readonly ConcurrentDictionary<string, WorkflowExecutionContext> _executionContexts;

        public WorkflowBuilder()
        {
            _workflows = new ConcurrentDictionary<string, WorkflowDefinition>();
            _executionContexts = new ConcurrentDictionary<string, WorkflowExecutionContext>();
        }

        public async Task<WorkflowDefinition> CreateWorkflowAsync(string name)
        {
            var workflow = new WorkflowDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Steps = new List<WorkflowStep>(),
                Configuration = new Dictionary<string, object>(),
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            _workflows.TryAdd(workflow.Id, workflow);
            return await Task.FromResult(workflow);
        }

        public async Task<WorkflowDefinition> GetWorkflowAsync(string workflowId)
        {
            if (_workflows.TryGetValue(workflowId, out var workflow))
                return await Task.FromResult(workflow);

            throw new KeyNotFoundException($"Workflow {workflowId} not found");
        }

        public async Task AddStepAsync(string workflowId, WorkflowStep step)
        {
            var workflow = await GetWorkflowAsync(workflowId);
            step.StepId = step.StepId ?? Guid.NewGuid().ToString();
            step.DependsOn = step.DependsOn ?? new List<string>();
            
            workflow.Steps.Add(step);
            workflow.ModifiedAt = DateTime.UtcNow;

            await Task.CompletedTask;
        }

        public async Task ConnectStepsAsync(string workflowId, string fromStepId, string toStepId)
        {
            var workflow = await GetWorkflowAsync(workflowId);
            var toStep = workflow.Steps.FirstOrDefault(s => s.StepId == toStepId);

            if (toStep == null)
                throw new KeyNotFoundException($"Step {toStepId} not found");

            if (!toStep.DependsOn.Contains(fromStepId))
                toStep.DependsOn.Add(fromStepId);

            workflow.ModifiedAt = DateTime.UtcNow;
            await Task.CompletedTask;
        }

        public async Task RemoveStepAsync(string workflowId, string stepId)
        {
            var workflow = await GetWorkflowAsync(workflowId);
            
            workflow.Steps.RemoveAll(s => s.StepId == stepId);
            foreach (var step in workflow.Steps)
            {
                step.DependsOn?.Remove(stepId);
            }

            workflow.ModifiedAt = DateTime.UtcNow;
            await Task.CompletedTask;
        }

        public async Task<WorkflowValidationResult> ValidateWorkflowAsync(string workflowId)
        {
            var workflow = await GetWorkflowAsync(workflowId);
            var errors = new List<string>();
            var warnings = new List<string>();

            if (!workflow.Steps.Any())
            {
                errors.Add("Workflow must have at least one step");
            }

            // Check for circular dependencies
            var circularDeps = DetectCircularDependencies(workflow);
            if (circularDeps.Any())
            {
                errors.AddRange(circularDeps.Select(cd => $"Circular dependency detected: {cd}"));
            }

            // Check for orphaned steps
            var orphaned = workflow.Steps.Where(s => s.DependsOn?.Any() == false && workflow.Steps.IndexOf(s) > 0);
            if (orphaned.Any())
            {
                warnings.Add($"Found {orphaned.Count()} orphaned steps without dependencies");
            }

            return await Task.FromResult(new WorkflowValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors,
                Warnings = warnings,
                ValidatedAt = DateTime.UtcNow
            });
        }

        public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object> inputs)
        {
            var validation = await ValidateWorkflowAsync(workflowId);
            if (!validation.IsValid)
            {
                return new WorkflowExecutionResult
                {
                    WorkflowId = workflowId,
                    Status = ExecutionStatus.Failed,
                    Errors = validation.Errors,
                    ExecutedAt = DateTime.UtcNow
                };
            }

            var workflow = await GetWorkflowAsync(workflowId);
            var executionId = Guid.NewGuid().ToString();
            var context = new WorkflowExecutionContext
            {
                ExecutionId = executionId,
                WorkflowId = workflowId,
                StartTime = DateTime.UtcNow,
                Inputs = inputs,
                StepResults = new Dictionary<string, object>()
            };

            _executionContexts.TryAdd(executionId, context);

            try
            {
                var orderedSteps = TopologicalSort(workflow.Steps);
                
                foreach (var step in orderedSteps)
                {
                    var stepResult = await ExecuteStepAsync(step, context);
                    context.StepResults[step.StepId] = stepResult;
                    
                    if (stepResult == null)
                    {
                        context.Errors.Add($"Step {step.StepId} failed");
                        break;
                    }
                }

                context.Status = context.Errors.Any() ? ExecutionStatus.PartialFailure : ExecutionStatus.Success;
            }
            catch (Exception ex)
            {
                context.Status = ExecutionStatus.Failed;
                context.Errors.Add(ex.Message);
            }

            context.EndTime = DateTime.UtcNow;

            return await Task.FromResult(new WorkflowExecutionResult
            {
                WorkflowId = workflowId,
                ExecutionId = executionId,
                Status = context.Status,
                StepResults = context.StepResults,
                Errors = context.Errors,
                ExecutedAt = context.StartTime,
                DurationMs = (long)(context.EndTime - context.StartTime).TotalMilliseconds
            });
        }

        public async Task SaveWorkflowAsync(string workflowId, string filePath)
        {
            var workflow = await GetWorkflowAsync(workflowId);
            var json = JsonSerializer.Serialize(workflow, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filePath, json);
        }

        public async Task<WorkflowDefinition> LoadWorkflowAsync(string filePath)
        {
            var json = System.IO.File.ReadAllText(filePath);
            var workflow = JsonSerializer.Deserialize<WorkflowDefinition>(json);
            
            if (workflow != null)
            {
                _workflows.TryAdd(workflow.Id, workflow);
            }

            return await Task.FromResult(workflow);
        }

        private async Task<object> ExecuteStepAsync(WorkflowStep step, WorkflowExecutionContext context)
        {
            await Task.Delay(100); // Simulate execution
            return new { success = true, stepId = step.StepId };
        }

        private List<string> DetectCircularDependencies(WorkflowDefinition workflow)
        {
            var circularDeps = new List<string>();
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var step in workflow.Steps)
            {
                if (!visited.Contains(step.StepId))
                {
                    DfsForCycle(step.StepId, workflow.Steps, visited, recursionStack, circularDeps);
                }
            }

            return circularDeps;
        }

        private void DfsForCycle(string stepId, List<WorkflowStep> steps, HashSet<string> visited, 
            HashSet<string> recursionStack, List<string> circularDeps)
        {
            visited.Add(stepId);
            recursionStack.Add(stepId);

            var step = steps.FirstOrDefault(s => s.StepId == stepId);
            if (step?.DependsOn != null)
            {
                foreach (var dependency in step.DependsOn)
                {
                    if (!visited.Contains(dependency))
                    {
                        DfsForCycle(dependency, steps, visited, recursionStack, circularDeps);
                    }
                    else if (recursionStack.Contains(dependency))
                    {
                        circularDeps.Add($"{stepId} -> {dependency}");
                    }
                }
            }

            recursionStack.Remove(stepId);
        }

        private List<WorkflowStep> TopologicalSort(List<WorkflowStep> steps)
        {
            var sorted = new List<WorkflowStep>();
            var visited = new HashSet<string>();

            foreach (var step in steps)
            {
                if (!visited.Contains(step.StepId))
                {
                    TopologicalSortDfs(step, steps, visited, sorted);
                }
            }

            return sorted;
        }

        private void TopologicalSortDfs(WorkflowStep step, List<WorkflowStep> allSteps, 
            HashSet<string> visited, List<WorkflowStep> sorted)
        {
            visited.Add(step.StepId);

            if (step.DependsOn != null)
            {
                foreach (var depId in step.DependsOn)
                {
                    var depStep = allSteps.FirstOrDefault(s => s.StepId == depId);
                    if (depStep != null && !visited.Contains(depId))
                    {
                        TopologicalSortDfs(depStep, allSteps, visited, sorted);
                    }
                }
            }

            sorted.Add(step);
        }
    }

    public class WorkflowExecutionContext
    {
        public string ExecutionId { get; set; }
        public string WorkflowId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Dictionary<string, object> Inputs { get; set; }
        public Dictionary<string, object> StepResults { get; set; }
        public List<string> Errors { get; set; } = new();
        public ExecutionStatus Status { get; set; }
    }

    public class WorkflowValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public DateTime ValidatedAt { get; set; }
    }

    public class WorkflowExecutionResult
    {
        public string WorkflowId { get; set; }
        public string ExecutionId { get; set; }
        public ExecutionStatus Status { get; set; }
        public Dictionary<string, object> StepResults { get; set; }
        public List<string> Errors { get; set; }
        public DateTime ExecutedAt { get; set; }
        public long DurationMs { get; set; }
    }

    public enum ExecutionStatus
    {
        Pending,
        Running,
        Success,
        PartialFailure,
        Failed,
        Cancelled
    }
}
