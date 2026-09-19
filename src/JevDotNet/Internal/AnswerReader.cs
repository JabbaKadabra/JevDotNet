using System;
using System.Globalization;
using System.Text.Json;

namespace JevDotNet.Internal
{
    /// <summary>Strict readers for the answer elements returned by the API.</summary>
    internal static class AnswerReader
    {
        public static JsonElement RequireObject(JsonElement element, string questionId)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new JevProtocolException(
                    $"The answer for question '{questionId}' must be a JSON object but was {element.ValueKind}.");
            }

            return element;
        }

        public static JsonElement RequireProperty(JsonElement element, string name, string questionId)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                throw new JevProtocolException(
                    $"The answer for question '{questionId}' is missing the '{name}' property.");
            }

            return value;
        }

        public static void RequireType(JsonElement answer, string expectedType, string questionId)
        {
            var typeElement = RequireProperty(answer, "type", questionId);
            if (typeElement.ValueKind != JsonValueKind.String)
            {
                throw new JevProtocolException(
                    $"The 'type' property of the answer for question '{questionId}' must be a string.");
            }

            var actual = typeElement.GetString();
            if (!string.Equals(actual, expectedType, StringComparison.Ordinal))
            {
                throw new JevProtocolException(
                    $"The answer for question '{questionId}' has type '{actual}' but '{expectedType}' was expected.");
            }
        }

        public static string RequireString(JsonElement element, string name, string questionId)
        {
            var value = RequireProperty(element, name, questionId);
            if (value.ValueKind != JsonValueKind.String)
            {
                throw new JevProtocolException(
                    $"The '{name}' property of the answer for question '{questionId}' must be a string.");
            }

            return value.GetString()!;
        }

        public static double RequireNumber(JsonElement element, string name, string questionId)
        {
            var value = RequireProperty(element, name, questionId);
            if (value.ValueKind != JsonValueKind.Number)
            {
                throw new JevProtocolException(
                    $"The '{name}' property of the answer for question '{questionId}' must be a number.");
            }

            return value.GetDouble();
        }

        public static double RequireUnitInterval(double value, string description, string questionId)
        {
            if (double.IsNaN(value) || value < 0d || value > 1d)
            {
                throw new JevProtocolException(
                    $"{description} for question '{questionId}' must be between 0 and 1 but was {value.ToString(CultureInfo.InvariantCulture)}.");
            }

            return value;
        }

        public static int ParseOptionIndex(string name, int optionCount, string questionId)
        {
            if (!int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out var index) ||
                index < 0 ||
                index >= optionCount)
            {
                throw new JevProtocolException(
                    $"The answer for question '{questionId}' contains the unknown option id '{name}'.");
            }

            return index;
        }
    }
}
