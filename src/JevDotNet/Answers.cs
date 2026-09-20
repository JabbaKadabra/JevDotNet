namespace JevDotNet;

/// <summary>
/// A single option and its probability inside a <see cref="ChoiceAnswer{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the original option value.</typeparam>
public sealed record ChoiceProbability<T>
{
    internal ChoiceProbability(T option, double probability)
    {
        Option = option;
        Probability = probability;
    }

    /// <summary>
    /// Gets the original option value that was supplied to the question.
    /// </summary>
    public T Option { get; init; }

    /// <summary>
    /// Gets the probability assigned to this option.
    /// </summary>
    public double Probability { get; init; }
}

/// <summary>
/// The detailed answer to a choice question.
/// </summary>
/// <typeparam name="T">The type of the original option values.</typeparam>
public sealed record ChoiceAnswer<T>
{
    internal ChoiceAnswer(T choice, double confidence, IReadOnlyList<ChoiceProbability<T>> options)
    {
        Choice = choice;
        Confidence = confidence;
        Options = options;
    }

    /// <summary>
    /// Gets the selected option, as the original value that was supplied to the question.
    /// </summary>
    public T Choice { get; init; }

    /// <summary>
    /// Gets the model's confidence in the selected option, between 0 and 1.
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Gets every option with its probability, ordered as the options were supplied to the question.
    /// </summary>
    public IReadOnlyList<ChoiceProbability<T>> Options { get; init; }
}

/// <summary>
/// A single score level and its probability inside a <see cref="ScoreAnswer"/>.
/// </summary>
public sealed record ScoreProbability
{
    internal ScoreProbability(int levelIndex, string level, double probability)
    {
        LevelIndex = levelIndex;
        Level = level;
        Probability = probability;
    }

    /// <summary>
    /// Gets the zero-based index of the level.
    /// </summary>
    public int LevelIndex { get; init; }

    /// <summary>
    /// Gets the level description.
    /// </summary>
    public string Level { get; init; }

    /// <summary>
    /// Gets the probability assigned to this level.
    /// </summary>
    public double Probability { get; init; }
}

/// <summary>
/// The detailed answer to a score question.
/// </summary>
public sealed record ScoreAnswer
{
    internal ScoreAnswer(
        double score,
        IReadOnlyList<string> legend,
        double confidence,
        IReadOnlyList<ScoreProbability>? probabilities)
    {
        Score = score;
        Legend = legend;
        Confidence = confidence;
        Probabilities = probabilities;
    }

    /// <summary>
    /// Gets the probability-weighted score across the levels. The value can land between levels.
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// Gets the level descriptions ordered by their level index.
    /// </summary>
    public IReadOnlyList<string> Legend { get; init; }

    /// <summary>
    /// Gets the model's confidence in the answer, between 0 and 1.
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Gets every level with its probability, ordered by level index, or <see langword="null"/> when the
    /// API omitted the probability distribution.
    /// </summary>
    public IReadOnlyList<ScoreProbability>? Probabilities { get; init; }
}

/// <summary>
/// The detailed answer to a noul (yes/no) question.
/// </summary>
public sealed record NoulAnswer
{
    internal NoulAnswer(double noul)
    {
        Noul = noul;
    }

    /// <summary>
    /// Gets the probability that the answer is yes, from 0 (no) to 1 (yes).
    /// No automatic boolean conversion is performed.
    /// </summary>
    public double Noul { get; init; }
}
