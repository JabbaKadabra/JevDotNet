using System.Text.Json;
using SystemOneDotNet.Answers;
using SystemOneDotNet.Internal.Json;
using SystemOneDotNet.Questions;

namespace SystemOneDotNet.Internal.Questions;

/// <summary>
/// A noul (yes/no) question. The answer is the probability that the answer is yes.
/// </summary>
internal sealed class NoulQuestion : INoulQuestion, ISystemOneQuestion
{
    public NoulQuestion(string id, string instructions, string? trueDescription, string? falseDescription)
    {
        Id = Guard.Id(id);
        Instructions = Guard.Instructions(instructions);
        TrueDescription = trueDescription;
        FalseDescription = falseDescription;
    }

    public string Id { get; }

    public string Instructions { get; }

    public string? TrueDescription { get; }

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
