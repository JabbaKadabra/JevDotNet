namespace SystemOneDotNet.Internal;

/// <summary>
/// An immutable, validated snapshot of <see cref="SystemOneOptions"/>. Taken once when a client is
/// constructed so later requests cannot observe a different configuration.
/// </summary>
/// <param name="Endpoint">The absolute URL of the System One endpoint.</param>
/// <param name="Model">The model that handles requests.</param>
/// <param name="MaxChoiceProperties">The maximum number of serialized properties allowed per choice option.</param>
internal sealed record SystemOneSettings(string Endpoint, string Model, int MaxChoiceProperties)
{
    /// <summary>Validates the supplied options and captures them in a snapshot.</summary>
    /// <param name="options">The options to validate.</param>
    /// <returns>The validated snapshot.</returns>
    /// <exception cref="ArgumentException">An option value is missing or malformed.</exception>
    public static SystemOneSettings From(SystemOneOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var endpoint = options.Endpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("SystemOneOptions.Endpoint must not be empty.", nameof(options));
        }

        if (!IsAbsoluteHttpEndpoint(endpoint))
        {
            throw new ArgumentException(
                $"SystemOneOptions.Endpoint must be an absolute HTTP or HTTPS URL but was '{endpoint}'.",
                nameof(options));
        }

        var model = options.Model;
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("SystemOneOptions.Model must not be empty.", nameof(options));
        }

        var maxChoiceProperties = options.MaxChoiceProperties;
        if (maxChoiceProperties <= 0)
        {
            throw new ArgumentException(
                $"SystemOneOptions.MaxChoiceProperties must be positive but was {maxChoiceProperties}.",
                nameof(options));
        }

        return new SystemOneSettings(endpoint, model, maxChoiceProperties);
    }

    private static bool IsAbsoluteHttpEndpoint(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
