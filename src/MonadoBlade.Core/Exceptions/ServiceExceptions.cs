namespace MonadoBlade.Core.Exceptions;

/// <summary>
/// Marker interface for business exceptions in the system.
/// </summary>
public interface IBusinessException
{
    /// <summary>Gets the error code.</summary>
    string ErrorCode { get; }

    /// <summary>Gets the user-friendly error message.</summary>
    string UserMessage { get; }
}

/// <summary>
/// Base class for all business exceptions.
/// </summary>
public abstract class BusinessException : Exception, IBusinessException
{
    /// <summary>Initializes a new instance of BusinessException.</summary>
    protected BusinessException(string errorCode, string message, string? userMessage = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        UserMessage = userMessage ?? message;
    }

    /// <summary>Gets the error code.</summary>
    public string ErrorCode { get; }

    /// <summary>Gets the user-friendly error message.</summary>
    public string UserMessage { get; }
}

/// <summary>
/// Thrown when a service is temporarily unavailable or unreachable.
/// </summary>
public class ServiceUnavailableException : BusinessException
{
    /// <summary>Initializes a new instance of ServiceUnavailableException.</summary>
    public ServiceUnavailableException(string serviceName, string? message = null, Exception? innerException = null)
        : base("SERVICE_UNAVAILABLE", 
            message ?? $"Service '{serviceName}' is currently unavailable.",
            userMessage: $"The {serviceName} service is temporarily unavailable. Please try again later.",
            innerException)
    {
        ServiceName = serviceName;
    }

    /// <summary>Gets the name of the unavailable service.</summary>
    public string ServiceName { get; }
}

/// <summary>
/// Thrown when an operation fails for non-transient reasons.
/// </summary>
public class OperationFailedException : BusinessException
{
    /// <summary>Initializes a new instance of OperationFailedException.</summary>
    public OperationFailedException(string operationName, string? message = null, string? userMessage = null, Exception? innerException = null)
        : base("OPERATION_FAILED",
            message ?? $"Operation '{operationName}' failed.",
            userMessage: userMessage ?? $"The operation could not be completed. Please try again or contact support.",
            innerException)
    {
        OperationName = operationName;
    }

    /// <summary>Gets the name of the failed operation.</summary>
    public string OperationName { get; }
}

/// <summary>
/// Thrown when an optimistic concurrency check fails (another user modified the data).
/// </summary>
public class ConcurrencyConflictException : BusinessException
{
    /// <summary>Initializes a new instance of ConcurrencyConflictException.</summary>
    public ConcurrencyConflictException(string entityType, string entityId, Exception? innerException = null)
        : base("CONCURRENCY_CONFLICT",
            $"Concurrency conflict detected for {entityType} with ID '{entityId}'. The entity was modified by another user.",
            userMessage: "The data was modified by another user. Please refresh and try again.",
            innerException)
    {
        EntityType = entityType;
        EntityId = entityId;
    }

    /// <summary>Gets the type of entity involved in the conflict.</summary>
    public string EntityType { get; }

    /// <summary>Gets the ID of the entity involved in the conflict.</summary>
    public string EntityId { get; }
}

/// <summary>
/// Thrown when entity or data validation fails.
/// </summary>
public class ValidationFailedException : BusinessException
{
    /// <summary>Initializes a new instance of ValidationFailedException.</summary>
    public ValidationFailedException(string entityType, IEnumerable<string> errors, Exception? innerException = null)
        : base("VALIDATION_FAILED",
            $"Validation failed for {entityType}: {string.Join("; ", errors)}",
            userMessage: $"Please correct the following errors: {string.Join(", ", errors)}",
            innerException)
    {
        EntityType = entityType;
        Errors = errors.ToList();
    }

    /// <summary>Gets the type of entity that failed validation.</summary>
    public string EntityType { get; }

    /// <summary>Gets the validation errors.</summary>
    public IReadOnlyList<string> Errors { get; }
}

/// <summary>
/// Thrown when an entity or resource is not found.
/// </summary>
public class EntityNotFoundException : BusinessException
{
    /// <summary>Initializes a new instance of EntityNotFoundException.</summary>
    public EntityNotFoundException(string entityType, string entityId, Exception? innerException = null)
        : base("ENTITY_NOT_FOUND",
            $"{entityType} with ID '{entityId}' was not found.",
            userMessage: $"The requested {entityType} could not be found.",
            innerException)
    {
        EntityType = entityType;
        EntityId = entityId;
    }

    /// <summary>Gets the type of entity that was not found.</summary>
    public string EntityType { get; }

    /// <summary>Gets the ID of the entity that was not found.</summary>
    public string EntityId { get; }
}

/// <summary>
/// Thrown when a resource conflict occurs (e.g., duplicate key).
/// </summary>
public class ResourceConflictException : BusinessException
{
    /// <summary>Initializes a new instance of ResourceConflictException.</summary>
    public ResourceConflictException(string resourceType, string? conflictingField = null, Exception? innerException = null)
        : base("RESOURCE_CONFLICT",
            $"A {resourceType} with this {conflictingField ?? "value"} already exists.",
            userMessage: $"A {resourceType} with this value already exists. Please use a different value.",
            innerException)
    {
        ResourceType = resourceType;
        ConflictingField = conflictingField;
    }

    /// <summary>Gets the type of resource in conflict.</summary>
    public string ResourceType { get; }

    /// <summary>Gets the field that caused the conflict.</summary>
    public string? ConflictingField { get; }
}

/// <summary>
/// Thrown when the current user lacks required permissions.
/// </summary>
public class UnauthorizedException : BusinessException
{
    /// <summary>Initializes a new instance of UnauthorizedException.</summary>
    public UnauthorizedException(string requiredPermission, Exception? innerException = null)
        : base("UNAUTHORIZED",
            $"User does not have required permission: '{requiredPermission}'",
            userMessage: "You do not have permission to perform this action.",
            innerException)
    {
        RequiredPermission = requiredPermission;
    }

    /// <summary>Gets the required permission that the user lacks.</summary>
    public string RequiredPermission { get; }
}

/// <summary>
/// Thrown when an integration with an external service fails.
/// </summary>
public class IntegrationException : BusinessException
{
    /// <summary>Initializes a new instance of IntegrationException.</summary>
    public IntegrationException(string externalService, string? message = null, int? httpStatusCode = null, Exception? innerException = null)
        : base("INTEGRATION_FAILED",
            message ?? $"Integration with '{externalService}' failed.",
            userMessage: $"Unable to communicate with {externalService}. Please try again later.",
            innerException)
    {
        ExternalService = externalService;
        HttpStatusCode = httpStatusCode;
    }

    /// <summary>Gets the name of the external service.</summary>
    public string ExternalService { get; }

    /// <summary>Gets the HTTP status code if applicable.</summary>
    public int? HttpStatusCode { get; }
}

/// <summary>
/// Thrown when a plugin fails to execute.
/// </summary>
public class PluginExecutionException : BusinessException
{
    /// <summary>Initializes a new instance of PluginExecutionException.</summary>
    public PluginExecutionException(string pluginId, string methodName, string? message = null, Exception? innerException = null)
        : base("PLUGIN_EXECUTION_FAILED",
            message ?? $"Plugin '{pluginId}' failed to execute method '{methodName}'",
            userMessage: $"The plugin encountered an error. Please check the logs for details.",
            innerException)
    {
        PluginId = pluginId;
        MethodName = methodName;
    }

    /// <summary>Gets the ID of the plugin.</summary>
    public string PluginId { get; }

    /// <summary>Gets the method that failed.</summary>
    public string MethodName { get; }
}
