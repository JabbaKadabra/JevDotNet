using SystemOneDotNet.Answers;
using SystemOneDotNet.Questions;

namespace SystemOneDotNet;

/// <summary>
/// A mutable builder that collects the questions of one batch. Builders are not thread-safe; use one
/// builder per batch. Inputs are snapshotted when the batch is sent.
/// </summary>
public interface ISystemOneQuery
{
    /// <summary>
    /// Adds any question to the batch and returns this builder.
    /// </summary>
    /// <typeparam name="TAnswer">The answer type of the question.</typeparam>
    /// <param name="question">The question to add.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    ISystemOneQuery Question<TAnswer>(IQuestion<TAnswer> question);

    /// <summary>
    /// Adds a choice question to the batch and returns this builder.
    /// </summary>
    /// <typeparam name="T">The type of the choice option values.</typeparam>
    /// <param name="question">The choice question to add.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    ISystemOneQuery Choice<T>(IQuestion<ChoiceAnswer<T>> question);

    /// <summary>
    /// Adds a score question to the batch and returns this builder.
    /// </summary>
    /// <param name="question">The score question to add.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    ISystemOneQuery Score(IQuestion<ScoreAnswer> question);

    /// <summary>
    /// Adds a noul question to the batch and returns this builder.
    /// </summary>
    /// <param name="question">The noul question to add.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    ISystemOneQuery Noul(IQuestion<NoulAnswer> question);

    /// <summary>
    /// Sends the batch and returns the typed answers.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the HTTP request and response buffering.</param>
    /// <returns>The result containing one typed answer per question.</returns>
    Task<ISystemOneResult> SendAsync(CancellationToken cancellationToken = default);
}
