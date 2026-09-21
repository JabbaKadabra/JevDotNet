using System.Globalization;
using System.Text.Json;
using SystemOneDotNet.Exceptions;

namespace SystemOneDotNet.Internal.Json;

/// <summary>
/// Strict readers for the answer elements returned by the API.
/// </summary>
internal static class AnswerReader
{
    /// <summary>
    /// Float tolerance for comparing parsed numbers against their allowed ranges.
    /// </summary>
    private const double Tolerance = 1e-6;

    public static JsonElement RequireObject(JsonElement element, string questionId)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new SystemOneProtocolException(
                $"The answer for question '{questionId}' must be a JSON object but was {element.ValueKind}.");
        }

        return element;
    }

    public static JsonElement RequireProperty(JsonElement element, string name, string questionId)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            throw new SystemOneProtocolException(
                $"The answer for question '{questionId}' is missing the '{name}' property.");
        }

        return value;
    }

    public static void RequireType(JsonElement answer, string expectedType, string questionId)
    {
        var typeElement = RequireProperty(answer, "type", questionId);
        if (typeElement.ValueKind != JsonValueKind.String)
        {
            throw new SystemOneProtocolException(
                $"The 'type' property of the answer for question '{questionId}' must be a string.");
        }

        var actual = typeElement.GetString();
        if (!string.Equals(actual, expectedType, StringComparison.Ordinal))
        {
            throw new SystemOneProtocolException(
                $"The answer for question '{questionId}' has type '{actual}' but '{expectedType}' was expected.");
        }
    }

    public static string RequireString(JsonElement element, string name, string questionId)
    {
        var value = RequireProperty(element, name, questionId);
        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        return text ?? throw new SystemOneProtocolException(
            $"The '{name}' property of the answer for question '{questionId}' must be a string.");
    }

    public static double RequireNumber(JsonElement element, string name, string questionId)
    {
        var value = RequireProperty(element, name, questionId);
        if (value.ValueKind != JsonValueKind.Number)
        {
            throw new SystemOneProtocolException(
                $"The '{name}' property of the answer for question '{questionId}' must be a number.");
        }

        return value.GetDouble();
    }

    public static double RequireUnitInterval(double value, string description, string questionId)
    {
        if (double.IsNaN(value) || value < 0d || value > 1d)
        {
            throw new SystemOneProtocolException(
                $"{description} for question '{questionId}' must be between 0 and 1 but was {Format(value)}.");
        }

        return value;
    }

    public static double RequireRange(double value, double minimum, double maximum, string description, string questionId)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < minimum - Tolerance || value > maximum + Tolerance)
        {
            throw new SystemOneProtocolException(
                $"{description} for question '{questionId}' must be between {Format(minimum)} and {Format(maximum)} but was {Format(value)}.");
        }

        return value;
    }

    public static int ParseOptionIndex(string name, int optionCount, string questionId)
    {
        if (!int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out var index) ||
            index >= optionCount ||
            !string.Equals(name, index.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
        {
            throw new SystemOneProtocolException(
                $"The answer for question '{questionId}' contains the unknown option id '{name}'.");
        }

        return index;
    }

    private static string Format(double value) =>
        double.IsNaN(value) || double.IsInfinity(value)
            ? value.ToString("R", CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
}
