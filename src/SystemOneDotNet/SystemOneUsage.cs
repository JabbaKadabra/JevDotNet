namespace SystemOneDotNet;

/// <summary>
/// Token usage reported for a completed request.
/// </summary>
/// <param name="InputTokens">The number of input tokens consumed by the request.</param>
/// <param name="OutputTokens">The number of output tokens produced by the request.</param>
public sealed record SystemOneUsage(int InputTokens, int OutputTokens);
