using System.Text.Json;
using SystemOneDotNet.Answers;
using SystemOneDotNet.Exceptions;
using SystemOneDotNet.Internal.Json;
using SystemOneDotNet.Questions;

namespace SystemOneDotNet.Internal.Questions;

/// <summary>
/// A choice question with named string options and optional descriptions, matching the criteria dictionary
/// of the TypeSafe quickstart.
/// </summary>
internal sealed class NamedChoiceQuestion : INamedChoiceQuestion, ISystemOneQuestion
{
    private readonly string[] names;
    private readonly string?[] descriptions;

    public NamedChoiceQuestion(string id, string instructions, IEnumerable<KeyValuePair<string, string?>> options)
    {
        Id = Guard.Id(id);
        Instructions = Guard.Instructions(instructions);
        Guard.NotNull(options, nameof(options), "Choice options must not be null.");

        var optionNames = new List<string>();
        var optionDescriptions = new List<string?>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in options)
        {
            if (option.Key is null)
            {
                throw new SystemOneValidationException(
                    $"Choice question '{Id}' contains a null option name at index {optionNames.Count}.");
            }

            if (string.IsNullOrWhiteSpace(option.Key))
            {
                throw new SystemOneValidationException(
                    $"Choice question '{Id}' contains an empty option name at index {optionNames.Count}.");
            }

            if (!seen.Add(option.Key))
            {
                throw new SystemOneValidationException(
                    $"Choice question '{Id}' contains the duplicate option name '{option.Key}'.");
            }

            optionNames.Add(option.Key);
            optionDescriptions.Add(option.Value);
        }

        if (optionNames.Count == 0)
        {
            throw new SystemOneValidationException(
                $"Choice question '{Id}' must define at least one option.");
        }

        if (optionNames.Count > QuestionLimits.MaxOptionCount)
        {
            throw new SystemOneValidationException(
                $"Choice question '{Id}' defines {optionNames.Count} options; the limit is {QuestionLimits.MaxOptionCount}.");
        }

        names = optionNames.ToArray();
        descriptions = optionDescriptions.ToArray();
    }

    public string Id { get; }

    public string Instructions { get; }

    public IReadOnlyList<string> Options => names;

    public IReadOnlyList<string?> Descriptions => descriptions;

    void ISystemOneQuestion.WriteQuestion(Utf8JsonWriter writer)
    {
        writer.WriteString("type", "choice");
        writer.WriteString("instructions", Instructions);
        writer.WriteStartObject("criteria");
        for (var index = 0; index < names.Length; index++)
        {
            writer.WritePropertyName(names[index]);
            if (descriptions[index] is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(descriptions[index]);
            }
        }

        writer.WriteEndObject();
    }

    object ISystemOneQuestion.ReadAnswer(JsonElement answer)
    {
        var answerObject = AnswerReader.RequireObject(answer, Id);
        AnswerReader.RequireType(answerObject, "choice", Id);

        var choice = AnswerReader.RequireString(answerObject, "choice", Id);
        var confidence = AnswerReader.RequireUnitInterval(
            AnswerReader.RequireNumber(answerObject, "confidence", Id),
            "The 'confidence' value",
            Id);

        var probabilities = AnswerReader.RequireObject(
            AnswerReader.RequireProperty(answerObject, "probabilities", Id),
            Id);

        var probabilitiesByName = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var property in probabilities.EnumerateObject())
        {
            if (probabilitiesByName.ContainsKey(property.Name))
            {
                throw new SystemOneProtocolException(
                    $"The answer for question '{Id}' contains duplicate probabilities for the option '{property.Name}'.");
            }

            if (property.Value.ValueKind != JsonValueKind.Number)
            {
                throw new SystemOneProtocolException(
                    $"The probability for the option '{property.Name}' of question '{Id}' must be a number.");
            }

            probabilitiesByName.Add(
                property.Name,
                AnswerReader.RequireUnitInterval(
                    property.Value.GetDouble(),
                    $"The probability for the option '{property.Name}'",
                    Id));
        }

        var ordered = new List<ChoiceProbability<string>>(names.Length);
        foreach (var name in names)
        {
            if (!probabilitiesByName.TryGetValue(name, out var probability))
            {
                throw new SystemOneProtocolException(
                    $"The answer for question '{Id}' is missing the probability for the option '{name}'.");
            }

            ordered.Add(new ChoiceProbability<string>(name, probability));
        }

        foreach (var name in probabilitiesByName.Keys)
        {
            if (Array.IndexOf(names, name) < 0)
            {
                throw new SystemOneProtocolException(
                    $"The answer for question '{Id}' contains the unknown option '{name}'.");
            }
        }

        if (Array.IndexOf(names, choice) < 0)
        {
            throw new SystemOneProtocolException(
                $"The answer for question '{Id}' selected the unknown option '{choice}'.");
        }

        return new ChoiceAnswer<string>(choice, confidence, ordered);
    }
}
