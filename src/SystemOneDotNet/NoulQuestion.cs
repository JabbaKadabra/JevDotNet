using System.Text.Json;
using SystemOneDotNet.Internal;

namespace SystemOneDotNet;

/// <summary>
/// A noul (yes/no) question. The answer is the probability that the answer is yes.
/// </summary>
public record NoulQuestion : IQuestion<NoulAnswer>, ISystemOneQuestion
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoulQuestion"/> class.
    /// </summary>
    /// <param name="id">The identifier that matches the question with its answer inside a batch.</param>
    /// <param name="instructions">The yes/no question to evaluate.</param>
    /// <param name="trueDescription">An optional description of what a yes means.</param>
    /// <param name="falseDescription">An optional description of what a no means.</param>
    public NoulQuestion(string id, string instructions, string? trueDescription = null, string? falseDescription = null)
    {
        Id = Guard.Id(id);
        Instructions = Guard.Instructions(instructions);
        TrueDescription = trueDescription;
        FalseDescription = falseDescription;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Instructions { get; }

    /// <summary>
    /// Gets the optional description of what a yes (value near 1) means.
    /// </summary>
    public string? TrueDescription { get; }

    /// <summary>
    /// Gets the optional description of what a no (value near 0) means.
    /// </summary>
    public string? FalseDescription { get; }

    void ISystemOneQuestion.WriteQuestion(Utf8JsonWriter writer)
    {
        writer.WriteString("type", "noul");
        writer.WriteString("instructions", Instructions);

        if (TrueDescription is not null || FalseDescription is not null)
        {
            writer.WriteStartObject("criteria");
            if (TrueDescription is not null)
            {
                writer.WriteString("true", TrueDescription);
            }

            if (FalseDescription is not null)
            {
                writer.WriteString("false", FalseDescription);
            }

            writer.WriteEndObject();
        }
    }

    object ISystemOneQuestion.ReadAnswer(JsonElement answer)
    {
        var answerObject = AnswerReader.RequireObject(answer, Id);
        AnswerReader.RequireType(answerObject, "noul", Id);

        var noul = AnswerReader.RequireUnitInterval(
            AnswerReader.RequireNumber(answerObject, "noul", Id),
            "The 'noul' value",
            Id);

        return new NoulAnswer(noul);
    }
}
