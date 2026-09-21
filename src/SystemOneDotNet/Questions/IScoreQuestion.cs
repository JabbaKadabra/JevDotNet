using SystemOneDotNet.Answers;

namespace SystemOneDotNet.Questions;

/// <summary>
/// A score question that rates the state along an ordered list of level descriptions.
/// </summary>
public interface IScoreQuestion : IQuestion<ScoreAnswer>
{
    /// <summary>
    /// Gets the level descriptions, in the order they were supplied.
    /// </summary>
    IReadOnlyList<string> Levels { get; }
}
