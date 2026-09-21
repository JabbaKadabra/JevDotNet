namespace SystemOneDotNet.Answers;

/// <summary>
/// The detailed answer to a choice question.
/// </summary>
/// <typeparam name="T">The type of the original option values.</typeparam>
/// <param name="Choice">The selected option, as the original value that was supplied to the question.</param>
/// <param name="Confidence">The model's confidence in the selected option, between 0 and 1.</param>
/// <param name="Options">Every option with its probability, ordered as the options were supplied to the question.</param>
public sealed record ChoiceAnswer<T>(
    T Choice,
    double Confidence,
    IReadOnlyList<ChoiceProbability<T>> Options);
