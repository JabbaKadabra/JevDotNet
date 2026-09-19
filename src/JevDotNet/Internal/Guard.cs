using System;

namespace JevDotNet.Internal
{
    /// <summary>Argument validation helpers for the public API surface.</summary>
    internal static class Guard
    {
        public static string ApiKey(string? apiKey)
        {
            if (apiKey == null)
            {
                throw new ArgumentNullException(nameof(apiKey), "The Jev API key must not be null.");
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("The Jev API key must not be empty.", nameof(apiKey));
            }

            return apiKey;
        }

        public static string Id(string? id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id), "Question ids must not be null.");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Question ids must not be empty.", nameof(id));
            }

            return id;
        }

        public static string Instructions(string? instructions)
        {
            if (instructions == null)
            {
                throw new ArgumentNullException(nameof(instructions), "Question instructions must not be null.");
            }

            if (string.IsNullOrWhiteSpace(instructions))
            {
                throw new ArgumentException("Question instructions must not be empty.", nameof(instructions));
            }

            return instructions;
        }

        public static T NotNull<T>(T? value, string parameterName, string message)
            where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName, message);
            }

            return value;
        }
    }
}
