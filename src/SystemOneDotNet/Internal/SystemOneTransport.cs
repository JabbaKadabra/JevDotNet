using System.Globalization;
using System.Text;
using System.Text.Json;
using SystemOneDotNet.Exceptions;
using SystemOneDotNet.Internal.Json;
using SystemOneDotNet.Internal.Questions;

namespace SystemOneDotNet.Internal;

/// <summary>Builds request payloads, sends them over HTTP, and parses responses into results.</summary>
internal static class SystemOneTransport
{
    private const int MaxErrorBodyLength = 4096;

    public static async Task<ISystemOneResult> SendAsync(
        HttpClient httpClient,
        string apiKey,
        string endpoint,
        string model,
        object state,
        IReadOnlyList<ISystemOneQuestion> questions,
        CancellationToken cancellationToken)
    {
        var payload = BuildPayload(model, state, questions);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey);
        request.Content = new StringContent(
            payload,
            new UTF8Encoding(false),
            "application/json");

        using var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        var body = await ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new SystemOneApiException(response.StatusCode, Truncate(body));
        }

        return ParseResponse(body, questions);
    }

    private static string BuildPayload(string model, object state, IReadOnlyList<ISystemOneQuestion> questions)
    {
        try
        {
            return BuildRequestBody(model, state, questions);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new SystemOneValidationException(
                $"The System One request could not be serialized to JSON: {exception.Message}",
                exception);
        }
    }

    private static string BuildRequestBody(string model, object state, IReadOnlyList<ISystemOneQuestion> questions)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("model", model);

            writer.WritePropertyName("state");
            JsonSerializer.Serialize(writer, state, state.GetType(), JsonFormat.Default);

            writer.WriteStartObject("questions");
            foreach (var question in questions)
            {
                writer.WritePropertyName(question.Id);
                writer.WriteStartObject();
                question.WriteQuestion(writer);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static async Task<string> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, 81920, cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static ISystemOneResult ParseResponse(string body, IReadOnlyList<ISystemOneQuestion> questions)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new SystemOneProtocolException("The SystemOne API returned an empty response body.");
        }

        using var document = ParseDocument(body);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new SystemOneProtocolException("The SystemOne API response body must be a JSON object.");
        }

        var model = AnswerReader.RequireString(root, "model", "response");
        var answersElement = AnswerReader.RequireProperty(root, "answers", "response");
        if (answersElement.ValueKind != JsonValueKind.Object)
        {
            throw new SystemOneProtocolException("The 'answers' property of the response must be a JSON object.");
        }

        var usageElement = AnswerReader.RequireProperty(root, "usage", "response");
        if (usageElement.ValueKind != JsonValueKind.Object)
        {
            throw new SystemOneProtocolException("The 'usage' property of the response must be a JSON object.");
        }

        var usage = new SystemOneUsage(
            ReadTokenCount(usageElement, "input_tokens"),
            ReadTokenCount(usageElement, "output_tokens"));

        var answers = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var question in questions)
        {
            if (!answersElement.TryGetProperty(question.Id, out var answerElement))
            {
                throw new SystemOneProtocolException(
                    $"The response is missing an answer for question '{question.Id}'.");
            }

            answers.Add(question.Id, question.ReadAnswer(answerElement));
        }

        return new SystemOneResult(model, usage, answers);
    }

    private static JsonDocument ParseDocument(string body)
    {
        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException exception)
        {
            throw new SystemOneProtocolException("The SystemOne API returned a malformed JSON response body.", exception);
        }
    }

    private static int ReadTokenCount(JsonElement usage, string name)
    {
        if (!usage.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
        {
            throw new SystemOneProtocolException($"The 'usage.{name}' property of the response must be a number.");
        }

        if (value.TryGetInt32(out var tokens) && tokens >= 0)
        {
            return tokens;
        }

        if (value.TryGetInt64(out var longTokens) && longTokens >= 0 && longTokens <= int.MaxValue)
        {
            return (int)longTokens;
        }

        if (value.TryGetDouble(out var doubleTokens) &&
            doubleTokens >= 0 &&
            doubleTokens <= int.MaxValue &&
            doubleTokens == Math.Floor(doubleTokens))
        {
            return (int)doubleTokens;
        }

        throw new SystemOneProtocolException(
            $"The 'usage.{name}' property of the response is not a valid token count: {value.GetRawText()}.");
    }

    private static string Truncate(string body) =>
        body.Length <= MaxErrorBodyLength
            ? body
            : body.Substring(0, MaxErrorBodyLength) +
              string.Format(
                  CultureInfo.InvariantCulture,
                  "... ({0} characters truncated)",
                  body.Length - MaxErrorBodyLength);
}
