using System;

namespace JevDotNet.Internal
{
    /// <summary>An immutable snapshot of <see cref="JevOptions"/> taken when a request is sent.</summary>
    internal sealed class JevSettings
    {
        private JevSettings(string endpoint, string model, int maxChoiceProperties)
        {
            Endpoint = endpoint;
            Model = model;
            MaxChoiceProperties = maxChoiceProperties;
        }

        public string Endpoint { get; }

        public string Model { get; }

        public int MaxChoiceProperties { get; }

        public static JevSettings From(JevOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var endpoint = options.Endpoint;
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("JevOptions.Endpoint must not be empty.", nameof(options));
            }

            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException(
                    $"JevOptions.Endpoint must be an absolute HTTP or HTTPS URL but was '{endpoint}'.",
                    nameof(options));
            }

            var model = options.Model;
            if (string.IsNullOrWhiteSpace(model))
            {
                throw new ArgumentException("JevOptions.Model must not be empty.", nameof(options));
            }

            var maxChoiceProperties = options.MaxChoiceProperties;
            if (maxChoiceProperties <= 0)
            {
                throw new ArgumentException(
                    $"JevOptions.MaxChoiceProperties must be positive but was {maxChoiceProperties}.",
                    nameof(options));
            }

            return new JevSettings(endpoint, model, maxChoiceProperties);
        }
    }
}
