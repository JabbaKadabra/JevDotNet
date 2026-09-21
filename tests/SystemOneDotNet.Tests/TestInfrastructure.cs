using System.Net;
using System.Text;
using System.Text.Json;

namespace SystemOneDotNet.Tests;

/// <summary>A request captured by <see cref="RecordingHandler"/>.</summary>
internal sealed class RecordedRequest
{
    public RecordedRequest(HttpRequestMessage message, string body)
    {
        Message = message;
        Body = body;
    }

    public HttpRequestMessage Message { get; }

    public string Body { get; }

    public JsonElement Json => JsonDocument.Parse(Body).RootElement;

    public string? Authorization =>
        Message.Headers.TryGetValues("Authorization", out var values) ? values.FirstOrDefault() : null;

    public string? ContentType => Message.Content?.Headers.ContentType?.MediaType;

    public JsonElement Question(string id) => Json.GetProperty("questions").GetProperty(id);

    public IEnumerable<string> QuestionIds =>
        Json.GetProperty("questions").EnumerateObject().Select(property => property.Name);
}

/// <summary>An <see cref="HttpMessageHandler"/> that records requests and returns canned responses.</summary>
internal sealed class RecordingHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, string, CancellationToken, Task<HttpResponseMessage>> handler;
    private readonly List<RecordedRequest> requests = new();

    public RecordingHandler(Func<HttpRequestMessage, string, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        this.handler = handler;
    }

    public IReadOnlyList<RecordedRequest> Requests => requests;

    public int CallCount => requests.Count;

    public RecordedRequest Last => requests[^1];

    public static RecordingHandler Json(string body, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new((_, _, _) => Task.FromResult(FakeApi.Message(body, statusCode)));

    public static RecordingHandler Responding(Func<RecordedRequest, HttpResponseMessage> factory) =>
        RespondingAsync(request => Task.FromResult(factory(request)));

    public static RecordingHandler RespondingAsync(Func<RecordedRequest, Task<HttpResponseMessage>> factory) =>
        new((message, body, _) => factory(new RecordedRequest(message, body)));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);
        requests.Add(new RecordedRequest(request, body));
        return await handler(request, body, cancellationToken);
    }
}

/// <summary>Helpers that build API-shaped JSON responses.</summary>
internal static class FakeApi
{
    private static readonly JsonSerializerOptions SnakeCase = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public static string Serialize(object payload) => JsonSerializer.Serialize(payload, SnakeCase);

    public static HttpResponseMessage Message(string body, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Response(object payload, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        Message(Serialize(payload), statusCode);

    public static object Envelope(object answers, string model = "jev-latest", int inputTokens = 312, int outputTokens = 48) =>
        new { model, answers, usage = new { input_tokens = inputTokens, output_tokens = outputTokens } };

    public static object ChoiceAnswer(string choice, double confidence, Dictionary<string, double> probabilities) =>
        new { type = "choice", choice, probabilities, confidence };

    public static object ScoreAnswer(
        double score,
        double confidence,
        Dictionary<string, string> legend,
        Dictionary<string, double>? probabilities = null) =>
        probabilities is null
            ? (object)new { type = "score", score, legend, confidence }
            : new { type = "score", score, legend, probabilities, confidence };

    public static object NoulAnswer(double noul) => new { type = "noul", noul };
}

/// <summary>Shared client construction for tests.</summary>
internal static class TestClient
{
    public const string ApiKey = "test-key";

    public static ISystemOneClient Create(RecordingHandler handler, SystemOneOptions? options = null) =>
        SystemOneClient.Create(ApiKey, options, new HttpClient(handler));
}
