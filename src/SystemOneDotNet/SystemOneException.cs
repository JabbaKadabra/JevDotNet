using System.Net;

namespace SystemOneDotNet;

/// <summary>
/// Serves as the base class for exceptions thrown by <see cref="SystemOne"/>.
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

/// <summary>
/// Thrown when the System One API returns an unsuccessful HTTP status code.
/// </summary>
public sealed class SystemOneApiException : SystemOneException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemOneApiException"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned by the API.</param>
    /// <param name="responseBody">The raw response body returned by the API.</param>
    public SystemOneApiException(HttpStatusCode statusCode, string responseBody)
        : base($"The SystemOne API returned HTTP {(int)statusCode} ({statusCode}). Response body: {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// Gets the HTTP status code returned by the API.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the raw response body returned by the API.
    /// </summary>
    public string ResponseBody { get; }
}
