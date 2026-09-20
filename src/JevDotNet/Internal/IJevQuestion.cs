using System.Text.Json;

namespace JevDotNet.Internal;

/// <summary>The internal contract implemented by every question type that can be sent to the API.</summary>
internal interface IJevQuestion : IQuestion
{
    /// <summary>Writes the question object, without the id property, to the supplied writer.</summary>
    /// <param name="writer">The writer that receives the question.</param>
    void WriteQuestion(Utf8JsonWriter writer);

    /// <summary>Reads the answer element that was returned for this question.</summary>
    /// <param name="answer">The answer element for this question.</param>
    /// <returns>The typed answer object.</returns>
    object ReadAnswer(JsonElement answer);
}
