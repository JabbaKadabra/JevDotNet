using JevDotNet.Internal;

namespace JevDotNet;

/// <summary>
/// A mutable builder that collects the questions of one batch. Builders are not thread-safe; use one
/// builder per batch. Inputs are snapshotted when the batch is sent.
/// </summary>
public sealed class JevQuery
{
    private readonly Jev jev;
    private readonly object state;
    private readonly List<IQuestion> questions = [];
    private readonly HashSet<string> questionIds = new(StringComparer.Ordinal);
    private readonly HashSet<object> questionHandles = new(ReferenceComparer.Instance);

    internal JevQuery(Jev jev, object state)
    {
        this.jev = jev;
        this.state = state;
    }

    /// <summary>
    /// Adds any question to the batch and returns this builder.
    /// </summary>
    /// <typeparam name="TAnswer">The answer type of the question.</typeparam>
    /// <param name="question">The question to add.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public JevQuery Question<TAnswer>(IQuestion<TAnswer> question) =>
        Add(Guard.NotNull(question, nameof(question), "Questions must not be null."));

    /// <summary>
    /// Adds a choice question to the batch and returns this builder.
    /// </summary>
    /// <typeparam name="T">The type of the choice option values.</typeparam>
    /// <param name="question">The choice question to add.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public JevQuery Choice<T>(IQuestion<ChoiceAnswer<T>> question) =>
        Add(Guard.NotNull(question, nameof(question), "Questions must not be null."));

    /// <summary>
    /// Adds a score question to the batch and returns this builder.
    /// </summary>
    /// <param name="question">The score question to add.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public JevQuery Score(IQuestion<ScoreAnswer> question) =>
        Add(Guard.NotNull(question, nameof(question), "Questions must not be null."));

    /// <summary>
    /// Adds a noul question to the batch and returns this builder.
    /// </summary>
    /// <param name="question">The noul question to add.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public JevQuery Noul(IQuestion<NoulAnswer> question) =>
        Add(Guard.NotNull(question, nameof(question), "Questions must not be null."));

    /// <summary>
    /// Sends the batch and returns the typed answers.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the HTTP request and response buffering.</param>
    /// <returns>The result containing one typed answer per question.</returns>
    public Task<JevResult> SendAsync(CancellationToken cancellationToken = default)
    {
        var batch = questions.ToArray();
        if (batch.Length == 0)
        {
            throw new JevValidationException("A Jev batch must contain at least one question.");
        }

        return jev.SendAsync(state, batch, cancellationToken);
    }

    private JevQuery Add(IQuestion question)
    {
        if (!questionHandles.Add(question))
        {
            throw new JevValidationException(
                $"The question with id '{question.Id}' was already added to this batch.");
        }

        if (!questionIds.Add(question.Id))
        {
            questionHandles.Remove(question);
            throw new JevValidationException(
                $"The question id '{question.Id}' is already used in this batch. Question ids must be unique.");
        }

        questions.Add(question);
        return this;
    }
}
