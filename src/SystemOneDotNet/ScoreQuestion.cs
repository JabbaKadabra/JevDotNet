using System.Globalization;
using System.Text.Json;
using SystemOneDotNet.Internal;

namespace SystemOneDotNet;

/// <summary>
/// A score question that rates the state along an ordered list of level descriptions.
/// </summary>
public record ScoreQuestion : IQuestion<ScoreAnswer>, ISystemOneQuestion
{
    private readonly string[] levels;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScoreQuestion"/> class.
    /// </summary>
    /// <param name="id">The identifier that matches the question with its answer inside a batch.</param>
    /// <param name="instructions">The question the model answers.</param>
    /// <param name="levels">The ordered level descriptions. At least two levels are required.</param>
    public ScoreQuestion(string id, string instructions, IEnumerable<string> levels)
    {
        Id = Guard.Id(id);
        Instructions = Guard.Instructions(instructions);
        Guard.NotNull(levels, nameof(levels), "Score levels must not be null.");

        this.levels = levels.ToArray();
        if (this.levels.Length < 2)
        {
            throw new SystemOneValidationException(
                $"Score question '{Id}' must define at least two levels but defines {this.levels.Length}.");
        }

        for (var index = 0; index < this.levels.Length; index++)
        {
            if (this.levels[index] is null)
            {
                throw new SystemOneValidationException(
                    $"Score question '{Id}' contains a null level at index {index}.");
            }

            if (string.IsNullOrWhiteSpace(this.levels[index]))
            {
                throw new SystemOneValidationException(
                    $"Score question '{Id}' contains an empty level at index {index}.");
            }
        }
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Instructions { get; }

    /// <summary>
    /// Gets the level descriptions, in the order they were supplied.
    /// </summary>
    public IReadOnlyList<string> Levels => levels;

    void ISystemOneQuestion.WriteQuestion(Utf8JsonWriter writer)
    {
        writer.WriteString("type", "score");
        writer.WriteString("instructions", Instructions);
        writer.WriteStartArray("criteria");
        foreach (var level in levels)
        {
            writer.WriteStringValue(level);
        }

        writer.WriteEndArray();
    }

    object ISystemOneQuestion.ReadAnswer(JsonElement answer)
    {
        var answerObject = AnswerReader.RequireObject(answer, Id);
        AnswerReader.RequireType(answerObject, "score", Id);

        var score = AnswerReader.RequireNumber(answerObject, "score", Id);
        AnswerReader.RequireRange(score, 0, levels.Length - 1, "The 'score' value", Id);
        var confidence = AnswerReader.RequireUnitInterval(
            AnswerReader.RequireNumber(answerObject, "confidence", Id),
            "The 'confidence' value",
            Id);

        var legend = ReadLegend(answerObject);

        List<ScoreProbability>? probabilities = null;
        if (answerObject.TryGetProperty("probabilities", out var probabilitiesElement) &&
            probabilitiesElement.ValueKind != JsonValueKind.Null)
        {
            probabilities = ReadProbabilities(probabilitiesElement, legend);
        }

        return new ScoreAnswer(score, legend, confidence, probabilities);
    }

    private string[] ReadLegend(JsonElement answerObject)
    {
        var legendElement = AnswerReader.RequireObject(
            AnswerReader.RequireProperty(answerObject, "legend", Id),
            Id);

        var legendByIndex = new string?[levels.Length];
        foreach (var property in legendElement.EnumerateObject())
        {
            var index = AnswerReader.ParseOptionIndex(property.Name, levels.Length, Id);
            if (legendByIndex[index] is not null)
            {
                throw new SystemOneProtocolException(
                    $"The answer for question '{Id}' contains duplicate legend entries for level id '{property.Name}'.");
            }

            if (property.Value.ValueKind != JsonValueKind.String)
            {
                throw new SystemOneProtocolException(
                    $"The legend entry '{property.Name}' of question '{Id}' must be a string.");
            }

            legendByIndex[index] = property.Value.GetString();
        }

        var legend = new string[levels.Length];
        for (var index = 0; index < levels.Length; index++)
        {
            legend[index] = legendByIndex[index] ?? throw new SystemOneProtocolException(
                $"The answer for question '{Id}' is missing the legend entry for level id '{index.ToString(CultureInfo.InvariantCulture)}'.");
        }

        return legend;
    }

    private List<ScoreProbability> ReadProbabilities(JsonElement element, IReadOnlyList<string> legend)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new SystemOneProtocolException(
                $"The 'probabilities' property of the answer for question '{Id}' must be an object.");
        }

        var probabilityByIndex = new double[levels.Length];
        var found = new bool[levels.Length];
        foreach (var property in element.EnumerateObject())
        {
            var index = AnswerReader.ParseOptionIndex(property.Name, levels.Length, Id);
            if (found[index])
            {
                throw new SystemOneProtocolException(
                    $"The answer for question '{Id}' contains duplicate probabilities for level id '{property.Name}'.");
            }

            if (property.Value.ValueKind != JsonValueKind.Number)
            {
                throw new SystemOneProtocolException(
                    $"The probability for level id '{property.Name}' of question '{Id}' must be a number.");
            }

            probabilityByIndex[index] = AnswerReader.RequireUnitInterval(
                property.Value.GetDouble(),
                $"The probability for level id '{property.Name}'",
                Id);
            found[index] = true;
        }

        var probabilities = new List<ScoreProbability>(levels.Length);
        for (var index = 0; index < levels.Length; index++)
        {
            if (!found[index])
            {
                throw new SystemOneProtocolException(
                    $"The answer for question '{Id}' is missing the probability for level id '{index.ToString(CultureInfo.InvariantCulture)}'.");
            }

            probabilities.Add(new ScoreProbability(index, legend[index], probabilityByIndex[index]));
        }

        return probabilities;
    }
}
