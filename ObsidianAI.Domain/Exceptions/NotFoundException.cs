namespace ObsidianAI.Domain.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public sealed class NotFoundException : DomainException
{
    /// <summary>
    /// Gets the name of the resource that was not found.
    /// </summary>
    public string ResourceName { get; }

    /// <summary>
    /// Gets the identifier of the resource that was not found.
    /// </summary>
    public object ResourceId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="resourceId">The identifier of the resource.</param>
    public NotFoundException(string resourceName, object resourceId)
        : base($"{resourceName} with identifier '{resourceId}' was not found.", "RESOURCE_NOT_FOUND")
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="message">Custom error message.</param>
    public NotFoundException(string message)
        : base(message, "RESOURCE_NOT_FOUND")
    {
        ResourceName = string.Empty;
        ResourceId = string.Empty;
    }
}
