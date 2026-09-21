using System.Net;

namespace SystemOneDotNet.Exceptions;

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
