namespace JevDotNet;

/// <summary>
/// A question that can be evaluated by Jev.
/// </summary>
/// <remarks>
/// This interface is implemented by the question types shipped with this library
/// (<see cref="ChoiceQuestion{T}"/>, <see cref="ChoiceQuestion"/>, <see cref="ScoreQuestion"/>,
/// and <see cref="NoulQuestion"/>). It is not intended to be implemented by user code.
/// </remarks>
public interface IQuestion
{
    /// <summary>
    /// Gets the identifier that matches the question with its answer inside a batch.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the instructions sent to the model.
    /// </summary>
    string Instructions { get; }
}
