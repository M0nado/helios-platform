using System;
using System.Collections.Generic;
using System.Linq;
using HELIOS.Platform.Core.ActionFlow.Models;

namespace HELIOS.Platform.Core.ActionFlow.Templates
{
    /// <summary>
    /// Represents a template for pages
    /// </summary>
    public class PageTemplate
    {
        public ActionFlowId Id { get; set; } = ActionFlowId.New();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        
        public List<PageElement> Elements { get; set; } = new();
        public Dictionary<string, object> DefaultProperties { get; set; } = new();
        public Dictionary<string, object> LayoutDefinition { get; set; } = new();
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// Represents a template for workflows
    /// </summary>
    public class WorkflowTemplate
    {
        public ActionFlowId Id { get; set; } = ActionFlowId.New();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        
        public List<ActionStep> StepTemplates { get; set; } = new();
        public List<WorkflowTransition> TransitionTemplates { get; set; } = new();
        public Dictionary<string, object> DefaultVariables { get; set; } = new();
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// Represents a template for projects
    /// </summary>
    public class ProjectTemplate
    {
        public ActionFlowId Id { get; set; } = ActionFlowId.New();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        public List<PageTemplate> PageTemplates { get; set; } = new();
        public List<WorkflowTemplate> WorkflowTemplates { get; set; } = new();
        public Dictionary<string, object> ProjectConfig { get; set; } = new();
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// Template manager for creating instances from templates
    /// </summary>
    public class TemplateManager
    {
        private readonly Dictionary<ActionFlowId, PageTemplate> _pageTemplates = new();
        private readonly Dictionary<ActionFlowId, WorkflowTemplate> _workflowTemplates = new();
        private readonly Dictionary<ActionFlowId, ProjectTemplate> _projectTemplates = new();
        
        private readonly Dictionary<string, List<ActionFlowId>> _templatesByCategory = new();
        private readonly object _lockObject = new();

        /// <summary>
        /// Registers a page template
        /// </summary>
        public void RegisterPageTemplate(PageTemplate template)
        {
            lock (_lockObject)
            {
                _pageTemplates[template.Id] = template;
                
                if (!_templatesByCategory.ContainsKey($"page:{template.Category}"))
                    _templatesByCategory[$"page:{template.Category}"] = new();
                    
                _templatesByCategory[$"page:{template.Category}"].Add(template.Id);
            }
        }

        /// <summary>
        /// Registers a workflow template
        /// </summary>
        public void RegisterWorkflowTemplate(WorkflowTemplate template)
        {
            lock (_lockObject)
            {
                _workflowTemplates[template.Id] = template;
                
                if (!_templatesByCategory.ContainsKey($"workflow:{template.Category}"))
                    _templatesByCategory[$"workflow:{template.Category}"] = new();
                    
                _templatesByCategory[$"workflow:{template.Category}"].Add(template.Id);
            }
        }

        /// <summary>
        /// Registers a project template
        /// </summary>
        public void RegisterProjectTemplate(ProjectTemplate template)
        {
            lock (_lockObject)
            {
                _projectTemplates[template.Id] = template;
            }
        }

        /// <summary>
        /// Creates a page instance from template
        /// </summary>
        public PageConfiguration CreatePageFromTemplate(ActionFlowId templateId)
        {
            lock (_lockObject)
            {
                if (!_pageTemplates.TryGetValue(templateId, out var template))
                    throw new InvalidOperationException($"Page template not found: {templateId}");

                var page = new PageConfiguration
                {
                    Id = ActionFlowId.New(),
                    Name = template.Name,
                    Description = template.Description,
                    LayoutData = new Dictionary<string, object>(template.LayoutDefinition),
                    Elements = DeepCopyElements(template.Elements)
                };

                return page;
            }
        }

        /// <summary>
        /// Creates a workflow instance from template
        /// </summary>
        public WorkflowDefinition CreateWorkflowFromTemplate(ActionFlowId templateId)
        {
            lock (_lockObject)
            {
                if (!_workflowTemplates.TryGetValue(templateId, out var template))
                    throw new InvalidOperationException($"Workflow template not found: {templateId}");

                var workflow = new WorkflowDefinition
                {
                    Id = ActionFlowId.New(),
                    Name = template.Name,
                    Description = template.Description,
                    Steps = new List<ActionStep>(template.StepTemplates.Select(s => new ActionStep
                    {
                        Id = ActionFlowId.New(),
                        Name = s.Name,
                        Description = s.Description,
                        ActionType = s.ActionType,
                        Configuration = new Dictionary<string, object>(s.Configuration),
                        InputParameters = new List<string>(s.InputParameters),
                        OutputParameters = new List<string>(s.OutputParameters)
                    })),
                    Transitions = new List<WorkflowTransition>(template.TransitionTemplates),
                    GlobalVariables = new Dictionary<string, object>(template.DefaultVariables)
                };

                return workflow;
            }
        }

        /// <summary>
        /// Creates a project instance from template
        /// </summary>
        public ProjectDefinition CreateProjectFromTemplate(ActionFlowId templateId)
        {
            lock (_lockObject)
            {
                if (!_projectTemplates.TryGetValue(templateId, out var template))
                    throw new InvalidOperationException($"Project template not found: {templateId}");

                var project = new ProjectDefinition
                {
                    Id = ActionFlowId.New(),
                    Name = template.Name,
                    Description = template.Description,
                    GlobalConfig = new Dictionary<string, object>(template.ProjectConfig)
                };

                foreach (var pageTemplate in template.PageTemplates)
                {
                    var page = CreatePageFromTemplate(pageTemplate.Id);
                    project.Pages.Add(page);
                }

                foreach (var workflowTemplate in template.WorkflowTemplates)
                {
                    var workflow = CreateWorkflowFromTemplate(workflowTemplate.Id);
                    project.Workflows.Add(workflow);
                }

                return project;
            }
        }

        /// <summary>
        /// Gets templates by category
        /// </summary>
        public List<PageTemplate> GetPageTemplatesByCategory(string category)
        {
            lock (_lockObject)
            {
                var key = $"page:{category}";
                if (!_templatesByCategory.TryGetValue(key, out var ids))
                    return new();

                return ids
                    .Where(id => _pageTemplates.ContainsKey(id))
                    .Select(id => _pageTemplates[id])
                    .ToList();
            }
        }

        /// <summary>
        /// Gets all page templates
        /// </summary>
        public List<PageTemplate> GetAllPageTemplates()
        {
            lock (_lockObject)
            {
                return _pageTemplates.Values.ToList();
            }
        }

        /// <summary>
        /// Gets all workflow templates
        /// </summary>
        public List<WorkflowTemplate> GetAllWorkflowTemplates()
        {
            lock (_lockObject)
            {
                return _workflowTemplates.Values.ToList();
            }
        }

        /// <summary>
        /// Gets all project templates
        /// </summary>
        public List<ProjectTemplate> GetAllProjectTemplates()
        {
            lock (_lockObject)
            {
                return _projectTemplates.Values.ToList();
            }
        }

        /// <summary>
        /// Saves a page as template
        /// </summary>
        public PageTemplate SavePageAsTemplate(PageConfiguration page, string category, string createdBy)
        {
            var template = new PageTemplate
            {
                Name = page.Name,
                Description = page.Description,
                Category = category,
                Elements = DeepCopyElements(page.Elements),
                LayoutDefinition = new Dictionary<string, object>(page.LayoutData),
                CreatedBy = createdBy
            };

            RegisterPageTemplate(template);
            return template;
        }

        /// <summary>
        /// Saves a workflow as template
        /// </summary>
        public WorkflowTemplate SaveWorkflowAsTemplate(WorkflowDefinition workflow, string category, string createdBy)
        {
            var template = new WorkflowTemplate
            {
                Name = workflow.Name,
                Description = workflow.Description,
                Category = category,
                StepTemplates = new List<ActionStep>(workflow.Steps),
                TransitionTemplates = new List<WorkflowTransition>(workflow.Transitions),
                DefaultVariables = new Dictionary<string, object>(workflow.GlobalVariables),
                CreatedBy = createdBy
            };

            RegisterWorkflowTemplate(template);
            return template;
        }

        /// <summary>
        /// Deep copies page elements
        /// </summary>
        private List<PageElement> DeepCopyElements(List<PageElement> elements)
        {
            var result = new List<PageElement>();
            foreach (var element in elements)
            {
                result.Add(DeepCopyElement(element));
            }
            return result;
        }

        /// <summary>
        /// Deep copies a page element
        /// </summary>
        private PageElement DeepCopyElement(PageElement element)
        {
            return new PageElement
            {
                Id = ActionFlowId.New(),
                ElementType = element.ElementType,
                Name = element.Name,
                Properties = new Dictionary<string, object>(element.Properties),
                BindingRules = new Dictionary<string, object>(element.BindingRules),
                Children = DeepCopyElements(element.Children),
                ZIndex = element.ZIndex,
                IsVisible = element.IsVisible,
                IsEnabled = element.IsEnabled
            };
        }
    }

    /// <summary>
    /// Built-in page templates
    /// </summary>
    public static class BuiltInPageTemplates
    {
        public static PageTemplate CreateBlankTemplate()
        {
            return new PageTemplate
            {
                Name = "Blank Page",
                Description = "A blank page template",
                Category = "Basic"
            };
        }

        public static PageTemplate CreateFormTemplate()
        {
            return new PageTemplate
            {
                Name = "Form Page",
                Description = "A page template with form layout",
                Category = "Basic",
                LayoutDefinition = new Dictionary<string, object>
                {
                    { "type", "form" },
                    { "columns", 1 }
                }
            };
        }

        public static PageTemplate CreateDashboardTemplate()
        {
            return new PageTemplate
            {
                Name = "Dashboard",
                Description = "A dashboard template with widget layout",
                Category = "Dashboard",
                LayoutDefinition = new Dictionary<string, object>
                {
                    { "type", "grid" },
                    { "columns", 3 },
                    { "gap", "16px" }
                }
            };
        }

        public static PageTemplate CreateListTemplate()
        {
            return new PageTemplate
            {
                Name = "List Page",
                Description = "A page template for displaying lists",
                Category = "Data",
                LayoutDefinition = new Dictionary<string, object>
                {
                    { "type", "list" },
                    { "itemsPerPage", 10 }
                }
            };
        }
    }

    /// <summary>
    /// Built-in workflow templates
    /// </summary>
    public static class BuiltInWorkflowTemplates
    {
        public static WorkflowTemplate CreateSimpleWorkflow()
        {
            return new WorkflowTemplate
            {
                Name = "Simple Workflow",
                Description = "A simple linear workflow",
                Category = "Basic"
            };
        }

        public static WorkflowTemplate CreateApprovalWorkflow()
        {
            return new WorkflowTemplate
            {
                Name = "Approval Workflow",
                Description = "A workflow for approval processes",
                Category = "Business",
                StepTemplates = new List<ActionStep>
                {
                    new ActionStep { Name = "Submit", ActionType = ActionType.Simple },
                    new ActionStep { Name = "Review", ActionType = ActionType.Simple },
                    new ActionStep { Name = "Approve or Reject", ActionType = ActionType.Conditional },
                    new ActionStep { Name = "Notify", ActionType = ActionType.Simple }
                }
            };
        }

        public static WorkflowTemplate CreateParallelProcessing()
        {
            return new WorkflowTemplate
            {
                Name = "Parallel Processing",
                Description = "A workflow that processes items in parallel",
                Category = "Advanced",
                StepTemplates = new List<ActionStep>
                {
                    new ActionStep { Name = "Fetch Items", ActionType = ActionType.Simple },
                    new ActionStep { Name = "Process in Parallel", ActionType = ActionType.Parallel },
                    new ActionStep { Name = "Aggregate Results", ActionType = ActionType.Simple }
                }
            };
        }
    }
}
