namespace ObsidianAI.Domain.Exceptions;

/// <summary>
/// Exception thrown when an external service (LLM, MCP, etc.) fails.
/// </summary>
public sealed class ExternalServiceException : DomainException
{
    /// <summary>
    /// Gets the name of the external service.
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalServiceException"/> class.
    /// </summary>
    /// <param name="serviceName">The name of the external service.</param>
    /// <param name="message">The error message.</param>
    public ExternalServiceException(string serviceName, string message)
        : base($"{serviceName}: {message}", "EXTERNAL_SERVICE_ERROR")
    {
        ServiceName = serviceName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalServiceException"/> class.
    /// </summary>
    /// <param name="serviceName">The name of the external service.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ExternalServiceException(string serviceName, string message, Exception innerException)
        : base($"{serviceName}: {message}", "EXTERNAL_SERVICE_ERROR", innerException)
    {
        ServiceName = serviceName;
    }
}
