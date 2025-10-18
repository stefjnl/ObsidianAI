namespace ObsidianAI.Domain.Exceptions;

/// <summary>
/// Exception thrown when an operation conflicts with the current state.
/// </summary>
public sealed class ConflictException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConflictException(string message)
        : base(message, "CONFLICT")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConflictException(string message, Exception innerException)
        : base(message, "CONFLICT", innerException)
    {
    }
}
