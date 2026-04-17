namespace HELIOS.Platform.Core.ActionFlow;

/// <summary>
/// PowerShell script execution action - executes a PowerShell script and captures output.
/// </summary>
public class PowerShellAction : IWorkflowAction
{
    private readonly List<IActionCondition> _conditions = new();
    private readonly List<Guid> _parallelActionIds = new();

    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string ActionType => "PowerShell";
    public IReadOnlyDictionary<string, ActionParameter> Parameters { get; }
    public IReadOnlyList<IActionCondition> Conditions => _conditions.AsReadOnly();
    public Guid? NextActionId { get; set; }
    public IReadOnlyList<Guid> ParallelActionIds => _parallelActionIds.AsReadOnly();
    public RetryPolicy? RetryPolicy { get; set; }

    public string Script { get; set; } = string.Empty;
    public bool RunAsAdmin { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 300;

    public PowerShellAction(string name, string description, string script)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Script = script;

        Parameters = new Dictionary<string, ActionParameter>
        {
            {
                "script",
                new ActionParameter
                {
                    Name = "script",
                    Type = "string",
                    Required = true,
                    Description = "PowerShell script to execute"
                }
            }
        };
    }

    public async Task<ActionResult> ExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var scriptToRun = context.GetVariable("scriptOverride", Script) as string ?? Script;
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{scriptToRun}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start PowerShell process");

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(TimeoutSeconds * 1000))
            {
                process.Kill();
                throw new TimeoutException($"PowerShell execution exceeded timeout of {TimeoutSeconds} seconds");
            }

            var output = await outputTask;
            var error = await errorTask;
            var duration = DateTime.UtcNow - startTime;

            return new ActionResult
            {
                Success = process.ExitCode == 0,
                Message = process.ExitCode == 0 ? "PowerShell script executed successfully" : "PowerShell script exited with error",
                Data = output,
                ExecutionTime = duration,
                ErrorMessage = error,
                Exception = process.ExitCode != 0 ? new Exception(error) : null
            };
        }
        catch (Exception ex)
        {
            return new ActionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTime = DateTime.UtcNow - startTime,
                Exception = ex
            };
        }
    }

    public ValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Script))
            errors.Add("Script cannot be empty");

        return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }

    public void AddCondition(IActionCondition condition) => _conditions.Add(condition);
    public void AddParallelAction(Guid actionId) => _parallelActionIds.Add(actionId);
}

/// <summary>
/// REST API call action - makes HTTP requests and processes responses.
/// </summary>
public class RestApiAction : IWorkflowAction
{
    private readonly List<IActionCondition> _conditions = new();
    private readonly List<Guid> _parallelActionIds = new();
    private static readonly HttpClient _httpClient = new();

    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string ActionType => "RestAPI";
    public IReadOnlyDictionary<string, ActionParameter> Parameters { get; }
    public IReadOnlyList<IActionCondition> Conditions => _conditions.AsReadOnly();
    public Guid? NextActionId { get; set; }
    public IReadOnlyList<Guid> ParallelActionIds => _parallelActionIds.AsReadOnly();
    public RetryPolicy? RetryPolicy { get; set; }

    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Body { get; set; }
    public int TimeoutSeconds { get; set; } = 30;

    public RestApiAction(string name, string description, string url, string method = "GET")
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Url = url;
        Method = method;

        Parameters = new Dictionary<string, ActionParameter>
        {
            { "url", new ActionParameter { Name = "url", Type = "string", Required = true } },
            { "method", new ActionParameter { Name = "method", Type = "string", Required = false, DefaultValue = "GET" } }
        };
    }

    public async Task<ActionResult> ExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var url = context.GetVariable("urlOverride", Url) as string ?? Url;
            using var request = new HttpRequestMessage(new HttpMethod(Method), url);

            foreach (var header in Headers)
                request.Headers.Add(header.Key, header.Value);

            if (!string.IsNullOrEmpty(Body))
                request.Content = new StringContent(Body, System.Text.Encoding.UTF8, "application/json");

            _httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            return new ActionResult
            {
                Success = response.IsSuccessStatusCode,
                Message = $"API call completed with status {response.StatusCode}",
                Data = content,
                ExecutionTime = duration,
                ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {response.StatusCode}: {content}"
            };
        }
        catch (Exception ex)
        {
            return new ActionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTime = DateTime.UtcNow - startTime,
                Exception = ex
            };
        }
    }

    public ValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Url))
            errors.Add("URL cannot be empty");
        if (!Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            errors.Add("URL must start with http:// or https://");

        return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }

    public void AddCondition(IActionCondition condition) => _conditions.Add(condition);
    public void AddParallelAction(Guid actionId) => _parallelActionIds.Add(actionId);
}

/// <summary>
/// Delay/Wait action - pauses workflow execution for a specified duration.
/// </summary>
public class DelayAction : IWorkflowAction
{
    private readonly List<IActionCondition> _conditions = new();
    private readonly List<Guid> _parallelActionIds = new();

    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string ActionType => "Delay";
    public IReadOnlyDictionary<string, ActionParameter> Parameters { get; }
    public IReadOnlyList<IActionCondition> Conditions => _conditions.AsReadOnly();
    public Guid? NextActionId { get; set; }
    public IReadOnlyList<Guid> ParallelActionIds => _parallelActionIds.AsReadOnly();
    public RetryPolicy? RetryPolicy { get; set; }

    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(1);

    public DelayAction(string name, string description, TimeSpan duration)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Duration = duration;

        Parameters = new Dictionary<string, ActionParameter>
        {
            { "seconds", new ActionParameter { Name = "seconds", Type = "int", Required = true, Description = "Number of seconds to delay" } }
        };
    }

    public async Task<ActionResult> ExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            await Task.Delay(Duration, cancellationToken);
            return new ActionResult
            {
                Success = true,
                Message = $"Delayed for {Duration.TotalSeconds} seconds",
                ExecutionTime = DateTime.UtcNow - startTime
            };
        }
        catch (OperationCanceledException)
        {
            return new ActionResult
            {
                Success = false,
                ErrorMessage = "Delay was cancelled",
                ExecutionTime = DateTime.UtcNow - startTime
            };
        }
    }

    public ValidationResult Validate()
    {
        var errors = new List<string>();
        if (Duration <= TimeSpan.Zero)
            errors.Add("Duration must be greater than zero");

        return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }

    public void AddCondition(IActionCondition condition) => _conditions.Add(condition);
    public void AddParallelAction(Guid actionId) => _parallelActionIds.Add(actionId);
}

/// <summary>
/// Set Variable action - sets or updates a workflow variable.
/// </summary>
public class SetVariableAction : IWorkflowAction
{
    private readonly List<IActionCondition> _conditions = new();
    private readonly List<Guid> _parallelActionIds = new();

    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string ActionType => "SetVariable";
    public IReadOnlyDictionary<string, ActionParameter> Parameters { get; }
    public IReadOnlyList<IActionCondition> Conditions => _conditions.AsReadOnly();
    public Guid? NextActionId { get; set; }
    public IReadOnlyList<Guid> ParallelActionIds => _parallelActionIds.AsReadOnly();
    public RetryPolicy? RetryPolicy { get; set; }

    public string VariableName { get; set; } = string.Empty;
    public object? VariableValue { get; set; }

    public SetVariableAction(string name, string description, string variableName, object? variableValue)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        VariableName = variableName;
        VariableValue = variableValue;

        Parameters = new Dictionary<string, ActionParameter>
        {
            { "variableName", new ActionParameter { Name = "variableName", Type = "string", Required = true } },
            { "variableValue", new ActionParameter { Name = "variableValue", Type = "string", Required = true } }
        };
    }

    public async Task<ActionResult> ExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            context.SetVariable(VariableName, VariableValue);
            return await Task.FromResult(new ActionResult
            {
                Success = true,
                Message = $"Variable '{VariableName}' set to '{VariableValue}'",
                ExecutionTime = DateTime.UtcNow - startTime
            });
        }
        catch (Exception ex)
        {
            return new ActionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTime = DateTime.UtcNow - startTime,
                Exception = ex
            };
        }
    }

    public ValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(VariableName))
            errors.Add("Variable name cannot be empty");

        return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }

    public void AddCondition(IActionCondition condition) => _conditions.Add(condition);
    public void AddParallelAction(Guid actionId) => _parallelActionIds.Add(actionId);
}
