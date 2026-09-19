using System;
using System.Net;

namespace JevDotNet
{
    /// <summary>Serves as the base class for exceptions thrown by <see cref="Jev"/>.</summary>
    public class JevException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="JevException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        public JevException(string message)
            : base(message)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="JevException"/> class with an inner exception.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        public JevException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Thrown when a question, batch, or option fails local validation and no request is sent to the Jev API.
    /// </summary>
    public sealed class JevValidationException : JevException
    {
        /// <summary>Initializes a new instance of the <see cref="JevValidationException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        public JevValidationException(string message)
            : base(message)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="JevValidationException"/> class with an inner exception.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        public JevValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Thrown when the Jev API returns a successful response whose body is malformed or does not match the requested questions.
    /// </summary>
    public sealed class JevProtocolException : JevException
    {
        /// <summary>Initializes a new instance of the <see cref="JevProtocolException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        public JevProtocolException(string message)
            : base(message)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="JevProtocolException"/> class with an inner exception.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        public JevProtocolException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>Thrown when the Jev API returns an unsuccessful HTTP status code.</summary>
    public sealed class JevApiException : JevException
    {
        /// <summary>Initializes a new instance of the <see cref="JevApiException"/> class.</summary>
        /// <param name="statusCode">The HTTP status code returned by the API.</param>
        /// <param name="responseBody">The raw response body returned by the API.</param>
        public JevApiException(HttpStatusCode statusCode, string responseBody)
            : base($"The Jev API returned HTTP {(int)statusCode} ({statusCode}). Response body: {responseBody}")
        {
            StatusCode = statusCode;
            ResponseBody = responseBody ?? string.Empty;
        }

        /// <summary>Gets the HTTP status code returned by the API.</summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>Gets the raw response body returned by the API.</summary>
        public string ResponseBody { get; }
    }
}
