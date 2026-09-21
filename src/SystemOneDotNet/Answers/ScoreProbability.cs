namespace SystemOneDotNet.Answers;

/// <summary>
/// A single score level and its probability inside a <see cref="ScoreAnswer"/>.
/// </summary>
/// <param name="LevelIndex">The zero-based index of the level.</param>
/// <param name="Level">The level description.</param>
/// <param name="Probability">The probability assigned to this level, between 0 and 1.</param>
public sealed record ScoreProbability(
    int LevelIndex, 
    string Level, 
    double Probability);
