namespace SystemOneDotNet.Exceptions;

/// <summary>
/// Serves as the base class for exceptions thrown by an <see cref="ISystemOneClient"/>.
/// </summary>
public class SystemOneException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemOneException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SystemOneException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemOneException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public SystemOneException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
