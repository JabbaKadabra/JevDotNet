using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JevDotNet.Internal;

/// <summary>JSON formatting helpers shared by the request writer and the property counter.</summary>
internal static class JsonFormat
{
    /// <summary>Options used for every payload this library serializes.</summary>
    public static readonly JsonSerializerOptions Default = CreateDefaultOptions();

    /// <summary>
    /// Writes a choice option as its JSON description. Strings stay strings, enums are written by name, and
    /// every other value is serialized as a JSON object or array. Numeric and boolean values are written as
    /// strings because the API accepts string, object, or array descriptions.
    /// </summary>
    /// <param name="writer">The writer that receives the description.</param>
    /// <param name="value">The option value. Must not be null.</param>
    public static void WriteDescription(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                return;
            case string text:
                writer.WriteStringValue(text);
                return;
            case bool flag:
                writer.WriteStringValue(flag ? "true" : "false");
                return;
            case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                writer.WriteStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            default:
                JsonSerializer.Serialize(writer, value, value.GetType(), Default);
                return;
        }
    }

    private static JsonSerializerOptions CreateDefaultOptions() =>
        new() { Converters = { new JsonStringEnumConverter() } };
}
