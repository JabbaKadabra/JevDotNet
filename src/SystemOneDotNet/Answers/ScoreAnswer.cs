namespace SystemOneDotNet.Answers;

/// <summary>
/// The detailed answer to a score question.
/// </summary>
/// <param name="Score">The probability-weighted score across the levels. The value can land between levels.</param>
/// <param name="Legend">The level descriptions ordered by their level index.</param>
/// <param name="Confidence">The model's confidence in the answer, between 0 and 1.</param>
/// <param name="Probabilities">
/// Every level with its probability, ordered by level index, or <see langword="null"/> when the API omitted
/// the probability distribution.
/// </param>
public sealed record ScoreAnswer(
    double Score,
    IReadOnlyList<string> Legend,
    double Confidence,
    IReadOnlyList<ScoreProbability>? Probabilities);
