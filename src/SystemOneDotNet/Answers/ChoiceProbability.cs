namespace SystemOneDotNet.Answers;

/// <summary>
/// A single option and its probability inside a <see cref="ChoiceAnswer{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the original option value.</typeparam>
/// <param name="Option">The original option value that was supplied to the question.</param>
/// <param name="Probability">The probability assigned to this option, between 0 and 1.</param>
public sealed record ChoiceProbability<T>(
    T Option, 
    double Probability);
