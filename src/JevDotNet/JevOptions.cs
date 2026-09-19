namespace JevDotNet
{
    /// <summary>Configuration for a <see cref="Jev"/> client.</summary>
    public sealed class JevOptions
    {
        /// <summary>The default TypeSafe System One endpoint.</summary>
        public const string DefaultEndpoint = "https://api.typesafe.ai/v1/systemone";

        /// <summary>The default model alias that handles requests.</summary>
        public const string DefaultModel = "jev-latest";

        /// <summary>The default maximum number of serialized properties allowed in a single choice option.</summary>
        public const int DefaultMaxChoiceProperties = 20;

        /// <summary>
        /// Gets or sets the absolute URL of the System One endpoint.
        /// Defaults to <see cref="DefaultEndpoint"/>.
        /// </summary>
        public string Endpoint { get; set; } = DefaultEndpoint;

        /// <summary>
        /// Gets or sets the model that handles requests.
        /// Defaults to <see cref="DefaultModel"/>.
        /// </summary>
        public string Model { get; set; } = DefaultModel;

        /// <summary>
        /// Gets or sets the maximum number of serialized properties allowed in a single choice option's
        /// serialized object tree. The value must be positive. Defaults to <see cref="DefaultMaxChoiceProperties"/>.
        /// </summary>
        public int MaxChoiceProperties { get; set; } = DefaultMaxChoiceProperties;
    }
}
