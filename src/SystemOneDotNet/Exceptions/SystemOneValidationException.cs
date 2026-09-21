namespace SystemOneDotNet.Exceptions;

/// <summary>
/// Thrown when a question, batch, or option fails local validation and no request is sent to the System One API.
/// </summary>
public sealed class SystemOneValidationException : SystemOneException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemOneValidationException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SystemOneValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemOneValidationException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public SystemOneValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
