namespace SystemOneDotNet.Questions;

/// <summary>
/// A question that can be evaluated by System One.
/// </summary>
/// <remarks>
/// Questions are created with the <see cref="Question"/> factory. The interface is not intended to be
/// implemented by user code; a batch rejects questions that were not created by this library.
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
