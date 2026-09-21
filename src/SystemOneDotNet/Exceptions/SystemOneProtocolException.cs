namespace SystemOneDotNet.Exceptions;

/// <summary>
/// Thrown when the System One API returns a successful response whose body is malformed or does not match the requested questions.
/// </summary>
public sealed class SystemOneProtocolException : SystemOneException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemOneProtocolException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SystemOneProtocolException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemOneProtocolException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public SystemOneProtocolException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
