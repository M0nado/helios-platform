// HELIOS Platform Action Flow System - Quick Start Guide
// This file demonstrates how to use the system

using HELIOS.Platform.Core.ActionFlow.Models;
using HELIOS.Platform.Core.ActionFlow.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Core.ActionFlow.Examples
{
    /// <summary>
    /// Quick start examples for the Action Flow System
    /// </summary>
    public class QuickStartExamples
    {
        /// <summary>
        /// Example 1: Creating a simple project with a workflow
        /// </summary>
        public static async Task Example1_CreateProjectWithWorkflowAsync()
        {
            Console.WriteLine("=== Example 1: Create Project with Workflow ===\n");

            // Create the action flow service
            var actionFlow = new ActionFlowService();

            // Create a project
            var project = await actionFlow.CreateProjectAsync(
                "Invoice Processing System",
                "Automated invoice approval and processing workflow");

            Console.WriteLine($"✓ Created project: {project.Name}");

            // Create a workflow from template
            var workflowTemplates = actionFlow.GetWorkflowTemplates();
            var approvalTemplate = workflowTemplates.First(w => w.Name == "Approval Workflow");
            var workflow = actionFlow.CreateWorkflowFromTemplate(approvalTemplate.Id);

            Console.WriteLine($"✓ Created workflow from template: {workflow.Name}");

            // Add workflow to project
            project.Workflows.Add(workflow);

            Console.WriteLine($"✓ Project now contains {project.Workflows.Count} workflow(s)\n");
        }

        /// <summary>
        /// Example 2: Executing a workflow and monitoring results
        /// </summary>
        public static async Task Example2_ExecuteAndMonitorWorkflowAsync()
        {
            Console.WriteLine("=== Example 2: Execute and Monitor Workflow ===\n");

            var actionFlow = new ActionFlowService();

            // Create project and workflow
            var project = await actionFlow.CreateProjectAsync("Test Project", "For testing");
            var workflowTemplates = actionFlow.GetWorkflowTemplates();
            var workflow = actionFlow.CreateWorkflowFromTemplate(workflowTemplates.First().Id);
            project.Workflows.Add(workflow);

            // Subscribe to events for monitoring
            var eventCount = 0;
            actionFlow.SubscribeToEvents(@event =>
            {
                eventCount++;
                Console.WriteLine($"📊 Event: {@event.EventType} - {@event.Message}");
            });

            // Execute workflow with input variables
            var variables = new Dictionary<string, object>
            {
                { "invoiceAmount", 5000 },
                { "requestedBy", "john.doe@company.com" },
                { "department", "Finance" }
            };

            Console.WriteLine("Executing workflow...\n");
            var execution = await actionFlow.ExecuteWorkflowAsync(workflow.Id, project.Id, variables);

            Console.WriteLine($"\n✓ Workflow execution completed");
            Console.WriteLine($"  Status: {execution.CurrentState}");
            Console.WriteLine($"  Events recorded: {eventCount}");
            Console.WriteLine($"  Duration: {(execution.CompletedAt - execution.StartedAt)?.TotalSeconds}s\n");

            // Get statistics
            var stats = actionFlow.GetWorkflowStatistics(workflow.Id);
            Console.WriteLine($"✓ Workflow Statistics:");
            Console.WriteLine($"  Total Executions: {stats.TotalExecutions}");
            Console.WriteLine($"  Successful: {stats.SuccessfulExecutions}");
            Console.WriteLine($"  Failed: {stats.FailedExecutions}");
            Console.WriteLine($"  Success Rate: {stats.SuccessRate:F2}%\n");
        }

        /// <summary>
        /// Example 3: Building a workflow visually with drag-and-drop
        /// </summary>
        public static async Task Example3_VisualWorkflowBuilderAsync()
        {
            Console.WriteLine("=== Example 3: Visual Workflow Builder ===\n");

            var actionFlow = new ActionFlowService();
            var builder = actionFlow.GetWorkflowBuilder();

            // Create workflow steps
            var validateStep = new ActionStep
            {
                Name = "Validate Input",
                Description = "Validate incoming data",
                ActionType = ActionType.Simple
            };

            var processStep = new ActionStep
            {
                Name = "Process Data",
                Description = "Process the validated data",
                ActionType = ActionType.Simple
            };

            var checkStep = new ActionStep
            {
                Name = "Quality Check",
                Description = "Perform quality checks",
                ActionType = ActionType.Conditional,
                Configuration = new() { { "condition", "quality > 90" } }
            };

            var finalizeStep = new ActionStep
            {
                Name = "Finalize",
                Description = "Finalize the process",
                ActionType = ActionType.Simple
            };

            // Add steps to builder
            builder.AddStep(validateStep);
            builder.AddStep(processStep);
            builder.AddStep(checkStep);
            builder.AddStep(finalizeStep);

            Console.WriteLine($"✓ Added {builder.GetSteps().Count} steps");

            // Connect steps
            builder.ConnectSteps(validateStep.Id, processStep.Id);
            builder.ConnectSteps(processStep.Id, checkStep.Id);
            builder.ConnectSteps(checkStep.Id, finalizeStep.Id, "passed == true");

            Console.WriteLine($"✓ Created {builder.GetTransitions().Count} transitions");

            // Build workflow
            var workflow = builder.Build("Data Processing Workflow", "Automated data workflow");
            Console.WriteLine($"✓ Built workflow: {workflow.Name}\n");

            // Execute the workflow
            var project = await actionFlow.CreateProjectAsync("Data Processing", "");
            project.Workflows.Add(workflow);

            var execution = await actionFlow.ExecuteWorkflowAsync(
                workflow.Id, 
                project.Id,
                new() { { "quality", 95 } });

            Console.WriteLine($"✓ Workflow execution: {execution.CurrentState}\n");
        }

        /// <summary>
        /// Example 4: Using Undo/Redo functionality
        /// </summary>
        public static async Task Example4_UndoRedoAsync()
        {
            Console.WriteLine("=== Example 4: Undo/Redo Functionality ===\n");

            var actionFlow = new ActionFlowService();

            // Create operations that will be undoable
            var project1 = await actionFlow.CreateProjectAsync("Project 1", "First project");
            Console.WriteLine($"✓ Created: {project1.Name}");

            var project2 = await actionFlow.CreateProjectAsync("Project 2", "Second project");
            Console.WriteLine($"✓ Created: {project2.Name}");

            var project3 = await actionFlow.CreateProjectAsync("Project 3", "Third project");
            Console.WriteLine($"✓ Created: {project3.Name}");

            // Check undo/redo state
            var undoState = actionFlow.GetUndoRedoState();
            Console.WriteLine($"\n📋 Undo/Redo State:");
            Console.WriteLine($"  Can Undo: {undoState.CanUndo}");
            Console.WriteLine($"  Undo Count: {undoState.UndoCount}");

            // Undo last operation
            if (actionFlow.Undo())
            {
                Console.WriteLine("\n✓ Undone: Create Project 3");
            }

            // Undo again
            if (actionFlow.Undo())
            {
                Console.WriteLine("✓ Undone: Create Project 2");
            }

            // Redo
            if (actionFlow.Redo())
            {
                Console.WriteLine("✓ Redone: Create Project 2");
            }

            // Check final state
            undoState = actionFlow.GetUndoRedoState();
            Console.WriteLine($"\n✓ Final Undo/Redo State:");
            Console.WriteLine($"  Can Undo: {undoState.CanUndo} ({undoState.UndoCount} operations)");
            Console.WriteLine($"  Can Redo: {undoState.CanRedo} ({undoState.RedoCount} operations)\n");
        }

        /// <summary>
        /// Example 5: Auto-save with conflict resolution
        /// </summary>
        public static async Task Example5_AutoSaveAsync()
        {
            Console.WriteLine("=== Example 5: Auto-Save with Conflict Resolution ===\n");

            var persistence = new InMemoryAutoSavePersistence();
            var actionFlow = new ActionFlowService(persistence);

            // Configure conflict resolution
            actionFlow._autoSaveManager.RegisterConflictResolver(
                async conflicts =>
                {
                    Console.WriteLine($"⚠️ Conflicts detected: {conflicts.Count}");
                    foreach (var conflict in conflicts)
                    {
                        Console.WriteLine($"   - {conflict.ResourceType}: {conflict.ResourceId}");
                    }
                    return ConflictResolutionStrategy.Merge;
                });

            // Subscribe to save events
            actionFlow._autoSaveManager.SaveCompleted += (s, checkpoint) =>
            {
                Console.WriteLine($"✓ Auto-save completed at {checkpoint.SavedAt}");
                Console.WriteLine($"  Workflows saved: {checkpoint.SerializedWorkflows.Count}");
                Console.WriteLine($"  Pages saved: {checkpoint.SerializedPages.Count}");
            };

            // Start auto-save (every 5 seconds for demo)
            Console.WriteLine("Starting auto-save every 5 seconds...\n");
            actionFlow.StartAutoSave(intervalSeconds: 5);

            // Create some data
            var project = await actionFlow.CreateProjectAsync("Auto-Save Test", "");
            var workflow = actionFlow.CreateWorkflowFromTemplate(
                actionFlow.GetWorkflowTemplates().First().Id);
            project.Workflows.Add(workflow);

            // Wait for auto-save to trigger
            await Task.Delay(6000);

            // Manual save
            Console.WriteLine("\nPerforming manual save...");
            var checkpoint = await actionFlow.SaveAsync("user123");
            Console.WriteLine($"✓ Manual save completed");
            Console.WriteLine($"  Checkpoint ID: {checkpoint.Id}");
            Console.WriteLine($"  Saved by: {checkpoint.SavedBy}\n");

            actionFlow.StopAutoSave();
        }

        /// <summary>
        /// Example 6: Page navigation and routing
        /// </summary>
        public static async Task Example6_PageNavigationAsync()
        {
            Console.WriteLine("=== Example 6: Page Navigation ===\n");

            var actionFlow = new ActionFlowService();

            // Create pages from templates
            var pageTemplates = actionFlow.GetPageTemplates();
            var dashboardPage = actionFlow.CreatePageFromTemplate(
                pageTemplates.First(p => p.Name == "Dashboard").Id);
            var formPage = actionFlow.CreatePageFromTemplate(
                pageTemplates.First(p => p.Name == "Form Page").Id);

            Console.WriteLine($"✓ Created pages:");
            Console.WriteLine($"  - {dashboardPage.Name}");
            Console.WriteLine($"  - {formPage.Name}");

            // Create project and add pages
            var project = await actionFlow.CreateProjectAsync("Page Navigation Demo", "");
            project.Pages.Add(dashboardPage);
            project.Pages.Add(formPage);
            project.InitialPageId = dashboardPage.Id;

            // Create router
            var router = actionFlow.CreateProjectRouter(project.Id);

            // Register routes
            router.RegisterRoute(new PageRoute
            {
                Path = "/dashboard",
                PageId = dashboardPage.Id,
                PageName = "Dashboard"
            });

            router.RegisterRoute(new PageRoute
            {
                Path = "/form",
                PageId = formPage.Id,
                PageName = "Form"
            });

            Console.WriteLine($"\n✓ Registered {router.GetAllRoutes().Count} routes\n");

            // Subscribe to navigation
            router.SubscribeToNavigation((from, to) =>
            {
                Console.WriteLine($"  🔄 Navigated from {from} to {to}");
                return Task.CompletedTask;
            });

            // Navigate
            Console.WriteLine("Navigating to /dashboard...");
            await router.NavigateToAsync("/dashboard");

            Console.WriteLine("Navigating to /form...");
            await router.NavigateToAsync("/form");

            Console.WriteLine("Going back...");
            await router.GoBackAsync();

            Console.WriteLine($"\n✓ Navigation history: {router.HistoryCount} entries\n");
        }

        /// <summary>
        /// Example 7: Comprehensive example with all features
        /// </summary>
        public static async Task Example7_ComprehensiveAsync()
        {
            Console.WriteLine("=== Example 7: Comprehensive Demo ===\n");

            var actionFlow = new ActionFlowService();

            // 1. Create Project
            Console.WriteLine("1️⃣  Creating project...");
            var project = await actionFlow.CreateProjectAsync("Complete Demo", "Full feature demo");

            // 2. Create Pages
            Console.WriteLine("2️⃣  Creating pages...");
            var dashboardPage = actionFlow.CreatePageFromTemplate(
                actionFlow.GetPageTemplates().First(p => p.Name == "Dashboard").Id);
            project.Pages.Add(dashboardPage);

            // 3. Create Workflow
            Console.WriteLine("3️⃣  Creating workflow...");
            var workflow = actionFlow.CreateWorkflowFromTemplate(
                actionFlow.GetWorkflowTemplates().First().Id);
            project.Workflows.Add(workflow);

            // 4. Start Auto-Save
            Console.WriteLine("4️⃣  Starting auto-save...");
            actionFlow.StartAutoSave(intervalSeconds: 30);

            // 5. Execute Workflow
            Console.WriteLine("5️⃣  Executing workflow...");
            var execution = await actionFlow.ExecuteWorkflowAsync(
                workflow.Id,
                project.Id,
                new() { { "userId", "demo-user" } });

            // 6. Get Statistics
            Console.WriteLine("6️⃣  Collecting statistics...");
            var stats = actionFlow.GetWorkflowStatistics(workflow.Id);

            // 7. Check Health
            Console.WriteLine("7️⃣  Checking system health...");
            var health = actionFlow.CheckHealth();

            // 8. Display Results
            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("✅ COMPREHENSIVE DEMO RESULTS");
            Console.WriteLine(new string('=', 50));
            Console.WriteLine($"\nProject: {project.Name}");
            Console.WriteLine($"  Pages: {project.Pages.Count}");
            Console.WriteLine($"  Workflows: {project.Workflows.Count}");
            Console.WriteLine($"\nWorkflow Execution:");
            Console.WriteLine($"  Status: {execution.CurrentState}");
            Console.WriteLine($"  Started: {execution.StartedAt}");
            Console.WriteLine($"  Completed: {execution.CompletedAt}");
            Console.WriteLine($"\nStatistics:");
            Console.WriteLine($"  Total Executions: {stats.TotalExecutions}");
            Console.WriteLine($"  Success Rate: {stats.SuccessRate:F2}%");
            Console.WriteLine($"  Avg Duration: {stats.AverageDuration:F2}s");
            Console.WriteLine($"\nHealth Status:");
            foreach (var check in health)
            {
                var status = check.Value ? "✓" : "✗";
                Console.WriteLine($"  {status} {check.Key}");
            }
            Console.WriteLine();

            actionFlow.StopAutoSave();
        }

        /// <summary>
        /// Main entry point - Run all examples
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   HELIOS Platform Action Flow System - Quick Start Examples    ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            try
            {
                // Run examples
                await Example1_CreateProjectWithWorkflowAsync();
                Console.WriteLine(new string('─', 70) + "\n");

                await Example2_ExecuteAndMonitorWorkflowAsync();
                Console.WriteLine(new string('─', 70) + "\n");

                await Example3_VisualWorkflowBuilderAsync();
                Console.WriteLine(new string('─', 70) + "\n");

                await Example4_UndoRedoAsync();
                Console.WriteLine(new string('─', 70) + "\n");

                await Example5_AutoSaveAsync();
                Console.WriteLine(new string('─', 70) + "\n");

                await Example6_PageNavigationAsync();
                Console.WriteLine(new string('─', 70) + "\n");

                await Example7_ComprehensiveAsync();

                Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                   ✅ All Examples Completed!                  ║");
                Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
