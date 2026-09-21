using SystemOneDotNet.Exceptions;
using SystemOneDotNet.Questions;

namespace SystemOneDotNet;

/// <summary>
/// The answers, model, and token usage returned for a batch.
/// </summary>
public interface ISystemOneResult
{
    /// <summary>
    /// Gets the model that performed the evaluation.
    /// </summary>
    string Model { get; }

    /// <summary>
    /// Gets the token usage for the request.
    /// </summary>
    SystemOneUsage Usage { get; }

    /// <summary>
    /// Gets the typed answer for a question that was part of the batch.
    /// </summary>
    /// <typeparam name="TAnswer">The answer type of the question.</typeparam>
    /// <param name="question">A question that was added to the batch.</param>
    /// <returns>The typed answer.</returns>
    /// <exception cref="SystemOneValidationException">The question was not part of the batch that produced this result.</exception>
    /// <exception cref="SystemOneProtocolException">The stored answer does not match the question's answer type.</exception>
    TAnswer Get<TAnswer>(IQuestion<TAnswer> question);
}
