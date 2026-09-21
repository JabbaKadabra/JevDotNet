using SystemOneDotNet.Answers;
using SystemOneDotNet.Exceptions;
using SystemOneDotNet.Internal.Questions;
using SystemOneDotNet.Questions;

namespace SystemOneDotNet.Internal;

/// <summary>
/// The default <see cref="ISystemOneQuery"/>. Collects questions, rejects duplicates and foreign question
/// implementations, and hands the snapshot to the owning client when sent.
/// </summary>
internal sealed class SystemOneQuery : ISystemOneQuery
{
    private readonly SystemOne systemOne;
    private readonly object state;
    private readonly List<ISystemOneQuestion> questions = [];
    private readonly HashSet<string> questionIds = new(StringComparer.Ordinal);
    private readonly HashSet<object> questionHandles = new(ReferenceComparer.Instance);

    public SystemOneQuery(SystemOne systemOne, object state)
    {
        this.systemOne = systemOne;
        this.state = state;
    }

    public ISystemOneQuery Question<TAnswer>(IQuestion<TAnswer> question) => Add(question);

    public ISystemOneQuery Choice<T>(IQuestion<ChoiceAnswer<T>> question) => Add(question);

    public ISystemOneQuery Score(IQuestion<ScoreAnswer> question) => Add(question);

    public ISystemOneQuery Noul(IQuestion<NoulAnswer> question) => Add(question);

    public Task<ISystemOneResult> SendAsync(CancellationToken cancellationToken = default)
    {
        var batch = questions.ToArray();
        if (batch.Length == 0)
        {
            throw new SystemOneValidationException("A System One batch must contain at least one question.");
        }

        return systemOne.SendAsync(state, batch, cancellationToken);
    }

    private SystemOneQuery Add(IQuestion? question)
    {
        question = Guard.NotNull(question, nameof(question), "Questions must not be null.");

        if (question is not ISystemOneQuestion systemOneQuestion)
        {
            throw new SystemOneValidationException(
                $"The question with id '{question.Id}' is a {question.GetType().Name}, which this library cannot send. " +
                "Create questions with the Question factory.");
        }

        if (!questionHandles.Add(question))
        {
            throw new SystemOneValidationException(
                $"The question with id '{question.Id}' was already added to this batch.");
        }

        if (!questionIds.Add(question.Id))
        {
            questionHandles.Remove(question);
            throw new SystemOneValidationException(
                $"The question id '{question.Id}' is already used in this batch. Question ids must be unique.");
        }

        questions.Add(systemOneQuestion);
        return this;
    }
}
