using SystemOneDotNet.Exceptions;
using SystemOneDotNet.Questions;

namespace SystemOneDotNet.Internal;

/// <summary>
/// The default <see cref="ISystemOneResult"/>: typed answers keyed by question id.
/// </summary>
internal sealed class SystemOneResult : ISystemOneResult
{
    private readonly Dictionary<string, object> answers;

    public SystemOneResult(string model, SystemOneUsage usage, Dictionary<string, object> answers)
    {
        Model = model;
        Usage = usage;
        this.answers = answers;
    }

    public string Model { get; }

    public SystemOneUsage Usage { get; }

    public TAnswer Get<TAnswer>(IQuestion<TAnswer> question)
    {
        Guard.NotNull(question, nameof(question), "Questions must not be null.");

        if (!answers.TryGetValue(question.Id, out var answer))
        {
            throw new SystemOneValidationException(
                $"The result does not contain an answer for question '{question.Id}'. Only questions added to the batch can be read.");
        }

        if (answer is TAnswer typed)
        {
            return typed;
        }

        throw new SystemOneProtocolException(
            $"The answer for question '{question.Id}' is a {answer.GetType().Name} and cannot be read as {typeof(TAnswer).Name}.");
    }
}
