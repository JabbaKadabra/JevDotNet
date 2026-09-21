using System.Globalization;
using System.Text.Json;
using SystemOneDotNet.Answers;
using SystemOneDotNet.Exceptions;
using SystemOneDotNet.Internal.Json;
using SystemOneDotNet.Questions;

namespace SystemOneDotNet.Internal.Questions;

/// <summary>
/// A choice question whose options are arbitrary values. Generic options are materialized once and each
/// option is sent to the model as its JSON description under a deterministic index id.
/// </summary>
/// <typeparam name="T">The type of the option values.</typeparam>
internal sealed class ChoiceQuestion<T> : IChoiceQuestion<T>, ISystemOneQuestion, IChoiceQuestionLimits
{
    private readonly T[] options;

    public ChoiceQuestion(string id, string instructions, IEnumerable<T> options)
    {
        Id = Guard.Id(id);
        Instructions = Guard.Instructions(instructions);
        Guard.NotNull(options, nameof(options), "Choice options must not be null.");

        this.options = options.ToArray();
        if (this.options.Length == 0)
        {
            throw new SystemOneValidationException(
                $"Choice question '{Id}' must define at least one option.");
        }

        if (this.options.Length > QuestionLimits.MaxOptionCount)
        {
            throw new SystemOneValidationException(
                $"Choice question '{Id}' defines {this.options.Length} options; the limit is {QuestionLimits.MaxOptionCount}.");
        }

        for (var index = 0; index < this.options.Length; index++)
        {
            if (this.options[index] is null)
            {
                throw new SystemOneValidationException(
                    $"Choice question '{Id}' contains a null option at index {index}.");
            }
        }
    }

    public string Id { get; }

    public string Instructions { get; }

    public IReadOnlyList<T> Options => options;

    private static string OptionId(int index) => index.ToString(CultureInfo.InvariantCulture);

    void ISystemOneQuestion.WriteQuestion(Utf8JsonWriter writer)
    {
        writer.WriteString("type", "choice");
        writer.WriteString("instructions", Instructions);
        writer.WriteStartObject("criteria");
        for (var index = 0; index < options.Length; index++)
        {
            writer.WritePropertyName(OptionId(index));
            JsonFormat.WriteDescription(writer, options[index]);
        }

        writer.WriteEndObject();
    }

    void IChoiceQuestionLimits.ValidatePropertyLimits(int maxProperties)
    {
        for (var index = 0; index < options.Length; index++)
        {
            var count = PropertyCounter.Count(options[index], $"Choice option {index}");
            if (count > maxProperties)
            {
                throw new SystemOneValidationException(
                    $"Choice option {index} contains {count} serialized properties; the limit is {maxProperties}.\n" +
                    "Use a dedicated smaller POCO or increase SystemOneOptions.MaxChoiceProperties\n" +
                    "when creating the client.");
            }
        }
    }

    object ISystemOneQuestion.ReadAnswer(JsonElement answer)
    {
        var answerObject = AnswerReader.RequireObject(answer, Id);
        AnswerReader.RequireType(answerObject, "choice", Id);

        var choiceId = AnswerReader.RequireString(answerObject, "choice", Id);
        var confidence = AnswerReader.RequireUnitInterval(
            AnswerReader.RequireNumber(answerObject, "confidence", Id),
            "The 'confidence' value",
            Id);

        var probabilities = AnswerReader.RequireObject(
            AnswerReader.RequireProperty(answerObject, "probabilities", Id),
            Id);

        var probabilitiesByIndex = new double[options.Length];
        var found = new bool[options.Length];
        foreach (var property in probabilities.EnumerateObject())
        {
            var index = AnswerReader.ParseOptionIndex(property.Name, options.Length, Id);
            if (found[index])
            {
                throw new SystemOneProtocolException(
                    $"The answer for question '{Id}' contains duplicate probabilities for option id '{property.Name}'.");
            }

            if (property.Value.ValueKind != JsonValueKind.Number)
            {
                throw new SystemOneProtocolException(
                    $"The probability for option id '{property.Name}' of question '{Id}' must be a number.");
            }

            probabilitiesByIndex[index] = AnswerReader.RequireUnitInterval(
                property.Value.GetDouble(),
                $"The probability for option id '{property.Name}'",
                Id);
            found[index] = true;
        }

        var selectedIndex = -1;
        var ordered = new List<ChoiceProbability<T>>(options.Length);
        for (var index = 0; index < options.Length; index++)
        {
            if (!found[index])
            {
                throw new SystemOneProtocolException(
                    $"The answer for question '{Id}' is missing the probability for option id '{OptionId(index)}'.");
            }

            ordered.Add(new ChoiceProbability<T>(options[index], probabilitiesByIndex[index]));
            if (string.Equals(choiceId, OptionId(index), StringComparison.Ordinal))
            {
                selectedIndex = index;
            }
        }

        if (selectedIndex < 0)
        {
            throw new SystemOneProtocolException(
                $"The answer for question '{Id}' selected the unknown option id '{choiceId}'.");
        }

        return new ChoiceAnswer<T>(options[selectedIndex], confidence, ordered);
    }
}
