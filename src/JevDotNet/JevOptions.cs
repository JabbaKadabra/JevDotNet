namespace JevDotNet;

/// <summary>
/// Configuration for a <see cref="Jev"/> client. Instances are immutable; create one with an object
/// initializer and reuse it.
/// </summary>
public sealed record JevOptions
{
    /// <summary>
    /// The default TypeSafe System One endpoint.
    /// </summary>
    public const string DefaultEndpoint = "https://api.typesafe.ai/v1/systemone";

    /// <summary>
    /// The default model alias that handles requests.
    /// </summary>
    public const string DefaultModel = "jev-latest";

    /// <summary>
    /// The default maximum number of serialized properties allowed in a single choice option.
    /// </summary>
    public const int DefaultMaxChoiceProperties = 20;

    /// <summary>
    /// Gets the absolute URL of the System One endpoint. Defaults to <see cref="DefaultEndpoint"/>.
    /// </summary>
    public string Endpoint { get; init; } = DefaultEndpoint;

    /// <summary>
    /// Gets the model that handles requests. Defaults to <see cref="DefaultModel"/>.
    /// </summary>
    public string Model { get; init; } = DefaultModel;

    /// <summary>
    /// Gets the maximum number of serialized properties allowed in a single choice option's
    /// serialized object tree. The value must be positive. Defaults to
    /// <see cref="DefaultMaxChoiceProperties"/>.
    /// </summary>
    public int MaxChoiceProperties { get; init; } = DefaultMaxChoiceProperties;
}
